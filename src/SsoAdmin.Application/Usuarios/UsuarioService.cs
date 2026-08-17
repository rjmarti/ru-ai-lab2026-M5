using Microsoft.EntityFrameworkCore;
using SsoAdmin.Data;
using SsoAdmin.Models;

namespace SsoAdmin.Application.Usuarios;

/// <summary>
/// Implementación de <see cref="IUsuarioService"/> contra <see cref="SsoAdminDbContext"/>. Mapea
/// <see cref="Usuario"/> ↔ <see cref="UsuarioDto"/>: la entidad de dominio nunca cruza a
/// <c>SsoAdmin.Web</c>.
/// </summary>
/// <param name="db">Contexto de base de datos, inyectado por DI.</param>
public class UsuarioService(SsoAdminDbContext db) : IUsuarioService
{
    private const int NombreMaxLength = 200;

    /// <inheritdoc />
    public async Task<List<UsuarioDto>> ListarAsync()
    {
        // Se proyecta con un object initializer inline (no llamando a MapearADto) porque EF Core
        // no puede traducir una invocación de método arbitraria dentro de Select() a SQL.
        return await db.Usuarios
            .OrderBy(usuario => usuario.Nombre)
            .Select(usuario => new UsuarioDto
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre,
                Activo = usuario.Activo,
            })
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<UsuarioDto?> ObtenerPorIdAsync(int id)
    {
        Usuario? usuario = await db.Usuarios.FindAsync(id);

        return usuario is null ? null : MapearADto(usuario);
    }

    /// <inheritdoc />
    public async Task<UsuarioDto> CrearAsync(string nombre)
    {
        string nombreValidado = ValidarNombre(nombre);

        Usuario usuario = new() { Nombre = nombreValidado, Activo = true };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();

        return MapearADto(usuario);
    }

    /// <inheritdoc />
    public async Task<UsuarioDto?> EditarNombreAsync(int id, string nombre)
    {
        // Se valida el nombre ANTES de tocar la base, per el bloque: un nombre inválido no debe
        // ni siquiera disparar la consulta de búsqueda.
        string nombreValidado = ValidarNombre(nombre);

        Usuario? usuario = await db.Usuarios.FindAsync(id);
        if (usuario is null)
        {
            return null;
        }

        usuario.Nombre = nombreValidado;
        await db.SaveChangesAsync();

        return MapearADto(usuario);
    }

    /// <inheritdoc />
    public async Task<bool> DarDeBajaAsync(int id)
    {
        Usuario? usuario = await db.Usuarios.FindAsync(id);
        if (usuario is null)
        {
            return false;
        }

        usuario.Activo = false;
        await db.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Recorta el nombre y valida el invariante de dominio (requerido, máximo 200 caracteres) —
    /// la misma regla que <c>UsuarioConfiguration</c> aplica a nivel de columna (Block 2), pero
    /// verificada acá también para no depender únicamente de la excepción de EF Core al guardar.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Si, tras el <see cref="string.Trim()"/>, el nombre queda vacío o supera los 200 caracteres.
    /// </exception>
    private static string ValidarNombre(string nombre)
    {
        string nombreRecortado = nombre.Trim();

        if (nombreRecortado.Length == 0 || nombreRecortado.Length > NombreMaxLength)
        {
            throw new ArgumentException(
                $"El nombre es requerido y debe tener como máximo {NombreMaxLength} caracteres.",
                nameof(nombre));
        }

        return nombreRecortado;
    }

    private static UsuarioDto MapearADto(Usuario usuario)
    {
        return new UsuarioDto
        {
            Id = usuario.Id,
            Nombre = usuario.Nombre,
            Activo = usuario.Activo,
        };
    }
}
