using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Raster;

namespace ChartForgeX.Typography;

public static partial class TextLayoutEngine {
    /// <summary>Uses the incumbent plain wrapping/trimming and exact width arithmetic with an already captured face and glyph owner.</summary>
    internal static TextLayout LayoutCaptured(string text, double maximumWidth, TextStyle style, ResolvedTypeface face,
        Func<string, IReadOnlyList<ShapedGlyph>> prepareDisplayed, TextWrapMode wrapMode = TextWrapMode.Word,
        int? maximumLines = null, TextTrimming trimming = TextTrimming.Ellipsis) {
        ValidateLayoutArguments(text, maximumWidth, style, wrapMode, maximumLines, trimming);
        if (prepareDisplayed == null) throw new ArgumentNullException(nameof(prepareDisplayed));
        var font = face.Font ?? throw new NotSupportedException("Captured plain layout requires an owned outline face.");
        var displayed = TextCaseTransformer.Apply(text, style.TextCase, CultureInfo.InvariantCulture);
        return TextFlowLayout.Layout(TextContent.Plain(displayed), maximumWidth, content => {
            var value = content.PlainText; var glyphs = prepareDisplayed(value); var size = style.EffectiveFontSize;
            var width = font.MeasureCaptured(value, Math.Max(double.Epsilon, size), face.SynthesizeItalic, glyphs);
            if (style.Font.Italic && !face.SynthesizeItalic && value.Length > 0) width += TrueTypeFont.ItalicOverhang(size);
            if (face.SynthesizeBold && value.Length > 0 && font.NeedsSyntheticBoldCaptured(value, glyphs)) width += Math.Max(.6, size / 18d);
            return width;
        }, ResolveLineHeight(style, font), wrapMode, maximumLines, trimming);
    }
}
