# SAST FEAT-001: Setup de solución SsoAdmin + login básico + administración de Usuarios

| Field | Value |
|-------|-------|
| Ticket | FEAT-001 |
| Date | 2026-08-17T15:23:48Z |
| Scope | `src/` completo (los 4 bloques del spec) |

## Secretos

- ✅ F-SAST-01: sin API keys/tokens/contraseñas hardcodeadas. La única cadena de conexión en
  código (`SsoAdminDbContextFactory.cs:22`, solo tooling de diseño para `dotnet ef`) y en
  `appsettings.json` (`Data Source=ssoadmin.db`) es un path local de archivo SQLite — no contiene
  credenciales (SQLite no las requiere). Los `Data Source={dbPath}`/`{_dbPath}` en los tests son
  rutas temporales dinámicas (GUID), no secretos.
- ✅ `.env` y `*.db` están en `.gitignore` (líneas 40 y 43).

## Inyección

- ✅ F-SAST-02 (SQL/NoSQL): sin `FromSqlRaw`/`ExecuteSqlRaw`/`FromSqlInterpolated` en todo el
  árbol. Todo el acceso a datos pasa por LINQ/EF Core parametrizado.
- ✅ F-SAST-03 (comandos): sin `Process.Start`/`eval` en ningún archivo.
- ✅ F-SAST-05 (path traversal): no hay rutas de archivo construidas a partir de input de
  usuario en ningún bloque.

## XSS y funciones inseguras

- ✅ F-SAST-06 (XSS): sin `Html.Raw`/`innerHTML` en las vistas Razor (`Index.cshtml`,
  `Create.cshtml`, `Edit.cshtml`, `Login.cshtml`). Toda interpolación de datos de usuario
  (`@usuario.Nombre`, mensajes de error) pasa por el encoding automático de Razor.
- ✅ F-SAST-04 (deserialización insegura): sin `BinaryFormatter` ni deserialización de tipos
  arbitrarios.
- ✅ F-SAST-08 (criptografía débil): sin MD5/SHA1/DES/ECB. El hash de contraseñas usa
  `Microsoft.AspNetCore.Identity.PasswordHasher<T>` (PBKDF2 adaptativo), no una implementación
  propia.

## Resto de categorías obligatorias

- ✅ F-SAST-07 (SSRF): no hay llamadas salientes a URLs derivadas de input de usuario (no hay
  integraciones externas en este ticket).
- ✅ F-SAST-09 (modo debug en producción): `Program.cs:52-58` — `UseExceptionHandler("/Error")` +
  `UseHsts()` se activan solo cuando `!IsDevelopment()`; el modo detallado (`DetailedErrors`)
  vive únicamente en `appsettings.Development.json`, que no se despliega a producción.
- ✅ F-SAST-10 (logging de datos sensibles): revisado en las 3 rondas de revisión de cada bloque
  — ningún `ILogger` registra la contraseña en texto plano ni el hash. El único falso positivo de
  este scan (`LoginPageTests.cs:67`) es el nombre de un parámetro de test (`password` en la firma
  de un helper HTTP), no una llamada de logging real.
- ✅ F-SAST-11 (upload sin restricciones): no hay funcionalidad de carga de archivos en este
  ticket.
- ✅ F-SAST-12 (CSRF): antiforgery activo por defecto en todos los `<form method="post">` de
  `Usuarios/*` y `Login`/`Logout` (tag helpers de Razor Pages, sin deshabilitar). La única
  ocurrencia de `[IgnoreAntiforgeryToken]` en todo el árbol es preexistente en
  `Error.cshtml.cs:8` (scaffold default de .NET, página de error sin formulario) — no relacionada
  con ningún bloque de este ticket.
- ✅ F-SAST-14 (validación de input incompleta): `Nombre` (Usuario), `Username`/`Password`
  (Login) validados tanto en el `PageModel` (`[Required]`, `[StringLength]`) como en la capa de
  aplicación (`UsuarioService`, invariante de dominio con `Trim()`+maxlength).
- ✅ F-SAST-15 (errores que filtran internals): el mensaje de login inválido es genérico
  ("Usuario o contraseña incorrectos"), sin exponer cuál credencial falló ni detalles internos.

## Dependencias

- ✅ F-SAST-13/16: `dotnet list src/SsoAdmin.sln package --vulnerable --include-transitive` → sin
  paquetes vulnerables en ninguno de los 6 proyectos (Microsoft.EntityFrameworkCore.Sqlite,
  .Design, .InMemory, .Tools; Microsoft.AspNetCore.Identity; Microsoft.AspNetCore.Mvc.Testing;
  xunit — todas oficiales/mantenidas activamente).

## Suppressions

Ninguna — no hubo hallazgos Medium/Low que requirieran supresión documentada.

## Resumen

| Categoría | Cantidad |
|---|---|
| 🔴 Critical | 0 |
| 🟠 High | 0 |
| 🟡 Medium | 0 |
| 🟢 Low/Informational | 0 |

**Total: 0 vulnerabilidades. Result: PASSED.**
