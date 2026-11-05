using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Products.API.DTOs;
using Products.API.Exceptions;
using Products.API.Models;
using Products.API.Services;

namespace Products.API.Controllers;

/// <summary>
/// Endpoints de la API de productos.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    /// <summary>
    /// Constructor. Inyecta el servicio de productos.
    /// </summary>
    /// <param name="productService">Servicio de productos.</param>
    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>
    /// Lista todos los productos.
    /// </summary>
    /// <param name="categoria">Filtrar por categoría (opcional).</param>
    /// <param name="nombre">Filtrar por nombre (opcional, búsqueda parcial).</param>
    /// <returns>Lista de productos.</returns>
    /// <response code="200">Lista de productos (puede estar vacía).</response>
    /// <response code="500">Error interno al procesar el producto (PRD-005).</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Product>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public ActionResult<IEnumerable<Product>> GetAll(
        [FromQuery] string? categoria = null,
        [FromQuery] string? nombre = null)
    {
        var products = _productService.GetAll(categoria, nombre);
        return Ok(products);
    }

    /// <summary>
    /// Obtiene un producto por su ID.
    /// </summary>
    /// <param name="id">Identificador del producto (GUID). Si no es un GUID válido se responde 404 (PRD-001).</param>
    /// <returns>El producto si existe.</returns>
    /// <response code="200">Producto encontrado.</response>
    /// <response code="404">Producto no encontrado (PRD-001).</response>
    /// <response code="500">Error interno al procesar el producto (PRD-005).</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Product), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public ActionResult<Product> GetById(string id)
    {
        var product = _productService.GetById(ParseProductId(id));
        return Ok(product);
    }

    /// <summary>
    /// Crea un nuevo producto.
    /// </summary>
    /// <param name="request">Datos del producto a crear.</param>
    /// <returns>El producto creado.</returns>
    /// <response code="201">Producto creado correctamente.</response>
    /// <response code="400">Los datos del producto son inválidos (PRD-002).</response>
    /// <response code="409">Ya existe un producto con ese nombre en la categoría (PRD-003).</response>
    /// <response code="500">Error interno al procesar el producto (PRD-005).</response>
    [HttpPost]
    [ProducesResponseType(typeof(Product), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public ActionResult<Product> Create([FromBody] CreateProductRequest request)
    {
        EnsureValidModel();

        var product = _productService.Create(request);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    /// <summary>
    /// Actualiza un producto existente.
    /// </summary>
    /// <param name="id">Identificador del producto (GUID). Si no es un GUID válido se responde 404 (PRD-001).</param>
    /// <param name="request">Datos actualizados del producto.</param>
    /// <returns>El producto actualizado.</returns>
    /// <response code="200">Producto actualizado correctamente.</response>
    /// <response code="400">Los datos del producto son inválidos (PRD-002).</response>
    /// <response code="404">Producto no encontrado (PRD-001).</response>
    /// <response code="500">Error interno al procesar el producto (PRD-005).</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(Product), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public ActionResult<Product> Update(string id, [FromBody] UpdateProductRequest request)
    {
        var productId = ParseProductId(id);
        EnsureValidModel();

        var product = _productService.Update(productId, request);
        return Ok(product);
    }

    /// <summary>
    /// Elimina un producto.
    /// </summary>
    /// <param name="id">Identificador del producto (GUID). Si no es un GUID válido se responde 404 (PRD-001).</param>
    /// <response code="204">Producto eliminado correctamente.</response>
    /// <response code="404">Producto no encontrado (PRD-001).</response>
    /// <response code="409">El producto tiene órdenes activas y no puede eliminarse (PRD-004).</response>
    /// <response code="500">Error interno al procesar el producto (PRD-005).</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult Delete(string id)
    {
        _productService.Delete(ParseProductId(id));
        return NoContent();
    }

    /// <summary>
    /// Convierte el id de la ruta a Guid. Un id que no es GUID no puede existir,
    /// así que se responde igual que un producto inexistente (404, PRD-001)
    /// en lugar del 404 vacío que daba la restricción de ruta {id:guid}.
    /// </summary>
    private static Guid ParseProductId(string id)
    {
        if (!Guid.TryParse(id, out var productId))
            throw new NotFoundException(ErrorCodes.PRD_001, ErrorCodes.PRD_001_Message);

        return productId;
    }

    /// <summary>
    /// Convierte ModelState en PRD-002. errorMessage junta los textos de Data Annotations
    /// y los errores de deserialización del JSON, traducidos con <see cref="ToSpanishMessages"/>.
    /// </summary>
    private void EnsureValidModel()
    {
        if (ModelState.IsValid)
            return;
        /* In real life (LINQ):
        // SelectMany flattens the error list and Select gets the ErrorMessage
        // End result, join of the ErrorMessage of all errors 
        var errors = string.Join("; ", ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage));
        */
        var errorMessages = new List<string>();
        foreach (var entry in ModelState)
        {
            // "request" is the [FromBody] parameter. When the body cannot be deserialized MVC adds
            // a generic "The request field is required." on top of the real error, so we skip it.
            if (entry.Key == "request")
                continue;

            foreach (var error in entry.Value.Errors)
            {
                errorMessages.AddRange(ToSpanishMessages(entry.Key, error.ErrorMessage));
            }
        }

        var errors = string.Join("; ", errorMessages);

        throw new ValidationException(
            ErrorCodes.PRD_002,
            string.IsNullOrWhiteSpace(errors)
                ? ErrorCodes.PRD_002_Message
                : errors);
    }

    /// <summary>
    /// System.Text.Json reporta sus errores en inglés y con nombres de tipos internos
    /// (System.Decimal, Products.API.DTOs...). Se reconocen por la clave de ModelState y se
    /// reemplazan por textos en espanol. Los mensajes de Data Annotations pasan tal cual.
    /// </summary>
    private static IEnumerable<string> ToSpanishMessages(string key, string message)
    {
        // Empty key: there was no body at all (or a literal null)
        if (key == string.Empty)
            return ["El cuerpo de la solicitud es obligatorio."];

        // "$" is the JSON root: either [JsonRequired] fields are missing or the JSON is broken
        if (key == "$")
        {
            const string missingRequired = "missing required properties including:";
            var index = message.IndexOf(missingRequired, StringComparison.Ordinal);
            if (index < 0)
                return ["El cuerpo de la solicitud no es un JSON válido."];

            // The message ends with "including: 'nombre', 'stock'." keep only the quoted field names,
            // the type name is quoted too but it comes before "including:"
            return Regex.Matches(message[(index + missingRequired.Length)..], "'([^']+)'")
                .Select(match => $"El campo '{match.Groups[1].Value}' es obligatorio.");
        }

        // "$.precio": the value sent for that field could not be read (e.g. "abc" for a decimal)
        if (key.StartsWith("$.", StringComparison.Ordinal))
            return [$"El campo '{key[2..]}' tiene un formato inválido."];

        // Any other key is a property validated by Data Annotations, already in Spanish
        return [message];
    }
}