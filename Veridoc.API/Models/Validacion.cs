namespace Veridoc.API.Models;

/// <summary>
/// Entidad de persistencia que registra el resultado de una ejecución de validación en PostgreSQL.
/// Cumple con el principio de minimización de datos (Ley 81 de 2019): almacena únicamente el resultado y metadatos,
/// sin guardar el JSON del documento completo ni datos personales de los titulares.
/// </summary>
public class Validacion
{
    /// <summary>
    /// Identificador único (clave primaria) del registro de validación.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Clave foránea opcional hacia el tipo de documento validado (puede ser nulo si el cliente envió un tipo no soportado).
    /// </summary>
    public int? TipoDocumentoId { get; set; }

    /// <summary>
    /// Código del tipo de documento solicitado en la validación.
    /// </summary>
    public string CodigoTipoDocumento { get; set; } = string.Empty;

    /// <summary>
    /// Indica si el documento superó todas las reglas (<c>true</c>) o presentó errores (<c>false</c>).
    /// </summary>
    public bool EsValido { get; set; }

    /// <summary>
    /// Cantidad total de errores o inconsistencias detectados en la ejecución.
    /// </summary>
    public int TotalErrores { get; set; }

    /// <summary>
    /// Identificador de correlación HTTP asociado a la solicitud para trazabilidad y auditoría.
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Fecha y hora UTC exacta en que se ejecutó la validación.
    /// </summary>
    public DateTime FechaValidacionUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Propiedad de navegación hacia el tipo de documento del catálogo.
    /// </summary>
    public TipoDocumento? TipoDocumento { get; set; }

    /// <summary>
    /// Colección de errores individuales detectados durante esta ejecución de validación.
    /// </summary>
    public List<DetalleError> DetallesError { get; set; } = new();
}
