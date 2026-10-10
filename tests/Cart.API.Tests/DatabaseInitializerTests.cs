using Cart.API.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;

namespace Cart.API.Tests;

public class DatabaseInitializerTests
{
    [Fact]
    public void Initialize_ShouldOpenTheDatabase_CartTablesShouldExist()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"carts-{Guid.NewGuid():N}.db");
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

            using var carts = connection.CreateCommand();
            carts.CommandText = """
                                SELECT name FROM sqlite_master
                                WHERE type = 'table' AND name = 'carts'
                                """;
            (carts.ExecuteScalar() as string).Should().Be("carts");

            using var cartColumns = connection.CreateCommand();
            cartColumns.CommandText = """
                                      SELECT name FROM pragma_table_info('carts')
                                      ORDER BY cid
                                      """;
            using var cartReader = cartColumns.ExecuteReader();
            var columns = new List<string>();
            while (cartReader.Read())
                columns.Add(cartReader.GetString(0));

            columns.Should().Equal("UsuarioId", "FechaActualizacion");

            using var items = connection.CreateCommand();
            items.CommandText = """
                                SELECT name FROM sqlite_master
                                WHERE type = 'table' AND name = 'cart_items'
                                """;
            (items.ExecuteScalar() as string).Should().Be("cart_items");

            using var itemColumns = connection.CreateCommand();
            itemColumns.CommandText = """
                                      SELECT name FROM pragma_table_info('cart_items')
                                      ORDER BY cid
                                      """;
            using var itemReader = itemColumns.ExecuteReader();
            columns.Clear();
            while (itemReader.Read())
                columns.Add(itemReader.GetString(0));

            columns.Should().Equal("UsuarioId", "ProductoId", "Cantidad");
        }
        finally
        {
            if (File.Exists(dbPath))
                File.Delete(dbPath);
        }
    }
}
