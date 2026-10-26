using Dapper;
using Microsoft.Data.Sqlite;

namespace Notifications.API.Services;

/// <summary>
/// Inicializa la base de datos de notificaciones.
/// </summary>
public class DatabaseInitializer
{
    private readonly string _connectionString;

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