using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Products.API.DTOs;

/// <summary>
/// Datos necesarios para actualizar un producto existente.
/// </summary>
public class UpdateProductRequest
{
    /// <summary>
    /// Nombre del producto. Requerido, máximo 100 caracteres.
    /// </summary>
    /// <example>Notebook Dell XPS 15</example>
    [JsonRequired]
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Descripción opcional. Máximo 500 caracteres.
    /// </summary>
    /// <example>Laptop 15 pulgadas, 64GB RAM</example>
    [MaxLength(500, ErrorMessage = "La descripción no puede superar los 500 caracteres.")]
    public string? Descripcion { get; set; }

    /// <summary>
    /// Precio. Requerido y mayor a 0.
    /// </summary>
    /// <example>1750.00</example>
    [JsonRequired]
    [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0.")]
    public decimal Precio { get; set; }

    /// <summary>
    /// Stock disponible. Requerido y mayor o igual a 0.
    /// </summary>
    /// <example>8</example>
    [JsonRequired]
    [Range(0, int.MaxValue, ErrorMessage = "El stock debe ser mayor o igual a 0.")]
    public int Stock { get; set; }

    /// <summary>
    /// Categoría del producto. Requerido.
    /// </summary>
    /// <example>Electrónica</example>
    [JsonRequired]
    [Required(ErrorMessage = "La categoría es obligatoria.")]
    public string Categoria { get; set; } = string.Empty;
}
