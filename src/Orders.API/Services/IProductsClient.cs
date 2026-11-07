using Orders.API.DTOs;

namespace Orders.API.Services;

public interface IProductsClient
{
    Task<ProductResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}