using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;

namespace Cart.API.Tests;

public class StatusCodePagesTests : IClassFixture<CartApiFactory>
{
    private const string UserPath = "/api/cart/a1b2c3d4-0000-0000-0000-000000000001";
    private const string ItemPath = UserPath + "/items/11111111-1111-1111-1111-111111111111";

    private readonly HttpClient _client;

    public StatusCodePagesTests(CartApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("/api/nope")]
    [InlineData("/api/cart")]
    [InlineData(UserPath + "/items/11111111-1111-1111-1111-111111111111/extra")]
    public async Task UnknownRoute_ShouldReturnNotFoundErrorResponse(string path)
    {
        var response = await _client.GetAsync(path);

        await AssertErrorResponse(response, HttpStatusCode.NotFound, path, "La ruta solicitada no existe.");
    }

    [Theory]
    [InlineData("POST", UserPath)]
    [InlineData("GET", UserPath + "/items")]
    [InlineData("POST", ItemPath)]
    public async Task WrongMethod_ShouldReturnMethodNotAllowedErrorResponse(string method, string path)
    {
        var response = await _client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));

        await AssertErrorResponse(response, HttpStatusCode.MethodNotAllowed, path, "Método no permitido.");
    }

    [Theory]
    [InlineData("POST", UserPath + "/items")]
    [InlineData("PUT", ItemPath)]
    public async Task NonJsonBody_ShouldReturnUnsupportedMediaTypeErrorResponse(string method, string path)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path)
        {
            Content = new StringContent("cantidad=1", Encoding.UTF8, "text/plain")
        };

        var response = await _client.SendAsync(request);

        await AssertErrorResponse(
            response,
            HttpStatusCode.UnsupportedMediaType,
            path,
            "El body debe enviarse como application/json.");
    }

    [Fact]
    public async Task UnknownRoute_ShouldEchoCorrelationIdInBody()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/nope");
        request.Headers.Add(CorrelationId.HeaderName, "corr-status-page-001");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("correlationId").GetString().Should().Be("corr-status-page-001");
        response.Headers.GetValues(CorrelationId.HeaderName).Should().ContainSingle("corr-status-page-001");
    }

    private static async Task AssertErrorResponse(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string expectedInstance,
        string expectedMessage)
    {
        response.StatusCode.Should().Be(expectedStatus);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        root.GetProperty("status").GetInt32().Should().Be((int)expectedStatus);
        root.GetProperty("instance").GetString().Should().Be(expectedInstance);
        root.GetProperty("errorCode").GetString().Should().BeEmpty();
        root.GetProperty("errorMessage").GetString().Should().Be(expectedMessage);
        root.GetProperty("correlationId").GetString().Should().Be(
            response.Headers.GetValues(CorrelationId.HeaderName).Single());
    }
}
