
using Microsoft.AspNetCore.Mvc;
using Orders.API.DTOs;
using Orders.API.Models;
using Orders.API.Services;
using Orders.API.Swagger;
using Swashbuckle.AspNetCore.Filters;

namespace Orders.API.Controllers;

/// <summary>
/// Permite consultar, crear y actualizar órdenes.
/// </summary>
[ApiController]
[Route("api/orders")]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly IOrdersService _ordersService;

    /// <summary>
    /// Inicializa el controlador de órdenes.
    /// </summary>
    /// <param name="ordersService">Servicio encargado de gestionar las órdenes.</param>
    public OrdersController(IOrdersService ordersService)
    {
        _ordersService = ordersService;
    }

    /// <summary>
    /// Obtiene todas las órdenes registradas, opcionalmente filtradas por usuario.
    /// </summary>
    /// <param name="usuarioId">Identificador del usuario por el cual filtrar las órdenes. Es opcional.</param>
    /// <param name="cancellationToken">Token para cancelar la solicitud.</param>
    /// <returns>Lista de órdenes encontradas.</returns>
    /// <response code="200">Las órdenes se obtuvieron correctamente.</response>
    /// <response code="500">Ocurrió un error inesperado.</response>
    [HttpGet]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(OrdersResponseExample))]
    [SwaggerResponseExample(StatusCodes.Status500InternalServerError, typeof(InternalServerErrorExample))]
    [ProducesResponseType(typeof(IEnumerable<Order>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? usuarioId,
        CancellationToken cancellationToken)
    {
        var orders = await _ordersService.GetAllAsync(
            usuarioId, cancellationToken);

        return Ok(orders);
    }

    /// <summary>
    /// Obtiene una orden por su identificador.
    /// </summary>
    /// <param name="id">Identificador único de la orden.</param>
    /// <param name="cancellationToken">Token para cancelar la solicitud.</param>
    /// <returns>Información detallada de la orden.</returns>
    /// <response code="200">La orden se encontró correctamente.</response>
    /// <response code="404">La orden solicitada no existe.</response>
    /// <response code="500">Ocurrió un error inesperado.</response>
    [HttpGet("{id:guid}")]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(OrderResponseExample))]
    [SwaggerResponseExample(StatusCodes.Status404NotFound, typeof(OrderNotFoundErrorExample))]
    [SwaggerResponseExample(StatusCodes.Status500InternalServerError, typeof(InternalServerErrorExample))]
    [ProducesResponseType(typeof(Order), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var order = await _ordersService.GetByIdAsync(
            id, cancellationToken);

        return Ok(order);
    }

    /// <summary>
    /// Crea una nueva orden y consulta los productos en Products API para obtener sus precios y verificar el stock.
    /// </summary>
    /// <param name="request">Datos necesarios para crear la orden.</param>
    /// <param name="cancellationToken">Token para cancelar la solicitud.</param>
    /// <returns>La orden creada.</returns>
    /// <response code="201">La orden se creó correctamente.</response>
    /// <response code="400">Los datos enviados son inválidos o no se incluyeron productos.</response>
    /// <response code="404">Alguno de los productos indicados no existe.</response>
    /// <response code="422">No hay stock suficiente para alguno de los productos.</response>
    /// <response code="500">Ocurrió un error inesperado.</response>
    [HttpPost]
    [SwaggerRequestExample(typeof(CreateOrderRequest), typeof(CreateOrderRequestExample))]
    [SwaggerResponseExample(StatusCodes.Status201Created, typeof(OrderResponseExample))]
    [SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(ValidationErrorExample))]
    [SwaggerResponseExample(StatusCodes.Status404NotFound, typeof(ProductNotFoundErrorExample))]
    [SwaggerResponseExample(StatusCodes.Status422UnprocessableEntity, typeof(StockInsufficientExample))]
    [SwaggerResponseExample(StatusCodes.Status500InternalServerError, typeof(InternalServerErrorExample))]
    [ProducesResponseType(typeof(Order), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var order = await _ordersService.CreateAsync(
            request, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = order.Id },
            order);
    }

    /// <summary>
    /// Actualiza el estado de una orden existente.
    /// </summary>
    /// <param name="id">Identificador único de la orden.</param>
    /// <param name="request">Nuevo estado de la orden.</param>
    /// <param name="cancellationToken">Token para cancelar la solicitud.</param>
    /// <returns>La orden con su estado actualizado.</returns>
    /// <response code="200">El estado se actualizó correctamente.</response>
    /// <response code="400">El estado enviado es inválido.</response>
    /// <response code="404">La orden solicitada no existe.</response>
    /// <response code="409">La transición de estado no está permitida.</response>
    /// <response code="500">Ocurrió un error inesperado.</response>
    [HttpPut("{id:guid}/status")]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(OrderResponseExample))]
    [SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(ValidationErrorExample))]
    [SwaggerResponseExample(StatusCodes.Status404NotFound, typeof(OrderNotFoundErrorExample))]
    [SwaggerResponseExample(StatusCodes.Status409Conflict, typeof(InvalidTransitionExample))]
    [SwaggerResponseExample(StatusCodes.Status500InternalServerError, typeof(InternalServerErrorExample))]
    [ProducesResponseType(typeof(Order), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        var order = await _ordersService.UpdateStatusAsync(
            id, request, cancellationToken);

        return Ok(order);
    }
}
