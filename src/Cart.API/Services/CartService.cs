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
        return Read(connection, usuarioId)
            ?? throw new NotFoundException(ErrorCodes.CRT_001, ErrorCodes.CRT_001_Message);
    }


    /// <inheritdoc />
    public async Task<CartModel> AddItem(Guid usuarioId, Guid productoId, int cantidad, CancellationToken cancellationToken = default)
    {
        if (cantidad <= 0)
            throw new ValidationException(ErrorCodes.CRT_004, ErrorCodes.CRT_004_Message);

        // A missing ProductoId deserializes to Guid.Empty: [Required] on a
        // non-nullable Guid is a no-op, so reject it explicitly here.
        // Why the range check lives here (service) instead of on the DTO ([Range]),
        // unlike Products.API:
        //
        // Products.API has a generic invalid-body code (PRD-002), so a blanket
        // ModelState check in the controller can map every annotation failure to it.
        // Cart.API has no generic code: the only code for a bad quantity is CRT-004,
        // which is value-specific. ModelState runs BEFORE the service, so if
        // [Range(1, ...)] were on AddCartItemRequest, a body with cantidad = 0 would
        // be caught by the controller and answered with the invalid-body 400 (empty
        // errorCode) — the service would never run and CRT-004 would never be
        // emitted, breaking the documented contract.
        //
        // So: presence is enforced at deserialization ([JsonRequired]) and answered
        // by the controller with the ErrorResponse contract and an empty errorCode
        // (spec consistency error: no CRT code exists for an invalid body, see
        // CartController.EnsureValidModel); validity (cantidad > 0) is enforced here, where the spec's
        // CRT-004 can be raised with the full error contract. Same checks as
        // Products, different layer because the error code lives there.
        if (productoId == Guid.Empty)
            throw new NotFoundException(ErrorCodes.CRT_002, ErrorCodes.CRT_002_Message);

        if (_products is null)
            throw new InvalidOperationException("Product catalog is not configured.");

        var product = await _products.GetAsync(productoId, cancellationToken);
        if (product is null)
            throw new NotFoundException(ErrorCodes.CRT_002, ErrorCodes.CRT_002_Message);

        // The Products call stays outside the write lock: a slow catalog must not
        // block every other cart write. Only the read-modify-write of the cart is locked.
        return InWriteTransaction(connection =>
        {
            var cart = Read(connection, usuarioId) ?? new CartModel { UsuarioId = usuarioId };

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

            Write(connection, cart, null);
            return cart;
        });
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

        // Same as AddItem: Products is called before taking the write lock.
        return InWriteTransaction(connection =>
        {
            var cart = Read(connection, usuarioId)
                ?? throw new NotFoundException(ErrorCodes.CRT_001, ErrorCodes.CRT_001_Message);
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
            Write(connection, cart, null);
            return cart;
        });
    }


    /// <inheritdoc />
    public void RemoveItem(Guid usuarioId, Guid productoId)
    {
        // Write rewrites every item of the cart, so the read and the rewrite share the
        // write lock: otherwise a concurrent AddItem of another product can be lost.
        InWriteTransaction(connection =>
        {
            var cart = Read(connection, usuarioId)
                ?? throw new NotFoundException(ErrorCodes.CRT_001, ErrorCodes.CRT_001_Message);
            var existing = cart.Items.FirstOrDefault(item => item.ProductoId == productoId);
            if (existing is null)
                throw new NotFoundException(ErrorCodes.CRT_001, ErrorCodes.CRT_001_Message);

            cart.Items.Remove(existing);
            Write(connection, cart, null);
            return cart;
        });
    }

    /// <inheritdoc />
    public CartModel Save(CartModel cart)
    {
        using var connection = Open();
        using var tx = connection.BeginTransaction();
        Write(connection, cart, tx);
        // Only when everything is OK we commit both header and items
        tx.Commit();
        return cart;
    }

    // Header and items commit together. A failed item write must not leave a half-updated cart.
    // tx is null when the caller already opened a raw BEGIN IMMEDIATE on the connection.
    private static void Write(SqliteConnection connection, CartModel cart, SqliteTransaction? tx)
    {
        cart.FechaActualizacion = DateTime.UtcNow;
        var usuarioId = cart.UsuarioId.ToString();

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
    }

    /// <inheritdoc />
    public void Delete(Guid usuarioId)
    {
        Get(usuarioId);
        using var connection = Open();
        using var tx = connection.BeginTransaction();
        var id = usuarioId.ToString();
        connection.Execute("DELETE FROM cart_items WHERE UsuarioId = @UsuarioId", new { UsuarioId = id }, tx);
        connection.Execute("DELETE FROM carts WHERE UsuarioId = @UsuarioId", new { UsuarioId = id }, tx);
        tx.Commit();
    }

    // BEGIN IMMEDIATE takes the write lock at the start of the transaction, same as
    // ProductService.Create. With a deferred BEGIN two concurrent requests can both
    // read the same cart and the last Write drops the other one's change (lost update,
    // or a stock check passing against a stale quantity). With IMMEDIATE the second
    // request waits (busy_timeout) and then reads the committed cart.
    private CartModel InWriteTransaction(Func<SqliteConnection, CartModel> work)
    {
        using var connection = Open();
        connection.Execute("PRAGMA busy_timeout = 5000; BEGIN IMMEDIATE;");
        try
        {
            var cart = work(connection);
            connection.Execute("COMMIT;");
            return cart;
        }
        catch
        {
            connection.Execute("ROLLBACK;");
            throw;
        }
    }

    private static CartModel? Read(SqliteConnection connection, Guid usuarioId)
    {
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
