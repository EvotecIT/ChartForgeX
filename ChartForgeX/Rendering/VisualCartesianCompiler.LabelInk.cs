using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    // Placement owns the chosen candidate. Ink for a contained alternative is selected only
    // after that choice, so outside labels keep their configured typography and foreground.
    private sealed class CartesianLabels : List<LabelPlacementRequest> {
        private readonly Dictionary<(ChartSeries Series, int Index), ResolvedPointLabel> _points = new();
        internal readonly Dictionary<LabelPlacementRequest, string> LabelIds = new();
        internal readonly Dictionary<LabelPlacementRequest, (ChartRect Bounds, TextStyle Style, SvgPaint? Paint)> ContainedInk = new();
        internal readonly HashSet<LabelPlacementRequest> Outlined = new();

        private readonly Dictionary<ChartSeries, System.Func<double, string>?> _seriesFormats = new();

        internal ResolvedPointLabel PointLabel(Chart chart, ChartSeries series, int pointIndex, TextStyle style) {
            var key = (series, pointIndex);
            if (!_points.TryGetValue(key, out var label)) {
                if (!_seriesFormats.TryGetValue(series, out var format))
                    _seriesFormats.Add(series, format = SeriesCaptionFormat(chart, series));
                label = ResolvePointLabel(chart, series, pointIndex, style, format);
                _points.Add(key, label);
            }
            return label;
        }
    }
}
