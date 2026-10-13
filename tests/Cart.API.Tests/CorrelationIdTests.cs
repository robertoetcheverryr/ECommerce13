using FluentAssertions;

namespace Cart.API.Tests;

public class CorrelationIdTests : IClassFixture<CartApiFactory>
{
    private readonly CartApiFactory _factory;

    public CorrelationIdTests(CartApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_WhenHeaderMissing_ShouldGenerateAndEchoCorrelationId()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.EnsureSuccessStatusCode();
        var correlationId = GetHeader(response);
        correlationId.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(correlationId, out _).Should().BeTrue("generated ids are Guids");
    }

    [Fact]
    public async Task Health_WhenHeaderProvided_ShouldEchoTheSameValue()
    {
        var client = _factory.CreateClient();
        var incoming = "corr-from-client-001";
        var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add(CorrelationId.HeaderName, incoming);

        var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        GetHeader(response).Should().Be(incoming);
    }

    [Fact]
    public async Task Health_WhenHeaderIsBlank_ShouldGenerateANewId()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add(CorrelationId.HeaderName, "   ");

        var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        var correlationId = GetHeader(response);
        correlationId.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(correlationId, out _).Should().BeTrue();
    }

    private static string GetHeader(HttpResponseMessage response) =>
        response.Headers.GetValues(CorrelationId.HeaderName).Single();
}
