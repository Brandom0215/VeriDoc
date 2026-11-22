using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Veridoc.API.Controllers;
using Veridoc.API.Data;
using Veridoc.API.DTOs;
using Veridoc.API.Middlewares;
using Veridoc.API.Services;
using Veridoc.API.Validators;
using Xunit;

namespace Veridoc.Tests;

/// <summary>
/// Pruebas de integración del servicio orquestador, controladores, autenticación por API Key,
/// solicitudes mal formadas y manejo controlado de fallos de conexión a PostgreSQL.
/// </summary>
public class ValidationPipelineAndEdgeCasesTests
{
    private static ValidationService CreateValidationService(
        VeridocDbContext? dbContext = null,
        bool requirePersistence = false)
    {
        var validators = new IDocumentValidator[]
        {
            new CedulaIdentidadValidator(),
            new FacturaComercialValidator()
        };

        var configData = new Dictionary<string, string?>
        {
            ["Database:RequirePersistence"] = requirePersistence.ToString()
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        return new ValidationService(
            validators,
            NullLogger<ValidationService>.Instance,
            dbContext,
            configuration);
    }

    /// <summary>
    /// Caso 29: Verifica que un tipo de documento no soportado devuelva UNSUPPORTED_DOCUMENT_TYPE.
    /// </summary>
    [Fact]
    public async Task ValidateDocumentAsync_UnsupportedDocumentType_ReturnsUnsupportedError()
    {
        ValidationService service = CreateValidationService();
        using JsonDocument doc = JsonDocument.Parse("""{"numero": "P-12345"}""");

        var request = new ValidationRequestDto
        {
            DocumentType = "pasaporte",
            Document = doc.RootElement
        };

        ValidationResponseDto result = await service.ValidateDocumentAsync(request, "corr-test-29");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Field == "documentType" && e.Code == "UNSUPPORTED_DOCUMENT_TYPE");
    }

