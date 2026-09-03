using System.ComponentModel.DataAnnotations;

namespace Products.API.DTOs;

/// <summary>
/// Datos necesarios para crear un nuevo producto.
/// </summary>
public class CreateProductRequest
{
    /// <summary>
    /// Nombre del producto. Requerido, máximo 100 caracteres.
    /// </summary>
    /// <example>Notebook Dell XPS 15</example>
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Descripción opcional. Máximo 500 caracteres.
    /// </summary>
    /// <example>Laptop 15 pulgadas, 32GB RAM</example>
    [MaxLength(500, ErrorMessage = "La descripción no puede superar los 500 caracteres.")]
    // ? indicates it is nullable, equivalent to saying Optional[str] or str|None in Python
    public string? Descripcion { get; set; }

    /// <summary>
    /// Precio. Debe ser mayor a 0.
    /// </summary>
    /// <example>1500.00</example>
    [Required(ErrorMessage = "El precio es obligatorio.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0.")]
    public decimal Precio { get; set; }

    /// <summary>
    /// Stock disponible. Debe ser mayor o igual a 0.
    /// </summary>
    /// <example>10</example>
    [Required(ErrorMessage = "El stock es obligatorio.")]
    [Range(0, int.MaxValue, ErrorMessage = "El stock debe ser mayor o igual a 0.")]
    public int Stock { get; set; }

    /// <summary>
    /// Categoría del producto. Requerido.
    /// </summary>
    /// <example>Electrónica</example>
    [Required(ErrorMessage = "La categoría es obligatoria.")]
    public string Categoria { get; set; } = string.Empty;
}