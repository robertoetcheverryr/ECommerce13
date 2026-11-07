namespace Orders.API.Models;

/// <summary>
/// Representa una orden de compra.
/// </summary>
public class Order
{
    public Guid Id { get; set; }

    public Guid UsuarioId { get; set; }

    public List<OrderItem> Items { get; set; } = [];

    public decimal Total { get; set; }

    public string Estado { get; set; } = string.Empty;

    public DateTime FechaCreacion { get; set; }
}