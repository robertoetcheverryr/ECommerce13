using System.Globalization;
using System.Text;
using Dapper;
using Microsoft.Data.Sqlite;
using Products.API.DTOs;
using Products.API.Exceptions;
using Products.API.Models;

namespace Products.API.Services;

/// <summary>
/// Implementación del servicio de productos.
/// Persistencia en SQLite a través de Dapper.
/// </summary>
public class ProductService : IProductService
{
    private readonly IActiveOrdersChecker _activeOrdersChecker;
    private readonly string _connectionString;

    /// <summary>
    /// Constructor. Inyecta el verificador de órdenes activas y la cadena de conexión.
    /// </summary>
    /// <param name="activeOrdersChecker">Puerto hacia órdenes activas.</param>
    /// <param name="connectionString">Cadena de conexión de SQLite.</param>
    public ProductService(IActiveOrdersChecker activeOrdersChecker, string connectionString)
    {
        _activeOrdersChecker = activeOrdersChecker;
        _connectionString = connectionString;
    }

    /// <inheritdoc />
    public IEnumerable<Product> GetAll(string? categoria = null, string? nombre = null)
    {
        using var connection = Open();
        // fold strips accents and lowercases, so NOCASE is not required here.
        connection.CreateFunction("fold", (string? value) => Fold(value));
        var rows = connection.Query<ProductRow>("""
            SELECT Id, Nombre, Descripcion, Precio, Stock, Categoria, FechaCreacion
            FROM products
            WHERE (@Categoria IS NULL OR fold(Categoria) = fold(@Categoria))
              AND (@Nombre IS NULL OR fold(Nombre) LIKE '%' || fold(@Nombre) || '%')
            """, new { Categoria = categoria, Nombre = nombre });

        var result = new List<Product>();
        /* In real life (LINQ):
        var result = rows.Select(Map).ToList();
        */
        foreach (var row in rows)
            result.Add(Map(row));

        return result;
    }

    /// <inheritdoc />
    public Product GetById(Guid id)
    {
        /* In real life (LINQ):
        var product = Products.FirstOrDefault(p => p.Id == id);
        */

        using var connection = Open();
        var row = connection.QuerySingleOrDefault<ProductRow>("""
            SELECT Id, Nombre, Descripcion, Precio, Stock, Categoria, FechaCreacion
            FROM products
            WHERE Id = @Id
            """, new { Id = id.ToString() });

        if (row is null)
            throw new NotFoundException(ErrorCodes.PRD_001, ErrorCodes.PRD_001_Message);

        return Map(row);
    }

    /// <inheritdoc />
    public Product Create(CreateProductRequest request)
    {
        /* In real life (LINQ):
        var exists = Products.Any(p =>
            p.Nombre.Equals(request.Nombre, StringComparison.OrdinalIgnoreCase) &&
            p.Categoria.Equals(request.Categoria, StringComparison.OrdinalIgnoreCase));
        */

        using var connection = Open();
        var exists = connection.ExecuteScalar<int>("""
            SELECT COUNT(1)
            FROM products
            WHERE Nombre = @Nombre COLLATE NOCASE
              AND Categoria = @Categoria COLLATE NOCASE
            """, new { request.Nombre, request.Categoria }) > 0;

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

        connection.Execute("""
            INSERT INTO products (Id, Nombre, Descripcion, Precio, Stock, Categoria, FechaCreacion)
            VALUES (@Id, @Nombre, @Descripcion, @Precio, @Stock, @Categoria, @FechaCreacion)
            """, ToParameters(product));

        return product;
    }

    /// <inheritdoc />
    public Product Update(Guid id, UpdateProductRequest request)
    {
        // PRD-003 applies to create only. Update does not check for a duplicate name.
        var product = GetById(id);

        product.Nombre = request.Nombre;
        product.Descripcion = request.Descripcion;
        product.Precio = request.Precio;
        product.Stock = request.Stock;
        product.Categoria = request.Categoria;

        using var connection = Open();
        connection.Execute("""
            UPDATE products
            SET Nombre = @Nombre,
                Descripcion = @Descripcion,
                Precio = @Precio,
                Stock = @Stock,
                Categoria = @Categoria
            WHERE Id = @Id
            """, ToParameters(product));

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

        using var connection = Open();
        connection.Execute("DELETE FROM products WHERE Id = @Id", new { Id = product.Id.ToString() });
    }

    private static string Fold(string? value)
    {
        // Comparable form for filters: split accents, drop the marks, lowercase. Stored text is unchanged.
        // Electrónica therefore becomes electronica
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var decomposed = value.Normalize(NormalizationForm.FormD);
        var folded = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                folded.Append(char.ToLowerInvariant(c));
        }

        return folded.ToString();
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private static object ToParameters(Product product) => new
    {
        // Dapper binds this object. Guid, money and the date are text in the table, so they are formatted here.
        // Note: We have NO contract regarding how the grading DB will be built.
        // TODO ask for the DB file or at least the CREATE TABLE they used, so we have interoperability.
        Id = product.Id.ToString(),
        product.Nombre,
        product.Descripcion,
        Precio = product.Precio.ToString(CultureInfo.InvariantCulture),
        product.Stock,
        product.Categoria,
        FechaCreacion = product.FechaCreacion.ToString("o", CultureInfo.InvariantCulture)
    };

    private static Product Map(ProductRow row) => new()
    {
        // Back from text to our own object
        // This method might/should live in the object itself. As we did at work
        Id = Guid.Parse(row.Id),
        Nombre = row.Nombre,
        Descripcion = row.Descripcion,
        Precio = decimal.Parse(row.Precio, CultureInfo.InvariantCulture),
        Stock = row.Stock,
        Categoria = row.Categoria,
        FechaCreacion = DateTime.Parse(row.FechaCreacion, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
    };

    private sealed class ProductRow
    {
        public string Id { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string Precio { get; set; } = string.Empty;
        public int Stock { get; set; }
        public string Categoria { get; set; } = string.Empty;
        public string FechaCreacion { get; set; } = string.Empty;
    }
}
