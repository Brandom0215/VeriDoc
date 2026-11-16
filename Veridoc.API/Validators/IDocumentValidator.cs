using System.Text.Json;
using Veridoc.API.DTOs;

namespace Veridoc.API.Validators;

/// <summary>
/// Define el contrato común que debe implementar cualquier validador de un tipo de documento en Veridoc API.
/// </summary>
public interface IDocumentValidator
{
    /// <summary>
    /// Identificador único técnico del tipo de documento (por ejemplo, "cedula_identidad" o "factura_comercial").
    /// </summary>
    string DocumentType { get; }

    /// <summary>
    /// Nombre descriptivo legible del tipo de documento.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Lista de nombres de campos que componen la estructura esperada del documento.
    /// </summary>
    IReadOnlyList<string> ExpectedFields { get; }

    /// <summary>
    /// Obtiene el catálogo de reglas configuradas para este tipo de documento.
    /// </summary>
    /// <returns>Colección de metadatos de reglas de validación.</returns>
    IReadOnlyList<RuleMetadataDto> GetRulesMetadata();

    /// <summary>
    /// Evalúa un objeto JSON correspondiente al documento contra todas las reglas definidas para su tipo.
    /// </summary>
    /// <param name="documentElement">Elemento JSON que contiene los campos del documento.</param>
    /// <returns>Lista de errores detectados. Si el documento es válido, devuelve una lista vacía.</returns>
    List<ValidationErrorDto> Validate(JsonElement documentElement);
}
