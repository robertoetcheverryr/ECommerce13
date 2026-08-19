using Microsoft.AspNetCore.Mvc;
using Products.API.Models;

namespace Products.API.Controllers;

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

    [HttpGet]
    public ActionResult<IEnumerable<Product>> GetAll()
    {
        return Ok(new[] { DemoProduct });
    }

    [HttpGet("{id:guid}")]
    public ActionResult<Product> GetById(Guid id)
    {
        if (id != DemoProduct.Id)
            return NotFound();

        return Ok(DemoProduct);
    }

    [HttpPost]
    public IActionResult Create()
    {
        return Ok("yes, you have reached the POST /api/products endpoint");
    }

    [HttpPut("{id}")]
    public IActionResult Update(string id)
    {
        return Ok($"yes, you have reached the PUT /api/products/{id} endpoint");
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(string id)
    {
        return Ok($"yes, you have reached the DELETE /api/products/{id} endpoint");
    }
}