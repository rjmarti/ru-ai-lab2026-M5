# Threat Model FEAT-001: Setup de solución SsoAdmin + login básico + administración de Usuarios

| Field | Value |
|-------|-------|
| Ticket | FEAT-001 |
| Spec | docs/daw/specs/spec-FEAT-001.md |
| Date | 2026-08-17T13:51:35Z |

## Componentes analizados

1. **Login** (`SsoAdmin.Web/Pages/Login`, `AuthenticationService`, `PasswordHasherService`) — Block 3.
2. **Sesión / autorización** (cookie auth de ASP.NET Core, middlewares) — Block 3.
3. **Administración de Usuarios** (`SsoAdmin.Web/Pages/Usuarios/*`, `UsuarioService`) — Block 4.
4. **Capa de datos** (`SsoAdminDbContext`, migraciones, seed, archivo SQLite) — Block 2.

## Trust boundaries (F-TM-02)

| # | Boundary | Cruce |
|---|---|---|
| TB1 | Navegador (no confiable) → `SsoAdmin.Web` | Envío del formulario de login y de los formularios de Usuarios |
| TB2 | `SsoAdmin.Web` → `SsoAdmin.Application` | In-process (misma confianza de proceso, pero `Application` no debe asumir que el input ya fue validado por `Web`) |
| TB3 | `SsoAdmin.Application` → `SsoAdmin.Data` (SQLite) | Toda consulta pasa por EF Core (parametrizado, sin SQL crudo) |
| TB4 | Navegador (cookie de sesión) → `SsoAdmin.Web` en cada request autenticado | Validación del cookie de auth en cada acceso a `/Usuarios/*` |
| TB5 | Filesystem → archivo `ssoadmin.db` | Acceso al archivo SQLite en disco |

## Clasificación de datos sensibles (F-TM-05)

| Dato | Clasificación | Dónde |
|---|---|---|
| `Login.Username`, `Login.PasswordHash` | Credenciales | Tabla `Login` (SQLite) |
| Cookie de sesión (ticket de auth) | Credenciales / token de sesión | Navegador del usuario de SI |
| `Usuario.Nombre` | PII (baja sensibilidad — solo nombre, sin DNI/email/datos financieros) | Tabla `Usuario` (SQLite) |

**Cifrado (F-TM-07):**
- **En tránsito:** HTTPS obligatorio (`UseHttpsRedirection()`, Block 3) para credenciales, cookie
  de sesión y `Usuario.Nombre` — cubre las tres filas de la tabla.
- **En reposo — credenciales:** `Login.PasswordHash` nunca se cifra, se **hashea** con
  `PasswordHasher<T>` (PBKDF2, one-way) — el control correcto para contraseñas es hash no
  reversible, no cifrado reversible (AC-10 ya lo exige).
- **En reposo — PII (`Usuario.Nombre`):** el archivo SQLite **no está cifrado**. Ver riesgo R9 más
  abajo — es un riesgo aceptado, no una mitigación.

## Riesgos

### R1 — Credencial `admin`/`admin` predecible (Spoofing)
- **Likelihood:** Medium (herramienta interna, pero la credencial es pública en el propio PRD).
- **Impact:** High (compromete el 100% de la administración — no hay roles).
- **Mitigación:** ninguna viable dentro del alcance de este ticket — el PRD excluye
  explícitamente cambio/recuperación de contraseña y roles avanzados (`Out of Scope`).
- **Accepted risk** (F-TM-04):
  - **Quién acepta:** Ruben Martinez (product owner de este ticket, en esta conversación).
  - **Justificación:** MVP de uso interno, un único usuario de SI precargado; el PRD general
    (`docs/daw/prd/PRD.md`) ya declara SQLite y este flujo de login como transitorios hasta
    reemplazar la base antes de producción.
  - **Condición de revisión:** antes de desplegar a un ambiente accesible fuera de la red interna,
    o al agregar un segundo usuario de SI — lo que ocurra primero. Ese ticket debe forzar cambio
    de contraseña en el primer login.

### R2 — Cookie de sesión sin hardening (Tampering / Information Disclosure)
- **Likelihood:** Medium. **Impact:** High (secuestro de sesión = acceso admin completo).
- **Mitigación (folded into spec, Block 3):** `HttpOnly=true`, `SecurePolicy=Always`,
  `SameSite=Strict`, expiración de 8h con sliding expiration. Test agregado que verifica los
  atributos del `Set-Cookie`.

### R3 — CSRF sobre los POST de Login/Usuarios (Tampering)
- **Likelihood:** Low (Razor Pages incluye antiforgery tokens por defecto en los `<form>` con tag
  helpers). **Impact:** High si se desactivara.
- **Mitigación:** ninguna acción nueva — usar los tag helpers estándar de Razor Pages y no
  deshabilitar `AutoValidateAntiforgeryTokenAttribute` (comportamiento default). Documentado como
  restricción de implementación en Block 3/4.

