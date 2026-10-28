using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    private static void ValidateObservationChangeLimits(int? maximumObservations, int maximumChanges) {
        if (maximumChanges <= 0) throw new ArgumentOutOfRangeException(nameof(maximumChanges));
        if (maximumObservations.HasValue && maximumObservations.Value <= 0) throw new ArgumentOutOfRangeException(nameof(maximumObservations));
    }

    /// <summary>Captures and validates a bounded transaction without modifying installed data or ordinal presentation.</summary>
    private List<ObservationChangeEntry<T>> PlanObservationChanges<T>(IReadOnlyList<T> current,
        IEnumerable<ChartObservationChange<T>> changes, Func<T, T> validate, int? maximumObservations,
        int maximumChanges, CancellationToken cancellationToken) where T : struct {
        cancellationToken.ThrowIfCancellationRequested();
        var ordered = new List<ObservationChangeEntry<T>>(current.Count);
        var indexed = new Dictionary<ChartObservationId, ObservationChangeEntry<T>>();
        for (var index = 0; index < current.Count; index++) {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_observationIds.TryGetValue(index, out var id))
                throw new InvalidOperationException("Assign an explicit identity to every observation before applying keyed changes.");
            var entry = new ObservationChangeEntry<T>(id, current[index], index);
            ordered.Add(entry); indexed.Add(id, entry);
        }
        var seen = new HashSet<ChartObservationId>();
        var count = 0;
        foreach (var change in changes) {
            cancellationToken.ThrowIfCancellationRequested();
            if (++count > maximumChanges)
                throw new ArgumentException("The batch exceeds maximumChanges; split or coalesce updates in the host.", nameof(changes));
            _ = change.Id.Value;
            if (!seen.Add(change.Id)) throw new ArgumentException("Each observation ID may occur only once in a batch.", nameof(changes));
            if (change.Kind == ChartObservationChangeKind.Append) {
                if (indexed.ContainsKey(change.Id)) throw new ArgumentException("An appended observation ID already exists.", nameof(changes));
                var added = new ObservationChangeEntry<T>(change.Id, validate(change.Value!.Value), -1);
                indexed.Add(change.Id, added); ordered.Add(added);
            } else {
                if (!indexed.TryGetValue(change.Id, out var entry)) throw new ArgumentException("A replaced or removed observation ID does not exist.", nameof(changes));
                if (change.Kind == ChartObservationChangeKind.Remove) entry.Removed = true;
                else entry.Value = validate(change.Value!.Value);
            }
        }
        var retained = ordered.Where(entry => !entry.Removed).ToList();
        if (maximumObservations.HasValue && retained.Count > maximumObservations.Value)
            retained.RemoveRange(0, retained.Count - maximumObservations.Value);
        return retained;
    }

    private static TValue[] RemapObservationOverrides<TValue, T>(IReadOnlyList<TValue> original,
        List<ObservationChangeEntry<T>> retained) where T : struct =>
        RemapObservationOverrides(original, retained.Count, index => retained[index].OriginalIndex);

    /// <summary>Retains genuinely authored sparse slots, including trailing nulls, through a native selection.</summary>
    private static TValue[] RemapObservationOverrides<TValue>(IReadOnlyList<TValue> original, IReadOnlyList<int> retained) =>
        RemapObservationOverrides(original, retained.Count, index => retained[index]);

    private static TValue[] RemapObservationOverrides<TValue>(IReadOnlyList<TValue> original, int count, Func<int, int> sourceAt) {
        var length = 0;
        for (var index = 0; index < count; index++) {
            var source = sourceAt(index);
            if (source >= 0 && source < original.Count) length = index + 1;
        }
        var result = new TValue[length];
        for (var index = 0; index < length; index++) {
            var source = sourceAt(index);
            result[index] = source >= 0 && source < original.Count ? original[source] : default!;
        }
        return result;
    }

    /// <summary>Detaches the same ordinal presentation for numeric and typed data before either transaction commits.</summary>
    private ObservationChangePresentation CaptureObservationChangePresentation<T>(List<ObservationChangeEntry<T>> retained) where T : struct => new() {
        FunnelConnectorStyles = RemapObservationOverrides(_funnelConnectorStyles, retained),
        Colors = RemapObservationOverrides(PointColors, retained),
        StrokeColors = RemapObservationOverrides(_pointStrokeColors, retained),
        Patterns = RemapObservationOverrides(PointFillPatterns, retained),
        Labels = RemapObservationOverrides(PointLabels, retained),
        LabelStyles = RemapObservationOverrides(PointDataLabelStyles, retained),
        MarkerOptions = RemapObservationOverrides(_pointMarkers, retained),
        SliceOffsets = RemapObservationOverrides(PointSliceOffsets, retained),
        SliceRadiusRatios = RemapObservationOverrides(_pointSliceRadiusRatios, retained),
        FinancialStyles = RemapObservationOverrides(_financialStyles, retained)
    };

    private void InstallObservationChangePresentation<T>(List<ObservationChangeEntry<T>> retained,
        ObservationChangePresentation presentation) where T : struct {
        InstallPointOverrides(_funnelConnectorStyles, presentation.FunnelConnectorStyles);
        InstallPointOverrides(PointColors, presentation.Colors); InstallPointOverrides(_pointStrokeColors, presentation.StrokeColors);
        InstallPointOverrides(PointFillPatterns, presentation.Patterns);
        InstallPointOverrides(PointLabels, presentation.Labels); InstallPointOverrides(PointDataLabelStyles, presentation.LabelStyles);
        InstallPointOverrides(_pointMarkers, presentation.MarkerOptions);
        InstallPointOverrides(PointSliceOffsets, presentation.SliceOffsets);
        InstallPointOverrides(_pointSliceRadiusRatios, presentation.SliceRadiusRatios);
        _financialStyles = Array.AsReadOnly(presentation.FinancialStyles);
        _observationIds.Clear();
        for (var index = 0; index < retained.Count; index++) _observationIds.Add(index, retained[index].Id);
        ObservationDataChanged(retained.Count);
    }

    private sealed class ObservationChangePresentation {
        internal ChartFunnelConnectorStyle?[] FunnelConnectorStyles = null!;
        internal ChartColor?[] Colors = null!;
        internal ChartColor?[] StrokeColors = null!;
        internal ChartFillPattern?[] Patterns = null!;
        internal string?[] Labels = null!;
        internal TextStyleOverride?[] LabelStyles = null!;
        internal ChartMarkerOptions?[] MarkerOptions = null!;
        internal double[] SliceOffsets = null!;
        internal double?[] SliceRadiusRatios = null!;
        internal ChartFinancialItemOptions?[] FinancialStyles = null!;
    }

    private sealed class ObservationChangeEntry<T> where T : struct {
        internal ChartObservationId Id { get; }
        internal T Value { get; set; }
        internal int OriginalIndex { get; }
        internal bool Removed { get; set; }
        internal ObservationChangeEntry(ChartObservationId id, T value, int originalIndex) { Id = id; Value = value; OriginalIndex = originalIndex; }
    }
}
