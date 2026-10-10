namespace Cart.API.DTOs;

/// <summary>
/// Confirmación de que el carrito fue vaciado.
/// </summary>
public class ClearCartResponse
{
    /// <summary>
    /// Mensaje de confirmación.
    /// </summary>
    /// <example>Carrito vaciado exitosamente.</example>
    public string Mensaje { get; set; } = "Carrito vaciado exitosamente.";
}
