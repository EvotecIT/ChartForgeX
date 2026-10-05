using System;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Primitives;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawHeader(StringBuilder sb, Chart chart, ChartRect plot) {
        var t = chart.Options.Theme;
        var left = t.UseGraphiteLayout ? plot.Left : 40;
        var maxWidth = Math.Max(24, chart.Options.Size.Width - left - chart.Options.Padding.Right);
        DrawSvgTextLeft(sb, chart, "chart-title", chart.Title, left, ChartLayout.TitleBaseline(chart), t.Text, StyleFontSize(chart.Options.TitleStyle, chart.Options.IsPanel ? 15 : t.TitleFontSize), maxWidth, t.UseGraphiteLayout ? "700" : "750", chart.Options.TitleStyle);
        if (!string.IsNullOrWhiteSpace(chart.Subtitle)) DrawSvgTextLeft(sb, chart, "chart-subtitle", chart.Subtitle, left, ChartLayout.SubtitleBaseline(chart), t.MutedText, StyleFontSize(chart.Options.SubtitleStyle, t.SubtitleFontSize), maxWidth, "400", chart.Options.SubtitleStyle);
    }
}
