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
    private readonly bool _strictPrimary;
    private readonly TextShaper.CapturedClusters? _capturedClusters;
    private readonly Action<VisualScenePreparedText>? _record;

    internal VisualSceneTextFace(FontSpec font, int weight) : this(new TextStyle {
        Font = WithWeight(font, weight), LineHeight = 1
    }) { }

    internal VisualSceneTextFace(TextStyle style) : this(style, useAttachedCapture: true) { }
    internal VisualSceneTextFace(TextStyle style, bool useAttachedCapture) {
        if (useAttachedCapture && style?.Font.NativeTextCapture is {} capture) {
            var bound = capture.Bind(style);
            _style = bound._style; _face = bound._face; _strictPrimary = bound._strictPrimary;
            _capturedClusters = bound._capturedClusters; _record = bound._record; return;
        }
        _style = (style ?? throw new ArgumentNullException(nameof(style))).Clone();
        _face = TypographyFontResolver.WithLanguage(TypographyFontResolver.ResolveFace(_style.Font), _style.OpenTypeLanguageTag);
    }

    internal VisualSceneTextFace(TextStyle style, ResolvedTypeface face, bool strictPrimary, Action<VisualScenePreparedText>? record = null)
        : this(style, face, strictPrimary, record, null) { }
    internal VisualSceneTextFace(TextStyle style, ResolvedTypeface face, bool strictPrimary, Action<VisualScenePreparedText>? record, TextShaper.CapturedClusters? capturedClusters) {
        _style = (style ?? throw new ArgumentNullException(nameof(style))).Clone(); _face = face; _strictPrimary = strictPrimary; _record = record; _capturedClusters = capturedClusters;
    }

    internal ResolvedTypeface CapturedFace => _face;
    internal TextShaper.CapturedClusters? CapturedClusters => _capturedClusters;
    internal void RecordPrepared(VisualScenePreparedText prepared) => _record?.Invoke(prepared);
    internal TextStyle CapturedStyle {
        get { var copy = _style.Clone(); copy.Font = copy.Font.WithoutCapturedText(); return copy; }
    }

    internal static double BaselineOffset(TextStyle style) => style.Baseline == TextBaseline.Superscript ? -style.EffectiveFontSize * 0.35
        : style.Baseline == TextBaseline.Subscript ? style.EffectiveFontSize * 0.22 : 0;

    internal double Ascent(double size) => _face.Font?.Ascent(size) ?? TinyFont.Height * FallbackScale(size);

    /// <summary>Reserves the face's alternate platform ascenders without changing its prepared baseline.</summary>
    internal double AscentEnvelope(double size) => _face.Font?.AscentEnvelope(size) ?? Math.Max(size, Ascent(size));

    internal VisualScenePreparedText Prepare(string text, double size) => PrepareCore(text, size, alreadyDisplayed: false);
    /// <summary>The plain layout owner already applied its display transform once before slicing candidate lines.</summary>
    internal VisualScenePreparedText PrepareDisplayed(string text, double size) => PrepareCore(text, size, alreadyDisplayed: true);
    private VisualScenePreparedText PrepareCore(string text, double size, bool alreadyDisplayed) {
        if (text == null) throw new ArgumentNullException(nameof(text));
        ChartGuards.Finite(size, nameof(size));
        if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));
        var displayed = alreadyDisplayed ? text : TextCaseTransformer.Apply(text, _style.TextCase, CultureInfo.InvariantCulture);
        var style = _style.Clone(); style.Font = style.Font.WithoutCapturedText(); style.FontSize = size; style.Baseline = TextBaseline.Normal;
        var lineHeight = _face.Font == null ? TinyFont.Height * FallbackScale(size) * style.LineHeight
            : TextLayoutEngine.ResolveLineHeight(style, _face.Font);
        var lines = new List<VisualSceneTextLine>(); var width = 0d;
        foreach (var slice in TextLineScanner.Enumerate(displayed)) {
            var value = slice.Read(displayed);
            var glyphs = _face.Font == null ? Array.Empty<ShapedGlyph>() : TextShaper.Shape(_face.Font, value, size, _strictPrimary, _capturedClusters);
            var measuredWidth = _face.Font == null ? FallbackWidth(value, size) : TrueTypeFont.MeasureGlyphs(glyphs, size);
            if (style.Font.Italic && value.Length > 0) measuredWidth += TrueTypeFont.ItalicOverhang(size);
            if (_face.SynthesizeBold && value.Length > 0 && (_face.Font == null || _face.Font.NeedsSyntheticBold(glyphs)))
                measuredWidth += RgbaCanvas.EmphasisOffset(size);
            width = Math.Max(width, measuredWidth);
            lines.Add(new VisualSceneTextLine(value, measuredWidth, glyphs));
        }
        var prepared = new VisualScenePreparedText(_face, style, size, Ascent(size), Array.AsReadOnly(lines.ToArray()),
            new TextMetrics(width, Math.Max(1, lines.Count) * lineHeight, lineHeight));
        _record?.Invoke(prepared); return prepared;
    }

    internal static double FallbackScale(double size) => TinyFont.ScaleForFontSize(size);
    private static double FallbackWidth(string text, double size) {
        var width = 0;
        foreach (var c in text) width += TinyFont.AdvanceFor(c);
        return width * FallbackScale(size);
    }
    private static FontSpec WithWeight(FontSpec font, int weight) {
        var copy = font.Clone(); copy.Weight = weight; return copy;
    }
}

internal sealed class VisualScenePreparedText {
    internal VisualScenePreparedText(ResolvedTypeface face, TextStyle style, double size, double ascent,
        IReadOnlyList<VisualSceneTextLine> lines, TextMetrics metrics, bool nativeGlyphInk = false) {
        Face = face; Style = style; Size = size; Ascent = ascent; Lines = lines; Metrics = metrics; NativeGlyphInk = nativeGlyphInk;
    }
    internal bool NativeGlyphInk { get; }
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
        TextAlignment alignment, string? role, string? id, SvgPaint? paint = null,
        ChartColor? stroke = null, double strokeWidth = 0, SvgPaint? strokePaint = null) : base(role, id) {
        Text = text; X = x; Baseline = baseline; Color = color; Alignment = alignment; Paint = paint;
        Stroke = stroke; StrokeWidth = strokeWidth; StrokePaint = strokePaint;
    }
    internal VisualScenePreparedText Text { get; }
    internal double X { get; }
    internal double Baseline { get; }
    internal ChartColor Color { get; }
    internal SvgPaint? Paint { get; }
    internal ChartColor? Stroke { get; }
    internal double StrokeWidth { get; }
    internal SvgPaint? StrokePaint { get; }
    internal TextAlignment Alignment { get; }
    internal double LineLeft(VisualSceneTextLine line) => X - (Alignment == TextAlignment.Center ? line.Width / 2 : Alignment == TextAlignment.Right ? line.Width : 0);
}
