namespace SsoAdmin.Application.Auth;

/// <summary>
/// Servicio de hasheo de contraseñas. Interfaz mínima requerida por Block 2 (seed del usuario
/// admin); el método <c>Verify</c> usado por el login se agrega en Block 3.
/// </summary>
public interface IPasswordHasherService
{
    /// <summary>
    /// Genera el hash de una contraseña en texto plano. El resultado nunca permite recuperar la
    /// contraseña original.
    /// </summary>
    /// <param name="password">Contraseña en texto plano a hashear.</param>
    /// <returns>El hash de la contraseña.</returns>
    string Hash(string password);
}
