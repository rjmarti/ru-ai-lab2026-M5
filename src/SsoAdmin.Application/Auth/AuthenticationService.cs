using Microsoft.EntityFrameworkCore;
using SsoAdmin.Data;
using SsoAdmin.Models;

namespace SsoAdmin.Application.Auth;

/// <summary>
/// Implementación de <see cref="IAuthenticationService"/> que busca el <see cref="Login"/> por
/// <c>Username</c> en <see cref="SsoAdminDbContext"/> y verifica la contraseña con
/// <see cref="IPasswordHasherService"/>.
/// </summary>
/// <param name="db">Contexto de base de datos, inyectado por DI.</param>
/// <param name="hasher">Servicio de hasheo/verificación de contraseñas, inyectado por DI.</param>
public class AuthenticationService(SsoAdminDbContext db, IPasswordHasherService hasher) : IAuthenticationService
{
    /// <inheritdoc />
    public async Task<bool> ValidateCredentialsAsync(string username, string password)
    {
        Login? login = await db.Logins.SingleOrDefaultAsync(l => l.Username == username);

        if (login is null)
        {
            return false;
        }

        return hasher.Verify(login.PasswordHash, password);
    }
}
