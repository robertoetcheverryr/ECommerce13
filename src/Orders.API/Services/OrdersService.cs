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

        var rows = await connection.QueryAsync<Order>(
            sql,
            new { UsuarioId = usuarioId?.ToString() });

        var orders = rows.ToList();

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

        var order = await connection.QuerySingleOrDefaultAsync<Order>(
            "SELECT * FROM orders WHERE Id = @Id;",
            new { Id = id.ToString() });

        if (order is null)
            throw new OrderException(
                ErrorCodes.ORD_001,
                ErrorCodes.ORD_001_Message,
                StatusCodes.Status404NotFound);

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
        if (request.Items is null || request.Items.Count == 0)
            throw new OrderException(
                ErrorCodes.ORD_002,
                ErrorCodes.ORD_002_Message,
                StatusCodes.Status400BadRequest);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            UsuarioId = request.UsuarioId,
            Estado = "Pendiente",
            FechaCreacion = DateTime.UtcNow
        };

        foreach (var item in request.Items)
        {
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
                    ErrorCodes.ORD_005_Message,
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

        if (order.Estado == "Entregada" && request.Estado == "Pendiente")
            throw new OrderException(
                ErrorCodes.ORD_006,
                ErrorCodes.ORD_006_Message,
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
        var items = await connection.QueryAsync<OrderItem>(
            """
            SELECT ProductoId, Cantidad, PrecioUnitario
            FROM order_items
            WHERE OrderId = @OrderId;
            """,
            new { OrderId = orderId.ToString() });

        return items.ToList();
    }
}
