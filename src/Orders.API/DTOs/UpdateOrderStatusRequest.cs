using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Orders.API.DTOs;

/// <summary>
/// Datos necesarios para actualizar el estado de una orden.
/// </summary>
public class UpdateOrderStatusRequest
{
    /// <summary>
    /// Nuevo estado que se desea asignar a la orden.
    /// [JsonRequired] lo declara requerido en el OpenAPI (igual que usuarioId
    /// e items en CreateOrderRequest): ausente caía en string.Empty y el
    /// service lo respondía ORD-002, pero el schema lo dejaba pasar.
    /// [RegularExpression] declara "al menos un carácter no vacío", la misma
    /// regla que IsNullOrWhiteSpace en OrdersService (ORD-002). El patrón es
    /// "[\s\S]*[no-blanco][\s\S]*" con la clase de espacios explícita y
    /// anclable: MVC ancla la regex al inicio/fin y su "." no cruza \n
    /// (".*\S.*" fallaba en textos multilínea); la clase excluye los
    /// espacios de .NET (incluye \0-\x20 y C1) y de Python, o sea todo lo
    /// que IsNullOrWhiteSpace también considera vacío.
    /// </summary>
    [JsonRequired]
    [RegularExpression(@"[\s\S]*[^\u0000-\u0020\u007f-\u009f\u00a0\u1680\u2000-\u200a\u2028\u2029\u202f\u205f\u3000\ufeff][\s\S]*",
        ErrorMessage = "El estado es obligatorio.")]
    public string Estado { get; set; } = string.Empty;
}
