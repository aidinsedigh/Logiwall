using System.Collections.Concurrent;
using Serilog.Core;
using Serilog.Events;

namespace Logiwall.IntegrationTests.Infrastructure;

/// <summary>
/// Keeps every log event of the test host in memory. Program.cs reads sinks from DI
/// (<c>ReadFrom.Services</c>), so registering this sink is enough.
/// </summary>
public class CapturingLogSink : ILogEventSink
{
    private readonly ConcurrentQueue<LogEvent> events = new();

    public IReadOnlyCollection<LogEvent> Events => events;

    public void Emit(LogEvent logEvent) => events.Enqueue(logEvent);
}
