using FluentAssertions;
using static Users.API.Tests.LogEventAssertions;

namespace Users.API.Tests;

public class RequestLoggingTests : IClassFixture<UsersApiFactory>
{
    private readonly UsersApiFactory _factory;

    public RequestLoggingTests(UsersApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_ShouldLogRequestMethodPathStatusAndDuration()
    {
        var sink = new CollectingSink();
        using var client = _factory.CreateClientWithLogs(sink);

        var response = await client.GetAsync("/health");
        response.EnsureSuccessStatusCode();

        var requestLog = sink.Events.Should().ContainSingle(e =>
            e.MessageTemplate.Text.Contains("HTTP {RequestMethod} {RequestPath}") &&
            e.MessageTemplate.Text.Contains("responded {StatusCode}") &&
            e.MessageTemplate.Text.Contains("{Elapsed")).Subject;

        AssertScalar(requestLog, "RequestMethod", "GET");
        AssertScalar(requestLog, "RequestPath", "/health");
        AssertScalar(requestLog, "StatusCode", "200");
        requestLog.Properties.Should().ContainKey("Elapsed");
        sink.Events.ShouldAllHaveEndpoint("/health");
        sink.Events.ShouldAllHaveCorrelationId(
            response.Headers.GetValues(CorrelationId.HeaderName).Single());
    }

    [Fact]
    public async Task Health_ShouldLogRequestStartBeforeCompletion_WithSameCorrelationIdAndEndpoint()
    {
        var sink = new CollectingSink();
        using var client = _factory.CreateClientWithLogs(sink);

        var response = await client.GetAsync("/health");
        response.EnsureSuccessStatusCode();

        var events = sink.Events.ToList();
        var startLog = events.Should().ContainSingle(e =>
            e.MessageTemplate.Text == "HTTP {RequestMethod} {RequestPath} started").Subject;
        var completionLog = events.Should().ContainSingle(e =>
            e.MessageTemplate.Text.Contains("responded {StatusCode}")).Subject;
        var correlationId = response.Headers.GetValues(CorrelationId.HeaderName).Single();

        events.IndexOf(startLog).Should().BeLessThan(events.IndexOf(completionLog));
        AssertScalar(startLog, "RequestMethod", "GET");
        AssertScalar(startLog, "RequestPath", "/health");
        AssertScalar(startLog, "Endpoint", "/health");
        AssertScalar(completionLog, "Endpoint", "/health");
        AssertScalar(startLog, "CorrelationId", correlationId);
        AssertScalar(completionLog, "CorrelationId", correlationId);
    }
}
