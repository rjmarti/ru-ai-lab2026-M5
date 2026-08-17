# Contexto del Proyecto
Este proyecto es una aplicación para administrar un Backend para un Single Sign On (SSO) 

**NombreProyecto** SsoAdmin
Crear el proyecto en ./src
                                                 
## Stack
- **Lenguaje:** C# 12
- **Frameworks:** .NET 10 
- **Frontend:** ASP.NET, Razor, Bootstrap, Javascript Vanilla.
- **Patrones de Arquitectura:** Vertical Slice Architecture, Domain-Driven Design (DDD), Inyección de Dependencias.
- **ORM:** Entity Framework Core

## Architecture conventions
- Sigue siempre las convenciones oficiales de Microsoft para C#.
- Usa **PascalCase** para nombres de clases, métodos y propiedades.
- Usa **camelCase** para parámetros y variables locales.
- Evita el uso de variables implícitas (`var`) a menos que el tipo sea evidente en la misma línea.
- Las interfaces deben comenzar con la letra `I` (ej. `IRepository`).
- Todos los servicios deben ser inyectados mediante inyección de dependencia en el constructor.
- Documenta clases y métodos públicos utilizando comentarios XML (`/// <summary>`).

## Domain glossary
Asume la siguiente estructura de solución:
- `[NombreProyecto].Data`: Acceso y Mapeo a la base de Datos.Implementación de EF Core.
- `[NombreProyecto].Models`: Entidades base.
- `[NombreProyecto].Application`: Casos de uso y lógica de negocios.
- `[NombreProyecto].API`: Controladores, middlewares y configuración de la API.
- `[NombreProyecto].Web`: Sitio Web para administrar la aplicación
- `[NombreProyecto].Test`: Casos de Test para cubrir los criterios de aceptación

## 4. Restricciones
- No expongas entidades del Dominio directamente en las respuestas de la API; utiliza DTOs (Data Transfer Objects).
- No uses `System.Console.WriteLine` para logs. Utiliza siempre `ILogger<T>`.
- Evita llamadas asíncronas bloqueantes (no uses `.Result` o `.Wait()`); utiliza siempre `await`.

## 5. Comandos y Verificaciones (CLI)
Antes de declarar una tarea como terminada, el agente debe ejecutar los siguientes comandos en la terminal desde la raíz del proyecto:
- Restaurar dependencias: `dotnet restore`
- Construir solución: `dotnet build`
- Ejecutar pruebas: `dotnet test`

<!-- BEGIN DAW (managed by DAW — do not edit by hand) -->
# DAW — Dilux Agentic Workflow

This repo uses **DAW**: an agent-driven development pipeline with the phases
`CLASSIFY → DEFINE → PLAN → CODE → VERIFY → RELEASE`.

Before answering, read `.daw/orchestrator.md` and run its Boot Sequence. It is a strict state
machine: it decides what you are allowed to do based on the phase recorded in `.daw-state.json`.

The project's own context — stack, architecture, domain — is elsewhere in this file. It lives here,
in `AGENTS.md`, and not in any one tool's file, on purpose: it is tool-agnostic and comes along
unchanged when the pipeline is ported to another agent.
<!-- END DAW -->
