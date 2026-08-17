using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SsoAdmin.Application.Auth;
using SsoAdmin.Application.Seed;
using SsoAdmin.Data;
using SsoAdmin.Models;

namespace SsoAdmin.Test;

/// <summary>
/// Tests de integración de Block 2 de spec-FEAT-001: migración de esquema y seed del login
/// <c>admin</c>/<c>admin</c> sobre una base SQLite en archivo temporal.
/// </summary>
public class AdminLoginSeederTests : IDisposable
{
    private readonly string _dbPath;
    private readonly IPasswordHasherService _hasher;

    public AdminLoginSeederTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"ssoadmin-test-{Guid.NewGuid():N}.db");
        _hasher = new PasswordHasherService();
    }

    /// <summary>
    /// Crea un <see cref="SsoAdminDbContext"/> apuntando a un archivo SQLite en la ruta indicada.
    /// </summary>
    private static SsoAdminDbContext CreateContext(string dbPath)
    {
        DbContextOptions<SsoAdminDbContext> options = new DbContextOptionsBuilder<SsoAdminDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        return new SsoAdminDbContext(options);
    }

    [Fact]
    public async Task Seed_SobreBaseVacia_CreaAdminConHashVerificable()
    {
        await using SsoAdminDbContext db = CreateContext(_dbPath);
        await db.Database.MigrateAsync();

        await AdminLoginSeeder.SeedAsync(db, _hasher);

        List<Login> logins = await db.Logins.ToListAsync();
        Login admin = Assert.Single(logins);
        Assert.Equal("admin", admin.Username);

        PasswordHasher<Login> verifier = new();
        PasswordVerificationResult result = verifier.VerifyHashedPassword(
            user: null!,
            hashedPassword: admin.PasswordHash,
            providedPassword: "admin");

        Assert.Equal(PasswordVerificationResult.Success, result);
    }

    [Fact]
    public async Task Seed_EjecutadoDosVeces_NoDuplicaElAdmin()
    {
        await using SsoAdminDbContext db = CreateContext(_dbPath);
        await db.Database.MigrateAsync();

        await AdminLoginSeeder.SeedAsync(db, _hasher);
        await AdminLoginSeeder.SeedAsync(db, _hasher);

        int cantidad = await db.Logins.CountAsync();
        Assert.Equal(1, cantidad);
    }

    [Fact]
    public async Task EsquemaDeLogin_NoTieneColumnaDeContraseniaEnTextoPlano()
    {
        await using SsoAdminDbContext db = CreateContext(_dbPath);
        await db.Database.MigrateAsync();
        await AdminLoginSeeder.SeedAsync(db, _hasher);

        IEntityType entityType = db.Model.FindEntityType(typeof(Login))!;
        List<string> nombresDeColumnas = entityType.GetProperties()
            .Select(propiedad => propiedad.Name)
            .ToList();

        Assert.Contains("PasswordHash", nombresDeColumnas);
        Assert.DoesNotContain("Password", nombresDeColumnas);
        Assert.DoesNotContain(nombresDeColumnas, nombre => nombre.Contains("PlainText", StringComparison.OrdinalIgnoreCase));

        Login admin = await db.Logins.SingleAsync();
        Assert.NotEqual("admin", admin.PasswordHash);
    }

    [Fact]
    public async Task Migrate_ConRutaInvalida_LanzaExcepcion()
    {
        string rutaInvalida = Path.Combine(
            Path.GetTempPath(),
            $"directorio-inexistente-{Guid.NewGuid():N}",
            "ssoadmin.db");

        await using SsoAdminDbContext db = CreateContext(rutaInvalida);

        await Assert.ThrowsAnyAsync<Exception>(() => db.Database.MigrateAsync());
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }

        GC.SuppressFinalize(this);
    }
}
