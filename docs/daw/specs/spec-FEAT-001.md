# Spec FEAT-001: Setup de solución SsoAdmin + login básico + administración de Usuarios

| Field | Value |
|-------|-------|
| Ticket | FEAT-001 |
| PRD | docs/daw/prd/prd-FEAT-001.md |
| Tier | FEATURE |
| Date | 2026-08-17T13:45:44Z |
| Spec loops | 2 |

## Summary

Se crea la solución .NET 10 en `src/` con los 6 proyectos declarados en `AGENTS.md` (Vertical
Slice Architecture + DDD), configurados con EF Core sobre SQLite. `SsoAdmin.Web` (Razor Pages)
consulta `SsoAdmin.Application` **in-process** (inyección de dependencias, sin HTTP) — decisión
tomada en PLAN porque este ticket no expone ningún endpoint público; `SsoAdmin.API` queda
scaffoldeada pero vacía, reservada para el futuro endpoint del SSO (RF-08, fuera de alcance). La
autenticación de SI usa cookie auth de ASP.NET Core; las contraseñas se hashean con
`Microsoft.AspNetCore.Identity.PasswordHasher<T>` usado como servicio standalone (sin traer el
resto de ASP.NET Identity). El seed del usuario `admin`/`admin` se aplica en el arranque de
`SsoAdmin.Web`, después de `Database.Migrate()`, solo si la tabla `Login` está vacía.

## Coverage: PRD → blocks

| Requirement | Covered by |
|---|---|
| FR-01 | Block 1 |
| FR-02 | Block 2 |
| FR-03 | Block 2 |
| FR-04 | Block 2 |
| FR-05 | Block 3 |
| FR-06 | Block 3 |
| FR-07 | Block 4 |
| FR-08 | Block 4 |
| FR-09 | Block 4 |
| FR-10 | Block 4 |
| NFR-01 | Strategy: la contraseña nunca se persiste ni se loguea en texto plano — solo `PasswordHasher<T>.HashPassword` (Block 2, Block 3), verificado por AC-10 |
| NFR-02 | Strategy: TargetFramework `net10.0`, PascalCase/camelCase, DI por constructor, sin `.Result`/`.Wait()`, DTOs en el límite de cada capa — aplicado en todos los bloques |

## Dependencies between blocks

Secuencial: Block 1 → Block 2 → Block 3 → Block 4. Cada bloque depende del anterior (el
scaffolding antes que las entidades, las entidades antes que el login, el login antes de proteger
el CRUD de Usuarios).

## Block 1 — Scaffolding de la solución

**Files**
- `src/SsoAdmin.sln` (new)
- `src/SsoAdmin.Models/SsoAdmin.Models.csproj` (new) — class library, `net10.0`
- `src/SsoAdmin.Data/SsoAdmin.Data.csproj` (new) — class library, referencia `SsoAdmin.Models`,
  paquetes `Microsoft.EntityFrameworkCore.Sqlite` y `Microsoft.EntityFrameworkCore.Design`
- `src/SsoAdmin.Application/SsoAdmin.Application.csproj` (new) — class library, referencia
  `SsoAdmin.Data` y `SsoAdmin.Models`, paquete `Microsoft.AspNetCore.Identity` (solo por
  `PasswordHasher<T>`, sin el resto de Identity)
- `src/SsoAdmin.API/SsoAdmin.API.csproj` (new) — proyecto web mínimo (ASP.NET Core Web API),
  referencia `SsoAdmin.Application`
- `src/SsoAdmin.Web/SsoAdmin.Web.csproj` (new) — proyecto web Razor Pages, referencia
  `SsoAdmin.Application`, paquete `Microsoft.AspNetCore.Authentication.Cookies` (incluido en el
  SDK web, sin paquete adicional)
- `src/SsoAdmin.Test/SsoAdmin.Test.csproj` (new) — proyecto de test xUnit, referencia
  `SsoAdmin.Application`, `SsoAdmin.Data`, **y `SsoAdmin.API`** (única excepción a "ningún
  proyecto referencia `SsoAdmin.API`" — es imprescindible para instanciar
  `WebApplicationFactory<Program>` contra el `Program` de `SsoAdmin.API` en el smoke test de
  este mismo bloque; ningún proyecto de producción la referencia), `Microsoft.EntityFrameworkCore.InMemory`
  y `Microsoft.AspNetCore.Mvc.Testing` (para tests sin SQLite físico y para el smoke test de
  arranque)
- `.gitignore` (modified) — agregar `src/**/bin/`, `src/**/obj/`, `*.db` si no están cubiertos ya

**Logic**

