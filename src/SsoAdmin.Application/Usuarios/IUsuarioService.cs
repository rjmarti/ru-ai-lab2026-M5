namespace SsoAdmin.Application.Usuarios;

/// <summary>
/// Casos de uso de administración de <see cref="SsoAdmin.Models.Usuario"/> (alta, edición de
/// nombre, baja lógica y listado).
/// </summary>
public interface IUsuarioService
{
    /// <summary>
    /// Lista todos los usuarios (activos e inactivos).
    /// </summary>
    Task<List<UsuarioDto>> ListarAsync();

    /// <summary>
    /// Obtiene un único usuario por su identificador.
    /// </summary>
    /// <param name="id">Identificador del usuario a buscar.</param>
    /// <returns>
    /// El <see cref="UsuarioDto"/> correspondiente, o <c>null</c> si no existe un usuario con ese
    /// <paramref name="id"/> (el <c>PageModel</c> traduce esto a <c>NotFound()</c>).
    /// </returns>
    Task<UsuarioDto?> ObtenerPorIdAsync(int id);

    /// <summary>
    /// Crea un usuario nuevo, activo por defecto.
    /// </summary>
    /// <param name="nombre">Nombre del usuario. Se recorta y valida como invariante de dominio.</param>
    /// <exception cref="ArgumentException">
    /// Si <paramref name="nombre"/>, tras recortarlo, queda vacío o supera los 200 caracteres.
    /// </exception>
    Task<UsuarioDto> CrearAsync(string nombre);

    /// <summary>
    /// Edita el nombre de un usuario existente.
    /// </summary>
    /// <param name="id">Identificador del usuario a editar.</param>
    /// <param name="nombre">Nuevo nombre. Se recorta y valida como invariante de dominio.</param>
    /// <returns>
    /// El <see cref="UsuarioDto"/> actualizado, o <c>null</c> si no existe un usuario con ese
    /// <paramref name="id"/> (el <c>PageModel</c> traduce esto a <c>NotFound()</c>).
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Si <paramref name="nombre"/>, tras recortarlo, queda vacío o supera los 200 caracteres.
    /// </exception>
    Task<UsuarioDto?> EditarNombreAsync(int id, string nombre);

    /// <summary>
    /// Da de baja lógica a un usuario (<c>Activo = false</c>). Es idempotente: dar de baja a un
    /// usuario ya inactivo no es un error.
    /// </summary>
    /// <param name="id">Identificador del usuario a dar de baja.</param>
    /// <returns>
    /// <c>true</c> si el usuario existe (y quedó/estaba inactivo); <c>false</c> si no existe un
    /// usuario con ese <paramref name="id"/> (el <c>PageModel</c> traduce esto a <c>NotFound()</c>).
    /// </returns>
    Task<bool> DarDeBajaAsync(int id);
}
