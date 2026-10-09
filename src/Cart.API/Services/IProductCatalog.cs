namespace Cart.API.Services;

/// <summary>
/// Consulta de productos contra Products.API.
/// </summary>
public interface IProductCatalog
{
    /// <summary>
    /// Obtiene el stock de un producto.
    /// </summary>
    /// <param name="productId">Identificador del producto.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El stock si el producto existe; null si Products responde 404.</returns>
    Task<ProductStock?> GetAsync(Guid productId, CancellationToken cancellationToken = default);
}
