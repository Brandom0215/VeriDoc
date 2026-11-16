using System.Globalization;
using System.Text.Json;

namespace Veridoc.API.Validators;

/// <summary>
/// Clase utilitaria para inspeccionar y extraer campos de un <see cref="JsonElement"/> de manera segura,
/// tolerante a mayúsculas/minúsculas y protegida frente a tipos de datos inesperados en el JSON.
/// </summary>
public static class JsonElementHelper
{
    /// <summary>
    /// Busca una propiedad dentro de un objeto JSON ignorando diferencias entre mayúsculas y minúsculas.
    /// </summary>
    /// <param name="element">Elemento JSON raíz de tipo Object.</param>
    /// <param name="propertyName">Nombre esperado de la propiedad (por ejemplo, "numeroCedula").</param>
    /// <param name="value">Elemento JSON encontrado, si existe.</param>
    /// <returns><c>true</c> si la propiedad existe y no es nula; de lo contrario, <c>false</c>.</returns>
    public static bool TryGetPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement value)
    {
        value = default;

        if (element.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        foreach (JsonProperty prop in element.EnumerateObject())
        {
            if (string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                if (prop.Value.ValueKind == JsonValueKind.Null || prop.Value.ValueKind == JsonValueKind.Undefined)
                {
                    return false;
                }

                value = prop.Value;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Extrae de forma segura una cadena de texto de una propiedad JSON.
    /// </summary>
    /// <param name="element">Elemento JSON del documento.</param>
    /// <param name="propertyName">Nombre del campo a extraer.</param>
    /// <param name="stringValue">Valor de texto extraído (recortado con Trim), o <c>null</c> si no existe.</param>
    /// <param name="isInvalidType">Indica <c>true</c> si la propiedad existe pero no es de tipo String en el JSON.</param>
    /// <returns><c>true</c> si el campo existe, es de tipo String y contiene texto no vacío.</returns>
    public static bool TryGetNonEmptyString(
        JsonElement element,
        string propertyName,
        out string? stringValue,
        out bool isInvalidType)
    {
        stringValue = null;
        isInvalidType = false;

        if (!TryGetPropertyIgnoreCase(element, propertyName, out JsonElement propValue))
        {
            return false;
        }

        if (propValue.ValueKind != JsonValueKind.String)
        {
            isInvalidType = true;
            return false;
        }

        string? raw = propValue.GetString()?.Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        stringValue = raw;
        return true;
    }

    /// <summary>
    /// Extrae de forma segura un valor numérico decimal de una propiedad JSON.
    /// </summary>
    /// <param name="element">Elemento JSON del documento.</param>
    /// <param name="propertyName">Nombre del campo numérico a extraer.</param>
    /// <param name="decimalValue">Valor decimal extraído si la conversión fue exitosa.</param>
    /// <param name="isInvalidType">Indica <c>true</c> si la propiedad existe pero no es un número decimal válido.</param>
    /// <returns><c>true</c> si el campo existe y pudo leerse como <see cref="decimal"/>.</returns>
    public static bool TryGetDecimal(
        JsonElement element,
        string propertyName,
        out decimal decimalValue,
        out bool isInvalidType)
    {
        decimalValue = 0m;
        isInvalidType = false;

        if (!TryGetPropertyIgnoreCase(element, propertyName, out JsonElement propValue))
        {
            return false;
        }

        if (propValue.ValueKind == JsonValueKind.Number && propValue.TryGetDecimal(out decimalValue))
        {
            return true;
        }

        // Si el cliente envió un texto numérico o un tipo distinto a Number, se marca como formato/tipo inválido
        isInvalidType = true;
        return false;
    }

    /// <summary>
    /// Intenta convertir una cadena en formato estricto <c>YYYY-MM-DD</c> a <see cref="DateOnly"/>.
    /// </summary>
    /// <param name="dateText">Texto de la fecha a evaluar.</param>
    /// <param name="date">Fecha resultante si el formato es válido.</param>
    /// <returns><c>true</c> si cumple exactamente el formato <c>yyyy-MM-dd</c> y es una fecha real del calendario.</returns>
    public static bool TryParseIsoDate(string? dateText, out DateOnly date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(dateText))
        {
            return false;
        }

        return DateOnly.TryParseExact(
            dateText.Trim(),
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date);
    }
}
