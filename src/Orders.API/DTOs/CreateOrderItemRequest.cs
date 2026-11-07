namespace Orders.API.DTOs;

/// <summary>
/// Representa un producto solicitado al crear una orden.
/// </summary>
public class CreateOrderItemRequest
{
    /// <summary>
    /// Identificador del producto.
    /// </summary>
    public Guid ProductoId { get; set; }

    /// <summary>
    /// Cantidad de unidades solicitadas.
    /// </summary>
    public int Cantidad { get; set; }
}