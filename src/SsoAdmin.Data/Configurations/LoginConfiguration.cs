using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SsoAdmin.Models;

namespace SsoAdmin.Data.Configurations;

/// <summary>
/// Configuración de EF Core para la entidad <see cref="Login"/>.
/// </summary>
public class LoginConfiguration : IEntityTypeConfiguration<Login>
{
    /// <summary>
    /// Aplica los constraints de <see cref="Login"/>: <c>Username</c> requerido, máximo 100
    /// caracteres, con índice único; <c>PasswordHash</c> requerido, sin límite práctico de
    /// longitud (el hash de <c>PasswordHasher&lt;T&gt;</c> es Base64).
    /// </summary>
    /// <param name="builder">Constructor de la configuración de la entidad.</param>
    public void Configure(EntityTypeBuilder<Login> builder)
    {
        builder.HasKey(login => login.Id);

        builder.Property(login => login.Username)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(login => login.Username)
            .IsUnique();

        builder.Property(login => login.PasswordHash)
            .IsRequired();
    }
}
