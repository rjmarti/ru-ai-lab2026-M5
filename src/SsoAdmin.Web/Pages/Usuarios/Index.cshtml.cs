using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SsoAdmin.Application.Usuarios;

namespace SsoAdmin.Web.Pages.Usuarios;

/// <summary>
/// Lista los usuarios administrados por el SSO, con su estado activo/inactivo, y expone el
/// handler de baja lógica. Requiere sesión de SI (<see cref="AuthorizeAttribute"/>).
/// </summary>
/// <param name="usuarioService">Casos de uso de administración de usuarios, inyectado por DI.</param>
/// <param name="logger">Logger de auditoría (mitigación R7 del threat model, no repudio).</param>
[Authorize]
public class IndexModel(IUsuarioService usuarioService, ILogger<IndexModel> logger) : PageModel
{
    /// <summary>
    /// Usuarios a mostrar en el listado.
    /// </summary>
    public List<UsuarioDto> Usuarios { get; private set; } = [];

    /// <summary>
    /// Carga el listado de usuarios.
    /// </summary>
    public async Task OnGetAsync()
    {
        Usuarios = await usuarioService.ListarAsync();
    }

    /// <summary>
    /// Da de baja lógica al usuario indicado. Loguea qué usuario de SI dio de baja a qué
    /// <c>Id</c> de <c>Usuario</c> (mitigación R7 del threat model). Si el <c>Id</c> no existe,
    /// devuelve <c>404</c>.
    /// </summary>
    /// <param name="id">Identificador del usuario a dar de baja.</param>
    public async Task<IActionResult> OnPostDarDeBajaAsync(int id)
    {
        bool exito = await usuarioService.DarDeBajaAsync(id);

        if (!exito)
        {
            return NotFound();
        }

        logger.LogInformation(
            "Usuario de SI {Username} dio de baja al Usuario {UsuarioId}.",
            User.Identity?.Name,
            id);

        return RedirectToPage();
    }
}
