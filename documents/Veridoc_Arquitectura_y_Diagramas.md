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
    User["Usuario / Evaluador<br/>(Persona que prueba o revisa documentos)"]
    ClientSys["Sistema Cliente / Integrador<br/>(Aplicación empresarial o Veridoc.DemoClient)"]
    VeridocSystem["Veridoc API<br/>(Servicio RESTful en ASP.NET Core .NET 10<br/>que valida documentos JSON mediante reglas)"]
    SupabaseDB[("PostgreSQL en Supabase<br/>(Almacena catálogo de reglas e historial<br/>de auditoría sin datos personales)")]

    User -->|"Introduce datos de prueba y visualiza errores"| ClientSys
    ClientSys -->|"Solicitudes HTTP/HTTPS + JSON<br/>Cabecera X-Api-Key"| VeridocSystem
    VeridocSystem -->|"Consultas y registros EF Core (SSL / Npgsql)"| SupabaseDB
```

---

### 1.2. Nivel 2: Diagrama C4 de Contenedores

Muestra los contenedores tecnológicos que conforman la solución completa y cómo se comunican entre sí de forma desacoplada.

```mermaid
flowchart LR
    subgraph Cliente ["Entorno del Cliente (Navegador / Sistema Externo)"]
        DemoUI["Veridoc.DemoClient<br/>(HTML5 + CSS3 + JavaScript Fetch)<br/>Consola web independiente sin lógica de negocio"]
        SwaggerUI["Swagger UI / Cliente HTTP<br/>(OpenAPI v1 + Veridoc.API.http)"]
    end

    subgraph Backend ["Servidor de Aplicación (.NET 10)"]
        WebApi["Veridoc.API<br/>(ASP.NET Core Web API)<br/>Expone endpoints REST, autentica X-Api-Key,<br/>ejecuta reglas y registra auditoría"]
    end

    subgraph CloudDB ["Almacenamiento Cloud (Supabase)"]
        Postgres[("Base de Datos PostgreSQL 16<br/>Esquema public: tipos_documento,<br/>reglas_validacion, validaciones, detalles_error")]
    end

    DemoUI -->|"POST /api/validations<br/>GET /api/validations/document-types<br/>GET /api/validations/history<br/>(CORS + X-Api-Key)"| WebApi
    SwaggerUI -->|"HTTP/JSON + X-Api-Key"| WebApi
    WebApi -->|"TCP/SSL (Puerto 6543 / 5432)<br/>Npgsql + EF Core 10"| Postgres
```

---

### 1.3. Nivel 3: Diagrama C4 de Componentes (Interior de `Veridoc.API`)

Muestra la separación de responsabilidades interna del proyecto `Veridoc.API`.

```mermaid
flowchart TD
    subgraph Pipeline ["Tubería HTTP (Middlewares)"]
        ExMw["GlobalExceptionMiddleware<br/>(Asigna X-Correlation-Id y captura errores 500)"]
        AuthMw["ApiKeyAuthMiddleware<br/>(Valida cabecera X-Api-Key en tiempo constante -> 401)"]
    end

    subgraph Controllers ["Capa de Controladores (Delgados)"]
        HealthCtrl["HealthController<br/>(GET /api/health)"]
        ValCtrl["ValidationsController<br/>(POST /api/validations, GET document-types, GET history)"]
    end

    subgraph Services ["Capa de Servicios de Negocio"]
        ValSvc["ValidationService (IValidationService)<br/>(Orquesta validación, selecciona validador y persiste auditoría)"]
    end

    subgraph Validators ["Módulo de Validación (Patrón Strategy)"]
        IVal["IDocumentValidator<br/>(Contrato común de validación)"]
        CedVal["CedulaIdentidadValidator<br/>(6 reglas para cedula_identidad)"]
        FacVal["FacturaComercialValidator<br/>(6 reglas para factura_comercial)"]
        JsonHelper["JsonElementHelper<br/>(Extracción segura de tipos JSON sin excepciones)"]
    end

    subgraph DataLayer ["Capa de Acceso a Datos (EF Core)"]
        DbInit["DbInitializer<br/>(DDL idempotente y seed de las 12 reglas)"]
        DbCtx["VeridocDbContext<br/>(Mapeo Fluent API de 4 entidades)"]
        ConnHelper["PostgresConnectionStringHelper<br/>(Normaliza URL postgresql:// de Supabase)"]
    end

    ExMw --> AuthMw
    AuthMw --> HealthCtrl
    AuthMw --> ValCtrl
    ValCtrl --> ValSvc
    ValSvc --> IVal
    IVal <|.. CedVal
    IVal <|.. FacVal
    CedVal --> JsonHelper
    FacVal --> JsonHelper
    ValSvc --> DbCtx
    DbInit --> DbCtx
    ConnHelper --> DbCtx
