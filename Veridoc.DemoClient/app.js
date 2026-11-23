/**
 * ============================================================================
 * Veridoc.DemoClient - Cliente JavaScript desacoplado para Veridoc API
 * ============================================================================
 * Responsabilidad:
 * - Capturar los datos de prueba seleccionados o ingresados por el usuario.
 * - Enviar solicitudes HTTP (fetch) a Veridoc.API adjuntando la cabecera X-Api-Key.
 * - Renderizar la respuesta JSON del servidor sin duplicar ninguna regla de negocio.
 */

"use strict";

/**
 * Definición de la estructura visual de los formularios por tipo de documento.
 * Nota: Solo define etiquetas y tipos de input HTML para facilitar la demostración;
 * ninguna validación de negocio se ejecuta en el navegador.
 */
const DOCUMENT_SCHEMAS = {
    cedula_identidad: [
        { name: "numeroCedula", label: "Número de Cédula", type: "text", placeholder: "Ej. 8-888-8888" },
        { name: "nombreCompleto", label: "Nombre Completo", type: "text", placeholder: "Ej. Juan Carlos Pérez" },
        { name: "fechaNacimiento", label: "Fecha de Nacimiento (YYYY-MM-DD)", type: "text", placeholder: "1995-05-15" },
        { name: "fechaExpedicion", label: "Fecha de Expedición (YYYY-MM-DD)", type: "text", placeholder: "2022-06-01" },
        { name: "fechaExpiracion", label: "Fecha de Expiración (YYYY-MM-DD)", type: "text", placeholder: "2032-06-01" },
        { name: "nacionalidad", label: "Nacionalidad", type: "text", placeholder: "Panameña" }
    ],
    factura_comercial: [
        { name: "numeroFactura", label: "Número de Factura", type: "text", placeholder: "Ej. FAC-0001" },
        { name: "rucEmisor", label: "RUC del Emisor", type: "text", placeholder: "Ej. 1556789-1-700123" },
        { name: "nombreCliente", label: "Nombre del Cliente", type: "text", placeholder: "Ej. Distribuidora Istmeña S.A." },
        { name: "fechaEmision", label: "Fecha de Emisión (YYYY-MM-DD)", type: "text", placeholder: "2026-02-01" },
        { name: "fechaVencimiento", label: "Fecha de Vencimiento (YYYY-MM-DD)", type: "text", placeholder: "2026-03-01" },
        { name: "subtotal", label: "Subtotal", type: "number", step: "0.01", placeholder: "100.00" },
        { name: "itbms", label: "ITBMS (0% o 7%)", type: "number", step: "0.01", placeholder: "7.00" },
        { name: "total", label: "Total", type: "number", step: "0.01", placeholder: "107.00" },
        { name: "moneda", label: "Moneda (USD o PAB)", type: "text", placeholder: "USD" }
    ],
    pasaporte: [
        { name: "numeroPasaporte", label: "Número de Pasaporte", type: "text", placeholder: "PA-998877" },
        { name: "titular", label: "Titular", type: "text", placeholder: "Usuario de Prueba" }
    ]
};

/**
 * Casos de prueba ficticios preconfigurados para demostrar rápidamente escenarios válidos e inválidos.
 */
const PRESET_CASES = {
    cedula_valida: {
        documentType: "cedula_identidad",
        document: {
            numeroCedula: "8-888-8888",
            nombreCompleto: "Juan Carlos Pérez Gómez",
            fechaNacimiento: "1995-05-15",
            fechaExpedicion: "2022-06-01",
            fechaExpiracion: "2032-06-01",
            nacionalidad: "Panameña"
        }
    },
    cedula_invalida: {
        documentType: "cedula_identidad",
        document: {
            numeroCedula: "99-ABC-123",
            nombreCompleto: "Ana López",
            fechaNacimiento: "2012-08-20",
            fechaExpedicion: "2024-01-10",
            fechaExpiracion: "2023-01-10",
            nacionalidad: "Panameña"
        }
    },
    factura_valida: {
        documentType: "factura_comercial",
        document: {
            numeroFactura: "FAC-0001",
            rucEmisor: "1556789-1-700123",
            nombreCliente: "Distribuidora Istmeña S.A.",
            fechaEmision: "2026-02-01",
            fechaVencimiento: "2026-03-01",
            subtotal: 100.00,
            itbms: 7.00,
            total: 107.00,
            moneda: "USD"
        }
    },
    factura_invalida: {
        documentType: "factura_comercial",
        document: {
            numeroFactura: "0001",
            rucEmisor: "RUC_INVALIDO",
            nombreCliente: "Distribuidora Istmeña S.A.",
            fechaEmision: "2026-03-10",
            fechaVencimiento: "2026-02-01",
            subtotal: 100.00,
            itbms: 15.00,
            total: 120.00,
            moneda: "EUR"
        }
    }
};

