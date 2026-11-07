using Orders.API.DTOs;
using Orders.API.Models;
using Swashbuckle.AspNetCore.Filters;

namespace Orders.API.Swagger;

/// <summary>
/// Proporciona ejemplos de requests y responses para Swagger.
/// </summary>
public class CreateOrderRequestExample : IExamplesProvider<CreateOrderRequest>
{
    /// <summary>
    /// Obtiene el ejemplo correspondiente.
    /// </summary>
    public CreateOrderRequest GetExamples() => new()
    {
        UsuarioId = Guid.Parse("a1b2c3d4-0000-0000-0000-111122223333"),
        Items =
        [
            new CreateOrderItemRequest
            {
                ProductoId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
                Cantidad = 2
            }
        ]
    };
}

/// <summary>
/// Proporciona un ejemplo de orden.
/// </summary>
public class OrderResponseExample : IExamplesProvider<Order>
{
    /// <summary>
    /// Obtiene el ejemplo correspondiente.
    /// </summary>
    public Order GetExamples() => new()
    {
        Id = Guid.Parse("f1e2d3c4-0000-0000-0000-aabbccddeeff"),
        UsuarioId = Guid.Parse("a1b2c3d4-0000-0000-0000-111122223333"),
        Items =
        [
            new OrderItem
            {
                ProductoId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
                Cantidad = 2,
                PrecioUnitario = 1500.00m
            }
        ],
        Total = 3000.00m,
        Estado = "Pendiente",
        FechaCreacion = new DateTime(2024, 3, 10, 11, 0, 0, DateTimeKind.Utc)
    };
}

/// <summary>
/// Proporciona un ejemplo de lista de órdenes.
/// </summary>
public class OrdersResponseExample : IExamplesProvider<List<Order>>
{
    /// <summary>
    /// Obtiene el ejemplo correspondiente.
    /// </summary>
    public List<Order> GetExamples() =>
    [
        new OrderResponseExample().GetExamples()
    ];
}

/// <summary>
/// Proporciona un ejemplo de error de datos inválidos.
/// </summary>
public class ValidationErrorExample : IExamplesProvider<object>
{
    /// <summary>
    /// Obtiene el ejemplo correspondiente.
    /// </summary>
    public object GetExamples() => new
    {
        type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        title = "Bad Request",
        status = 400,
        detail = "Los datos de la orden son inválidos.",
        instance = "/api/orders",
        errorCode = "ORD-002",
        errorMessage = "Los datos de la orden son inválidos.",
        correlationId = "example-correlation-id"
    };
}

/// <summary>
/// Proporciona un ejemplo de orden no encontrada.
/// </summary>
public class OrderNotFoundErrorExample : IExamplesProvider<object>
{
    /// <summary>
    /// Obtiene el ejemplo correspondiente.
    /// </summary>
    public object GetExamples() => new
    {
        type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
        title = "Not Found",
        status = 404,
        detail = "Orden no encontrada.",
        instance = "/api/orders/f1e2d3c4-0000-0000-0000-aabbccddeeff",
        errorCode = "ORD-001",
        errorMessage = "Orden no encontrada.",
        correlationId = "example-correlation-id"
    };
}

/// <summary>
/// Proporciona un ejemplo de producto no encontrado.
/// </summary>
public class ProductNotFoundErrorExample : IExamplesProvider<object>
{
    /// <summary>
    /// Obtiene el ejemplo correspondiente.
    /// </summary>
    public object GetExamples() => new
    {
        type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
        title = "Not Found",
        status = 404,
        detail = "Producto no encontrado.",
        instance = "/api/orders",
        errorCode = "ORD-004",
        errorMessage = "Producto no encontrado.",
        correlationId = "example-correlation-id"
    };
}

/// <summary>
/// Proporciona un ejemplo de stock insuficiente.
/// </summary>
public class StockInsufficientExample : IExamplesProvider<object>
{
    /// <summary>
    /// Obtiene el ejemplo correspondiente.
    /// </summary>
    public object GetExamples() => new
    {
        type = "https://tools.ietf.org/html/rfc4918#section-11.2",
        title = "Unprocessable Entity",
        status = 422,
        detail = "Stock insuficiente para 'Notebook Dell XPS 15'. Disponible: 2, solicitado: 5.",
        instance = "/api/orders",
        errorCode = "ORD-005",
        errorMessage = "Stock insuficiente para 'Notebook Dell XPS 15'. Disponible: 2, solicitado: 5.",
        correlationId = "example-correlation-id"
    };
}

/// <summary>
/// Proporciona un ejemplo de transición de estado inválida.
/// </summary>
public class InvalidTransitionExample : IExamplesProvider<object>
{
    /// <summary>
    /// Obtiene el ejemplo correspondiente.
    /// </summary>
    public object GetExamples() => new
    {
        type = "https://tools.ietf.org/html/rfc7231#section-6.5.9",
        title = "Conflict",
        status = 409,
        detail = "Una orden en estado 'Entregada' no puede volver a 'Pendiente'.",
        instance = "/api/orders/f1e2d3c4-0000-0000-0000-aabbccddeeff/status",
        errorCode = "ORD-006",
        errorMessage = "Una orden en estado 'Entregada' no puede volver a 'Pendiente'.",
        correlationId = "example-correlation-id"
    };
}

/// <summary>
/// Proporciona un ejemplo de error interno del servidor.
/// </summary>
public class InternalServerErrorExample : IExamplesProvider<object>
{
    /// <summary>
    /// Obtiene el ejemplo correspondiente.
    /// </summary>
    public object GetExamples() => new
    {
        type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
        title = "Internal Server Error",
        status = 500,
        detail = "Error interno al procesar la orden.",
        instance = "/api/orders",
        errorCode = "ORD-007",
        errorMessage = "Error interno al procesar la orden.",
        correlationId = "example-correlation-id"
    };
}
