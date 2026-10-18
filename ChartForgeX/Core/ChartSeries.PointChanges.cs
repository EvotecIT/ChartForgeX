using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    /// <summary>Applies a bounded, keyed numeric transaction while retaining surviving observations' identities and presentation.</summary>
    /// <remarks>
    /// Supported by the line, step-line, area, step-area, stacked-area, scatter, bar, horizontal-bar, radial-bar,
    /// radial-column and lollipop families. Derived and typed series use their own source APIs.
    /// Every existing observation must have an explicit ID. Each batch may name an ID once; append requires a new ID,
    /// and replace/remove require an existing ID. Enumeration, key, capacity and cancellation failures leave installed data unchanged.
    /// The optional rolling limit keeps the last observations in source order, rather than sorting by X.
    /// Collection cadence, asynchronous queues and retry/coalescing policy remain with the host. An oversized batch
    /// is rejected after at most <paramref name="maximumChanges"/> plus one entries, without queuing work.
    /// Like other chart authoring APIs, this method requires the caller to serialize access to its model.
    /// </remarks>
    /// <param name="changes">The finite sequence of uniquely keyed append, replace and remove operations.</param>
    /// <param name="maximumObservations">An optional positive rolling capacity. Oldest surviving observations are removed first.</param>
    /// <param name="maximumChanges">The positive maximum number of operations accepted in this call.</param>
    /// <param name="cancellationToken">Cancellation checked while capturing operations and before installation.</param>
    /// <returns>The current series.</returns>
    public ChartSeries ApplyPointChanges(IEnumerable<ChartPointChange> changes, int? maximumObservations = null,
        int maximumChanges = 4096, CancellationToken cancellationToken = default) {
        if (changes == null) throw new ArgumentNullException(nameof(changes));
        if (maximumChanges <= 0) throw new ArgumentOutOfRangeException(nameof(maximumChanges));
        if (maximumObservations.HasValue && maximumObservations.Value <= 0) throw new ArgumentOutOfRangeException(nameof(maximumObservations));
        RequireMutableNumericObservations();
        if (!ChartSeriesKindTraits.SupportsPointSeriesMapping(Kind))
            throw new NotSupportedException("Keyed point changes require a point-mapped numeric series family.");
        cancellationToken.ThrowIfCancellationRequested();

        var ordered = new List<PointChangeEntry>(_points.Count);
        var indexed = new Dictionary<ChartObservationId, PointChangeEntry>();
        for (var index = 0; index < _points.Count; index++) {
            if (!_observationIds.TryGetValue(index, out var id))
                throw new InvalidOperationException("Assign an explicit identity to every observation before applying keyed changes.");
            var entry = new PointChangeEntry(id, _points[index], index);
            ordered.Add(entry); indexed.Add(id, entry);
        }
        var seen = new HashSet<ChartObservationId>();
        var count = 0;
        foreach (var change in changes) {
            cancellationToken.ThrowIfCancellationRequested();
            if (++count > maximumChanges) throw new ArgumentException("The batch exceeds maximumChanges; split or coalesce updates in the host.", nameof(changes));
            _ = change.Id.Value;
            if (!seen.Add(change.Id)) throw new ArgumentException("Each observation ID may occur only once in a batch.", nameof(changes));
            if (change.Kind == ChartPointChangeKind.Append) {
                if (indexed.ContainsKey(change.Id)) throw new ArgumentException("An appended observation ID already exists.", nameof(changes));
                var added = new PointChangeEntry(change.Id, change.Point!.Value, -1);
                indexed.Add(change.Id, added); ordered.Add(added);
            } else {
                if (!indexed.TryGetValue(change.Id, out var entry)) throw new ArgumentException("A replaced or removed observation ID does not exist.", nameof(changes));
                if (change.Kind == ChartPointChangeKind.Remove) entry.Removed = true;
                else entry.Point = change.Point!.Value;
            }
        }

        var retained = ordered.Where(entry => !entry.Removed).ToList();
        if (maximumObservations.HasValue && retained.Count > maximumObservations.Value)
            retained.RemoveRange(0, retained.Count - maximumObservations.Value);
        var points = retained.Select(entry => entry.Point).ToArray();
        var colors = RemapPointOverrides(PointColors, retained);
        var patterns = RemapPointOverrides(PointFillPatterns, retained);
        var labels = RemapPointOverrides(PointLabels, retained);
        var styles = RemapPointOverrides(PointDataLabelStyles, retained);
        var offsets = RemapPointOverrides(PointSliceOffsets, retained);
        cancellationToken.ThrowIfCancellationRequested();

        _points.Clear(); _points.AddRange(points);
        InstallPointOverrides(PointColors, colors); InstallPointOverrides(PointFillPatterns, patterns);
        InstallPointOverrides(PointLabels, labels); InstallPointOverrides(PointDataLabelStyles, styles);
        InstallPointOverrides(PointSliceOffsets, offsets);
        _observationIds.Clear();
        for (var index = 0; index < retained.Count; index++) _observationIds.Add(index, retained[index].Id);
        return this;
    }

    private static T[] RemapPointOverrides<T>(List<T> original, List<PointChangeEntry> retained) {
        var length = 0;
        for (var index = 0; index < retained.Count; index++)
            if (retained[index].OriginalIndex >= 0 && retained[index].OriginalIndex < original.Count) length = index + 1;
        var result = new T[length];
        for (var index = 0; index < length; index++) {
            var source = retained[index].OriginalIndex;
            result[index] = source >= 0 && source < original.Count ? original[source] : default!;
        }
        return result;
    }

    private static void InstallPointOverrides<T>(List<T> target, T[] values) { target.Clear(); target.AddRange(values); }

    private sealed class PointChangeEntry {
        internal ChartObservationId Id { get; }
        internal ChartPoint Point { get; set; }
        internal int OriginalIndex { get; }
        internal bool Removed { get; set; }
        internal PointChangeEntry(ChartObservationId id, ChartPoint point, int originalIndex) { Id = id; Point = point; OriginalIndex = originalIndex; }
    }
}
