using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

internal sealed partial class RgbaCanvas {
    public void DrawTextTiny(double x, double y, string text, ChartColor color, int scale = 2) {
        var font = _outlineFont;
        if (font != null && font.Draw(this, x, y, text, color, OutlineFontSize(scale))) return;

        var cursor = (int)Math.Round(x * _scale);
        var glyphScale = Math.Max(1, scale * _scale);
        foreach (var ch in text) {
            DrawGlyph(cursor, (int)Math.Round(y * _scale), ch, color, glyphScale);
            cursor += TinyFont.AdvanceFor(ch) * glyphScale;
        }
    }

    public void DrawText(double x, double y, string text, ChartColor color, double fontSize) => DrawText(x, y, text, color, fontSize, _outlineFont);

    internal void DrawText(double x, double y, string text, ChartColor color, double fontSize, TrueTypeFont? font) {
        if (font != null && font.Draw(this, x, y, text, color, Math.Max(1, fontSize))) return;
        DrawTextTiny(x, y, text, color, FallbackScaleForFontSize(fontSize));
    }

    internal void DrawTextFitted(double x, double y, string text, ChartColor color, double fontSize, double maximumWidth) {
        if (string.IsNullOrEmpty(text) || color.A == 0 || maximumWidth <= 0) return;
        var naturalWidth = MeasureTextWidth(text, fontSize);
        if (naturalWidth <= maximumWidth) {
            DrawText(x, y, text, color, fontSize);
            return;
        }

        var naturalHeight = Math.Max(1, (int)Math.Ceiling(MeasureTextHeight(fontSize)));
        var bufferWidth = Math.Max(1, (int)Math.Ceiling(naturalWidth));
        var buffer = new RgbaCanvas(bufferWidth, naturalHeight, _supersamplingScale, _outlineFont, 1, useDefaultOutlineFont: false);
        buffer.DrawText(0, 0, text, color, fontSize);
        var pixels = buffer.ToOutputPixels();
        DrawImageScaled(
            (int)Math.Round(x),
            (int)Math.Round(y),
            Math.Max(1, (int)Math.Floor(maximumWidth)),
            naturalHeight,
            buffer.OutputWidth,
            buffer.OutputHeight,
            pixels);
    }

    public void DrawTextEmphasized(double x, double y, string text, ChartColor color, double fontSize) => DrawTextEmphasized(x, y, text, color, fontSize, _outlineFont);

    internal void DrawTextEmphasized(double x, double y, string text, ChartColor color, double fontSize, TrueTypeFont? font) {
        if (string.IsNullOrEmpty(text) || color.A == 0) return;
        DrawText(x, y, text, color, fontSize, font);
        DrawText(x + EmphasisOffset(fontSize), y, text, color, fontSize, font);
    }
}
