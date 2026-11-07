using Orders.API.DTOs;

namespace Orders.API.Services;

/// <summary>
/// Define las operaciones para comunicarse con Products API.
/// </summary>
public interface IProductsClient
{
    /// <summary>
    /// Obtiene un producto mediante su identificador.
    /// </summary>
    /// <param name="id">Identificador del producto.</param>
    /// <param name="cancellationToken">Token para cancelar la operación.</param>
    /// <returns>
    /// El producto encontrado o null si no existe.
    /// </returns>
    Task<ProductResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}