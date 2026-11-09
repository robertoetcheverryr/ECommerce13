using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Orders.API.DTOs;

/// <summary>
/// Representa un producto solicitado al crear una orden.
/// </summary>
public class CreateOrderItemRequest
{
    /// <summary>
    /// Identificador del producto. Requerido.
    /// [JsonRequired] exige el campo a nivel de deserialización: sin él,
    /// un Guid no nullable simplemente tomaría Guid.Empty ([Required]
    /// sobre un Guid no nullable es un no-op). Un Guid.Empty explícito
    /// resuelve como producto inexistente (ORD-004).
    /// </summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    [JsonRequired]
    public Guid ProductoId { get; set; }

    /// <summary>
    /// Cantidad de unidades solicitadas. Requerida a nivel de
    /// deserialización ([JsonRequired]). [Range(1, ...)] declara el
    /// "mayor a 0" del Apéndice A del TP: 0 y negativos ya se respondían
    /// ORD-002 en el service, pero el schema no lo indicaba.
    /// </summary>
    /// <example>2</example>
    [JsonRequired]
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0.")]
    public int Cantidad { get; set; }
}
