using FluentAssertions;
using Microsoft.Data.Sqlite;
using Products.API.Services;

namespace Products.API.Tests;

public class DatabaseInitializerTests
{
    [Fact]
    public void Initialize_ShouldOpenTheDatabase_ProductsTableShouldExist()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"products-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath}";

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
                                WHERE type = 'table' AND name = 'products'
                                """;
            (table.ExecuteScalar() as string).Should().Be("products");

            using var info = connection.CreateCommand();
            info.CommandText = """
                               SELECT name FROM pragma_table_info('products') 
                               ORDER BY cid
                               """;
            using var reader = info.ExecuteReader();
            var columns = new List<string>();
            while (reader.Read())
                columns.Add(reader.GetString(0));

            columns.Should().Equal(
                "Id",
                "Nombre",
                "Descripcion",
                "Precio",
                "Stock",
                "Categoria",
                "FechaCreacion");
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath))
                File.Delete(dbPath);
        }
    }
}
