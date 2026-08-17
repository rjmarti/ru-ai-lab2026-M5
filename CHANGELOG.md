# Changelog

Todos los cambios notables de este proyecto se documentan en este archivo.

El formato está basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/).

## [Unreleased]

### Added
- [FEAT-001] Setup de la solución SsoAdmin (.NET 10, 6 proyectos con Vertical Slice Architecture +
  DDD, EF Core sobre SQLite).
- [FEAT-001] Login básico de Seguridad Informática (SI) con cookie auth endurecida (`HttpOnly`,
  `Secure`, `SameSite=Strict`) y usuario `admin`/`admin` precargado en el primer arranque.
- [FEAT-001] Administración de Usuarios vía web: listar (con estado activo/inactivo), crear,
  editar nombre y dar de baja lógica, protegido detrás del login.
- [FEAT-001] Logging de auditoría de cada login y cada acción sobre Usuarios (alta, edición, baja).
