using System;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Raster;

internal sealed partial class RgbaCanvas {
    /// <summary>How text drawn on this canvas is fitted to the output pixel grid; buffers it draws text through inherit it.</summary>
    internal TextHinting TextHinting { get; set; }

    /// <summary>Output pixels per canvas unit, the grid text is fitted to.</summary>
    internal int OutputScale => _outputScale;

    // A text buffer has one pixel per canvas unit; its grid is this canvas's only at an output scale of one.
    private TextHinting BufferHinting => _outputScale == 1 ? TextHinting : TextHinting.None;

    public void DrawTextTiny(double x, double y, string text, ChartColor color, int scale = 2) {
        DrawTextTiny(x, y, text, color, scale, italic: false);
    }

    internal void DrawTextTiny(double x, double y, string text, ChartColor color, int scale, bool italic, double boldOffset = 0) {
        var font = _outlineFont;
        if (font != null && font.Draw(this, x, y, text, color, OutlineFontSize(scale), italic, boldOffset)) return;

        var cursor = (int)Math.Round(x * _scale);
        var glyphScale = Math.Max(1, scale * _scale);
        foreach (var ch in text) {
            DrawGlyph(cursor, (int)Math.Round(y * _scale), ch, color, glyphScale, italic, boldOffset);
            cursor += TinyFont.AdvanceFor(ch) * glyphScale;
        }
    }

    public void DrawText(double x, double y, string text, ChartColor color, double fontSize) => DrawText(x, y, text, color, fontSize, _outlineFont);

    internal void DrawText(double x, double y, string text, ChartColor color, double fontSize, bool italic) => DrawText(x, y, text, color, fontSize, _outlineFont, italic);

    internal void DrawText(double x, double y, string text, ChartColor color, double fontSize, TrueTypeFont? font) {
        DrawText(x, y, text, color, fontSize, font, italic: false);
    }

