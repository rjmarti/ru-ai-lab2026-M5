using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ISsoAuthenticationService = SsoAdmin.Application.Auth.IAuthenticationService;

namespace SsoAdmin.Web.Pages;

/// <summary>
/// Página de login de SI. Valida credenciales contra
/// <see cref="SsoAdmin.Application.Auth.IAuthenticationService"/> (alias
/// <see cref="ISsoAuthenticationService"/> por conflicto de nombre con
/// <see cref="Microsoft.AspNetCore.Authentication.IAuthenticationService"/> del framework) y, en
/// éxito, emite el cookie de sesión (<see cref="HttpContext.SignInAsync(string, ClaimsPrincipal)"/>).
/// </summary>
/// <param name="authenticationService">Servicio de validación de credenciales, inyectado por DI.</param>
/// <param name="logger">Logger de auditoría (mitigación R7 del threat model, no repudio).</param>
public class LoginModel(ISsoAuthenticationService authenticationService, ILogger<LoginModel> logger) : PageModel
{
    /// <summary>
    /// Nombre de usuario ingresado en el formulario.
    /// </summary>
    [BindProperty]
    [Required]
    [StringLength(100)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Contraseña ingresada en el formulario.
    /// </summary>
    [BindProperty]
    [Required]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Renderiza el formulario de login.
    /// </summary>
    public void OnGet()
    {
    }

    /// <summary>
    /// Valida las credenciales ingresadas. En éxito, crea la sesión (cookie de auth) y redirige a
    /// <c>/Usuarios</c> (página de Block 4). En fallo, no crea sesión, re-renderiza el formulario
    /// con un mensaje de error genérico (no distingue usuario inexistente de contraseña
    /// incorrecta, mitigación R6) y responde con <c>401</c>.
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        string username = Username.Trim();
        string password = Password.Trim();

        bool credencialesValidas = await authenticationService.ValidateCredentialsAsync(username, password);

        if (!credencialesValidas)
        {
            // Nunca se loguea la contraseña, sólo el username intentado (mitigación R7).
            logger.LogWarning("Intento de login fallido para el usuario {Username}.", username);

            ModelState.AddModelError(string.Empty, "Usuario o contraseña incorrectos.");
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Page();
        }

        logger.LogInformation("Login exitoso para el usuario {Username}.", username);

        List<Claim> claims = [new Claim(ClaimTypes.Name, username)];
        ClaimsIdentity identity = new(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        ClaimsPrincipal principal = new(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        // Redirect() en vez de RedirectToPage("/Usuarios/Index"): la página de Block 4 todavía no
        // existe, y RedirectToPageResult valida la existencia de la página al ejecutar el
        // resultado (LinkGenerator), lo que lanzaría una excepción DESPUÉS de SignInAsync y
        // limpiaría la respuesta (incluido el Set-Cookie recién emitido) antes de llegar al
        // cliente. Redirect() con la ruta literal no valida existencia y funciona igual una vez
        // que Block 4 cree "/Usuarios" (convención de ruta para Pages/Usuarios/Index.cshtml).
        return Redirect("/Usuarios");
    }
}
