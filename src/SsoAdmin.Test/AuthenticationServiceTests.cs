using Microsoft.EntityFrameworkCore;
using SsoAdmin.Application.Auth;
using SsoAdmin.Application.Seed;
using SsoAdmin.Data;

namespace SsoAdmin.Test;

/// <summary>
/// Tests de <see cref="AuthenticationService"/> (Block 3 de spec-FEAT-001), contra una base SQLite
/// en archivo temporal con el seed <c>admin</c>/<c>admin</c> aplicado, igual que
/// <c>AdminLoginSeederTests</c> de Block 2.
/// </summary>
public class AuthenticationServiceTests : IAsyncLifetime
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"ssoadmin-auth-test-{Guid.NewGuid():N}.db");
    private SsoAdminDbContext _db = null!;
    private IAuthenticationService _sut = null!;

    public async Task InitializeAsync()
    {
        DbContextOptions<SsoAdminDbContext> options = new DbContextOptionsBuilder<SsoAdminDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;

        _db = new SsoAdminDbContext(options);
        await _db.Database.MigrateAsync();

        IPasswordHasherService hasher = new PasswordHasherService();
        await AdminLoginSeeder.SeedAsync(_db, hasher);

        _sut = new AuthenticationService(_db, hasher);
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();

        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    [Fact]
    public async Task ValidateCredentialsAsync_ConCredencialesCorrectas_DevuelveTrue()
    {
        bool resultado = await _sut.ValidateCredentialsAsync("admin", "admin");

        Assert.True(resultado);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_ConContraseniaIncorrecta_DevuelveFalse()
    {
        bool resultado = await _sut.ValidateCredentialsAsync("admin", "wrong");

        Assert.False(resultado);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_ConUsuarioInexistente_DevuelveFalse()
    {
        bool resultado = await _sut.ValidateCredentialsAsync("inexistente", "admin");

        Assert.False(resultado);
    }
}
