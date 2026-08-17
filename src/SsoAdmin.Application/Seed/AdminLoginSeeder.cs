using Microsoft.EntityFrameworkCore;
using SsoAdmin.Application.Auth;
using SsoAdmin.Data;
using SsoAdmin.Models;

namespace SsoAdmin.Application.Seed;

/// <summary>
/// Siembra el login <c>admin</c>/<c>admin</c> al arrancar la aplicación, sólo si la tabla
/// <see cref="Login"/> está vacía. Idempotente: correrlo más de una vez no duplica el registro.
/// </summary>
/// <remarks>
/// Vive en <c>SsoAdmin.Application</c> (y no en <c>SsoAdmin.Data</c>, como sugiere el spec de
/// Block 2) porque depende de <see cref="IPasswordHasherService"/>, que vive en
/// <c>SsoAdmin.Application.Auth</c>. <c>SsoAdmin.Application</c> ya referencia
/// <c>SsoAdmin.Data</c> (grafo de Block 1); la referencia inversa (Data → Application) crearía un
/// ciclo de proyectos que no compila. Es un supuesto documentado, no un desvío arbitrario.
/// </remarks>
public static class AdminLoginSeeder
{
    private const string AdminUsername = "admin";
    private const string AdminPassword = "admin";

    /// <summary>
    /// Crea el login <c>admin</c>/<c>admin</c> si la tabla <see cref="Login"/> está vacía.
    /// </summary>
    /// <param name="db">Contexto de base de datos ya migrado.</param>
    /// <param name="hasher">Servicio usado para hashear la contraseña del seed.</param>
    public static async Task SeedAsync(SsoAdminDbContext db, IPasswordHasherService hasher)
    {
        bool hayLogins = await db.Logins.AnyAsync();

        if (hayLogins)
        {
            return;
        }

        Login adminLogin = new()
        {
            Username = AdminUsername,
            PasswordHash = hasher.Hash(AdminPassword),
        };

        db.Logins.Add(adminLogin);
        await db.SaveChangesAsync();
    }
}
