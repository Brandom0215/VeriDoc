using System.Text.Json;
using System.Text.RegularExpressions;
using Veridoc.API.DTOs;

namespace Veridoc.API.Validators;

/// <summary>
/// Validador de reglas de negocio para el tipo documental Cédula de Identidad (<c>cedula_identidad</c>).
/// Evalúa obligatoriedad, formato panameño de cédula, mayoría de edad, coherencia cronológica y vigencia.
/// </summary>
public partial class CedulaIdentidadValidator : IDocumentValidator
{
    /// <summary>
    /// Expresión regular para validar el formato oficial de la cédula de identidad panameña:
    /// Provincias (1 a 13, con sufijo opcional AV o PI) o prefijos especiales (PE, E, N) seguidos de tomo y asiento.
    /// </summary>
    [GeneratedRegex(@"^(?:(?:[1-9]|1[0-3])(?:-(?:AV|PI))?-\d{1,5}-\d{1,6}|(?:PE|E|N)-\d{1,5}-\d{1,6})$", RegexOptions.CultureInvariant)]
    private static partial Regex CedulaPanamenaRegex();

    private const int MinimumAdultAge = 18;
    private const int MaximumReasonableAge = 120;

    private static readonly IReadOnlyList<string> Fields = new[]
    {
        "numeroCedula",
        "nombreCompleto",
        "fechaNacimiento",
        "fechaExpedicion",
        "fechaExpiracion",
        "nacionalidad"
    };

    private static readonly IReadOnlyList<RuleMetadataDto> RulesCatalog = new[]
    {
        new RuleMetadataDto
        {
            Code = "REQUIRED_FIELD",
            Name = "Campos obligatorios de cédula",
            Description = "Todos los campos (numeroCedula, nombreCompleto, fechaNacimiento, fechaExpedicion, fechaExpiracion, nacionalidad) son obligatorios."
        },
        new RuleMetadataDto
        {
            Code = "INVALID_CEDULA_FORMAT",
            Name = "Formato de cédula panameña",
            Description = "El campo numeroCedula debe cumplir la nomenclatura panameña (ej. 8-888-8888, PE-12-345, E-8-123456, N-10-1234, 1-AV-12-345)."
        },
        new RuleMetadataDto
        {
            Code = "INVALID_BIRTH_DATE",
            Name = "Validez y coherencia de fecha de nacimiento",
            Description = "La fechaNacimiento debe tener formato YYYY-MM-DD, no ser futura y corresponder a una edad máxima de 120 años."
        },
        new RuleMetadataDto
        {
            Code = "UNDERAGE_HOLDER",
            Name = "Mayoría de edad del titular",
            Description = "El titular del documento debe tener al menos 18 años cumplidos a la fecha de validación."
        },
        new RuleMetadataDto
        {
            Code = "INCONSISTENT_ISSUE_EXPIRY_DATES",
            Name = "Consistencia entre fechas de nacimiento, expedición y expiración",
            Description = "Las fechas de expedición y expiración deben ser válidas (YYYY-MM-DD), fechaExpedicion debe ser posterior a fechaNacimiento (y no futura) y fechaExpiracion debe ser posterior a fechaExpedicion."
        },
        new RuleMetadataDto
        {
            Code = "DOCUMENT_EXPIRED",
            Name = "Vigencia del documento",
            Description = "La cédula no debe estar vencida (fechaExpiracion debe ser mayor o igual a la fecha actual)."
        }
    };

    /// <inheritdoc />
    public string DocumentType => "cedula_identidad";

