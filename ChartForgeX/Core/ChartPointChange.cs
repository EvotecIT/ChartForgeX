using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>One immutable keyed operation in a numeric observation transaction.</summary>
public readonly struct ChartPointChange {
    /// <summary>Gets the source identity within the series.</summary>
    public ChartObservationId Id { get; }
    /// <summary>Gets the operation to perform.</summary>
    public ChartObservationChangeKind Kind { get; }
    /// <summary>Gets the replacement or appended value; null for removal.</summary>
    public ChartPoint? Point { get; }

    private ChartPointChange(ChartObservationId id, ChartObservationChangeKind kind, ChartPoint? point) {
        _ = id.Value;
        Id = id; Kind = kind; Point = point;
    }

    /// <summary>Appends a new source identity and value.</summary>
    public static ChartPointChange Append(ChartObservationId id, ChartPoint point) => new(id, ChartObservationChangeKind.Append, point);
    /// <summary>Replaces the value of an existing identity, retaining its ordinal and point overrides.</summary>
    public static ChartPointChange Replace(ChartObservationId id, ChartPoint point) => new(id, ChartObservationChangeKind.Replace, point);
    /// <summary>Removes an existing identity and its point overrides.</summary>
    public static ChartPointChange Remove(ChartObservationId id) => new(id, ChartObservationChangeKind.Remove, null);
}
