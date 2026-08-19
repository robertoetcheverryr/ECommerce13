namespace Products.API.Models;

/// <summary>
/// Producto del catálogo de e-commerce.
/// </summary>
public class Product
{
    /// <summary>
    /// Identificador único del producto.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Nombre del producto. Requerido, máximo 100 caracteres.
    /// </summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Descripción opcional del producto. Máximo 500 caracteres.
    /// </summary>
    public string? Descripcion { get; set; }

    /// <summary>
    /// Precio unitario. Debe ser mayor a 0.
    /// </summary>
    public decimal Precio { get; set; }

    /// <summary>
    /// Stock disponible. Debe ser mayor o igual a 0.
    /// </summary>
    public int Stock { get; set; }

    /// <summary>
    /// Categoría del producto (informativa, sin validación de lista fija).
    /// </summary>
    public string Categoria { get; set; } = string.Empty;

    /// <summary>
    /// Fecha y hora de creación en UTC. Se asigna automáticamente al crear.
    /// </summary>
    public DateTime FechaCreacion { get; set; }
}