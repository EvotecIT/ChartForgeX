using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static class ChartLayout {
    /// <summary>Baseline of the chart title drawn by the header, shared by the SVG and PNG renderers.</summary>
    public const double HeaderTitleBaseline = 52;

    /// <summary>Baseline of the chart subtitle drawn by the header.</summary>
    public const double HeaderSubtitleBaseline = 79;

    public static ChartRect PlotArea(ChartOptions options) {
        var header = options.ShowHeader ? options.Theme.UseGraphiteLayout ? 44 : 34 : 0;
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
        if (chart.Options.Theme.UseGraphiteLayout) {
            if (!string.IsNullOrWhiteSpace(chart.Subtitle)) return SubtitleBaseline(chart) + 4;
            return string.IsNullOrWhiteSpace(chart.Title) ? chart.Options.Padding.Top : TitleBaseline(chart) + 4;
        }
        if (!string.IsNullOrWhiteSpace(chart.Subtitle)) return HeaderSubtitleBaseline + 12;
        return string.IsNullOrWhiteSpace(chart.Title) ? 0 : HeaderTitleBaseline + 14;
    }

    internal static double TitleBaseline(Chart chart) => chart.Options.Theme.UseGraphiteLayout
        ? chart.Options.Padding.Top + (chart.Options.IsPanel ? 15 : chart.Options.TitleStyle.FontSize ?? chart.Options.Theme.TitleFontSize) : HeaderTitleBaseline;
    internal static double SubtitleBaseline(Chart chart) => chart.Options.Theme.UseGraphiteLayout
        ? TitleBaseline(chart) + (chart.Options.SubtitleStyle.FontSize ?? chart.Options.Theme.SubtitleFontSize) + 4 : HeaderSubtitleBaseline;
}
