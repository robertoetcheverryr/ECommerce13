using Dapper;
using Microsoft.Data.Sqlite;

namespace Products.API.Services;

/// <summary>
/// Crea la base SQLite de productos si todavía no existe.
/// </summary>
public class DatabaseInitializer
{
    private readonly string _connectionString;

    /// <summary>
    /// Inicializa el acceso con la cadena de conexión indicada.
    /// </summary>
    /// <param name="connectionString">Cadena de conexión de SQLite.</param>
    public DatabaseInitializer(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>
    /// Abre la base y crea la tabla products con las columnas del producto.
    /// </summary>
    public void Initialize()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        // Columns match Product. No extra fields. Guid, money and date are text so a dropped file is not coerced.
        connection.Execute("""
            CREATE TABLE IF NOT EXISTS products (
                Id TEXT NOT NULL PRIMARY KEY,
                Nombre TEXT NOT NULL,
                Descripcion TEXT,
                Precio TEXT NOT NULL,
                Stock INTEGER NOT NULL,
                Categoria TEXT NOT NULL,
                FechaCreacion TEXT NOT NULL
            );
            """);
    }
}
