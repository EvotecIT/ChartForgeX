using System;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Raster;

/// <summary>Draws the shared measured typography layout on immediate raster producers.</summary>
internal static class RasterStyledText {
    internal static void DrawStyledText(this RgbaCanvas canvas, double x, double y, double width, string text,
        TextStyle style, TextWrapMode wrapMode = TextWrapMode.Word, int? maximumLines = null,
        TextTrimming trimming = TextTrimming.Ellipsis) {
        if (text == null) throw new ArgumentNullException(nameof(text));
        if (style == null) throw new ArgumentNullException(nameof(style));
        if (text.Length == 0 || style.Color.A == 0) return;
        var layout = TextLayoutEngine.Layout(text, width, style, wrapMode, maximumLines, trimming);
        var face = TypographyFontResolver.WithLanguage(TypographyFontResolver.ResolveFace(style.Font), style.OpenTypeLanguageTag);
        var fontSize = style.EffectiveFontSize;
        var hinting = canvas.TextHinting;
        canvas.TextHinting = style.Hinting;
        try {
            for (var index = 0; index < layout.Lines.Count; index++) {
                var line = layout.Lines[index];
                var drawX = style.Alignment == TextAlignment.Center ? x + (width - line.Width) / 2
                    : style.Alignment == TextAlignment.Right ? x + width - line.Width : x;
                var baseline = style.Baseline == TextBaseline.Superscript ? -fontSize * 0.35
                    : style.Baseline == TextBaseline.Subscript ? fontSize * 0.22 : 0;
                var drawY = y + index * layout.Metrics.LineHeight + baseline;
                if (face.SynthesizeBold) canvas.DrawTextEmphasized(drawX, drawY, line.Text, style.Color, fontSize, face.Font, face.SynthesizeItalic);
                else canvas.DrawText(drawX, drawY, line.Text, style.Color, fontSize, face.Font, face.SynthesizeItalic);
                var thickness = Math.Max(1, fontSize / 16);
                RasterTextDecoration.Draw(canvas, drawX, drawX + line.Width, drawY + fontSize * 1.05, style.UnderlineStyle, style.Color, thickness);
                RasterTextDecoration.Draw(canvas, drawX, drawX + line.Width, drawY + fontSize * 0.55, style.StrikethroughStyle, style.Color, thickness);
            }
        } finally {
            canvas.TextHinting = hinting;
        }
    }
}
