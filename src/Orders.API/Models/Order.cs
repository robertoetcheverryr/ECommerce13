namespace Orders.API.Models;

/// <summary>
/// Representa una orden realizada por un usuario.
/// </summary>
public class Order
{
    /// <summary>
    /// Identificador único de la orden.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Identificador del usuario que realizó la orden.
    /// </summary>
    public Guid UsuarioId { get; set; }

    /// <summary>
    /// Productos incluidos en la orden.
    /// </summary>
    public List<OrderItem> Items { get; set; } = [];

    /// <summary>
    /// Importe total de la orden.
    /// </summary>
    public decimal Total { get; set; }

    /// <summary>
    /// Estado actual de la orden.
    /// </summary>
    public string Estado { get; set; } = string.Empty;

    /// <summary>
    /// Fecha y hora de creación de la orden.
    /// </summary>
    public DateTime FechaCreacion { get; set; }
}