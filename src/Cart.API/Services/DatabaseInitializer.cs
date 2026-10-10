using Dapper;
using Microsoft.Data.Sqlite;

namespace Cart.API.Services;

/// <summary>
/// Crea la base SQLite del carrito si todavía no existe.
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
    /// Abre la base y crea las tablas carts y cart_items con las columnas del carrito.
    /// </summary>
    public void Initialize()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        // Columns match Cart and CartItem. No extra fields. Guid and date are text so a dropped file is not coerced.
        // Items are a child table: UsuarioId is the owner key, not a column of CartItem.
        // Not really liking this, as the cartId should not be the UsuarioId but good enough for the TP.
        connection.Execute("""
            CREATE TABLE IF NOT EXISTS carts (
                UsuarioId TEXT NOT NULL PRIMARY KEY,
                FechaActualizacion TEXT NOT NULL
            );
            """);

        connection.Execute("""
            CREATE TABLE IF NOT EXISTS cart_items (
                UsuarioId TEXT NOT NULL,
                ProductoId TEXT NOT NULL,
                Cantidad INTEGER NOT NULL,
                PRIMARY KEY (UsuarioId, ProductoId)
            );
            """);
    }
}
