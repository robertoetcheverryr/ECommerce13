namespace Products.API.Models;

/// <summary>
/// Producto del catálogo de e-commerce.
/// </summary>
public class Product
{
    /// <summary>
    /// Identificador único del producto.
    /// </summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid Id { get; set; }

    /// <summary>
    /// Nombre del producto. Requerido, máximo 100 caracteres.
    /// </summary>
    /// <example>Notebook Dell XPS 15</example>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Descripción opcional del producto. Máximo 500 caracteres.
    /// </summary>
    /// <example>Laptop 15 pulgadas, 32GB RAM</example>
    public string? Descripcion { get; set; }

    /// <summary>
    /// Precio unitario. Debe ser mayor a 0.
    /// </summary>
    /// <example>1500.00</example>
    public decimal Precio { get; set; }

    /// <summary>
    /// Stock disponible. Debe ser mayor o igual a 0.
    /// </summary>
    /// <example>10</example>
    public int Stock { get; set; }

    /// <summary>
    /// Categoría del producto (informativa, sin validación de lista fija).
    /// </summary>
    /// <example>Electrónica</example>
    public string Categoria { get; set; } = string.Empty;

    /// <summary>
    /// Fecha y hora de creación en UTC. Se asigna automáticamente al crear.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime FechaCreacion { get; set; }
}