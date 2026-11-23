# Veridoc API — Matriz de Casos de Prueba y Resultados Obtenidos

**Proyecto de pruebas:** `Veridoc.Tests` (xUnit v2.9.3 sobre .NET 10.0)\
**Comando de verificación:** `dotnet test Veridoc.slnx`\
**Meta de calidad definida en el proyecto:** $\ge 30$ casos de prueba con un mínimo de $95\%$ de resultados correctos y tiempo $< 500\text{ ms}$.\
**Resultado real medido:** **38 casos ejecutados | 38 exitosos ($100\%$ de acierto) | Tiempo total de suite: $< 1.0\text{ s}$ ($< 25\text{ ms}$ por caso).**

---

## 1. Pruebas de Cédula de Identidad (`CedulaIdentidadValidatorTests.cs`)

| # | Método / Caso de Prueba | Categoría | Datos Ficticios de Entrada (Resumen) | Resultado Esperado | Resultado Obtenido | Estado |
| :-: | :--- | :--- | :--- | :--- | :--- | :-: |
| 01 | `Validate_ValidPanamanianCedulaFormats` (Caso 1) | Documento válido | `numeroCedula: "8-888-8888"`, fechas vigentes, titular de 31 años | `Errors` vacío (`IsValid = true`) | `Errors` vacío (`0` errores) | PASS |
| 02 | `Validate_ValidPanamanianCedulaFormats` (Caso 2) | Documento válido | `numeroCedula: "PE-12-345"`, Panameño nacido en el extranjero | `Errors` vacío (`IsValid = true`) | `Errors` vacío (`0` errores) | PASS |
| 03 | `Validate_ValidPanamanianCedulaFormats` (Caso 3) | Documento válido | `numeroCedula: "E-8-123456"`, Extranjero residente | `Errors` vacío (`IsValid = true`) | `Errors` vacío (`0` errores) | PASS |
| 04 | `Validate_ValidPanamanianCedulaFormats` (Caso 4) | Documento válido | `numeroCedula: "1-AV-12-345"`, Antes de la vigencia | `Errors` vacío (`IsValid = true`) | `Errors` vacío (`0` errores) | PASS |
| 05 | `Validate_MissingRequiredField` (`numeroCedula`) | Campo obligatorio ausente | JSON de cédula sin la propiedad `numeroCedula` | Error `REQUIRED_FIELD` en `numeroCedula` | Error `REQUIRED_FIELD` en `numeroCedula` | PASS |
| 06 | `Validate_MissingRequiredField` (`nombreCompleto`) | Campo obligatorio ausente | JSON de cédula sin la propiedad `nombreCompleto` | Error `REQUIRED_FIELD` en `nombreCompleto` | Error `REQUIRED_FIELD` en `nombreCompleto` | PASS |
| 07 | `Validate_MissingRequiredField` (`fechaNacimiento`) | Campo obligatorio ausente | JSON de cédula sin la propiedad `fechaNacimiento` | Error `REQUIRED_FIELD` en `fechaNacimiento` | Error `REQUIRED_FIELD` en `fechaNacimiento` | PASS |
| 08 | `Validate_InvalidCedulaFormat` | Formato incorrecto | `numeroCedula: "15-888-8888"` (provincia 15 inexistente) | Error `INVALID_CEDULA_FORMAT` en `numeroCedula` | Error `INVALID_CEDULA_FORMAT` en `numeroCedula` | PASS |
| 09 | `Validate_UnexpectedJsonTypeInStringField` | Tipo JSON inesperado | `numeroCedula: 8888888` (número entero en lugar de string) | Error controlado `INVALID_FORMAT` en `numeroCedula` | Error `INVALID_FORMAT` en `numeroCedula` | PASS |
| 10 | `Validate_InvalidBirthDateFormat` | Formato incorrecto | `fechaNacimiento: "15/05/1995"` (formato no ISO) | Error `INVALID_BIRTH_DATE` en `fechaNacimiento` | Error `INVALID_BIRTH_DATE` en `fechaNacimiento` | PASS |
| 11 | `Validate_FutureBirthDate` | Valor fuera de límites | `fechaNacimiento: "2028-01-01"` (fecha futura) | Error `INVALID_BIRTH_DATE` en `fechaNacimiento` | Error `INVALID_BIRTH_DATE` en `fechaNacimiento` | PASS |
| 12 | `Validate_AgeExceedingMaximumLimit` | Valor fuera de límites | `fechaNacimiento: "1880-01-01"` (edad $> 120$ años) | Error `INVALID_BIRTH_DATE` en `fechaNacimiento` | Error `INVALID_BIRTH_DATE` en `fechaNacimiento` | PASS |
| 13 | `Validate_UnderageHolder` | Valor fuera de límites | `fechaNacimiento: "2012-09-10"` (edad $13 < 18$ años) | Error `UNDERAGE_HOLDER` en `fechaNacimiento` | Error `UNDERAGE_HOLDER` en `fechaNacimiento` | PASS |
| 14 | `Validate_ExpiredAndInconsistentIssueExpiryDates` | Inconsistencia entre campos y vigencia | `fechaExpedicion: "2024-05-01"`, `fechaExpiracion: "2023-05-01"` | Errores `INCONSISTENT_ISSUE_EXPIRY_DATES` y `DOCUMENT_EXPIRED` | Ambos errores reportados en `fechaExpiracion` | PASS |

