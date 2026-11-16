namespace Veridoc.API.DTOs;

/// <summary>
/// Contrato de salida que devuelve el resultado de evaluar un documento contra las reglas configuradas.
/// </summary>
public class ValidationResponseDto
{
    /// <summary>
    /// Indica si el documento cumplió todas las reglas de validación (<c>true</c>) o si presentó algún incumplimiento (<c>false</c>).
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Identificador del tipo de documento que fue evaluado.
    /// </summary>
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>
    /// Fecha y hora en formato UTC en que se procesó la validación.
    /// </summary>
    public DateTime ValidatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Lista detallada de los errores o inconsistencias encontrados en el documento.
    /// Si <see cref="IsValid"/> es <c>true</c>, esta colección estará vacía.
    /// </summary>
    public List<ValidationErrorDto> Errors { get; set; } = new();
}
