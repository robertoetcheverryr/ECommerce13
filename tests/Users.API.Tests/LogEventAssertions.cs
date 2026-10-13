using FluentAssertions;
using Serilog.Events;

namespace Users.API.Tests;

internal static class LogEventAssertions
{
    extension(IEnumerable<LogEvent> events)
    {
        public void ShouldAllHaveEndpoint(string endpoint)
        {
            var requestEvents = events.RequestEvents();
            requestEvents.Should().NotBeEmpty("the request should produce at least one request-scoped log event");
            foreach (var logEvent in requestEvents)
                AssertScalar(logEvent, "Endpoint", endpoint);
        }

        public void ShouldAllHaveCorrelationId(string correlationId)
        {
            var requestEvents = events.RequestEvents();
            requestEvents.Should().NotBeEmpty("the request should produce at least one request-scoped log event");
            foreach (var logEvent in requestEvents)
                AssertScalar(logEvent, "CorrelationId", correlationId);
        }

        private IReadOnlyList<LogEvent> RequestEvents() =>
            [.. events.Where(IsApiLog)];
    }

    public static void AssertScalar(LogEvent logEvent, string property, string expected)
    {
        logEvent.Properties.Should().ContainKey(property);
        logEvent.Properties[property].ToString().Trim('"').Should().Be(expected);
    }

    private static bool IsApiLog(LogEvent logEvent)
    {
        if (logEvent.Properties.TryGetValue("SourceContext", out var source))
        {
            var name = source.ToString().Trim('"');
            if (name.StartsWith("Users.API", StringComparison.Ordinal))
                return true;
            if (name.Contains("RequestLoggingMiddleware", StringComparison.Ordinal))
                return true;
        }

        return logEvent.MessageTemplate.Text.Contains("HTTP {RequestMethod} {RequestPath}");
    }
}
