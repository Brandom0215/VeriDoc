namespace Veridoc.API.Middlewares;

/// <summary>
/// Middleware de ciberseguridad encargado de inyectar cabeceras HTTP defensivas en todas las respuestas.
/// Mitiga ataques de MIME-Sniffing, Clickjacking, almacenamiento en caché de datos sensibles y fuga de Referrer.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="SecurityHeadersMiddleware"/>.
    /// </summary>
    /// <param name="next">Siguiente delegado en la tubería HTTP.</param>
    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Agrega las cabeceras de seguridad antes de enviar la respuesta al cliente.
    /// </summary>
    /// <param name="context">Contexto HTTP de la solicitud actual.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            IHeaderDictionary headers = context.Response.Headers;

            // Evita que el navegador adivine (sniff) el MIME type fuera de application/json
            headers["X-Content-Type-Options"] = "nosniff";

            // Previene ataques de Clickjacking impidiendo que la API se cargue en un <iframe>
            headers["X-Frame-Options"] = "DENY";

            // Evita que se filtren URLs o parámetros en la cabecera Referer hacia terceros
            headers["Referrer-Policy"] = "no-referrer";

            // Restringe APIs del navegador innecesarias
            headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

            // Para endpoints de la API (/api/*), deshabilita caché y aplica CSP estricta
            if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
            {
                headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
                headers["Pragma"] = "no-cache";
                headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
            }

            return Task.CompletedTask;
        });

        await _next(context);
    }
}
