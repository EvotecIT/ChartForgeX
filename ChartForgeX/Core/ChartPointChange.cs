using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>The operation applied to one explicitly identified numeric observation.</summary>
public enum ChartPointChangeKind {
    /// <summary>Appends a new observation after the existing observations.</summary>
    Append,
    /// <summary>Replaces an existing observation without changing its position or presentation.</summary>
    Replace,
    /// <summary>Removes an existing observation and its presentation.</summary>
    Remove
}

/// <summary>One immutable keyed operation in a numeric observation transaction.</summary>
public readonly struct ChartPointChange {
    /// <summary>Gets the source identity within the series.</summary>
    public ChartObservationId Id { get; }
    /// <summary>Gets the operation to perform.</summary>
    public ChartPointChangeKind Kind { get; }
    /// <summary>Gets the replacement or appended value; null for removal.</summary>
    public ChartPoint? Point { get; }

    private ChartPointChange(ChartObservationId id, ChartPointChangeKind kind, ChartPoint? point) {
        _ = id.Value;
        Id = id; Kind = kind; Point = point;
    }

    /// <summary>Appends a new source identity and value.</summary>
    public static ChartPointChange Append(ChartObservationId id, ChartPoint point) => new(id, ChartPointChangeKind.Append, point);
    /// <summary>Replaces the value of an existing identity, retaining its ordinal and point overrides.</summary>
    public static ChartPointChange Replace(ChartObservationId id, ChartPoint point) => new(id, ChartPointChangeKind.Replace, point);
    /// <summary>Removes an existing identity and its point overrides.</summary>
    public static ChartPointChange Remove(ChartObservationId id) => new(id, ChartPointChangeKind.Remove, null);
}
