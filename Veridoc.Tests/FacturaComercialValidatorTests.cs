using System.Text.Json;
using Veridoc.API.DTOs;
using Veridoc.API.Validators;
using Xunit;

namespace Veridoc.Tests;

/// <summary>
/// Conjunto de pruebas unitarias para <see cref="FacturaComercialValidator"/>.
/// Verifica facturas válidas (con 7% ITBMS y exentas), campos obligatorios, formatos, límites de montos,
/// consistencia aritmética, tasa de impuesto y consistencia de fechas.
/// </summary>
public class FacturaComercialValidatorTests
{
    private static readonly DateOnly FixedReferenceDate = new(2026, 6, 15);
    private readonly FacturaComercialValidator _validator = new();

    /// <summary>
    /// Caso 15: Verifica que una factura comercial válida con 7% de ITBMS en USD sea aprobada sin errores.
    /// </summary>
    [Fact]
    public void Validate_ValidInvoiceWithSevenPercentItbms_ReturnsNoErrors()
    {
        const string json = """
        {
          "numeroFactura": "FAC-0001",
          "rucEmisor": "1556789-1-700123",
          "nombreCliente": "Distribuidora Istmeña S.A.",
          "fechaEmision": "2026-06-01",
          "fechaVencimiento": "2026-07-01",
          "subtotal": 200.00,
          "itbms": 14.00,
          "total": 214.00,
          "moneda": "USD"
        }
        """;

        using JsonDocument doc = JsonDocument.Parse(json);
        List<ValidationErrorDto> errors = _validator.Validate(doc.RootElement, FixedReferenceDate);

        Assert.Empty(errors);
    }

    /// <summary>
    /// Caso 16: Verifica que una factura comercial exenta de ITBMS (0.00) en PAB sea aprobada sin errores.
    /// </summary>
    [Fact]
    public void Validate_ValidTaxExemptInvoiceInPab_ReturnsNoErrors()
    {
        const string json = """
        {
          "numeroFactura": "FAC-202605",
          "rucEmisor": "8-888-8888 DV 45",
          "nombreCliente": "Cooperativa Agropecuaria R.L.",
          "fechaEmision": "2026-06-10",
          "fechaVencimiento": "2026-06-10",
          "subtotal": 150.00,
          "itbms": 0.00,
          "total": 150.00,
          "moneda": "PAB"
        }
        """;

        using JsonDocument doc = JsonDocument.Parse(json);
        List<ValidationErrorDto> errors = _validator.Validate(doc.RootElement, FixedReferenceDate);

        Assert.Empty(errors);
    }

    /// <summary>
    /// Caso 17: Verifica que la ausencia de un campo de texto obligatorio (numeroFactura) reporte REQUIRED_FIELD.
    /// </summary>
    [Fact]
    public void Validate_MissingNumeroFactura_ReturnsRequiredFieldError()
    {
        const string json = """
        {
          "numeroFactura": "   ",
          "rucEmisor": "1556789-1-700123",
          "nombreCliente": "Cliente Ejemplo S.A.",
          "fechaEmision": "2026-06-01",
          "fechaVencimiento": "2026-07-01",
          "subtotal": 100.00,
          "itbms": 7.00,
          "total": 107.00,
          "moneda": "USD"
        }
        """;

        using JsonDocument doc = JsonDocument.Parse(json);
        List<ValidationErrorDto> errors = _validator.Validate(doc.RootElement, FixedReferenceDate);

        Assert.Contains(errors, e => e.Field == "numeroFactura" && e.Code == "REQUIRED_FIELD");
    }

