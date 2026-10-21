namespace ChartForgeX.Core;

/// <summary>The shared operation applied to one explicitly identified numeric or typed authored observation.</summary>
public enum ChartObservationChangeKind {
    /// <summary>Appends a new observation after the existing observations.</summary>
    Append = 0,
    /// <summary>Replaces the value of an existing observation, retaining its ordinal and presentation.</summary>
    Replace = 1,
    /// <summary>Removes an existing observation and its presentation.</summary>
    Remove = 2
}

/// <summary>One immutable keyed operation in a numeric or typed observation transaction.</summary>
/// <typeparam name="T">The authored value type accepted by the series' change method, such as <see cref="ChartForgeX.Primitives.ChartPoint"/>.</typeparam>
/// <remarks>Transactions require an explicit ID for every existing observation. Each batch may name an ID once.
/// Enumeration, validation, capacity and cancellation failures leave installed values and presentation unchanged.
/// Rolling limits retain source order. Collection cadence and serialized access remain the host's responsibility.</remarks>
public readonly struct ChartObservationChange<T> where T : struct {
    /// <summary>Gets the source identity within the series.</summary>
    public ChartObservationId Id { get; }
    /// <summary>Gets the requested operation.</summary>
    public ChartObservationChangeKind Kind { get; }
    /// <summary>Gets the appended or replacement value, or null for removal.</summary>
    public T? Value { get; }

    private ChartObservationChange(ChartObservationId id, ChartObservationChangeKind kind, T? value) {
        _ = id.Value;
        Id = id; Kind = kind; Value = value;
    }

    /// <summary>Appends a new source identity and value.</summary>
    public static ChartObservationChange<T> Append(ChartObservationId id, T value) => new(id, ChartObservationChangeKind.Append, value);
    /// <summary>Replaces an existing value while retaining its ordinal, identity and presentation.</summary>
    public static ChartObservationChange<T> Replace(ChartObservationId id, T value) => new(id, ChartObservationChangeKind.Replace, value);
    /// <summary>Removes an existing identity, value and presentation.</summary>
    public static ChartObservationChange<T> Remove(ChartObservationId id) => new(id, ChartObservationChangeKind.Remove, null);
}
