namespace Cart.API.Models;

/// <summary>
/// Carrito de un usuario.
/// </summary>
public class Cart
{
    /// <summary>
    /// Identificador del usuario dueño del carrito.
    /// </summary>
    /// <example>a1b2c3d4-0000-0000-0000-111122223333</example>
    public Guid UsuarioId { get; set; }

    /// <summary>
    /// Productos incluidos en el carrito.
    /// </summary>
    public List<CartItem> Items { get; set; } = [];

    /// <summary>
    /// Fecha y hora de la última actualización en UTC. Se asigna automáticamente en cada operación.
    /// </summary>
    /// <example>2024-03-10T10:45:00Z</example>
    public DateTime FechaActualizacion { get; set; }
}