Crear la solución con `dotnet new sln`, cada proyecto con `dotnet new classlib`/`webapp`/`webapi`/
`xunit` según corresponda, y cablear las referencias de proyecto (`dotnet add reference`) según el
grafo de dependencias: `Models` ← `Data` ← `Application` ← (`API`, `Web`); `Test` referencia
`Application` y `Data`. Ningún proyecto referencia `SsoAdmin.API` — `SsoAdmin.Web` no pasa por la
API (decisión de PLAN).

`SsoAdmin.API/Program.cs` queda mínimo: host ASP.NET Core levantado, sin controladores ni
endpoints propios — solo demuestra que el proyecto compila y arranca, listo para que un ticket
futuro agregue `POST /api/sso/verificar`.

**Input validation**

N/A (bloque de infraestructura, sin lógica de negocio).

**Error handling**

N/A.

**Required tests**

- [ ] `dotnet build` sobre `src/SsoAdmin.sln` compila sin errores ni warnings — valida FR-01, NFR-02.
- [ ] `dotnet run --project src/SsoAdmin.API` arranca sin excepciones (smoke test manual o vía
  `WebApplicationFactory` en `SsoAdmin.Test`).

**Completion criterion**

`dotnet build` sobre la solución completa termina en 0 errores, con los 6 proyectos presentes y
las referencias de proyecto correctas (`dotnet list <proj> reference`).

## Block 2 — Modelo de datos: Usuario, Login y seed del admin

**Files**
- `src/SsoAdmin.Models/Usuario.cs` (new) — entidad de dominio
- `src/SsoAdmin.Models/Login.cs` (new) — entidad de dominio
- `src/SsoAdmin.Data/SsoAdminDbContext.cs` (new) — `DbContext` con `DbSet<Usuario>`,
  `DbSet<Login>`
- `src/SsoAdmin.Data/Configurations/UsuarioConfiguration.cs` (new) — `IEntityTypeConfiguration<Usuario>`
- `src/SsoAdmin.Data/Configurations/LoginConfiguration.cs` (new) — `IEntityTypeConfiguration<Login>`
- `src/SsoAdmin.Data/Migrations/*` (new) — migración inicial generada con
  `dotnet ef migrations add InitialCreate`
- `src/SsoAdmin.Data/Seed/AdminLoginSeeder.cs` (new) — clase con
  `Task SeedAsync(SsoAdminDbContext db, IPasswordHasherService hasher)`, precarga `admin`/`admin`
  solo si `Login` está vacía
- `src/SsoAdmin.Web/Program.cs` (modified) — registra `SsoAdminDbContext` (SQLite,
  connection string desde `appsettings.json`), llama `Database.Migrate()` y el seeder al arranque
- `src/SsoAdmin.Web/appsettings.json` (new) — connection string `Data Source=ssoadmin.db`

**Data model**

- **`Usuario`**: `Id` (`int`, PK, identity) · `Nombre` (`string`, requerido, `maxlength 200`) ·
  `Activo` (`bool`, default `true`).
- **`Login`**: `Id` (`int`, PK, identity) · `Username` (`string`, requerido, `maxlength 100`,
  índice único) · `PasswordHash` (`string`, requerido, sin límite de longitud práctico — el hash
  de `PasswordHasher<T>` es Base64). **No existe columna de contraseña en texto plano ni de
  ningún otro derivado reversible** (AC-10). `Login` no tiene FK hacia `Usuario`: son conceptos
  distintos — `Login` son las credenciales de SI para entrar a esta app, `Usuario` son las
  personas que SI administra para el SSO.

**Input validation**

- `Usuario.Nombre`: no vacío, `maxlength 200` — aplicado también a nivel de `Application` (Block
  4), no solo en la entidad.

**Error handling**

- Si `Database.Migrate()` falla al arrancar (ej. archivo SQLite bloqueado), la excepción propaga y
  el host no levanta — comportamiento intencional: no tiene sentido servir tráfico sin esquema.

**Required tests**

- [ ] Test de integración (EF Core InMemory o SQLite en archivo temporal): tras `Migrate()` +
  seed sobre una base vacía, `Login` contiene exactamente un registro `Username="admin"` cuyo
  `PasswordHash` verifica correctamente contra `"admin"` con `PasswordHasher<T>.VerifyHashedPassword`
  — valida AC-01, AC-02.
- [ ] Test: correr el seeder dos veces no duplica el registro `admin` (idempotencia) — valida AC-02.
- [ ] Test: inspeccionar el esquema/columnas de `Login` y confirmar que ninguna columna se llama
  o contiene literalmente la contraseña en texto plano — valida AC-10.
- [ ] Test: apuntar `SsoAdminDbContext` a una ruta de archivo SQLite inválida (ej. un directorio
  inexistente) y verificar que `Database.Migrate()` lanza una excepción — valida el manejo de
  error documentado en este bloque.

