using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SsoAdmin.Models;

namespace SsoAdmin.Data.Configurations;

/// <summary>
/// Configuración de EF Core para la entidad <see cref="Usuario"/>.
/// </summary>
public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    /// <summary>
    /// Aplica los constraints de <see cref="Usuario"/>: <c>Nombre</c> requerido con máximo 200
    /// caracteres, <c>Activo</c> con valor por defecto <c>true</c>.
    /// </summary>
    /// <param name="builder">Constructor de la configuración de la entidad.</param>
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.HasKey(usuario => usuario.Id);

        builder.Property(usuario => usuario.Nombre)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(usuario => usuario.Activo)
            .IsRequired()
            .HasDefaultValue(true);
    }
}
