using Dapper;
using Microsoft.Data.Sqlite;

namespace Notifications.API.Services;

/// <summary>
/// Inicializa la estructura de la base de datos de notificaciones.
/// </summary>
public class DatabaseInitializer
{
    private readonly string _connectionString;

    /// <summary>
    /// Inicializa el acceso a la base de datos.
    /// </summary>
    /// <param name="connectionString">Cadena de conexión a SQLite.</param>
    public DatabaseInitializer(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>
    /// Crea la tabla de notificaciones si todavía no existe.
    /// </summary>
    public void Initialize()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        connection.Execute("""
                           CREATE TABLE IF NOT EXISTS notifications (
                               Id TEXT NOT NULL PRIMARY KEY,
                               UsuarioId TEXT NOT NULL,
                               Mensaje TEXT NOT NULL,
                               Tipo TEXT NOT NULL,
                               Estado TEXT NOT NULL,
                               FechaEnvio TEXT NOT NULL
                           );
                           """);
    }
}