    /// <summary>
    /// Caso 18: Verifica que la ausencia de un campo numérico obligatorio (subtotal) reporte REQUIRED_FIELD.
    /// </summary>
    [Fact]
    public void Validate_MissingNumericSubtotal_ReturnsRequiredFieldError()
    {
        const string json = """
        {
          "numeroFactura": "FAC-0010",
          "rucEmisor": "1556789-1-700123",
          "nombreCliente": "Cliente Ejemplo S.A.",
          "fechaEmision": "2026-06-01",
          "fechaVencimiento": "2026-07-01",
          "itbms": 7.00,
          "total": 107.00,
          "moneda": "USD"
        }
        """;

        using JsonDocument doc = JsonDocument.Parse(json);
        List<ValidationErrorDto> errors = _validator.Validate(doc.RootElement, FixedReferenceDate);

        Assert.Contains(errors, e => e.Field == "subtotal" && e.Code == "REQUIRED_FIELD");
    }

    /// <summary>
    /// Casos 19 a 21: Verifica formatos inválidos en numeroFactura, rucEmisor y moneda.
    /// </summary>
    [Theory]
    [InlineData("0001", "1556789-1-700123", "USD", "numeroFactura")]
    [InlineData("FAC-0001", "RUC_SIN_GUIONES", "USD", "rucEmisor")]
    [InlineData("FAC-0001", "1556789-1-700123", "EUR", "moneda")]
    public void Validate_InvalidFieldFormats_ReturnsInvalidFormatError(
        string numeroFactura,
        string rucEmisor,
        string moneda,
        string expectedErrorField)
    {
        string json = $$"""
        {
          "numeroFactura": "{{numeroFactura}}",
          "rucEmisor": "{{rucEmisor}}",
          "nombreCliente": "Cliente Ejemplo S.A.",
          "fechaEmision": "2026-06-01",
          "fechaVencimiento": "2026-07-01",
          "subtotal": 100.00,
          "itbms": 7.00,
          "total": 107.00,
          "moneda": "{{moneda}}"
        }
        """;

        using JsonDocument doc = JsonDocument.Parse(json);
        List<ValidationErrorDto> errors = _validator.Validate(doc.RootElement, FixedReferenceDate);

        Assert.Contains(errors, e => e.Field == expectedErrorField && e.Code == "INVALID_FORMAT");
    }

    /// <summary>
    /// Caso 22: Verifica que si un campo numérico recibe texto ("cien") en lugar de número, reporte INVALID_FORMAT sin lanzar excepción.
    /// </summary>
    [Fact]
    public void Validate_StringInsteadOfDecimalInSubtotal_ReturnsInvalidFormatError()
    {
        const string json = """
        {
          "numeroFactura": "FAC-0001",
          "rucEmisor": "1556789-1-700123",
          "nombreCliente": "Cliente Ejemplo S.A.",
          "fechaEmision": "2026-06-01",
          "fechaVencimiento": "2026-07-01",
          "subtotal": "cien",
          "itbms": 7.00,
          "total": 107.00,
          "moneda": "USD"
        }
        """;

        using JsonDocument doc = JsonDocument.Parse(json);
        List<ValidationErrorDto> errors = _validator.Validate(doc.RootElement, FixedReferenceDate);

        Assert.Contains(errors, e => e.Field == "subtotal" && e.Code == "INVALID_FORMAT");
    }

    /// <summary>
    /// Casos 23 y 24: Verifica que montos fuera de límites (subtotal menor o igual a 0, o itbms menor a 0) reporten OUT_OF_RANGE_AMOUNT.
    /// </summary>
    [Theory]
    [InlineData(0.00, 0.00, 10.00, "subtotal")]
    [InlineData(100.00, -5.00, 95.00, "itbms")]
    public void Validate_OutOfRangeAmounts_ReturnsOutOfRangeAmountError(
        decimal subtotal,
        decimal itbms,
        decimal total,
        string expectedField)
    {
        string json = $$"""
        {
          "numeroFactura": "FAC-0001",
          "rucEmisor": "1556789-1-700123",
          "nombreCliente": "Cliente Ejemplo S.A.",
          "fechaEmision": "2026-06-01",
          "fechaVencimiento": "2026-07-01",
          "subtotal": {{subtotal.ToString(System.Globalization.CultureInfo.InvariantCulture)}},
          "itbms": {{itbms.ToString(System.Globalization.CultureInfo.InvariantCulture)}},
          "total": {{total.ToString(System.Globalization.CultureInfo.InvariantCulture)}},
          "moneda": "USD"
        }
        """;

        using JsonDocument doc = JsonDocument.Parse(json);
        List<ValidationErrorDto> errors = _validator.Validate(doc.RootElement, FixedReferenceDate);

        Assert.Contains(errors, e => e.Field == expectedField && e.Code == "OUT_OF_RANGE_AMOUNT");
    }