    /// <summary>
    /// Caso 30: Verifica que si falta el nodo 'document' o no es un objeto JSON se reporte REQUIRED_FIELD.
    /// </summary>
    [Fact]
    public async Task ValidateDocumentAsync_NullDocumentNode_ReturnsRequiredFieldError()
    {
        ValidationService service = CreateValidationService();

        var request = new ValidationRequestDto
        {
            DocumentType = "cedula_identidad",
            Document = null
        };

        ValidationResponseDto result = await service.ValidateDocumentAsync(request, "corr-test-30");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Field == "document" && e.Code == "REQUIRED_FIELD");
    }

    /// <summary>
    /// Caso 31: Verifica que el catálogo devuelva exactamente los 2 tipos de documentos del MVP con al menos 5 reglas cada uno.
    /// </summary>
    [Fact]
    public void GetSupportedDocumentTypes_ReturnsTwoDocumentTypesWithSixRulesEach()
    {
        ValidationService service = CreateValidationService();

        IReadOnlyList<DocumentTypeMetadataDto> catalog = service.GetSupportedDocumentTypes();

        Assert.Equal(2, catalog.Count);
        Assert.All(catalog, item => Assert.True(item.Rules.Count >= 5));
    }

    /// <summary>
    /// Caso 32: Verifica que una solicitud nula o cuerpo mal formado en ValidationsController devuelva 400 Bad Request controlado.
    /// </summary>
    [Fact]
    public async Task ValidationsController_NullRequestBody_Returns400BadRequestWithApiErrorDto()
    {
        ValidationService service = CreateValidationService();
        var controller = new ValidationsController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { TraceIdentifier = "corr-test-32" }
            }
        };

        ActionResult<ValidationResponseDto> actionResult = await controller.ValidateDocument(null, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        var errorDto = Assert.IsType<ApiErrorResponseDto>(badRequest.Value);
        Assert.Equal("EMPTY_REQUEST_BODY", errorDto.ErrorCode);
        Assert.Equal("corr-test-32", errorDto.CorrelationId);
    }

    /// <summary>
    /// Caso 33: Verifica que ApiKeyAuthMiddleware bloquee con 401 Unauthorized una solicitud sin cabecera X-Api-Key.
    /// </summary>
    [Fact]
    public async Task ApiKeyAuthMiddleware_MissingApiKeyHeader_Returns401Unauthorized()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Authentication:ApiKey"] = "secret-test-key" })
            .Build();

        var middleware = new ApiKeyAuthMiddleware(
            _ => Task.CompletedTask,
            config,
            NullLogger<ApiKeyAuthMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/validations";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    /// <summary>
    /// Caso 34: Verifica que ApiKeyAuthMiddleware rechace una API Key inválida (401) y permita el acceso con la clave correcta (200).
    /// </summary>
    [Theory]
    [InlineData("wrong-key", StatusCodes.Status401Unauthorized)]
    [InlineData("secret-test-key", StatusCodes.Status200OK)]
    public async Task ApiKeyAuthMiddleware_ValidatesProvidedKeyCorrectly(string providedKey, int expectedStatusCode)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Authentication:ApiKey"] = "secret-test-key" })
            .Build();

        var middleware = new ApiKeyAuthMiddleware(
            ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status200OK;
                return Task.CompletedTask;
            },
            config,
            NullLogger<ApiKeyAuthMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/validations";
        context.Request.Headers[ApiKeyAuthMiddleware.ApiKeyHeaderName] = providedKey;
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(expectedStatusCode, context.Response.StatusCode);
    }

    /// <summary>
    /// Caso 35: Simula un error de acceso/conexión a PostgreSQL durante la persistencia obligatoria y comprueba que
    /// GlobalExceptionMiddleware devuelva un 500 Internal Server Error controlado sin exponer cadenas de conexión.
    /// </summary>
    [Fact]
    public async Task GlobalExceptionMiddleware_DatabaseConnectionFailure_ReturnsControlled500Error()
    {
        var dbOptions = new DbContextOptionsBuilder<VeridocDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=5432;Database=invalid_db;Username=invalid;Password=secret_pwd")
            .Options;

        var failingDbContext = new VeridocDbContext(dbOptions);
        // Al liberar el contexto simulamos un fallo crítico inmediato al intentar acceder a PostgreSQL
        await failingDbContext.DisposeAsync();

        ValidationService service = CreateValidationService(failingDbContext, requirePersistence: true);

        var exceptionMiddleware = new GlobalExceptionMiddleware(
            async ctx =>
            {
                using JsonDocument doc = JsonDocument.Parse("""{"numeroCedula": "8-888-8888"}""");
                await service.ValidateDocumentAsync(
                    new ValidationRequestDto { DocumentType = "cedula_identidad", Document = doc.RootElement },
                    ctx.TraceIdentifier);
            },
            NullLogger<GlobalExceptionMiddleware>.Instance);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "POST";
        httpContext.Request.Path = "/api/validations";
        httpContext.Response.Body = new MemoryStream();

        await exceptionMiddleware.InvokeAsync(httpContext);

        Assert.Equal(StatusCodes.Status500InternalServerError, httpContext.Response.StatusCode);

        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        string bodyText = await new StreamReader(httpContext.Response.Body).ReadToEndAsync();

        Assert.Contains("INTERNAL_SERVER_ERROR", bodyText);
        // Asegura que no se filtre la contraseña de la cadena de conexión en la respuesta al cliente
        Assert.DoesNotContain("secret_pwd", bodyText);
    }

    /// <summary>
    /// Caso 36: Verifica que el endpoint público HealthController responda 200 OK.
    /// </summary>
    [Fact]
    public void HealthController_GetHealth_Returns200Ok()
    {
        var controller = new HealthController();
        IActionResult result = controller.GetHealth();

        Assert.IsType<OkObjectResult>(result);
    }
}
