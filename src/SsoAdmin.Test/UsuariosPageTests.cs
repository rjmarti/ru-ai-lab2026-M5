extern alias WebHost;

using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SsoAdmin.Data;
using SsoAdmin.Models;
using WebProgram = WebHost::Program;

namespace SsoAdmin.Test;

/// <summary>
/// Tests de integración de las páginas <c>/Usuarios/*</c> (Block 4 de spec-FEAT-001), contra
/// <c>WebApplicationFactory&lt;Program&gt;</c> de <c>SsoAdmin.Web</c>, siguiendo el mismo patrón
/// que <c>LoginPageTests</c> de Block 3.
/// </summary>
public class UsuariosPageTests : IDisposable
{
    private readonly string _dbPath;
    private readonly WebApplicationFactory<WebProgram> _factory;

    public UsuariosPageTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"ssoadmin-usuarios-test-{Guid.NewGuid():N}.db");

        _factory = new WebApplicationFactory<WebProgram>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = $"Data Source={_dbPath}",
                });
            });
        });
    }

    /// <summary>
    /// Crea un <see cref="HttpClient"/> con <c>BaseAddress=https://localhost</c>. Es
    /// imprescindible para los tests autenticados: la cookie de auth tiene
    /// <c>SecurePolicy=CookieSecurePolicy.Always</c> (mitigación R2, Block 3), y el cookie jar
    /// automático del <see cref="HttpClient"/> (<c>HandleCookies=true</c> por default en
    /// <see cref="WebApplicationFactory{TEntryPoint}"/>) sólo reenvía una cookie <c>Secure</c> en
    /// requests con scheme <c>https</c> — con el <c>http://localhost</c> por default, la cookie de
    /// sesión nunca viajaría de vuelta en el siguiente request y la autenticación se perdería.
    /// </summary>
    private HttpClient CreateAuthenticatedClient(bool allowAutoRedirect = true)
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = allowAutoRedirect,
        });
    }

    /// <summary>
    /// Siembra usuarios directamente contra la base de datos, resolviendo <see cref="SsoAdminDbContext"/>
    /// desde el <see cref="WebApplicationFactory{TEntryPoint}.Services"/> real del host bajo test
    /// (en vez de abrir una conexión propia con la connection string de <see cref="_dbPath"/>): así
    /// se siembra exactamente en la misma base que la aplicación consulta en cada request, sin
    /// depender de si/cuándo el override de configuración del <c>ConfigureAppConfiguration</c> del
    /// constructor termina de aplicarse.
    /// </summary>
    private async Task SeedUsuarioAsync(string nombre, bool activo)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        SsoAdminDbContext db = scope.ServiceProvider.GetRequiredService<SsoAdminDbContext>();

        db.Usuarios.Add(new Usuario { Nombre = nombre, Activo = activo });
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Hace <c>GET</c> a <paramref name="path"/> y extrae el token antiforgery del formulario
    /// (Razor Pages valida el antiforgery token por default en todo POST; no se deshabilita esa
    /// protección, per R3 del threat model). La cookie antiforgery asociada queda guardada
    /// automáticamente por el <see cref="HttpClient"/> (<c>HandleCookies=true</c> por default en
    /// <see cref="WebApplicationFactory{TEntryPoint}"/>).
    /// </summary>
    private static async Task<string> GetAntiforgeryTokenAsync(HttpClient client, string path)
    {
        HttpResponseMessage response = await client.GetAsync(path);
        response.EnsureSuccessStatusCode();

        string html = await response.Content.ReadAsStringAsync();
        Match match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");

        if (!match.Success)
        {
            throw new InvalidOperationException($"No se encontró el antiforgery token en {path}.");
        }

        return match.Groups[1].Value;
    }

    /// <summary>
    /// Hace <c>POST /Login</c> con <c>admin</c>/<c>admin</c> en el mismo <see cref="HttpClient"/>
    /// (que conserva cookies automáticamente) para dejar la sesión autenticada lista para los
    /// requests subsiguientes contra <c>/Usuarios</c>.
    /// </summary>
    private static async Task LoginAsAdminAsync(HttpClient client)
    {
        string token = await GetAntiforgeryTokenAsync(client, "/Login");

        Dictionary<string, string> form = new()
        {
            ["Username"] = "admin",
            ["Password"] = "admin",
            ["__RequestVerificationToken"] = token,
        };

        HttpResponseMessage response = await client.PostAsync("/Login", new FormUrlEncodedContent(form));

        if (response.StatusCode is not HttpStatusCode.Found and not HttpStatusCode.OK)
        {
            throw new InvalidOperationException($"El login de admin/admin falló en el setup del test (status {response.StatusCode}).");
        }
    }

    [Fact]
    public async Task GetUsuarios_Autenticado_Devuelve200YElListado()
    {
        using HttpClient client = CreateAuthenticatedClient();

        // Dispara el arranque del host (y con él, la migración del esquema — Program.cs corre
        // `Database.MigrateAsync()` al iniciar) antes de sembrar.
        await LoginAsAdminAsync(client);

        await SeedUsuarioAsync("Usuario De Prueba Listado", activo: true);
        await SeedUsuarioAsync("Usuario De Prueba Inactivo", activo: false);

        HttpResponseMessage response = await client.GetAsync("/Usuarios");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Usuarios", body);

        // Verifica que el usuario de prueba realmente aparece en el listado, con su estado
        // correcto, y no sólo que la página cargó (Index.cshtml renderiza cada fila como
        // "<td>{Nombre}</td><td>{Activo|Inactivo}</td>").
        Assert.Matches(
            new Regex(@"<td>Usuario De Prueba Listado</td>\s*<td>Activo</td>"),
            body);
        Assert.Matches(
            new Regex(@"<td>Usuario De Prueba Inactivo</td>\s*<td>Inactivo</td>"),
            body);
    }

    [Fact]
    public async Task GetUsuarios_SinCookieDeSesion_RedirigeALogin()
    {
        using HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        HttpResponseMessage response = await client.GetAsync("/Usuarios");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        // El Location puede llegar como URI absoluta (ej. "http://localhost/Login?ReturnUrl=...")
        // según el cliente; se compara sólo el path para no acoplarse al scheme/host.
        Assert.StartsWith("/Login", response.Headers.Location!.AbsolutePath);
    }

    [Fact]
    public async Task GetEditarUsuarioInexistente_Devuelve404()
    {
        using HttpClient client = CreateAuthenticatedClient();

        await LoginAsAdminAsync(client);

        HttpResponseMessage response = await client.GetAsync("/Usuarios/Edit/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostDarDeBaja_ConIdInexistente_Devuelve404()
    {
        using HttpClient client = CreateAuthenticatedClient(allowAutoRedirect: false);

        await LoginAsAdminAsync(client);

        string token = await GetAntiforgeryTokenAsync(client, "/Usuarios");

        Dictionary<string, string> form = new()
        {
            ["__RequestVerificationToken"] = token,
        };

        HttpResponseMessage response = await client.PostAsync("/Usuarios?handler=DarDeBaja&id=9999", new FormUrlEncodedContent(form));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    public void Dispose()
    {
        _factory.Dispose();

        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }

        GC.SuppressFinalize(this);
    }
}
