namespace Products.API.Services;

/// <summary>
/// Abstracción para saber si un producto tiene órdenes activas (Pendiente o Confirmada).
/// La implementación real cuando exista Orders.API.
/// </summary>
public interface IActiveOrdersChecker
{
    /// <summary>
    /// Indica si el producto está referenciado por órdenes activas.
    /// </summary>
    /// <param name="productId">Identificador del producto.</param>
    /// <returns>true si no se puede eliminar el producto.</returns>
    bool HasActiveOrders(Guid productId);
}
