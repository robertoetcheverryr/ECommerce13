using Microsoft.AspNetCore.Mvc;
using Products.API.DTOs;
using Products.API.Exceptions;
using Products.API.Models;

namespace Products.API.Controllers;

/// <summary>
/// Endpoints de gestión de productos.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    // Gen a readonly product to have something to return for now
    private static readonly Product DemoProduct = new()
    {
        Id = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
        Nombre = "Notebook Dell XPS 15",
        Descripcion = "Laptop 15 pulgadas, 32GB RAM",
        Precio = 1500.00m,
        Stock = 10,
        Categoria = "Electrónica",
        FechaCreacion = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc)
    };

    /// <summary>
    /// Lista todos los productos.
    /// </summary>
    /// <returns>Lista de productos.</returns>
    /// <response code="200">Lista de productos (puede estar vacía).</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Product>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<Product>> GetAll()
    {
        return Ok(new[] { DemoProduct });
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
        if (id != DemoProduct.Id)
            throw new NotFoundException("PRD-001", "Producto no encontrado.");

        return Ok(DemoProduct);
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