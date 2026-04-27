using System;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawFunnel(StringBuilder sb, Chart chart, ChartRect basePlot, string id) {
        var series = chart.Series.FirstOrDefault(item => item.Kind == ChartSeriesKind.Funnel);
        if (series == null) return;
        var values = series.Points.Where(point => point.Y > 0).ToArray();
        if (values.Length == 0) return;

        var t = chart.Options.Theme;
        var max = values.Max(point => point.Y);
        var metricsReserve = values.Length > 1 ? Math.Min(168, Math.Max(126, basePlot.Width * 0.22)) : 0;
        var innerLeft = basePlot.Left + 44;
        var innerRight = basePlot.Right - 44 - metricsReserve;
        var plot = new ChartRect(innerLeft, basePlot.Top + 18, Math.Max(120, innerRight - innerLeft), Math.Max(90, basePlot.Height - 62));
        var metricsX = Math.Min(basePlot.Right - metricsReserve + 10, plot.Right + 18);
        var gap = Math.Min(10, Math.Max(4, plot.Height / values.Length * 0.08));
        var segmentHeight = Math.Max(18, (plot.Height - gap * (values.Length - 1)) / values.Length);
        sb.AppendLine("<g data-cfx-role=\"funnel-chart\">");

        for (var i = 0; i < values.Length; i++) {
            var y = plot.Top + i * (segmentHeight + gap);
            var topWidth = FunnelWidth(plot.Width, values[i].Y, max);
            var nextValue = i + 1 < values.Length ? values[i + 1].Y : values[i].Y * 0.82;
            var bottomWidth = FunnelWidth(plot.Width, nextValue, max);
            var topLeft = plot.Left + (plot.Width - topWidth) / 2;
            var topRight = topLeft + topWidth;
            var bottomLeft = plot.Left + (plot.Width - bottomWidth) / 2;
            var bottomRight = bottomLeft + bottomWidth;
            var color = series.Color ?? chart.Options.Theme.Palette[i % chart.Options.Theme.Palette.Length];
            var fill = series.Color.HasValue ? color.ToCss() : $"url(#{id}-sliceFill{i % chart.Options.Theme.Palette.Length})";
            var label = FormatX(chart, values[i].X);
            var value = FormatValue(chart, values[i].Y);
            var retention = values[i].Y / values[0].Y;
            var previousRetention = i == 0 ? 1 : values[i].Y / values[i - 1].Y;
            var dropOff = i == 0 ? 0 : 1 - previousRetention;
            var summary = label + ": " + value + ", retained " + FormatPercent(retention);
            if (i > 0) summary += ", drop-off " + FormatPercent(dropOff);
            sb.AppendLine($"<path data-cfx-role=\"funnel-segment\" data-cfx-point=\"{i}\" data-cfx-retention=\"{F(retention)}\" data-cfx-dropoff=\"{F(dropOff)}\" role=\"img\" aria-label=\"{Escape(summary)}\" d=\"M {F(topLeft)} {F(y)} L {F(topRight)} {F(y)} L {F(bottomRight)} {F(y + segmentHeight)} L {F(bottomLeft)} {F(y + segmentHeight)} Z\" fill=\"{fill}\" stroke=\"{t.CardBackground.ToCss()}\" stroke-width=\"2\" stroke-linejoin=\"round\"/>");

            var centerX = plot.Left + plot.Width / 2;
            var centerY = y + segmentHeight / 2;
            var labelColor = FunnelTextColor(color);
            var labelStroke = FunnelTextHalo(labelColor, t.CardBackground);
            sb.AppendLine($"<text data-cfx-role=\"funnel-label\" x=\"{F(centerX)}\" y=\"{F(centerY - 4)}\" text-anchor=\"middle\" dominant-baseline=\"middle\" fill=\"{labelColor.ToCss()}\" stroke=\"{labelStroke.ToCss()}\" stroke-width=\"1.8\" paint-order=\"stroke fill\" stroke-linejoin=\"round\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.LegendFontSize)}\" font-weight=\"800\">{Escape(label)}</text>");
            sb.AppendLine($"<text data-cfx-role=\"funnel-value\" x=\"{F(centerX)}\" y=\"{F(centerY + 15)}\" text-anchor=\"middle\" dominant-baseline=\"middle\" fill=\"{labelColor.ToCss()}\" stroke=\"{labelStroke.ToCss()}\" stroke-width=\"1.8\" paint-order=\"stroke fill\" stroke-linejoin=\"round\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.DataLabelFontSize)}\" font-weight=\"750\">{Escape(value)}</text>");
            if (i > 0) {
                var guideX = Math.Min(metricsX - 10, bottomRight + 8);
                sb.AppendLine($"<line data-cfx-role=\"funnel-dropoff-line\" x1=\"{F(guideX)}\" y1=\"{F(y + gap * -0.35)}\" x2=\"{F(guideX)}\" y2=\"{F(y + segmentHeight * 0.45)}\" stroke=\"{t.Axis.ToCss()}\" stroke-width=\"1.1\" stroke-dasharray=\"3 4\" opacity=\"0.62\"/>");
                sb.AppendLine($"<text data-cfx-role=\"funnel-retention\" x=\"{F(metricsX)}\" y=\"{F(centerY - 3)}\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.TickLabelFontSize)}\" font-weight=\"700\">{Escape(FormatPercent(retention))} retained</text>");
                sb.AppendLine($"<text data-cfx-role=\"funnel-dropoff\" x=\"{F(metricsX)}\" y=\"{F(centerY + 14)}\" fill=\"{t.Negative.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.TickLabelFontSize)}\" font-weight=\"650\">-{Escape(FormatPercent(dropOff))} from prev</text>");
            }
        }

        sb.AppendLine("</g>");
    }

    private static double FunnelWidth(double plotWidth, double value, double max) {
        var ratio = max <= 0 ? 1 : Clamp(value / max, 0.04, 1);
        return Math.Max(70, plotWidth * (0.22 + ratio * 0.74));
    }

    private static ChartColor FunnelTextColor(ChartColor background) {
        var luminance = (0.2126 * background.R + 0.7152 * background.G + 0.0722 * background.B) / 255;
        return luminance > 0.58 ? ChartColor.FromRgb(15, 23, 42) : ChartColor.White;
    }

    private static ChartColor FunnelTextHalo(ChartColor text, ChartColor cardBackground) =>
        text.R > 240 && text.G > 240 && text.B > 240 ? cardBackground : ChartColor.Transparent;
}
