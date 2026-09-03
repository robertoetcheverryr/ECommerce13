namespace Products.API.Services;

/// <summary>
/// Implementación temporal: todavía no hay Orders.API, así que no hay órdenes activas.
/// </summary>
public class NoOpActiveOrdersChecker : IActiveOrdersChecker
{
    /// <inheritdoc />
    public bool HasActiveOrders(Guid productId)
    {
        return false;
    }
}
