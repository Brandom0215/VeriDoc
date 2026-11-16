namespace Veridoc.API.DTOs;

/// <summary>
/// Representa los metadatos descriptivos de una regla de validación implementada en el sistema.
/// </summary>
public class RuleMetadataDto
{
    /// <summary>
    /// Código único de la regla dentro del tipo documental (por ejemplo, "REQUIRED_FIELD" o "UNDERAGE_HOLDER").
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Nombre corto descriptivo de la regla.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Explicación detallada de la condición que verifica esta regla.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Representa la información de un tipo de documento soportado por Veridoc API y sus reglas asociadas.
/// </summary>
public class DocumentTypeMetadataDto
{
    /// <summary>
    /// Identificador técnico del tipo de documento (por ejemplo, "cedula_identidad" o "factura_comercial").
    /// </summary>
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>
    /// Nombre legible del tipo de documento para mostrar en interfaces de usuario.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Lista de nombres de campos esperados en el objeto JSON del documento.
    /// </summary>
    public IReadOnlyList<string> ExpectedFields { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Colección de reglas de validación activas para este tipo de documento.
    /// </summary>
    public IReadOnlyList<RuleMetadataDto> Rules { get; set; } = Array.Empty<RuleMetadataDto>();
}
