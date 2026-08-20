namespace Products.API.Services;

using Products.API.DTOs;
using Products.API.Models;

// IProductService is the contract: it only declares what methods the product service must expose,
// without any implementation.
// ProductService is the concrete class that actually implements those methods (currently using an in-memory list).
// Separating them allows the Controller to depend only on the contract rather than the concrete implementation,
// making it easy to swap the persistence layer later and to mock the service in tests.

/// <summary>
/// Contrato del servicio de productos.
/// Contiene la lógica de negocio y acceso a datos (por ahora in-memory).
/// </summary>
public interface IProductService
{
    // Executive decision, partial filter on name as it is not specified and makes sense.
    /// <summary>
    /// Obtiene todos los productos, con filtros opcionales.
    /// </summary>
    /// <param name="categoria">Filtrar por categoría (opcional).</param>
    /// <param name="nombre">Filtrar por nombre (opcional, búsqueda parcial).</param>
    /// <returns>Lista de productos (puede estar vacía).</returns>
    IEnumerable<Product> GetAll(string? categoria = null, string? nombre = null);

    /// <summary>
    /// Obtiene un producto por su identificador.
    /// </summary>
    /// <param name="id">Identificador del producto.</param>
    /// <returns>El producto si existe.</returns>
    /// <exception cref="Exceptions.NotFoundException">Cuando el producto no existe (PRD-001).</exception>
    Product GetById(Guid id);

    /// <summary>
    /// Crea un nuevo producto.
    /// </summary>
    /// <param name="request">Datos del producto a crear.</param>
    /// <returns>El producto creado.</returns>
    /// <exception cref="Exceptions.BusinessRuleException">Cuando ya existe un producto con el mismo nombre en la categoría (PRD-003).</exception>
    Product Create(CreateProductRequest request);
}