// Estado local de modo de edición ("form" o "json")
let currentEditMode = "form";

// Referencias a elementos del DOM
const apiBaseUrlInput = document.getElementById("apiBaseUrl");
const apiKeyInput = document.getElementById("apiKeyInput");
const documentTypeSelect = document.getElementById("documentTypeSelect");
const dynamicFormContainer = document.getElementById("dynamicFormContainer");
const rawJsonContainer = document.getElementById("rawJsonContainer");
const rawJsonTextarea = document.getElementById("rawJsonTextarea");
const tabFormMode = document.getElementById("tabFormMode");
const tabJsonMode = document.getElementById("tabJsonMode");

/**
 * Obtiene la URL base limpia (sin barra diagonal final) configurada por el usuario.
 * @returns {string} URL base de Veridoc API.
 */
function getCleanApiBaseUrl() {
    return (apiBaseUrlInput.value || "").trim().replace(/\/+$/, "");
}

/**
 * Genera dinámicamente los campos HTML del formulario según el tipo de documento seleccionado.
 * @param {string} documentType Identificador del tipo documental.
 * @param {Object} initialData Objeto opcional con valores iniciales para poblar los inputs.
 */
function renderFormFields(documentType, initialData = {}) {
    dynamicFormContainer.innerHTML = "";
    const fields = DOCUMENT_SCHEMAS[documentType] || [];

    fields.forEach((field) => {
        const wrapper = document.createElement("div");
        wrapper.className = "form-group";

        const label = document.createElement("label");
        label.setAttribute("for", `field_${field.name}`);
        label.textContent = `${field.label} (${field.name})`;

        const input = document.createElement("input");
        input.id = `field_${field.name}`;
        input.name = field.name;
        input.type = field.type || "text";
        if (field.step) {
            input.step = field.step;
        }
        input.placeholder = field.placeholder || "";

        if (Object.prototype.hasOwnProperty.call(initialData, field.name) && initialData[field.name] !== null) {
            input.value = initialData[field.name];
        }

        input.addEventListener("input", syncFormToRawJson);

        wrapper.appendChild(label);
        wrapper.appendChild(input);
        dynamicFormContainer.appendChild(wrapper);
    });

    syncFormToRawJson();
}

/**
 * Construye el objeto de solicitud (ValidationRequestDto) a partir de los valores actuales del formulario
 * y actualiza el editor JSON crudo.
 */
function syncFormToRawJson() {
    const docType = documentTypeSelect.value;
    const fields = DOCUMENT_SCHEMAS[docType] || [];
    const documentObj = {};

    fields.forEach((field) => {
        const inputEl = document.getElementById(`field_${field.name}`);
        if (!inputEl) {
            return;
        }

        const rawVal = inputEl.value;
        if (field.type === "number") {
            documentObj[field.name] = rawVal === "" ? null : Number(rawVal);
        } else {
            documentObj[field.name] = rawVal;
        }
    });

    const payload = {
        documentType: docType,
        document: documentObj
    };

    rawJsonTextarea.value = JSON.stringify(payload, null, 2);
}

/**
 * Cambia entre la vista de Formulario de Campos y el Editor JSON Directo.
 * @param {"form"|"json"} mode Modo de visualización a activar.
 */
