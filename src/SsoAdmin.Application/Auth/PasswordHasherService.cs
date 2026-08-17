using Microsoft.AspNetCore.Identity;
using SsoAdmin.Models;

namespace SsoAdmin.Application.Auth;

/// <summary>
/// Implementación de <see cref="IPasswordHasherService"/> que usa
/// <see cref="PasswordHasher{TUser}"/> como adaptador standalone (sin traer el resto de ASP.NET
/// Identity). El genérico <see cref="Login"/> no se usa realmente por <c>PasswordHasher</c>: es
/// sólo el tipo requerido por su firma.
/// </summary>
public class PasswordHasherService : IPasswordHasherService
{
    private readonly PasswordHasher<Login> _passwordHasher = new();

    /// <inheritdoc />
    public string Hash(string password)
    {
        return _passwordHasher.HashPassword(user: null!, password: password);
    }
}
