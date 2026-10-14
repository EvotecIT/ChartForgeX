using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    private IReadOnlyList<ChartWaterfallItem> _waterfallItems = Array.Empty<ChartWaterfallItem>();
    internal bool IsTypedWaterfall { get; private set; }

    /// <summary>Gets immutable typed waterfall items in authored order; empty for the delta-point overload and other series.</summary>
    /// <remarks>
    /// Typed waterfall series keep their source here and leave <see cref="Points"/> empty. Point colors, labels, patterns
    /// and label styles index this collection, including checkpoints. <see cref="SourcePointCount"/> counts the supplied
    /// opening balance and deltas. An opening balance, when present, must be first and may occur only once.
    /// </remarks>
    public IReadOnlyList<ChartWaterfallItem> WaterfallItems => _waterfallItems;

    /// <summary>Replaces all waterfall items atomically and clears their IDs, labels and paint overrides. Series configuration is retained.</summary>
    /// <param name="items">The complete replacement in accumulation order. An empty sequence creates a no-data typed waterfall without an appended total.</param>
    /// <returns>The current series.</returns>
    /// <remarks>Replacing a numeric waterfall switches it to typed authoring. Coordinates must be distinct; an opening balance may occur only first.</remarks>
    public ChartSeries ReplaceWaterfallItems(IEnumerable<ChartWaterfallItem> items) {
        RequireObservationKind(ChartSeriesKind.Waterfall);
        if (items == null) throw new ArgumentNullException(nameof(items));
        var snapshot = items.ToArray();
        ValidateWaterfallItems(snapshot);
        ClearObservationMetadata();
        InstallWaterfallItems(snapshot);
        return this;
    }

    /// <summary>Replaces one complete waterfall item while preserving its ordinal, ID, label and paint overrides.</summary>
    /// <param name="index">The zero-based authored item index, including opening balances and checkpoints.</param>
    /// <param name="item">The complete replacement item. The resulting collection must keep distinct coordinates and its opening balance first.</param>
    /// <returns>The current series.</returns>
    public ChartSeries UpdateWaterfallItem(int index, ChartWaterfallItem item) {
        RequireObservationKind(ChartSeriesKind.Waterfall);
        if (index < 0 || index >= _waterfallItems.Count) throw new ArgumentOutOfRangeException(nameof(index));
        var snapshot = _waterfallItems.ToArray(); snapshot[index] = item;
        ValidateWaterfallItems(snapshot);
        InstallWaterfallItems(snapshot);
        return this;
    }

    private static void ValidateWaterfallItems(IReadOnlyList<ChartWaterfallItem> items) {
        var coordinates = new HashSet<double>();
        for (var index = 0; index < items.Count; index++) {
            var item = items[index];
            if (item == null) throw new ArgumentException("Waterfall items must not contain null entries.", nameof(items));
            if (item.Kind == ChartWaterfallItemKind.OpeningBalance && index != 0)
                throw new ArgumentException("A waterfall opening balance must be the first item and may occur only once.", nameof(items));
            if (!coordinates.Add(item.X)) throw new ArgumentException("Waterfall display coordinates must be distinct.", nameof(items));
        }
    }

    private void InstallWaterfallItems(ChartWaterfallItem[] snapshot) {
        _waterfallItems = Array.AsReadOnly(snapshot);
        IsTypedWaterfall = true;
        _points.Clear();
        ObservationDataChanged(snapshot.Count(item => item.Value.HasValue));
    }

    internal void ValidateWaterfall() {
        if (IsTypedWaterfall && _points.Count > 0)
            throw new InvalidOperationException("Typed waterfall items own this series. Raw points cannot be added to a typed waterfall.");
    }
}