### R4 — Inyección SQL (Tampering)
- **Likelihood:** Low. **Impact:** Critical.
- **Mitigación:** EF Core parametrizado en toda la capa de datos (Block 2/4), sin SQL crudo — ya
  es la convención declarada en `AGENTS.md` y `.daw/rules/security.instructions.md`.

### R5 — Elevación de privilegios (Elevation of Privilege)
- **Likelihood:** Low. **Impact:** N/A — no hay roles en este ticket (un único login = admin
  total), por lo que no existe un privilegio menor desde el cual escalar.
- **Mitigación:** N/A por diseño; queda fuera de alcance (RBAC excluido explícitamente por el
  PRD general).

### R6 — Enumeración de usuario por mensaje de error (Information Disclosure)
- **Likelihood:** Low. **Impact:** Low (un solo usuario válido posible: `admin`).
- **Mitigación:** ya cubierta en el spec (Block 3) — mensaje de error genérico que no distingue
  usuario inexistente de contraseña incorrecta.

### R7 — Sin registro de auditoría de acciones administrativas (Repudiation)
- **Likelihood:** High (nada lo registraba antes de esta revisión). **Impact:** Medium (no hay
  forma de saber quién dio de baja a un usuario).
- **Mitigación (folded into spec, Block 3 y Block 4):** `ILogger<T>` registra cada intento de
  login (éxito/fallo, sin loguear la contraseña) y cada alta/edición/baja de Usuario con el
  `Username` de SI que la ejecutó — vía `ILogger<T>`, nunca `Console.WriteLine` (regla de
  `AGENTS.md`).

### R8 — Denegación de servicio (Denial of Service)
- **Likelihood:** Low. **Impact:** Low — herramienta interna de bajo tráfico (RNF-01 del PRD
  general habla de hasta 3000 usuarios y 100 aplicaciones, no de tráfico público).
- **Mitigación:** ninguna acción — W-TM-02 aplica (servicio interno no crítico), no se agrega
  rate limiting en este ticket.

### R9 — PII (`Usuario.Nombre`) sin cifrar en reposo (Information Disclosure)
- **Likelihood:** Low (requiere acceso al filesystem del servidor). **Impact:** Medium (nombres
  de personas, sin otro dato identificatorio).
- **Accepted risk** (F-TM-04):
  - **Quién acepta:** Ruben Martinez.
  - **Justificación:** el dato es solo `Nombre` (sin DNI/email/teléfono); SQLite ya está aceptado
    como dependencia transitoria del MVP en el PRD general (`docs/daw/prd/PRD.md`, sección
    Riesgos y Dependencias) — cifrar el archivo agregaría una dependencia (SQLCipher) para un MVP
    que de todas formas va a migrar de motor de base de datos.
  - **Condición de revisión:** al migrar de SQLite a la base de datos definitiva antes de
    producción (ya comprometido en el PRD general), evaluar cifrado a nivel de columna o de disco
    según el motor elegido.

### R10 — Ataque de timing para enumerar usuarios (Information Disclosure)
- **Likelihood:** Low. **Impact:** Low — solo existe un `Username` válido posible (`admin`) en
  este ticket, así que no hay un conjunto de usuarios sobre el que enumerar.
- **Mitigación:** ninguna acción — riesgo no aplicable en la práctica con un solo login;
  revisar si se agregan más cuentas de SI en el futuro.

## Dependencias de terceros (W-TM-01)

Nuevas dependencias introducidas: `Microsoft.EntityFrameworkCore.Sqlite`,
`Microsoft.AspNetCore.Identity` (solo `PasswordHasher<T>`), `Microsoft.EntityFrameworkCore.InMemory`
(test). Todas son paquetes oficiales de Microsoft, mantenidos activamente, sin CVEs conocidos a la
fecha de este análisis. No se agrega ninguna dependencia de terceros no-Microsoft.

## Resumen

| Categoría | Cantidad |
|---|---|
| 🔴 Critical | 0 |
| 🟠 High | 2 (R1, R2) — R1 aceptado, R2 mitigado en el spec |
| 🟡 Medium | 2 (R7, R9) — R7 mitigado en el spec, R9 aceptado |
| 🟢 Low | 6 (R3, R4, R5, R6, R8, R10) — mitigados o no aplicables |

**Mitigaciones incorporadas al spec:**
1. Hardening de cookie de sesión (Block 3) — R2.
2. Logging de auditoría de login y de CRUD de Usuarios vía `ILogger<T>` (Block 3, Block 4) — R7.

**Riesgos aceptados — confirmados por Ruben Martinez el 2026-08-17:** R1 (credencial
`admin`/`admin`) y R9 (PII sin cifrar en SQLite), con las condiciones de revisión detalladas en
cada uno.
