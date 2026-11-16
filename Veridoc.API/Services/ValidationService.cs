using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Veridoc.API.Data;
using Veridoc.API.DTOs;
using Veridoc.API.Models;
using Veridoc.API.Validators;

namespace Veridoc.API.Services;

/// <summary>
/// Implementación del servicio orquestador de validaciones documentales.
/// Selecciona dinámicamente el validador correspondiente al tipo de documento, ejecuta todas sus reglas
/// y persiste el historial de auditoría en PostgreSQL respetando la minimización de datos (Ley 81 de 2019).
/// </summary>
public class ValidationService : IValidationService
{
    private readonly Dictionary<string, IDocumentValidator> _validatorsByType;
    private readonly ILogger<ValidationService> _logger;
    private readonly VeridocDbContext? _dbContext;
    private readonly bool _requirePersistence;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ValidationService"/>.
    /// </summary>
    /// <param name="validators">Colección de validadores documentales registrados en el contenedor de dependencias.</param>
    /// <param name="logger">Servicio de registro de eventos de validación (sin almacenar datos sensibles).</param>
    /// <param name="dbContext">Contexto opcional de Entity Framework Core para persistencia en PostgreSQL.</param>
    /// <param name="configuration">Configuración de la aplicación para determinar políticas de persistencia.</param>
    public ValidationService(
        IEnumerable<IDocumentValidator> validators,
        ILogger<ValidationService> logger,
        VeridocDbContext? dbContext = null,
        IConfiguration? configuration = null)
    {
        _validatorsByType = validators.ToDictionary(
            v => v.DocumentType,
            v => v,
            StringComparer.OrdinalIgnoreCase);
        _logger = logger;
        _dbContext = dbContext;

        // Si RequirePersistence es true (valor por defecto cuando se inyecta un DbContext en pruebas de persistencia),
        // un fallo de conexión a PostgreSQL lanzará excepción para devolver HTTP 500 controlado.
        _requirePersistence = configuration?.GetValue<bool>("Database:RequirePersistence", false) ?? (dbContext is not null);
    }

    /// <inheritdoc />
    public async Task<ValidationResponseDto> ValidateDocumentAsync(
        ValidationRequestDto request,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        // 1. Verificar que se haya indicado el tipo de documento
        if (string.IsNullOrWhiteSpace(request.DocumentType))
        {
            return new ValidationResponseDto
            {
                IsValid = false,
                DocumentType = string.Empty,
                ValidatedAtUtc = DateTime.UtcNow,
                Errors = new List<ValidationErrorDto>
                {
                    new()
                    {
                        Field = "documentType",
                        Code = "REQUIRED_FIELD",
                        Message = "El campo 'documentType' es obligatorio para identificar las reglas a ejecutar."
                    }
                }
            };
        }

        string normalizedType = request.DocumentType.Trim().ToLowerInvariant();

        // 2. Comprobar que el tipo de documento esté soportado en el MVP
        if (!_validatorsByType.TryGetValue(normalizedType, out IDocumentValidator? validator))
        {
            string supportedList = string.Join(", ", _validatorsByType.Keys);
            var unsupportedResponse = new ValidationResponseDto
            {
                IsValid = false,
                DocumentType = normalizedType,
                ValidatedAtUtc = DateTime.UtcNow,
                Errors = new List<ValidationErrorDto>
                {
                    new()
                    {
                        Field = "documentType",
                        Code = "UNSUPPORTED_DOCUMENT_TYPE",
                        Message = $"El tipo de documento '{request.DocumentType}' no está soportado. Tipos válidos: {supportedList}."
                    }
                }
            };

            await TryPersistValidationResultAsync(unsupportedResponse, correlationId, cancellationToken);
            return unsupportedResponse;
        }

        // 3. Verificar que el nodo 'document' exista y sea un objeto JSON
        if (request.Document is null ||
            request.Document.Value.ValueKind == JsonValueKind.Undefined ||
            request.Document.Value.ValueKind == JsonValueKind.Null ||
            request.Document.Value.ValueKind != JsonValueKind.Object)
        {
            var missingDocumentResponse = new ValidationResponseDto
            {
                IsValid = false,
                DocumentType = validator.DocumentType,
                ValidatedAtUtc = DateTime.UtcNow,
                Errors = new List<ValidationErrorDto>
                {
                    new()
                    {
                        Field = "document",
                        Code = "REQUIRED_FIELD",
                        Message = "El campo 'document' es obligatorio y debe ser un objeto JSON con los datos del documento."
                    }
                }
            };

            await TryPersistValidationResultAsync(missingDocumentResponse, correlationId, cancellationToken);
            return missingDocumentResponse;
        }

        // 4. Ejecutar todas las reglas del validador correspondiente
        List<ValidationErrorDto> errors = validator.Validate(request.Document.Value);
        bool isValid = errors.Count == 0;

        // Registro de observabilidad sin datos personales (Ley 81 de 2019)
        _logger.LogInformation(
            "Validación completada para tipo '{DocumentType}'. Resultado IsValid={IsValid}, Errores={ErrorCount}, CorrelationId={CorrelationId}",
            validator.DocumentType,
            isValid,
            errors.Count,
            correlationId);

        var response = new ValidationResponseDto
        {
            IsValid = isValid,
            DocumentType = validator.DocumentType,
            ValidatedAtUtc = DateTime.UtcNow,
            Errors = errors
        };

        // 5. Persistir el resultado de auditoría en PostgreSQL (sin guardar datos personales del documento)
        await TryPersistValidationResultAsync(response, correlationId, cancellationToken);

        return response;
    }

