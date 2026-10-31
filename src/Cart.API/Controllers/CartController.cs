using Cart.API.DTOs;
using Cart.API.Exceptions;
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
    /// <param name="userId">Identificador del usuario dueño del carrito (GUID). Si no es un GUID válido se responde 404 (CRT-001).</param>
    /// <returns>El carrito si existe.</returns>
    /// <response code="200">Carrito encontrado.</response>
    /// <response code="404">Carrito no encontrado (CRT-001).</response>
    /// <response code="500">Error interno al procesar el carrito (CRT-005).</response>
    [HttpGet("{userId}")]
    [ProducesResponseType(typeof(CartResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public ActionResult<CartResponse> Get(string userId)
    {
        var cart = _cartService.Get(ParseUserId(userId));
        return Ok(ToResponse(cart));
    }

    /// <summary>
    /// Agrega un producto al carrito.
    /// </summary>
    /// <param name="userId">Identificador del usuario dueño del carrito (GUID). Si no es un GUID válido se responde 404 sin errorCode.</param>
    /// <param name="request">Producto y cantidad.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El carrito actualizado.</returns>
    /// <response code="200">Producto agregado.</response>
    /// <response code="400">Cantidad inválida (CRT-004).</response>
    /// <response code="404">Producto no encontrado (CRT-002) o userId que no es GUID (sin errorCode).</response>
    /// <response code="422">Stock insuficiente (CRT-003).</response>
    /// <response code="500">Error interno al procesar el carrito (CRT-005).</response>
    [HttpPost("{userId}/items")]
    [ProducesResponseType(typeof(CartResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CartResponse>> AddItem(
        string userId,
        [FromBody] AddCartItemRequest request,
        CancellationToken cancellationToken)
    {
        // Spec consistency gap: CRT-001 is listed only for GET/PUT/DELETE, so a POST
        // userId that is not a GUID gets a 404 without errorCode instead of borrowing it.
        var cartUserId = ParseId(userId, string.Empty, ErrorCodes.InvalidUserId_Message);
        var invalidModel = EnsureValidModel();
        if (invalidModel is not null)
            return invalidModel;

        var cart = await _cartService.AddItem(cartUserId, request.ProductoId, request.Cantidad, cancellationToken);
        return Ok(ToResponse(cart));
    }

    /// <summary>
    /// Actualiza la cantidad de un producto del carrito.
    /// </summary>
    /// <param name="userId">Identificador del usuario dueño del carrito (GUID). Si no es un GUID válido se responde 404 (CRT-001).</param>
    /// <param name="productId">Identificador del producto (GUID). Si no es un GUID válido se responde 404 (CRT-002).</param>
    /// <param name="request">Nueva cantidad.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>El carrito actualizado.</returns>
    /// <response code="200">Cantidad actualizada.</response>
    /// <response code="400">Cantidad inválida (CRT-004).</response>
    /// <response code="404">Carrito no encontrado (CRT-001) o producto no encontrado (CRT-002).</response>
    /// <response code="422">Stock insuficiente (CRT-003).</response>
    /// <response code="500">Error interno al procesar el carrito (CRT-005).</response>
    [HttpPut("{userId}/items/{productId}")]
    [ProducesResponseType(typeof(CartResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CartResponse>> UpdateItem(
        string userId,
        string productId,
        [FromBody] UpdateCartItemRequest request,
        CancellationToken cancellationToken)
    {
        // Como la spec no aclara qué responder si ambos ids son inválidos, lo dejamos de esta manera:
        // gana productId (CRT-002) porque CartService.UpdateItem chequea el producto antes que el carrito.
        var itemProductId = ParseProductId(productId);
        var cartUserId = ParseUserId(userId);
        var invalidModel = EnsureValidModel();
        if (invalidModel is not null)
            return invalidModel;

        var cart = await _cartService.UpdateItem(cartUserId, itemProductId, request.Cantidad, cancellationToken);
        return Ok(ToResponse(cart));
    }

    /// <summary>
    /// Quita un producto del carrito.
    /// </summary>
    /// <param name="userId">Identificador del usuario dueño del carrito (GUID). Si no es un GUID válido se responde 404 (CRT-001).</param>
    /// <param name="productId">Identificador del producto a quitar (GUID). Si no es un GUID válido se responde 404 sin errorCode.</param>
    /// <response code="204">Producto eliminado del carrito.</response>
    /// <response code="404">Carrito no encontrado (CRT-001) o productId que no es GUID (sin errorCode).</response>
    /// <response code="500">Error interno al procesar el carrito (CRT-005).</response>
    [HttpDelete("{userId}/items/{productId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult RemoveItem(string userId, string productId)
    {
        // Como la spec no aclara qué responder si ambos ids son inválidos, lo dejamos de esta manera:
        // gana userId (CRT-001) porque CartService.RemoveItem chequea el carrito antes que el ítem.
        var cartUserId = ParseUserId(userId);
        // Spec consistency gap: CRT-002 is listed only for POST/PUT, so a DELETE
        // productId that is not a GUID gets a 404 without errorCode instead of borrowing it.
        var itemProductId = ParseId(productId, string.Empty, ErrorCodes.InvalidProductId_Message);
        _cartService.RemoveItem(cartUserId, itemProductId);
        return NoContent();
    }

    /// <summary>
    /// Vacía el carrito del usuario.
    /// </summary>
    /// <param name="userId">Identificador del usuario dueño del carrito (GUID). Si no es un GUID válido se responde 404 (CRT-001).</param>
    /// <response code="204">Carrito vaciado.</response>
    /// <response code="404">Carrito no encontrado (CRT-001).</response>
    /// <response code="500">Error interno al procesar el carrito (CRT-005).</response>
    [HttpDelete("{userId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult Clear(string userId)
    {
        _cartService.Delete(ParseUserId(userId));
        return NoContent();
    }

    /// <summary>
    /// Convierte el userId de la ruta a Guid. Un userId que no es GUID no puede tener carrito,
    /// así que se responde igual que un carrito inexistente (404, CRT-001)
    /// en lugar del 404 vacío que daba la restricción de ruta {userId:guid}.
    /// </summary>
    private static Guid ParseUserId(string userId) =>
        ParseId(userId, ErrorCodes.CRT_001, ErrorCodes.CRT_001_Message);

    /// <summary>
    /// Convierte el productId de la ruta a Guid. Un productId que no es GUID no puede existir
    /// en Products, así que se responde igual que un producto inexistente (404, CRT-002).
    /// </summary>
    private static Guid ParseProductId(string productId) =>
        ParseId(productId, ErrorCodes.CRT_002, ErrorCodes.CRT_002_Message);

    private static Guid ParseId(string value, string errorCode, string errorMessage)
    {
        if (!Guid.TryParse(value, out var id))
            throw new NotFoundException(errorCode, errorMessage);

        return id;
    }

    /// <summary>
    /// Manual model validation for request bodies. The Cart spec defines no error
    /// code for missing or malformed fields (unlike PRD-002 and USR-002 in the
    /// other APIs), so we return the plain framework 400 ValidationProblemDetails.
    /// NOTE: this is where we should raise CRT-002 if the SPEC were consistent with
    /// the previous ones.
    /// </summary>
    private ActionResult? EnsureValidModel()
    {
        if (ModelState.IsValid)
            return null;

        return BadRequest(new ValidationProblemDetails(ModelState));
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
