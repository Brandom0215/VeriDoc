namespace Veridoc.API.DTOs;

/// <summary>
/// Modelo de transporte que representa los datos extraídos de una Cédula de Identidad (<c>cedula_identidad</c>).
/// </summary>
public class CedulaIdentidadDto
{
    /// <summary>
    /// Número de cédula de identidad panameña (por ejemplo, "8-888-8888", "PE-12-345", "E-8-123456").
    /// </summary>
    public string? NumeroCedula { get; set; }

    /// <summary>
    /// Nombre y apellido completos de la persona titular del documento.
    /// </summary>
    public string? NombreCompleto { get; set; }

    /// <summary>
    /// Fecha de nacimiento del titular en formato ISO <c>YYYY-MM-DD</c>.
    /// </summary>
    public string? FechaNacimiento { get; set; }

    /// <summary>
    /// Fecha de expedición del documento en formato ISO <c>YYYY-MM-DD</c>.
    /// </summary>
    public string? FechaExpedicion { get; set; }

    /// <summary>
    /// Fecha de expiración del documento en formato ISO <c>YYYY-MM-DD</c>.
    /// </summary>
    public string? FechaExpiracion { get; set; }

    /// <summary>
    /// Nacionalidad declarada en el documento (por ejemplo, "Panameña").
    /// </summary>
    public string? Nacionalidad { get; set; }
}