---

## 2. Pruebas de Factura Comercial (`FacturaComercialValidatorTests.cs`)

| # | Método / Caso de Prueba | Categoría | Datos Ficticios de Entrada (Resumen) | Resultado Esperado | Resultado Obtenido | Estado |
| :-: | :--- | :--- | :--- | :--- | :--- | :-: |
| 15 | `Validate_ValidInvoiceWithSevenPercentItbms` | Documento válido | `FAC-0001`, `subtotal: 200`, `itbms: 14`, `total: 214`, `USD` | `Errors` vacío (`IsValid = true`) | `Errors` vacío (`0` errores) | PASS |
| 16 | `Validate_ValidTaxExemptInvoiceInPab` | Documento válido | `FAC-202605`, `subtotal: 150`, `itbms: 0`, `total: 150`, `PAB` | `Errors` vacío (`IsValid = true`) | `Errors` vacío (`0` errores) | PASS |
| 17 | `Validate_MissingNumeroFactura` | Campo obligatorio ausente | `numeroFactura: "   "` (solo espacios en blanco) | Error `REQUIRED_FIELD` en `numeroFactura` | Error `REQUIRED_FIELD` en `numeroFactura` | PASS |
| 18 | `Validate_MissingNumericSubtotal` | Campo obligatorio ausente | JSON de factura sin la propiedad numérica `subtotal` | Error `REQUIRED_FIELD` en `subtotal` | Error `REQUIRED_FIELD` en `subtotal` | PASS |
| 19 | `Validate_InvalidFieldFormats` (`numeroFactura`) | Formato incorrecto | `numeroFactura: "0001"` (sin prefijo `FAC-`) | Error `INVALID_FORMAT` en `numeroFactura` | Error `INVALID_FORMAT` en `numeroFactura` | PASS |
| 20 | `Validate_InvalidFieldFormats` (`rucEmisor`) | Formato incorrecto | `rucEmisor: "RUC_SIN_GUIONES"` | Error `INVALID_FORMAT` en `rucEmisor` | Error `INVALID_FORMAT` en `rucEmisor` | PASS |
| 21 | `Validate_InvalidFieldFormats` (`moneda`) | Formato incorrecto | `moneda: "EUR"` (moneda distinta de `USD`/`PAB`) | Error `INVALID_FORMAT` en `moneda` | Error `INVALID_FORMAT` en `moneda` | PASS |
| 22 | `Validate_StringInsteadOfDecimalInSubtotal` | Tipo JSON inesperado | `subtotal: "cien"` (string en campo numérico) | Error controlado `INVALID_FORMAT` en `subtotal` | Error `INVALID_FORMAT` en `subtotal` | PASS |
| 23 | `Validate_OutOfRangeAmounts` (`subtotal <= 0`) | Valor fuera de límites | `subtotal: 0.00`, `itbms: 0.00`, `total: 10.00` | Error `OUT_OF_RANGE_AMOUNT` en `subtotal` | Error `OUT_OF_RANGE_AMOUNT` en `subtotal` | PASS |
| 24 | `Validate_OutOfRangeAmounts` (`itbms < 0`) | Valor fuera de límites | `subtotal: 100.00`, `itbms: -5.00`, `total: 95.00` | Error `OUT_OF_RANGE_AMOUNT` en `itbms` | Error `OUT_OF_RANGE_AMOUNT` en `itbms` | PASS |
| 25 | `Validate_InconsistentTotal` | Inconsistencia entre campos | `subtotal: 100.00`, `itbms: 7.00`, `total: 120.00` | Error `INCONSISTENT_TOTAL` en `total` | Error `INCONSISTENT_TOTAL` en `total` | PASS |
| 26 | `Validate_InconsistentTaxRate` | Inconsistencia entre campos | `subtotal: 100.00`, `itbms: 15.00` ($15\% \ne 7\%$), `total: 115.00` | Error `INCONSISTENT_TAX_RATE` en `itbms` | Error `INCONSISTENT_TAX_RATE` en `itbms` | PASS |
| 27 | `Validate_InconsistentInvoiceDates` (Vencimiento) | Inconsistencia entre campos | `fechaEmision: "2026-06-10"`, `fechaVencimiento: "2026-06-01"` | Error `INCONSISTENT_INVOICE_DATES` en `fechaVencimiento` | Error `INCONSISTENT_INVOICE_DATES` en `fechaVencimiento` | PASS |
| 28 | `Validate_InconsistentInvoiceDates` (Emisión futura) | Valor fuera de límites | `fechaEmision: "2026-12-01"` (posterior a fecha de referencia) | Error `INCONSISTENT_INVOICE_DATES` en `fechaEmision` | Error `INCONSISTENT_INVOICE_DATES` en `fechaEmision` | PASS |

