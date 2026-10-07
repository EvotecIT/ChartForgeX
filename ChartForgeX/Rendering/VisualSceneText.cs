using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Typography;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

/// <summary>Resolved once while preparing a scene, never looked up by its painters.</summary>
internal sealed class VisualSceneTextFace {
    private readonly ResolvedTypeface _face;
    private readonly TextStyle _style;

    internal VisualSceneTextFace(FontSpec font, int weight) : this(new TextStyle {
        Font = WithWeight(font, weight), LineHeight = 1
    }) { }

    internal VisualSceneTextFace(TextStyle style) {
        _style = (style ?? throw new ArgumentNullException(nameof(style))).Clone();
        _face = TypographyFontResolver.WithLanguage(TypographyFontResolver.ResolveFace(_style.Font), _style.OpenTypeLanguageTag);
    }

    internal double Ascent(double size) => _face.Font?.Ascent(size) ?? TinyFont.Height * FallbackScale(size);

    internal VisualScenePreparedText Prepare(string text, double size) {
        if (text == null) throw new ArgumentNullException(nameof(text));
        ChartGuards.Finite(size, nameof(size));
        if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));
        var displayed = TextCaseTransformer.Apply(text, _style.TextCase, CultureInfo.InvariantCulture);
        var style = _style.Clone(); style.FontSize = size; style.Baseline = TextBaseline.Normal;
        var lineHeight = _face.Font == null ? TinyFont.Height * FallbackScale(size) * style.LineHeight
            : TextLayoutEngine.ResolveLineHeight(style, _face.Font);
        var lines = new List<VisualSceneTextLine>(); var width = 0d;
        foreach (var slice in TextLineScanner.Enumerate(displayed)) {
            var value = slice.Read(displayed);
            var glyphs = _face.Font == null ? Array.Empty<ShapedGlyph>() : TextShaper.Shape(_face.Font, value, size);
            var measuredWidth = _face.Font == null ? FallbackWidth(value, size) : TrueTypeFont.MeasureGlyphs(glyphs, size);
            if (style.Font.Italic && value.Length > 0) measuredWidth += TrueTypeFont.ItalicOverhang(size);
            if (_face.SynthesizeBold && value.Length > 0 && (_face.Font == null || _face.Font.NeedsSyntheticBold(glyphs)))
                measuredWidth += RgbaCanvas.EmphasisOffset(size);
            width = Math.Max(width, measuredWidth);
            lines.Add(new VisualSceneTextLine(value, measuredWidth, glyphs));
        }
        return new VisualScenePreparedText(_face, style, size, Ascent(size), Array.AsReadOnly(lines.ToArray()),
            new TextMetrics(width, Math.Max(1, lines.Count) * lineHeight, lineHeight));
    }

    internal static int FallbackScale(double size) => Math.Max(1, (int)Math.Round(Math.Max(1, size) / 8d));
    private static double FallbackWidth(string text, double size) {
        var width = 0;
        foreach (var c in text) width += TinyFont.AdvanceFor(c);
        return width * FallbackScale(size);
    }
    private static FontSpec WithWeight(FontSpec font, int weight) {
        var copy = font.Clone(); copy.Weight = Math.Min(900, weight); return copy;
    }
}

internal sealed class VisualScenePreparedText {
    internal VisualScenePreparedText(ResolvedTypeface face, TextStyle style, double size, double ascent,
        IReadOnlyList<VisualSceneTextLine> lines, TextMetrics metrics) {
        Face = face; Style = style; Size = size; Ascent = ascent; Lines = lines; Metrics = metrics;
    }
    internal ResolvedTypeface Face { get; }
    internal TextStyle Style { get; }
    internal double Size { get; }
    internal double Ascent { get; }
    internal IReadOnlyList<VisualSceneTextLine> Lines { get; }
    internal TextMetrics Metrics { get; }
}

internal sealed class VisualSceneTextLine {
    internal VisualSceneTextLine(string text, double width, IReadOnlyList<ShapedGlyph> glyphs) {
        Text = text; Width = width;
        var copy = new ShapedGlyph[glyphs.Count];
        for (var i = 0; i < copy.Length; i++) copy[i] = glyphs[i];
        Glyphs = Array.AsReadOnly(copy);
    }
    internal string Text { get; }
    internal double Width { get; }
    internal IReadOnlyList<ShapedGlyph> Glyphs { get; }
}

internal sealed class VisualSceneText : VisualSceneNode {
    internal VisualSceneText(VisualScenePreparedText text, double x, double baseline, ChartColor color,
        TextAlignment alignment, string? role, string? id, SvgPaint? paint = null) : base(role, id) {
        Text = text; X = x; Baseline = baseline; Color = color; Alignment = alignment; Paint = paint;
    }
    internal VisualScenePreparedText Text { get; }
    internal double X { get; }
    internal double Baseline { get; }
    internal ChartColor Color { get; }
    internal SvgPaint? Paint { get; }
    internal TextAlignment Alignment { get; }
    internal double LineLeft(VisualSceneTextLine line) => X - (Alignment == TextAlignment.Center ? line.Width / 2 : Alignment == TextAlignment.Right ? line.Width : 0);
}
