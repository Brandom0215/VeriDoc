# Veridoc API — Registro de Decisiones de Arquitectura (ADR), Riesgos y Cronograma

Este documento registra formalmente las decisiones arquitectónicas tomadas durante el diseño e implementación del MVP de **Veridoc API**, así como el plan de trabajo y la matriz de riesgos gestionados por el equipo.

---

## 1. Registro de Decisiones de Arquitectura (ADR)

### ADR-001: Arquitectura Monolítica Modular en ASP.NET Core (.NET 10)
- **Estado:** Aprobado e Implementado.
- **Contexto:** El equipo cuenta con 3 integrantes y un plazo de 4 semanas para entregar un servicio funcional, verificable y defendible académicamente.
- **Decisión:** Implementar un único servicio backend (`Veridoc.API`) con separación interna por capas/carpetas (`Controllers`, `Services`, `Validators`, `Data`, `Models`, `DTOs`, `Middlewares`), descartando microservicios múltiples, colas de mensajes o gateways.
- **Consecuencias:** Menor complejidad operativa, depuración directa desde Visual Studio y latencias de validación inferiores a $25\text{ ms}$.

---

### ADR-002: Recepción Polimórfica con `JsonElement` y Patrón Strategy (`IDocumentValidator`)
- **Estado:** Aprobado e Implementado.
- **Contexto:** Un único endpoint (`POST /api/validations`) debe recibir distintos tipos de documentos (`cedula_identidad`, `factura_comercial`) cuyos esquemas JSON son completamente diferentes, sin romper el deserializador cuando un campo trae un tipo de dato inesperado.
- **Decisión:**
  1. Definir la propiedad `Document` de `ValidationRequestDto` como `JsonElement?`.
  2. Definir la interfaz `IDocumentValidator` e inyectar sus implementaciones (`CedulaIdentidadValidator`, `FacturaComercialValidator`) en un diccionario dentro de `ValidationService`.
  3. Centralizar la lectura segura de propiedades en `JsonElementHelper`.
- **Consecuencias:** Agregar un tercer tipo de documento en el futuro solo requiere crear una clase que implemente `IDocumentValidator` sin modificar el controlador ni el servicio orquestador. Además, los errores de tipo en JSON (ej. enviar texto en `subtotal`) se reportan como errores de validación (`INVALID_FORMAT`) en vez de causar caídas `500`.

---

### ADR-003: Modelo Híbrido de Reglas (Lógica en C# + Catálogo en PostgreSQL/Supabase)
- **Estado:** Aprobado e Implementado.
- **Contexto:** Se requería evaluar si las reglas se programarían en código o se interpretarían dinámicamente desde tablas SQL.
- **Decisión:** Implementar la lógica de evaluación en clases C# compiladas (usando `[GeneratedRegex]` y aritmética `decimal`) y sincronizar automáticamente al arranque (`DbInitializer`) el catálogo descriptivo en las tablas `tipos_documento` y `reglas_validacion` de PostgreSQL en **Supabase**.
- **Consecuencias:** Permite escribir pruebas unitarias rápidas y deterministas en xUnit sobre cada regla, manteniendo al mismo tiempo la trazabilidad relacional en PostgreSQL.

---

### ADR-004: Minimización de Datos Personales en Persistencia y Logs (Ley 81 de 2019)
- **Estado:** Aprobado e Implementado.
- **Contexto:** Los documentos validados contienen datos sensibles (números de cédula, nombres, fechas de nacimiento, RUC y montos financieros) regulados en Panamá por la Ley 81 de 2019 sobre Protección de Datos Personales.
- **Decisión:** No almacenar en PostgreSQL ni en los logs del servidor el JSON del documento recibido ni los valores ingresados por el usuario. Las tablas `validaciones` y `detalles_error` guardan exclusivamente el tipo de documento, si fue válido, el `CorrelationId`, la fecha UTC, el nombre del campo que falló y el código/mensaje de la regla.
- **Consecuencias:** Se elimina el riesgo de exposición de datos personales en caso de acceso a los registros de auditoría o logs técnicos.

---

