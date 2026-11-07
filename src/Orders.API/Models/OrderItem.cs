namespace Orders.API.Models;

/// <summary>
/// Representa un producto dentro de una orden.
/// </summary>
public class OrderItem
{
    public Guid ProductoId { get; set; }

    public int Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }
}