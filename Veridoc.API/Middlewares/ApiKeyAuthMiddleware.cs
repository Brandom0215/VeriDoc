using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Veridoc.API.DTOs;

namespace Veridoc.API.Middlewares;

/// <summary>
/// Middleware encargado de autenticar las solicitudes entrantes mediante la cabecera HTTP <c>X-Api-Key</c>.
/// Permite acceso libre al endpoint de estado (<c>/api/health</c>) y a la interfaz de documentación Swagger.
/// </summary>
public class ApiKeyAuthMiddleware
{
    /// <summary>
    /// Nombre de la cabecera HTTP donde el cliente debe enviar la clave de acceso.
    /// </summary>
    public const string ApiKeyHeaderName = "X-Api-Key";

    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApiKeyAuthMiddleware> _logger;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="ApiKeyAuthMiddleware"/>.
    /// </summary>
    /// <param name="next">Siguiente delegado en el pipeline HTTP.</param>
    /// <param name="configuration">Configuración de la aplicación para obtener la clave esperada.</param>
    /// <param name="logger">Servicio de registro de intentos de acceso no autorizados.</param>
    public ApiKeyAuthMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<ApiKeyAuthMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Evalúa si la ruta requiere autenticación por API Key y comprueba la validez de la credencial recibida.
    /// </summary>
    /// <param name="context">Contexto HTTP actual.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        // Permitir preflight CORS (OPTIONS) y rutas públicas como /api/health o Swagger
        if (HttpMethods.IsOptions(context.Request.Method) || IsPublicEndpoint(context.Request.Path))
        {
            await _next(context);
            return;
        }

        string? expectedApiKey = _configuration["Authentication:ApiKey"];

        // Si no se ha configurado la API Key en el servidor, se bloquea el acceso de forma controlada
        if (string.IsNullOrWhiteSpace(expectedApiKey))
        {
            _logger.LogWarning(
                "La clave 'Authentication:ApiKey' no está configurada en el servidor. CorrelationId: {CorrelationId}",
                context.TraceIdentifier);

            await WriteUnauthorizedResponseAsync(
                context,
                "SERVICE_AUTH_NOT_CONFIGURED",
                "El servicio de autenticación no está configurado correctamente en el servidor.");
            return;
        }

        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey) ||
            string.IsNullOrWhiteSpace(extractedApiKey))
        {
            await WriteUnauthorizedResponseAsync(
                context,
                "MISSING_API_KEY",
                $"Se requiere la cabecera '{ApiKeyHeaderName}' para consumir este endpoint.");
            return;
        }

        if (!FixedTimeEquals(expectedApiKey, extractedApiKey.ToString()))
        {
            _logger.LogWarning(
                "Intento de acceso con API Key inválida a {Path}. CorrelationId: {CorrelationId}",
                context.Request.Path,
                context.TraceIdentifier);

            await WriteUnauthorizedResponseAsync(
                context,
                "INVALID_API_KEY",
                "La clave de API proporcionada no es válida.");
            return;
        }

        await _next(context);
    }

    /// <summary>
    /// Determina si la ruta solicitada es pública y no requiere cabecera <c>X-Api-Key</c>.
    /// </summary>
    private static bool IsPublicEndpoint(PathString path)
    {
        return path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWithSegments("/api/health", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Compara dos cadenas en tiempo constante para mitigar ataques de temporización (timing attacks).
    /// </summary>
    private static bool FixedTimeEquals(string expected, string provided)
    {
        byte[] expectedBytes = Encoding.UTF8.GetBytes(expected);
        byte[] providedBytes = Encoding.UTF8.GetBytes(provided);

        return CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }

    /// <summary>
    /// Escribe una respuesta HTTP 401 estandarizada en formato JSON.
    /// </summary>
    private static async Task WriteUnauthorizedResponseAsync(HttpContext context, string errorCode, string message)
    {
        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        context.Response.ContentType = "application/json; charset=utf-8";

        var errorResponse = new ApiErrorResponseDto
        {
            ErrorCode = errorCode,
            Message = message,
            CorrelationId = context.TraceIdentifier,
            TimestampUtc = DateTime.UtcNow
        };

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, jsonOptions));
    }
}
