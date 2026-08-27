using System;
using System.Text;
using ChartForgeX.Core;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawHeader(StringBuilder sb, Chart chart) {
        var t = chart.Options.Theme;
        var maxWidth = Math.Max(24, chart.Options.Size.Width - 80);
        DrawSvgTextLeft(sb, chart, "chart-title", chart.Title, 40, 52, t.Text, StyleFontSize(chart.Options.TitleStyle, t.TitleFontSize), maxWidth, "750", chart.Options.TitleStyle);
        if (!string.IsNullOrWhiteSpace(chart.Subtitle)) DrawSvgTextLeft(sb, chart, "chart-subtitle", chart.Subtitle, 40, 79, t.MutedText, StyleFontSize(chart.Options.SubtitleStyle, t.SubtitleFontSize), maxWidth, "400", chart.Options.SubtitleStyle);
    }
}
