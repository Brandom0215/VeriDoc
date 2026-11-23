using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Veridoc.API.DTOs;

namespace Veridoc.API.Middlewares;

/// <summary>
/// Middleware global encargado de asignar un identificador de correlación seguro a cada solicitud HTTP
/// y capturar cualquier excepción técnica no controlada para devolver una respuesta JSON estandarizada.
/// </summary>
public partial class GlobalExceptionMiddleware
{
    private const string CorrelationHeaderName = "X-Correlation-Id";

    /// <summary>
    /// Lista blanca estricta para X-Correlation-Id (solo caracteres alfanuméricos, guion y guion bajo, de 1 a 64 caracteres).
    /// Previene ataques de Log Injection (CRLF Injection) y HTTP Response Splitting.
    /// </summary>
    [GeneratedRegex(@"^[a-zA-Z0-9\-_]{1,64}$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeCorrelationIdRegex();

    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="GlobalExceptionMiddleware"/>.
    /// </summary>
    /// <param name="next">Siguiente delegado en la tubería (pipeline) de procesamiento HTTP.</param>
    /// <param name="logger">Servicio de registro de eventos para auditoría técnica sin exponer datos sensibles.</param>
    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Procesa la solicitud HTTP actual, gestiona el identificador de correlación y captura fallos inesperados.
    /// </summary>
    /// <param name="context">Contexto HTTP de la solicitud en curso.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        string correlationId = ResolveCorrelationId(context);
        context.Response.Headers[CorrelationHeaderName] = correlationId;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // Se registra únicamente el tipo de fallo y el CorrelationId sanitizado; nunca el cuerpo del documento (Ley 81 de 2019).
            _logger.LogError(
                ex,
                "Error técnico no controlado al procesar {Method} {Path}. CorrelationId: {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                correlationId);

            await HandleExceptionAsync(context, correlationId);
        }
    }

    /// <summary>
    /// Obtiene el identificador de correlación enviado por el cliente si cumple la lista blanca alfanumérica;
    /// de lo contrario, genera un GUID criptográficamente limpio para evitar Log Forging / CRLF Injection.
    /// </summary>
    private static string ResolveCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationHeaderName, out var headerValue))
        {
            string candidate = headerValue.ToString().Trim();
            if (SafeCorrelationIdRegex().IsMatch(candidate))
            {
                context.TraceIdentifier = candidate;
                return context.TraceIdentifier;
            }
        }

        context.TraceIdentifier = Guid.NewGuid().ToString("N");
        return context.TraceIdentifier;
    }

    /// <summary>
    /// Construye y escribe en la respuesta HTTP un error técnico controlado con código de estado 500.
    /// </summary>
    private static async Task HandleExceptionAsync(HttpContext context, string correlationId)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "application/json; charset=utf-8";

        var errorResponse = new ApiErrorResponseDto
        {
            ErrorCode = "INTERNAL_SERVER_ERROR",
            Message = "Ocurrió un error técnico interno al procesar la solicitud. Por favor, intente nuevamente más tarde.",
            CorrelationId = correlationId,
            TimestampUtc = DateTime.UtcNow
        };

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, jsonOptions));
    }
}
