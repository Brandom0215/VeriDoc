using Microsoft.EntityFrameworkCore;
using Veridoc.API.DTOs;
using Veridoc.API.Models;
using Veridoc.API.Validators;

namespace Veridoc.API.Data;

/// <summary>
/// Componente encargado de asegurar la creación del esquema en PostgreSQL y poblar (seed)
/// el catálogo inicial de tipos de documentos y reglas definidas en los validadores del MVP.
/// </summary>
public static class DbInitializer
{
    /// <summary>
    /// Intenta inicializar la base de datos y sincronizar el catálogo de reglas al arrancar la aplicación.
    /// Si PostgreSQL no está disponible en el entorno local, registra una advertencia controlada sin abortar el inicio de la API.
    /// </summary>
    /// <param name="serviceProvider">Proveedor de servicios de la aplicación.</param>
    public static async Task TryInitializeAsync(IServiceProvider serviceProvider)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        ILogger logger = loggerFactory.CreateLogger("DbInitializer");

        string? connectionString = configuration.GetConnectionString("PostgreSqlConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            logger.LogInformation("No se configuró 'PostgreSqlConnection'. La API operará sin inicialización automática de PostgreSQL.");
            return;
        }

        try
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<VeridocDbContext>();
            var validators = scope.ServiceProvider.GetServices<IDocumentValidator>();

            await SeedCatalogAsync(dbContext, validators);
            logger.LogInformation("Esquema de PostgreSQL y catálogo de reglas sincronizados correctamente.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "No se pudo conectar a PostgreSQL durante el inicio de la aplicación. Verifique que el servicio o contenedor Docker esté activo.");
        }
    }

    /// <summary>
    /// Crea las tablas si no existen y sincroniza los tipos documentales y sus reglas en el contexto proporcionado.
    /// </summary>
    /// <param name="dbContext">Contexto de base de datos activo.</param>
    /// <param name="validators">Validadores registrados cuyas reglas se registrarán en el catálogo.</param>
    public static async Task SeedCatalogAsync(
        VeridocDbContext dbContext,
        IEnumerable<IDocumentValidator> validators)
    {
        await dbContext.Database.EnsureCreatedAsync();

        foreach (IDocumentValidator validator in validators)
        {
            TipoDocumento? tipoDoc = await dbContext.TiposDocumento
                .Include(t => t.Reglas)
                .FirstOrDefaultAsync(t => t.Codigo == validator.DocumentType);

            if (tipoDoc is null)
            {
                tipoDoc = new TipoDocumento
                {
                    Codigo = validator.DocumentType,
                    Nombre = validator.DisplayName,
                    Activo = true,
                    FechaCreacionUtc = DateTime.UtcNow
                };
                dbContext.TiposDocumento.Add(tipoDoc);
                await dbContext.SaveChangesAsync();
            }

            IReadOnlyList<RuleMetadataDto> rulesMetadata = validator.GetRulesMetadata();
            foreach (RuleMetadataDto ruleMeta in rulesMetadata)
            {
                bool exists = tipoDoc.Reglas.Any(r =>
                    string.Equals(r.Codigo, ruleMeta.Code, StringComparison.OrdinalIgnoreCase));

                if (!exists)
                {
                    dbContext.ReglasValidacion.Add(new ReglaValidacion
                    {
                        TipoDocumentoId = tipoDoc.Id,
                        Codigo = ruleMeta.Code,
                        Nombre = ruleMeta.Name,
                        Descripcion = ruleMeta.Description,
                        Activa = true
                    });
                }
            }

            await dbContext.SaveChangesAsync();
        }
    }
}
