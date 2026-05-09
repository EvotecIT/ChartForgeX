using System;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawCircleChart(StringBuilder sb, Chart chart, ChartRect plot) {
        var series = chart.Series.FirstOrDefault(item => item.Kind == ChartSeriesKind.Circle);
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
        var cy = plot.Top + plot.Height * 0.54;
        var radius = Math.Max(24, Math.Min(plot.Width, plot.Height) * 0.28 * chart.Options.CircleRadiusScale);
        var stroke = Math.Max(5, radius * 0.16 * chart.Options.CircleStrokeScale);
        var start = -Math.PI / 2;
        var valueEnd = start + Math.PI * 2 * ratio;
        var valueLabel = FormatValue(chart, value);
        var statusLabel = status.Replace("-", " ");
        var labelWidth = Math.Max(60, Math.Min(plot.Width - 24, radius * 1.65));
        var summary = series.Name + ": " + valueLabel + ", " + statusLabel;
        var showLabels = series.ShowDataLabels != false;

        var writer = new SvgMarkupWriter(2048);
        writer
            .StartElement("g")
            .Attribute("data-cfx-role", "circle-chart")
            .Attribute("data-cfx-status", status)
            .Attribute("data-cfx-label", series.Name)
            .Attribute("data-cfx-value", value)
            .Attribute("data-cfx-min", min)
            .Attribute("data-cfx-max", max)
            .Attribute("data-cfx-percent", ratio)
            .Attribute("data-cfx-radius-scale", chart.Options.CircleRadiusScale)
            .Attribute("data-cfx-stroke-scale", chart.Options.CircleStrokeScale)
            .Attribute("role", "img")
            .Attribute("aria-label", summary)
            .EndStartElement()
            .Line();
        WriteCirclePath(
            writer,
            "circle-track",
            null,
            0,
            0,
            BuildRadialBarArc(cx, cy, radius, start, start + Math.PI * 2),
            t.Grid.ToCss(),
            stroke,
            ChartVisualPrimitives.CircleTrackOpacity);
        if (ratio > 0) {
            WriteCirclePath(
                writer,
                "circle-value",
                series.Name,
                value,
                ratio,
                BuildRadialBarArc(cx, cy, radius, start, valueEnd),
                color.ToCss(),
                stroke);
        }

        writer
            .StartElement("circle")
            .Attribute("data-cfx-role", "circle-center")
            .Attribute("cx", cx)
            .Attribute("cy", cy)
            .Attribute("r", Math.Max(18, radius - stroke * 0.82))
            .Attribute("fill", t.CardBackground.ToCss())
            .Attribute("fill-opacity", ChartVisualPrimitives.CircleCenterFillOpacity)
            .Attribute("stroke", t.Grid.ToCss())
            .Attribute("stroke-opacity", ChartVisualPrimitives.CircleCenterStrokeOpacity)
            .EndEmptyElement()
            .Line();
        if (showLabels) {
            var valueFontSize = Math.Max(24, Math.Min(t.TitleFontSize * 1.72, radius * 0.72));
            var titleFontSize = Math.Max(9, Math.Min(t.LegendFontSize, radius * 0.22));
            var centerLineGap = Math.Max(4, Math.Min(8, radius * 0.05));
            var centerGroupHeight = valueFontSize + centerLineGap + titleFontSize;
            var valueY = cy - centerGroupHeight / 2.0 + valueFontSize / 2.0;
            var titleY = valueY + valueFontSize / 2.0 + centerLineGap + titleFontSize / 2.0;
            DrawSvgTextCenteredX(writer, chart, "circle-label", valueLabel, cx, valueY, t.Text, valueFontSize, labelWidth, "850", t.CardBackground, 3.2);
            DrawSvgTextCenteredX(writer, chart, "circle-title", series.Name, cx, titleY, t.MutedText, titleFontSize, labelWidth, "700", t.CardBackground, 2.4);
            if (chart.Options.ShowCircleStatusLabel) {
                var statusFontSize = TextFontSizeForSvgWidth(statusLabel, labelWidth, t.TickLabelFontSize);
                statusLabel = TrimSvgLabelToWidth(statusLabel, statusFontSize, labelWidth);
                var statusLeft = cx - EstimateTextWidth(statusLabel, statusFontSize) / 2.0;
                writer
                    .StartElement("circle")
                    .Attribute("data-cfx-role", "circle-status-marker")
                    .Attribute("data-cfx-status", status)
                    .Attribute("cx", statusLeft - 9)
                    .Attribute("cy", cy + radius + 36)
                    .Attribute("r", ChartVisualPrimitives.StatusMarkerRadius)
                    .Attribute("fill", statusColor.ToCss())
                    .Attribute("stroke", t.CardBackground.ToCss())
                    .Attribute("stroke-width", ChartVisualPrimitives.StatusMarkerStrokeWidth)
                    .EndEmptyElement()
                    .Line()
                    .StartElement("text")
                    .Attribute("data-cfx-role", "circle-status-label")
                    .Attribute("x", statusLeft)
                    .Attribute("y", cy + radius + 40)
                    .Attribute("fill", t.MutedText.ToCss())
                    .Attribute("font-family", t.FontFamily)
                    .Attribute("font-size", statusFontSize)
                    .Attribute("font-weight", "650")
                    .Text(statusLabel)
                    .EndElement()
                    .Line();
            }
        }
        writer.EndElement().Line();
        sb.Append(writer.Build());
    }

    private static void WriteCirclePath(
        SvgMarkupWriter writer,
        string role,
        string? label,
        double value,
        double ratio,
        string path,
        string color,
        double strokeWidth,
        double? opacity = null) {
        writer.StartElement("path").Attribute("data-cfx-role", role);
        if (label != null) {
            writer
                .Attribute("data-cfx-label", label)
                .Attribute("data-cfx-value", value)
                .Attribute("data-cfx-ratio", ratio)
                .Attribute("data-cfx-percent", ratio);
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

    private static bool IsCircleChart(Chart chart) => ChartSeriesKindTraits.ContainsKind(chart, ChartSeriesKind.Circle);
}