    /// <inheritdoc />
    public IReadOnlyList<DocumentTypeMetadataDto> GetSupportedDocumentTypes()
    {
        return _validatorsByType.Values
            .Select(v => new DocumentTypeMetadataDto
            {
                DocumentType = v.DocumentType,
                DisplayName = v.DisplayName,
                ExpectedFields = v.ExpectedFields,
                Rules = v.GetRulesMetadata()
            })
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ValidationHistoryItemDto>> GetValidationHistoryAsync(
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        if (_dbContext is null)
        {
            throw new InvalidOperationException("El contexto de base de datos PostgreSQL no está disponible.");
        }

        int safeLimit = Math.Clamp(limit, 1, 100);

        List<Validacion> records = await _dbContext.Validaciones
            .AsNoTracking()
            .Include(v => v.DetallesError)
            .OrderByDescending(v => v.FechaValidacionUtc)
            .Take(safeLimit)
            .ToListAsync(cancellationToken);

        return records.Select(r => new ValidationHistoryItemDto
        {
            Id = r.Id,
            DocumentType = r.CodigoTipoDocumento,
            IsValid = r.EsValido,
            ErrorCount = r.TotalErrores,
            CorrelationId = r.CorrelationId,
            ValidatedAtUtc = r.FechaValidacionUtc,
            Errors = r.DetallesError.Select(d => new ValidationErrorDto
            {
                Field = d.Campo,
                Code = d.CodigoRegla,
                Message = d.Mensaje
            }).ToList()
        }).ToList();
    }

    /// <summary>
    /// Guarda el registro de validación y sus errores en PostgreSQL.
    /// Si <c>_requirePersistence</c> es <c>true</c>, propaga cualquier excepción de base de datos para que
    /// <see cref="Middlewares.GlobalExceptionMiddleware"/> devuelva un HTTP 500 controlado.
    /// </summary>
    private async Task TryPersistValidationResultAsync(
        ValidationResponseDto response,
        string correlationId,
        CancellationToken cancellationToken)
    {
        if (_dbContext is null)
        {
            return;
        }

        try
        {
            TipoDocumento? tipoDoc = await _dbContext.TiposDocumento
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Codigo == response.DocumentType, cancellationToken);

            var validacionEntity = new Validacion
            {
                TipoDocumentoId = tipoDoc?.Id,
                CodigoTipoDocumento = response.DocumentType,
                EsValido = response.IsValid,
                TotalErrores = response.Errors.Count,
                CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? Guid.NewGuid().ToString("N") : correlationId,
                FechaValidacionUtc = response.ValidatedAtUtc,
                DetallesError = response.Errors.Select(err => new DetalleError
                {
                    Campo = err.Field,
                    CodigoRegla = err.Code,
                    Mensaje = err.Message
                }).ToList()
            };

            _dbContext.Validaciones.Add(validacionEntity);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Fallo al persistir el resultado de validación en PostgreSQL. CorrelationId={CorrelationId}",
                correlationId);

            if (_requirePersistence)
            {
                throw;
            }
        }
    }
}
