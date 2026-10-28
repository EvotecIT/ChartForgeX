using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;

namespace ChartForgeX.Core;

/// <summary>Immutable exact-X comparison with complete independent source captures and explicit matched-source provenance.</summary>
/// <remarks>Unmatched sources remain in FirstPoints/SecondPoints; they never become fabricated paired values. Source access must be serialized by the caller during capture.</remarks>
public sealed class ChartDifferenceAlignment {
    /// <summary>Gets the first captured series identity, scoped to its source chart when automatic.</summary>
    public ChartSeriesId FirstSeriesId { get; }
    /// <summary>Gets the second captured series identity, scoped to its source chart when automatic.</summary>
    public ChartSeriesId SecondSeriesId { get; }
    /// <summary>Gets the exact captured first source name, including an empty display name.</summary>
    public string FirstName { get; }
    /// <summary>Gets the exact captured second source name, including an empty display name.</summary>
    public string SecondName { get; }
    /// <summary>Gets every original first observation, including unmatched points and authored breaks/color values.</summary>
    public IReadOnlyList<ChartPoint> FirstPoints { get; }
    /// <summary>Gets every original second observation, including unmatched points and authored breaks/color values.</summary>
    public IReadOnlyList<ChartPoint> SecondPoints { get; }
    /// <summary>Gets authored first observation IDs by original source index. Missing entries have no fabricated identity.</summary>
    public IReadOnlyDictionary<int, ChartObservationId> FirstObservationIds { get; }
    /// <summary>Gets authored second observation IDs by original source index. Missing entries have no fabricated identity.</summary>
    public IReadOnlyDictionary<int, ChartObservationId> SecondObservationIds { get; }
    /// <summary>Gets complete exact-X pairs in increasing coordinate order.</summary>
    public IReadOnlyList<ChartDifferencePoint> Points { get; }
    /// <summary>Gets the original first-source index for each paired observation.</summary>
    public IReadOnlyList<int> FirstSourceIndices { get; }
    /// <summary>Gets the original second-source index for each paired observation.</summary>
    public IReadOnlyList<int> SecondSourceIndices { get; }
    /// <summary>Gets whether unmatched union positions break or connect matched observations. Explicit source breaks always remain.</summary>
    public ChartMissingDataPolicy MissingDataPolicy { get; }
    /// <summary>Gets the captured first connected-path interpolation.</summary>
    public ChartBoundaryInterpolation FirstBoundary { get; }
    /// <summary>Gets the captured second connected-path interpolation.</summary>
    public ChartBoundaryInterpolation SecondBoundary { get; }

    internal ChartDifferenceAlignment(ChartSeries first, ChartSeries second, ChartMissingDataPolicy policy, CancellationToken cancellationToken) {
        ChartSamplePoints.ValidatePolicy(policy); cancellationToken.ThrowIfCancellationRequested();
        var a = Capture(first, nameof(first), cancellationToken); var b = Capture(second, nameof(second), cancellationToken);
        var matched = ChartPairedSamples.Align(a.Points, b.Points, policy, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        FirstSeriesId = a.Id; SecondSeriesId = b.Id; FirstName = a.Name; SecondName = b.Name;
        FirstPoints = Array.AsReadOnly(a.Points); SecondPoints = Array.AsReadOnly(b.Points);
        FirstObservationIds = new ReadOnlyDictionary<int, ChartObservationId>(a.Ids); SecondObservationIds = new ReadOnlyDictionary<int, ChartObservationId>(b.Ids);
        FirstBoundary = a.Boundary; SecondBoundary = b.Boundary; MissingDataPolicy = policy;
        Points = Array.AsReadOnly(matched.Points); FirstSourceIndices = Array.AsReadOnly(matched.First); SecondSourceIndices = Array.AsReadOnly(matched.Second);
    }

    private static (ChartSeriesId Id, string Name, ChartPoint[] Points, Dictionary<int, ChartObservationId> Ids, ChartBoundaryInterpolation Boundary)
        Capture(ChartSeries source, string parameter, CancellationToken cancellationToken) {
        if (source == null) throw new ArgumentNullException(parameter);
        source.RequireDifferenceAlignmentSource();
        var id = source.Id; var name = source.Name;
        var boundary = new ChartBoundaryInterpolation(source.Interpolation, source.StepPosition);
        var points = new List<ChartPoint>();
        foreach (var point in source.Points) {
            cancellationToken.ThrowIfCancellationRequested();
            ChartGuards.Finite(point.X, parameter); ChartGuards.Finite(point.Y, parameter);
            if (points.Count > 0 && point.X <= points[points.Count - 1].X)
                throw new ArgumentException("Alignment requires globally increasing unique X in each source; duplicates or restarted X are ambiguous even at a break.", parameter);
            points.Add(point);
        }
        var ids = new Dictionary<int, ChartObservationId>();
        foreach (var entry in source.ObservationIds) {
            cancellationToken.ThrowIfCancellationRequested();
            if (entry.Key < 0 || entry.Key >= points.Count) throw new ArgumentException("Source identity indices must refer to captured observations.", parameter);
            _ = entry.Value.Value; ids.Add(entry.Key, entry.Value);
        }
        cancellationToken.ThrowIfCancellationRequested();
        return (id, name, points.ToArray(), ids, boundary);
    }
}
