using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualHierarchyCompiler {
    /// <summary>Fits a secondary run beneath an unchanged primary caption in their shared native sector.</summary>
    private static bool SunburstSecondaryLabel(Chart chart, VisualSceneBuilder builder, ChartSunburstNode node,
        ChartColor fill, SvgPaint fillPaint, TextStyle primaryStyle, string primary, TextMetrics primaryMetrics,
        string secondary, double x, double y, double[] orientations, Func<TextMetrics, double, bool> fits) {
        var options = chart.Options.Sunburst;
        var inherited = primaryStyle.Clone(); inherited.FontSize = primaryStyle.EffectiveFontSize * .8;
        var style = options.SecondaryLabelStyle.Resolve(inherited);
        style.FontSize = style.EffectiveFontSize; style.Baseline = TextBaseline.Normal;
        var metrics = builder.MeasureText(secondary, style);
        if (TryRun(secondary, metrics)) return true;
        var wrapped = ChartLabelWrapping.BalancedTwoLine(secondary, style.EffectiveFontSize, metrics.Width * .6,
            (value, _) => builder.MeasureText(value, style).Width);
        var twoLines = string.Join("\n", wrapped);
        if (wrapped.Length > 1 && TryRun(twoLines, builder.MeasureText(twoLines, style))) return true;

        string shortened = string.Empty; double rotation = 0;
        foreach (var degrees in orientations) {
            var candidate = ChartTextFitting.FitEnd(secondary, value => fits(Block(builder.MeasureText(value, style)), degrees), "…");
            if (candidate.Length <= shortened.Length || candidate == "…") continue;
            shortened = candidate; rotation = degrees;
        }
        if (shortened.Length == 0) return false;
        Draw(shortened, builder.MeasureText(shortened, style), rotation);
        builder.AddDiagnostic(new VisualDiagnostic("hierarchy.label-overflow", "A sunburst secondary label was shortened to fit its segment; complete text remains in source semantics."));
        return true;

        TextMetrics Block(TextMetrics secondaryMetrics) => new(Math.Max(primaryMetrics.Width, secondaryMetrics.Width),
            primaryMetrics.Height + options.SecondaryLabelSpacing + secondaryMetrics.Height, primaryMetrics.LineHeight);

        bool TryRun(string text, TextMetrics measured) {
            foreach (var degrees in orientations) {
                if (!fits(Block(measured), degrees)) continue;
                Draw(text, measured, degrees); return true;
            }
            return false;
        }

        void Draw(string text, TextMetrics measured, double degrees) {
            var top = y - Block(measured).Height / 2;
            primaryStyle.Alignment = TextAlignment.Center; style.Alignment = TextAlignment.Center;
            var primaryPaint = VisualChartPaint.ExplicitDataLabelColor(chart, node.Index) ? VisualChartPaint.Text(primaryStyle) : SvgPaint.Contrast(fill, fillPaint);
            var secondaryPaint = options.SecondaryLabelStyle.Color.HasValue || VisualChartPaint.ExplicitDataLabelColor(chart, node.Index)
                ? VisualChartPaint.Text(style) : SvgPaint.Contrast(fill, fillPaint);
            using (Math.Abs(degrees) < .001 ? null : builder.PushRotation(degrees, x, y)) {
                builder.Text(primary, x, top + builder.TextAscent(primaryStyle), primaryStyle, "sunburst-label", Id("node-label", node.Index), primaryPaint);
                builder.Text(text, x, top + primaryMetrics.Height + options.SecondaryLabelSpacing + builder.TextAscent(style), style,
                    "sunburst-secondary-label", Id("node-secondary-label", node.Index), secondaryPaint);
            }
        }
    }
}
