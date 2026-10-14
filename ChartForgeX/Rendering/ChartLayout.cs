using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static class ChartLayout {
    public static ChartRect PlotArea(ChartOptions options) {
        var header = options.ShowHeader ? 44 : 0;
        var x = options.Padding.Left;
        var y = options.Padding.Top + header;
        var width = Math.Max(1, options.Size.Width - options.Padding.Left - options.Padding.Right);
        var height = Math.Max(1, options.Size.Height - options.Padding.Top - options.Padding.Bottom - header);
        return new ChartRect(x, y, width, height);
    }

    /// <summary>
    /// Returns the y below the text the header draws: under the subtitle, under the title, or zero when the header has
    /// no text. Callers check <see cref="ChartOptions.ShowHeader"/>.
    /// </summary>
    public static double HeaderBottom(Chart chart) {
        if (!string.IsNullOrWhiteSpace(chart.Subtitle)) {
            var metrics = SubtitleMetrics(chart);
            return SubtitleBaseline(chart) + metrics.Height - metrics.Ascent + 4;
        }
        if (string.IsNullOrWhiteSpace(chart.Title)) return chart.Options.Padding.Top;
        var title = TitleMetrics(chart);
        return TitleBaseline(chart) + title.Height - title.Ascent + 4;
    }

    internal static double TitleBaseline(Chart chart) => chart.Options.Padding.Top + (chart.Options.IsPanel ? 15 : chart.Options.TitleStyle.FontSize ?? chart.Options.ResolvedTheme.TitleFontSize);
    internal static double SubtitleBaseline(Chart chart) {
        var separation = (chart.Options.SubtitleStyle.FontSize ?? chart.Options.ResolvedTheme.SubtitleFontSize) + 4;
        if (!string.IsNullOrWhiteSpace(chart.Title)) {
            var title = TitleMetrics(chart);
            var subtitle = SubtitleMetrics(chart);
            separation = Math.Max(separation, title.Height - title.Ascent + subtitle.Ascent + 2.25);
        }
        return TitleBaseline(chart) + separation;
    }

    private static (double Height, double Ascent) TitleMetrics(Chart chart) => ChartTextLineMetrics.Measure(chart, chart.Title,
        chart.Options.TitleStyle.FontSize ?? (chart.Options.IsPanel ? 15 : chart.Options.ResolvedTheme.TitleFontSize), chart.Options.TitleStyle, 700);
    private static (double Height, double Ascent) SubtitleMetrics(Chart chart) => ChartTextLineMetrics.Measure(chart, chart.Subtitle,
        chart.Options.SubtitleStyle.FontSize ?? chart.Options.ResolvedTheme.SubtitleFontSize, chart.Options.SubtitleStyle, 400);
}
