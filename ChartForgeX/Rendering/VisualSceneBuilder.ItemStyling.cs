using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal sealed partial class VisualSceneBuilder {
    /// <summary>Applies captured datum paint through the same source scopes used by navigation and accessibility.</summary>
    private void CaptureItemStyles(Chart chart) {
        if (!chart.Series.Any(series => series.HasItemStyler || series.Fill != null || series.PointFillOverrides.Count > 0)) return;
        var scopes = new Stack<(ChartSeries? Series, int Point, bool Relationship, bool Decoration)>();
        (ChartSeries? Series, int Point, bool Relationship, bool Decoration) scope = (null, -1, false, false);
        for (var index = 0; index < _nodes.Count; index++) {
            var node = _nodes[index];
            if (node is VisualSceneEndGroup) { scope = scopes.Pop(); continue; }
            if (node is VisualSceneGroup group) {
                scopes.Push(scope);
                var metadata = group.Metadata;
                if (metadata.TryGetValue("data-cfx-series", out var ordinal)
                    && int.TryParse(ordinal, NumberStyles.None, CultureInfo.InvariantCulture, out var seriesIndex)
                    && seriesIndex >= 0 && seriesIndex < chart.Series.Count) {
                    if (!ReferenceEquals(scope.Series, chart.Series[seriesIndex])) { scope.Point = -1; scope.Relationship = false; }
                    scope.Series = chart.Series[seriesIndex];
                }
                if (metadata.TryGetValue("data-cfx-source-link-index", out var link)
                    && int.TryParse(link, NumberStyles.None, CultureInfo.InvariantCulture, out var linkIndex)) {
                    scope.Point = linkIndex; scope.Relationship = true;
                } else if (TryStylingObservationIndex(metadata, out var observation)) { scope.Point = observation; scope.Relationship = false; }
                else if (metadata.ContainsKey("data-cfx-source-points") || metadata.ContainsKey("data-cfx-derived")
                    || metadata.ContainsKey("data-cfx-source-link-index")) scope.Point = -1;
                scope.Decoration |= metadata.ContainsKey("data-cfx-label-decoration") || group.Role is "legend-entry" or "legend-entry-omitted";
                continue;
            }
            if (scope.Decoration || scope.Series == null) continue;
            var gradient = node as VisualSceneGradient;
            var mark = gradient?.Shape ?? node as VisualSceneMark;
            if (mark == null || KeepsSemanticPaint(mark)) continue;
            var style = scope.Relationship ? scope.Series.PreparedRelationshipStyle(scope.Point) : scope.Series.PreparedItemStyle(scope.Point);
            var fill = style?.Fill ?? (style?.Color.HasValue == true ? null : scope.Series.ResolveFill(scope.Point));
            if (style == null && fill == null) continue;
            // Existing family gradients retain a null solid fill; the wrapper itself supplies their interior.
            if (gradient != null && !mark.Fill.HasValue)
                mark = VisualSceneMarkStyle.WithPaint(mark, gradient.Stops.Count > 0 ? gradient.Stops[0].Color : ChartColor.Transparent,
                    mark.Stroke, mark.StrokeWidth, null);
            var styled = style == null ? mark : StyleItemMark(mark, style, _capturedMarkerStrokes?.Contains(mark) == true);
            _nodes[index] = fill != null ? VisualSceneChartFills.ApplyFill(styled, fill)
                : gradient != null && style?.Color.HasValue != true ? gradient.WithShape(styled) : styled;
        }
    }

    // A displayed decimated point is still the source of its paint. Derived aggregates do not borrow one contributor's style.
    private static bool TryStylingObservationIndex(IReadOnlyDictionary<string, string> metadata, out int index) {
        index = -1;
        if (metadata.TryGetValue("data-cfx-source-points", out var sources) && sources.IndexOf(',') >= 0) return false;
        if (metadata.TryGetValue("data-cfx-observation-index", out var value)
            || metadata.TryGetValue("data-cfx-source-node-index", out value))
            return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out index);
        return (metadata.TryGetValue("data-cfx-point", out value) || metadata.TryGetValue("data-cfx-source-point", out value))
            && int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out index);
    }

    private static VisualSceneMark StyleItemMark(VisualSceneMark mark, ChartItemStyle style, bool capturedMarkerStroke) {
        var fill = mark.Fill.HasValue ? style.Color ?? mark.Fill : null;
        var stroke = capturedMarkerStroke ? null : style.Stroke;
        var ink = stroke?.Color ?? (!mark.Fill.HasValue ? style.Color : null) ?? mark.Stroke;
        if (stroke != null && !ink.HasValue) ink = fill ?? style.Color;
        if (ink.HasValue && stroke != null) ink = ChartColorMath.WithOpacity(ink.Value, stroke.Opacity);
        return VisualSceneMarkStyle.WithPaint(mark, fill, ink, stroke?.Width ?? mark.StrokeWidth, stroke?.Dash?.ToArray());
    }

    // Surface rings, label backdrops, state overlays and hatch ink retain their semantic colors.
    private static bool KeepsSemanticPaint(VisualSceneMark mark) =>
        mark.Role != null && (mark.Role.EndsWith("-ring", StringComparison.Ordinal) && mark.Role != "progress-ring-ring"
            || mark.Role.IndexOf("pattern", StringComparison.Ordinal) >= 0 || mark.Role.IndexOf("label", StringComparison.Ordinal) >= 0
            || mark.Role.EndsWith("-center", StringComparison.Ordinal) || mark.Role.IndexOf("track", StringComparison.Ordinal) >= 0
            || mark.Role.StartsWith("mark-state", StringComparison.Ordinal));
}
