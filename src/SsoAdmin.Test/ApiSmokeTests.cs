using Microsoft.AspNetCore.Mvc.Testing;

namespace SsoAdmin.Test;

/// <summary>
/// Smoke tests that verify the <c>SsoAdmin.API</c> host starts up without throwing, per the
/// Required tests of Block 1 (scaffolding) of spec-FEAT-001.
/// </summary>
public class ApiSmokeTests
{
    /// <summary>
    /// The ASP.NET Core host for <c>SsoAdmin.API</c> must build and start without raising any
    /// exception when a client is created against it.
    /// </summary>
    [Fact]
    public void ApiHost_StartsUp_WithoutThrowing()
    {
        using WebApplicationFactory<Program> factory = new();

        using HttpClient client = factory.CreateClient();

        Assert.NotNull(client);
    }
}