    /// <inheritdoc />
    public string DisplayName => "Cédula de Identidad / Datos Personales";

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
    /// Evalúa un elemento JSON de Cédula de Identidad usando una fecha de referencia específica (útil para pruebas deterministas).
    /// </summary>
    /// <param name="documentElement">Objeto JSON del documento.</param>
    /// <param name="referenceDate">Fecha actual de referencia para el cálculo de edad y vigencia.</param>
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
                Message = "El nodo 'document' debe ser un objeto JSON con los campos de la cédula."
            });
            return errors;
        }

        // Regla 1: Extracción y verificación de campos obligatorios
        string? numeroCedula = ExtractRequiredString(documentElement, "numeroCedula", errors);
        string? nombreCompleto = ExtractRequiredString(documentElement, "nombreCompleto", errors);
        string? fechaNacimientoStr = ExtractRequiredString(documentElement, "fechaNacimiento", errors);
        string? fechaExpedicionStr = ExtractRequiredString(documentElement, "fechaExpedicion", errors);
        string? fechaExpiracionStr = ExtractRequiredString(documentElement, "fechaExpiracion", errors);
        string? nacionalidad = ExtractRequiredString(documentElement, "nacionalidad", errors);

        // Validación adicional de longitud mínima coherente para nombreCompleto y nacionalidad
        if (nombreCompleto is not null && nombreCompleto.Length < 3)
        {
            errors.Add(new ValidationErrorDto
            {
                Field = "nombreCompleto",
                Code = "INVALID_FORMAT",
                Message = "El campo 'nombreCompleto' debe contener al menos 3 caracteres."
            });
        }

        if (nacionalidad is not null && nacionalidad.Length < 3)
        {
            errors.Add(new ValidationErrorDto
            {
                Field = "nacionalidad",
                Code = "INVALID_FORMAT",
                Message = "El campo 'nacionalidad' debe contener al menos 3 caracteres."
            });
        }

        // Regla 2: Formato de cédula panameña
        if (numeroCedula is not null && !CedulaPanamenaRegex().IsMatch(numeroCedula))
        {
            errors.Add(new ValidationErrorDto
            {
                Field = "numeroCedula",
                Code = "INVALID_CEDULA_FORMAT",
                Message = "El formato de 'numeroCedula' no es válido. Use el formato panameño (ej. 8-888-8888, PE-12-345, E-8-123456, N-10-1234)."
            });
        }

        // Regla 3 y 4: Fecha de nacimiento válida, no futura, coherente y mayoría de edad (>= 18 años)
        bool hasValidBirthDate = false;
        DateOnly fechaNacimiento = default;

        if (fechaNacimientoStr is not null)
        {
            if (!JsonElementHelper.TryParseIsoDate(fechaNacimientoStr, out fechaNacimiento))
            {
                errors.Add(new ValidationErrorDto
                {
                    Field = "fechaNacimiento",
                    Code = "INVALID_BIRTH_DATE",
                    Message = "El campo 'fechaNacimiento' debe tener el formato válido YYYY-MM-DD."
                });
            }
            else if (fechaNacimiento > referenceDate)
            {
                errors.Add(new ValidationErrorDto
                {
                    Field = "fechaNacimiento",
                    Code = "INVALID_BIRTH_DATE",
                    Message = "La fecha de nacimiento no puede ser una fecha futura."
                });
            }
            else
            {
                int age = CalculateAge(fechaNacimiento, referenceDate);
                if (age > MaximumReasonableAge)
                {
                    errors.Add(new ValidationErrorDto
                    {
                        Field = "fechaNacimiento",
                        Code = "INVALID_BIRTH_DATE",
                        Message = $"La fecha de nacimiento excede la edad máxima permitida ({MaximumReasonableAge} años)."
                    });
                }
                else
                {
                    hasValidBirthDate = true;
                    if (age < MinimumAdultAge)
                    {
                        errors.Add(new ValidationErrorDto
                        {
                            Field = "fechaNacimiento",
                            Code = "UNDERAGE_HOLDER",
                            Message = $"El titular debe ser mayor de edad (mínimo {MinimumAdultAge} años cumplidos)."
                        });
                    }
                }
            }
        }

        // Regla 5 y 6: Consistencia de fechas de expedición/expiración y vigencia del documento
        bool hasValidIssueDate = false;
        DateOnly fechaExpedicion = default;

        if (fechaExpedicionStr is not null)
        {
            if (!JsonElementHelper.TryParseIsoDate(fechaExpedicionStr, out fechaExpedicion))
            {
                errors.Add(new ValidationErrorDto
                {
                    Field = "fechaExpedicion",
                    Code = "INCONSISTENT_ISSUE_EXPIRY_DATES",
                    Message = "El campo 'fechaExpedicion' debe tener el formato válido YYYY-MM-DD."
                });
            }
            else
            {
                hasValidIssueDate = true;

                if (fechaExpedicion > referenceDate)
                {
                    errors.Add(new ValidationErrorDto
                    {
                        Field = "fechaExpedicion",
                        Code = "INCONSISTENT_ISSUE_EXPIRY_DATES",
                        Message = "La fecha de expedición no puede ser posterior a la fecha actual."
                    });
                }

                if (hasValidBirthDate && fechaExpedicion <= fechaNacimiento)
                {
                    errors.Add(new ValidationErrorDto
                    {
                        Field = "fechaExpedicion",
                        Code = "INCONSISTENT_ISSUE_EXPIRY_DATES",
                        Message = "La fecha de expedición debe ser posterior a la fecha de nacimiento del titular."
                    });
                }
            }
        }

        if (fechaExpiracionStr is not null)
        {
            if (!JsonElementHelper.TryParseIsoDate(fechaExpiracionStr, out DateOnly fechaExpiracion))
            {
                errors.Add(new ValidationErrorDto
                {
                    Field = "fechaExpiracion",
                    Code = "INCONSISTENT_ISSUE_EXPIRY_DATES",
                    Message = "El campo 'fechaExpiracion' debe tener el formato válido YYYY-MM-DD."
                });
            }
            else
            {
                if (hasValidIssueDate && fechaExpiracion <= fechaExpedicion)
                {
                    errors.Add(new ValidationErrorDto
                    {
                        Field = "fechaExpiracion",
                        Code = "INCONSISTENT_ISSUE_EXPIRY_DATES",
                        Message = "La fecha de expiración debe ser estrictamente posterior a la fecha de expedición."
                    });
                }

                // Regla 6: Vigencia del documento
                if (fechaExpiracion < referenceDate)
                {
                    errors.Add(new ValidationErrorDto
                    {
                        Field = "fechaExpiracion",
                        Code = "DOCUMENT_EXPIRED",
                        Message = "El documento de identidad se encuentra vencido."
                    });
                }
            }
        }

        return errors;
    }

    /// <summary>
    /// Extrae un campo de texto obligatorio y registra el error correspondiente si falta o su tipo JSON no es String.
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
    /// Calcula la edad exacta en años cumplidos entre la fecha de nacimiento y la fecha de referencia.
    /// </summary>
    private static int CalculateAge(DateOnly birthDate, DateOnly referenceDate)
    {
        int age = referenceDate.Year - birthDate.Year;
        if (referenceDate < birthDate.AddYears(age))
        {
            age--;
        }

        return age;
    }
}
