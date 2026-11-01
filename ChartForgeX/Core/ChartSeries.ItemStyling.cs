using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    private Func<ChartSeries, ChartSeriesInfo, int, ChartItemStyle?>? _itemStyler;
    private IReadOnlyList<ChartItemStyle?>? _preparedItemStyles;

    /// <summary>Sets a typed observation styling callback. Null clears it. Callbacks run once per display observation during Prepare, never during export.</summary>
    /// <remarks>Use the family's authored type: numeric ChartPoint, ChartBubble, ChartInterval, ChartRangeBand,
    /// ChartBoxPlot, ChartHierarchyItem, and so on. A mismatched type rejects before installation, including on empty series.
    /// Keep callbacks pure: output must depend only on their arguments and immutable captured configuration.
    /// Current/selected mark metadata supplies static highlight state; browser hover remains in the optional adapter.
    /// A throwing callback prevents publication of a prepared result and leaves authored presentation unchanged.</remarks>
    /// <typeparam name="TDatum">The series' authored observation type.</typeparam>
    /// <param name="styler">Returns immutable overrides, or null to retain the authored presentation.</param>
    /// <returns>The current series.</returns>
    public ChartSeries WithItemStyler<TDatum>(Func<ChartItemStyleContext<TDatum>, ChartItemStyle?>? styler) {
        if (TrySetRelationshipItemStyler(styler)) return this;
        if (typeof(TDatum) != DatumType)
            throw new ArgumentException("This series uses " + DatumType.Name + " observations, not " + typeof(TDatum).Name + ".", nameof(styler));
        _itemStyler = styler == null ? null : (series, info, index) => styler(new ChartItemStyleContext<TDatum>(
            (TDatum)series.ObservationDatum(index)!, info, index, series.ObservationMarkOptions(index) ?? series.MarkOptions,
            ChartRelationshipPaint.State(series, index)));
        return this;
    }

    internal ChartSeries PrepareItemStyles() {
        if (_itemStyler == null && _relationshipItemStyler == null && !HasFormatters) return this;
        var view = (ChartSeries)MemberwiseClone();
        view._itemStyler = null;
        var info = new ChartSeriesInfo(this);
        view.ResolveRelationshipItemStyles(this, info);
        if (_itemStyler == null) return view;
        view.PointColors = new List<ChartColor?>(PointColors);
        var styles = new ChartItemStyle?[ObservationCount];
        for (var index = 0; index < styles.Length; index++) {
            var style = _itemStyler(this, info, index);
            if (style?.MarkerSnapshot is { } marker) {
                if (!ChartSeriesKindTraits.SupportsMarkers(Kind))
                    throw new InvalidOperationException("Series '" + Name + "' does not support item marker overrides.");
                if (Kind == ChartSeriesKind.Bubble && marker.Radius.HasValue)
                    throw new InvalidOperationException("Bubble item sizes use the shared bubble scale; item marker radius cannot replace it.");
            }
            styles[index] = style;
            if (style?.Color is { } color) {
                while (view.PointColors.Count <= index) view.PointColors.Add(null);
                view.PointColors[index] = color;
            }
        }
        view._preparedItemStyles = Array.AsReadOnly(styles);
        return view;
    }

    internal ChartItemStyle? PreparedItemStyle(int index) => index >= 0 && _preparedItemStyles != null && index < _preparedItemStyles.Count
        ? _preparedItemStyles[index] : null;
    internal bool HasItemStyler => _itemStyler != null || _preparedItemStyles != null || _relationshipItemStyler != null || _preparedRelationshipStyles != null;
    internal bool HasPresentationCallbacks => _itemStyler != null || _relationshipItemStyler != null || HasFormatters;
    internal IReadOnlyList<ChartItemStyle?> CaptureItemStyles() => _preparedItemStyles ?? Array.Empty<ChartItemStyle?>();

    internal ChartMarkOptions? ObservationMarkOptions(int index) {
        if (index < 0 || index >= ObservationCount) return null;
        if (IsCategoricalHeatmapRow) return HeatmapCells[index]?.MarkOptions;
        return Kind switch {
            ChartSeriesKind.Gantt => GanttItems[index].MarkOptions,
            ChartSeriesKind.GanttLane => GanttLaneItems[index].MarkOptions,
            ChartSeriesKind.StateTimeline => StateTimelineSegments[index].MarkOptions,
            ChartSeriesKind.CalendarHeatmap => CalendarItems[index].MarkOptions,
            ChartSeriesKind.RangeBand or ChartSeriesKind.RangeArea => Ranges[index].MarkOptions,
            ChartSeriesKind.RangeBar => Intervals[index].MarkOptions,
            ChartSeriesKind.Dumbbell => Dumbbells[index].MarkOptions,
            _ => index < Points.Count ? Points[index].MarkOptions : null
        };
    }
}
