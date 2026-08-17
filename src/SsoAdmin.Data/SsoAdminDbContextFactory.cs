using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SsoAdmin.Data;

/// <summary>
/// Fábrica de <see cref="SsoAdminDbContext"/> usada exclusivamente en tiempo de diseño por las
/// herramientas de EF Core (<c>dotnet ef migrations add</c>, <c>dotnet ef database update</c>).
/// No participa del arranque de la aplicación: en runtime el contexto se registra en
/// <c>SsoAdmin.Web/Program.cs</c> con la connection string real de <c>appsettings.json</c>.
/// </summary>
public class SsoAdminDbContextFactory : IDesignTimeDbContextFactory<SsoAdminDbContext>
{
    /// <summary>
    /// Crea una instancia de <see cref="SsoAdminDbContext"/> apuntando a una base SQLite local,
    /// solo para que las herramientas de diseño de EF Core puedan generar migraciones.
    /// </summary>
    /// <param name="args">Argumentos de línea de comandos pasados por la herramienta de EF Core.</param>
    public SsoAdminDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<SsoAdminDbContext> optionsBuilder = new();
        optionsBuilder.UseSqlite("Data Source=ssoadmin.db");

        return new SsoAdminDbContext(optionsBuilder.Options);
    }
}
