using Cart.API.Exceptions;
using Cart.API.Models;
using Cart.API.Services;
using FluentAssertions;

namespace Cart.API.Tests;
using CartModel = global::Cart.API.Models.Cart;

public class CartServicePersistenceTests
{
    [Fact]
    public void Save_ShouldBeVisibleToANewServiceInstance()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"carts-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath};Pooling=false";
        var usuarioId = Guid.Parse("a1b2c3d4-0000-0000-0000-111122223333");
        var productoId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");

        try
        {
            new DatabaseInitializer(connectionString).Initialize();

            var writer = new CartService(connectionString);
            var saved = writer.Save(new CartModel
            {
                UsuarioId = usuarioId,
                Items =
                [
                    new CartItem { ProductoId = productoId, Cantidad = 2 }
                ]
            });

            var reader = new CartService(connectionString);
            var loaded = reader.Get(usuarioId);

            loaded.Should().NotBeNull();
            loaded.UsuarioId.Should().Be(usuarioId);
            loaded.FechaActualizacion.Should().Be(saved.FechaActualizacion);
            loaded.Items.Should().ContainSingle();
            loaded.Items[0].ProductoId.Should().Be(productoId);
            loaded.Items[0].Cantidad.Should().Be(2);
        }
        finally
        {
            if (File.Exists(dbPath))
                File.Delete(dbPath);
        }
    }

    [Fact]
    public void Save_ShouldReplaceItems()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"carts-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath};Pooling=false";
        var usuarioId = Guid.Parse("a1b2c3d4-0000-0000-0000-111122223333");
        var first = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");
        var second = Guid.Parse("aaaabbbb-cccc-dddd-eeee-ffff00001111");

        try
        {
            new DatabaseInitializer(connectionString).Initialize();

            var writer = new CartService(connectionString);
            writer.Save(new CartModel
            {
                UsuarioId = usuarioId,
                Items =
                [
                    new CartItem { ProductoId = first, Cantidad = 1 },
                    new CartItem { ProductoId = second, Cantidad = 3 }
                ]
            });
            writer.Save(new CartModel
            {
                UsuarioId = usuarioId,
                Items = [new CartItem { ProductoId = second, Cantidad = 4 }]
            });

            var loaded = new CartService(connectionString).Get(usuarioId);

            loaded.Should().NotBeNull();
            loaded.Items.Should().ContainSingle();
            loaded.Items[0].ProductoId.Should().Be(second);
            loaded.Items[0].Cantidad.Should().Be(4);
        }
        finally
        {
            if (File.Exists(dbPath))
                File.Delete(dbPath);
        }
    }

    [Fact]
    public void Delete_ShouldRemoveCartAndItems()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"carts-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath};Pooling=false";
        var usuarioId = Guid.Parse("a1b2c3d4-0000-0000-0000-111122223333");

        try
        {
            new DatabaseInitializer(connectionString).Initialize();

            var writer = new CartService(connectionString);
            writer.Save(new CartModel
            {
                UsuarioId = usuarioId,
                Items =
                [
                    new CartItem
                    {
                        ProductoId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
                        Cantidad = 1
                    }
                ]
            });
            writer.Delete(usuarioId);

            Action missing = () => new CartService(connectionString).Get(usuarioId);
            missing.Should().Throw<NotFoundException>()
                .Which.ErrorCode.Should().Be(ErrorCodes.CRT_001);
        }
        finally
        {
            if (File.Exists(dbPath))
                File.Delete(dbPath);
        }
    }

    [Fact]
    public async Task AddItem_ConcurrentSameProduct_ShouldNotExceedStock()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"carts-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath};Pooling=false";
        var usuarioId = Guid.Parse("a1b2c3d4-0000-0000-0000-111122223333");
        var productoId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");
        const int stock = 5;
        const int requests = 20;

        try
        {
            new DatabaseInitializer(connectionString).Initialize();
            var catalog = new FixedStockCatalog(stock);
            using var start = new ManualResetEventSlim();

            var tasks = Enumerable.Range(0, requests)
                .Select(_ => Task.Run(async () =>
                {
                    start.Wait();
                    await new CartService(connectionString, catalog).AddItem(usuarioId, productoId, 1);
                }))
                .ToList();
            start.Set();

            var succeeded = 0;
            var rejected = 0;
            foreach (var task in tasks)
            {
                try { await task; succeeded++; }
                catch (BusinessRuleException ex) when (ex.ErrorCode == ErrorCodes.CRT_003) { rejected++; }
            }

            var loaded = new CartService(connectionString).Get(usuarioId);

            succeeded.Should().Be(stock);
            rejected.Should().Be(requests - stock);
            loaded.Items.Should().ContainSingle();
            loaded.Items[0].Cantidad.Should().Be(stock);
        }
        finally
        {
            if (File.Exists(dbPath))
                File.Delete(dbPath);
        }
    }

    [Fact]
    public async Task UpdateItem_ConcurrentDifferentProducts_ShouldKeepEveryUpdate()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"carts-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath};Pooling=false";
        var usuarioId = Guid.Parse("a1b2c3d4-0000-0000-0000-111122223333");
        var productIds = Enumerable.Range(1, 20)
            .Select(i => Guid.Parse($"00000000-0000-0000-0000-{i:D12}"))
            .ToList();

        try
        {
            new DatabaseInitializer(connectionString).Initialize();
            new CartService(connectionString).Save(new CartModel
            {
                UsuarioId = usuarioId,
                Items = productIds.Select(id => new CartItem { ProductoId = id, Cantidad = 1 }).ToList()
            });
            var catalog = new FixedStockCatalog(100);
            using var start = new ManualResetEventSlim();

            var tasks = productIds
                .Select(id => Task.Run(async () =>
                {
                    start.Wait();
                    await new CartService(connectionString, catalog).UpdateItem(usuarioId, id, 5);
                }))
                .ToList();
            start.Set();
            await Task.WhenAll(tasks);

            var loaded = new CartService(connectionString).Get(usuarioId);

            loaded.Items.Should().HaveCount(productIds.Count);
            loaded.Items.Should().OnlyContain(item => item.Cantidad == 5);
        }
        finally
        {
            if (File.Exists(dbPath))
                File.Delete(dbPath);
        }
    }

    [Fact]
    public async Task RemoveItem_ConcurrentWithAddItem_ShouldKeepEveryChange()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"carts-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath};Pooling=false";
        var usuarioId = Guid.Parse("a1b2c3d4-0000-0000-0000-111122223333");
        var removedIds = Enumerable.Range(1, 10)
            .Select(i => Guid.Parse($"00000000-0000-0000-0000-{i:D12}"))
            .ToList();
        var addedIds = Enumerable.Range(101, 10)
            .Select(i => Guid.Parse($"00000000-0000-0000-0000-{i:D12}"))
            .ToList();

        try
        {
            new DatabaseInitializer(connectionString).Initialize();
            new CartService(connectionString).Save(new CartModel
            {
                UsuarioId = usuarioId,
                Items = removedIds.Select(id => new CartItem { ProductoId = id, Cantidad = 1 }).ToList()
            });
            var catalog = new FixedStockCatalog(100);
            using var start = new ManualResetEventSlim();

            var removals = removedIds.Select(id => Task.Run(() =>
            {
                start.Wait();
                new CartService(connectionString, catalog).RemoveItem(usuarioId, id);
            }));
            var additions = addedIds.Select(id => Task.Run(async () =>
            {
                start.Wait();
                await new CartService(connectionString, catalog).AddItem(usuarioId, id, 1);
            }));
            var tasks = removals.Concat(additions).ToList();
            start.Set();
            await Task.WhenAll(tasks);

            var loaded = new CartService(connectionString).Get(usuarioId);

            loaded.Items.Select(item => item.ProductoId).Should().BeEquivalentTo(addedIds);
        }
        finally
        {
            if (File.Exists(dbPath))
                File.Delete(dbPath);
        }
    }

    private sealed class FixedStockCatalog : IProductCatalog
    {
        private readonly int _stock;

        public FixedStockCatalog(int stock)
        {
            _stock = stock;
        }

        public Task<ProductStock?> GetAsync(Guid productId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ProductStock?>(new ProductStock { Stock = _stock });
    }
}
