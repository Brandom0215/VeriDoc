using System.Text.Json;

namespace Veridoc.API.DTOs;

/// <summary>
/// Contrato de entrada para solicitar la validación de un documento estructurado en formato JSON.
/// </summary>
public class ValidationRequestDto
{
    /// <summary>
    /// Identificador del tipo de documento a evaluar (por ejemplo, "cedula_identidad" o "factura_comercial").
    /// </summary>
    public string? DocumentType { get; set; }

    /// <summary>
    /// Objeto JSON dinámico que contiene los campos específicos del documento que será evaluado.
    /// Se utiliza <see cref="JsonElement"/> para permitir que cada validador inspeccione la estructura
    /// sin acoplar el contrato general a un único esquema documental.
    /// </summary>
    public JsonElement? Document { get; set; }
}
