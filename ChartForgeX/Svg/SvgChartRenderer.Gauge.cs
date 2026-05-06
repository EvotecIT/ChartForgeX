using System;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

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
        var showLabels = series.ShowDataLabels != false;
        var cx = plot.Left + plot.Width / 2;
        var cy = plot.Top + plot.Height * ChartVisualPrimitives.GaugeCenterYFactor;
        var radius = Math.Max(ChartVisualPrimitives.GaugeMinRadius, Math.Min(plot.Width * ChartVisualPrimitives.GaugeRadiusWidthFactor, plot.Height * ChartVisualPrimitives.GaugeRadiusHeightFactor));
        var stroke = Math.Max(ChartVisualPrimitives.GaugeMinStrokeWidth, radius * ChartVisualPrimitives.GaugeStrokeRadiusFactor);
        var start = Math.PI;
        var end = Math.PI * 2;
        var valueEnd = start + (end - start) * ratio;
        var valueLabel = FormatValue(chart, value);
        var statusLabel = status.Replace("-", " ");
        var summary = series.Name + ": " + valueLabel + ", " + statusLabel;
        var writer = new SvgMarkupWriter(1024);

        writer
            .StartElement("g")
            .Attribute("data-cfx-role", "gauge")
            .Attribute("data-cfx-status", status)
            .Attribute("data-cfx-label", series.Name)
            .Attribute("data-cfx-value", value)
            .Attribute("data-cfx-min", min)
            .Attribute("data-cfx-max", max)
            .Attribute("data-cfx-percent", ratio)
            .Attribute("role", "img")
            .Attribute("aria-label", summary)
            .EndStartElement()
            .Line();
        WriteGaugePath(writer, "gauge-track", null, 0, 0, BuildGaugeArc(cx, cy, radius, start, end), t.Grid.ToCss(), stroke, ChartVisualPrimitives.GaugeTrackOpacity);
        WriteGaugePath(writer, "gauge-value", series.Name, value, ratio, BuildGaugeArc(cx, cy, radius, start, valueEnd), color.ToCss(), stroke);

        var labelWidth = Math.Max(48, Math.Min(plot.Width - 24, radius * 1.8));
        if (showLabels) {
            DrawSvgTextCenteredX(writer, chart, "gauge-label", valueLabel, cx, cy - radius * ChartVisualPrimitives.GaugeValueOffsetFactor, t.Text, Math.Max(34, t.TitleFontSize * 1.65), labelWidth, "850");
            DrawSvgTextCenteredX(writer, chart, "gauge-title", series.Name, cx, cy + ChartVisualPrimitives.GaugeTitleOffsetY, t.MutedText, t.LegendFontSize, labelWidth, "650", middleBaseline: false);
            var statusFontSize = TextFontSizeForSvgWidth(statusLabel, labelWidth, t.TickLabelFontSize);
            statusLabel = TrimSvgLabelToWidth(statusLabel, statusFontSize, labelWidth);
            var statusLeft = cx - EstimateTextWidth(statusLabel, statusFontSize) / 2.0;
            writer
                .StartElement("circle")
                .Attribute("data-cfx-role", "gauge-status-marker")
                .Attribute("data-cfx-status", status)
                .Attribute("cx", statusLeft - ChartVisualPrimitives.GaugeStatusMarkerOffsetX)
                .Attribute("cy", cy + ChartVisualPrimitives.GaugeStatusMarkerOffsetY)
                .Attribute("r", ChartVisualPrimitives.StatusMarkerRadius)
                .Attribute("fill", statusColor.ToCss())
                .Attribute("stroke", t.CardBackground.ToCss())
                .Attribute("stroke-width", ChartVisualPrimitives.StatusMarkerStrokeWidth)
                .EndEmptyElement()
                .Line()
                .StartElement("text")
                .Attribute("data-cfx-role", "gauge-status-label")
                .Attribute("x", statusLeft)
                .Attribute("y", cy + ChartVisualPrimitives.GaugeStatusTextOffsetY)
                .Attribute("fill", t.MutedText.ToCss())
                .Attribute("font-family", t.FontFamily)
                .Attribute("font-size", statusFontSize)
                .Attribute("font-weight", "650")
                .Text(statusLabel)
                .EndElement()
                .Line();
        }
        if (chart.Options.ShowAxes) {
            var axisLabelWidth = Math.Max(32, radius * 0.76);
            DrawSvgTextCenteredX(writer, chart, "gauge-min-label", FormatValue(chart, min), cx - radius, cy + ChartVisualPrimitives.GaugeAxisLabelOffsetY, t.MutedText, t.TickLabelFontSize, axisLabelWidth, "400");
            DrawSvgTextCenteredX(writer, chart, "gauge-max-label", FormatValue(chart, max), cx + radius, cy + ChartVisualPrimitives.GaugeAxisLabelOffsetY, t.MutedText, t.TickLabelFontSize, axisLabelWidth, "400");
        }
        writer.EndElement().Line();
        sb.Append(writer.Build());
    }

    private static string BuildGaugeArc(double cx, double cy, double radius, double start, double end) {
        var x1 = cx + Math.Cos(start) * radius;
        var y1 = cy + Math.Sin(start) * radius;
        var x2 = cx + Math.Cos(end) * radius;
        var y2 = cy + Math.Sin(end) * radius;
        return new SvgPathDataBuilder()
            .MoveTo(x1, y1)
            .ArcTo(radius, radius, 0, end - start > Math.PI, true, x2, y2)
            .Build();
    }

    private static void WriteGaugePath(SvgMarkupWriter writer, string role, string? label, double value, double percent, string path, string color, double strokeWidth, double? opacity = null) {
        writer
            .StartElement("path")
            .Attribute("data-cfx-role", role);
        if (label != null) {
            writer
                .Attribute("data-cfx-label", label)
                .Attribute("data-cfx-value", value)
                .Attribute("data-cfx-percent", percent);
        }

        writer
            .Attribute("d", path)
            .Attribute("fill", "none")
            .Attribute("stroke", color)
            .Attribute("stroke-width", strokeWidth)
            .Attribute("stroke-linecap", "round")
            .OptionalAttribute("opacity", opacity)
            .EndEmptyElement()
            .Line();
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
