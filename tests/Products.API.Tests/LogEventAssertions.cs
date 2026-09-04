using FluentAssertions;
using Serilog.Events;

namespace Products.API.Tests;

internal static class LogEventAssertions
{
    public static void ShouldAllHaveEndpoint(this IReadOnlyCollection<LogEvent> events, string endpoint)
    {
        events.Should().NotBeEmpty("the request should produce at least one log event");
        foreach (var logEvent in events)
            AssertScalar(logEvent, "Endpoint", endpoint);
    }

    public static void AssertScalar(LogEvent logEvent, string property, string expected)
    {
        logEvent.Properties.Should().ContainKey(property);
        logEvent.Properties[property].ToString().Trim('"').Should().Be(expected);
    }
}
