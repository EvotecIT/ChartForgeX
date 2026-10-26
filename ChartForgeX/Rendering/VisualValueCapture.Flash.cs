using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal sealed partial class VisualValueCapture {
    private static readonly HashSet<string> FlashRoles = new(StringComparer.Ordinal) {
        "progress-fill", "layered-radial-layer", "layered-radial-layer-cap", "circle-value", "circle-value-cap",
        "progress-ring-ring", "progress-ring-ring-cap", "gauge-value", "gauge-value-marker", "gauge-needle", "bullet-value"
    };

    private static readonly HashSet<string> CartesianPointFlashRoles = new(StringComparer.Ordinal) {
        "marker", "marker-pattern", "bar", "bar-pattern", "bar-highlight", "bar-cap-shadow-soft", "bar-cap-shadow", "bar-cap", "bar-cap-highlight"
    };
    private static readonly HashSet<string> SharedLineFlashRoles = new(StringComparer.Ordinal) {
        "line", "line-ambient-halo", "line-halo", "line" + ChartLineVisualLayer.HighlightSuffix
    };
    private static readonly HashSet<string> SharedAreaFlashRoles = new(StringComparer.Ordinal) { "area", "area-pattern" };
    // A paired envelope is shared series ink, like the existing Line path. Tuple markers keep their point owner.
    private static readonly HashSet<string> SharedRangeFlashRoles = new(StringComparer.Ordinal) {
        "range-band", "range-band-pattern", "range-area", "range-area-pattern", "range-midline",
        "range-upper", "range-upper-ambient-halo", "range-upper-halo", "range-upper" + ChartLineVisualLayer.HighlightSuffix,
        "range-lower", "range-lower-ambient-halo", "range-lower-halo", "range-lower" + ChartLineVisualLayer.HighlightSuffix
    };
    private static readonly HashSet<string> RadialFlashRoles = new(StringComparer.Ordinal) { "pie-slice", "donut-slice", "radial-slice-outline", "radial-fill-pattern" };
    private static readonly HashSet<string> HierarchyFlashRoles = new(StringComparer.Ordinal) { "treemap-group-mark", "treemap-tile-mark", "treemap-pattern", "sunburst-segment-mark", "sunburst-pattern" };

    /// <summary>Repaints actual captured value ink; shared Line/Range ink is series-owned and point markers remain observation-owned.</summary>
    private static PreparedVisual Flash(PreparedVisual source, FlashSelection changed, ChartColor peak, double amount) {
        var scene = source.Scene; var nodes = new VisualSceneNode[scene.Nodes.Count];
        var scopes = new Stack<(string? Series, string? Observation, string? Node, bool Other, bool Point)>();
        var current = (Series: (string?)null, Observation: (string?)null, Node: (string?)null, Other: false, Point: false);
        for (var index = 0; index < nodes.Length; index++) {
            var node = scene.Nodes[index]; nodes[index] = node;
            if (node is VisualSceneGroup group) {
                scopes.Push(current);
                if (group.Metadata.TryGetValue("data-cfx-series-key", out var series)) {
                    if (current.Series != series) { current.Observation = null; current.Node = null; current.Other = false; current.Point = false; }
                    current.Series = series;
                }
                if (group.Metadata.TryGetValue("data-cfx-observation-id", out var observation)) current.Observation = observation;
                if (group.Metadata.TryGetValue("data-cfx-node", out var nativeNode)) current.Node = nativeNode;
                if (group.Role == "point") current.Point = true;
                if (group.Role == "radial-point") current.Other = group.Metadata.TryGetValue("data-cfx-point", out var point) && point == "-1";
            } else if (node is VisualSceneEndGroup) current = scopes.Pop();
            else if (current.Series != null && node.Role != null &&
                (current.Observation != null && changed.Readings.Contains((current.Series, current.Observation)) && (changed.CartesianPointInk && current.Point && (node is VisualSceneMark or VisualSceneGradient) || FlashRoles.Contains(node.Role) || CartesianPointFlashRoles.Contains(node.Role) || RadialFlashRoles.Contains(node.Role))
                || changed.Series.Contains(current.Series) && !current.Point && (SharedLineFlashRoles.Contains(node.Role) || SharedAreaFlashRoles.Contains(node.Role) || changed.RangeEnvelopeInk && SharedRangeFlashRoles.Contains(node.Role))
                || current.Node != null && changed.Nodes.Contains((current.Series, current.Node)) && HierarchyFlashRoles.Contains(node.Role)
                || current.Other && changed.OtherSeries.Contains(current.Series) && RadialFlashRoles.Contains(node.Role))) {
                nodes[index] = node is VisualSceneGradient gradient
                    ? new VisualSceneGradient(Clone(gradient.Shape), gradient.Start, gradient.End, gradient.Stops.Select(stop => new VisualGradientStop(stop.Offset,
                        Blend(stop.Color)!.Value, Binding(stop.Color, stop.Paint))).ToArray())
                    : node is VisualSceneMark mark ? Clone(mark) : throw new NotSupportedException("Native flash ink requires a supported completed native mark or gradient.");
            }
        }
        return source.WithNativeFrame(new VisualScene(scene.Size, nodes, scene.Diagnostics, scene.Regions, scene.DataProjection));

        VisualSceneMark Clone(VisualSceneMark mark) {
            var fill = Blend(mark.Fill); var stroke = Blend(mark.Stroke);
            var paint = new VisualScenePaintBinding(Binding(mark.Fill, mark.Paint?.Fill), Binding(mark.Stroke, mark.Paint?.Stroke));
            return mark switch {
                VisualSceneRectangle rect => new VisualSceneRectangle(rect.Bounds, rect.Radius, fill, stroke, rect.StrokeWidth, rect.Role, rect.Id, paint, rect.Opacity),
                VisualSceneEllipse ellipse => new VisualSceneEllipse(ellipse.Cx, ellipse.Cy, ellipse.Rx, ellipse.Ry, fill, stroke, ellipse.StrokeWidth, ellipse.Role, ellipse.Id, paint, ellipse.Opacity),
                VisualSceneLine line => new VisualSceneLine(line.Start, line.End, stroke!.Value, line.StrokeWidth, line.Role, line.Id, line.Dash?.ToArray(), paint, line.DashOffset, line.Opacity),
                VisualScenePath path => new VisualScenePath(new ChartPath(path.Commands), path.Close, fill, stroke, path.StrokeWidth, path.Role, path.Id, path.Dash?.ToArray(), path.Cap, path.Join, paint, path.DashOffset, path.Opacity),
                VisualSceneSlice slice => new VisualSceneSlice(slice.Cx, slice.Cy, slice.Outer, slice.Inner, slice.Start, slice.Sweep, fill!.Value, stroke, slice.StrokeWidth, slice.Role, slice.Id, paint, slice.Opacity),
                _ => throw new NotSupportedException("The native flash value painter requested an unsupported mark.")
            };
        }
        ChartColor? Blend(ChartColor? color) => color.HasValue ? ChartColorMath.BlendPremultiplied(color.Value, peak, amount) : null;
        SvgPaint? Binding(ChartColor? color, SvgPaint? paint) => color.HasValue
            ? SvgPaint.Mix(Blend(color)!.Value, paint ?? SvgPaint.Literal(color.Value), SvgPaint.Literal(peak), amount) : null;
    }
}
