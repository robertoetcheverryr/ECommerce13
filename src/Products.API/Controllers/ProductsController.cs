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
    /// <returns>Lista de productos.</returns>
    /// <response code="200">Lista de productos (puede estar vacía).</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Product>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<Product>> GetAll()
    {
        var products = _productService.GetAll();
        return Ok(products);
    }

    /// <summary>
    /// Obtiene un producto por su ID.
    /// </summary>
    /// <param name="id">Identificador del producto.</param>
    /// <returns>El producto si existe.</returns>
    /// <response code="200">Producto encontrado.</response>
    /// <response code="404">Producto no encontrado (PRD-001).</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Product), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public ActionResult<Product> GetById(Guid id)
    {
        var product = _productService.GetById(id);
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
    [HttpPost]
    [ProducesResponseType(typeof(Product), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public ActionResult<Product> Create([FromBody] CreateProductRequest request)
    {
        if (!ModelState.IsValid)
        {
            /* In real life (LINQ):
            var errors = string.Join("; ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));
            */

            var errorMessages = new List<string>();
            foreach (var entry in ModelState)
            {
                foreach (var error in entry.Value.Errors)
                {
                    errorMessages.Add(error.ErrorMessage);
                }
            }

            var errors = string.Join("; ", errorMessages);
            
            // Ternary operator, equivalent to 
            //"Los datos del producto son inválidos." if not errors or errors.isspace() else errors
            throw new ValidationException(
                "PRD-002",
                string.IsNullOrWhiteSpace(errors)
                    ? "Los datos del producto son inválidos."
                    : errors);
        }

        var product = _productService.Create(request);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    /// <summary>
    /// Actualiza un producto existente. (Hello World - TODO)
    /// </summary>
    [HttpPut("{id}")]
    public IActionResult Update(string id)
    {
        return Ok($"yes, you have reached the PUT /api/products/{id} endpoint");
    }

    /// <summary>
    /// Elimina un producto. (Hello World - TODO)
    /// </summary>
    [HttpDelete("{id}")]
    public IActionResult Delete(string id)
    {
        return Ok($"yes, you have reached the DELETE /api/products/{id} endpoint");
    }
}