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
        return Ok(ToResponse(cart));
    }

    /// <summary>
    /// Agrega un producto al carrito.
    /// </summary>
    /// <param name="userId">Identificador del usuario dueño del carrito.</param>
    /// <param name="request">Producto y cantidad.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El carrito actualizado.</returns>
    /// <response code="200">Producto agregado.</response>
    /// <response code="400">Cantidad inválida (CRT-004).</response>
    /// <response code="404">Producto no encontrado (CRT-002).</response>
    /// <response code="422">Stock insuficiente (CRT-003).</response>
    /// <response code="500">Error interno al procesar el carrito (CRT-005).</response>
    [HttpPost("{userId:guid}/items")]
    [ProducesResponseType(typeof(CartResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CartResponse>> AddItem(
        Guid userId,
        [FromBody] AddCartItemRequest request,
        CancellationToken cancellationToken)
    {
        var cart = await _cartService.AddItem(userId, request.ProductoId, request.Cantidad, cancellationToken);
        return Ok(ToResponse(cart));
    }

    /// <summary>
    /// Actualiza la cantidad de un producto del carrito.
    /// </summary>
    /// <param name="userId">Identificador del usuario dueño del carrito.</param>
    /// <param name="productId">Identificador del producto.</param>
    /// <param name="request">Nueva cantidad.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El carrito actualizado.</returns>
    /// <response code="200">Cantidad actualizada.</response>
    /// <response code="400">Cantidad inválida (CRT-004).</response>
    /// <response code="404">Carrito no encontrado (CRT-001) o producto no encontrado (CRT-002).</response>
    /// <response code="422">Stock insuficiente (CRT-003).</response>
    /// <response code="500">Error interno al procesar el carrito (CRT-005).</response>
    [HttpPut("{userId:guid}/items/{productId:guid}")]
    [ProducesResponseType(typeof(CartResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CartResponse>> UpdateItem(
        Guid userId,
        Guid productId,
        [FromBody] UpdateCartItemRequest request,
        CancellationToken cancellationToken)
    {
        var cart = await _cartService.UpdateItem(userId, productId, request.Cantidad, cancellationToken);
        return Ok(ToResponse(cart));
    }

    private static CartResponse ToResponse(global::Cart.API.Models.Cart cart) => new()
    {
        UsuarioId = cart.UsuarioId,
        FechaActualizacion = cart.FechaActualizacion,
        Items = cart.Items.Select(item => new CartItemResponse
        {
            ProductoId = item.ProductoId,
            Cantidad = item.Cantidad
        }).ToList()
    };
}
