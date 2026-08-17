namespace SsoAdmin.Application.Usuarios;

/// <summary>
/// DTO de <see cref="SsoAdmin.Models.Usuario"/> expuesto fuera de <c>SsoAdmin.Application</c>. La
/// entidad de dominio <see cref="SsoAdmin.Models.Usuario"/> nunca cruza a <c>SsoAdmin.Web</c>.
/// </summary>
public class UsuarioDto
{
    /// <summary>
    /// Identificador único del usuario.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Nombre del usuario.
    /// </summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Indica si el usuario está activo (baja lógica).
    /// </summary>
    public bool Activo { get; set; }
}
