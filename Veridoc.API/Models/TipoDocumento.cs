namespace Veridoc.API.Models;

/// <summary>
/// Entidad de persistencia que representa un tipo de documento soportado en el catálogo de PostgreSQL.
/// </summary>
public class TipoDocumento
{
    /// <summary>
    /// Clave primaria autoincremental del tipo de documento.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Código técnico único del tipo documental (por ejemplo, "cedula_identidad" o "factura_comercial").
    /// </summary>
    public string Codigo { get; set; } = string.Empty;

    /// <summary>
    /// Nombre descriptivo legible del tipo de documento.
    /// </summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Indica si el tipo de documento se encuentra habilitado para validaciones.
    /// </summary>
    public bool Activo { get; set; } = true;

    /// <summary>
    /// Fecha y hora UTC de registro en la base de datos.
    /// </summary>
    public DateTime FechaCreacionUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Colección de reglas de validación asociadas a este tipo de documento.
    /// </summary>
    public List<ReglaValidacion> Reglas { get; set; } = new();

    /// <summary>
    /// Colección de ejecuciones de validación registradas para este tipo de documento.
    /// </summary>
    public List<Validacion> Validaciones { get; set; } = new();
}
