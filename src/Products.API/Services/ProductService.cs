namespace Products.API.Services;

using Products.API.Exceptions;
using Products.API.Models;

/// <summary>
/// Implementación del servicio de productos.
/// Persistencia temporal in-memory hasta que la cátedra entregue la librería.
/// </summary>
public class ProductService : IProductService
{
    // Store in-memory compartido. En el futuro se reemplazará por la librería de persistencia.
    private static readonly List<Product> Products =
    [
        new Product
        {
            Id = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
            Nombre = "Notebook Dell XPS 15",
            Descripcion = "Laptop 15 pulgadas, 32GB RAM",
            Precio = 1500.00m,
            Stock = 10,
            Categoria = "Electrónica",
            FechaCreacion = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc)
        }
    ];

    /// <inheritdoc />
    public IEnumerable<Product> GetAll()
    {
        return Products;
    }
    
    // inheritdoc tells doc tools and IntelliSense to copy the docs from the member being implemented
    // or overriden
    /// <inheritdoc />
    public Product GetById(Guid id)
    {
        var product = Products.FirstOrDefault(p => p.Id == id);

        if (product is null)
            throw new NotFoundException("PRD-001", "Producto no encontrado.");

        return product;
    }
}