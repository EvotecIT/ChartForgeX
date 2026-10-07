using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.SvgRaster;

namespace ChartForgeX.Rendering;

internal sealed partial class ChartLabelScene {
    private ChartRect? ResolveMarkClip(string? reference, SvgRasterMatrix matrix, ChartRect? inherited) {
        if (reference == null || !reference.StartsWith("url(#", StringComparison.Ordinal) || !reference.EndsWith(")", StringComparison.Ordinal)
            || !_definitions.TryGetClipPath(reference.Substring(5, reference.Length - 6), out var source)) return inherited;
        var definition = source.Element;
        var rect = definition.Children.FirstOrDefault(e => e.Name == "rect");
        if (rect == null || definition.Get("clipPathUnits") == "objectBoundingBox") return inherited;
        double Coordinate(string name) => double.TryParse(rect.Get(name), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var number) ? number : 0;
        var bounds = TransformBox(new ChartRect(Coordinate("x"), Coordinate("y"), Coordinate("width"), Coordinate("height")),
            matrix.Multiply(SvgRasterMatrix.ParseTransform(definition.Get("transform"))).Multiply(SvgRasterMatrix.ParseTransform(rect.Get("transform"))));
        return inherited.HasValue ? new ChartRect(Math.Max(inherited.Value.Left, bounds.Left), Math.Max(inherited.Value.Top, bounds.Top),
            Math.Max(0, Math.Min(inherited.Value.Right, bounds.Right) - Math.Max(inherited.Value.Left, bounds.Left)),
            Math.Max(0, Math.Min(inherited.Value.Bottom, bounds.Bottom) - Math.Max(inherited.Value.Top, bounds.Top))) : bounds;
    }

    private static bool IsMark(string role) => role is "bar" or "horizontal-bar" or "scatter-point" or "line-marker" or "line" or "area"
        or "range-band" or "range-area" or "funnel-segment" or "gauge-value" or "bullet-value" or "bullet-target" or "pie-slice" or "donut-slice"
        or "sankey-node" or "sankey-link" or "dotted-map-point" or "dotted-map-connector" or "region-map-region" or "tile-map-region"
        or "topology-node-body" or "topology-edge-path" or "annotation-line" or "annotation-band";

