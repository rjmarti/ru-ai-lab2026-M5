# PRD-001: Backend para un Single Sign on (SSO)
Versión: 3

## Contexto y Problema
Nuestro organismo tiene múltiples aplicaciones y cada usuario ingresa a cada una de ellas con distintas credenciales.
Seguridad Informática (SI) necesita centralizar la administración de accesos para reducir la carga operativa y poder dar de baja usuarios en forma unificada, proceso que hoy toma horas.
Se implementará un backend que sirva como fuente de verdad para un SSO externo que consultará si un usuario tiene acceso a una aplicación determinada.


### Actores
- **Seguridad Informática (SI):** administra usuarios, credenciales, aplicaciones y permisos a través de esta aplicación.
- **SSO:** servicio externo que recibe credenciales de un proveedor de autenticación y consulta este backend para verificar si el usuario puede acceder a una aplicación.
- **Usuario Final:** accede a las aplicaciones; es autenticado por un servicio externo y sus credenciales son usadas para validar sus permisos.

## Objetivos
- Centralizar la administración de Usuarios, Credenciales y Aplicaciones.
- Permitir dar de baja un usuario en forma unificada, sin intervenir aplicación por aplicación.
- Exponer una API que el SSO pueda consultar para determinar si una credencial tiene acceso a una aplicación.


## Requerimientos Funcionales

- **RF-01:** El sistema debe permitir crear una credencial asociada a un usuario, con un `username` y un `emisor`, validando que la combinación (`username` + `emisor`) sea única en el sistema.
- **RF-02:** El sistema debe permitir asociar múltiples credenciales a un mismo usuario, siempre que provengan de emisores distintos.
- **RF-03:** El sistema debe permitir registrar aplicaciones con un nombre y una URL no vacía.
- **RF-04:** El sistema debe permitir otorgar permisos de acceso a una aplicación para un usuario, indicando una `fecha_desde` obligatoria y una `fecha_hasta` opcional (si se omite, el permiso es indefinido). Los períodos de un mismo usuario para una misma aplicación no deben solaparse.
- **RF-05:** El sistema debe permitir revocar el permiso de acceso de un usuario a una aplicación, estableciendo la `fecha_hasta` igual a la fecha actual.
- **RF-06:** El sistema debe permitir dar de baja lógica a un usuario, lo que debe caducar automáticamente todos sus permisos activos en todas las aplicaciones.
- **RF-07:** El sistema debe implementar un login básico para que los usuarios de SI puedan autenticarse en la aplicación. Para esta etapa se almacenara en una tabla `login` de la base de datos con un hash no reversible. Para el primer login precargar un usuario `admin` con password `admin`
- **RF-08:** El sistema debe exponer un endpoint REST para que el SSO consulte si una credencial tiene acceso a una aplicación, con el siguiente contrato:
  - **Método:** `POST /api/sso/verificar`
  - **Request body:**
    ```json
    {
      "username": "string",
      "emisor": "string",
      "aplicacion_url": "string"
    }
    ```
  - **Response body:**
    ```json
    {
      "allowed": true | false,
      "motivo": "string | null"
    }
    ```
  - **Valores de `motivo`** (presentes solo cuando `allowed=false`): `credencial_no_encontrada`, `usuario_inactivo`, `aplicacion_no_encontrada`, `permiso_no_encontrado`, `permiso_vencido`.
  - **Códigos HTTP:** `200 OK` en todos los casos resueltos (incluso denegaciones); `400 Bad Request` si falta algún campo; `500 Internal Server Error` ante error inesperado.
  - **Tiempo de respuesta:** menor a 500 ms (ver RNF-01).
- **RF-09:** La app web debe permitir administrar Usuarios: listar todos los usuarios con su estado activo/inactivo, crear un nuevo usuario, editar su nombre, y dar de baja lógica (lo que caduca sus permisos activos según RF-06).
- **RF-10:** La app web debe permitir administrar Credenciales: listar todas las credenciales con el usuario asociado, crear una nueva credencial indicando usuario, `username` y `emisor`, y eliminar una credencial existente.
- **RF-11:** La app web debe permitir administrar Aplicaciones: listar todas las aplicaciones, registrar una nueva aplicación con nombre y URL, editar nombre y URL de una aplicación existente, y eliminar una aplicación.


## Requerimientos No Funcionales
- **RNF-01:** La consulta del endpoint RF-08 debe responder en menos de 500 ms, asumiendo un máximo de 100 aplicaciones y 3000 usuarios.
- **RNF-02:** La baja lógica de un usuario debe caducar todos sus permisos activos en menos de 3 segundos.
- **RNF-03:** El sistema no debe almacenar contraseñas; solo el `emisor` y el `username` de cada credencial.


## Criterios de Aceptación

### AC-01 — Credencial única (RF-01)
- **Dado** que ya existe una credencial con `username=u1` y `emisor=google`, **cuando** SI intenta crear otra credencial con los mismos valores para cualquier usuario, **entonces** el sistema devuelve un error `400` indicando que la combinación ya existe.

### AC-02 — Credencial asignada a un solo usuario (RF-01, RF-02)
- **Dado** que la credencial (`u1`, `google`) está asignada al usuario A, **cuando** SI intenta asignarla al usuario B, **entonces** el sistema devuelve un error `400` indicando que la credencial ya está en uso.

### AC-03 — URL de aplicación no vacía (RF-03)
- **Dado** que SI intenta registrar una aplicación sin URL o con URL vacía, **cuando** se envía la solicitud, **entonces** el sistema devuelve un error `400` de validación.

