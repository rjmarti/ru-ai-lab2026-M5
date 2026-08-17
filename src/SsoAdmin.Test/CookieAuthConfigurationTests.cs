extern alias WebHost;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WebProgram = WebHost::Program;

namespace SsoAdmin.Test;

/// <summary>
/// Verifica que el cookie de autenticación quede registrado con el hardening exigido por la
/// mitigación R2 del threat model (Block 3 de spec-FEAT-001): <c>HttpOnly</c>,
/// <c>SecurePolicy=Always</c>, <c>SameSite=Strict</c>, expiración de 8h con sliding expiration, y
/// <c>LoginPath=/Login</c> (mecanismo del que Block 4 depende para proteger <c>/Usuarios/*</c>).
/// </summary>
public class CookieAuthConfigurationTests : IDisposable
{
    private readonly string _dbPath;
    private readonly WebApplicationFactory<WebProgram> _factory;

    public CookieAuthConfigurationTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"ssoadmin-cookie-cfg-test-{Guid.NewGuid():N}.db");

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

    [Fact]
    public void CookieAuthentication_QuedaConfiguradaConElHardeningDeR2()
    {
        IOptionsMonitor<CookieAuthenticationOptions> monitor =
            _factory.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>();
        CookieAuthenticationOptions options = monitor.Get(CookieAuthenticationDefaults.AuthenticationScheme);

        Assert.Equal("/Login", options.LoginPath.Value);
        Assert.True(options.Cookie.HttpOnly);
        Assert.Equal(CookieSecurePolicy.Always, options.Cookie.SecurePolicy);
        Assert.Equal(SameSiteMode.Strict, options.Cookie.SameSite);
        Assert.Equal(TimeSpan.FromHours(8), options.ExpireTimeSpan);
        Assert.True(options.SlidingExpiration);
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