**Completion criterion**

Con una base SQLite vacía, al arrancar `SsoAdmin.Web` el esquema queda creado y la tabla `Login`
tiene el usuario `admin` con password hasheada, verificable con el hasher.

**Rollback**

Es la migración inicial (`InitialCreate`): no hay un esquema previo al que volver. Revertirla es
`dotnet ef database update 0` (o, en el MVP con SQLite de archivo único, borrar el archivo
`.db` y volver a migrar). No aplica versionado de datos porque no hay datos previos que preservar.

## Block 3 — Login básico y autorización

**Files**
- `src/SsoAdmin.Application/Auth/IPasswordHasherService.cs` (new) — interfaz: `string Hash(string
  password)`, `bool Verify(string hash, string password)`
- `src/SsoAdmin.Application/Auth/PasswordHasherService.cs` (new) — implementación con
  `PasswordHasher<Login>` (el `TUser` genérico no se usa realmente, es un adaptador)
- `src/SsoAdmin.Application/Auth/IAuthenticationService.cs` (new) — `Task<bool>
  ValidateCredentialsAsync(string username, string password)`
- `src/SsoAdmin.Application/Auth/AuthenticationService.cs` (new) — busca en `Login` por
  `Username`, verifica hash con `IPasswordHasherService`
- `src/SsoAdmin.Web/Program.cs` (modified) — registra
  `AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(...)`,
  `AddAuthorization()`, middlewares `UseAuthentication()`/`UseAuthorization()`,
  `UseHttpsRedirection()`, DI de `IAuthenticationService`/`IPasswordHasherService`. Opciones de
  cookie endurecidas (mitigación de threat modeling — ver `docs/daw/security/threat-FEAT-001.md`
  R2): `HttpOnly = true`, `SecurePolicy = CookieSecurePolicy.Always`, `SameSite =
  SameSiteMode.Strict`, `ExpireTimeSpan = TimeSpan.FromHours(8)`, `SlidingExpiration = true`.
- `src/SsoAdmin.Web/Pages/Login.cshtml` + `Login.cshtml.cs` (new) — formulario usuario/contraseña,
  `OnPostAsync` llama `IAuthenticationService`, en éxito emite el cookie de sesión
  (`HttpContext.SignInAsync`) con un `Claim` de nombre de usuario, en fallo re-renderiza con error
  `401`. Loguea cada intento (éxito o fallo) vía `ILogger<LoginModel>` — en fallo nunca loguea la
  contraseña recibida, solo el `username` intentado (mitigación R7, no repudio)
- `src/SsoAdmin.Web/Pages/Logout.cshtml.cs` (new) — `OnPost` hace `SignOutAsync`
- `src/SsoAdmin.Web/Pages/Shared/_Layout.cshtml` (new) — layout base con link de logout cuando hay
  sesión

**Input validation**

- `Username`/`Password` (formulario de login): ambos requeridos (`[Required]` en el `PageModel`),
  `Username` con `maxlength 100` (igual que la columna `Login.Username`). No se aplican reglas de
  complejidad sobre `Password` — fuera de alcance del PRD, que fija un único usuario `admin`
  precargado para el MVP. Ambos campos se recortan (`Trim()`) antes de validar.

**Error handling**

- Credenciales inválidas: `AuthenticationService` devuelve `false`; la página de login no crea
  sesión y muestra un mensaje de error genérico ("usuario o contraseña incorrectos") — no revela
  cuál de los dos campos falló, para no filtrar si el username existe.
- Acceso no autenticado a `/Usuarios/*` (Block 4): el middleware de autorización redirige a
  `/Login` (comportamiento default de cookie auth con `[Authorize]`/`RequireAuthorization()` en
  esas páginas).

**Required tests**

- [ ] Test: `AuthenticationService.ValidateCredentialsAsync("admin", "admin")` devuelve `true`
  contra una base con el seed aplicado — valida AC-03.
- [ ] Test: `ValidateCredentialsAsync("admin", "wrong")` devuelve `false` — valida AC-04.
- [ ] Test de integración web (`WebApplicationFactory`): `POST /Login` con credenciales válidas
  responde con el cookie de auth seteado y redirige a `/Usuarios` — valida AC-03.
- [ ] Test de integración web: `GET /Usuarios` sin cookie de sesión redirige a `/Login` — valida
  AC-05.
- [ ] Test de integración web: tras un `POST /Login` exitoso, el `Set-Cookie` de respuesta tiene
  los atributos `HttpOnly` y `SameSite=Strict` — valida la mitigación R2 del threat model.

**Completion criterion**

