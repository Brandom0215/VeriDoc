using Npgsql;

namespace Veridoc.API.Data;

/// <summary>
/// Clase utilitaria para normalizar cadenas de conexión a PostgreSQL.
/// Permite utilizar tanto el formato URI estándar de proveedores cloud como Supabase
/// (<c>postgresql://usuario:password@host:puerto/basedatos</c>) como el formato clásico clave-valor de ADO.NET/Npgsql.
/// </summary>
public static class PostgresConnectionStringHelper
{
    /// <summary>
    /// Convierte una URL de conexión tipo <c>postgresql://</c> o <c>postgres://</c> (como la entregada por Supabase)
    /// en una cadena de conexión válida para <see cref="NpgsqlConnectionStringBuilder"/> con SSL habilitado.
    /// Si la cadena ya está en formato clave-valor (<c>Host=...;Port=...</c>), la devuelve normalizada.
    /// </summary>
    /// <param name="rawConnectionString">Cadena o URL de conexión configurada.</param>
    /// <returns>Cadena de conexión compatible con Npgsql y Entity Framework Core.</returns>
    /// <exception cref="ArgumentException">Se lanza si la URL de conexión tiene un formato inválido o incompleto.</exception>
    public static string Normalize(string rawConnectionString)
    {
        if (string.IsNullOrWhiteSpace(rawConnectionString))
        {
            return string.Empty;
        }

        string trimmed = rawConnectionString.Trim();

        // Comprobar si viene en formato URI de Supabase (postgresql:// o postgres://)
        if (trimmed.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
        {
            if (!Uri.TryCreate(trimmed, UriKind.Absolute, out Uri? uri) || string.IsNullOrWhiteSpace(uri.Host))
            {
                throw new ArgumentException("La URL de conexión a PostgreSQL/Supabase no tiene un formato URI válido.");
            }

            string username = string.Empty;
            string password = string.Empty;

            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                int colonIndex = uri.UserInfo.IndexOf(':');
                if (colonIndex >= 0)
                {
                    username = Uri.UnescapeDataString(uri.UserInfo[..colonIndex]);
                    password = Uri.UnescapeDataString(uri.UserInfo[(colonIndex + 1)..]);
                }
                else
                {
                    username = Uri.UnescapeDataString(uri.UserInfo);
                }
            }

            string database = uri.AbsolutePath.Trim('/');
            if (string.IsNullOrWhiteSpace(database))
            {
                database = "postgres";
            }

            int port = uri.Port > 0 ? uri.Port : 5432;

            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = uri.Host,
                Port = port,
                Database = database,
                Username = username,
                Password = password,
                SslMode = SslMode.Require
            };

            // Si se usa el puerto 6543 del Transaction Pooler (Supavisor) de Supabase,
            // se desactiva AutoPrepare para evitar conflictos de sentencias preparadas entre conexiones del pool.
            if (port == 6543)
            {
                builder.MaxAutoPrepare = 0;
                builder.NoResetOnClose = true;
            }

            return builder.ConnectionString;
        }

        return trimmed;
    }
}
