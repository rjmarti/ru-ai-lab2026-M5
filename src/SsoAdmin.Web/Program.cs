using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using SsoAdmin.Application.Auth;
using SsoAdmin.Application.Seed;
using SsoAdmin.Application.Usuarios;
using SsoAdmin.Data;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// La connection string se resuelve de forma perezosa dentro del lambda, contra el
// IConfiguration real del IServiceProvider del host, en vez de fijarla una única vez al ejecutar
// este archivo. Evaluarla eager (leyendo builder.Configuration antes de builder.Build()) rompe el
// aislamiento de WebApplicationFactory en tests de integración: el override de configuración vía
// ConfigureAppConfiguration/ConfigureWebHost sólo queda aplicado en el IConfiguration del host
// construido, y ese host no existe todavía en este punto del archivo. Resolverla dentro del
// lambda difiere la lectura hasta que el DbContext se construye por scope/request, momento en el
// que cualquier override ya está en efecto.
builder.Services.AddDbContext<SsoAdminDbContext>((serviceProvider, options) =>
{
    IConfiguration configuration = serviceProvider.GetRequiredService<IConfiguration>();
    string? connectionString = configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "La connection string 'DefaultConnection' no está configurada en appsettings.json.");
    }

    options.UseSqlite(connectionString);
});
builder.Services.AddScoped<IPasswordHasherService, PasswordHasherService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();

// Cookie auth endurecida (mitigación R2 del threat model, docs/daw/security/threat-FEAT-001.md):
// HttpOnly + Secure + SameSite=Strict, expiración de 8h con sliding expiration. LoginPath queda
// listo para que Block 4 proteja /Usuarios/* con [Authorize]/RequireAuthorization().
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

// Aplica migraciones pendientes y siembra el login admin/admin si la tabla Login está vacía. Si
// Database.MigrateAsync() falla (ej. archivo SQLite bloqueado), la excepción propaga y el host no
// levanta: comportamiento intencional, no tiene sentido servir tráfico sin esquema.
await using (AsyncServiceScope scope = app.Services.CreateAsyncScope())
{
    SsoAdminDbContext db = scope.ServiceProvider.GetRequiredService<SsoAdminDbContext>();
    IPasswordHasherService hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasherService>();

    await db.Database.MigrateAsync();
    await AdminLoginSeeder.SeedAsync(db, hasher);
}

app.Run();
