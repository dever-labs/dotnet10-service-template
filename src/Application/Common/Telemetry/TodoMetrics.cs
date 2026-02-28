using System.Diagnostics.Metrics;

namespace ServiceTemplate.Application.Common.Telemetry;

/// <summary>Abstraction over domain metrics — allows mocking in unit tests.</summary>
public interface ITodoMetrics
{
    void RecordCreated();
    void RecordUpdated();
    void RecordDeleted();
}

/// <summary>
/// Custom OpenTelemetry metrics for the Todo domain.
///
/// Instrument naming follows OTel semantic conventions:
///   {namespace}.{noun}  — e.g. "todos.created"
///
/// Units use UCUM-style annotations in braces: "{todo}", "ms", "By".
///
/// Register the meter name via <c>.AddMeter(TodoMetrics.MeterName)</c> in Program.cs
/// so the OTel SDK collects and exports these instruments.
///
/// Uses <see cref="IMeterFactory"/> (injected from DI) for test-isolation — each test
/// host gets its own Meter instance without cross-contamination.
/// </summary>
public sealed class TodoMetrics : ITodoMetrics, IDisposable
{
    public const string MeterName = "ServiceTemplate.Todos";

    private readonly Meter _meter;

    // Monotonically increasing counters — cumulative totals since process start
    private readonly Counter<long> _created;
    private readonly Counter<long> _updated;
    private readonly Counter<long> _deleted;

    // Tracks current live (non-completed) todos; can go up AND down
    private readonly UpDownCounter<long> _active;

    public TodoMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);

        _created = _meter.CreateCounter<long>(
            name: "todos.created",
            unit: "{todo}",
            description: "Total number of todo items created.");

        _updated = _meter.CreateCounter<long>(
            name: "todos.updated",
            unit: "{todo}",
            description: "Total number of todo items updated.");

        _deleted = _meter.CreateCounter<long>(
            name: "todos.deleted",
            unit: "{todo}",
            description: "Total number of todo items deleted.");

        // UpDownCounter models a value that rises and falls over time.
        // Increment on create; decrement on delete so dashboards show the live count.
        _active = _meter.CreateUpDownCounter<long>(
            name: "todos.active",
            unit: "{todo}",
            description: "Current number of active (open or in-progress) todo items.");
    }

    /// <summary>Call after a todo is successfully persisted.</summary>
    public void RecordCreated()
    {
        _created.Add(1);
        _active.Add(1);
    }

    /// <summary>Call after a todo is successfully updated.</summary>
    public void RecordUpdated() => _updated.Add(1);

    /// <summary>Call after a todo is successfully deleted.</summary>
    public void RecordDeleted()
    {
        _deleted.Add(1);
        _active.Add(-1);
    }

    public void Dispose() => _meter.Dispose();
}