function switchEditMode(mode) {
    currentEditMode = mode;
    if (mode === "form") {
        tabFormMode.classList.add("active");
        tabJsonMode.classList.remove("active");
        dynamicFormContainer.classList.remove("hidden");
        rawJsonContainer.classList.add("hidden");
    } else {
        tabJsonMode.classList.add("active");
        tabFormMode.classList.remove("active");
        dynamicFormContainer.classList.add("hidden");
        rawJsonContainer.classList.remove("hidden");
    }
}

/**
 * Limpia el resaltado visual de errores previos en los inputs del formulario.
 */
function clearFieldErrorHighlights() {
    const inputs = dynamicFormContainer.querySelectorAll("input");
    inputs.forEach((inp) => inp.classList.remove("input-invalid"));
}

/**
 * Verifica la disponibilidad del servicio consumiendo GET /api/health.
 */
async function checkApiHealth() {
    const indicator = document.getElementById("healthIndicator");
    const text = document.getElementById("healthText");
    const baseUrl = getCleanApiBaseUrl();

    try {
        const response = await fetch(`${baseUrl}/api/health`, { method: "GET" });
        if (response.ok) {
            const data = await response.json();
            indicator.className = "status-dot dot-online";
            text.textContent = `API Activa (${data.status})`;
        } else {
            indicator.className = "status-dot dot-offline";
            text.textContent = `API respondió HTTP ${response.status}`;
        }
    } catch {
        indicator.className = "status-dot dot-offline";
        text.textContent = "Sin conexión (Verifique que Veridoc.API esté ejecutándose)";
    }
}

/**
 * Envía el payload JSON al endpoint POST /api/validations de Veridoc.API
 * y presenta en pantalla el resultado devuelto por el backend.
 */
async function sendValidationRequest() {
    clearFieldErrorHighlights();

    const baseUrl = getCleanApiBaseUrl();
    const apiKey = apiKeyInput.value.trim();

    if (currentEditMode === "form") {
        syncFormToRawJson();
    }

    const rawBody = rawJsonTextarea.value;
    const startTime = performance.now();

    try {
        const headers = {
            "Content-Type": "application/json"
        };

        if (apiKey !== "") {
            headers["X-Api-Key"] = apiKey;
        }

        const response = await fetch(`${baseUrl}/api/validations`, {
            method: "POST",
            headers: headers,
            body: rawBody
        });

        const elapsedMs = Math.round(performance.now() - startTime);
        const correlationId = response.headers.get("X-Correlation-Id") || "-";

        let responseData = {};
        try {
            responseData = await response.json();
        } catch {
            responseData = { message: "La respuesta no contenía un cuerpo JSON." };
        }

        renderApiResponse(response.status, correlationId, elapsedMs, responseData);
    } catch (networkError) {
        renderNetworkFailure(networkError);
    }
}

/**
 * Renderiza en la columna derecha el resultado de validación o error técnico/protocolo devuelto por la API.
 * @param {number} statusCode Código de estado HTTP devuelto.
 * @param {string} correlationId Identificador de correlación de la petición.
 * @param {number} elapsedMs Tiempo de respuesta en milisegundos.
 * @param {Object} data Objeto JSON devuelto por Veridoc.API.
 */
