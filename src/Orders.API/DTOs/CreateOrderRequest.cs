using System.Text.Json.Serialization;

namespace Orders.API.DTOs;

/// <summary>
/// Datos necesarios para crear una nueva orden.
/// </summary>
public class CreateOrderRequest
{
    /// <summary>
    /// Identificador del usuario que realiza la orden. Requerido.
    /// [JsonRequired] exige el campo a nivel de deserialización: sin él,
    /// un Guid no nullable simplemente tomaría Guid.Empty ([Required]
    /// sobre un Guid no nullable es un no-op). Guid.Empty se rechaza
    /// además explícitamente en OrdersService.CreateAsync (ORD-002).
    /// </summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    [JsonRequired]
    public Guid UsuarioId { get; set; }

    /// <summary>
    /// Lista de productos y cantidades incluidos en la orden. Requerida.
    /// </summary>
    [JsonRequired]
    public List<CreateOrderItemRequest> Items { get; set; } = [];
}
