using Dapper;
using Microsoft.Data.Sqlite;

namespace Orders.API;

/// <summary>
/// Crea la base SQLite de órdenes si todavía no existe.
/// </summary>
public class DatabaseInitializer
{
    private readonly string _connectionString;

    /// <summary>
    /// Inicializa el componente encargado de crear la base de datos.
    /// </summary>
    /// <param name="connectionString">
    /// Cadena de conexión a la base de datos SQLite.
    /// </param>
    public DatabaseInitializer(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>
    /// Inicializa la base de datos y crea las tablas necesarias.
    /// </summary>
    public void Initialize()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        connection.Execute("""
                           CREATE TABLE IF NOT EXISTS orders (
                               Id TEXT NOT NULL PRIMARY KEY,
                               UsuarioId TEXT NOT NULL,
                               Total TEXT NOT NULL,
                               Estado TEXT NOT NULL,
                               FechaCreacion TEXT NOT NULL
                           );

                           CREATE TABLE IF NOT EXISTS order_items (
                               Id INTEGER PRIMARY KEY AUTOINCREMENT,
                               OrderId TEXT NOT NULL,
                               ProductoId TEXT NOT NULL,
                               Cantidad INTEGER NOT NULL,
                               PrecioUnitario TEXT NOT NULL,
                               FOREIGN KEY (OrderId) REFERENCES orders(Id)
                           );
                           """);
    }
}