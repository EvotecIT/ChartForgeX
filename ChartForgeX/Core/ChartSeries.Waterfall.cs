using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    /// <summary>Gets immutable typed waterfall items in authored order; empty for the delta-point overload and other series.</summary>
    /// <remarks>
    /// Typed waterfall series keep their source here and leave <see cref="Points"/> empty. Point colors, labels, patterns
    /// and label styles index this collection, including checkpoints. <see cref="SourcePointCount"/> counts supplied deltas.
    /// </remarks>
    public IReadOnlyList<ChartWaterfallItem> WaterfallItems { get; private set; } = Array.Empty<ChartWaterfallItem>();

    internal void SetWaterfallItems(IEnumerable<ChartWaterfallItem> items) {
        if (items == null) throw new ArgumentNullException(nameof(items));
        var snapshot = items.ToArray();
        if (snapshot.Length == 0) throw new ArgumentException("A typed waterfall must contain at least one item.", nameof(items));
        var coordinates = new HashSet<double>();
        foreach (var item in snapshot) {
            if (item == null) throw new ArgumentException("Waterfall items must not contain null entries.", nameof(items));
            if (!coordinates.Add(item.X)) throw new ArgumentException("Waterfall display coordinates must be distinct.", nameof(items));
        }
        WaterfallItems = Array.AsReadOnly(snapshot);
        SourcePointCount = snapshot.Count(item => item.Kind == ChartWaterfallItemKind.Delta);
    }

    internal void ValidateWaterfall() {
        if (WaterfallItems.Count > 0 && Points.Count > 0)
            throw new InvalidOperationException("Typed waterfall items own this series. Raw points cannot be added to a typed waterfall.");
    }
}
