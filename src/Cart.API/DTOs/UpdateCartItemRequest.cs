using System.ComponentModel.DataAnnotations;

namespace Cart.API.DTOs;

/// <summary>
/// Datos para actualizar la cantidad de un ítem del carrito.
/// </summary>
public class UpdateCartItemRequest
{
    /// <summary>
    /// Nueva cantidad. Debe ser mayor a 0.
    /// </summary>
    /// <example>4</example>
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0.")]
    public int Cantidad { get; set; }
}
