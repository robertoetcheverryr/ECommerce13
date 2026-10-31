using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using static Products.API.Tests.ErrorResponseAssertions;

namespace Products.API.Tests;

public class StatusCodePagesTests : IClassFixture<ProductsApiFactory>
{
    private readonly HttpClient _client;

    public StatusCodePagesTests(ProductsApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("/api/nope")]
    [InlineData("/api/products/3fa85f64-5717-4562-b3fc-2c963f66afa6/extra")]
    public async Task UnknownRoute_ShouldReturnNotFoundErrorResponse(string path)
    {
        var response = await _client.GetAsync(path);

        await AssertNotFound(response, path, string.Empty, "La ruta solicitada no existe.");
    }

    [Theory]
    [InlineData("PATCH", "/api/products")]
    [InlineData("DELETE", "/api/products")]
    [InlineData("POST", "/api/products/3fa85f64-5717-4562-b3fc-2c963f66afa6")]
    public async Task WrongMethod_ShouldReturnMethodNotAllowedErrorResponse(string method, string path)
    {
        var response = await _client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));

        await AssertMethodNotAllowed(response, path, string.Empty, "Método no permitido.");
    }

    [Theory]
    [InlineData("POST", "/api/products")]
    [InlineData("PUT", "/api/products/3fa85f64-5717-4562-b3fc-2c963f66afa6")]
    public async Task NonJsonBody_ShouldReturnUnsupportedMediaTypeErrorResponse(string method, string path)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path)
        {
            Content = new StringContent("nombre=Notebook", Encoding.UTF8, "text/plain")
        };

        var response = await _client.SendAsync(request);

        await AssertUnsupportedMediaType(
            response, path, string.Empty, "El body debe enviarse como application/json.");
    }

    [Fact]
    public async Task UnknownRoute_ShouldEchoCorrelationIdInBody()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/nope");
        request.Headers.Add(CorrelationId.HeaderName, "corr-status-page-001");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body!["correlationId"].ToString().Should().Be("corr-status-page-001");
        response.Headers.GetValues(CorrelationId.HeaderName).Should().ContainSingle("corr-status-page-001");
    }
}
