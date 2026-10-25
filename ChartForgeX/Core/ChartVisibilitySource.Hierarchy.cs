using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Rendering;

namespace ChartForgeX.Core;

internal sealed partial class ChartVisibilitySource {
    private readonly HierarchyPolicy? _hierarchy;
    internal HierarchyPolicy? Hierarchy => _hierarchy;
    private static void ValidateHierarchySource(Chart chart) {
        if (!chart.Series.Any(series => series.Kind is ChartSeriesKind.Treemap or ChartSeriesKind.Sunburst)) return;
        if (chart.Series.Count != 1 || chart.Series[0].HierarchyItems.Count == 0)
            throw new NotSupportedException("Native hierarchy capture requires one nonempty authored Treemap or Sunburst source.");
        var series = chart.Series[0];
        var policy = series.Kind == ChartSeriesKind.Sunburst ? chart.Options.Sunburst.ParentValuePolicy : ChartHierarchyValuePolicy.LeafAggregate;
        _ = series.Relationships!.ResolveHierarchyValues(policy); // Full source, never a filtered substitute.
        if (series.Kind == ChartSeriesKind.Sunburst && chart.Options.Sunburst.SecondaryLabelFormatter != null)
            throw new NotSupportedException("Captured hierarchy frames do not retain a mutable secondary-caption callback; ordinary native preparation remains available.");
        var dimension = series.Kind == ChartSeriesKind.Sunburst ? chart.Options.Sunburst.ColorDimension : chart.Options.Treemap.ColorDimension;
        if (dimension.ValueFormat != null) _ = dimension.ValueFormat.PortablePolicy();
    }
    internal sealed class HierarchyPolicy {
        private readonly ChartTreemapOptions? _treemap;
        private readonly ChartSunburstOptions? _sunburst;
        internal HierarchyPolicy(Chart source) {
            if (source.Series[0].Kind == ChartSeriesKind.Treemap) _treemap = source.Options.Treemap.Clone(); else _sunburst = source.Options.Sunburst.Clone();
            Root = source.HierarchyPreparation?.Request == null ? null : new ChartHierarchyView(source.HierarchyPreparation.Request.RootId);
        }
        internal ChartHierarchyView? Root { get; }
        internal ChartTreemapOptions? CopyTreemap() => _treemap?.Clone();
        internal ChartSunburstOptions? CopySunburst() => _sunburst?.Clone();
        internal void Apply(ChartOptions target) { if (_treemap != null) target.Treemap = _treemap.Clone(); if (_sunburst != null) target.Sunburst = _sunburst.Clone(); }
    }
    internal sealed partial class Series {
        private IReadOnlyList<ChartHierarchyItem> _hierarchyItems = Array.Empty<ChartHierarchyItem>();
        private IReadOnlyDictionary<string, ChartSeriesState> _nodeStates = new Dictionary<string, ChartSeriesState>();
        private IReadOnlyList<ChartForgeX.Primitives.ChartColor?> _pointStrokes = Array.Empty<ChartForgeX.Primitives.ChartColor?>();
        private void CaptureHierarchy(ChartSeries source) {
            _hierarchyItems = Array.AsReadOnly(source.HierarchyItems.ToArray());
            _nodeStates = new System.Collections.ObjectModel.ReadOnlyDictionary<string, ChartSeriesState>(source.NodeStates.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal));
            _pointStrokes = Array.AsReadOnly(source.PointStrokeColors.ToArray());
        }
        internal bool IsHierarchy => Kind is ChartSeriesKind.Treemap or ChartSeriesKind.Sunburst;
        internal IReadOnlyList<ChartHierarchyItem> HierarchyItems => _hierarchyItems;
        internal IReadOnlyDictionary<string, ChartSeriesState> NodeStates => _nodeStates;
        internal IReadOnlyList<ChartForgeX.Primitives.ChartColor?> PointStrokes => _pointStrokes;
    }
}
