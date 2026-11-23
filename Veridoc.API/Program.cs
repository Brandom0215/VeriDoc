using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Veridoc.API.Data;
using Veridoc.API.DTOs;
using Veridoc.API.Middlewares;
using Veridoc.API.Services;
using Veridoc.API.Validators;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuración de controladores y respuesta estandarizada para JSON mal formado (400 Bad Request)
builder.Services
    .AddControllers()
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

// 2. Configuración de Entity Framework Core con PostgreSQL
string? postgreSqlConnection = builder.Configuration.GetConnectionString("PostgreSqlConnection");
if (!string.IsNullOrWhiteSpace(postgreSqlConnection))
{
    builder.Services.AddDbContext<VeridocDbContext>(options =>
        options.UseNpgsql(postgreSqlConnection));
}

// 3. Registro de validadores de documentos y servicio orquestador de validaciones
builder.Services.AddSingleton<IDocumentValidator, CedulaIdentidadValidator>();
builder.Services.AddSingleton<IDocumentValidator, FacturaComercialValidator>();
builder.Services.AddScoped<IValidationService, ValidationService>();

// 4. Configuración de CORS para permitir que la interfaz de demostración externa consuma la API
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

// 5. Configuración de OpenAPI / Swagger incluyendo comentarios XML de documentación
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// 6. Inicialización resiliente del esquema y catálogo de reglas en PostgreSQL
await DbInitializer.TryInitializeAsync(app.Services);

// 7. Tubería (Pipeline) de procesamiento HTTP
// El middleware de excepciones va primero para envolver todo el procesamiento posterior
app.UseMiddleware<GlobalExceptionMiddleware>();

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

// Autenticación por cabecera X-Api-Key para proteger los endpoints de la API
app.UseMiddleware<ApiKeyAuthMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();
