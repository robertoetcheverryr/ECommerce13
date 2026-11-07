namespace Orders.API.DTOs;

/// <summary>
/// Representa los datos del producto obtenidos desde Products API.
/// </summary>
public class ProductResponse
{
    /// <summary>
    /// Identificador único del producto.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Precio actual del producto.
    /// </summary>
    public decimal Precio { get; set; }

    /// <summary>
    /// Cantidad de unidades disponibles en stock.
    /// </summary>
    public int Stock { get; set; }
}