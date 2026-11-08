using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Products.API.Exceptions;
using Products.API.Services;
using static Products.API.Tests.ErrorResponseAssertions;
using static Products.API.Tests.ProductResponseAssertions;

namespace Products.API.Tests;

/*
The cross-service test Cart's header comment postponed ("A cross-service test
will be built when all API are done"): here it is. These tests do not replace
IActiveOrdersChecker with a fake. They run the real ActiveOrdersClient inside
the Products host, and on the other end there is a fake Orders listening on a
real socket. What gets tested is the whole wire: URL, BaseAddress, JSON, the
outbound correlation header, and failing closed when Orders answers badly.
*/
public class ActiveOrdersClientTests : IClassFixture<ProductsApiFactory>
{
    private readonly ProductsApiFactory _factory;

    public ActiveOrdersClientTests(ProductsApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Delete_WhenOrdersHasActiveOrderWithProduct_ShouldReturnPrd004()
    {
        await using var orders = await FakeOrders.StartAsync([]);

        using var host = HostOnline(orders);
        using var client = host.CreateClient();

        var created = await AssertProductCreated(
            await client.PostAsJsonAsync("/api/products", Create("Notebook Pedida")));
        orders.AnswerWith("Pendiente", created.Id);

        var response = await client.DeleteAsync($"/api/products/{created.Id}");

        await AssertConflict(
            response,
            $"/api/products/{created.Id}",
            ErrorCodes.PRD_004,
            ErrorCodes.PRD_004_Message,
            ErrorCodes.PRD_004_Detail);
    }

    [Fact]
    public async Task Delete_WhenOrderWithProductIsCancelled_ShouldDeleteProduct()
    {
        await using var orders = await FakeOrders.StartAsync([]);

        using var host = HostOnline(orders);
        using var client = host.CreateClient();

        var created = await AssertProductCreated(
            await client.PostAsJsonAsync("/api/products", Create("Mouse Devuelto")));
        orders.AnswerWith("Cancelada", created.Id);

        var response = await client.DeleteAsync($"/api/products/{created.Id}");

        await AssertNoContent(response);
        (await client.GetAsync($"/api/products/{created.Id}")).StatusCode
            .Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_WhenOrdersHasTheProductInAnotherStatus_ShouldOnlyCountActiveStates()
    {
        // An Enviada and an Entregada order are not active (PRD-004 says Pendiente or
        // Confirmada). A client that filters badly would answer 409 on this test.
        await using var orders = await FakeOrders.StartAsync([]);

        using var host = HostOnline(orders);
        using var client = host.CreateClient();

        var created = await AssertProductCreated(
            await client.PostAsJsonAsync("/api/products", Create("Teclado Viejo")));
        orders.AnswerWith("Enviada", created.Id);
        orders.AddOrder("Entregada", created.Id);

        var response = await client.DeleteAsync($"/api/products/{created.Id}");

        await AssertNoContent(response);
    }

    [Fact]
    public async Task Delete_ShouldForwardTheCorrelationIdToOrders()
    {
        await using var orders = await FakeOrders.StartAsync([]);

        using var host = HostOnline(orders);
        using var client = host.CreateClient();

        var created = await AssertProductCreated(
            await client.PostAsJsonAsync("/api/products", Create("Monitor Tranquilo")));

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/products/{created.Id}");
        request.Headers.Add(CorrelationId.HeaderName, "prd004-unit-test");
        var response = await client.SendAsync(request);

        await AssertNoContent(response);
        orders.ReceivedCorrelationIds.Should().Contain("prd004-unit-test");
    }

    [Fact]
    public async Task Delete_WhenOrdersAnswersError_ShouldFailClosed_WithPrd005()
    {
        // Orders is down: the DELETE cannot just assume "no orders". The exception has
        // to reach the global handler and answer PRD-005, never a lying 204.
        await using var orders = await FakeOrders.StartAsync([], statusCode: StatusCodes.Status500InternalServerError);

        using var host = HostOnline(orders);
        using var client = host.CreateClient();

        var created = await AssertProductCreated(
            await client.PostAsJsonAsync("/api/products", Create("Silla Huérfana")));

        var response = await client.DeleteAsync($"/api/products/{created.Id}");

        await AssertInternalError(
            response,
            $"/api/products/{created.Id}",
            ErrorCodes.PRD_005,
            ErrorCodes.PRD_005_Message);
    }

    // Removes the factory's default fake ("never any orders") and puts the typed
    // client back into the graph. This is the place where Orders is really online.
    private WebApplicationFactory<Program> HostOnline(FakeOrders orders)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Orders:BaseUrl"] = orders.Url
                }));

            builder.ConfigureServices(services =>
            {
                foreach (var descriptor in services
                             .Where(d => d.ServiceType == typeof(IActiveOrdersChecker))
                             .ToList())
                {
                    services.Remove(descriptor);
                }

                // CreateClient with the typed client's name (AddHttpClient uses the name of
                // TClient, which here is the interface) returns the HttpClient already wired
                // like in production: BaseAddress from Orders:BaseUrl plus the correlation
                // handler. So this exercises the real wiring, not a test-only copy.
                services.AddTransient<IActiveOrdersChecker>(sp =>
                    new ActiveOrdersClient(
                        sp.GetRequiredService<IHttpClientFactory>()
                          .CreateClient(nameof(IActiveOrdersChecker))));
            });
        });
    }

    private static object Create(string nombre) => new
    {
        nombre,
        descripcion = "producto de un test de PRD-004 online",
        precio = 10.00m,
        stock = 1,
        categoria = "Electrónica"
    };

    // Same shape as the real GET /api/orders response (camelCase by default).
    private static object Order(string estado, Guid? productoId) => new
    {
        Estado = estado,
        Items = new[] { new { ProductoId = productoId ?? Guid.Empty } }
    };

    // Fake Orders: a real Kestrel (ASP.NET's built-in web server)
    // on 127.0.0.1 with a random port, answering whatever
    // the test asks and remembering the correlation ids that arrive.
    private sealed class FakeOrders : IAsyncDisposable
    {
        private readonly WebApplication _app;
        private readonly List<object> _orders = [];
        private readonly int _statusCode;

        public List<string?> ReceivedCorrelationIds { get; } = [];

        private FakeOrders(WebApplication app, int statusCode)
        {
            _app = app;
            _statusCode = statusCode;
        }

        public string Url => _app.Urls.First();

        public static async Task<FakeOrders> StartAsync(object[] orders, int statusCode = StatusCodes.Status200OK)
        {
            var builder = WebApplication.CreateBuilder();
            builder.Logging.ClearProviders();
            builder.WebHost.UseUrls("http://127.0.0.1:0");

            var app = builder.Build();
            var fake = new FakeOrders(app, statusCode);
            fake._orders.AddRange(orders);

            app.MapGet("/api/orders", (HttpContext ctx) =>
            {
                fake.ReceivedCorrelationIds.Add(ctx.Request.Headers["X-Correlation-Id"].FirstOrDefault());

                if (fake._statusCode != StatusCodes.Status200OK)
                    return Results.StatusCode(fake._statusCode);

                return Results.Json(fake._orders);
            });

            await app.StartAsync();
            return fake;
        }

        public void AnswerWith(string estado, Guid productoId)
        {
            _orders.Clear();
            _orders.Add(Order(estado, productoId));
        }

        public void AddOrder(string estado, Guid productoId) => _orders.Add(Order(estado, productoId));

        public async ValueTask DisposeAsync() => await _app.DisposeAsync();
    }
}
