using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.SvgRaster;

internal static partial class SvgRasterRenderer {
    private static void RenderMarkers(RgbaCanvas canvas, SvgRasterElement element, SvgRasterStyle style, SvgRasterMatrix pathMatrix, SvgRasterDefinitions definitions, IReadOnlyList<List<ChartPoint>> contours, int width, int height, int referenceDepth, IReadOnlyList<SvgRasterElement> ancestors) {
        if (referenceDepth >= 8) return;
        var startId = ReferenceId(element, "marker-start");
        var endId = ReferenceId(element, "marker-end");
        if (startId == null && endId == null) return;

        foreach (var contour in contours) {
            if (contour.Count < 2) continue;
            if (startId != null) RenderMarker(canvas, startId, contour[0], contour[1], true, style, pathMatrix, definitions, width, height, referenceDepth, ancestors);
            if (endId != null) RenderMarker(canvas, endId, contour[contour.Count - 1], contour[contour.Count - 2], false, style, pathMatrix, definitions, width, height, referenceDepth, ancestors);
        }
    }

    private static void RenderMarker(RgbaCanvas canvas, string markerId, ChartPoint point, ChartPoint adjacent, bool start, SvgRasterStyle pathStyle, SvgRasterMatrix pathMatrix, SvgRasterDefinitions definitions, int width, int height, int referenceDepth, IReadOnlyList<SvgRasterElement> ancestors) {
        if (!definitions.TryGetElement(markerId, out var marker) || !string.Equals(marker.Name, "marker", StringComparison.Ordinal)) return;
        var markerWidth = Math.Max(0, marker.GetDouble("markerWidth", 3));
        var markerHeight = Math.Max(0, marker.GetDouble("markerHeight", 3));
        if (markerWidth <= 0 || markerHeight <= 0) return;

        var markerUnits = string.Equals(marker.Get("markerUnits"), "userSpaceOnUse", StringComparison.Ordinal) ? 1 : Math.Max(0.000001, pathStyle.StrokeWidth);
        var scale = pathMatrix.ScaleFactor * markerUnits;
        var pixelWidth = Math.Max(1, (int)Math.Round(markerWidth * scale));
        var pixelHeight = Math.Max(1, (int)Math.Round(markerHeight * scale));
        var viewBox = string.IsNullOrWhiteSpace(marker.Get("viewBox"))
            ? new SvgRasterViewBox(0, 0, markerWidth, markerHeight)
            : SvgRasterViewBox.Parse(marker.Get("viewBox")!);
        var fit = SvgRasterMatrix.FromFit(viewBox, pixelWidth, pixelHeight, marker.Get("preserveAspectRatio"));
        var reference = fit.Transform(new ChartPoint(marker.GetDouble("refX"), marker.GetDouble("refY")));
        var rotation = MarkerRotation(marker.Get("orient"), point, adjacent, start);
        var markerMatrix = SvgRasterMatrix.Translate(point.X, point.Y)
            .Multiply(SvgRasterMatrix.Rotate(rotation))
            .Multiply(SvgRasterMatrix.Translate(-reference.X, -reference.Y))
            .Multiply(fit);

        var markerAncestors = new List<SvgRasterElement>(ancestors);
        var markerStyle = SvgRasterStyle.Resolve(SvgRasterStyle.Default, marker, definitions.StyleSheet, markerAncestors);
        markerAncestors.Add(marker);
        foreach (var child in marker.Children) RenderElement(canvas, child, markerStyle, markerMatrix, definitions, width, height, referenceDepth + 1, markerAncestors);
    }

    private static double MarkerRotation(string? orient, ChartPoint point, ChartPoint adjacent, bool start) {
        if (!string.IsNullOrWhiteSpace(orient)) {
            var value = orient!.Trim();
            if (!string.Equals(value, "auto", StringComparison.OrdinalIgnoreCase) && !string.Equals(value, "auto-start-reverse", StringComparison.OrdinalIgnoreCase)) {
                if (value.EndsWith("deg", StringComparison.OrdinalIgnoreCase)) value = value.Substring(0, value.Length - 3).Trim();
                if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var explicitRotation)) return explicitRotation;
            }
        }

        var reverseStart = start && string.Equals(orient?.Trim(), "auto-start-reverse", StringComparison.OrdinalIgnoreCase);
        var dx = reverseStart ? point.X - adjacent.X : start ? adjacent.X - point.X : point.X - adjacent.X;
        var dy = reverseStart ? point.Y - adjacent.Y : start ? adjacent.Y - point.Y : point.Y - adjacent.Y;
        return Math.Atan2(dy, dx) * 180 / Math.PI;
    }
}