```

---

## 2. Diagrama de Flujo de Validación Documental

Representa el recorrido completo de una solicitud desde que ingresa a `Veridoc.API` hasta que devuelve la respuesta JSON diferenciando entre **documento válido**, **documento inválido por reglas**, **solicitud mal formada (`400`)**, **no autorizada (`401`)** y **error técnico (`500`)**.

```mermaid
flowchart TD
    Start(["Inicio: Solicitud HTTP entrante"]) --> GenCorr["GlobalExceptionMiddleware:<br/>Asignar o leer X-Correlation-Id"]
    GenCorr --> CheckPublic{"¿Es ruta pública?<br/>(/api/health o /swagger)"}

    CheckPublic -->|"Sí"| RouteCtrl
    CheckPublic -->|"No"| CheckKey{"ApiKeyAuthMiddleware:<br/>¿X-Api-Key presente y válida?"}

    CheckKey -->|"No"| Ret401(["Devolver HTTP 401 Unauthorized<br/>(ApiErrorResponseDto)"])
    CheckKey -->|"Sí"| CheckJson{"¿JSON bien formado<br/>y cuerpo no nulo?"}

    CheckJson -->|"No"| Ret400(["Devolver HTTP 400 Bad Request<br/>(INVALID_REQUEST_FORMAT / EMPTY_REQUEST_BODY)"])
    CheckJson -->|"Sí"| RouteCtrl["ValidationsController:<br/>Delegar a ValidationService"]

    RouteCtrl --> CheckTypeProvided{"¿documentType<br/>presente?"}
    CheckTypeProvided -->|"No"| BuildInvalidType["Registrar error REQUIRED_FIELD<br/>en campo documentType"]

    CheckTypeProvided -->|"Sí"| CheckTypeSupported{"¿Existe IDocumentValidator<br/>para documentType?"}
    CheckTypeSupported -->|"No"| BuildUnsupported["Registrar error UNSUPPORTED_DOCUMENT_TYPE"]

    CheckTypeSupported -->|"Sí"| CheckDocNode{"¿Nodo document es<br/>un objeto JSON?"}
    CheckDocNode -->|"No"| BuildMissingDoc["Registrar error REQUIRED_FIELD<br/>en campo document"]

    CheckDocNode -->|"Sí"| RunRules["Ejecutar todas las reglas del validador<br/>(CedulaIdentidadValidator / FacturaComercialValidator)<br/>acumulando incumplimientos en lista errors"]

    BuildInvalidType --> PersistStep
    BuildUnsupported --> PersistStep
    BuildMissingDoc --> PersistStep
    RunRules --> PersistStep["Persistir auditoría en PostgreSQL (Supabase)<br/>(Validacion + DetalleError, sin guardar datos personales)"]

    PersistStep --> CheckDbError{"¿Ocurrió fallo de BD<br/>con RequirePersistence=true?"}
    CheckDbError -->|"Sí"| Ret500(["GlobalExceptionMiddleware:<br/>Devolver HTTP 500 Internal Server Error controlado"])
    CheckDbError -->|"No"| Ret200(["Devolver HTTP 200 OK<br/>(ValidationResponseDto: isValid + errors)"])
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
        int Id PK "GENERATED BY DEFAULT AS IDENTITY"
        varchar(64) Codigo UK "Ej. cedula_identidad, factura_comercial"
        varchar(150) Nombre "Nombre legible del tipo documental"
        boolean Activo "Estado de disponibilidad"
        timestamptz FechaCreacionUtc "Fecha de registro UTC"
    }

    REGLAS_VALIDACION {
        int Id PK "GENERATED BY DEFAULT AS IDENTITY"
        int TipoDocumentoId FK "Referencia a tipos_documento(Id)"
        varchar(80) Codigo "Ej. REQUIRED_FIELD, UNDERAGE_HOLDER"
        varchar(150) Nombre "Título corto de la regla"
        varchar(500) Descripcion "Condición verificada por la regla"
        boolean Activa "Indica si la regla está activa"
    }

    VALIDACIONES {
        int Id PK "GENERATED BY DEFAULT AS IDENTITY"
        int TipoDocumentoId FK "Nullable (NULL si tipo no soportado)"
        varchar(64) CodigoTipoDocumento "Tipo solicitado por el cliente"
        boolean EsValido "true si TotalErrores = 0"
        int TotalErrores "Cantidad de reglas incumplidas"
        varchar(64) CorrelationId "ID de trazabilidad HTTP"
        timestamptz FechaValidacionUtc "Fecha y hora UTC de ejecución"
    }

    DETALLES_ERROR {
        int Id PK "GENERATED BY DEFAULT AS IDENTITY"
        int ValidacionId FK "Referencia a validaciones(Id) ON DELETE CASCADE"
        varchar(100) Campo "Nombre del campo que falló (sin su valor personal)"
        varchar(80) CodigoRegla "Código de la regla incumplida"
        varchar(500) Mensaje "Explicación del error devuelta al cliente"
    }
```
