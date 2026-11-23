# Veridoc API — Arquitectura, Diagramas C4, Flujo y Modelo Entidad-Relación

**Proyecto:** Veridoc API (MVP)\
**Stack:** C# (.NET 10), ASP.NET Core Web API, Entity Framework Core, PostgreSQL (Supabase) y Cliente Web Ligero (HTML/CSS/JS)

> **Nota de fidelidad arquitectónica:** Todos los diagramas de este documento reflejan únicamente los componentes reales implementados y verificados en la solución `Veridoc.slnx`.

---

## 1. Diagramas C4 del Sistema

### 1.1. Nivel 1: Diagrama C4 de Contexto

Muestra cómo **Veridoc API** interactúa con sus usuarios/actores y con el almacenamiento en la nube (Supabase PostgreSQL).

```mermaid
flowchart TD
    User["Usuario o Evaluador - Introduce datos de prueba y revisa resultados"]
    ClientSys["Sistema Cliente o Veridoc.DemoClient - Aplicacion consumidora"]
    VeridocSystem["Veridoc API - Servicio RESTful en ASP.NET Core .NET 10"]
    SupabaseDB[("PostgreSQL en Supabase - Catalogo de reglas e historial sin datos personales")]

    User -->|"Introduce datos y visualiza errores"| ClientSys
    ClientSys -->|"Solicitudes HTTP/HTTPS + JSON con cabecera X-Api-Key"| VeridocSystem
    VeridocSystem -->|"Consultas y registros EF Core sobre SSL"| SupabaseDB
```

---

### 1.2. Nivel 2: Diagrama C4 de Contenedores

Muestra los contenedores tecnológicos que conforman la solución completa y cómo se comunican entre sí de forma desacoplada.

```mermaid
flowchart LR
    subgraph Cliente ["Entorno del Cliente Externo"]
        DemoUI["Veridoc.DemoClient (HTML5 + CSS3 + JS Fetch)"]
        SwaggerUI["Swagger UI / Cliente HTTP (OpenAPI v1)"]
    end

    subgraph Backend ["Servidor de Aplicacion (.NET 10)"]
        WebApi["Veridoc.API (ASP.NET Core Web API)"]
    end

    subgraph CloudDB ["Almacenamiento Cloud (Supabase)"]
        Postgres[("PostgreSQL 16 - Esquema public")]
    end

    DemoUI -->|"POST y GET /api/validations (CORS + X-Api-Key)"| WebApi
    SwaggerUI -->|"HTTP/JSON + X-Api-Key"| WebApi
    WebApi -->|"TCP/SSL (Puerto 6543) Npgsql + EF Core 10"| Postgres
```

---

### 1.3. Nivel 3: Diagrama C4 de Componentes (Interior de `Veridoc.API`)

Muestra la separación de responsabilidades interna del proyecto `Veridoc.API`.

```mermaid
flowchart TD
    subgraph Pipeline ["Tuberia HTTP y Seguridad (Middlewares)"]
        ExMw["GlobalExceptionMiddleware (X-Correlation-Id y errores 500)"]
        SecMw["SecurityHeadersMiddleware (Cabeceras HTTP defensivas)"]
        RateMw["RateLimiter (Proteccion anti-fuerza bruta 429)"]
        AuthMw["ApiKeyAuthMiddleware (Validacion X-Api-Key en tiempo constante 401)"]
    end

    subgraph Controllers ["Capa de Controladores (Delgados)"]
        HealthCtrl["HealthController (GET /api/health)"]
        ValCtrl["ValidationsController (POST /api/validations, GET document-types, GET history)"]
    end

    subgraph Services ["Capa de Servicios de Negocio"]
        ValSvc["ValidationService (IValidationService)"]
    end

    subgraph Validators ["Modulo de Validacion (Patron Strategy)"]
        IVal["IDocumentValidator (Interfaz comun)"]
        CedVal["CedulaIdentidadValidator (6 reglas de cedula_identidad)"]
        FacVal["FacturaComercialValidator (6 reglas de factura_comercial)"]
        JsonHelper["JsonElementHelper (Extraccion segura de tipos y longitudes JSON)"]
    end

    subgraph DataLayer ["Capa de Acceso a Datos (EF Core)"]
        DbInit["DbInitializer (DDL idempotente y seed de las 12 reglas)"]
        DbCtx["VeridocDbContext (Mapeo Fluent API de 4 entidades)"]
        ConnHelper["PostgresConnectionStringHelper (Normaliza URL postgresql de Supabase)"]
    end

    ExMw --> SecMw
    SecMw --> RateMw
    RateMw --> AuthMw
    AuthMw --> HealthCtrl
    AuthMw --> ValCtrl
    ValCtrl --> ValSvc
    ValSvc --> IVal
    IVal -.->|"implementado por"| CedVal
    IVal -.->|"implementado por"| FacVal
    CedVal --> JsonHelper
    FacVal --> JsonHelper
    ValSvc --> DbCtx
    DbInit --> DbCtx
    ConnHelper --> DbCtx
```

---

## 2. Diagrama de Flujo de Validación Documental

