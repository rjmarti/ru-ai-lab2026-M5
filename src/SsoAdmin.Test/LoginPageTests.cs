extern alias WebHost;

using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using WebProgram = WebHost::Program;

namespace SsoAdmin.Test;

/// <summary>
/// Tests de integración de la página <c>/Login</c> (Block 3 de spec-FEAT-001), contra
/// <c>WebApplicationFactory&lt;Program&gt;</c> de <c>SsoAdmin.Web</c>. Cada test usa un archivo
/// SQLite temporal propio (vía override de la connection string), sobre el cual
/// <c>Program.cs</c> aplica migración + seed automáticamente al levantar el host, igual que en
/// el arranque real.
/// </summary>
public class LoginPageTests : IDisposable
{
    private readonly string _dbPath;
    private readonly WebApplicationFactory<WebProgram> _factory;

    public LoginPageTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"ssoadmin-web-test-{Guid.NewGuid():N}.db");

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
    /// Hace <c>GET /Login</c> y extrae el token antiforgery del formulario y las cookies de la
    /// respuesta (Razor Pages valida el antiforgery token por default en todo POST; no se
    /// deshabilita esa protección, per R3 del threat model).
    /// </summary>
    private static async Task<(string Token, string CookieHeader)> GetAntiforgeryAsync(HttpClient client)
    {
        HttpResponseMessage response = await client.GetAsync("/Login");
        response.EnsureSuccessStatusCode();

        string html = await response.Content.ReadAsStringAsync();
        Match match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");

        if (!match.Success)
        {
            throw new InvalidOperationException("No se encontró el antiforgery token en /Login.");
        }

        string token = match.Groups[1].Value;

        IEnumerable<string> setCookies = response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? values)
            ? values
            : [];
        string cookieHeader = string.Join("; ", setCookies.Select(cookie => cookie.Split(';')[0]));

        return (token, cookieHeader);
    }

    private static HttpRequestMessage BuildLoginRequest(string username, string password, string token, string cookieHeader)
    {
        Dictionary<string, string> form = new()
        {
            ["Username"] = username,
            ["Password"] = password,
            ["__RequestVerificationToken"] = token,
        };

        HttpRequestMessage request = new(HttpMethod.Post, "/Login")
        {
            Content = new FormUrlEncodedContent(form),
        };
        request.Headers.Add("Cookie", cookieHeader);

        return request;
    }

    [Fact]
    public async Task PostLogin_ConCredencialesValidas_SeteaCookieDeAuthConHttpOnlyYSameSiteStrict()
    {
        using HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        (string token, string cookieHeader) = await GetAntiforgeryAsync(client);

        HttpResponseMessage response = await client.SendAsync(BuildLoginRequest("admin", "admin", token, cookieHeader));

        Assert.True(response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? setCookies));
        List<string> cookies = setCookies!.ToList();
        string? authCookie = cookies.FirstOrDefault(cookie => cookie.StartsWith(".AspNetCore.Cookies", StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(authCookie);
        Assert.Contains("httponly", authCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", authCookie.Replace(" ", string.Empty), StringComparison.OrdinalIgnoreCase);

        // Redirect("/Usuarios") de LoginModel.OnPostAsync produce un 302 Found (RedirectResult no
        // permanente); se verifica el status y el destino sin seguir la redirección (AllowAutoRedirect
        // = false en el HttpClient), para poder inspeccionar la respuesta de login en sí.
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Equal("/Usuarios", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task PostLogin_ConCredencialesInvalidas_NoSeteaCookieDeAuth()
    {
        using HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        (string token, string cookieHeader) = await GetAntiforgeryAsync(client);

        HttpResponseMessage response = await client.SendAsync(BuildLoginRequest("admin", "wrong", token, cookieHeader));

        bool tieneCookieDeAuth = response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? setCookies)
            && setCookies.Any(cookie => cookie.StartsWith(".AspNetCore.Cookies", StringComparison.OrdinalIgnoreCase));

        Assert.False(tieneCookieDeAuth);

        // LoginModel.OnPostAsync setea explícitamente Response.StatusCode = 401 antes de
        // re-renderizar la página (a diferencia del 200 OK que Razor Pages devuelve por default
        // ante un ModelState inválido), y el mensaje de error genérico (mitigación R6 del threat
        // model, no distingue usuario inexistente de contraseña incorrecta) queda en el body HTML.
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        // Se decodifica el body porque Razor HTML-encodea entidades no ASCII (la "ñ" queda como
        // "&#xF1;" en el HTML crudo); se compara contra el texto literal del mensaje de error.
        string body = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains("Usuario o contraseña incorrectos.", body);
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
