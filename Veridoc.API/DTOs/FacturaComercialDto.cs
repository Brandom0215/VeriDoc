namespace Veridoc.API.DTOs;

/// <summary>
/// Modelo de transporte que representa los datos extraídos de una Factura Comercial (<c>factura_comercial</c>).
/// </summary>
public class FacturaComercialDto
{
    /// <summary>
    /// Número único de la factura en formato <c>FAC-XXXX</c> (por ejemplo, "FAC-0001").
    /// </summary>
    public string? NumeroFactura { get; set; }

    /// <summary>
    /// Registro Único de Contribuyente (RUC) de la entidad emisora de la factura.
    /// </summary>
    public string? RucEmisor { get; set; }

    /// <summary>
    /// Nombre o razón social del cliente receptor de la factura.
    /// </summary>
    public string? NombreCliente { get; set; }

    /// <summary>
    /// Fecha de emisión de la factura en formato ISO <c>YYYY-MM-DD</c>.
    /// </summary>
    public string? FechaEmision { get; set; }

    /// <summary>
    /// Fecha de vencimiento de pago de la factura en formato ISO <c>YYYY-MM-DD</c>.
    /// </summary>
    public string? FechaVencimiento { get; set; }

    /// <summary>
    /// Monto subtotal antes de impuestos.
    /// </summary>
    public decimal? Subtotal { get; set; }

    /// <summary>
    /// Monto del Impuesto de Transferencia de Bienes Corporales Muebles y la Prestación de Servicios (ITBMS).
    /// </summary>
    public decimal? Itbms { get; set; }

    /// <summary>
    /// Monto total a pagar (<c>Subtotal + Itbms</c>).
    /// </summary>
    public decimal? Total { get; set; }

    /// <summary>
    /// Código de moneda de la operación (<c>USD</c> o <c>PAB</c>).
    /// </summary>
    public string? Moneda { get; set; }
}
