using System.Text.Json;
using System.Text.RegularExpressions;
using Veridoc.API.DTOs;

namespace Veridoc.API.Validators;

/// <summary>
/// Validador de reglas de negocio para el tipo documental Factura Comercial (<c>factura_comercial</c>).
/// Evalúa campos obligatorios, formatos de numeración/RUC/moneda, límites de montos,
/// consistencia aritmética (<c>subtotal + itbms = total</c>), tasa de ITBMS (0% o 7%) y fechas.
/// </summary>
public partial class FacturaComercialValidator : IDocumentValidator
{
    /// <summary>
    /// Expresión regular para validar el número de factura comercial (ej. FAC-0001 hasta 10 dígitos).
    /// </summary>
    [GeneratedRegex(@"^FAC-\d{4,10}$", RegexOptions.CultureInvariant)]
    private static partial Regex NumeroFacturaRegex();

    /// <summary>
    /// Expresión regular para validar el formato de RUC panameño (dígitos/letras separados por guiones, con DV opcional).
    /// </summary>
    [GeneratedRegex(@"^[0-9A-Z]+(?:-[0-9A-Z]+){2,3}(?:\s*DV\s*\d{1,2})?$", RegexOptions.CultureInvariant)]
    private static partial Regex RucPanamenoRegex();

    private const decimal StandardItbmsRate = 0.07m;
    private const decimal RoundingTolerance = 0.01m;