---

## 3. Pruebas de Pipeline, Seguridad, Formato y Base de Datos (`ValidationPipelineAndEdgeCasesTests.cs`)

| # | Método / Caso de Prueba | Categoría | Escenario / Entrada | Resultado Esperado | Resultado Obtenido | Estado |
| :-: | :--- | :--- | :--- | :--- | :--- | :-: |
| 29 | `ValidateDocumentAsync_UnsupportedDocumentType` | Tipo de documento no soportado | `documentType: "pasaporte"` | `IsValid = false`, código `UNSUPPORTED_DOCUMENT_TYPE` | `IsValid = false`, `UNSUPPORTED_DOCUMENT_TYPE` | PASS |
| 30 | `ValidateDocumentAsync_NullDocumentNode` | Estructura incompleta | `documentType: "cedula_identidad"`, `document: null` | `IsValid = false`, código `REQUIRED_FIELD` en `document` | `IsValid = false`, `REQUIRED_FIELD` en `document` | PASS |
| 31 | `GetSupportedDocumentTypes` | Catálogo de reglas | Consulta de metadatos de tipos documentales | 2 tipos documentales con $\ge 5$ reglas cada uno | 2 tipos con 6 reglas cada uno (12 reglas) | PASS |
| 32 | `ValidationsController_NullRequestBody` | JSON mal formado / Solicitud inválida | Cuerpo nulo enviado a `POST /api/validations` | `HTTP 400 Bad Request` (`EMPTY_REQUEST_BODY`) con `CorrelationId` | `HTTP 400 Bad Request` con `ApiErrorResponseDto` | PASS |
| 33 | `ApiKeyAuthMiddleware_MissingApiKeyHeader` | Seguridad (`X-Api-Key`) | Petición a `/api/validations` sin cabecera `X-Api-Key` | `HTTP 401 Unauthorized` (`MISSING_API_KEY`) | `HTTP 401 Unauthorized` | PASS |
| 34 | `ApiKeyAuthMiddleware_ValidatesProvidedKey` (Inválida) | Seguridad (`X-Api-Key`) | Petición con `X-Api-Key: "wrong-key"` | `HTTP 401 Unauthorized` (`INVALID_API_KEY`) | `HTTP 401 Unauthorized` | PASS |
| 35 | `ApiKeyAuthMiddleware_ValidatesProvidedKey` (Válida) | Seguridad (`X-Api-Key`) | Petición con `X-Api-Key: "secret-test-key"` | `HTTP 200 OK` (permite el paso al controlador) | `HTTP 200 OK` | PASS |
| 36 | `GlobalExceptionMiddleware_DatabaseConnectionFailure` | Error de conexión a PostgreSQL | Fallo de contexto PostgreSQL con `RequirePersistence = true` | `HTTP 500 Internal Server Error` (`INTERNAL_SERVER_ERROR`) sin filtrar contraseña | `HTTP 500` controlado, secreto no expuesto | PASS |
| 37 | `HealthController_GetHealth_Returns200Ok` | Disponibilidad / Observabilidad | `GET /api/health` | `HTTP 200 OK` (`status = "Healthy"`) | `HTTP 200 OK` | PASS |
| 38 | `PostgresConnectionStringHelper_ValidSupabaseUri` | Integración Cloud (Supabase) | URL `postgresql://postgres.abcdefg:MiClave%402026@...pooler.supabase.com:6543/postgres` | Cadena Npgsql con `SSL Mode=Require` y password decodificado | Cadena Npgsql normalizada correctamente | PASS |