function renderApiResponse(statusCode, correlationId, elapsedMs, data) {
    const httpMetaBar = document.getElementById("httpMetaBar");
    const httpStatusBadge = document.getElementById("httpStatusBadge");
    const correlationIdText = document.getElementById("correlationIdText");
    const elapsedTimeText = document.getElementById("elapsedTimeText");
    const resultCard = document.getElementById("resultSummaryCard");
    const resultTitle = document.getElementById("resultTitle");
    const resultDesc = document.getElementById("resultDescription");
    const errorsSection = document.getElementById("errorsSection");
    const errorsTableBody = document.getElementById("errorsTableBody");
    const errorCountText = document.getElementById("errorCountText");
    const responseJsonViewer = document.getElementById("responseJsonViewer");

    httpMetaBar.classList.remove("hidden");
    httpStatusBadge.textContent = `${statusCode}`;
    httpStatusBadge.className = `http-badge badge-${statusCode}`;
    correlationIdText.textContent = data.correlationId || correlationId;
    elapsedTimeText.textContent = `${elapsedMs} ms`;

    responseJsonViewer.textContent = JSON.stringify(data, null, 2);
    errorsTableBody.innerHTML = "";

    // Caso 1: Respuesta 200 OK de validación documental (ValidationResponseDto)
    if (statusCode === 200 && typeof data.isValid === "boolean") {
        if (data.isValid) {
            resultCard.className = "result-card result-valid";
            resultTitle.textContent = "DOCUMENTO VÁLIDO — Cumple todas las reglas";
            resultDesc.textContent = `El documento de tipo '${data.documentType}' superó satisfactoriamente todas las reglas en el backend.`;
            errorsSection.classList.add("hidden");
        } else {
            const errors = Array.isArray(data.errors) ? data.errors : [];
            resultCard.className = "result-card result-invalid";
            resultTitle.textContent = "DOCUMENTO NO VÁLIDO — Incumplimiento de reglas";
            resultDesc.textContent = `Se detectaron ${errors.length} error(es) o inconsistencia(s) al evaluar '${data.documentType || "desconocido"}'.`;

            errorCountText.textContent = String(errors.length);
            errorsSection.classList.remove("hidden");

            errors.forEach((err) => {
                const tr = document.createElement("tr");

                const tdField = document.createElement("td");
                const codeField = document.createElement("code");
                codeField.textContent = err.field || "-";
                tdField.appendChild(codeField);

                const tdCode = document.createElement("td");
                const codeRule = document.createElement("code");
                codeRule.textContent = err.code || "-";
                tdCode.appendChild(codeRule);

                const tdMsg = document.createElement("td");
                tdMsg.textContent = err.message || "-";

                tr.appendChild(tdField);
                tr.appendChild(tdCode);
                tr.appendChild(tdMsg);
                errorsTableBody.appendChild(tr);

                // Resaltar visualmente el campo correspondiente en el formulario si existe
                const inputEl = document.getElementById(`field_${err.field}`);
                if (inputEl) {
                    inputEl.classList.add("input-invalid");
                }
            });
        }
        return;
    }

    // Caso 2: Error de protocolo o técnico controlado (400 Bad Request, 401 Unauthorized, 500 Internal Server Error)
    errorsSection.classList.add("hidden");
    resultCard.className = "result-card result-technical-error";
    resultTitle.textContent = `Error HTTP ${statusCode} — ${data.errorCode || "ERROR_CONTROLADO"}`;
    resultDesc.textContent = data.message || "La solicitud fue rechazada por formato inválido, autenticación o error técnico.";
}

/**
 * Muestra un mensaje claro cuando el navegador no puede conectarse con el servidor backend.
 * @param {Error} error Objeto de error de red.
 */
function renderNetworkFailure(error) {
    const resultCard = document.getElementById("resultSummaryCard");
    const resultTitle = document.getElementById("resultTitle");
    const resultDesc = document.getElementById("resultDescription");
    const errorsSection = document.getElementById("errorsSection");

    errorsSection.classList.add("hidden");
    resultCard.className = "result-card result-technical-error";
    resultTitle.textContent = "No se pudo conectar con Veridoc.API";
    resultDesc.textContent = `Verifique que el proyecto Veridoc.API esté en ejecución en ${getCleanApiBaseUrl()}. Detalle: ${error.message}`;
}

/**
 * Consulta GET /api/validations/document-types y muestra el catálogo de reglas configuradas en el backend.
 */
async function loadRulesCatalog() {
    const auxPanel = document.getElementById("auxiliaryPanel");
    const auxTitle = document.getElementById("auxiliaryTitle");
    const auxContent = document.getElementById("auxiliaryContent");

    auxPanel.classList.remove("hidden");
    auxTitle.textContent = "Catálogo de Tipos Documentales y Reglas (GET /api/validations/document-types)";
    auxContent.textContent = "Cargando catálogo desde Veridoc.API...";

    try {
        const response = await fetch(`${getCleanApiBaseUrl()}/api/validations/document-types`, {
            headers: { "X-Api-Key": apiKeyInput.value.trim() }
        });
        const data = await response.json();

        if (!response.ok) {
            auxContent.textContent = data.message || `Error HTTP ${response.status}`;
            return;
        }

        auxContent.innerHTML = "";
        data.forEach((docType) => {
            const card = document.createElement("div");
            card.className = "aux-card";

            const title = document.createElement("strong");
            title.textContent = `${docType.displayName} (${docType.documentType}) — ${docType.rules.length} reglas:`;
            card.appendChild(title);

            const ul = document.createElement("ul");
            ul.style.marginTop = "0.4rem";
            ul.style.paddingLeft = "1.2rem";

            docType.rules.forEach((rule) => {
                const li = document.createElement("li");
                const code = document.createElement("code");
                code.textContent = rule.code;
                li.appendChild(code);
                li.appendChild(document.createTextNode(`: ${rule.description}`));
                ul.appendChild(li);
            });

            card.appendChild(ul);
            auxContent.appendChild(card);
        });
    } catch (err) {
        auxContent.textContent = `No se pudo consultar el catálogo: ${err.message}`;
    }
}

