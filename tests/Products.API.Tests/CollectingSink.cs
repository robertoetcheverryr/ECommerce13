using Serilog.Core;
using Serilog.Events;

namespace Products.API.Tests;

// Internal as always means it does not go outside of the project
// Sealed however is special in that it means un-inheritable
// Finally we create our own sink that only logs to a list
// We assume Serilog works as it is not our package, what we care about is that we are logging
// in the right places
internal sealed class CollectingSink : ILogEventSink
{
    public List<LogEvent> Events { get; } = new();

    public void Emit(LogEvent logEvent) => Events.Add(logEvent);
}
