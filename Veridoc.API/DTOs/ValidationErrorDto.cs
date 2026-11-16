namespace Veridoc.API.DTOs;

/// <summary>
/// Representa un error o inconsistencia de negocio detectado al evaluar una regla sobre un campo del documento.
/// </summary>
public class ValidationErrorDto
{
    /// <summary>
    /// Nombre del campo del documento donde se detectó el incumplimiento (por ejemplo, "numeroCedula" o "total").
    /// </summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// Código identificador estandarizado de la regla incumplida (por ejemplo, "REQUIRED_FIELD" o "INCONSISTENT_TOTAL").
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Descripción legible para el usuario o sistema cliente explicando el motivo del incumplimiento.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
