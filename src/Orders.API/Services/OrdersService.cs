using System.Globalization;
using Dapper;
using Microsoft.Data.Sqlite;
using Orders.API.DTOs;
using Orders.API.Exceptions;
using Orders.API.Models;

namespace Orders.API.Services;

/// <summary>
/// Implementa la lógica de negocio y persistencia de las órdenes.
/// </summary>
public class OrdersService : IOrdersService
{
    // Únicas transiciones de estado permitidas para una orden.
    // Cualquier otra combinación de estado actual y estado nuevo es inválida (ORD-006).
    private static readonly HashSet<(string EstadoActual, string EstadoNuevo)> TransicionesValidas = new()
    {
        ("Pendiente", "Confirmada"),
        ("Confirmada", "Enviada"),
        ("Enviada", "Entregada"),
        ("Pendiente", "Cancelada"),
        ("Confirmada", "Cancelada")
    };

    private readonly string _connectionString;
    private readonly IProductsClient _productsClient;

    /// <summary>
    /// Inicializa el servicio de órdenes.
    /// </summary>
    /// <param name="configuration">Configuración de la aplicación.</param>
    /// <param name="productsClient">Cliente para consultar Products API.</param>
    public OrdersService(
        IConfiguration configuration,
        IProductsClient productsClient)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "No se configuró la conexión a la base de datos.");

        _productsClient = productsClient;
    }

    /// <summary>
    /// Obtiene todas las órdenes, opcionalmente filtradas por usuario.
    /// </summary>
    /// <param name="usuarioId">Identificador del usuario por el cual filtrar.</param>
    /// <param name="cancellationToken">Token para cancelar la operación.</param>
    /// <returns>Listado de órdenes con sus respectivos productos.</returns>
    public async Task<IEnumerable<Order>> GetAllAsync(
        Guid? usuarioId = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = new SqliteConnection(_connectionString);

        var sql = """
            SELECT *
            FROM orders
            WHERE (@UsuarioId IS NULL OR UsuarioId = @UsuarioId)
            ORDER BY FechaCreacion DESC;
            """;

        var rows = await connection.QueryAsync<OrderRow>(
            sql,
            new { UsuarioId = usuarioId?.ToString() });

        var orders = rows.Select(Map).ToList();

        foreach (var order in orders)
            order.Items = await GetItemsAsync(connection, order.Id);

        return orders;
    }

    /// <summary>
    /// Obtiene una orden mediante su identificador.
    /// </summary>
    /// <param name="id">Identificador de la orden.</param>
    /// <param name="cancellationToken">Token para cancelar la operación.</param>
    /// <returns>La orden encontrada con sus productos.</returns>
    /// <exception cref="OrderException">
    /// Se produce cuando la orden no existe.
    /// </exception>
    public async Task<Order> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        using var connection = new SqliteConnection(_connectionString);

        var row = await connection.QuerySingleOrDefaultAsync<OrderRow>(
            "SELECT * FROM orders WHERE Id = @Id;",
            new { Id = id.ToString() });

        if (row is null)
            throw new OrderException(
                ErrorCodes.ORD_001,
                ErrorCodes.ORD_001_Message,
                StatusCodes.Status404NotFound);

        var order = Map(row);
        order.Items = await GetItemsAsync(connection, order.Id);

        return order;
    }

    /// <summary>
    /// Crea una nueva orden y persiste sus productos.
    /// </summary>
    /// <param name="request">Datos necesarios para crear la orden.</param>
    /// <param name="cancellationToken">Token para cancelar la operación.</param>
    /// <returns>La orden creada.</returns>
    /// <exception cref="OrderException">
    /// Se produce cuando los datos son inválidos, un producto no existe
    /// o no hay stock suficiente.
    /// </exception>
    public async Task<Order> CreateAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        // The DTO's [JsonRequired] forces usuarioId to be present, but an explicit
        // 00000000-... still binds; without this guard the order persists with it and
        // silently poisons the PRD-004 active-orders checks on Products (same
        // two-layer doctrine Cart uses for productoId, see AddCartItemRequest).
        if (request.UsuarioId == Guid.Empty)
            throw new OrderException(
                ErrorCodes.ORD_002,
                ErrorCodes.ORD_002_Message,
                StatusCodes.Status400BadRequest);

        if (request.Items is null || request.Items.Count == 0)
            throw new OrderException(
                ErrorCodes.ORD_002,
                ErrorCodes.ORD_002_Message,
                StatusCodes.Status400BadRequest);

        // ORD-003: falta validar que el UsuarioId exista en Users API.
        // Queda pendiente hasta que el profesor defina si hacemos un
        // endpoint o como lo resolvemos.
        // Cuando exista, acá se debe consultar y tirar ORD-003 (404)
        // si el usuario no existe.

        var order = new Order
        {
            Id = Guid.NewGuid(),
            UsuarioId = request.UsuarioId,
            Estado = "Pendiente",
            FechaCreacion = DateTime.UtcNow
        };

        foreach (var item in request.Items)
        {
            // Cart rejects this with CRT-004; the Orders catalog has no quantity
            // code, so invalid values (0 or negative) answer as ORD-002. Checked
            // before the Products call: a bad quantity needs no catalog to reject.
            if (item.Cantidad <= 0)
                throw new OrderException(
                    ErrorCodes.ORD_002,
                    ErrorCodes.ORD_002_Message,
                    StatusCodes.Status400BadRequest);

            var product = await _productsClient.GetByIdAsync(
                item.ProductoId,
                cancellationToken);

            if (product is null)
                throw new OrderException(
                    ErrorCodes.ORD_004,
                    ErrorCodes.ORD_004_Message,
                    StatusCodes.Status404NotFound);

            if (item.Cantidad > product.Stock)
                throw new OrderException(
                    ErrorCodes.ORD_005,
                    $"Stock insuficiente para '{product.Nombre}'. " +
                    $"Disponible: {product.Stock}, solicitado: {item.Cantidad}.",
                    StatusCodes.Status422UnprocessableEntity);

            order.Items.Add(new OrderItem
            {
                ProductoId = item.ProductoId,
                Cantidad = item.Cantidad,
                PrecioUnitario = product.Precio
            });
        }

        order.Total = order.Items.Sum(
            item => item.Cantidad * item.PrecioUnitario);

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        using var transaction = connection.BeginTransaction();

        await connection.ExecuteAsync(
            """
            INSERT INTO orders (Id, UsuarioId, Total, Estado, FechaCreacion)
            VALUES (@Id, @UsuarioId, @Total, @Estado, @FechaCreacion);
            """,
            new
            {
                Id = order.Id.ToString(),
                UsuarioId = order.UsuarioId.ToString(),
                order.Total,
                order.Estado,
                FechaCreacion = order.FechaCreacion.ToString("O")
            },
            transaction);

        foreach (var item in order.Items)
        {
            await connection.ExecuteAsync(
                """
                INSERT INTO order_items
                    (OrderId, ProductoId, Cantidad, PrecioUnitario)
                VALUES
                    (@OrderId, @ProductoId, @Cantidad, @PrecioUnitario);
                """,
                new
                {
                    OrderId = order.Id.ToString(),
                    ProductoId = item.ProductoId.ToString(),
                    item.Cantidad,
                    item.PrecioUnitario
                },
                transaction);
        }

        transaction.Commit();

        return order;
    }

    /// <summary>
    /// Actualiza el estado de una orden existente.
    /// </summary>
    /// <param name="id">Identificador de la orden.</param>
    /// <param name="request">Nuevo estado solicitado.</param>
    /// <param name="cancellationToken">Token para cancelar la operación.</param>
    /// <returns>La orden con el estado actualizado.</returns>
    /// <exception cref="OrderException">
    /// Se produce cuando la orden no existe o la transición de estado es inválida.
    /// </exception>
    public async Task<Order> UpdateStatusAsync(
        Guid id,
        UpdateOrderStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var order = await GetByIdAsync(id, cancellationToken);

        if (string.IsNullOrWhiteSpace(request.Estado))
            throw new OrderException(
                ErrorCodes.ORD_002,
                ErrorCodes.ORD_002_Message,
                StatusCodes.Status400BadRequest);

        // La única transición válida es la que está en la tabla.
        // Cualquier otra (mismo estado, estado inexistente, avance o retroceso
        // no permitido) es una transición inválida (ORD-006).
        if (!TransicionesValidas.Contains((order.Estado, request.Estado)))
            throw new OrderException(
                ErrorCodes.ORD_006,
                $"Una orden en estado '{order.Estado}' no puede pasar a '{request.Estado}'.",
                StatusCodes.Status409Conflict);

        using var connection = new SqliteConnection(_connectionString);

        await connection.ExecuteAsync(
            "UPDATE orders SET Estado = @Estado WHERE Id = @Id;",
            new
            {
                Estado = request.Estado,
                Id = id.ToString()
            });

        order.Estado = request.Estado;

        return order;
    }

    private static async Task<List<OrderItem>> GetItemsAsync(
        SqliteConnection connection,
        Guid orderId)
    {
        var rows = await connection.QueryAsync<OrderItemRow>(
            """
            SELECT ProductoId, Cantidad, PrecioUnitario
            FROM order_items
            WHERE OrderId = @OrderId;
            """,
            new { OrderId = orderId.ToString() });

        return rows.Select(MapItem).ToList();
    }

    private static Order Map(OrderRow row) => new()
    {
        // Back from text to our own object
        Id = Guid.Parse(row.Id),
        UsuarioId = Guid.Parse(row.UsuarioId),
        Total = decimal.Parse(row.Total, CultureInfo.InvariantCulture),
        Estado = row.Estado,
        FechaCreacion = DateTime.Parse(
            row.FechaCreacion,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind)
    };

    private static OrderItem MapItem(OrderItemRow row) => new()
    {
        // Back from text to our own object
        ProductoId = Guid.Parse(row.ProductoId),
        Cantidad = row.Cantidad,
        PrecioUnitario = decimal.Parse(row.PrecioUnitario, CultureInfo.InvariantCulture)
    };

    private sealed class OrderRow
    {
        public string Id { get; set; } = string.Empty;
        public string UsuarioId { get; set; } = string.Empty;
        public string Total { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string FechaCreacion { get; set; } = string.Empty;
    }

    private sealed class OrderItemRow
    {
        public string ProductoId { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public string PrecioUnitario { get; set; } = string.Empty;
    }
}
