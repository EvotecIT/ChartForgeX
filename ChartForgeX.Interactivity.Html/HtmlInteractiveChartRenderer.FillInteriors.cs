using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Svg;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlInteractiveChartRenderer {
    // Scalar fills already contain flattened native contours. Interior witnesses are adapter
    // metadata only: live native fill, clipping and paint remain the browser's authority.
    private static Dictionary<string, List<ChartRect>> PreparedFillInteriors(PreparedVisual prepared) {
        var results = new Dictionary<string, List<ChartRect>>(StringComparer.Ordinal);
        var regions = new Dictionary<string, List<VisualSemanticRegion>>(StringComparer.Ordinal);
        foreach (var region in prepared.Regions) {
            if (region.Role != "point") continue;
            var separator = region.Id.IndexOf("-point-", StringComparison.Ordinal);
            if (separator < 0) continue;
            var series = region.Id.Substring(0, separator);
            if (!regions.TryGetValue(series, out var points)) regions.Add(series, points = new List<VisualSemanticRegion>());
            points.Add(region);
        }
        string? owner = null; ChartRect? clip = null;
        var stack = new Stack<(string? Owner, ChartRect? Clip)>();
        foreach (var node in prepared.Scene.Nodes) {
            if (node is VisualSceneGroup group) {
                stack.Push((owner, clip));
                if (group.Clip.HasValue) clip = clip.HasValue ? IntersectFillFootprint(clip.Value, group.Clip.Value) : group.Clip;
                if (group.Role == "series") owner = group.Metadata.TryGetValue("data-cfx-kind", out var kind)
                    && (kind == "Area" || kind == "StepArea" || kind == "StackedArea") ? group.Id : null;
            } else if (node is VisualSceneEndGroup) {
                var previous = stack.Pop(); owner = previous.Owner; clip = previous.Clip;
            } else if (owner != null && node is VisualScenePath path && path.Role == "area" && regions.TryGetValue(owner, out var points)) {
                var contours = VisualSceneGeometry.Flatten(path, 1);
                var left = double.PositiveInfinity; var top = double.PositiveInfinity;
                var right = double.NegativeInfinity; var bottom = double.NegativeInfinity;
                foreach (var contour in contours) for (var index = 0; index < contour.Count; index++) {
                    var point = contour[index];
                    var x = double.Parse(SvgMarkupWriter.FormatNumber(point.X), CultureInfo.InvariantCulture);
                    var y = double.Parse(SvgMarkupWriter.FormatNumber(point.Y), CultureInfo.InvariantCulture);
                    contour[index] = new ChartPoint(x, y);
                    left = Math.Min(left, x); top = Math.Min(top, y); right = Math.Max(right, x); bottom = Math.Max(bottom, y);
                }
                if (right <= left || bottom <= top) continue;
                var footprints = new List<ChartRect>(); var identities = new List<string>();
                foreach (var point in points) {
                    var bounds = IntersectFillFootprint(point.Bounds, left, top, right, bottom);
                    if (clip.HasValue) bounds = IntersectFillFootprint(bounds, clip.Value);
                    if (bounds.Width <= 0 || bounds.Height <= 0) continue;
                    footprints.Add(bounds); identities.Add(point.Id);
                }
                var interiors = VisualSceneGeometry.FindFillInteriors(contours, footprints, .000125);
                for (var index = 0; index < interiors.Length; index++) if (interiors[index].HasValue) {
                    if (!results.TryGetValue(identities[index], out var candidates)) results.Add(identities[index], candidates = new List<ChartRect>());
                    candidates.Add(interiors[index]!.Value);
                }
            }
        }
        return results;
    }

    private static ChartRect IntersectFillFootprint(ChartRect first, ChartRect second) =>
        IntersectFillFootprint(first, second.Left, second.Top, second.Right, second.Bottom);

    private static ChartRect IntersectFillFootprint(ChartRect first, double left, double top, double right, double bottom) {
        left = Math.Max(first.Left, left); top = Math.Max(first.Top, top);
        return new ChartRect(left, top, Math.Max(0, Math.Min(first.Right, right) - left), Math.Max(0, Math.Min(first.Bottom, bottom) - top));
    }
}