    /// <summary>
    /// Caso 25: Verifica que una diferencia aritmética entre subtotal + itbms y total reporte INCONSISTENT_TOTAL.
    /// </summary>
    [Fact]
    public void Validate_InconsistentTotal_ReturnsInconsistentTotalError()
    {
        const string json = """
        {
          "numeroFactura": "FAC-0001",
          "rucEmisor": "1556789-1-700123",
          "nombreCliente": "Cliente Ejemplo S.A.",
          "fechaEmision": "2026-06-01",
          "fechaVencimiento": "2026-07-01",
          "subtotal": 100.00,
          "itbms": 7.00,
          "total": 120.00,
          "moneda": "USD"
        }
        """;

        using JsonDocument doc = JsonDocument.Parse(json);
        List<ValidationErrorDto> errors = _validator.Validate(doc.RootElement, FixedReferenceDate);

        Assert.Contains(errors, e => e.Field == "total" && e.Code == "INCONSISTENT_TOTAL");
    }

    /// <summary>
    /// Caso 26: Verifica que un ITBMS que no corresponde ni al 0% ni al 7% del subtotal reporte INCONSISTENT_TAX_RATE.
    /// </summary>
    [Fact]
    public void Validate_InconsistentTaxRate_ReturnsInconsistentTaxRateError()
    {
        const string json = """
        {
          "numeroFactura": "FAC-0001",
          "rucEmisor": "1556789-1-700123",
          "nombreCliente": "Cliente Ejemplo S.A.",
          "fechaEmision": "2026-06-01",
          "fechaVencimiento": "2026-07-01",
          "subtotal": 100.00,
          "itbms": 15.00,
          "total": 115.00,
          "moneda": "USD"
        }
        """;

        using JsonDocument doc = JsonDocument.Parse(json);
        List<ValidationErrorDto> errors = _validator.Validate(doc.RootElement, FixedReferenceDate);

        Assert.Contains(errors, e => e.Field == "itbms" && e.Code == "INCONSISTENT_TAX_RATE");
    }

    /// <summary>
    /// Casos 27 y 28: Verifica que fechaVencimiento anterior a fechaEmision o fechaEmision futura reporten INCONSISTENT_INVOICE_DATES.
    /// </summary>
    [Theory]
    [InlineData("2026-06-10", "2026-06-01", "fechaVencimiento")]
    [InlineData("2026-12-01", "2026-12-15", "fechaEmision")]
    public void Validate_InconsistentInvoiceDates_ReturnsInconsistentInvoiceDatesError(
        string fechaEmision,
        string fechaVencimiento,
        string expectedField)
    {
        string json = $$"""
        {
          "numeroFactura": "FAC-0001",
          "rucEmisor": "1556789-1-700123",
          "nombreCliente": "Cliente Ejemplo S.A.",
          "fechaEmision": "{{fechaEmision}}",
          "fechaVencimiento": "{{fechaVencimiento}}",
          "subtotal": 100.00,
          "itbms": 7.00,
          "total": 107.00,
          "moneda": "USD"
        }
        """;

        using JsonDocument doc = JsonDocument.Parse(json);
        List<ValidationErrorDto> errors = _validator.Validate(doc.RootElement, FixedReferenceDate);

        Assert.Contains(errors, e => e.Field == expectedField && e.Code == "INCONSISTENT_INVOICE_DATES");
    }
}
