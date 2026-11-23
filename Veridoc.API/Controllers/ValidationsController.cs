using Microsoft.AspNetCore.Mvc;
using Veridoc.API.DTOs;
using Veridoc.API.Services;

namespace Veridoc.API.Controllers;

/// <summary>
/// Controlador REST encargado de exponer los endpoints de validación documental, consulta de reglas soportadas
/// e historial de auditoría en PostgreSQL.
/// Mantiene responsabilidades delgadas delegando la lógica de negocio a <see cref="IValidationService"/>.
/// </summary>
[ApiController]
[Route("api/validations")]
[Produces("application/json")]
public class ValidationsController : ControllerBase
{
    private readonly IValidationService _validationService;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ValidationsController"/>.
    /// </summary>
    /// <param name="validationService">Servicio orquestador de validaciones documentales.</param>
    public ValidationsController(IValidationService validationService)
    {
        _validationService = validationService;
    }

    /// <summary>
    /// Evalúa un documento estructurado en JSON según las reglas correspondientes a su tipo documental.
    /// </summary>
    /// <param name="request">Objeto JSON con el tipo de documento (<c>documentType</c>) y sus campos (<c>document</c>).</param>
    /// <param name="cancellationToken">Token de cancelación de la petición HTTP.</param>
    /// <returns>
    /// Devuelve <c>200 OK</c> con <see cref="ValidationResponseDto"/> indicando si el documento es válido o detallando los errores de reglas.
    /// </returns>
    [HttpPost]
    [Consumes("application/json")]
    [RequestSizeLimit(65_536)]
    [ProducesResponseType(typeof(ValidationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ValidationResponseDto>> ValidateDocument(
        [FromBody] ValidationRequestDto? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new ApiErrorResponseDto
            {
                ErrorCode = "EMPTY_REQUEST_BODY",
                Message = "El cuerpo de la solicitud no puede estar vacío.",
                CorrelationId = HttpContext.TraceIdentifier,
                TimestampUtc = DateTime.UtcNow
            });
        }

        ValidationResponseDto result = await _validationService.ValidateDocumentAsync(
            request,
            HttpContext.TraceIdentifier,
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Obtiene el listado de los tipos de documentos soportados por el MVP junto con sus campos esperados y catálogo de reglas.
    /// </summary>
    /// <returns>Lista de tipos de documentos y reglas configuradas.</returns>
    [HttpGet("document-types")]
    [ProducesResponseType(typeof(IReadOnlyList<DocumentTypeMetadataDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status401Unauthorized)]
    public ActionResult<IReadOnlyList<DocumentTypeMetadataDto>> GetSupportedDocumentTypes()
    {
        IReadOnlyList<DocumentTypeMetadataDto> documentTypes = _validationService.GetSupportedDocumentTypes();
        return Ok(documentTypes);
    }

    /// <summary>
    /// Consulta el historial reciente de validaciones almacenado en PostgreSQL (sin datos personales de los documentos).
    /// </summary>
    /// <param name="limit">Cantidad máxima de registros a consultar (por defecto 20, máximo 100).</param>
    /// <param name="cancellationToken">Token de cancelación de la petición HTTP.</param>
    /// <returns>Lista de ejecuciones de validación registradas en la base de datos.</returns>
    [HttpGet("history")]
    [ProducesResponseType(typeof(IReadOnlyList<ValidationHistoryItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<ValidationHistoryItemDto>>> GetValidationHistory(
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ValidationHistoryItemDto> history = await _validationService.GetValidationHistoryAsync(
            limit,
            cancellationToken);

        return Ok(history);
    }
}
