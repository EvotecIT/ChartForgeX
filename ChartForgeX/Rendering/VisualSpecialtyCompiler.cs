using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

/// <summary>Compiles funnel stages, repeated pictograms and measured word-cloud terms.</summary>
internal static partial class VisualSpecialtyCompiler {
    internal static IReadOnlyList<VisualLegendEntry> LegendEntries(Chart chart, VisualThemeColors colors) {
        if (chart.Series.Count == 0 || !chart.Series[0].ShowInLegend) return Array.Empty<VisualLegendEntry>();
        var series = chart.Series[0];
        if (chart.Options.ShowPointLegend) return Enumerable.Range(0, series.Points.Count).Select(index =>
            new VisualLegendEntry(Category(chart, index), Color(series, index, colors), Id(index), series.Kind,
                FillPattern(series, index), series.StateRole, series.InteractionIdentityKey)).ToArray();
        return new[] { new VisualLegendEntry(series.Name, ChartSeriesColours.Resolve(series, 0, colors), "series-0", series.Kind,
            series.FillPattern, series.StateRole, series.InteractionIdentityKey) };
    }

    internal static void Build(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot) {
        if (chart.Series.Count != 1) throw new InvalidOperationException("Funnel, pictorial and word-cloud charts require a single series.");
        var series = chart.Series[0];
        if (series.Kind != ChartSeriesKind.Funnel && series.Kind != ChartSeriesKind.Pictorial && series.Kind != ChartSeriesKind.WordCloud)
            throw new InvalidOperationException("This compiler supports only funnel, pictorial and word-cloud charts.");
        if (series.Points.Any(point => double.IsNaN(point.X) || double.IsInfinity(point.X) || double.IsNaN(point.Y) || double.IsInfinity(point.Y) || point.Y < 0))
            throw new InvalidOperationException("Funnel, pictorial and word-cloud values must be finite and non-negative.");
        if (series.Points.Count == 0) { builder.AddDiagnostic(new VisualDiagnostic("specialty.no-data", "The chart has no source values.")); return; }
        if (plot.Width <= 0 || plot.Height <= 0) return;
        using (builder.PushClip(plot)) {
            if (series.Kind == ChartSeriesKind.Funnel) Funnel(chart, context, builder, plot);
            else if (series.Kind == ChartSeriesKind.Pictorial) Pictorial(chart, context, builder, plot);
            else WordCloud(chart, context, builder, plot);
        }
    }

    private static string Category(Chart chart, int index) {
        var x = chart.Series[0].Points[index].X;
        foreach (var label in chart.Options.XAxisLabels) if (ChartMath.SameCoordinate(label.Value, x)) return label.Text;
        return (index + 1).ToString(CultureInfo.InvariantCulture);
    }
    private static string Id(int index) => "series-0-point-" + index.ToString(CultureInfo.InvariantCulture);
    private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    private static ChartColor Color(ChartSeries series, int index, VisualThemeColors colors) =>
        index < series.PointColors.Count && series.PointColors[index].HasValue ? series.PointColors[index]!.Value
            : ChartSeriesColours.Resolve(series, index, colors);
    private static ChartFillPattern FillPattern(ChartSeries series, int index) =>
        index < series.PointFillPatterns.Count && series.PointFillPatterns[index].HasValue
            ? series.PointFillPatterns[index]!.Value : series.FillPattern;
    private static TextStyle Style(Chart chart, VisualRenderContext context, int index, ChartColor color, double? size = null) {
        var style = VisualStateSceneTools.DataStyle(chart, context, chart.Series[0], index, color);
        if (size.HasValue && !chart.Options.DataLabelStyle.FontSize.HasValue && !chart.Series[0].DataLabelStyle.FontSize.HasValue
            && !(index < chart.Series[0].PointDataLabelStyles.Count && chart.Series[0].PointDataLabelStyles[index]?.FontSize != null)) style.FontSize = size.Value;
        return style;
    }
    private static IDisposable Point(Chart chart, VisualSceneBuilder builder, int index, string role, ChartRect bounds, string? label = null,
        Dictionary<string, string>? extra = null, string idSuffix = "") {
        var series = chart.Series[0]; var point = series.Points[index];
        var metadata = extra ?? new Dictionary<string, string>();
        metadata["data-cfx-series"] = "0"; metadata["data-cfx-point"] = N(index);
        metadata["data-cfx-series-name"] = series.Name;
        metadata["data-cfx-series-key"] = series.InteractionIdentityKey;
        metadata["data-cfx-x"] = N(point.X); metadata["data-cfx-value"] = N(point.Y);
        metadata["data-cfx-label"] = Category(chart, index);
        metadata["data-cfx-state"] = series.StateRole.ToString();
        return VisualStateSceneTools.Mark(builder, Id(index) + idSuffix, role, bounds,
            label ?? Category(chart, index) + ": " + VisualStateSceneTools.Value(chart, series, index, point.Y), metadata);
    }
    private static void Pattern(Chart chart, VisualSceneBuilder builder, int index, ChartPath path, ChartColor color) {
        var series = chart.Series[0];
        builder.Pattern(path, FillPattern(series, index), ChartColorMath.AccessibleTextOnBackground(color).WithOpacity(.45), role: "fill-pattern");
    }

