using Cart.API.Exceptions;
using Cart.API.Services;

namespace Cart.API.Tests;

public class QuantityOverflowTests
{
    [Theory]
    [InlineData(int.MaxValue, 1)]
    [InlineData(int.MaxValue - 1, 2)]
    [InlineData(1, int.MaxValue)]
    [InlineData(int.MaxValue, int.MaxValue)]
    public async Task AddItem_AccumulatedQuantityOverflow_ReturnsCrt004AndPreservesCart(int first, int second)
    {
        var path = Path.Combine(Path.GetTempPath(), $"cart-overflow-{Guid.NewGuid():N}.db");
        var connection = $"Data Source={path};Pooling=false";
        var user = Guid.NewGuid();
        var product = Guid.NewGuid();
        try
        {
            new DatabaseInitializer(connection).Initialize();
            var service = new CartService(connection, new MaximumStockCatalog());
            var original = await service.AddItem(user, product, first);
            ValidationException? failure = null;
            try { await service.AddItem(user, product, second); }
            catch (ValidationException error) { failure = error; }
            Assert.NotNull(failure);
            Assert.Equal(ErrorCodes.CRT_004, failure.ErrorCode);
            var loaded = new CartService(connection).Get(user);
            Assert.Single(loaded.Items);
            Assert.Equal(first, loaded.Items[0].Cantidad);
            Assert.Equal(original.FechaActualizacion, loaded.FechaActualizacion);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task AddItem_AccumulationAtIntMaximum_SucceedsAndPutStillReplaces()
    {
        var path = Path.Combine(Path.GetTempPath(), $"cart-overflow-{Guid.NewGuid():N}.db");
        var connection = $"Data Source={path};Pooling=false";
        var user = Guid.NewGuid();
        var product = Guid.NewGuid();
        try
        {
            new DatabaseInitializer(connection).Initialize();
            var service = new CartService(connection, new MaximumStockCatalog());
            await service.AddItem(user, product, int.MaxValue - 1);
            await service.AddItem(user, product, 1);
            Assert.Equal(int.MaxValue, new CartService(connection).Get(user).Items[0].Cantidad);
            await service.UpdateItem(user, product, 1);
            Assert.Equal(1, new CartService(connection).Get(user).Items[0].Cantidad);
        }
        finally { File.Delete(path); }
    }

    private sealed class MaximumStockCatalog : IProductCatalog
    {
        public Task<ProductStock?> GetAsync(Guid productoId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ProductStock?>(new ProductStock { Stock = int.MaxValue });
        }
    }
}