    private static readonly HashSet<string> AllowedCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "USD",
        "PAB"
    };

    private static readonly IReadOnlyList<string> Fields = new[]
    {
        "numeroFactura",
        "rucEmisor",
        "nombreCliente",
        "fechaEmision",
        "fechaVencimiento",
        "subtotal",
        "itbms",
        "total",
        "moneda"
    };

    private static readonly IReadOnlyList<RuleMetadataDto> RulesCatalog = new[]
    {
        new RuleMetadataDto
        {
            Code = "REQUIRED_FIELD",
            Name = "Campos obligatorios de factura",
            Description = "Todos los campos (numeroFactura, rucEmisor, nombreCliente, fechaEmision, fechaVencimiento, subtotal, itbms, total, moneda) son obligatorios."
        },
        new RuleMetadataDto
        {
            Code = "INVALID_FORMAT",
            Name = "Formatos de factura, RUC y moneda",
            Description = "numeroFactura debe cumplir el patrón FAC-XXXX, rucEmisor debe tener estructura válida de RUC y moneda debe ser USD o PAB."
        },
        new RuleMetadataDto
        {
            Code = "OUT_OF_RANGE_AMOUNT",
            Name = "Límites de importes monetarios",
            Description = "Los montos subtotal y total deben ser mayores que cero (> 0), y el monto itbms debe ser mayor o igual a cero (>= 0)."
        },
        new RuleMetadataDto
        {
            Code = "INCONSISTENT_TOTAL",
            Name = "Consistencia aritmética del total",
            Description = "La suma exacta de subtotal + itbms debe coincidir con el monto total declarado."
        },
        new RuleMetadataDto
        {
            Code = "INCONSISTENT_TAX_RATE",
            Name = "Congruencia de tasa de ITBMS",
            Description = "El monto de itbms debe corresponder al 0% (exento) o al 7% del subtotal (con tolerancia de redondeo de ±0.01)."
        },
        new RuleMetadataDto
        {
            Code = "INCONSISTENT_INVOICE_DATES",
            Name = "Consistencia cronológica de fechas de factura",
            Description = "fechaEmision y fechaVencimiento deben tener formato YYYY-MM-DD, fechaEmision no puede ser futura y fechaVencimiento debe ser mayor o igual a fechaEmision."
        }
    };

    /// <inheritdoc />
    public string DocumentType => "factura_comercial";

    /// <inheritdoc />
    public string DisplayName => "Factura Comercial";

    /// <inheritdoc />
    public IReadOnlyList<string> ExpectedFields => Fields;

    /// <inheritdoc />
    public IReadOnlyList<RuleMetadataDto> GetRulesMetadata() => RulesCatalog;

    /// <inheritdoc />
    public List<ValidationErrorDto> Validate(JsonElement documentElement)
    {
        return Validate(documentElement, DateOnly.FromDateTime(DateTime.UtcNow));
    }

    /// <summary>
    /// Evalúa un elemento JSON de Factura Comercial utilizando una fecha de referencia específica.
    /// </summary>
    /// <param name="documentElement">Objeto JSON de la factura.</param>
    /// <param name="referenceDate">Fecha actual de referencia para validar fechaEmision.</param>
    /// <returns>Lista de errores de validación detectados.</returns>
    public List<ValidationErrorDto> Validate(JsonElement documentElement, DateOnly referenceDate)
    {
        var errors = new List<ValidationErrorDto>();

        if (documentElement.ValueKind != JsonValueKind.Object)
        {
            errors.Add(new ValidationErrorDto
            {
                Field = "document",
                Code = "REQUIRED_FIELD",
                Message = "El nodo 'document' debe ser un objeto JSON con los campos de la factura comercial."
            });
            return errors;
        }

        // Regla 1: Campos obligatorios (texto y numéricos)
        string? numeroFactura = ExtractRequiredString(documentElement, "numeroFactura", errors);
        string? rucEmisor = ExtractRequiredString(documentElement, "rucEmisor", errors);
        string? nombreCliente = ExtractRequiredString(documentElement, "nombreCliente", errors);
        string? fechaEmisionStr = ExtractRequiredString(documentElement, "fechaEmision", errors);
        string? fechaVencimientoStr = ExtractRequiredString(documentElement, "fechaVencimiento", errors);
        string? moneda = ExtractRequiredString(documentElement, "moneda", errors);

        bool hasSubtotal = TryExtractRequiredDecimal(documentElement, "subtotal", out decimal subtotal, errors);
        bool hasItbms = TryExtractRequiredDecimal(documentElement, "itbms", out decimal itbms, errors);
        bool hasTotal = TryExtractRequiredDecimal(documentElement, "total", out decimal total, errors);

        // Regla 2: Validaciones de formato (numeroFactura, rucEmisor, nombreCliente, moneda)
        if (numeroFactura is not null && !NumeroFacturaRegex().IsMatch(numeroFactura))
        {
            errors.Add(new ValidationErrorDto
            {
                Field = "numeroFactura",
                Code = "INVALID_FORMAT",
                Message = "El campo 'numeroFactura' debe seguir el formato 'FAC-XXXX' (por ejemplo, 'FAC-0001')."
            });
        }

        if (rucEmisor is not null && (rucEmisor.Length < 5 || rucEmisor.Length > 30 || !RucPanamenoRegex().IsMatch(rucEmisor)))
        {
            errors.Add(new ValidationErrorDto
            {
                Field = "rucEmisor",
                Code = "INVALID_FORMAT",
                Message = "El campo 'rucEmisor' no tiene un formato de RUC válido (ejemplo válido: '1556789-1-700123' o '8-888-8888')."
            });
        }

        if (nombreCliente is not null && nombreCliente.Length < 3)
        {
            errors.Add(new ValidationErrorDto
            {
                Field = "nombreCliente",
                Code = "INVALID_FORMAT",
                Message = "El campo 'nombreCliente' debe contener al menos 3 caracteres."
            });
        }

        if (moneda is not null && !AllowedCurrencies.Contains(moneda))
        {
            errors.Add(new ValidationErrorDto
            {
                Field = "moneda",
                Code = "INVALID_FORMAT",
                Message = "El campo 'moneda' solo admite los códigos 'USD' o 'PAB'."
            });
        }

        // Regla 3: Límites de montos (subtotal > 0, itbms >= 0, total > 0)
        bool amountsWithinBounds = true;

        if (hasSubtotal && subtotal <= 0m)
        {
            amountsWithinBounds = false;
            errors.Add(new ValidationErrorDto
            {
                Field = "subtotal",
                Code = "OUT_OF_RANGE_AMOUNT",
                Message = "El campo 'subtotal' debe ser estrictamente mayor que cero (0)."
            });
        }

        if (hasItbms && itbms < 0m)
        {
            amountsWithinBounds = false;
            errors.Add(new ValidationErrorDto
            {
                Field = "itbms",
                Code = "OUT_OF_RANGE_AMOUNT",
                Message = "El campo 'itbms' no puede ser un valor negativo."
            });
        }

        if (hasTotal && total <= 0m)
        {
            amountsWithinBounds = false;
            errors.Add(new ValidationErrorDto
            {
                Field = "total",
                Code = "OUT_OF_RANGE_AMOUNT",
                Message = "El campo 'total' debe ser estrictamente mayor que cero (0)."
            });
        }

        // Regla 4 y 5: Consistencia aritmética (subtotal + itbms == total) y congruencia de ITBMS (0% o 7%)
        if (hasSubtotal && hasItbms && hasTotal && amountsWithinBounds)
        {
            decimal expectedTotal = Math.Round(subtotal + itbms, 2, MidpointRounding.AwayFromZero);
            decimal roundedTotal = Math.Round(total, 2, MidpointRounding.AwayFromZero);

            if (expectedTotal != roundedTotal)
            {
                errors.Add(new ValidationErrorDto
                {
                    Field = "total",
                    Code = "INCONSISTENT_TOTAL",
                    Message = $"El monto 'total' ({roundedTotal:F2}) no coincide con la suma de 'subtotal' + 'itbms' ({expectedTotal:F2})."
                });
            }

            decimal expectedSevenPercent = Math.Round(subtotal * StandardItbmsRate, 2, MidpointRounding.AwayFromZero);
            bool isTaxExempt = itbms == 0m;
            bool isSevenPercent = Math.Abs(itbms - expectedSevenPercent) <= RoundingTolerance;

            if (!isTaxExempt && !isSevenPercent)
            {
                errors.Add(new ValidationErrorDto
                {
                    Field = "itbms",
                    Code = "INCONSISTENT_TAX_RATE",
                    Message = $"El campo 'itbms' ({itbms:F2}) debe ser 0.00 (exento) o corresponder al 7% del subtotal ({expectedSevenPercent:F2})."
                });
            }
        }

        // Regla 6: Consistencia de fechas de emisión y vencimiento
        bool hasValidIssueDate = false;
        DateOnly fechaEmision = default;

        if (fechaEmisionStr is not null)
        {
            if (!JsonElementHelper.TryParseIsoDate(fechaEmisionStr, out fechaEmision))
            {
                errors.Add(new ValidationErrorDto
                {
                    Field = "fechaEmision",
                    Code = "INVALID_FORMAT",
                    Message = "El campo 'fechaEmision' debe tener el formato válido YYYY-MM-DD."
                });
            }
            else
            {
                hasValidIssueDate = true;
                if (fechaEmision > referenceDate)
                {
                    errors.Add(new ValidationErrorDto
                    {
                        Field = "fechaEmision",
                        Code = "INCONSISTENT_INVOICE_DATES",
                        Message = "La fecha de emisión de la factura no puede ser una fecha futura."
                    });
                }
            }
        }

        if (fechaVencimientoStr is not null)
        {
            if (!JsonElementHelper.TryParseIsoDate(fechaVencimientoStr, out DateOnly fechaVencimiento))
            {
                errors.Add(new ValidationErrorDto
                {
                    Field = "fechaVencimiento",
                    Code = "INVALID_FORMAT",
                    Message = "El campo 'fechaVencimiento' debe tener el formato válido YYYY-MM-DD."
                });
            }
            else if (hasValidIssueDate && fechaVencimiento < fechaEmision)
            {
                errors.Add(new ValidationErrorDto
                {
                    Field = "fechaVencimiento",
                    Code = "INCONSISTENT_INVOICE_DATES",
                    Message = "La fecha de vencimiento debe ser mayor o igual a la fecha de emisión."
                });
            }
        }

        return errors;
    }

    /// <summary>
    /// Extrae un campo de texto obligatorio y registra error si está ausente o no es de tipo String.
    /// </summary>
    private static string? ExtractRequiredString(
        JsonElement element,
        string fieldName,
        List<ValidationErrorDto> errors)
    {
        if (JsonElementHelper.TryGetNonEmptyString(element, fieldName, out string? value, out bool isInvalidType))
        {
            return value;
        }

        if (isInvalidType)
        {
            errors.Add(new ValidationErrorDto
            {
                Field = fieldName,
                Code = "INVALID_FORMAT",
                Message = $"El campo '{fieldName}' debe ser una cadena de texto válida."
            });
            return null;
        }

        errors.Add(new ValidationErrorDto
        {
            Field = fieldName,
            Code = "REQUIRED_FIELD",
            Message = $"El campo '{fieldName}' es obligatorio y no puede estar vacío."
        });
        return null;
    }

    /// <summary>
    /// Extrae un campo decimal obligatorio y registra error si está ausente o no es un número válido.
    /// </summary>
    private static bool TryExtractRequiredDecimal(
        JsonElement element,
        string fieldName,
        out decimal value,
        List<ValidationErrorDto> errors)
    {
        if (JsonElementHelper.TryGetDecimal(element, fieldName, out value, out bool isInvalidType))
        {
            return true;
        }

        if (isInvalidType)
        {
            errors.Add(new ValidationErrorDto
            {
                Field = fieldName,
                Code = "INVALID_FORMAT",
                Message = $"El campo '{fieldName}' debe ser un valor numérico decimal válido."
            });
            return false;
        }

        errors.Add(new ValidationErrorDto
        {
            Field = fieldName,
            Code = "REQUIRED_FIELD",
            Message = $"El campo numérico '{fieldName}' es obligatorio."
        });
        return false;
    }
}
