namespace SsoAdmin.Application.Auth;

/// <summary>
/// Valida las credenciales de un usuario de SI contra la tabla <c>Login</c>.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Valida si el par usuario/contraseña corresponde a un <c>Login</c> existente.
    /// </summary>
    /// <param name="username">Nombre de usuario ingresado.</param>
    /// <param name="password">Contraseña en texto plano ingresada.</param>
    /// <returns>
    /// <c>true</c> si existe un <c>Login</c> con ese <paramref name="username"/> y su hash
    /// verifica contra <paramref name="password"/>; <c>false</c> en cualquier otro caso (usuario
    /// inexistente o contraseña incorrecta), sin distinguir cuál de los dos falló.
    /// </returns>
    Task<bool> ValidateCredentialsAsync(string username, string password);
}
