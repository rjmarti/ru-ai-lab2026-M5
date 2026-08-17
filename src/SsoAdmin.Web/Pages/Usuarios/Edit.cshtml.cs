using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SsoAdmin.Application.Usuarios;

namespace SsoAdmin.Web.Pages.Usuarios;

/// <summary>
/// Edición del nombre de un usuario existente. Requiere sesión de SI.
/// </summary>
/// <param name="usuarioService">Casos de uso de administración de usuarios, inyectado por DI.</param>
/// <param name="logger">Logger de auditoría (mitigación R7 del threat model, no repudio).</param>
[Authorize]
public class EditModel(IUsuarioService usuarioService, ILogger<EditModel> logger) : PageModel
{
    /// <summary>
    /// Identificador del usuario a editar.
    /// </summary>
    [BindProperty]
    public int Id { get; set; }

    /// <summary>
    /// Nuevo nombre del usuario.
    /// </summary>
    [BindProperty]
    [Required]
    [StringLength(200)]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Carga el usuario a editar. Si el <c>id</c> no existe, devuelve <c>404</c>.
    /// </summary>
    /// <param name="id">Identificador del usuario a editar.</param>
    public async Task<IActionResult> OnGetAsync(int id)
    {
        UsuarioDto? usuario = await usuarioService.ObtenerPorIdAsync(id);

        if (usuario is null)
        {
            return NotFound();
        }

        Id = usuario.Id;
        Nombre = usuario.Nombre;

        return Page();
    }

    /// <summary>
    /// Guarda el nuevo nombre y redirige al listado. Loguea qué usuario de SI editó qué
    /// <c>Id</c> de <c>Usuario</c> y el cambio de nombre (mitigación R7 del threat model). El
    /// "nombre anterior" que va al log se obtiene con una lectura server-side ANTES de aplicar el
    /// cambio (no de un campo del formulario): el cliente no puede manipular qué queda escrito en
    /// el log de auditoría. Si el <c>Id</c> no existe, devuelve <c>404</c>.
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        UsuarioDto? usuarioAntesDeEditar = await usuarioService.ObtenerPorIdAsync(Id);

        if (usuarioAntesDeEditar is null)
        {
            return NotFound();
        }

        UsuarioDto? usuario = await usuarioService.EditarNombreAsync(Id, Nombre);

        if (usuario is null)
        {
            return NotFound();
        }

        logger.LogInformation(
            "Usuario de SI {Username} editó el Usuario {UsuarioId}: {NombreAnterior} -> {NombreNuevo}.",
            User.Identity?.Name,
            Id,
            usuarioAntesDeEditar.Nombre,
            usuario.Nombre);

        return RedirectToPage("./Index");
    }
}
