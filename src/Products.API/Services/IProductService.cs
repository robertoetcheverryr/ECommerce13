namespace Products.API.Services;

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
    /// <summary>
    /// Obtiene todos los productos.
    /// </summary>
    /// <returns>Lista de productos (puede estar vacía).</returns>
    IEnumerable<Product> GetAll();

    /// <summary>
    /// Obtiene un producto por su identificador.
    /// </summary>
    /// <param name="id">Identificador del producto.</param>
    /// <returns>El producto si existe.</returns>
    /// <exception cref="Exceptions.NotFoundException">Cuando el producto no existe (PRD-001).</exception>
    Product GetById(Guid id);
}