using System.Text.Json;
using Veridoc.API.DTOs;
using Veridoc.API.Validators;
using Xunit;

namespace Veridoc.Tests;

/// <summary>
/// Conjunto de pruebas unitarias para <see cref="CedulaIdentidadValidator"/>.
/// Verifica documentos válidos, campos obligatorios, formatos de cédula panameña, límites de edad y consistencia de fechas.
/// </summary>
public class CedulaIdentidadValidatorTests
{
    private static readonly DateOnly FixedReferenceDate = new(2026, 6, 15);
    private readonly CedulaIdentidadValidator _validator = new();

    /// <summary>
    /// Casos 1 a 4: Verifica que distintos formatos válidos de cédula panameña (ordinaria, PE, E, AV)
    /// con datos coherentes sean aprobados sin errores.
    /// </summary>
    [Theory]
    [InlineData("8-888-8888")]
    [InlineData("PE-12-345")]
    [InlineData("E-8-123456")]
    [InlineData("1-AV-12-345")]
    public void Validate_ValidPanamanianCedulaFormats_ReturnsNoErrors(string numeroCedula)
    {
        string json = $$"""
        {
          "numeroCedula": "{{numeroCedula}}",
          "nombreCompleto": "Juan Carlos Pérez Gómez",
          "fechaNacimiento": "1995-05-15",
          "fechaExpedicion": "2022-06-01",
          "fechaExpiracion": "2032-06-01",
          "nacionalidad": "Panameña"
        }
        """;

        using JsonDocument doc = JsonDocument.Parse(json);
        List<ValidationErrorDto> errors = _validator.Validate(doc.RootElement, FixedReferenceDate);

        Assert.Empty(errors);
    }

    /// <summary>
    /// Casos 5 a 7: Verifica que la ausencia de cualquiera de los campos obligatorios reporte REQUIRED_FIELD.
    /// </summary>
    [Theory]
    [InlineData("numeroCedula")]
    [InlineData("nombreCompleto")]
    [InlineData("fechaNacimiento")]
    public void Validate_MissingRequiredField_ReturnsRequiredFieldError(string missingField)
    {
        var fields = new Dictionary<string, string>
        {
            ["numeroCedula"] = "8-888-8888",
            ["nombreCompleto"] = "Juan Carlos Pérez",
            ["fechaNacimiento"] = "1995-05-15",
            ["fechaExpedicion"] = "2022-06-01",
            ["fechaExpiracion"] = "2032-06-01",
            ["nacionalidad"] = "Panameña"
        };

        fields.Remove(missingField);
        string json = JsonSerializer.Serialize(fields);

        using JsonDocument doc = JsonDocument.Parse(json);
        List<ValidationErrorDto> errors = _validator.Validate(doc.RootElement, FixedReferenceDate);

        Assert.Contains(errors, e => e.Field == missingField && e.Code == "REQUIRED_FIELD");
    }

    /// <summary>
    /// Caso 8: Verifica que un formato de cédula inválido (provincia inexistente o sin guiones) reporte INVALID_CEDULA_FORMAT.
    /// </summary>
    [Fact]
    public void Validate_InvalidCedulaFormat_ReturnsInvalidCedulaFormatError()
    {
        const string json = """
        {
          "numeroCedula": "15-888-8888",
          "nombreCompleto": "María Rodríguez",
          "fechaNacimiento": "1990-03-10",
          "fechaExpedicion": "2021-01-15",
          "fechaExpiracion": "2031-01-15",
          "nacionalidad": "Panameña"
        }
        """;

        using JsonDocument doc = JsonDocument.Parse(json);
        List<ValidationErrorDto> errors = _validator.Validate(doc.RootElement, FixedReferenceDate);

        Assert.Contains(errors, e => e.Field == "numeroCedula" && e.Code == "INVALID_CEDULA_FORMAT");
    }

    /// <summary>
    /// Caso 9: Verifica que si un campo de texto recibe un tipo JSON numérico inesperado, devuelva INVALID_FORMAT de forma controlada.
    /// </summary>
    [Fact]
    public void Validate_UnexpectedJsonTypeInStringField_ReturnsInvalidFormatError()
    {
        const string json = """
        {
          "numeroCedula": 8888888,
          "nombreCompleto": "María Rodríguez",
          "fechaNacimiento": "1990-03-10",
          "fechaExpedicion": "2021-01-15",
          "fechaExpiracion": "2031-01-15",
          "nacionalidad": "Panameña"
        }
        """;

        using JsonDocument doc = JsonDocument.Parse(json);
        List<ValidationErrorDto> errors = _validator.Validate(doc.RootElement, FixedReferenceDate);

        Assert.Contains(errors, e => e.Field == "numeroCedula" && e.Code == "INVALID_FORMAT");
    }

