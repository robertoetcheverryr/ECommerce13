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
}