### ADR-005: Autenticación mediante Cabecera `X-Api-Key` y Comparación en Tiempo Constante
- **Estado:** Aprobado e Implementado.
- **Contexto:** Los endpoints de validación e historial deben estar protegidos frente a consumos no autorizados, manteniendo credenciales fuera del repositorio Git.
- **Decisión:** Implementar `ApiKeyAuthMiddleware` exigiendo la cabecera `X-Api-Key`, comparada mediante `CryptographicOperations.FixedTimeEquals` contra la clave configurada en `appsettings.Development.json` (ignorado en `.gitignore`) o variables de entorno, e integrarla en el botón **Authorize** de Swagger UI.
- **Consecuencias:** Protección efectiva y sencilla de demostrar tanto desde clientes externos como desde Swagger UI, previniendo ataques de temporización (*timing attacks*).

---

### ADR-006: Cliente de Demostración Externo y Desacoplado (`Veridoc.DemoClient`)
- **Estado:** Aprobado e Implementado.
- **Contexto:** Se necesitaba una interfaz gráfica sencilla para demostrar el uso de la API sin convertirla en una plataforma empresarial ni acoplarla al backend.
- **Decisión:** Construir una aplicación web ligera en `Veridoc.DemoClient/` (`index.html`, `styles.css`, `app.js`) independiente del proyecto C# `Veridoc.API`, que consume la API vía `fetch()` utilizando CORS y la cabecera `X-Api-Key`, sin duplicar ninguna regla de validación en JavaScript.
- **Consecuencias:** Demuestra empíricamente que `Veridoc.API` puede integrarse con cualquier sistema cliente externo.

---

## 2. Cronograma de Trabajo y Responsabilidades (4 Semanas)

| Semana | Actividades Ejecutadas | Entregable / Hito Alcanzado |
| :---: | :--- | :--- |
| **Semana 1** | Selección de los 2 tipos documentales (`cedula_identidad` y `factura_comercial`), definición de 6 reglas por tipo, diseño del modelo relacional y contratos JSON. | Contexto y diseño inicial aprobados (`Veridoc_Contexto.md`). |
| **Semana 2** | Configuración de `Veridoc.API` (.NET 10), DTOs, `GlobalExceptionMiddleware`, `ApiKeyAuthMiddleware`, `JsonElementHelper` y validadores de Cédula y Factura. | API valida documentos en memoria y expone Swagger con seguridad `X-Api-Key`. |
| **Semana 3** | Integración de Entity Framework Core con PostgreSQL en **Supabase** (`VeridocDbContext`, `PostgresConnectionStringHelper`, `DbInitializer`) y construcción de `Veridoc.DemoClient`. | Flujo completo integrado de extremo a extremo con persistencia en Supabase. |
| **Semana 4** | Implementación de la suite `Veridoc.Tests` con 38 pruebas automatizadas en xUnit, pipeline CI en GitHub Actions y documentación técnica (C4, MER, Flujo, ADR). | Prototipo verificado al 100 % (`38/38` pruebas) y documentación lista para defensa. |

---

## 3. Matriz de Gestión de Riesgos

| Riesgo Identificado | Impacto | Mitigación Implementada en el Proyecto | Estado |
| :--- | :---: | :--- | :---: |
| **Reglas de negocio con falsos positivos o errores de redondeo** | Alto | Uso de tipo `decimal` con `MidpointRounding.AwayFromZero`, expresiones regulares compiladas `[GeneratedRegex]` y 38 pruebas automatizadas con fecha de referencia fija. | Mitigado |
| **Problemas de conexión o esquemas preexistentes en PostgreSQL / Supabase** | Alto | Creación de `PostgresConnectionStringHelper` (soporte URI + puerto 6543 Supavisor) y DDL idempotente `CREATE TABLE IF NOT EXISTS public.*` en `DbInitializer`. | Mitigado |
| **Exposición de credenciales o datos personales (Ley 81)** | Crítico | Exclusión de `appsettings.Development.json` en `.gitignore`, omisión del payload documental en logs y persistencia únicamente de metadatos de validación. | Mitigado |
| **Confusión entre documento inválido y fallo técnico de la API** | Medio | Separación estricta de contratos: `ValidationResponseDto` (`200 OK` con `isValid: false`) frente a `ApiErrorResponseDto` (`400`, `401`, `500` con `CorrelationId`). | Mitigado |
| **Regresiones al integrar cambios de los 3 integrantes** | Medio | Configuración del flujo `.github/workflows/dotnet-ci.yml` que compila y ejecuta las 38 pruebas automáticamente en cada `push` o `pull_request`. | Mitigado |
