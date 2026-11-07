using Orders.API.DTOs;
using Orders.API.Models;

namespace Orders.API.Services;

/// <summary>
/// Contrato del servicio de órdenes.
/// Define las operaciones de negocio disponibles.
/// </summary>
public interface IOrdersService
{
    /// <summary>
    /// Obtiene todas las órdenes, con filtro opcional por usuario.
    /// </summary>
    Task<IEnumerable<Order>> GetAllAsync(
        Guid? usuarioId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene una orden por su identificador.
    /// </summary>
    Task<Order> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Crea una nueva orden.
    /// </summary>
    Task<Order> CreateAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza el estado de una orden.
    /// </summary>
    Task<Order> UpdateStatusAsync(
        Guid id,
        UpdateOrderStatusRequest request,
        CancellationToken cancellationToken = default);
}