using System;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawBullet(StringBuilder sb, Chart chart, ChartRect basePlot, string id) {
        var rows = chart.Series
            .Select((series, index) => new { series, index })
            .Where(item => item.series.Kind == ChartSeriesKind.Bullet && item.series.Points.Count >= 2)
            .ToArray();
        if (rows.Length == 0) return;

        var t = chart.Options.Theme;
        var labelReserve = Math.Min(220, Math.Max(112, rows.Max(row => EstimateTextWidth(row.series.Name, t.LegendFontSize)) + 28));
        var valueReserve = Math.Min(120, Math.Max(72, rows.Max(row => EstimateTextWidth(FormatValue(chart, BulletValue(row.series)), t.DataLabelFontSize)) + 30));
        var plot = new ChartRect(basePlot.X + labelReserve, basePlot.Y + 18, Math.Max(80, basePlot.Width - labelReserve - valueReserve), Math.Max(80, basePlot.Height - 54));
        var rowHeight = Math.Min(64, plot.Height / Math.Max(1, rows.Length));
        var barHeight = Math.Max(16, Math.Min(26, rowHeight * 0.38));

        sb.AppendLine("<g data-cfx-role=\"bullet-chart\">");
        for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++) {
            var row = rows[rowIndex];
            var y = plot.Top + rowHeight * rowIndex + rowHeight / 2;
            var min = BulletMin(row.series);
            var max = BulletMax(row.series);
            if (Math.Abs(max - min) < 0.000001) max = min + 1;
            var accent = row.series.Color ?? chart.Options.Theme.Palette[row.index % chart.Options.Theme.Palette.Length];
            var actualValue = BulletValue(row.series);
            var targetValue = BulletTarget(row.series);
            var status = BulletStatus(actualValue, targetValue);
            var rowSummary = row.series.Name + ": " + FormatValue(chart, actualValue) + ", target " + FormatValue(chart, targetValue) + ", " + status.Replace("-", " ");
            var statusColor = BulletStatusColor(t, status);

            sb.AppendLine($"<g data-cfx-role=\"bullet-row\" data-cfx-series=\"{row.index}\" data-cfx-status=\"{status}\" role=\"group\" aria-label=\"{Escape(rowSummary)}\">");
            sb.AppendLine($"<text data-cfx-role=\"bullet-row-label\" x=\"{F(basePlot.Left)}\" y=\"{F(y + 4)}\" fill=\"{t.Text.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.LegendFontSize)}\" font-weight=\"700\">{Escape(row.series.Name)}</text>");
            DrawBulletRanges(sb, row.series, plot, y, barHeight, min, max, accent);

            var value = Clamp(actualValue, min, max);
            var target = Clamp(targetValue, min, max);
            var valueX = BulletX(plot, min, max, value);
            var targetX = BulletX(plot, min, max, target);
            sb.AppendLine($"<rect data-cfx-role=\"bullet-value\" x=\"{F(plot.Left)}\" y=\"{F(y - barHeight * 0.24)}\" width=\"{F(Math.Max(2, valueX - plot.Left))}\" height=\"{F(barHeight * 0.48)}\" rx=\"{F(barHeight * 0.24)}\" fill=\"url(#{id}-seriesFill{row.index})\"/>");
            sb.AppendLine($"<line data-cfx-role=\"bullet-target\" x1=\"{F(targetX)}\" y1=\"{F(y - barHeight * 0.68)}\" x2=\"{F(targetX)}\" y2=\"{F(y + barHeight * 0.68)}\" stroke=\"{t.Text.ToCss()}\" stroke-width=\"3\" stroke-linecap=\"round\"/>");
            DrawBulletTargetLabel(sb, chart, FormatValue(chart, targetValue), targetX, y - barHeight * 0.92, plot);
            sb.AppendLine($"<circle data-cfx-role=\"bullet-status-marker\" data-cfx-status=\"{status}\" cx=\"{F(plot.Right + 8)}\" cy=\"{F(y)}\" r=\"4\" fill=\"{statusColor.ToCss()}\" stroke=\"{t.CardBackground.ToCss()}\" stroke-width=\"1.5\"/>");
            sb.AppendLine($"<text data-cfx-role=\"bullet-value-label\" x=\"{F(plot.Right + 18)}\" y=\"{F(y + 4)}\" fill=\"{t.Text.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.DataLabelFontSize)}\" font-weight=\"800\">{Escape(FormatValue(chart, actualValue))}</text>");
            sb.AppendLine("</g>");
        }

        DrawBulletAxis(sb, chart, plot, rows[0].series, basePlot.Bottom - 12);
        sb.AppendLine("</g>");
    }

    private static void DrawBulletTargetLabel(StringBuilder sb, Chart chart, string label, double x, double y, ChartRect plot) {
        var t = chart.Options.Theme;
        var text = "target " + label;
        var width = EstimateTextWidth(text, t.TickLabelFontSize);
        var anchor = EdgeAwareAnchor(text, x, plot, t.TickLabelFontSize);
        var safeX = Clamp(x, plot.Left + width / 2 + 2, plot.Right - width / 2 - 2);
        if (anchor == "start") safeX = Clamp(x, plot.Left + 2, plot.Right - width - 2);
        if (anchor == "end") safeX = Clamp(x, plot.Left + width + 2, plot.Right - 2);
        sb.AppendLine($"<text data-cfx-role=\"bullet-target-label\" x=\"{F(safeX)}\" y=\"{F(y)}\" text-anchor=\"{anchor}\" fill=\"{t.MutedText.ToCss()}\" stroke=\"{t.CardBackground.ToCss()}\" stroke-width=\"3\" paint-order=\"stroke fill\" stroke-linejoin=\"round\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.TickLabelFontSize)}\" font-weight=\"650\">{Escape(text)}</text>");
    }

    private static void DrawBulletRanges(StringBuilder sb, ChartSeries series, ChartRect plot, double y, double barHeight, double min, double max, ChartColor accent) {
        var previous = min;
        var ends = BulletRangeEnds(series, min, max);
        for (var i = 0; i < ends.Length; i++) {
            var end = Clamp(ends[i], min, max);
            if (end <= previous) continue;
            var x = BulletX(plot, min, max, previous);
            var width = BulletX(plot, min, max, end) - x;
            var opacity = Math.Max(0.10, 0.30 - i * 0.055);
            sb.AppendLine($"<rect data-cfx-role=\"bullet-range\" x=\"{F(x)}\" y=\"{F(y - barHeight / 2)}\" width=\"{F(width)}\" height=\"{F(barHeight)}\" rx=\"{F(barHeight / 2)}\" fill=\"{accent.ToCss()}\" opacity=\"{F(opacity)}\"/>");
            previous = end;
        }
    }

    private static void DrawBulletAxis(StringBuilder sb, Chart chart, ChartRect plot, ChartSeries reference, double y) {
        if (!chart.Options.ShowAxes) return;
        var t = chart.Options.Theme;
        var min = BulletMin(reference);
        var max = BulletMax(reference);
        if (Math.Abs(max - min) < 0.000001) max = min + 1;
        var ticks = new[] { min, min + (max - min) / 2, max };
        sb.AppendLine("<g data-cfx-role=\"bullet-axis\">");
        sb.AppendLine($"<line x1=\"{F(plot.Left)}\" y1=\"{F(y)}\" x2=\"{F(plot.Right)}\" y2=\"{F(y)}\" stroke=\"{t.Axis.ToCss()}\" stroke-width=\"1.1\"/>");
        foreach (var tick in ticks) {
            var x = BulletX(plot, min, max, tick);
            var label = FormatValue(chart, tick);
            var anchor = EdgeAwareAnchor(label, x, plot, t.TickLabelFontSize);
            var safeX = EdgeAwareTextX(label, x, plot, t.TickLabelFontSize);
            sb.AppendLine($"<line data-cfx-role=\"bullet-axis-tick\" x1=\"{F(x)}\" y1=\"{F(y - 4)}\" x2=\"{F(x)}\" y2=\"{F(y + 4)}\" stroke=\"{t.Axis.ToCss()}\" stroke-width=\"1.1\"/>");
            sb.AppendLine($"<text data-cfx-role=\"bullet-axis-label\" x=\"{F(safeX)}\" y=\"{F(y + 20)}\" text-anchor=\"{anchor}\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.TickLabelFontSize)}\">{Escape(label)}</text>");
        }
        sb.AppendLine("</g>");
    }

    private static double BulletMin(ChartSeries series) => series.Points[0].X;

    private static double BulletMax(ChartSeries series) => series.Points[1].X;

    private static double BulletValue(ChartSeries series) => series.Points[0].Y;

    private static double BulletTarget(ChartSeries series) => series.Points[1].Y;

    private static double BulletX(ChartRect plot, double min, double max, double value) => plot.Left + (value - min) / (max - min) * plot.Width;

    private static string BulletStatus(double value, double target) {
        if (value < target - 0.000001) return "below-target";
        if (value > target + 0.000001) return "above-target";
        return "meets-target";
    }

    private static ChartColor BulletStatusColor(ChartForgeX.Themes.ChartTheme theme, string status) {
        return status == "below-target" ? theme.Negative : theme.Positive;
    }

    private static double[] BulletRangeEnds(ChartSeries series, double min, double max) {
        var ends = series.Points.Skip(2).Select(point => point.X).Where(value => value > min && value < max).OrderBy(value => value).ToList();
        if (ends.Count == 0) {
            var span = max - min;
            ends.Add(min + span * 0.6);
            ends.Add(min + span * 0.8);
        }

        ends.Add(max);
        return ends.Distinct().ToArray();
    }
}
