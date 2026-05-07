using System;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawWordCloud(StringBuilder sb, Chart chart, ChartRect plot) {
        var terms = ChartWordCloudLayout.Compute(chart, plot);
        if (terms.Count == 0) return;
        var series = chart.Series.First(item => item.Kind == ChartSeriesKind.WordCloud);
        var t = chart.Options.Theme;
        var maximumTerms = chart.Options.WordCloudMaximumTerms.HasValue ? chart.Options.WordCloudMaximumTerms.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "all";
        var fontFamily = string.IsNullOrWhiteSpace(t.FontFamily) ? "system-ui, sans-serif" : t.FontFamily;
        var writer = new SvgMarkupWriter(2048);
        writer
            .StartElement("g")
            .Attribute("data-cfx-role", "word-cloud")
            .Attribute("data-cfx-density", chart.Options.WordCloudDensity)
            .Attribute("data-cfx-maximum-terms", maximumTerms)
            .Attribute("data-cfx-edge-padding", ChartWordCloudLayout.EdgePadding(plot))
            .EndStartElement()
            .Line();
        for (var i = 0; i < terms.Count; i++) {
            var term = terms[i];
            var color = WordCloudTermColor(series, t, term.PointIndex, i);
            var summary = term.Text + ": " + FormatValue(chart, term.Value);
            writer
                .StartElement("text")
                .Attribute("data-cfx-role", "word-cloud-term")
                .Attribute("data-cfx-point", term.PointIndex)
                .Attribute("data-cfx-text", term.Text)
                .Attribute("data-cfx-value", term.Value)
                .Attribute("role", "img")
                .Attribute("aria-label", summary)
                .Attribute("x", term.X)
                .Attribute("y", term.Y)
                .Attribute("text-anchor", "middle")
                .Attribute("dominant-baseline", "middle")
                .Attribute("fill", color.ToCss())
                .Attribute("font-family", fontFamily)
                .Attribute("font-size", term.FontSize)
                .Attribute("font-weight", "800");
            if (Math.Abs(term.Angle) >= 0.001) {
                writer.Attribute("transform", $"rotate({F(term.Angle)} {F(term.X)} {F(term.Y)})");
            }
            writer
                .Text(term.Text)
                .EndElement()
                .Line();
        }

        writer.EndElement().Line();
        sb.Append(writer.Build());
    }

    private static ChartColor WordCloudTermColor(ChartSeries series, ChartTheme theme, int pointIndex, int renderIndex) {
        if (pointIndex < series.PointColors.Count && series.PointColors[pointIndex].HasValue) return series.PointColors[pointIndex]!.Value;
        return series.Color ?? theme.Palette[renderIndex % theme.Palette.Length];
    }
}
