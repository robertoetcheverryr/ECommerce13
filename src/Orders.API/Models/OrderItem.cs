namespace Orders.API.Models;

/// <summary>
/// Representa un producto dentro de una orden.
/// </summary>
public class OrderItem
{
    /// <summary>
    /// Identificador del producto.
    /// </summary>
    public Guid ProductoId { get; set; }

    /// <summary>
    /// Cantidad de unidades del producto.
    /// </summary>
    public int Cantidad { get; set; }

    /// <summary>
    /// Precio unitario del producto al momento de crear la orden.
    /// </summary>
    public decimal PrecioUnitario { get; set; }
}