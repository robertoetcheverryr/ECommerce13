using System.Globalization;
using Cart.API.Exceptions;
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
    private readonly IProductCatalog? _products;

    /// <summary>
    /// Constructor. Inyecta la cadena de conexión y, si hay llamadas a Products, el catálogo.
    /// </summary>
    /// <param name="connectionString">Cadena de conexión de SQLite.</param>
    /// <param name="products">Cliente de Products. Null en los tests de persistencia.</param>
    public CartService(string connectionString, IProductCatalog? products = null)
    {
        _connectionString = connectionString;
        _products = products;
    }

    /// <inheritdoc />
    public CartModel Get(Guid usuarioId)
    {
        using var connection = Open();
        var header = connection.QuerySingleOrDefault<CartRow>("""
            SELECT UsuarioId, FechaActualizacion
            FROM carts
            WHERE UsuarioId = @UsuarioId
            """, new { UsuarioId = usuarioId.ToString() });

        if (header is null)
            throw new NotFoundException(ErrorCodes.CRT_001, ErrorCodes.CRT_001_Message);

        var items = connection.Query<CartItemRow>("""
            SELECT ProductoId, Cantidad
            FROM cart_items
            WHERE UsuarioId = @UsuarioId
            ORDER BY ProductoId
            """, new { UsuarioId = usuarioId.ToString() });

        return Map(header, items);
    }


    /// <inheritdoc />
    public async Task<CartModel> AddItem(Guid usuarioId, Guid productoId, int cantidad, CancellationToken cancellationToken = default)
    {
        if (cantidad <= 0)
            throw new ValidationException(ErrorCodes.CRT_004, ErrorCodes.CRT_004_Message);

        if (_products is null)
            throw new InvalidOperationException("Product catalog is not configured.");

        var product = await _products.GetAsync(productoId, cancellationToken);
        if (product is null)
            throw new NotFoundException(ErrorCodes.CRT_002, ErrorCodes.CRT_002_Message);

        CartModel cart;
        try
        {
            cart = Get(usuarioId);
        }
        catch (NotFoundException)
        {
            cart = new CartModel { UsuarioId = usuarioId };
        }

        var existing = cart.Items.FirstOrDefault(item => item.ProductoId == productoId);
        var requested = cantidad + (existing?.Cantidad ?? 0);
        if (requested > product.Stock)
        {
            throw new BusinessRuleException(
                ErrorCodes.CRT_003,
                $"Stock insuficiente. Disponible: {product.Stock}, solicitado: {requested}.",
                ErrorCodes.CRT_003_Detail,
                StatusCodes.Status422UnprocessableEntity);
        }

        if (existing is null)
            cart.Items.Add(new CartItem { ProductoId = productoId, Cantidad = cantidad });
        else
            existing.Cantidad = requested;

        return Save(cart);
    }


    /// <inheritdoc />
    public async Task<CartModel> UpdateItem(Guid usuarioId, Guid productoId, int cantidad, CancellationToken cancellationToken = default)
    {
        if (cantidad <= 0)
            throw new ValidationException(ErrorCodes.CRT_004, ErrorCodes.CRT_004_Message);

        if (_products is null)
            throw new InvalidOperationException("Product catalog is not configured.");

        var product = await _products.GetAsync(productoId, cancellationToken);
        if (product is null)
            throw new NotFoundException(ErrorCodes.CRT_002, ErrorCodes.CRT_002_Message);

        var cart = Get(usuarioId);
        var existing = cart.Items.FirstOrDefault(item => item.ProductoId == productoId);
        if (existing is null)
            throw new NotFoundException(ErrorCodes.CRT_001, ErrorCodes.CRT_001_Message);

        // PUT replaces the quantity. Stock is checked against that value, not added to it.
        if (cantidad > product.Stock)
        {
            throw new BusinessRuleException(
                ErrorCodes.CRT_003,
                $"Stock insuficiente. Disponible: {product.Stock}, solicitado: {cantidad}.",
                ErrorCodes.CRT_003_Detail,
                StatusCodes.Status422UnprocessableEntity);
        }

        existing.Cantidad = cantidad;
        return Save(cart);
    }

    /// <inheritdoc />
    public CartModel Save(CartModel cart)
    {
        cart.FechaActualizacion = DateTime.UtcNow;
        var usuarioId = cart.UsuarioId.ToString();

        using var connection = Open();
        using var tx = connection.BeginTransaction();

        // Header and items commit together. A failed item write must not leave a half-updated cart.
        // Try to insert the entire cart, if it already exists (by UsuarioId as PK) update
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
        
        // Drop all items from the saved cart
        connection.Execute(
            "DELETE FROM cart_items WHERE UsuarioId = @UsuarioId",
            new { UsuarioId = usuarioId },
            tx);

        // Insert the current cart we have in memory
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
        
        // Only when everything is OK we commit both header and items
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
