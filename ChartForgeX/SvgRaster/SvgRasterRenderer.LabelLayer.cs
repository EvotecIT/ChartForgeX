using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.SvgRaster;

internal static partial class SvgRasterRenderer {
    [ThreadStatic] private static TrueTypeFont? LabelLayerFont;

    /// <summary>Paints positioned text and its decorations over native marks without rasterizing the chart twice.</summary>
    internal static void PaintLabelLayer(RgbaCanvas canvas, SvgRasterDocument document, TrueTypeFont? explicitFont = null) {
        var previous = LabelLayerFont;
        LabelLayerFont = explicitFont;
        try {
            var definitions = SvgRasterDefinitions.From(document);
            var ancestors = new List<SvgRasterElement>();
            var viewport = new SvgRasterViewport(document.ViewBox.Width, document.ViewBox.Height);
            PaintLabelChildren(canvas, document.Root, SvgRasterStyle.Default, SvgRasterMatrix.Identity, definitions, ancestors, viewport);
        } finally { LabelLayerFont = previous; }
    }

    private static void PaintLabelChildren(RgbaCanvas canvas, SvgRasterElement element, SvgRasterStyle parent, SvgRasterMatrix parentMatrix,
        SvgRasterDefinitions definitions, List<SvgRasterElement> ancestors, SvgRasterViewport viewport) {
        if (IsDefinitionElement(element.Name)) return;
        var style = SvgRasterStyle.Resolve(parent, element, definitions.StyleSheet, ancestors);
        if (!style.Displayed || !style.VisibilityVisible) return;
        var matrix = parentMatrix.Multiply(SvgRasterMatrix.ParseTransform(element.Get("transform")));
        // Group opacity, masks and clipping apply to the composed descendants, rather than
        // inheriting into each glyph. Use the existing compositor for these uncommon layers.
        if (element.Name != "text" && (style.Opacity < 0.999 || style.ClipPath != null || style.Filter != null || element.Get("mask") != null || element.Name == "svg" && ancestors.Count != 0)) {
            PaintCompositeLabel(canvas, element, parent, parentMatrix, style, matrix, definitions, ancestors, viewport);
            return;
        }
        if (element.Name == "text" || element.Get("data-cfx-label-decoration") == "true") {
            if (element.Name == "text" && TryPaintSimpleLabel(canvas, element, style, matrix)) return;
            if (element.Name == "rect" && TryPaintLabelRect(canvas, element, style, matrix)) return;
            PaintCompositeLabel(canvas, element, parent, parentMatrix, style, matrix, definitions, ancestors, viewport);
            return;
        }
        ancestors.Add(element);
        foreach (var child in element.Children) PaintLabelChildren(canvas, child, style, matrix, definitions, ancestors, viewport);
        ancestors.RemoveAt(ancestors.Count - 1);
    }

    private static bool TryPaintLabelRect(RgbaCanvas canvas, SvgRasterElement element, SvgRasterStyle style, SvgRasterMatrix matrix) {
        if (Math.Abs(matrix.A - 1) > 0.00001 || Math.Abs(matrix.D - 1) > 0.00001 || Math.Abs(matrix.B) > 0.00001 || Math.Abs(matrix.C) > 0.00001
            || style.Fill.IsReference || style.Stroke.IsReference || element.Get("filter") != null) return false;
        var point = matrix.Transform(new ChartPoint(LabelCoordinate(element, "x"), LabelCoordinate(element, "y")));
        var width = LabelCoordinate(element, "width"); var height = LabelCoordinate(element, "height"); var radius = LabelCoordinate(element, "rx");
        ChartColor Paint(SvgRasterPaint paint, double opacity) { var color = paint.Color ?? style.Color; return new ChartColor(color.R, color.G, color.B, (byte)Math.Round(color.A * opacity * style.Opacity)); }
        if (!style.Fill.IsNone) canvas.FillRoundedRect(point.X, point.Y, width, height, radius, Paint(style.Fill, style.FillOpacity));
        if (!style.Stroke.IsNone) canvas.StrokeRoundedRect(point.X, point.Y, width, height, radius, Paint(style.Stroke, style.StrokeOpacity), style.StrokeWidth);
        return true;
    }

    private static bool TryPaintSimpleLabel(RgbaCanvas canvas, SvgRasterElement element, SvgRasterStyle style, SvgRasterMatrix matrix) {
        if (element.Children.Count != 0 || element.Text.IndexOf('\n') >= 0 || Math.Abs(matrix.A - 1) > 0.00001 || Math.Abs(matrix.D - 1) > 0.00001
            || Math.Abs(matrix.B) > 0.00001 || Math.Abs(matrix.C) > 0.00001 || style.Fill.IsReference || style.Stroke.IsReference
            || style.TextDecoration != "none" || style.TextTransform != "none") return false;
        var text = element.Text;
        var face = SvgTextFace(style);
        var point = matrix.Transform(new ChartPoint(LabelCoordinate(element, "x"), LabelCoordinate(element, "y")));
        var x = point.X + TextAnchorOffset(style.TextAnchor, TextAdvanceWidth(text, style.FontSize, face));
        var y = TextTop(point.Y, style.FontSize, style.DominantBaseline, face.Font) + BaselineShiftOffset(style);
        var fill = style.Fill.Color ?? style.Color;
        var stroke = style.Stroke.Color ?? style.Color;
        fill = new ChartColor(fill.R, fill.G, fill.B, (byte)Math.Round(fill.A * style.FillOpacity * style.Opacity));
        stroke = new ChartColor(stroke.R, stroke.G, stroke.B, (byte)Math.Round(stroke.A * style.StrokeOpacity * style.Opacity));
        var glyphs = face.Font == null ? null : Typography.TextShaper.Shape(face.Font, text, style.FontSize);
        void Draw(ChartColor color, double dx = 0, double dy = 0) => DrawTextGlyphs(canvas, x + dx, y + dy, text, color, style.FontSize, face.SynthesizeBold, face.SynthesizeItalic, face.Font, glyphs);
        void Halo() {
            if (style.Stroke.IsNone || stroke.A == 0 || style.StrokeWidth <= 0) return;
            var offset = style.StrokeWidth / 2;
            Draw(stroke, -offset, 0); Draw(stroke, offset, 0); Draw(stroke, 0, -offset); Draw(stroke, 0, offset);
            Draw(stroke, -offset * 0.707, -offset * 0.707); Draw(stroke, offset * 0.707, -offset * 0.707);
            Draw(stroke, -offset * 0.707, offset * 0.707); Draw(stroke, offset * 0.707, offset * 0.707);
        }
        if (style.StrokeBeforeFill) Halo();
        if (!style.Fill.IsNone && fill.A != 0) Draw(fill);
        if (!style.StrokeBeforeFill) Halo();
        return true;
    }
    private static double LabelCoordinate(SvgRasterElement element, string name) => double.TryParse(element.Get(name), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value) ? value : 0;
}
