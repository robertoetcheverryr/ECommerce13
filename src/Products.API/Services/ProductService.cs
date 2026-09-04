namespace Products.API.Services;

using Products.API.DTOs;
using Products.API.Exceptions;
using Products.API.Models;

/// <summary>
/// Implementación del servicio de productos.
/// Persistencia temporal in-memory hasta que la cátedra entregue la librería.
/// </summary>
public class ProductService : IProductService
{
    // Store in-memory compartido. En el futuro se reemplazará por la librería de persistencia.
    private static readonly List<Product> Products = new();
    private readonly IActiveOrdersChecker _activeOrdersChecker;

    /// <summary>
    /// Constructor. Inyecta el verificador de órdenes activas.
    /// </summary>
    /// <param name="activeOrdersChecker">Puerto hacia órdenes activas.</param>
    public ProductService(IActiveOrdersChecker activeOrdersChecker)
    {
        _activeOrdersChecker = activeOrdersChecker;
    }

    /// <inheritdoc />
    public IEnumerable<Product> GetAll(string? categoria = null, string? nombre = null)
    {
        /* In real life (LINQ):
        return Products
            .Where(p => categoria == null || p.Categoria.Equals(categoria, StringComparison.OrdinalIgnoreCase))
            .Where(p => nombre == null || p.Nombre.Contains(nombre, StringComparison.OrdinalIgnoreCase));
        */

        var result = new List<Product>();

        foreach (var p in Products)
        {
            if (categoria is not null &&
                !p.Categoria.Equals(categoria, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (nombre is not null &&
                !p.Nombre.Contains(nombre, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            result.Add(p);
        }

        return result;
    }

    // inheritdoc tells doc tools and IntelliSense to copy the docs from the member being implemented
    // or overriden
    /// <inheritdoc />
    public Product GetById(Guid id)
    {
        /* In real life (LINQ):
        var product = Products.FirstOrDefault(p => p.Id == id);
        */

        Product? product = null;
        foreach (var p in Products)
        {
            if (p.Id == id)
            {
                product = p;
                break;
            }
        }

        if (product is null)
            throw new NotFoundException(ErrorCodes.PRD_001, ErrorCodes.PRD_001_Message);

        return product;
    }

    /// <inheritdoc />
    public Product Create(CreateProductRequest request)
    {
        /* In real life (LINQ):
        var exists = Products.Any(p =>
            p.Nombre.Equals(request.Nombre, StringComparison.OrdinalIgnoreCase) &&
            p.Categoria.Equals(request.Categoria, StringComparison.OrdinalIgnoreCase));
        */

        bool exists = false;
        foreach (var p in Products)
        {
            if (p.Nombre.Equals(request.Nombre, StringComparison.OrdinalIgnoreCase) &&
                p.Categoria.Equals(request.Categoria, StringComparison.OrdinalIgnoreCase))
            {
                exists = true;
                break;
            }
        }

        if (exists)
            throw new BusinessRuleException(
                ErrorCodes.PRD_003,
                string.Format(ErrorCodes.PRD_003_Message, request.Categoria),
                ErrorCodes.PRD_003_Detail,
                StatusCodes.Status409Conflict);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Nombre = request.Nombre,
            Descripcion = request.Descripcion,
            Precio = request.Precio,
            Stock = request.Stock,
            Categoria = request.Categoria,
            FechaCreacion = DateTime.UtcNow
        };

        Products.Add(product);
        return product;
    }

    /// <inheritdoc />
    public Product Update(Guid id, UpdateProductRequest request)
    {
        // Nota: PRD-003 por la spec aplica solo a CREATE.
        // Nada dice que haya que validar duplicados durante UPDATE.
        var product = GetById(id);

        product.Nombre = request.Nombre;
        product.Descripcion = request.Descripcion;
        product.Precio = request.Precio;
        product.Stock = request.Stock;
        product.Categoria = request.Categoria;

        return product;
    }

    /// <inheritdoc />
    public void Delete(Guid id)
    {
        var product = GetById(id);

        if (_activeOrdersChecker.HasActiveOrders(id))
        {
            throw new BusinessRuleException(
                ErrorCodes.PRD_004,
                ErrorCodes.PRD_004_Message,
                ErrorCodes.PRD_004_Detail,
                StatusCodes.Status409Conflict);
        }

        Products.Remove(product);
    }
}