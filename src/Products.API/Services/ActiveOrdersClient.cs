namespace Products.API.Services;

/// <summary>
/// Cliente HTTP de Orders.API para PRD-004. Consulta GET /api/orders y fija si el
/// producto esta referenciado por ordenes activas (Pendiente o Confirmada).
/// </summary>
public class ActiveOrdersClient : IActiveOrdersChecker
{
    private readonly HttpClient _http;

    /// <summary>
    /// Crea el cliente con el HttpClient nombrado por IHttpClientFactory.
    /// </summary>
    /// <param name="http">Cliente con base address de Orders.</param>
    public ActiveOrdersClient(HttpClient http)
    {
        _http = http;
    }

    /// <inheritdoc />
    public bool HasActiveOrders(Guid productId)
    {
        // Forzamos (hacemos sync el async awaiteandolo)
        // el GetAsync porque todo ProductService es sync (Dapper) desde el dia 1.
        // Si Orders no esta o responde mal, la excepcion sube y el handler global la
        // convierte en PRD-005: preferimos fallar el DELETE antes que asumir
        // "no hay ordenes" sin poder preguntar.
        using var response = _http
            .GetAsync("/api/orders")
            .GetAwaiter().GetResult();

        response.EnsureSuccessStatusCode();

        var orders = response.Content
            .ReadFromJsonAsync<List<OrderPayload>>()
            .GetAwaiter().GetResult();

        if (orders is null)
            return false;

        return orders.Any(order =>
            order.Estado is "Pendiente" or "Confirmada" &&
            order.Items.Any(item => item.ProductoId == productId));
    }

    private sealed class OrderPayload
    {
        public string Estado { get; set; } = string.Empty;
        public List<ItemPayload> Items { get; set; } = new();
    }

    private sealed class ItemPayload
    {
        public Guid ProductoId { get; set; }
    }
}
