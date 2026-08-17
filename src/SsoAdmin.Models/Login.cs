namespace SsoAdmin.Models;

/// <summary>
/// Representa las credenciales de un administrador de SI para ingresar a esta aplicación. No tiene
/// relación con <see cref="Usuario"/>: son conceptos distintos, <see cref="Login"/> son las
/// credenciales de SI para entrar a esta app, <see cref="Usuario"/> son las personas que SI
/// administra para el SSO.
/// </summary>
public class Login
{
    /// <summary>
    /// Identificador único del login.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Nombre de usuario, único, requerido, máximo 100 caracteres.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Hash de la contraseña, requerido. Nunca se persiste la contraseña en texto plano ni ningún
    /// derivado reversible: sólo el hash producido por <c>PasswordHasher&lt;T&gt;.HashPassword</c>.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;
}