/**
 * Consulta GET /api/validations/history y muestra el historial de auditoría almacenado en PostgreSQL.
 */
async function loadValidationHistory() {
    const auxPanel = document.getElementById("auxiliaryPanel");
    const auxTitle = document.getElementById("auxiliaryTitle");
    const auxContent = document.getElementById("auxiliaryContent");

    auxPanel.classList.remove("hidden");
    auxTitle.textContent = "Historial Reciente en PostgreSQL (GET /api/validations/history)";
    auxContent.textContent = "Consultando PostgreSQL...";

    try {
        const response = await fetch(`${getCleanApiBaseUrl()}/api/validations/history?limit=10`, {
            headers: { "X-Api-Key": apiKeyInput.value.trim() }
        });
        const data = await response.json();

        if (!response.ok) {
            auxContent.textContent = `${data.errorCode || "Error"}: ${data.message || "PostgreSQL no disponible."}`;
            return;
        }

        if (!Array.isArray(data) || data.length === 0) {
            auxContent.textContent = "No hay validaciones registradas aún en PostgreSQL.";
            return;
        }

        auxContent.innerHTML = "";
        data.forEach((item) => {
            const card = document.createElement("div");
            card.className = "aux-card";
            const statusText = item.isValid ? "VÁLIDO" : `INVÁLIDO (${item.errorCount} errores)`;
            card.textContent = `#${item.id} | Tipo: ${item.documentType} | Resultado: ${statusText} | Fecha UTC: ${item.validatedAtUtc} | CorrelationId: ${item.correlationId}`;
            auxContent.appendChild(card);
        });
    } catch (err) {
        auxContent.textContent = `Error de red al consultar historial: ${err.message}`;
    }
}

// ============================================================================
// Inicialización de Eventos al cargar la página
// ============================================================================
document.addEventListener("DOMContentLoaded", () => {
    // Cargar inicialmente el caso de Cédula Válida
    renderFormFields("cedula_identidad", PRESET_CASES.cedula_valida.document);

    documentTypeSelect.addEventListener("change", () => {
        clearFieldErrorHighlights();
        renderFormFields(documentTypeSelect.value);
    });

    tabFormMode.addEventListener("click", () => switchEditMode("form"));
    tabJsonMode.addEventListener("click", () => switchEditMode("json"));

    document.querySelectorAll(".btn-preset").forEach((btn) => {
        btn.addEventListener("click", () => {
            clearFieldErrorHighlights();
            const presetKey = btn.getAttribute("data-preset");

            if (presetKey === "json_malformado") {
                switchEditMode("json");
                rawJsonTextarea.value = '{\n  "documentType": "cedula_identidad",\n  "document": {\n    "numeroCedula": "8-888-8888"\n  /* JSON ROTO SIN CIERRE */';
                return;
            }

            const preset = PRESET_CASES[presetKey];
            if (preset) {
                documentTypeSelect.value = preset.documentType;
                switchEditMode("form");
                renderFormFields(preset.documentType, preset.document);
            }
        });
    });

    document.getElementById("btnCheckHealth").addEventListener("click", checkApiHealth);
    document.getElementById("btnValidate").addEventListener("click", sendValidationRequest);
    document.getElementById("btnLoadCatalog").addEventListener("click", loadRulesCatalog);
    document.getElementById("btnLoadHistory").addEventListener("click", loadValidationHistory);
});
