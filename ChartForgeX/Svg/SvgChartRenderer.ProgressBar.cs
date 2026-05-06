using System;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawProgressBar(StringBuilder sb, Chart chart, ChartRect plot) {
        var series = chart.Series.FirstOrDefault(item => item.Kind == ChartSeriesKind.ProgressBar);
        if (series == null || series.Points.Count == 0) return;
        var t = chart.Options.Theme;
        var maximum = chart.Options.ProgressMaximum;
        var values = series.Points.ToArray();
        var showValues = chart.Options.ShowProgressValues;
        var showHandles = chart.Options.ShowProgressHandles;
        var rowHeight = Math.Max(30, Math.Min(58, plot.Height / Math.Max(1, values.Length)));
        var labelWidth = Math.Min(150, Math.Max(80, plot.Width * 0.24));
        var valueWidth = showValues ? Math.Min(78, Math.Max(48, plot.Width * 0.13)) : 0;
        var barArea = Math.Max(1, plot.Width - labelWidth - valueWidth - (showValues ? 28 : 14));
        var barHeight = Math.Max(6, Math.Min(30, rowHeight * chart.Options.ProgressBarThicknessRatio));
        var startX = plot.Left + labelWidth + 12;
        var startY = plot.Top + Math.Max(0, (plot.Height - rowHeight * values.Length) / 2);
        var writer = new SvgMarkupWriter(2048);
        writer
            .StartElement("g")
            .Attribute("data-cfx-role", "progress-bar-chart")
            .Attribute("data-cfx-maximum", maximum)
            .Attribute("data-cfx-show-values", showValues)
            .Attribute("data-cfx-show-handles", showHandles)
            .Attribute("data-cfx-bar-thickness-ratio", chart.Options.ProgressBarThicknessRatio)
            .Attribute("data-cfx-track-opacity", chart.Options.ProgressTrackOpacity)
            .EndStartElement()
            .Line();
        for (var i = 0; i < values.Length; i++) {
            var point = values[i];
            var y = startY + i * rowHeight + rowHeight / 2;
            var labelMaxWidth = Math.Max(8, labelWidth - 8);
            var rawLabel = FormatX(chart, point.X);
            var labelFontSize = TextFontSizeForSvgWidth(rawLabel, labelMaxWidth, t.TickLabelFontSize);
            var label = TrimSvgLabelToWidth(rawLabel, labelFontSize, labelMaxWidth);
            var color = ProgressItemColor(series, t, i);
            var ratio = Clamp(point.Y / maximum, 0, 1);
            var filledWidth = barArea * ratio;
            if (label.Length > 0) {
                WriteProgressText(writer, "progress-label", i, plot.Left + labelWidth - 8, y + labelFontSize / 3.0, label, t.MutedText.ToCss(), t.FontFamily, labelFontSize, "700", "end");
            }
            WriteProgressRect(writer, "progress-track", i, startX, y - barHeight / 2, barArea, barHeight, barHeight / 2, PictorialOpacity(t.Grid, chart.Options.ProgressTrackOpacity).ToCss());
            WriteProgressRect(writer, "progress-fill", i, startX, y - barHeight / 2, filledWidth, barHeight, barHeight / 2, color.ToCss(), point.Y, ratio);
            if (showHandles) {
                writer
                    .StartElement("circle")
                    .Attribute("data-cfx-role", "progress-handle")
                    .Attribute("data-cfx-point", i)
                    .Attribute("cx", startX + filledWidth)
                    .Attribute("cy", y)
                    .Attribute("r", Math.Max(5, barHeight * 0.62))
                    .Attribute("fill", t.CardBackground.ToCss())
                    .Attribute("stroke", color.ToCss())
                    .Attribute("stroke-width", Math.Max(2, barHeight * 0.18))
                    .EndEmptyElement()
                    .Line();
            }
            if (showValues) {
                var valueMaxWidth = Math.Max(8, valueWidth - 4);
                var rawValue = FormatValue(chart, point.Y);
                var valueFontSize = TextFontSizeForSvgWidth(rawValue, valueMaxWidth, t.DataLabelFontSize);
                var value = TrimSvgLabelToWidth(rawValue, valueFontSize, valueMaxWidth);
                if (value.Length > 0) {
                    WriteProgressText(writer, "progress-value", i, startX + barArea + 12, y + valueFontSize / 3.0, value, t.Text.ToCss(), t.FontFamily, valueFontSize, "800");
                }
            }
        }

        writer.EndElement().Line();
        sb.Append(writer.Build());
    }

    private static ChartColor ProgressItemColor(ChartSeries series, ChartTheme theme, int index) {
        if (index < series.PointColors.Count && series.PointColors[index].HasValue) return series.PointColors[index]!.Value;
        return series.Color ?? theme.Palette[index % theme.Palette.Length];
    }

    private static void WriteProgressRect(SvgMarkupWriter writer, string role, int pointIndex, double x, double y, double width, double height, double radius, string fill, double? value = null, double? ratio = null) {
        writer
            .StartElement("rect")
            .Attribute("data-cfx-role", role)
            .Attribute("data-cfx-point", pointIndex);
        if (value.HasValue) writer.Attribute("data-cfx-value", value.Value);
        if (ratio.HasValue) writer.Attribute("data-cfx-ratio", ratio.Value);
        writer
            .Attribute("x", x)
            .Attribute("y", y)
            .Attribute("width", width)
            .Attribute("height", height)
            .Attribute("rx", radius)
            .Attribute("fill", fill)
            .EndEmptyElement()
            .Line();
    }

    private static void WriteProgressText(SvgMarkupWriter writer, string role, int pointIndex, double x, double y, string text, string fill, string fontFamily, double fontSize, string fontWeight, string? anchor = null) {
        writer
            .StartElement("text")
            .Attribute("data-cfx-role", role)
            .Attribute("data-cfx-point", pointIndex)
            .Attribute("x", x)
            .Attribute("y", y);
        if (anchor != null) writer.Attribute("text-anchor", anchor);
        writer
            .Attribute("fill", fill)
            .Attribute("font-family", fontFamily)
            .Attribute("font-size", fontSize)
            .Attribute("font-weight", fontWeight)
            .Text(text)
            .EndElement()
            .Line();
    }
}