    private static LabelMarkShape? Shape(XElement element, SvgRasterMatrix matrix, SvgRasterStyle? style) {
        var rings = new List<List<ChartPoint>>();
        var fill = style == null || !style.Fill.IsNone && style.FillOpacity * style.Opacity > 0 && (style.Fill.Color?.A ?? 255) > 0;
        var stroke = style == null ? Number(element, "stroke-width") : style.Stroke.IsNone || style.StrokeOpacity * style.Opacity <= 0 || (style.Stroke.Color?.A ?? 255) == 0 ? 0 : style.StrokeWidth;
        if (!fill && stroke == 0) return null;
        switch (element.Name.LocalName) {
            case "rect":
                var x = Number(element, "x"); var y = Number(element, "y"); var w = Number(element, "width"); var h = Number(element, "height");
                rings.Add(new List<ChartPoint> { new(x, y), new(x + w, y), new(x + w, y + h), new(x, y + h) }); break;
            case "circle":
            case "ellipse":
                var cx = Number(element, "cx"); var cy = Number(element, "cy");
                var rx = Number(element, "rx", Number(element, "r")); var ry = Number(element, "ry", Number(element, "r"));
                var circle = new List<ChartPoint>();
                for (var i = 0; i < 32; i++) { var angle = i * Math.PI / 16; circle.Add(new ChartPoint(cx + Math.Cos(angle) * rx, cy + Math.Sin(angle) * ry)); }
                rings.Add(circle); break;
            case "path":
                var path = (string?)element.Attribute("d"); if (string.IsNullOrEmpty(path)) return null;
                rings = ChartMapPathParser.ParseRings(path!); break;
            case "line":
                fill = false;
                rings.Add(new List<ChartPoint> { new(Number(element, "x1"), Number(element, "y1")), new(Number(element, "x2"), Number(element, "y2")) }); break;
            case "polygon":
            case "polyline":
                var points = ((string?)element.Attribute("points") ?? "").Split(new[] { ' ', ',', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var polygon = new List<ChartPoint>();
                for (var i = 0; i + 1 < points.Length; i += 2) polygon.Add(new ChartPoint(double.Parse(points[i], System.Globalization.CultureInfo.InvariantCulture), double.Parse(points[i + 1], System.Globalization.CultureInfo.InvariantCulture)));
                rings.Add(polygon); break;
            default: return null;
        }
        for (var i = 0; i < rings.Count; i++) for (var j = 0; j < rings[i].Count; j++) rings[i][j] = matrix.Transform(rings[i][j]);
        return new LabelMarkShape(rings, fill, stroke * matrix.ScaleFactor);
    }

    private Mark? AssociatedMark(Entry entry) {
        if (!CanMove(LabelRole(entry.Element)) || entry.IsLegendItem) return null;
        var svg = entry.Element.AncestorsAndSelf().FirstOrDefault(e => e.Name.LocalName == "svg");
        if ((string?)entry.Element.Attribute("data-cfx-label-mark") is { } key) {
            return _marks.FirstOrDefault(mark => mark.SourceId == key && mark.SvgRoot == svg);
        }
        var series = (string?)entry.Element.Attribute("data-cfx-series");
        var point = (string?)entry.Element.Attribute("data-cfx-point");
        var labelRole = LabelRole(entry.Element);
        if (labelRole.StartsWith("funnel-", StringComparison.Ordinal)) {
            point ??= entry.Element.Ancestors().Select(e => (string?)e.Attribute("data-cfx-point")).FirstOrDefault(value => value != null);
        }
        if (labelRole == "dotted-map-label" && point != null) {
            return _marks.FirstOrDefault(mark => mark.SvgRoot == svg && mark.Kind == "dotted-map-point" && (string?)mark.Element.Attribute("data-cfx-point") == point);
        }
        if (labelRole is "bullet-target-label" or "bullet-value-label") {
            var row = entry.Element.Ancestors().FirstOrDefault(e => Role(e) == "bullet-row");
            return _marks.FirstOrDefault(mark => mark.SvgRoot == svg && mark.Owner == row
                && mark.Kind == (labelRole == "bullet-target-label" ? "bullet-target" : "bullet-value"));
        }
        if (labelRole.StartsWith("topology-edge", StringComparison.Ordinal)) {
            var edgeId = entry.Element.AncestorsAndSelf().Select(e => (string?)e.Attribute("data-edge-id")).FirstOrDefault(id => id != null);
            if (edgeId != null) return _marks.FirstOrDefault(mark => mark.SvgRoot == svg && mark.Kind == "topology-edge-path"
                && mark.Element.AncestorsAndSelf().Any(e => (string?)e.Attribute("data-edge-id") == edgeId));
        }
        if (labelRole == "sankey-node-label") {
            var node = (string?)entry.Element.Attribute("data-cfx-node");
            return _marks.FirstOrDefault(mark => mark.SvgRoot == svg && mark.Kind == "sankey-node"
                && (node != null ? (string?)mark.Element.Attribute("data-cfx-node") == node : (string?)mark.Element.Attribute("data-cfx-label") == entry.Text));
        }
        if (labelRole is "region-map-label" or "tile-map-label") {
            return _marks.FirstOrDefault(mark => mark.SvgRoot == svg && mark.Kind == labelRole.Replace("-label", "-region")
                && (string?)mark.Element.Attribute("data-cfx-region") == ((string?)entry.Element.Attribute("data-cfx-label-original") ?? entry.Text));
        }
        if (labelRole == "dotted-map-connector-label") {
            return _marks.FirstOrDefault(mark => mark.SvgRoot == svg && mark.Kind == "dotted-map-connector"
                && (string?)mark.Element.Attribute("data-cfx-connector") == (string?)entry.Element.Attribute("data-cfx-connector"));
        }
        if (point != null && LabelRole(entry.Element).StartsWith("funnel-", StringComparison.Ordinal)) {
            return _marks.FirstOrDefault(mark => mark.SvgRoot == svg && mark.Kind == "funnel-segment" && (string?)mark.Element.Attribute("data-cfx-point") == point);
        }
        if (series != null && point != null) {
            var own = _marks.FirstOrDefault(mark => mark.SvgRoot == svg && (string?)mark.Element.Attribute("data-cfx-series") == series
                && (string?)mark.Element.Attribute("data-cfx-point") == point);
            if (own != null) return own;
        }
        var nodeId = entry.Element.AncestorsAndSelf().Select(e => (string?)e.Attribute("data-node-id")).FirstOrDefault(id => id != null);
        if (nodeId != null) {
            var own = _marks.FirstOrDefault(mark => mark.SvgRoot == svg && mark.Kind == "topology-node-body" && mark.NodeId == nodeId);
            if (own != null) return own;
        }
        // Explicit point/node identities take precedence over geometric proximity.
        var ancestors = entry.Element.AncestorsAndSelf().ToArray();
        foreach (var mark in _marks) {
            if (mark.SvgRoot != svg || series != null && (string?)mark.Element.Attribute("data-cfx-series") != series) continue;
            if (ancestors.Contains(mark.Element)) return mark;
            var owner = mark.Owner;
            if (owner != null && ancestors.Contains(owner) && mark.Shape.Contains(entry.Box)) return mark;
        }
        var contained = _marks.Where(mark => mark.SvgRoot == svg && (series == null || (string?)mark.Element.Attribute("data-cfx-series") == series)
            && CanContain(entry, mark) && mark.Shape.Contains(entry.Box)).OrderBy(mark => mark.Shape.Bounds.Width * mark.Shape.Bounds.Height).FirstOrDefault();
        if (contained != null) return contained;
        var role = Role(entry.Element);
        var center = new ChartPoint(entry.Box.Left + entry.Box.Width / 2, entry.Box.Top + entry.Box.Height / 2);
        Mark? nearest = null; var distance = 64d * 64;
        foreach (var mark in _marks) {
            if (mark.SvgRoot != svg || series != null && (string?)mark.Element.Attribute("data-cfx-series") != series) continue;
            var box = mark.Shape.Bounds;
            var dx = Math.Max(box.Left - center.X, Math.Max(0, center.X - box.Right));
            var dy = Math.Max(box.Top - center.Y, Math.Max(0, center.Y - box.Bottom));
            var next = dx * dx + dy * dy;
            if (next < distance) { nearest = mark; distance = next; }
        }
        return nearest;
    }

    private static bool CanContain(Entry entry, Mark mark) {
        var labelRole = Role(entry.Element); var markRole = Role(mark.Element);
        if (labelRole.StartsWith("funnel", StringComparison.Ordinal)) return markRole == "funnel-segment";
        if (labelRole.StartsWith("gauge", StringComparison.Ordinal)) return false;
        if (labelRole.StartsWith("region-map", StringComparison.Ordinal) || labelRole.StartsWith("tile-map", StringComparison.Ordinal)) return markRole is "region-map-region" or "tile-map-region";
        if (labelRole == "data-label") return markRole is "bar" or "horizontal-bar" or "pie-slice" or "donut-slice";
        return false;
    }

    private static List<XElement> Decorations(XElement element, bool legend) {
        if (legend) return new List<XElement>(); // Swatches travel with their legend group.
        var role = Role(element);
        var result = new List<XElement>();
        var previous = PreviousElement(element);
        while (previous != null) {
            var previousRole = Role(previous);
            var match = previousRole.Contains("label-backdrop") || previousRole.Contains("label-backplate") || previousRole.Contains("label-leader") || previousRole.Contains("label-connector")
                || previousRole == "annotation-label" && role == "annotation-label-text"
                || previousRole.StartsWith("point-callout-label", StringComparison.Ordinal) && role == "point-callout-label-text"
                || previousRole == "gauge-status-marker" && role == "gauge-status-label";
            if (!match) break;
            result.Add(previous); previous = PreviousElement(previous);
        }
        return result;
    }

    // The nearest element sibling before this one, as ElementsBeforeSelf().LastOrDefault() returns, without walking every
    // earlier sibling from the first.
    private static XElement? PreviousElement(XElement element) {
        for (var node = element.PreviousNode; node != null; node = node.PreviousNode) {
            if (node is XElement previous) return previous;
        }

        return null;
    }

    private sealed class Mark {
        internal Mark(XElement element, string id, LabelMarkShape shape, string kind) {
            Element = element; Id = id; Shape = shape;
            Kind = kind;
            SourceId = (string?)element.Attribute("data-cfx-mark-key") ?? id;
            SvgRoot = element.Ancestors().FirstOrDefault(e => e.Name.LocalName == "svg");
            Owner = element.Ancestors().FirstOrDefault(e => Role(e) is "topology-node" or "bullet-row");
            NodeId = element.AncestorsAndSelf().Select(e => (string?)e.Attribute("data-node-id")).FirstOrDefault(value => value != null);
        }
        internal XElement Element { get; }
        internal string Id { get; }
        internal LabelMarkShape Shape { get; }
        internal XElement? Owner { get; }
        internal string SourceId { get; }
        internal string Kind { get; }
        internal string? NodeId { get; }
        internal XElement? SvgRoot { get; }
    }
}
