using Cart.API.DTOs;
using Cart.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cart.API.Controllers;

/// <summary>
/// Endpoints de la API de carrito.
/// </summary>
[ApiController]
[Route("api/cart")]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;

    /// <summary>
    /// Constructor. Inyecta el servicio de carrito.
    /// </summary>
    /// <param name="cartService">Servicio de carrito.</param>
    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    /// <summary>
    /// Obtiene el carrito del usuario.
    /// </summary>
    /// <param name="userId">Identificador del usuario dueño del carrito.</param>
    /// <returns>El carrito si existe.</returns>
    /// <response code="200">Carrito encontrado.</response>
    /// <response code="404">Carrito no encontrado (CRT-001).</response>
    /// <response code="500">Error interno al procesar el carrito (CRT-005).</response>
    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(CartResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public ActionResult<CartResponse> Get(Guid userId)
    {
        var cart = _cartService.Get(userId);
        return Ok(new CartResponse
        {
            UsuarioId = cart.UsuarioId,
            FechaActualizacion = cart.FechaActualizacion,
            Items = cart.Items.Select(item => new CartItemResponse
            {
                ProductoId = item.ProductoId,
                Cantidad = item.Cantidad
            }).ToList()
        });
    }
}
