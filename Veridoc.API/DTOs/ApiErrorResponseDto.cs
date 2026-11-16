namespace Veridoc.API.DTOs;

/// <summary>
/// Contrato estándar para respuestas de error técnico (500), autenticación (401) o solicitudes mal formadas (400).
/// Permite distinguir fallos de protocolo/infraestructura de los resultados normales de validación documental.
/// </summary>
public class ApiErrorResponseDto
{
    /// <summary>
    /// Código identificador del tipo de error técnico o de solicitud (por ejemplo, "INVALID_JSON_FORMAT", "UNAUTHORIZED", "INTERNAL_SERVER_ERROR").
    /// </summary>
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>
    /// Mensaje claro y seguro que describe el problema ocurrido sin exponer información interna ni trazas sensibles.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Identificador único de correlación de la solicitud HTTP para facilitar el diagnóstico en los registros del servidor.
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Fecha y hora UTC en la que se registró el error.
    /// </summary>
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
