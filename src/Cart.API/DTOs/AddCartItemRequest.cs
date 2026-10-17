using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Cart.API.DTOs;

/// <summary>
/// Datos para agregar un producto al carrito.
/// </summary>
public class AddCartItemRequest
{
    /// <summary>
    /// Identificador del producto. Requerido.
    /// [JsonRequired] exige el campo a nivel de deserialización: sin él,
    /// un Guid no nullable simplemente tomaría Guid.Empty ([Required]
    /// sobre un Guid no nullable es un no-op). Guid.Empty se rechaza
    /// además explícitamente en CartService.AddItem (CRT-002).
    /// </summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    [JsonRequired]
    public Guid ProductoId { get; set; }

    /// <summary>
    /// Cantidad a agregar. Requerida a nivel de deserialización
    /// ([JsonRequired]); su validez (mayor a 0) la verifica el servicio
    /// con CRT-004.
    /// </summary>
    /// <example>2</example>
    [JsonRequired]
    public int Cantidad { get; set; }
}
