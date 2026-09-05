using FluentAssertions;
using Serilog.Events;

namespace Products.API.Tests;

internal static class LogEventAssertions
{
    // extension(T x) { ... }  ==  several "def" that all take the same first arg.
    // Classic C# was: static void Foo(this IEnumerable<LogEvent> events, ...)
    // The "this" is now the extension(...) parameter. Call site stays events.Foo().
    // Python: looks like but is not a class method; more like hanging functions on a type.
    // Proposed by Rider and since this is for learning, why not.
    extension(IEnumerable<LogEvent> events)
    {
        public void ShouldAllHaveEndpoint(string endpoint)
        {
            var requestEvents = events.RequestEvents();
            requestEvents.Should().NotBeEmpty("the request should produce at least one request-scoped log event");
            foreach (var logEvent in requestEvents)
                AssertScalar(logEvent, "Endpoint", endpoint);
        }

        // [.. seq] == [*seq] / list(seq). Materializes the lazy Where() so we can
        // count and foreach without running the filter twice. Same as .ToList().
        private IReadOnlyList<LogEvent> RequestEvents() =>
            [.. events.Where(IsApiLog)];
    }

    public static void AssertScalar(LogEvent logEvent, string property, string expected)
    {
        logEvent.Properties.Should().ContainKey(property);
        logEvent.Properties[property].ToString().Trim('"').Should().Be(expected);
    }

    // Only logs we own (handlers) or the Serilog request-completion event.
    // Hosting.Diagnostics has RequestPath/RequestId but runs outside our
    // LogContext middleware, so it never gets Endpoint — ignore it.
    private static bool IsApiLog(LogEvent logEvent)
    {
        if (logEvent.Properties.TryGetValue("SourceContext", out var source))
        {
            var name = source.ToString().Trim('"');
            if (name.StartsWith("Products.API", StringComparison.Ordinal))
                return true;
            if (name.Contains("RequestLoggingMiddleware", StringComparison.Ordinal))
                return true;
        }

        return logEvent.MessageTemplate.Text.Contains("HTTP {RequestMethod} {RequestPath}");
    }
}
