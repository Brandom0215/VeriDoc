using Microsoft.EntityFrameworkCore;
using Veridoc.API.Models;

namespace Veridoc.API.Data;

/// <summary>
/// Contexto de Entity Framework Core para la base de datos PostgreSQL de Veridoc API.
/// Define el mapeo relacional del catálogo de tipos documentales, reglas y el historial de validaciones.
/// </summary>
public class VeridocDbContext : DbContext
{
    /// <summary>
    /// Inicializa una nueva instancia de <see cref="VeridocDbContext"/> con las opciones configuradas.
    /// </summary>
    /// <param name="options">Opciones de configuración del contexto de base de datos.</param>
    public VeridocDbContext(DbContextOptions<VeridocDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Tabla de catálogo de tipos de documentos soportados (<c>tipos_documento</c>).
    /// </summary>
    public DbSet<TipoDocumento> TiposDocumento => Set<TipoDocumento>();

    /// <summary>
    /// Tabla de catálogo de reglas de validación por tipo documental (<c>reglas_validacion</c>).
    /// </summary>
    public DbSet<ReglaValidacion> ReglasValidacion => Set<ReglaValidacion>();

    /// <summary>
    /// Tabla de historial de ejecuciones de validación (<c>validaciones</c>).
    /// </summary>
    public DbSet<Validacion> Validaciones => Set<Validacion>();

    /// <summary>
    /// Tabla de detalle de errores detectados en cada validación (<c>detalles_error</c>).
    /// </summary>
    public DbSet<DetalleError> DetallesError => Set<DetalleError>();

    /// <summary>
    /// Configura mediante Fluent API las tablas, restricciones, índices y relaciones en PostgreSQL.
    /// </summary>
    /// <param name="modelBuilder">Constructor del modelo de entidades.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Configuración de TipoDocumento
        modelBuilder.Entity<TipoDocumento>(entity =>
        {
            entity.ToTable("tipos_documento");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Codigo)
                .IsRequired()
                .HasMaxLength(64);

            entity.Property(e => e.Nombre)
                .IsRequired()
                .HasMaxLength(150);

            entity.HasIndex(e => e.Codigo)
                .IsUnique();
        });

        // 2. Configuración de ReglaValidacion
        modelBuilder.Entity<ReglaValidacion>(entity =>
        {
            entity.ToTable("reglas_validacion");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Codigo)
                .IsRequired()
                .HasMaxLength(80);

            entity.Property(e => e.Nombre)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(e => e.Descripcion)
                .IsRequired()
                .HasMaxLength(500);

            entity.HasIndex(e => new { e.TipoDocumentoId, e.Codigo })
                .IsUnique();

            entity.HasOne(e => e.TipoDocumento)
                .WithMany(t => t.Reglas)
                .HasForeignKey(e => e.TipoDocumentoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 3. Configuración de Validacion (Auditoría/Historial sin datos personales)
        modelBuilder.Entity<Validacion>(entity =>
        {
            entity.ToTable("validaciones");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.CodigoTipoDocumento)
                .IsRequired()
                .HasMaxLength(64);

            entity.Property(e => e.CorrelationId)
                .IsRequired()
                .HasMaxLength(64);

            entity.HasIndex(e => e.FechaValidacionUtc);
            entity.HasIndex(e => e.CorrelationId);

            entity.HasOne(e => e.TipoDocumento)
                .WithMany(t => t.Validaciones)
                .HasForeignKey(e => e.TipoDocumentoId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // 4. Configuración de DetalleError
        modelBuilder.Entity<DetalleError>(entity =>
        {
            entity.ToTable("detalles_error");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Campo)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.CodigoRegla)
                .IsRequired()
                .HasMaxLength(80);

            entity.Property(e => e.Mensaje)
                .IsRequired()
                .HasMaxLength(500);

            entity.HasOne(e => e.Validacion)
                .WithMany(v => v.DetallesError)
                .HasForeignKey(e => e.ValidacionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