Un usuario sin sesión que pide `/Usuarios` termina en `/Login`; con `admin`/`admin` accede; con
credenciales inválidas no accede y ve el error.

## Block 4 — Administración de Usuarios (CRUD web)

**Files**
- `src/SsoAdmin.Application/Usuarios/UsuarioDto.cs` (new) — `Id`, `Nombre`, `Activo`
- `src/SsoAdmin.Application/Usuarios/IUsuarioService.cs` (new) — `ListarAsync()`,
  `CrearAsync(string nombre)`, `EditarNombreAsync(int id, string nombre)`,
  `DarDeBajaAsync(int id)`
- `src/SsoAdmin.Application/Usuarios/UsuarioService.cs` (new) — implementación contra
  `SsoAdminDbContext`, mapea `Usuario` ↔ `UsuarioDto` (la entidad de dominio no cruza a `Web`)
- `src/SsoAdmin.Web/Pages/Usuarios/Index.cshtml` + `.cshtml.cs` (new) — lista usuarios con estado
  activo/inactivo, requiere autenticación
- `src/SsoAdmin.Web/Pages/Usuarios/Create.cshtml` + `.cshtml.cs` (new) — alta de usuario (nombre)
- `src/SsoAdmin.Web/Pages/Usuarios/Edit.cshtml` + `.cshtml.cs` (new) — edición de nombre
- `src/SsoAdmin.Web/Pages/Usuarios/Index.cshtml.cs` (modified, mismo archivo que arriba) — handler
  `OnPostDarDeBajaAsync(int id)` para la baja lógica. Los tres handlers (`Create`, `Edit`,
  `DarDeBaja`) loguean vía `ILogger<T>` qué usuario de SI (`User.Identity.Name`) hizo qué acción
  sobre qué `Id` de Usuario (mitigación R7 del threat model, no repudio — sin esto no queda
  registro de quién dio de baja a alguien)
- `src/SsoAdmin.Web/Pages/_ViewImports.cshtml` (new) — habilita tag helpers, y directiva de
  autorización si aplica a nivel de página

**Input validation**

- `Nombre`: requerido, `maxlength 200`, se recorta (`Trim()`) antes de guardar. Validado tanto en
  `UsuarioService` (invariante de dominio) como en el `PageModel` (`[Required, StringLength(200)]`
  + `ModelState.IsValid`) para dar feedback inmediato en el formulario.

**Error handling**

- `EditarNombreAsync`/`DarDeBajaAsync` sobre un `Id` inexistente: la página devuelve `404` (Razor
  Pages `NotFound()`); no es un caso contemplado por ningún AC porque la UI nunca ofrece ese `Id`,
  pero el service no debe asumir que siempre existe.
- Dar de baja a un usuario ya inactivo: operación idempotente (`Activo` ya es `false`, no hay
  error) — no hay AC que lo pida como error, así que no se trata como uno.

**Required tests**

- [ ] `UsuarioService.CrearAsync("Juan")` persiste un `Usuario` con `Activo=true` — valida AC-07.
- [ ] `UsuarioService.EditarNombreAsync(id, "Nuevo Nombre")` actualiza el nombre — valida AC-08.
- [ ] `UsuarioService.DarDeBajaAsync(id)` deja `Activo=false` — valida AC-09.
- [ ] `UsuarioService.ListarAsync()` devuelve el estado activo/inactivo correcto tras las
  operaciones anteriores — valida AC-07/AC-08/AC-09.
- [ ] Test de integración web autenticado: `GET /Usuarios` devuelve `200` y el listado — valida
  AC-06.
- [ ] Test: `UsuarioService.EditarNombreAsync`/`DarDeBajaAsync` con un `Id` inexistente no lanza
  una excepción no controlada — devuelven un resultado que el `PageModel` traduce a `NotFound()`
  — valida el manejo de error documentado en este bloque.

**Completion criterion**

Con sesión iniciada, SI puede listar, crear, editar el nombre y dar de baja lógica a un usuario
desde `/Usuarios`, y los cambios se reflejan en el próximo `GET /Usuarios`.

## Final verification

- `dotnet build` sobre `src/SsoAdmin.sln`: 0 errores.
- `dotnet test` sobre `src/SsoAdmin.Test`: todos los tests en verde, cubriendo AC-01 a AC-10.
- Arranque manual de `SsoAdmin.Web` contra una base SQLite vacía: el seed crea `admin`/`admin`,
  el login funciona, `/Usuarios` está protegida y el CRUD completo (listar/crear/editar/dar de
  baja) funciona de punta a punta.
- Ninguna columna ni log contiene una contraseña en texto plano (revisión manual de
  `LoginConfiguration` y de cualquier `ILogger` que toque `Login`).
