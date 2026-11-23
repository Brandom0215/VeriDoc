# Veridoc API

Servicio backend RESTful desarrollado en **C# (.NET 10)**, **ASP.NET Core Web API** y **PostgreSQL**, diseñado para validar información estructurada de documentos en formato JSON mediante un motor de reglas de negocio programadas.

> **Guardarraíl funcional:** Veridoc comprueba únicamente las condiciones expresadas en las reglas implementadas (campos obligatorios, formatos, límites e inconsistencias entre campos). No certifica autenticidad legal, no verifica veracidad en el mundo real ni toma decisiones financieras.

---

## 1. Estructura de la Solución

```text
VeriDoc/
├── Veridoc.API/                  # Servicio Backend Principal (ASP.NET Core Web API)
│   ├── Controllers/              # Endpoints REST (HealthController, ValidationsController)
│   ├── DTOs/                     # Contratos de entrada/salida y catálogo de metadatos
│   ├── Models/                   # Entidades de persistencia para PostgreSQL (EF Core)
│   ├── Services/                 # Orquestación de validación y auditoría (ValidationService)
│   ├── Validators/               # Reglas por tipo documental (Cédula y Factura Comercial)
│   ├── Data/                     # VeridocDbContext y DbInitializer (auto-seed del catálogo)
│   ├── Middlewares/              # Manejo global de errores (500) y autenticación X-Api-Key (401)
│   └── Program.cs                # Configuración de DI, CORS, OpenAPI/Swagger y pipeline HTTP
├── Veridoc.Tests/                # Suite de 37 pruebas automatizadas en xUnit
├── Veridoc.DemoClient/           # Cliente web externo e independiente (HTML + CSS + JS/Fetch)
│   ├── index.html
│   ├── styles.css
│   └── app.js
├── documents/
│   └── Veridoc_Contexto.md       # Documento de contexto funcional, técnico y decisiones del MVP
├── docker-compose.yml            # Contenedor PostgreSQL 16 listo para desarrollo local
└── Veridoc.slnx                  # Solución de Visual Studio
```

---

## 2. Tipos de Documentos y Reglas del MVP

### A. Cédula de Identidad (`cedula_identidad`)
- **Campos esperados:** `numeroCedula`, `nombreCompleto`, `fechaNacimiento`, `fechaExpedicion`, `fechaExpiracion`, `nacionalidad`.
- **6 Reglas implementadas:**
  1. `REQUIRED_FIELD`: Todos los campos son obligatorios.
  2. `INVALID_CEDULA_FORMAT`: Nomenclatura de cédula panameña (`8-888-8888`, `PE-12-345`, `E-8-123456`, `N-10-1234`, `1-AV-12-345`).
  3. `INVALID_BIRTH_DATE`: Formato `YYYY-MM-DD`, no futura y edad máxima razonable ($\le 120$ años).
  4. `UNDERAGE_HOLDER`: Titular mayor de edad ($\ge 18$ años cumplidos).
  5. `INCONSISTENT_ISSUE_EXPIRY_DATES`: `fechaExpedicion > fechaNacimiento` (no futura) y `fechaExpiracion > fechaExpedicion`.
  6. `DOCUMENT_EXPIRED`: Documento vigente (`fechaExpiracion >= fecha actual`).

### B. Factura Comercial (`factura_comercial`)
- **Campos esperados:** `numeroFactura`, `rucEmisor`, `nombreCliente`, `fechaEmision`, `fechaVencimiento`, `subtotal`, `itbms`, `total`, `moneda`.
- **6 Reglas implementadas:**
  1. `REQUIRED_FIELD`: Todos los campos de texto y numéricos son obligatorios.
  2. `INVALID_FORMAT`: `numeroFactura` con patrón `FAC-XXXX`, `rucEmisor` con estructura válida de RUC y `moneda` en `USD` o `PAB`.
  3. `OUT_OF_RANGE_AMOUNT`: `subtotal > 0`, `itbms >= 0`, `total > 0`.
  4. `INCONSISTENT_TOTAL`: Consistencia aritmética exacta (`subtotal + itbms == total`).
  5. `INCONSISTENT_TAX_RATE`: Congruencia del ITBMS al $0\%$ (exento) o $7\%$ del `subtotal` ($\pm 0.01$).
  6. `INCONSISTENT_INVOICE_DATES`: Formato `YYYY-MM-DD`, `fechaEmision` no futura y `fechaVencimiento >= fechaEmision`.

---

## 3. Endpoints Principales

| Método | Ruta | Autenticación | Descripción |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/health` | Público | Verifica la disponibilidad operativa del servicio. |
| `POST` | `/api/validations` | `X-Api-Key` | Evalúa un documento JSON y devuelve `isValid` y la lista `errors`. |
| `GET` | `/api/validations/document-types` | `X-Api-Key` | Devuelve los tipos de documentos soportados y el catálogo de sus reglas. |
| `GET` | `/api/validations/history?limit=20` | `X-Api-Key` | Consulta en PostgreSQL el historial de validaciones (cumple minimización de datos Ley 81). |

---

## 4. Cómo Ejecutar el Proyecto

### Paso 1: Levantar PostgreSQL (Opcional para pruebas en memoria, requerido para historial)
```powershell
docker compose up -d
```

### Paso 2: Iniciar `Veridoc.API`
Desde Visual Studio (F5) o por terminal:
```powershell
dotnet run --project Veridoc.API
```
- URL HTTP local por defecto: `http://localhost:5197`
- Documentación Swagger UI: `http://localhost:5197/swagger`
- Clave de desarrollo local (`X-Api-Key`): `veridoc-dev-key-2026`

### Paso 3: Usar el Cliente Externo de Demostración (`Veridoc.DemoClient`)
Abra el archivo `Veridoc.DemoClient/index.html` directamente en cualquier navegador web (o mediante *Live Server*). El cliente se conecta vía HTTP/CORS a `http://localhost:5197` enviando la cabecera `X-Api-Key`.

### Paso 4: Ejecutar las Pruebas Automatizadas (37 casos en xUnit)
```powershell
dotnet test Veridoc.slnx
```
