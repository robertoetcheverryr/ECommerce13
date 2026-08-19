using Microsoft.AspNetCore.Mvc;

namespace Products.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok("yes, you have reached the GET /api/products endpoint");
    }

    [HttpGet("{id}")]
    public IActionResult GetById(string id)
    {
        return Ok($"yes, you have reached the GET /api/products/{id} endpoint");
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