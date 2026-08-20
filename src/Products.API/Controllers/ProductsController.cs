using Microsoft.AspNetCore.Mvc;
using Products.API.DTOs;
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
    /// Crea un nuevo producto. (Hello World - TODO)
    /// </summary>
    [HttpPost]
    public IActionResult Create()
    {
        return Ok("yes, you have reached the POST /api/products endpoint");
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