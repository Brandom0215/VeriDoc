using Microsoft.AspNetCore.Mvc;

namespace Veridoc.API.Controllers;

/// <summary>
/// Controlador para comprobar la disponibilidad operativa del servicio Veridoc API.
/// </summary>
[ApiController]
[Route("api/health")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Devuelve el estado operativo actual de la API.
    /// </summary>
    /// <returns>Objeto JSON indicando el estado del servicio y la marca de tiempo UTC.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetHealth()
    {
        return Ok(new
        {
            status = "Healthy",
            service = "Veridoc.API",
            timestampUtc = DateTime.UtcNow
        });
    }
}
