# PRD FEAT-001: Setup de solución SsoAdmin + login básico + administración de Usuarios

| Field | Value |
|-------|-------|
| Ticket | FEAT-001 |
| Tracker | none |
| Date | 2026-08-17T13:34:23Z |
| PRD loops | 1 |

## Context and Problem

SsoAdmin todavía no existe como código: el repositorio no tiene solución .NET (`./src` está vacío).
Antes de poder construir la administración de Credenciales, Aplicaciones y Permisos, y el endpoint
que consultará el SSO externo, se necesita la base sobre la que todo eso se apoya: la solución .NET
con sus proyectos, el modelo de datos inicial, un mecanismo de autenticación para que Seguridad
Informática (SI) pueda entrar a la app web, y la primera pantalla de administración funcional:
Usuarios.

Este PRD recorta del PRD general (`docs/daw/prd/PRD.md`) únicamente el subconjunto de
requerimientos necesario para esa primera entrega: RF-07 (login básico) y RF-09 (administración de
Usuarios), junto con la porción de RF-06 que aplica sin que existan aún Aplicaciones/Permisos (la
baja lógica del usuario en sí, sin la caducidad de permisos que depende de un módulo futuro).

## Goals

- Dejar creada la solución .NET (`SsoAdmin.Data`, `SsoAdmin.Models`, `SsoAdmin.Application`,
  `SsoAdmin.API`, `SsoAdmin.Web`, `SsoAdmin.Test`) siguiendo Vertical Slice Architecture + DDD, con
  EF Core y SQLite configurados para el MVP.
- Permitir que un usuario de SI se autentique en la app web mediante un login básico.
- Permitir que un usuario de SI administre Usuarios (listar, crear, editar, dar de baja lógica)
  desde la app web, protegido detrás del login.

## Functional Requirements

- FR-01: El sistema debe proveer una solución .NET con los proyectos `SsoAdmin.Data`,
  `SsoAdmin.Models`, `SsoAdmin.Application`, `SsoAdmin.API`, `SsoAdmin.Web` y `SsoAdmin.Test`,
  configurados con EF Core sobre SQLite.
- FR-02: El sistema debe definir una entidad `Usuario` con al menos: identificador, nombre y estado
  activo/inactivo.
- FR-03: El sistema debe almacenar credenciales de acceso de SI en una tabla `Login`, guardando la
  contraseña únicamente como un hash no reversible.
- FR-04: El sistema debe precargar, en el primer arranque, un usuario `admin` con password `admin`
  (almacenada como hash no reversible) en la tabla `Login`.
- FR-05: El sistema debe exponer un formulario de login donde un usuario de SI ingresa usuario y
  contraseña para autenticarse.
- FR-06: El sistema debe restringir el acceso a las páginas de administración de Usuarios a
  usuarios de SI autenticados.
- FR-07: El sistema debe permitir listar todos los usuarios con su estado activo/inactivo.
- FR-08: El sistema debe permitir crear un nuevo usuario indicando su nombre; el usuario nuevo
  queda activo.
- FR-09: El sistema debe permitir editar el nombre de un usuario existente.
- FR-10: El sistema debe permitir dar de baja lógica a un usuario activo, marcándolo como inactivo.

## Non-Functional Requirements

- NFR-01: El sistema no debe almacenar contraseñas en texto plano; únicamente el hash no reversible
  de la contraseña del login de SI.
- NFR-02: La solución debe compilar y correr sobre .NET 10 / C# 12, siguiendo las convenciones
  declaradas en `AGENTS.md` (PascalCase, DI por constructor, sin `.Result`/`.Wait()`).

## Acceptance Criteria

- AC-01: WHEN se despliega la solución sobre una base de datos vacía, THE sistema SHALL crear el
  esquema derivado de la entidad `Usuario` y la tabla `Login` vía EF Core/SQLite (FR-01, FR-02,
  FR-03).
- AC-02: WHEN el sistema arranca por primera vez sin registros en la tabla `Login`, THE sistema
  SHALL precargar un usuario `admin` con contraseña `admin` almacenada como hash no reversible
  (FR-04).
- AC-03: WHEN un usuario de SI ingresa credenciales válidas en el formulario de login, THE sistema
  SHALL otorgarle acceso a las funciones de administración (FR-05).
- AC-04: IF un usuario de SI ingresa credenciales inválidas en el login, THEN THE sistema SHALL
  denegar el acceso y no crear ninguna sesión autenticada (FR-05).
- AC-05: IF un usuario no autenticado intenta acceder a la sección de Usuarios, THEN THE sistema
  SHALL redirigirlo al formulario de login (FR-06).
- AC-06: WHEN un usuario de SI autenticado navega a la sección de Usuarios, THE sistema SHALL
  mostrar el listado de usuarios con su estado activo/inactivo (FR-07).
- AC-07: WHEN un usuario de SI crea un nuevo usuario indicando su nombre, THE sistema SHALL
  agregarlo al listado como activo (FR-08).
- AC-08: WHEN un usuario de SI edita el nombre de un usuario existente, THE sistema SHALL
  actualizar el nombre y reflejar el cambio en el listado (FR-09).
- AC-09: WHEN un usuario de SI da de baja lógica a un usuario activo, THE sistema SHALL marcarlo
  como inactivo y reflejar el cambio inmediatamente en el listado (FR-10).
- AC-10: IF se inspecciona la tabla `Login`, THEN THE sistema SHALL no exponer ningún campo que
  contenga la contraseña en texto plano (FR-03).

## Out of Scope

- Administración de Credenciales y Aplicaciones (RF-10, RF-11 del PRD general).
- Permisos de acceso a aplicaciones y su caducidad (RF-04, RF-05, y la porción de RF-06 referida a
  caducar permisos activos — no hay permisos todavía en este ticket).
- El endpoint `POST /api/sso/verificar` (RF-08).
- OAuth, SAML, OpenID Connect o login federado.
- Recuperación de contraseña, cambio de contraseña del usuario `admin`, y administración avanzada
  de roles.
- Aprovisionamiento automático, integración con Active Directory/LDAP o sincronización con otros
  sistemas.

## Risks and Mitigations

- **Riesgo:** el usuario `admin`/`admin` precargado es una credencial débil conocida. Mitigación:
  aceptado explícitamente para este MVP por RF-07 del PRD general; el cambio de contraseña queda
  fuera de alcance de este ticket.
- **Riesgo:** SQLite no es apto para producción. Mitigación: es la dependencia declarada para el
  MVP en el PRD general; se reemplazará antes de producción (fuera de alcance de este ticket).

## Dependencies

- Ninguna dependencia de otro módulo de SsoAdmin (este ticket es la base).
- SQLite como proveedor de EF Core para el MVP, según el PRD general.
