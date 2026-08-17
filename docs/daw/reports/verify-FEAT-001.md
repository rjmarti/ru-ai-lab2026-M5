# Verify FEAT-001: Setup de solución SsoAdmin + login básico + administración de Usuarios

| Field | Value |
|-------|-------|
| Ticket | FEAT-001 |
| PRD | docs/daw/prd/prd-FEAT-001.md |
| Spec | docs/daw/specs/spec-FEAT-001.md |

## Ronda 1 — 2026-08-17T18:58:53Z — BLOCKED

`dotnet build src/SsoAdmin.sln` → 0 errores, 0 warnings. `dotnet test src/SsoAdmin.Test` → 25/25
en verde. Cobertura (`--collect:"XPlat Code Coverage"`, `coverage.cobertura.xml`): **79.09%
líneas / 64.28% branches globales**. Por paquete: `SsoAdmin.API` 100%/100%,
`SsoAdmin.Application` 95.74%/87.5%, `SsoAdmin.Data` 93.16%/100%, `SsoAdmin.Models` 83.33%/100%,
**`SsoAdmin.Web` 56.57%/59.75%**.

### Matriz AC → código → test (F-VER-01)

| AC | Test(s) | Veredicto |
|---|---|---|
| AC-01 | `AdminLoginSeederTests.Seed_SobreBaseVacia_CreaAdminConHashVerificable` | ✅ |
| AC-02 | `Seed_SobreBaseVacia_...` + `Seed_EjecutadoDosVeces_NoDuplicaElAdmin` | ✅ |
| AC-03 | `AuthenticationServiceTests...DevuelveTrue` + `LoginPageTests.PostLogin_ConCredencialesValidas_...` | ✅ |
| AC-04 | `AuthenticationServiceTests...DevuelveFalse` + `LoginPageTests.PostLogin_ConCredencialesInvalidas_...` | ✅ |
| AC-05 | `UsuariosPageTests.GetUsuarios_SinCookieDeSesion_RedirigeALogin` + `CookieAuthConfigurationTests` | ✅ |
| AC-06 | `UsuariosPageTests.GetUsuarios_Autenticado_Devuelve200YElListado` (contenido real, no superficial) | ✅ |
| AC-07 | `UsuarioServiceTests.CrearAsync_PersisteUsuarioActivo` (solo nivel servicio) | ⚠️ falta nivel web |
| AC-08 | `UsuarioServiceTests.EditarNombreAsync_ActualizaElNombre` (solo nivel servicio) | ⚠️ falta nivel web |
| AC-09 | `UsuarioServiceTests.DarDeBajaAsync_...` (servicio) + solo el 404 a nivel web | ⚠️ falta happy path web |
| AC-10 | `AdminLoginSeederTests.EsquemaDeLogin_NoTieneColumnaDeContraseniaEnTextoPlano` | ✅ |

### Veredictos por regla

| Regla | Resultado | Motivo |
|---|---|---|
| F-VER-01 | PASS con 3 WARN | AC-07/08/09 sin test de integración web en el camino exitoso |
| F-VER-02 | PASS | Los 4 bloques del spec están implementados |
| F-VER-03 | **FAIL** | 79.09%/64.28% globales, ambos <80%, arrastrados por `SsoAdmin.Web` |
| F-VER-04 | **FAIL** | `Usuarios/Create.cshtml.cs:OnPostAsync` y `Usuarios/Edit.cshtml.cs:OnPostAsync` sin ningún test, ni happy ni sad path |
| F-VER-05 | PASS | `dotnet build` 0 errores/0 warnings |
| F-VER-06 | PASS | Todo test listado explícitamente en el spec existe y pasa |
| W-VER-01 | WARN | `Pages/Index`, `Pages/Privacy`, `Pages/Error` (scaffold de Block 1) sin relación a ningún FR/AC — decisión del usuario: **eliminarlas** |
| W-VER-02 | WARN | Rama de negocio en `Application` 87.5% (<90% recomendado) — falta sad-path de `ValidarNombre` |
| W-VER-03 | PASS | Sin tests frágiles |

**Veredicto: BLOCKED.** Corrective loop VERIFY → CODE.

### Plan de corrección aplicado

1. Eliminar `Pages/Index`, `Pages/Privacy`, `Pages/Error` (scaffold no solicitado) — decisión del
   usuario, confirmada 2026-08-17.
2. Agregar tests de integración web: happy path de `Create` (+ `Nombre` vacío), happy path de
   `Edit` (+ `OnGetAsync` con id existente, + `Nombre` vacío), happy path de `DarDeBaja`.
3. Agregar tests de sad-path de `ValidarNombre` en `UsuarioServiceTests` (nombre vacío, >200
   caracteres).
4. Agregar test de `Logout` (0% de cobertura, único flujo de sesión activa sin ningún test).
