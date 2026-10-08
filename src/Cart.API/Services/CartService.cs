using System.Globalization;
using Cart.API.Models;
using Dapper;
using Microsoft.Data.Sqlite;

namespace Cart.API.Services;
using CartModel = global::Cart.API.Models.Cart;
/// <summary>
/// Implementación del servicio de carrito.
/// Persistencia en SQLite a través de Dapper.
/// </summary>
public class CartService : ICartService
{
    private readonly string _connectionString;

    /// <summary>
    /// Constructor. Inyecta la cadena de conexión.
    /// </summary>
    /// <param name="connectionString">Cadena de conexión de SQLite.</param>
    public CartService(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <inheritdoc />
    public CartModel? Get(Guid usuarioId)
    {
        using var connection = Open();
        var header = connection.QuerySingleOrDefault<CartRow>("""
            SELECT UsuarioId, FechaActualizacion
            FROM carts
            WHERE UsuarioId = @UsuarioId
            """, new { UsuarioId = usuarioId.ToString() });

        if (header is null)
            return null;

        var items = connection.Query<CartItemRow>("""
            SELECT ProductoId, Cantidad
            FROM cart_items
            WHERE UsuarioId = @UsuarioId
            ORDER BY ProductoId
            """, new { UsuarioId = usuarioId.ToString() });

        return Map(header, items);
    }

    /// <inheritdoc />
    public CartModel Save(CartModel cart)
    {
        cart.FechaActualizacion = DateTime.UtcNow;
        var usuarioId = cart.UsuarioId.ToString();

        using var connection = Open();
        using var tx = connection.BeginTransaction();

        // Header and items commit together. A failed item write must not leave a half-updated cart.
        connection.Execute("""
            INSERT INTO carts (UsuarioId, FechaActualizacion)
            VALUES (@UsuarioId, @FechaActualizacion)
            ON CONFLICT(UsuarioId) DO UPDATE SET
                FechaActualizacion = excluded.FechaActualizacion
            """, new
        {
            UsuarioId = usuarioId,
            FechaActualizacion = cart.FechaActualizacion.ToString("o", CultureInfo.InvariantCulture)
        }, tx);

        connection.Execute(
            "DELETE FROM cart_items WHERE UsuarioId = @UsuarioId",
            new { UsuarioId = usuarioId },
            tx);

        foreach (var item in cart.Items)
        {
            connection.Execute("""
                INSERT INTO cart_items (UsuarioId, ProductoId, Cantidad)
                VALUES (@UsuarioId, @ProductoId, @Cantidad)
                """, new
            {
                UsuarioId = usuarioId,
                ProductoId = item.ProductoId.ToString(),
                item.Cantidad
            }, tx);
        }

        tx.Commit();
        return cart;
    }

    /// <inheritdoc />
    public void Delete(Guid usuarioId)
    {
        using var connection = Open();
        using var tx = connection.BeginTransaction();
        var id = usuarioId.ToString();
        connection.Execute("DELETE FROM cart_items WHERE UsuarioId = @UsuarioId", new { UsuarioId = id }, tx);
        connection.Execute("DELETE FROM carts WHERE UsuarioId = @UsuarioId", new { UsuarioId = id }, tx);
        tx.Commit();
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private static CartModel Map(CartRow header, IEnumerable<CartItemRow> items) => new()
    {
        UsuarioId = Guid.Parse(header.UsuarioId),
        FechaActualizacion = DateTime.Parse(header.FechaActualizacion, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
        Items = items.Select(item => new CartItem
        {
            ProductoId = Guid.Parse(item.ProductoId),
            Cantidad = item.Cantidad
        }).ToList()
    };

    private sealed class CartRow
    {
        public string UsuarioId { get; set; } = string.Empty;
        public string FechaActualizacion { get; set; } = string.Empty;
    }

    private sealed class CartItemRow
    {
        public string ProductoId { get; set; } = string.Empty;
        public int Cantidad { get; set; }
    }
}