    internal void DrawText(double x, double y, string text, ChartColor color, double fontSize, TrueTypeFont? font, bool italic) {
        if (font != null && font.Draw(this, x, y, text, color, Math.Max(1, fontSize), italic)) return;
        DrawTextTiny(x, y, text, color, FallbackScaleForFontSize(fontSize), italic);
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
        var naturalWidth = emphasized ? MeasureTextEmphasizedWidthWithFont(text, fontSize, font, italic: false) : MeasureTextWidthWithFont(text, fontSize, font);
        if (naturalWidth <= maximumWidth) {
            if (emphasized) DrawTextEmphasized(x, y, text, color, fontSize, font);
            else DrawText(x, y, text, color, fontSize, font);
            return;
        }

        var naturalHeight = Math.Max(1, (int)Math.Ceiling(font == null
            ? TinyFont.Height * FallbackScaleForFontSize(fontSize)
            : font.LineHeight(Math.Max(1, fontSize))));
        var bufferWidth = Math.Max(1, (int)Math.Ceiling(naturalWidth));
        var buffer = new RgbaCanvas(bufferWidth, naturalHeight, _supersamplingScale, font, 1, useDefaultOutlineFont: false) { TextHinting = BufferHinting };
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

    internal void DrawTextEmphasized(double x, double y, string text, ChartColor color, double fontSize, bool italic) => DrawTextEmphasized(x, y, text, color, fontSize, _outlineFont, italic);

    internal void DrawTextEmphasized(double x, double y, string text, ChartColor color, double fontSize, TrueTypeFont? font) {
        DrawTextEmphasized(x, y, text, color, fontSize, font, italic: false);
    }

    internal void DrawTextEmphasized(double x, double y, string text, ChartColor color, double fontSize, TrueTypeFont? font, bool italic) {
        if (string.IsNullOrEmpty(text) || color.A == 0) return;
        var bold = EmphasisFace(font);
        if (bold != null) {
            DrawText(x, y, text, color, fontSize, bold, italic);
            return;
        }

        if (font != null && font.Draw(this, x, y, text, color, Math.Max(1, fontSize), italic, EmphasisOffset(fontSize))) return;
        DrawTextTiny(x, y, text, color, FallbackScaleForFontSize(fontSize), italic, EmphasisOffset(fontSize));
    }

    public static double MeasureTextTinyWidth(string text, int scale) => MeasureTextTinyWidth(text, scale, null);

    public static double MeasureTextTinyWidth(string text, int scale, TrueTypeFont? outlineFont) {
        var font = outlineFont ?? DefaultOutlineFont;
        return font != null ? font.Measure(text, OutlineFontSize(scale)) : MeasureTinyFallbackWidth(text, scale);
    }

    public static double MeasureTextWidth(string text, double fontSize, TrueTypeFont? outlineFont) => MeasureTextWidth(text, fontSize, outlineFont, italic: false);

    internal static double MeasureTextWidth(string text, double fontSize, TrueTypeFont? outlineFont, bool italic) {
        var font = outlineFont ?? DefaultOutlineFont;
        if (font != null) return font.Measure(text, Math.Max(1, fontSize), italic);
        var width = MeasureTextTinyWidth(text, FallbackScaleForFontSize(fontSize), null);
        return width + (italic && text.Length > 0 ? TrueTypeFont.ItalicOverhang(fontSize) : 0);
    }

    internal double MeasureTextWidth(string text, double fontSize) => MeasureTextWidth(text, fontSize, italic: false);

    internal double MeasureTextWidth(string text, double fontSize, bool italic) {
        if (_outlineFont != null) return _outlineFont.Measure(text, Math.Max(1, fontSize), italic);
        var width = MeasureTinyFallbackWidth(text, FallbackScaleForFontSize(fontSize));
        return width + (italic && text.Length > 0 ? TrueTypeFont.ItalicOverhang(fontSize) : 0);
    }

    internal static double MeasureTextWidthWithFont(string text, double fontSize, TrueTypeFont? font) => MeasureTextWidthWithFont(text, fontSize, font, italic: false);

    internal static double MeasureTextWidthWithFont(string text, double fontSize, TrueTypeFont? font, bool italic) =>
        font != null
            ? font.Measure(text, Math.Max(1, fontSize), italic)
            : MeasureTinyFallbackWidth(text, FallbackScaleForFontSize(fontSize)) + (italic && text.Length > 0 ? TrueTypeFont.ItalicOverhang(fontSize) : 0);

    public static double MeasureTextEmphasizedWidth(string text, double fontSize, TrueTypeFont? outlineFont) => MeasureTextEmphasizedWidth(text, fontSize, outlineFont, italic: false);

    internal static double MeasureTextEmphasizedWidth(string text, double fontSize, TrueTypeFont? outlineFont, bool italic) {
        if (string.IsNullOrEmpty(text)) return 0;
        var bold = EmphasisFace(outlineFont ?? DefaultOutlineFont);
        var primary = outlineFont ?? DefaultOutlineFont;
        return bold != null ? bold.Measure(text, Math.Max(1, fontSize), italic) : MeasureTextWidth(text, fontSize, outlineFont, italic) + (primary == null || primary.NeedsSyntheticBold(text) ? EmphasisOffset(fontSize) : 0);
    }

    private static double MeasureTextEmphasizedWidthWithFont(string text, double fontSize, TrueTypeFont? font, bool italic) {
        if (string.IsNullOrEmpty(text)) return 0;
        var bold = EmphasisFace(font);
        return bold != null ? bold.Measure(text, Math.Max(1, fontSize), italic) : MeasureTextWidthWithFont(text, fontSize, font, italic) + (font == null || font.NeedsSyntheticBold(text) ? EmphasisOffset(fontSize) : 0);
    }

    internal double MeasureTextEmphasizedWidth(string text, double fontSize) => MeasureTextEmphasizedWidth(text, fontSize, _outlineFont);

    internal double MeasureTextEmphasizedWidth(string text, double fontSize, bool italic) => MeasureTextEmphasizedWidth(text, fontSize, _outlineFont, italic);

    public static double MeasureTextHeight(double fontSize, TrueTypeFont? outlineFont) {
        var font = outlineFont ?? DefaultOutlineFont;
        return font != null ? font.LineHeight(Math.Max(1, fontSize)) : TinyFont.Height * FallbackScaleForFontSize(fontSize);
    }

    internal double MeasureTextHeight(double fontSize) => MeasureTextHeight(fontSize, _outlineFont);

    public static double MeasureTextTinyHeight(int scale) => MeasureTextTinyHeight(scale, null);

    public static double MeasureTextTinyHeight(int scale, TrueTypeFont? outlineFont) {
        var font = outlineFont ?? DefaultOutlineFont;
        return font != null ? OutlineFontSize(scale) : TinyFont.Height * Math.Max(1, scale);
    }

    private static double OutlineFontSize(int scale) => TinyFont.Height * Math.Max(1, scale) * 1.45;
    private static int FallbackScaleForFontSize(double fontSize) => Math.Max(1, (int)Math.Round(Math.Max(1, fontSize) / OutlineFontSize(1)));
    private static double EmphasisOffset(double fontSize) => Math.Max(0.24, Math.Min(0.58, fontSize * 0.025));

    private static double MeasureTinyFallbackWidth(string text, int scale) {
        var width = 0;
        foreach (var ch in text) width += TinyFont.AdvanceFor(ch);
        return width * Math.Max(1, scale);
    }
}
