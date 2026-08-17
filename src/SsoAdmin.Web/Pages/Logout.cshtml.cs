using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SsoAdmin.Web.Pages;

/// <summary>
/// Handler de logout: cierra la sesión de SI (cookie de auth) y redirige a <c>/Login</c>. Es un
/// handler puro invocado sólo por <c>POST</c> desde el link de logout de <c>_Layout.cshtml</c>;
/// no expone contenido propio para <c>GET</c>.
/// </summary>
public class LogoutModel : PageModel
{
    /// <summary>
    /// Cierra la sesión actual y redirige a la página de login.
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Login");
    }
}
