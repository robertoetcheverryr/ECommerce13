using FluentAssertions;
using static Products.API.Tests.LogEventAssertions;

namespace Products.API.Tests;

public class RequestLoggingTests : IClassFixture<ProductsApiFactory>
{
    private readonly ProductsApiFactory _factory;

    public RequestLoggingTests(ProductsApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAll_ShouldLogRequestMethodPathStatusAndDuration()
    {
        var sink = new CollectingSink();
        using var client = _factory.CreateClientWithLogs(sink);

        var response = await client.GetAsync("/api/products");
        response.EnsureSuccessStatusCode();

        var requestLog = sink.Events.Should().ContainSingle(e =>
            e.MessageTemplate.Text.Contains("HTTP {RequestMethod} {RequestPath}") &&
            e.MessageTemplate.Text.Contains("responded {StatusCode}") &&
            e.MessageTemplate.Text.Contains("{Elapsed")).Subject;

        AssertScalar(requestLog, "RequestMethod", "GET");
        AssertScalar(requestLog, "RequestPath", "/api/products");
        AssertScalar(requestLog, "StatusCode", "200");
        requestLog.Properties.Should().ContainKey("Elapsed");
        sink.Events.ShouldAllHaveEndpoint("/api/products");
        sink.Events.ShouldAllHaveCorrelationId(
            response.Headers.GetValues(CorrelationId.HeaderName).Single());
    }
}
