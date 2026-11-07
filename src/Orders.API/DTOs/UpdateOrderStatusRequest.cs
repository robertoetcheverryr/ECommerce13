namespace Orders.API.DTOs;

/// <summary>
/// Datos necesarios para actualizar el estado de una orden.
/// </summary>
public class UpdateOrderStatusRequest
{
    /// <summary>
    /// Nuevo estado que se desea asignar a la orden.
    /// </summary>
    public string Estado { get; set; } = string.Empty;
}