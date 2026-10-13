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
}
