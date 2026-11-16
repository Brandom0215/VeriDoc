namespace Veridoc.API.DTOs;

/// <summary>
/// Contrato de salida que representa un registro histórico de validación almacenado en PostgreSQL.
/// </summary>
public class ValidationHistoryItemDto
{
    /// <summary>
    /// Identificador único del registro en la base de datos.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Código del tipo de documento evaluado.
    /// </summary>
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>
    /// Indica si la validación resultó válida (<c>true</c>) o inválida (<c>false</c>).
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Número total de errores detectados en esa ejecución.
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// Identificador de correlación HTTP de la solicitud original.
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Fecha y hora UTC en que se procesó la validación.
    /// </summary>
    public DateTime ValidatedAtUtc { get; set; }

    /// <summary>
    /// Lista de errores registrados durante la validación.
    /// </summary>
    public List<ValidationErrorDto> Errors { get; set; } = new();
}
