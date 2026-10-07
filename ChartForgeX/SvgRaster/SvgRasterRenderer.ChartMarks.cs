using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.SvgRaster;

internal static partial class SvgRasterRenderer {
    /// <summary>Paints chart geometry without allocating a full raster layer for each rectangular plot clip.</summary>
    internal static void PaintChartMarks(RgbaCanvas canvas, SvgRasterDocument document) {
        var definitions = SvgRasterDefinitions.From(document);
        var viewport = new SvgRasterViewport(document.ViewBox.Width, document.ViewBox.Height);
        PaintMarkChildren(canvas, document.Root, SvgRasterStyle.Default, SvgRasterMatrix.Identity, definitions,
            new List<SvgRasterElement>(), viewport);
    }

    private static void PaintMarkChildren(RgbaCanvas canvas, SvgRasterElement element, SvgRasterStyle parent,
        SvgRasterMatrix parentMatrix, SvgRasterDefinitions definitions, List<SvgRasterElement> ancestors, SvgRasterViewport viewport) {
        if (IsDefinitionElement(element.Name) || element.Name is "title" or "desc" or "text") return;
        var style = SvgRasterStyle.Resolve(parent, element, definitions.StyleSheet, ancestors);
        if (!style.Displayed || !style.VisibilityVisible || style.Opacity <= 0) return;
        var matrix = parentMatrix.Multiply(SvgRasterMatrix.ParseTransform(element.Get("transform")));
        var group = element.Name is "svg" or "g" or "a";
        var simpleClip = TryChartClip(style.ClipPath, matrix, definitions, out var bounds);
        if (!group || style.Opacity < .999 || element.Get("mask") != null || style.ClipPath != null && !simpleClip) {
            RenderElement(canvas, element, parent, parentMatrix, definitions, canvas.Width, canvas.Height, 0, ancestors, viewport);
            return;
        }
        using var clip = simpleClip ? canvas.PushClipBounds(bounds) : null;
        ancestors.Add(element);
        foreach (var child in element.Children) PaintMarkChildren(canvas, child, style, matrix, definitions, ancestors, viewport);
        ancestors.RemoveAt(ancestors.Count - 1);
    }

    private static bool TryChartClip(string? reference, SvgRasterMatrix matrix, SvgRasterDefinitions definitions, out ChartRect bounds) {
        bounds = default;
        if (!definitions.TryGetClipPath(ParseReference(reference), out var clip) || clip.Element.Children.Count != 1
            || clip.Element.Get("clipPathUnits") == "objectBoundingBox") return false;
        var rect = clip.Element.Children[0];
        if (rect.Name != "rect" || Math.Abs(matrix.B) > .000001 || Math.Abs(matrix.C) > .000001 || rect.Get("transform") != null) return false;
        var p = matrix.Transform(new ChartPoint(LabelCoordinate(rect, "x"), LabelCoordinate(rect, "y")));
        var end = matrix.Transform(new ChartPoint(LabelCoordinate(rect, "x") + LabelCoordinate(rect, "width"), LabelCoordinate(rect, "y") + LabelCoordinate(rect, "height")));
        bounds = new ChartRect(Math.Min(p.X, end.X), Math.Min(p.Y, end.Y), Math.Abs(end.X - p.X), Math.Abs(end.Y - p.Y));
        return true;
    }
}