Representa el recorrido completo de una solicitud desde que ingresa a `Veridoc.API` hasta que devuelve la respuesta JSON diferenciando entre **documento válido**, **documento inválido por reglas**, **solicitud mal formada (`400`)**, **no autorizada (`401`)**, **límite de peticiones (`429`)** y **error técnico (`500`)**.

```mermaid
flowchart TD
    Start(["Inicio: Solicitud HTTP entrante"]) --> GenCorr["GlobalExceptionMiddleware: Validar o generar X-Correlation-Id seguro"]
    GenCorr --> CheckRate{"RateLimiter: ¿Supera limite por IP?"}
    CheckRate -->|"Si"| Ret429(["Devolver HTTP 429 Too Many Requests (RATE_LIMIT_EXCEEDED)"])
    CheckRate -->|"No"| CheckPublic{"¿Es ruta publica? (/api/health o /swagger)"}

    CheckPublic -->|"Si"| RouteCtrl
    CheckPublic -->|"No"| CheckKey{"ApiKeyAuthMiddleware: ¿X-Api-Key presente y valida?"}

    CheckKey -->|"No"| Ret401(["Devolver HTTP 401 Unauthorized (ApiErrorResponseDto)"])
    CheckKey -->|"Si"| CheckJson{"¿JSON bien formado, <= 64 KB y profundidad <= 16?"}

    CheckJson -->|"No"| Ret400(["Devolver HTTP 400 Bad Request (INVALID_REQUEST_FORMAT)"])
    CheckJson -->|"Si"| RouteCtrl["ValidationsController: Delegar a ValidationService"]

    RouteCtrl --> CheckTypeProvided{"¿documentType presente y <= 64 caracteres?"}
    CheckTypeProvided -->|"No"| BuildInvalidType["Registrar error REQUIRED_FIELD o INVALID_FORMAT en documentType"]

    CheckTypeProvided -->|"Si"| CheckTypeSupported{"¿Existe IDocumentValidator para documentType?"}
    CheckTypeSupported -->|"No"| BuildUnsupported["Registrar error UNSUPPORTED_DOCUMENT_TYPE"]

    CheckTypeSupported -->|"Si"| CheckDocNode{"¿Nodo document es un objeto JSON?"}
    CheckDocNode -->|"No"| BuildMissingDoc["Registrar error REQUIRED_FIELD en campo document"]

    CheckDocNode -->|"Si"| RunRules["Ejecutar todas las reglas del validador acumulando errores"]

    BuildInvalidType --> PersistStep
    BuildUnsupported --> PersistStep
    BuildMissingDoc --> PersistStep
    RunRules --> PersistStep["Persistir auditoria en Supabase (Validacion + DetalleError sin datos personales)"]

    PersistStep --> CheckDbError{"¿Ocurrio fallo de BD con RequirePersistence=true?"}
    CheckDbError -->|"Si"| Ret500(["GlobalExceptionMiddleware: Devolver HTTP 500 Internal Server Error controlado"])
    CheckDbError -->|"No"| Ret200(["Devolver HTTP 200 OK (ValidationResponseDto: isValid + errors)"])
```

---

## 3. Modelo Entidad-Relación (PostgreSQL / Supabase)

El esquema relacional implementado en `public` consta de 4 tablas diseñadas bajo el principio de **minimización de datos personales (Ley 81 de 2019 de Panamá)**:

```mermaid
erDiagram
    TIPOS_DOCUMENTO ||--o{ REGLAS_VALIDACION : "tiene configuradas"
    TIPOS_DOCUMENTO |o--o{ VALIDACIONES : "clasifica"
    VALIDACIONES ||--o{ DETALLES_ERROR : "registra incumplimientos"

    TIPOS_DOCUMENTO {
        int Id PK "Identificador autoincremental"
        varchar Codigo UK "Maximo 64 caracteres (cedula_identidad, factura_comercial)"
        varchar Nombre "Maximo 150 caracteres"
        boolean Activo "Estado de disponibilidad"
        timestamptz FechaCreacionUtc "Fecha de registro UTC"
    }

    REGLAS_VALIDACION {
        int Id PK "Identificador autoincremental"
        int TipoDocumentoId FK "Referencia a tipos_documento.Id"
        varchar Codigo "Maximo 80 caracteres (REQUIRED_FIELD, etc.)"
        varchar Nombre "Maximo 150 caracteres"
        varchar Descripcion "Maximo 500 caracteres"
        boolean Activa "Indica si la regla esta activa"
    }

    VALIDACIONES {
        int Id PK "Identificador autoincremental"
        int TipoDocumentoId FK "Nullable (NULL si tipo no soportado)"
        varchar CodigoTipoDocumento "Maximo 64 caracteres"
        boolean EsValido "true si TotalErrores es 0"
        int TotalErrores "Cantidad de reglas incumplidas"
        varchar CorrelationId "Maximo 64 caracteres"
        timestamptz FechaValidacionUtc "Fecha y hora UTC de ejecucion"
    }

    DETALLES_ERROR {
        int Id PK "Identificador autoincremental"
        int ValidacionId FK "Referencia a validaciones.Id ON DELETE CASCADE"
        varchar Campo "Maximo 100 caracteres (sin valor personal)"
        varchar CodigoRegla "Maximo 80 caracteres"
        varchar Mensaje "Maximo 500 caracteres"
    }
```
