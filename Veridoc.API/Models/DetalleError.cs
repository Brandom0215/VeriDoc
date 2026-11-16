namespace Veridoc.API.Models;

/// <summary>
/// Entidad de persistencia que almacena un incumplimiento de regla individual dentro de una <see cref="Validacion"/>.
/// Solo guarda el nombre del campo, el código de la regla y el mensaje descriptivo (sin guardar el valor personal ingresado).
/// </summary>
public class DetalleError
{
    /// <summary>
    /// Clave primaria autoincremental del detalle de error.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Clave foránea que referencia a la ejecución de <see cref="Validacion"/> correspondiente.
    /// </summary>
    public int ValidacionId { get; set; }

    /// <summary>
    /// Nombre del campo donde ocurrió el incumplimiento (por ejemplo, "numeroCedula" o "total").
    /// </summary>
    public string Campo { get; set; } = string.Empty;

    /// <summary>
    /// Código identificador de la regla incumplida (por ejemplo, "REQUIRED_FIELD" o "INCONSISTENT_TOTAL").
    /// </summary>
    public string CodigoRegla { get; set; } = string.Empty;

    /// <summary>
    /// Mensaje explicativo del incumplimiento detectado.
    /// </summary>
    public string Mensaje { get; set; } = string.Empty;

    /// <summary>
    /// Propiedad de navegación hacia el registro padre de validación.
    /// </summary>
    public Validacion? Validacion { get; set; }
}
