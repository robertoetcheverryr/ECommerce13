using FluentAssertions;
using Microsoft.Data.Sqlite;
using Users.API.Services;

namespace Users.API.Tests;

public class DatabaseInitializerTests
{
    [Fact]
    public void Initialize_ShouldOpenTheDatabase_UsersTableShouldExist()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"users-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath};Pooling=false";

        try
        {
            new DatabaseInitializer(connectionString).Initialize();

            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            using var ping = connection.CreateCommand();
            // Basic check that the database is open
            ping.CommandText = "SELECT 1";
            Convert.ToInt32(ping.ExecuteScalar()).Should().Be(1);

            using var table = connection.CreateCommand();
            table.CommandText = """
                                SELECT name FROM sqlite_master 
                                WHERE type = 'table' AND name = 'users'
                                """;
            (table.ExecuteScalar() as string).Should().Be("users");

            using var info = connection.CreateCommand();
            info.CommandText = """
                               SELECT name FROM pragma_table_info('users') 
                               ORDER BY cid
                               """;
            using var reader = info.ExecuteReader();
            var columns = new List<string>();
            while (reader.Read())
                columns.Add(reader.GetString(0));

            columns.Should().Equal(
                "Id",
                "Nombre",
                "Apellido",
                "Email",
                "PasswordHash",
                "FechaRegistro",
                "Activo",
                "IntentosFallidos");
        }
        finally
        {
            if (File.Exists(dbPath))
                File.Delete(dbPath);
        }
    }
}
