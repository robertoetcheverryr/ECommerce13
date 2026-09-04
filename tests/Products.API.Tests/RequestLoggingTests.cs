using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Serilog;
using Serilog.Events;

namespace Products.API.Tests;

public class RequestLoggingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RequestLoggingTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAll_ShouldLogRequestMethodPathStatusAndDuration()
    {
        var sink = new CollectingSink();
        var previous = Log.Logger;
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Logger(previous)
            .WriteTo.Sink(sink)
            .CreateLogger();

        try
        {
            var client = _factory.CreateClient();
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
        }
        finally
        {
            Log.Logger = previous;
        }
    }

    private static void AssertScalar(LogEvent logEvent, string property, string expected)
    {
        logEvent.Properties.Should().ContainKey(property);
        logEvent.Properties[property].ToString().Trim('"').Should().Be(expected);
    }
}