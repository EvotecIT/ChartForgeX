using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Raster;

namespace ChartForgeX.Typography;

/// <summary>
/// Measures and wraps text with the dependency-free raster font engine used by ChartForgeX.
/// </summary>
public static partial class TextLayoutEngine {
    /// <summary>Measures text without wrapping.</summary>
    public static TextMetrics Measure(string text, TextStyle style) {
        if (text == null) throw new ArgumentNullException(nameof(text));
        if (style == null) throw new ArgumentNullException(nameof(style));
        text = TextCaseTransformer.Apply(text, style.TextCase, CultureInfo.InvariantCulture);
        var font = CapturedLayoutFace(style);
        var lineHeight = ResolveLineHeight(style, font.Font);
        var width = 0d;
        var lineCount = 0;
        foreach (var line in TextLineScanner.Enumerate(text)) {
            width = Math.Max(width, MeasureWidth(line.Read(text), style, font));
            lineCount++;
        }
        return new TextMetrics(width, Math.Max(1, lineCount) * lineHeight, lineHeight);
    }

    /// <summary>Wraps and measures text inside a fixed-width region.</summary>
    public static TextLayout Layout(string text, double maximumWidth, TextStyle style, TextWrapMode wrapMode = TextWrapMode.Word, int? maximumLines = null, TextTrimming trimming = TextTrimming.Ellipsis) {
        ValidateLayoutArguments(text, maximumWidth, style, wrapMode, maximumLines, trimming);
        text = TextCaseTransformer.Apply(text, style.TextCase, CultureInfo.InvariantCulture);
        var font = CapturedLayoutFace(style);
        return TextFlowLayout.Layout(TextContent.Plain(text), maximumWidth,
            content => MeasureWidth(content.PlainText, style, font), ResolveLineHeight(style, font.Font), wrapMode, maximumLines, trimming);
    }

    private static void ValidateLayoutArguments(string text, double maximumWidth, TextStyle style, TextWrapMode wrapMode, int? maximumLines, TextTrimming trimming) {
        if (text == null) throw new ArgumentNullException(nameof(text));
        if (style == null) throw new ArgumentNullException(nameof(style));
        if (!IsFinite(maximumWidth) || maximumWidth <= 0) throw new ArgumentOutOfRangeException(nameof(maximumWidth), maximumWidth, "Maximum width must be finite and greater than zero.");
        if (maximumLines <= 0) throw new ArgumentOutOfRangeException(nameof(maximumLines), maximumLines, "Maximum lines must be greater than zero.");
        if (!Enum.IsDefined(typeof(TextWrapMode), wrapMode)) throw new ArgumentOutOfRangeException(nameof(wrapMode), wrapMode, "Unknown text wrap mode.");
        if (!Enum.IsDefined(typeof(TextTrimming), trimming)) throw new ArgumentOutOfRangeException(nameof(trimming), trimming, "Unknown text trimming mode.");

    }

    /// <summary>Measures with a face chosen elsewhere; bold and italic requested by the style are synthesized on it.</summary>
    internal static double MeasureWidth(string text, TextStyle style, TrueTypeFont? font) =>
        MeasureWidth(text, style, new ResolvedTypeface(font, style.Font.Weight >= 600, style.Font.Italic));

    private static ResolvedTypeface CapturedLayoutFace(TextStyle style) => style.Font.NativeTextCapture is {} capture
        ? capture.Bind(style).CapturedFace : TypographyFontResolver.WithLanguage(TypographyFontResolver.ResolveFace(style.Font), style.OpenTypeLanguageTag);

    internal static double MeasureWidth(string text, TextStyle style, ResolvedTypeface face) {
        if (style.Font.NativeTextCapture is {} capture) return capture.Bind(style).PrepareDisplayed(text, style.EffectiveFontSize).Metrics.Width;
        face = TypographyFontResolver.WithLanguage(face, style.OpenTypeLanguageTag);
        if (style.LetterSpacing != 0) return new ChartForgeX.Rendering.VisualSceneTextFace(style, face, false).PrepareDisplayed(text, style.EffectiveFontSize).Metrics.Width;
        var width = RgbaCanvas.MeasureTextWidth(text, style.EffectiveFontSize, face.Font, face.SynthesizeItalic);
        // A real italic face leans past its last advance just as a sheared one does; reserving the same
        // overhang keeps layout independent of which faces the host has installed.
        if (style.Font.Italic && !face.SynthesizeItalic && text.Length > 0) width += TrueTypeFont.ItalicOverhang(style.EffectiveFontSize);
        if (face.SynthesizeBold && text.Length > 0 && (face.Font == null || face.Font.NeedsSyntheticBold(text))) width += Math.Max(0.6, style.EffectiveFontSize / 18.0);
        return width;
    }

    internal static double ResolveLineHeight(TextStyle style, TrueTypeFont? font) {
        var height = RgbaCanvas.MeasureTextHeight(style.EffectiveFontSize, font);
        if (style.UnderlineStyle != TextDecorationStyle.None) {
            var thickness = Math.Max(1, style.EffectiveFontSize / 13.0);
            height = Math.Max(height, style.EffectiveFontSize + 2 + TextDecorationMetrics.OuterExtent(style.UnderlineStyle, thickness));
        }
        return Math.Max(1, height * style.LineHeight);
    }

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}
