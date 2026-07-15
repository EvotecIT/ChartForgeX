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

    public void DrawTextEmphasized(double x, double y, string text, ChartColor color, double fontSize) => DrawTextEmphasized(x, y, text, color, fontSize, _outlineFont);

    internal void DrawTextEmphasized(double x, double y, string text, ChartColor color, double fontSize, TrueTypeFont? font) {
        if (string.IsNullOrEmpty(text) || color.A == 0) return;
        DrawText(x, y, text, color, fontSize, font);
        DrawText(x + EmphasisOffset(fontSize), y, text, color, fontSize, font);
    }
}
