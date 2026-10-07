using Dapper;
using Microsoft.Data.Sqlite;

namespace Users.API.Services;

/// <summary>
/// Crea la base SQLite de usuarios si todavía no existe.
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
    /// Abre la base y crea la tabla users con las columnas del usuario.
    /// </summary>
    public void Initialize()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        // Columns match User. No extra fields. Guid and date are text so a dropped file is not coerced.
        // Activo and IntentosFallidos stay INTEGER, the SQLite shape for bool and int.
        connection.Execute("""
            CREATE TABLE IF NOT EXISTS users (
                Id TEXT NOT NULL PRIMARY KEY,
                Nombre TEXT NOT NULL,
                Apellido TEXT NOT NULL,
                Email TEXT NOT NULL,
                PasswordHash TEXT NOT NULL,
                FechaRegistro TEXT NOT NULL,
                Activo INTEGER NOT NULL,
                IntentosFallidos INTEGER NOT NULL
            );
            """);
    }
}