### AC-04 — Períodos sin solapamiento (RF-04)
- **Dado** que el usuario A tiene un permiso a la aplicación X desde `2026-01-01` hasta `2026-06-30`, **cuando** SI intenta crear un permiso desde `2026-03-01` hasta `2026-12-31`, **entonces** el sistema devuelve un error `409 Conflict` indicando solapamiento de períodos.

### AC-05 — Revocación de permiso (RF-05)
- **Dado** que el usuario A tiene un permiso activo para la aplicación X, **cuando** SI revoca el permiso, **entonces** la `fecha_hasta` se establece en la fecha actual y cualquier consulta posterior del SSO para ese usuario y esa aplicación devuelve `allowed=false` con `motivo=permiso_vencido`.

### AC-06 — Baja lógica de usuario (RF-06)
- **Dado** que el usuario A tiene permisos activos en múltiples aplicaciones, **cuando** SI da de baja lógica al usuario, **entonces** todos sus permisos son caducados en menos de 3 segundos y el SSO recibe `allowed=false` con `motivo=usuario_inactivo` para cualquier consulta posterior.

### AC-07 — Endpoint SSO: acceso permitido (RF-08, RNF-01)
- **Dado** una credencial válida (`u1`, `google`) asociada a un usuario activo con permiso vigente en la aplicación `https://app.ejemplo.com`, **cuando** el SSO consulta `POST /api/sso/verificar`, **entonces** el sistema responde `200 OK` con `allowed=true` en menos de 500 ms.

### AC-08 — Endpoint SSO: permiso vencido (RF-08)
- **Dado** una credencial válida cuyo permiso a la aplicación venció ayer, **cuando** el SSO consulta, **entonces** el sistema responde `allowed=false` con `motivo=permiso_vencido`.

### AC-09 — Endpoint SSO: usuario dado de baja (RF-08)
- **Dado** una credencial válida de un usuario dado de baja, **cuando** el SSO consulta, **entonces** el sistema responde `allowed=false` con `motivo=usuario_inactivo`.

### AC-10 — Endpoint SSO: aplicación inexistente (RF-08)
- **Dado** una credencial válida y una URL de aplicación que no existe en el sistema, **cuando** el SSO consulta, **entonces** el sistema responde `allowed=false` con `motivo=aplicacion_no_encontrada`.

### AC-11 — Endpoint SSO: credencial inexistente (RF-08)
- **Dado** un `username` o `emisor` que no existe en el sistema, **cuando** el SSO consulta, **entonces** el sistema responde `allowed=false` con `motivo=credencial_no_encontrada`.

### AC-12 — Login básico (RF-07)
- **Dado** que un usuario de SI ingresa credenciales válidas en el formulario de login, **cuando** envía el formulario, **entonces** el sistema otorga acceso a las funciones de administración.
- **Dado** que un usuario de SI ingresa credenciales inválidas, **cuando** envía el formulario, **entonces** el sistema devuelve un error `401` y no otorga acceso.

### AC-13 — No almacenamiento de contraseñas (RNF-03)
- **Dado** que se registra o actualiza cualquier credencial, **cuando** se inspecciona el registro en la base de datos, **entonces** no existe ningún campo que contenga una contraseña, hash o derivado de contraseña.

### AC-14 — Web: administración de usuarios (RF-09)
- **Dado** que un usuario de SI está autenticado en la app web, **cuando** navega a la sección de Usuarios, **entonces** puede ver el listado de usuarios con su estado activo/inactivo, crear un nuevo usuario ingresando su nombre, editar el nombre de un usuario existente, y dar de baja lógica a un usuario activo; la baja se refleja inmediatamente en el listado y caduca todos sus permisos activos.

### AC-15 — Web: administración de credenciales (RF-10)
- **Dado** que un usuario de SI está autenticado en la app web, **cuando** navega a la sección de Credenciales, **entonces** puede ver el listado de credenciales con el usuario asociado, crear una nueva credencial indicando usuario, `username` y `emisor`, y eliminar una credencial existente; el sistema impide crear una credencial con combinación (`username`, `emisor`) ya registrada mostrando un mensaje de error.

### AC-16 — Web: administración de aplicaciones (RF-11)
- **Dado** que un usuario de SI está autenticado en la app web, **cuando** navega a la sección de Aplicaciones, **entonces** puede ver el listado de aplicaciones registradas, registrar una nueva aplicación indicando nombre y URL, editar nombre y URL de una aplicación existente, y eliminar una aplicación; el sistema impide registrar o editar una aplicación con URL vacía mostrando un mensaje de error.


## Fuera de Alcance
- La implementación del SSO queda fuera del alcance de este proyecto.
- El sistema no debe implementar OAuth, SAML, OpenID Connect ni login federado en esta etapa.
- No están dentro del alcance: aprovisionamiento automático, integración con Active Directory/LDAP, sincronización con otros sistemas, flujos de recuperación de credenciales ni administración avanzada de roles.


## Riesgos y Dependencias
- **Riesgo:** Inconsistencias por credenciales duplicadas entre usuarios. Mitigado por RF-01 y AC-01/AC-02.
- **Riesgo:** Permisos solapados para un mismo usuario y aplicación. Mitigado por RF-04 y AC-04.
- **Dependencia:** SQLite — utilizado para el MVP. Se reemplazará por una base de datos más robusta antes de pasar a producción.