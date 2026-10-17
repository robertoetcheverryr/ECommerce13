using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Cart.API.DTOs;

/// <summary>
/// Datos para actualizar la cantidad de un ítem del carrito.
/// </summary>
public class UpdateCartItemRequest
{
    /// <summary>
    /// Nueva cantidad. Requerida a nivel de deserialización
    /// ([JsonRequired]); su validez (mayor a 0) la verifica el servicio
    /// con CRT-004.
    /// </summary>
    /// <example>3</example>
    [JsonRequired]
    public int Cantidad { get; set; }
}
