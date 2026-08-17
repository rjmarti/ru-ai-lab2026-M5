using Microsoft.EntityFrameworkCore;
using SsoAdmin.Data.Configurations;
using SsoAdmin.Models;

namespace SsoAdmin.Data;

/// <summary>
/// Contexto de EF Core de la aplicación SsoAdmin, con acceso a las entidades <see cref="Usuario"/>
/// y <see cref="Login"/>.
/// </summary>
/// <param name="options">Opciones de configuración del contexto, inyectadas por DI.</param>
public class SsoAdminDbContext(DbContextOptions<SsoAdminDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Personas administradas por el SSO.
    /// </summary>
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    /// <summary>
    /// Credenciales de SI para ingresar a esta aplicación.
    /// </summary>
    public DbSet<Login> Logins => Set<Login>();

    /// <summary>
    /// Aplica las configuraciones de mapeo de <see cref="UsuarioConfiguration"/> y
    /// <see cref="LoginConfiguration"/>.
    /// </summary>
    /// <param name="modelBuilder">Constructor del modelo de EF Core.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new UsuarioConfiguration());
        modelBuilder.ApplyConfiguration(new LoginConfiguration());
    }
}
