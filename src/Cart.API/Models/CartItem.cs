namespace Cart.API.Models;

/// <summary>
/// Producto incluido en un carrito.
/// </summary>
public class CartItem
{
    /// <summary>
    /// Identificador del producto.
    /// </summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid ProductoId { get; set; }

    /// <summary>
    /// Cantidad solicitada. Debe ser mayor a 0.
    /// </summary>
    /// <example>2</example>
    public int Cantidad { get; set; }
}
