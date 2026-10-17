using FluentAssertions;
using Products.API.DTOs;
using Products.API.Exceptions;
using Products.API.Services;

namespace Products.API.Tests;

public class ProductServicePersistenceTests
{
    [Fact]
    public void Create_ShouldBeVisibleToANewServiceInstance()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"products-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath};Pooling=false";

        try
        {
            new DatabaseInitializer(connectionString).Initialize();

            var writer = new ProductService(new NoOpActiveOrdersChecker(), connectionString);
            var created = writer.Create(new CreateProductRequest
            {
                Nombre = "Notebook Dell XPS 15",
                Descripcion = "Laptop 15 pulgadas, 32GB RAM",
                Precio = 1500.00m,
                Stock = 10,
                Categoria = "Electrónica"
            });

            var reader = new ProductService(new NoOpActiveOrdersChecker(), connectionString);
            var loaded = reader.GetById(created.Id);

            loaded.Id.Should().Be(created.Id);
            loaded.Nombre.Should().Be("Notebook Dell XPS 15");
            loaded.Precio.Should().Be(1500.00m);
            loaded.Stock.Should().Be(10);
            loaded.Categoria.Should().Be("Electrónica");
            loaded.FechaCreacion.Should().Be(created.FechaCreacion);
        }
        finally
        {
            if (File.Exists(dbPath))
                File.Delete(dbPath);
        }
    }

    [Fact]
    public void Create_ConcurrentDuplicates_ShouldAllowOnlyOne()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"products-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath};Pooling=false";

        try
        {
            new DatabaseInitializer(connectionString).Initialize();

            var request = new CreateProductRequest
            {
                Nombre = "Notebook Dell XPS 15",
                Descripcion = "Laptop 15 pulgadas, 32GB RAM",
                Precio = 1500.00m,
                Stock = 10,
                Categoria = "Electrónica"
            };

            // Two creates racing for the same name+category. With the check and
            // the INSERT inside a BEGIN IMMEDIATE transaction, the second one
            // waits for the first commit and then sees the row -> PRD-003.
            var first = Task.Run(() => new ProductService(new NoOpActiveOrdersChecker(), connectionString).Create(request));
            var second = Task.Run(() => new ProductService(new NoOpActiveOrdersChecker(), connectionString).Create(request));

            var succeeded = 0;
            var conflicts = 0;
            try { first.GetAwaiter().GetResult(); succeeded++; } catch (BusinessRuleException) { conflicts++; }
            try { second.GetAwaiter().GetResult(); succeeded++; } catch (BusinessRuleException) { conflicts++; }

            succeeded.Should().Be(1);
            conflicts.Should().Be(1);
        }
        finally
        {
            if (File.Exists(dbPath))
                File.Delete(dbPath);
        }
    }
}
