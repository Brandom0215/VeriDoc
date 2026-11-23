using System.Reflection;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Veridoc.API.Data;
using Veridoc.API.DTOs;
using Veridoc.API.Middlewares;
using Veridoc.API.Services;
using Veridoc.API.Validators;

var builder = WebApplication.CreateBuilder(args);

// 1. Endurecimiento (Hardening) del servidor Kestrel: ocultar banner Server y limitar tamaño máximo de cuerpo HTTP a 64 KB
builder.WebHost.ConfigureKestrel(kestrel =>
{
    kestrel.AddServerHeader = false;
    kestrel.Limits.MaxRequestBodySize = 65_536;
});

// 2. Configuración de controladores, límite de profundidad JSON (MaxDepth = 16) y respuesta 400 estandarizada
builder.Services
    .AddControllers()
    .AddJsonOptions(json =>
    {
        json.JsonSerializerOptions.MaxDepth = 16;
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errorResponse = new ApiErrorResponseDto
            {
                ErrorCode = "INVALID_REQUEST_FORMAT",
                Message = "La estructura JSON de la solicitud no es válida o no pudo procesarse correctamente.",
                CorrelationId = context.HttpContext.TraceIdentifier,
                TimestampUtc = DateTime.UtcNow
            };

            return new BadRequestObjectResult(errorResponse);
        };
    });

// 3. Limitador de tasa de peticiones (Rate Limiting) por dirección IP para mitigar fuerza bruta y DoS
builder.Services.AddRateLimiter(rateLimiter =>
{
    rateLimiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    rateLimiter.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json; charset=utf-8";
        var errorDto = new ApiErrorResponseDto
        {
            ErrorCode = "RATE_LIMIT_EXCEEDED",
            Message = "Se ha excedido el límite de solicitudes permitidas por minuto. Intente nuevamente en unos segundos.",
            CorrelationId = context.HttpContext.TraceIdentifier,
            TimestampUtc = DateTime.UtcNow
        };

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.HttpContext.Response.WriteAsync(JsonSerializer.Serialize(errorDto, jsonOptions), cancellationToken);
    };

    rateLimiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        string clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: clientIp,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});

// 4. Configuración de Entity Framework Core con PostgreSQL (soporta URL directa de Supabase o formato Host=...)
string? rawPostgreSqlConnection = builder.Configuration.GetConnectionString("PostgreSqlConnection");
if (!string.IsNullOrWhiteSpace(rawPostgreSqlConnection))
{
    string normalizedConnection = PostgresConnectionStringHelper.Normalize(rawPostgreSqlConnection);
    builder.Services.AddDbContext<VeridocDbContext>(options =>
        options.UseNpgsql(normalizedConnection));
}

// 5. Registro de validadores de documentos y servicio orquestador de validaciones
builder.Services.AddSingleton<IDocumentValidator, CedulaIdentidadValidator>();
builder.Services.AddSingleton<IDocumentValidator, FacturaComercialValidator>();
builder.Services.AddScoped<IValidationService, ValidationService>();

// 6. Configuración de CORS para permitir que la interfaz de demostración externa consuma la API
builder.Services.AddCors(options =>
{
    options.AddPolicy("DemoClientPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .WithHeaders("Content-Type", ApiKeyAuthMiddleware.ApiKeyHeaderName, "X-Correlation-Id")
              .WithExposedHeaders("X-Correlation-Id");
    });
});

// 7. Configuración de OpenAPI / Swagger incluyendo comentarios XML y esquema de seguridad X-Api-Key
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Veridoc API",
        Version = "v1",
        Description = "Servicio RESTful para validación de documentos estructurados en JSON mediante reglas de negocio programadas."
    });

    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "Ingrese su clave de acceso para la cabecera X-Api-Key (Ejemplo local: veridoc-dev-key-2026).",
        Name = ApiKeyAuthMiddleware.ApiKeyHeaderName,
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("ApiKey", document)] = new List<string>()
    });

    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// 8. Inicialización resiliente del esquema y catálogo de reglas en PostgreSQL / Supabase
await DbInitializer.TryInitializeAsync(app.Services);

// 9. Tubería (Pipeline) de procesamiento HTTP y Seguridad
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

app.UseCors("DemoClientPolicy");

// Limitador de tasa antes de evaluar autenticación y controladores
app.UseRateLimiter();

// Autenticación por cabecera X-Api-Key para proteger los endpoints de la API
app.UseMiddleware<ApiKeyAuthMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();
