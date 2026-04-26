using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawHeatmap(StringBuilder sb, Chart chart, ChartRect basePlot) {
        var rows = chart.Series.Where(series => series.Kind == ChartSeriesKind.Heatmap).ToArray();
        if (rows.Length == 0) return;

        var columns = rows.SelectMany(series => series.Points.Select(point => point.X)).Distinct().OrderBy(value => value).ToArray();
        if (columns.Length == 0) return;

        var t = chart.Options.Theme;
        var values = rows.SelectMany(series => series.Points.Select(point => point.Y)).ToArray();
        var min = values.Length == 0 ? 0 : values.Min();
        var max = values.Length == 0 ? 1 : values.Max();
        if (Math.Abs(max - min) < 0.000001) max = min + 1;

        var plot = ApplyHeatmapLabelReserve(chart, basePlot, rows, columns);
        var gap = Math.Min(6, Math.Max(2, Math.Min(plot.Width / columns.Length, plot.Height / rows.Length) * 0.05));
        var cellWidth = Math.Max(1, (plot.Width - gap * (columns.Length - 1)) / columns.Length);
        var cellHeight = Math.Max(1, (plot.Height - gap * (rows.Length - 1)) / rows.Length);
        var radius = Math.Min(8, Math.Min(cellWidth, cellHeight) * 0.16);

        sb.AppendLine("<g data-cfx-role=\"heatmap\">");
        for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++) {
            var series = rows[rowIndex];
            var y = plot.Top + rowIndex * (cellHeight + gap);
            sb.AppendLine($"<text data-cfx-role=\"heatmap-row-label\" x=\"{F(plot.Left - 12)}\" y=\"{F(y + cellHeight / 2)}\" text-anchor=\"end\" dominant-baseline=\"middle\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.TickLabelFontSize)}\" font-weight=\"650\">{Escape(series.Name)}</text>");
            for (var columnIndex = 0; columnIndex < columns.Length; columnIndex++) {
                var column = columns[columnIndex];
                var value = FindHeatmapValue(series, column);
                var x = plot.Left + columnIndex * (cellWidth + gap);
                var ratio = HeatmapRatio(value, min, max);
                var status = HeatmapStatus(ratio);
                var color = HeatmapColor(chart, series.Color, value, min, max);
                var summary = series.Name + ", " + FormatX(chart, column) + ": " + FormatValue(chart, value);
                if (chart.Options.HeatmapScale == ChartHeatmapScale.Semantic) summary += ", " + status;
                sb.AppendLine($"<rect data-cfx-role=\"heatmap-cell\" data-cfx-row=\"{rowIndex}\" data-cfx-column=\"{columnIndex}\" data-cfx-status=\"{status}\" role=\"img\" aria-label=\"{Escape(summary)}\" x=\"{F(x)}\" y=\"{F(y)}\" width=\"{F(cellWidth)}\" height=\"{F(cellHeight)}\" rx=\"{F(radius)}\" fill=\"{color.ToCss()}\" stroke=\"{t.CardBackground.ToCss()}\" stroke-opacity=\"0.32\"/>");
                if (chart.Options.ShowDataLabels && cellWidth >= 34 && cellHeight >= 20) {
                    var label = FormatValue(chart, value);
                    sb.AppendLine($"<text data-cfx-role=\"data-label\" x=\"{F(x + cellWidth / 2)}\" y=\"{F(y + cellHeight / 2)}\" text-anchor=\"middle\" dominant-baseline=\"middle\" fill=\"{HeatmapTextColor(color).ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.DataLabelFontSize)}\" font-weight=\"750\">{Escape(label)}</text>");
                }
            }
        }

        for (var columnIndex = 0; columnIndex < columns.Length; columnIndex++) {
            var x = plot.Left + columnIndex * (cellWidth + gap) + cellWidth / 2;
            sb.AppendLine($"<text data-cfx-role=\"heatmap-column-label\" x=\"{F(x)}\" y=\"{F(plot.Bottom + 22)}\" text-anchor=\"middle\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.TickLabelFontSize)}\" font-weight=\"650\">{Escape(FormatX(chart, columns[columnIndex]))}</text>");
        }

        if (!string.IsNullOrWhiteSpace(chart.XAxisTitle)) sb.AppendLine($"<text x=\"{F(plot.Left + plot.Width / 2)}\" y=\"{F(plot.Bottom + 48)}\" text-anchor=\"middle\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.AxisTitleFontSize)}\" font-weight=\"600\">{Escape(chart.XAxisTitle)}</text>");
        if (!string.IsNullOrWhiteSpace(chart.YAxisTitle)) sb.AppendLine($"<text transform=\"translate(26 {F(plot.Top + plot.Height / 2)}) rotate(-90)\" text-anchor=\"middle\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.AxisTitleFontSize)}\" font-weight=\"600\">{Escape(chart.YAxisTitle)}</text>");

        DrawHeatmapScale(sb, chart, plot, min, max, rows[0].Color);
        sb.AppendLine("</g>");
    }

    private static ChartRect ApplyHeatmapLabelReserve(Chart chart, ChartRect plot, IReadOnlyList<ChartSeries> rows, IReadOnlyList<double> columns) {
        var t = chart.Options.Theme;
        var leftReserve = rows.Max(series => EstimateTextWidth(series.Name, t.TickLabelFontSize)) + 58;
        var bottomReserve = 56 + (string.IsNullOrWhiteSpace(chart.XAxisTitle) ? 0 : 20);
        var desiredLeft = Math.Max(plot.Left, leftReserve);
        var maxLeft = Math.Max(plot.Left, chart.Options.Size.Width - chart.Options.Padding.Right - 220);
        var shift = Math.Max(0, Math.Min(desiredLeft, maxLeft) - plot.Left);
        var maxColumnLabel = columns.Max(column => EstimateTextWidth(FormatX(chart, column), t.TickLabelFontSize));
        var bottom = Math.Max(bottomReserve, maxColumnLabel > 68 ? 70 : bottomReserve);
        return new ChartRect(plot.X + shift, plot.Y, Math.Max(1, plot.Width - shift), Math.Max(1, plot.Height - bottom));
    }

    private static void DrawHeatmapScale(StringBuilder sb, Chart chart, ChartRect plot, double min, double max, ChartColor? highColor) {
        var t = chart.Options.Theme;
        var steps = 5;
        var width = 118.0;
        var height = 8.0;
        var x = plot.Right - width;
        var y = plot.Bottom + 42;
        for (var i = 0; i < steps; i++) {
            var ratio = i / (double)(steps - 1);
            var value = min + (max - min) * ratio;
            var color = HeatmapColor(chart, highColor, value, min, max);
            sb.AppendLine($"<rect data-cfx-role=\"heatmap-scale-step\" data-cfx-status=\"{HeatmapStatus(HeatmapRatio(value, min, max))}\" x=\"{F(x + i * width / steps)}\" y=\"{F(y)}\" width=\"{F(width / steps + 0.5)}\" height=\"{F(height)}\" rx=\"2\" fill=\"{color.ToCss()}\"/>");
        }

        sb.AppendLine($"<text x=\"{F(x)}\" y=\"{F(y + 24)}\" text-anchor=\"start\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.TickLabelFontSize)}\">{Escape(FormatValue(chart, min))}</text>");
        sb.AppendLine($"<text x=\"{F(x + width)}\" y=\"{F(y + 24)}\" text-anchor=\"end\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.TickLabelFontSize)}\">{Escape(FormatValue(chart, max))}</text>");
    }

    private static ChartColor HeatmapColor(Chart chart, ChartColor? highColor, double value, double min, double max) {
        var ratio = HeatmapRatio(value, min, max);
        if (chart.Options.HeatmapScale == ChartHeatmapScale.Semantic) return SemanticHeatmapColor(chart, ratio);
        return Blend(chart.Options.Theme.PlotBackground, highColor ?? chart.Options.Theme.Palette[0], 0.18 + ratio * 0.82);
    }

    private static ChartColor SemanticHeatmapColor(Chart chart, double ratio) {
        var t = chart.Options.Theme;
        if (ratio < 0.60) return Blend(t.Negative, t.Warning, ratio / 0.60 * 0.42);
        if (ratio < 0.80) return Blend(t.Warning, t.Positive, (ratio - 0.60) / 0.20 * 0.5);
        return Blend(t.Warning, t.Positive, 0.65 + (ratio - 0.80) / 0.20 * 0.35);
    }

    private static double HeatmapRatio(double value, double min, double max) {
        if (min >= -0.000001 && max <= 100.000001) return Clamp(value / 100, 0, 1);
        return Clamp((value - min) / Math.Max(0.000001, max - min), 0, 1);
    }

    private static string HeatmapStatus(double ratio) {
        if (ratio < 0.60) return "negative";
        if (ratio < 0.80) return "warning";
        return "positive";
    }

    private static ChartColor Blend(ChartColor a, ChartColor b, double amount) {
        amount = Clamp(amount, 0, 1);
        return ChartColor.FromRgb(
            (byte)Math.Round(a.R + (b.R - a.R) * amount),
            (byte)Math.Round(a.G + (b.G - a.G) * amount),
            (byte)Math.Round(a.B + (b.B - a.B) * amount));
    }

    private static ChartColor HeatmapTextColor(ChartColor background) {
        var luminance = (0.2126 * background.R + 0.7152 * background.G + 0.0722 * background.B) / 255;
        return luminance > 0.54 ? ChartColor.FromRgb(15, 23, 42) : ChartColor.White;
    }

    private static double FindHeatmapValue(ChartSeries series, double column) {
        foreach (var point in series.Points) {
            if (Math.Abs(point.X - column) < 0.000001) return point.Y;
        }

        return 0;
    }
}
