using System.Text.Json;
using Veridoc.API.DTOs;
using Veridoc.API.Validators;

namespace Veridoc.API.Services;

/// <summary>
/// Implementación del servicio orquestador de validaciones documentales.
/// Selecciona dinámicamente el validador correspondiente al tipo de documento y ejecuta todas sus reglas.
/// </summary>
public class ValidationService : IValidationService
{
    private readonly Dictionary<string, IDocumentValidator> _validatorsByType;
    private readonly ILogger<ValidationService> _logger;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ValidationService"/>.
    /// </summary>
    /// <param name="validators">Colección de validadores documentales registrados en el contenedor de dependencias.</param>
    /// <param name="logger">Servicio de registro de eventos de validación (sin almacenar datos sensibles).</param>
    public ValidationService(
        IEnumerable<IDocumentValidator> validators,
        ILogger<ValidationService> logger)
    {
        _validatorsByType = validators.ToDictionary(
            v => v.DocumentType,
            v => v,
            StringComparer.OrdinalIgnoreCase);
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<ValidationResponseDto> ValidateDocumentAsync(
        ValidationRequestDto request,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        // 1. Verificar que se haya indicado el tipo de documento
        if (string.IsNullOrWhiteSpace(request.DocumentType))
        {
            return Task.FromResult(new ValidationResponseDto
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
            });
        }

        string normalizedType = request.DocumentType.Trim().ToLowerInvariant();

        // 2. Comprobar que el tipo de documento esté soportado en el MVP
        if (!_validatorsByType.TryGetValue(normalizedType, out IDocumentValidator? validator))
        {
            string supportedList = string.Join(", ", _validatorsByType.Keys);
            return Task.FromResult(new ValidationResponseDto
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
            });
        }

        // 3. Verificar que el nodo 'document' exista y sea un objeto JSON
        if (request.Document is null ||
            request.Document.Value.ValueKind == JsonValueKind.Undefined ||
            request.Document.Value.ValueKind == JsonValueKind.Null ||
            request.Document.Value.ValueKind != JsonValueKind.Object)
        {
            return Task.FromResult(new ValidationResponseDto
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
            });
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

        return Task.FromResult(response);
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
}
