using Microsoft.EntityFrameworkCore;
using SsoAdmin.Application.Auth;
using SsoAdmin.Application.Seed;
using SsoAdmin.Data;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "La connection string 'DefaultConnection' no está configurada en appsettings.json.");
}

builder.Services.AddDbContext<SsoAdminDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddScoped<IPasswordHasherService, PasswordHasherService>();

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
