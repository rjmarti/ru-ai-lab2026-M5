using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SsoAdmin.Application.Usuarios;

namespace SsoAdmin.Web.Pages.Usuarios;

/// <summary>
/// Alta de un nuevo usuario administrado por el SSO. Requiere sesión de SI.
/// </summary>
/// <param name="usuarioService">Casos de uso de administración de usuarios, inyectado por DI.</param>
/// <param name="logger">Logger de auditoría (mitigación R7 del threat model, no repudio).</param>
[Authorize]
public class CreateModel(IUsuarioService usuarioService, ILogger<CreateModel> logger) : PageModel
{
    /// <summary>
    /// Nombre del nuevo usuario.
    /// </summary>
    [BindProperty]
    [Required]
    [StringLength(200)]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Renderiza el formulario de alta.
    /// </summary>
    public void OnGet()
    {
    }

    /// <summary>
    /// Crea el usuario y redirige al listado. Loguea qué usuario de SI dio de alta a qué
    /// <c>Id</c> de <c>Usuario</c> resultante (mitigación R7 del threat model).
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        UsuarioDto usuario = await usuarioService.CrearAsync(Nombre);

        logger.LogInformation(
            "Usuario de SI {Username} dio de alta al Usuario {UsuarioId} ({Nombre}).",
            User.Identity?.Name,
            usuario.Id,
            usuario.Nombre);

        return RedirectToPage("./Index");
    }
}
