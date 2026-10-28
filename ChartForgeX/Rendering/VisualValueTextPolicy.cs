using System;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

/// <summary>Retains the finite native scalar/radial typography cascade and its normal/bold faces without a producer model.</summary>
internal sealed class VisualValueTextPolicy {
    private readonly FontSpec _font;
    private readonly TextStyleOverride _ticks, _chart, _series, _point;
    private readonly TextStyle[] _styles;
    private readonly VisualSceneTextFace[] _faces;
    internal VisualValueTextPolicy(Chart chart, ChartSeries series, VisualRenderContext context) {
        _font = context.Font; _ticks = chart.Options.TickLabelStyle.Clone(); _chart = chart.Options.DataLabelStyle.Clone();
        _series = series.DataLabelStyle.Clone(); _point = series.PointDataLabelStyles.Count > 0
            ? series.PointDataLabelStyles[0]?.Clone() ?? new TextStyleOverride() : new TextStyleOverride();
        _styles = Enumerable.Range(0, 6).Select(index => Style(ChartColor.FromRgb(0, 0, 0), 12, index % 2 == 0 ? 400 : 700,
            index / 2 == 1 ? 0 : -1, index / 2 == 2)).ToArray();
        _faces = _styles.Select(style => new VisualSceneTextFace(style)).ToArray();
    }
    internal bool CanTransition => _styles.All(style => style.Baseline == TextBaseline.Normal);
    internal TextStyle Style(ChartColor color, double size, int weight = 400, int point = -1, bool ticks = false) {
        var fallback = new TextStyle { Font = _font.Clone(), Color = color, FontSize = size }; fallback.Font.Weight = weight;
        if (ticks) return _ticks.Resolve(fallback);
        var style = _series.Resolve(_chart.Resolve(fallback)); return point >= 0 ? _point.Resolve(style) : style;
    }
    internal bool SamePolicy(VisualValueTextPolicy other) {
        // Two defaults distinguish inherited size/color from explicit overrides even while labels are hidden.
        for (var index = 0; index < _styles.Length; index++) for (var variant = 0; variant < 2; variant++) {
            var color = variant == 0 ? ChartColor.FromRgb(0, 0, 0) : ChartColor.FromRgb(255, 255, 255);
            var first = Style(color, variant == 0 ? 12 : 23, index % 2 == 0 ? 400 : 700, index / 2 == 1 ? 0 : -1, index / 2 == 2);
            var second = other.Style(color, variant == 0 ? 12 : 23, index % 2 == 0 ? 400 : 700, index / 2 == 1 ? 0 : -1, index / 2 == 2);
            if (!VisualValuePolicy.SameText(_faces[index], first, other._faces[index], second)) return false;
        }
        return CanTransition && other.CanTransition;
    }
    internal VisualSceneTextFace Face(TextStyle style) {
        if (TryFace(style, out var face)) return face!;
        throw new NotSupportedException("The native value drawing requested a face outside its captured typography cascade.");
    }
    internal bool TryFace(TextStyle style, out VisualSceneTextFace? face) {
        for (var index = 0; index < _styles.Length; index++) {
            var first = _styles[index].Clone(); var second = style.Clone();
            first.FontSize = second.FontSize; first.Color = second.Color; first.Alignment = second.Alignment;
            if (VisualValuePolicy.SameText(_faces[index], first, _faces[index], second)) { face = _faces[index]; return true; }
        }
        face = null; return false;
    }
    internal TextMetrics Measure(VisualSceneBuilder builder, string text, TextStyle style, bool captured) =>
        captured ? Face(style).Prepare(text, style.EffectiveFontSize).Metrics : builder.MeasureText(text, style);
    internal void Text(VisualSceneBuilder builder, string text, ChartRect area, TextStyle style, string role, string id,
        bool captured, SvgPaint? paint = null, TextAlignment alignment = TextAlignment.Center,
        LabelFallbackRule fallback = LabelFallbackRule.EllipsisThenDrop) =>
        VisualRadialPrimitives.Text(builder, text, area, style, role, id, paint, alignment, fallback, captured ? Face(style) : null);
}
