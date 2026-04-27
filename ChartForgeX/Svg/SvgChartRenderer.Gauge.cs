using System;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawGauge(StringBuilder sb, Chart chart, ChartRect plot) {
        var series = chart.Series.FirstOrDefault(item => item.Kind == ChartSeriesKind.Gauge);
        if (series == null || series.Points.Count == 0) return;

        var t = chart.Options.Theme;
        var min = series.Points[0].X;
        var max = series.Points.Count > 1 ? series.Points[1].X : 100;
        if (Math.Abs(max - min) < 0.000001) max = min + 1;
        var value = Clamp(series.Points[0].Y, min, max);
        var ratio = Clamp((value - min) / (max - min), 0, 1);
        var status = GaugeStatus(ratio);
        var statusColor = GaugeStatusColor(t, status);
        var color = series.Color ?? statusColor;
        var cx = plot.Left + plot.Width / 2;
        var cy = plot.Top + plot.Height * 0.66;
        var radius = Math.Max(48, Math.Min(plot.Width * 0.36, plot.Height * 0.52));
        var stroke = Math.Max(14, radius * 0.16);
        var start = Math.PI;
        var end = Math.PI * 2;
        var valueEnd = start + (end - start) * ratio;
        var valueLabel = FormatValue(chart, value);
        var statusLabel = status.Replace("-", " ");
        var summary = series.Name + ": " + valueLabel + ", " + statusLabel;

        sb.AppendLine($"<g data-cfx-role=\"gauge\" data-cfx-status=\"{status}\" role=\"img\" aria-label=\"{Escape(summary)}\">");
        sb.AppendLine($"<path data-cfx-role=\"gauge-track\" d=\"{BuildGaugeArc(cx, cy, radius, start, end)}\" fill=\"none\" stroke=\"{t.Grid.ToCss()}\" stroke-width=\"{F(stroke)}\" stroke-linecap=\"round\" opacity=\"0.62\"/>");
        sb.AppendLine($"<path data-cfx-role=\"gauge-value\" d=\"{BuildGaugeArc(cx, cy, radius, start, valueEnd)}\" fill=\"none\" stroke=\"{color.ToCss()}\" stroke-width=\"{F(stroke)}\" stroke-linecap=\"round\"/>");

        var labelWidth = Math.Max(48, Math.Min(plot.Width - 24, radius * 1.8));
        DrawSvgTextCenteredX(sb, chart, "gauge-label", valueLabel, cx, cy - radius * 0.1, t.Text, Math.Max(34, t.TitleFontSize * 1.65), labelWidth, "850");
        DrawSvgTextCenteredX(sb, chart, string.Empty, series.Name, cx, cy + 34, t.MutedText, t.LegendFontSize, labelWidth, "650", middleBaseline: false);
        var statusFontSize = TextFontSizeForSvgWidth(statusLabel, labelWidth, t.TickLabelFontSize);
        statusLabel = TrimSvgLabelToWidth(statusLabel, statusFontSize, labelWidth);
        var statusLeft = cx - EstimateTextWidth(statusLabel, statusFontSize) / 2.0;
        sb.AppendLine($"<circle data-cfx-role=\"gauge-status-marker\" data-cfx-status=\"{status}\" cx=\"{F(statusLeft - 9)}\" cy=\"{F(cy + 53)}\" r=\"4\" fill=\"{statusColor.ToCss()}\" stroke=\"{t.CardBackground.ToCss()}\" stroke-width=\"1.5\"/>");
        sb.AppendLine($"<text data-cfx-role=\"gauge-status-label\" x=\"{F(statusLeft)}\" y=\"{F(cy + 57)}\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(statusFontSize)}\" font-weight=\"650\">{Escape(statusLabel)}</text>");
        sb.AppendLine($"<text x=\"{F(cx - radius)}\" y=\"{F(cy + 27)}\" text-anchor=\"middle\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.TickLabelFontSize)}\">{Escape(FormatValue(chart, min))}</text>");
        sb.AppendLine($"<text x=\"{F(cx + radius)}\" y=\"{F(cy + 27)}\" text-anchor=\"middle\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.TickLabelFontSize)}\">{Escape(FormatValue(chart, max))}</text>");
        sb.AppendLine("</g>");
    }

    private static string BuildGaugeArc(double cx, double cy, double radius, double start, double end) {
        var x1 = cx + Math.Cos(start) * radius;
        var y1 = cy + Math.Sin(start) * radius;
        var x2 = cx + Math.Cos(end) * radius;
        var y2 = cy + Math.Sin(end) * radius;
        var largeArc = end - start > Math.PI ? 1 : 0;
        return $"M {F(x1)} {F(y1)} A {F(radius)} {F(radius)} 0 {largeArc} 1 {F(x2)} {F(y2)}";
    }

    private static string GaugeStatus(double ratio) {
        if (ratio < 0.60) return "negative";
        if (ratio < 0.80) return "warning";
        return "positive";
    }

    private static ChartColor GaugeStatusColor(ChartForgeX.Themes.ChartTheme theme, string status) {
        return status == "negative" ? theme.Negative : status == "warning" ? theme.Warning : theme.Positive;
    }
}