    /// <summary>
    /// Caso 10: Verifica que una fecha de nacimiento con formato distinto de YYYY-MM-DD reporte INVALID_BIRTH_DATE.
    /// </summary>
    [Fact]
    public void Validate_InvalidBirthDateFormat_ReturnsInvalidBirthDateError()
    {
        const string json = """
        {
          "numeroCedula": "8-888-8888",
          "nombreCompleto": "Carlos Mendoza",
          "fechaNacimiento": "15/05/1995",
          "fechaExpedicion": "2021-01-15",
          "fechaExpiracion": "2031-01-15",
          "nacionalidad": "Panameña"
        }
        """;

        using JsonDocument doc = JsonDocument.Parse(json);
        List<ValidationErrorDto> errors = _validator.Validate(doc.RootElement, FixedReferenceDate);

        Assert.Contains(errors, e => e.Field == "fechaNacimiento" && e.Code == "INVALID_BIRTH_DATE");
    }

    /// <summary>
    /// Caso 11: Verifica que una fecha de nacimiento en el futuro reporte INVALID_BIRTH_DATE.
    /// </summary>
    [Fact]
    public void Validate_FutureBirthDate_ReturnsInvalidBirthDateError()
    {
        const string json = """
        {
          "numeroCedula": "8-888-8888",
          "nombreCompleto": "Carlos Mendoza",
          "fechaNacimiento": "2028-01-01",
          "fechaExpedicion": "2024-01-15",
          "fechaExpiracion": "2034-01-15",
          "nacionalidad": "Panameña"
        }
        """;

        using JsonDocument doc = JsonDocument.Parse(json);
        List<ValidationErrorDto> errors = _validator.Validate(doc.RootElement, FixedReferenceDate);

        Assert.Contains(errors, e => e.Field == "fechaNacimiento" && e.Code == "INVALID_BIRTH_DATE");
    }

    /// <summary>
    /// Caso 12: Verifica que una edad fuera del límite máximo razonable (> 120 años) reporte INVALID_BIRTH_DATE.
    /// </summary>
    [Fact]
    public void Validate_AgeExceedingMaximumLimit_ReturnsInvalidBirthDateError()
    {
        const string json = """
        {
          "numeroCedula": "8-888-8888",
          "nombreCompleto": "Carlos Mendoza",
          "fechaNacimiento": "1880-01-01",
          "fechaExpedicion": "2020-01-15",
          "fechaExpiracion": "2030-01-15",
          "nacionalidad": "Panameña"
        }
        """;

        using JsonDocument doc = JsonDocument.Parse(json);
        List<ValidationErrorDto> errors = _validator.Validate(doc.RootElement, FixedReferenceDate);

        Assert.Contains(errors, e => e.Field == "fechaNacimiento" && e.Code == "INVALID_BIRTH_DATE");
    }

    /// <summary>
    /// Caso 13: Verifica que un titular menor de 18 años reporte UNDERAGE_HOLDER.
    /// </summary>
    [Fact]
    public void Validate_UnderageHolder_ReturnsUnderageHolderError()
    {
        const string json = """
        {
          "numeroCedula": "8-888-8888",
          "nombreCompleto": "Sofía Castillo",
          "fechaNacimiento": "2012-09-10",
          "fechaExpedicion": "2024-01-15",
          "fechaExpiracion": "2029-01-15",
          "nacionalidad": "Panameña"
        }
        """;

        using JsonDocument doc = JsonDocument.Parse(json);
        List<ValidationErrorDto> errors = _validator.Validate(doc.RootElement, FixedReferenceDate);

        Assert.Contains(errors, e => e.Field == "fechaNacimiento" && e.Code == "UNDERAGE_HOLDER");
    }

    /// <summary>
    /// Caso 14: Verifica inconsistencia entre fechaExpedicion y fechaExpiracion junto con documento vencido.
    /// </summary>
    [Fact]
    public void Validate_ExpiredAndInconsistentIssueExpiryDates_ReturnsBothErrors()
    {
        const string json = """
        {
          "numeroCedula": "8-888-8888",
          "nombreCompleto": "Pedro Alvarado",
          "fechaNacimiento": "1985-04-20",
          "fechaExpedicion": "2024-05-01",
          "fechaExpiracion": "2023-05-01",
          "nacionalidad": "Panameña"
        }
        """;

        using JsonDocument doc = JsonDocument.Parse(json);
        List<ValidationErrorDto> errors = _validator.Validate(doc.RootElement, FixedReferenceDate);

        Assert.Contains(errors, e => e.Field == "fechaExpiracion" && e.Code == "INCONSISTENT_ISSUE_EXPIRY_DATES");
        Assert.Contains(errors, e => e.Field == "fechaExpiracion" && e.Code == "DOCUMENT_EXPIRED");
    }
}
