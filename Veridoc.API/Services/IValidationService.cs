using Veridoc.API.DTOs;

namespace Veridoc.API.Services;

/// <summary>
/// Contrato del servicio de negocio encargado de coordinar la validación de documentos
/// y consultar los tipos documentales soportados.
/// </summary>
public interface IValidationService
{
    /// <summary>
    /// Procesa una solicitud de validación identificando el tipo documental y ejecutando sus reglas asociadas.
    /// </summary>
    /// <param name="request">Datos de entrada con el tipo de documento y su estructura JSON.</param>
    /// <param name="correlationId">Identificador de correlación de la petición para trazabilidad.</param>
    /// <param name="cancellationToken">Token de cancelación de la operación asíncrona.</param>
    /// <returns>Resultado estructurado de la validación con la lista de errores detectados.</returns>
    Task<ValidationResponseDto> ValidateDocumentAsync(
        ValidationRequestDto request,
        string correlationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene el catálogo de todos los tipos de documentos soportados y sus reglas configuradas.
    /// </summary>
    /// <returns>Colección de metadatos de tipos documentales y sus reglas.</returns>
    IReadOnlyList<DocumentTypeMetadataDto> GetSupportedDocumentTypes();
}
