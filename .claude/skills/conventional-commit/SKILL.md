---
name: conventional-commit
description: Genera un mensaje de commit siguiendo el estándar Conventional Commits. Se utiliza cuando el usuario va a crear un commit o cuando solicita un mensaje de commit para sus cambios.
---

Analiza los cambios staged (`git diff --cached`) y el historial reciente (`git log --oneline -5`) del repositorio actual. Con esa información, genera un mensaje de commit que cumpla estas reglas:

## Formato obligatorio

```
tipo(scope): descripción
```

- **tipo**: uno de `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `build`, `ci`, `chore`, `revert`
- **scope**: módulo, capa o componente afectado en minúscula (ej. `auth`, `api`, `domain`); omítelo si el cambio es transversal
- **descripción**: en imperativo, minúscula, sin punto final, máximo 72 caracteres en total (incluyendo `tipo(scope): `)

## Tipos de commit

| Tipo | Cuándo usarlo |
|------|--------------|
| `feat` | Nueva funcionalidad para el usuario |
| `fix` | Corrección de un bug |
| `docs` | Solo cambios en documentación |
| `style` | Formato, espacios, comas (sin cambio de lógica) |
| `refactor` | Refactorización sin nuevas features ni fixes |
| `perf` | Mejora de rendimiento |
| `test` | Añadir o corregir tests |
| `build` | Sistema de build, dependencias, scripts |
| `ci` | Configuración de CI/CD |
| `chore` | Tareas de mantenimiento sin impacto en producción |
| `revert` | Revertir un commit anterior |

## Reglas estrictas

1. Todo en minúscula excepto acrónimos obligatorios (ej. `SSO`, `JWT`)
2. Sin punto final en la descripción
3. La línea completa no supera 72 caracteres
4. Descripción en modo imperativo: "add", "fix", "remove", "update" — nunca "added", "fixing", "removed"
5. Si el diff afecta múltiples capas, elige el scope del cambio más relevante

## Salida esperada

Muestra únicamente el mensaje de commit listo para copiar, precedido de una línea que explique brevemente el tipo y scope elegidos. Si hay ambigüedad, propón dos alternativas ordenadas de mayor a menor confianza.

**Ejemplo de salida:**

> Tipo `feat`, scope `auth` — nueva funcionalidad en la capa de autenticación.
>
> ```
> feat(auth): add jwt refresh token endpoint
> ```
