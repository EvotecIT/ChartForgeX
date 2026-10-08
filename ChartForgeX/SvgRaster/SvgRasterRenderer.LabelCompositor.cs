using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.SvgRaster;

internal static partial class SvgRasterRenderer {
    /// <summary>Composes a tight label layer on the destination device grid, including opacity, clipping and rich text.</summary>
    private static void PaintCompositeLabel(RgbaCanvas canvas, SvgRasterElement element, SvgRasterStyle parent, SvgRasterMatrix parentMatrix,
        SvgRasterStyle style, SvgRasterMatrix matrix, SvgRasterDefinitions definitions, List<SvgRasterElement> ancestors, SvgRasterViewport viewport) {
        var points = new List<ChartPoint>();
        LabelLayerBounds(element, matrix, viewport, points);
        if (points.Count == 0) return;
        var left = double.PositiveInfinity; var top = double.PositiveInfinity;
        var right = double.NegativeInfinity; var bottom = double.NegativeInfinity;
        foreach (var point in points) { left = Math.Min(left, point.X); top = Math.Min(top, point.Y); right = Math.Max(right, point.X); bottom = Math.Max(bottom, point.Y); }
        var padding = style.Filter == null ? 4 : 48;
        var x = Math.Max(0, (int)Math.Floor(left - padding)); var y = Math.Max(0, (int)Math.Floor(top - padding));
        var width = Math.Min(canvas.Width, (int)Math.Ceiling(right + padding)) - x;
        var height = Math.Min(canvas.Height, (int)Math.Ceiling(bottom + padding)) - y;
        if (width <= 0 || height <= 0) return;
        var density = canvas.DeviceScale;
        var layer = new RgbaCanvas(width * density, height * density, 1) { TextHinting = canvas.TextHinting, FontStrikeScale = canvas.FontStrikeScale };
        var deviceMatrix = SvgRasterMatrix.Translate(-x * density, -y * density).Multiply(SvgRasterMatrix.Scale(density, density)).Multiply(parentMatrix);
        RenderElement(layer, element, parent, deviceMatrix, definitions, layer.Width, layer.Height, 0, ancestors, viewport);
        canvas.DrawImage(x, y, layer.Width, layer.Height, layer.Pixels);
    }

    private static void LabelLayerBounds(SvgRasterElement element, SvgRasterMatrix matrix, SvgRasterViewport viewport, List<ChartPoint> points) {
        if (element.Get("display") == "none" || IsDefinitionElement(element.Name)) return;
        if (element.Name == "svg") {
            var nested = ResolveNestedSvgViewport(element, viewport);
            matrix = ApplyNestedSvgViewport(element, matrix, nested);
            viewport = nested.UserViewport;
        }
        if (element.Get("data-cfx-label-x") != null) {
            var x = LabelCoordinate(element, "data-cfx-label-x"); var y = LabelCoordinate(element, "data-cfx-label-y");
            points.Add(new ChartPoint(x, y)); points.Add(new ChartPoint(x + LabelCoordinate(element, "data-cfx-label-width"), y + LabelCoordinate(element, "data-cfx-label-height")));
        }
        if (element.Get("data-cfx-label-decoration") == "true") {
            foreach (var contour in ClipContours(element, matrix, viewport)) points.AddRange(contour);
            if (element.Name == "line") {
                points.Add(matrix.Transform(new ChartPoint(LabelCoordinate(element, "x1"), LabelCoordinate(element, "y1"))));
                points.Add(matrix.Transform(new ChartPoint(LabelCoordinate(element, "x2"), LabelCoordinate(element, "y2"))));
            } else if (element.Name == "polyline") points.AddRange(TransformRing(ReadPointList(element.Get("points")), matrix));
        }
        foreach (var child in element.Children) LabelLayerBounds(child, matrix.Multiply(SvgRasterMatrix.ParseTransform(child.Get("transform"))), viewport, points);
    }
}
