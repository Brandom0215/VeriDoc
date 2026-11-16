using System.Reflection;
using Microsoft.AspNetCore.Mvc;
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

// 2. Registro de validadores de documentos y servicio orquestador de validaciones
builder.Services.AddSingleton<IDocumentValidator, CedulaIdentidadValidator>();
builder.Services.AddSingleton<IDocumentValidator, FacturaComercialValidator>();
builder.Services.AddScoped<IValidationService, ValidationService>();

// 3. Configuración de CORS para permitir que la interfaz de demostración consuma la API
builder.Services.AddCors(options =>
{
    options.AddPolicy("DemoClientPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .WithHeaders("Content-Type", ApiKeyAuthMiddleware.ApiKeyHeaderName, "X-Correlation-Id");
    });
});

// 4. Configuración de OpenAPI / Swagger incluyendo comentarios XML de documentación
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

// 5. Tubería (Pipeline) de procesamiento HTTP
// El middleware de excepciones va primero para envolver todo el procesamiento posterior
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("DemoClientPolicy");

// Autenticación por cabecera X-Api-Key para proteger los endpoints de la API
app.UseMiddleware<ApiKeyAuthMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();
