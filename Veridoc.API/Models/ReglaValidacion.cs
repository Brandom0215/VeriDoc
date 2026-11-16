namespace Veridoc.API.Models;

/// <summary>
/// Entidad de persistencia que representa una regla de validación registrada en el catálogo de PostgreSQL.
/// </summary>
public class ReglaValidacion
{
    /// <summary>
    /// Clave primaria autoincremental de la regla de validación.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Clave foránea que referencia al <see cref="TipoDocumento"/> al que pertenece esta regla.
    /// </summary>
    public int TipoDocumentoId { get; set; }

    /// <summary>
    /// Código estandarizado de la regla (por ejemplo, "REQUIRED_FIELD" o "UNDERAGE_HOLDER").
    /// </summary>
    public string Codigo { get; set; } = string.Empty;

    /// <summary>
    /// Nombre corto descriptivo de la regla.
    /// </summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Explicación detallada de la condición verificada por la regla.
    /// </summary>
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>
    /// Indica si la regla se encuentra activa en el catálogo.
    /// </summary>
    public bool Activa { get; set; } = true;

    /// <summary>
    /// Propiedad de navegación hacia el tipo de documento propietario.
    /// </summary>
    public TipoDocumento? TipoDocumento { get; set; }
}
