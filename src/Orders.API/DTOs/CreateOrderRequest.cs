namespace Orders.API.DTOs;

/// <summary>
/// Datos necesarios para crear una nueva orden.
/// </summary>
public class CreateOrderRequest
{
    /// <summary>
    /// Identificador del usuario que realiza la orden.
    /// </summary>
    public Guid UsuarioId { get; set; }

    /// <summary>
    /// Lista de productos y cantidades incluidos en la orden.
    /// </summary>
    public List<CreateOrderItemRequest> Items { get; set; } = [];
}