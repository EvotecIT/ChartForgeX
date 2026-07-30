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
        DrawTextFitted(x, y, text, color, fontSize, maximumWidth, false, _outlineFont);
    }

    internal void DrawTextFitted(double x, double y, string text, ChartColor color, double fontSize, double maximumWidth, TrueTypeFont? font) {
        DrawTextFitted(x, y, text, color, fontSize, maximumWidth, false, font);
    }

    internal void DrawTextFittedEmphasized(double x, double y, string text, ChartColor color, double fontSize, double maximumWidth) {
        DrawTextFitted(x, y, text, color, fontSize, maximumWidth, true, _outlineFont);
    }

    internal void DrawTextFittedEmphasized(double x, double y, string text, ChartColor color, double fontSize, double maximumWidth, TrueTypeFont? font) {
        DrawTextFitted(x, y, text, color, fontSize, maximumWidth, true, font);
    }

    private void DrawTextFitted(double x, double y, string text, ChartColor color, double fontSize, double maximumWidth, bool emphasized, TrueTypeFont? font) {
        if (string.IsNullOrEmpty(text) || color.A == 0 || maximumWidth <= 0) return;
        var naturalWidth = MeasureTextWidthWithFont(text, fontSize, font);
        if (emphasized && text.Length > 0) naturalWidth += EmphasisOffset(fontSize);
        if (naturalWidth <= maximumWidth) {
            if (emphasized) DrawTextEmphasized(x, y, text, color, fontSize, font);
            else DrawText(x, y, text, color, fontSize, font);
            return;
        }

        var naturalHeight = Math.Max(1, (int)Math.Ceiling(font == null
            ? TinyFont.Height * FallbackScaleForFontSize(fontSize)
            : font.LineHeight(Math.Max(1, fontSize))));
        var bufferWidth = Math.Max(1, (int)Math.Ceiling(naturalWidth));
        var buffer = new RgbaCanvas(bufferWidth, naturalHeight, _supersamplingScale, font, 1, useDefaultOutlineFont: false);
        if (emphasized) buffer.DrawTextEmphasized(0, 0, text, color, fontSize, font);
        else buffer.DrawText(0, 0, text, color, fontSize, font);
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
