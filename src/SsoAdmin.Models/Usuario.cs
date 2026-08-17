namespace SsoAdmin.Models;

/// <summary>
/// Representa a una persona administrada por el SSO. No debe confundirse con <see cref="Login"/>,
/// que son las credenciales usadas por el administrador de SI para ingresar a esta aplicación.
/// </summary>
public class Usuario
{
    /// <summary>
    /// Identificador único del usuario.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Nombre del usuario. Requerido, máximo 200 caracteres.
    /// </summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Indica si el usuario está activo. Por defecto <c>true</c>. La baja es lógica: nunca se
    /// elimina físicamente el registro.
    /// </summary>
    public bool Activo { get; set; } = true;
}
