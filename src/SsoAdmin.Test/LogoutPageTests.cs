extern alias WebHost;

using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using WebProgram = WebHost::Program;

namespace SsoAdmin.Test;

/// <summary>
/// Tests de integración de la página <c>/Logout</c> (Block 3 de spec-FEAT-001), contra
/// <c>WebApplicationFactory&lt;Program&gt;</c> de <c>SsoAdmin.Web</c>, siguiendo el mismo patrón
/// que <c>LoginPageTests</c>.
/// </summary>
public class LogoutPageTests : IDisposable
{
    private readonly string _dbPath;
    private readonly WebApplicationFactory<WebProgram> _factory;

    public LogoutPageTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"ssoadmin-logout-test-{Guid.NewGuid():N}.db");

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
    /// Crea un <see cref="HttpClient"/> con <c>BaseAddress=https://localhost</c>, imprescindible
    /// para que la cookie de auth (<c>SecurePolicy=CookieSecurePolicy.Always</c>) viaje de vuelta
    /// en los requests subsiguientes. Mismo criterio que <c>UsuariosPageTests</c>.
    /// </summary>
    private HttpClient CreateAuthenticatedClient(bool allowAutoRedirect = true)
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = allowAutoRedirect,
        });
    }

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
    public async Task PostLogout_EstandoAutenticado_RedirigeALoginYCierraLaSesion()
    {
        using HttpClient client = CreateAuthenticatedClient(allowAutoRedirect: false);

        await LoginAsAdminAsync(client);

        // El token antiforgery se obtiene de /Usuarios (requiere sesión activa): el form de
        // logout de _Layout.cshtml se renderiza ahí porque User.Identity.IsAuthenticated es true.
        string token = await GetAntiforgeryTokenAsync(client, "/Usuarios");

        Dictionary<string, string> form = new()
        {
            ["__RequestVerificationToken"] = token,
        };

        HttpResponseMessage logoutResponse = await client.PostAsync("/Logout", new FormUrlEncodedContent(form));

        Assert.Equal(HttpStatusCode.Found, logoutResponse.StatusCode);
        Assert.NotNull(logoutResponse.Headers.Location);
        Assert.StartsWith("/Login", logoutResponse.Headers.Location!.OriginalString);

        HttpResponseMessage usuariosDespuesDeLogout = await client.GetAsync("/Usuarios");

        Assert.Equal(HttpStatusCode.Redirect, usuariosDespuesDeLogout.StatusCode);
        Assert.NotNull(usuariosDespuesDeLogout.Headers.Location);
        Assert.StartsWith("/Login", usuariosDespuesDeLogout.Headers.Location!.AbsolutePath);
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