    private static void WordCloud(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot) {
        var colors = context.Theme.Resolve(context.ThemeMode); var series = chart.Series[0];
        var terms = ChartWordCloudLayout.Compute(chart, plot, (index, text, size) => builder.MeasureText(text,
            Style(chart, context, index, Color(series, index, colors), size)));
        var eligible = series.Points.Count(point => point.Y > 0);
        if (chart.Options.WordCloudMaximumTerms.HasValue) eligible = Math.Min(eligible, chart.Options.WordCloudMaximumTerms.Value);
        if (terms.Count < eligible) builder.AddDiagnostic(new VisualDiagnostic("word-cloud.overflow", "Some words do not fit the fixed viewport; complete source terms remain in descriptive regions."));
        var placed = new HashSet<int>(terms.Select(term => term.PointIndex));
        using var group = builder.PushGroup("word-cloud", "word-cloud", new Dictionary<string, string> {
            ["data-cfx-density"] = N(chart.Options.WordCloudDensity), ["data-cfx-edge-padding"] = N(ChartWordCloudLayout.EdgePadding(plot)),
            ["data-cfx-maximum-terms"] = chart.Options.WordCloudMaximumTerms?.ToString(CultureInfo.InvariantCulture) ?? "all"
        });
        // Limits, zero weights and lack of space affect paint, never the detached source inventory.
        for (var index = 0; index < series.Points.Count; index++) {
            using (Point(chart, builder, index, "word-cloud-source", new ChartRect(plot.Left, plot.Top, 0, 0),
                extra: new Dictionary<string, string> { ["data-cfx-text"] = Category(chart, index),
                    ["data-cfx-placed"] = placed.Contains(index) ? "true" : "false" }, idSuffix: "-source")) { }
        }
        foreach (var term in terms) {
            var radians = term.Angle * Math.PI / 180;
            var width = Math.Abs(Math.Cos(radians)) * term.Width + Math.Abs(Math.Sin(radians)) * term.Height;
            var height = Math.Abs(Math.Sin(radians)) * term.Width + Math.Abs(Math.Cos(radians)) * term.Height;
            var bounds = new ChartRect(term.X - width / 2, term.Y - height / 2, width, height);
            var style = Style(chart, context, term.PointIndex, Color(series, term.PointIndex, colors), term.FontSize);
            style.Alignment = TextAlignment.Center;
            var shift = style.Baseline == TextBaseline.Superscript ? -style.FontSize * .35
                : style.Baseline == TextBaseline.Subscript ? style.FontSize * .22 : 0;
            using (Point(chart, builder, term.PointIndex, "word-cloud-term", bounds, extra:
                new Dictionary<string, string> { ["data-cfx-text"] = term.Text, ["data-cfx-angle"] = N(term.Angle),
                    ["data-cfx-font-size"] = N(style.EffectiveFontSize) }))
            using (builder.PushRotation(term.Angle, term.X, term.Y))
                builder.Text(term.Text, term.X, term.Y - term.Height / 2 + builder.TextAscent(style) - shift, style, "word-cloud-text",
                    paint: VisualChartPaint.ExplicitDataLabelColor(chart, term.PointIndex) ? VisualChartPaint.Text(style) : VisualChartPaint.Series(series, style.Color, term.PointIndex));
        }
    }
}
