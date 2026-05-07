using System;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawFunnel(StringBuilder sb, Chart chart, ChartRect basePlot, string id) {
        ChartSeries? series = null;
        for (var i = 0; i < chart.Series.Count; i++) {
            if (chart.Series[i].Kind != ChartSeriesKind.Funnel) continue;
            series = chart.Series[i];
            break;
        }
        if (series == null) return;
        var values = series.Points.ToArray();
        if (values.Length == 0) return;

        var t = chart.Options.Theme;
        var max = values.Max(point => point.Y);
        var showLabels = series.ShowDataLabels != false;
        var metricsReserve = showLabels && values.Length > 1 ? Math.Min(168, Math.Max(126, basePlot.Width * 0.22)) : 0;
        var innerLeft = basePlot.Left + 44;
        var innerRight = basePlot.Right - 44 - metricsReserve;
        var plot = new ChartRect(innerLeft, basePlot.Top + 18, Math.Max(120, innerRight - innerLeft), Math.Max(90, basePlot.Height - 62));
        var metricsX = Math.Min(basePlot.Right - metricsReserve + 10, plot.Right + 18);
        var gap = Math.Min(10, Math.Max(4, plot.Height / values.Length * 0.08));
        var segmentHeights = FunnelSegmentHeights(values, plot.Height, gap);
        var y = plot.Top;
        var writer = new SvgMarkupWriter(4096);
        writer
            .StartElement("g")
            .Attribute("data-cfx-role", "funnel-chart")
            .EndStartElement()
            .Line();
        DrawFunnelPointGradients(writer, id, series);

        for (var i = 0; i < values.Length; i++) {
            var segmentHeight = segmentHeights[i];
            var segmentY = y;
            var segmentDrawHeight = segmentHeight;
            if (values[i].Y <= 0) {
                segmentDrawHeight = Math.Max(8, Math.Min(16, segmentHeight * 0.24));
                segmentY = y + (segmentHeight - segmentDrawHeight) / 2;
            }
            var topWidth = FunnelWidth(plot.Width, values[i].Y, max);
            var nextValue = i + 1 < values.Length ? values[i + 1].Y : values[i].Y * 0.82;
            var bottomWidth = values[i].Y <= 0 ? topWidth : FunnelWidth(plot.Width, nextValue, max);
            var topLeft = plot.Left + (plot.Width - topWidth) / 2;
            var topRight = topLeft + topWidth;
            var bottomLeft = plot.Left + (plot.Width - bottomWidth) / 2;
            var bottomRight = bottomLeft + bottomWidth;
            var color = FunnelSegmentColor(chart, series, i);
            var fill = FunnelSegmentFill(chart, series, i, id);
            var label = FormatX(chart, values[i].X);
            var value = FormatValue(chart, values[i].Y);
            var retention = FunnelRatio(values[i].Y, values[0].Y);
            var dropOff = i == 0 ? 0 : FunnelDropOff(values[i].Y, values[i - 1].Y);
            var summary = label + ": " + value + ", retained " + FormatPercent(retention);
            if (i > 0) summary += ", drop-off " + FormatPercent(dropOff);
            writer
                .StartElement("path")
                .Attribute("data-cfx-role", "funnel-segment")
                .Attribute("data-cfx-point", i)
                .Attribute("data-cfx-label", label)
                .Attribute("data-cfx-value", values[i].Y)
                .Attribute("data-cfx-retention", retention)
                .Attribute("data-cfx-dropoff", dropOff)
                .Attribute("role", "img")
                .Attribute("aria-label", summary)
                .Attribute("d", $"M {F(topLeft)} {F(segmentY)} L {F(topRight)} {F(segmentY)} L {F(bottomRight)} {F(segmentY + segmentDrawHeight)} L {F(bottomLeft)} {F(segmentY + segmentDrawHeight)} Z")
                .Attribute("fill", fill)
                .Attribute("stroke", t.CardBackground.ToCss())
                .Attribute("stroke-width", ChartVisualPrimitives.FunnelSegmentStrokeWidth)
                .Attribute("stroke-opacity", ChartVisualPrimitives.FunnelSegmentStrokeOpacity)
                .Attribute("stroke-linejoin", "round")
                .EndEmptyElement()
                .Line();

            var centerX = plot.Left + plot.Width / 2;
            var centerY = segmentY + segmentDrawHeight / 2;
            var labelColor = FunnelTextColor(color);
            var labelStroke = FunnelTextHalo(labelColor, t.CardBackground);
            var labelWidth = Math.Max(36, Math.Min(topWidth, bottomWidth) - 18);
            if (showLabels) {
                var labelWriter = new StringBuilder();
                if (values[i].Y <= 0) {
                    var zeroLabelX = topRight + 12;
                    var zeroLabelWidth = Math.Max(44, Math.Min(metricsX - zeroLabelX - 12, plot.Right - zeroLabelX));
                    DrawSvgTextLeft(labelWriter, chart, "funnel-label", label, zeroLabelX, centerY - 6, t.Text, t.LegendFontSize, zeroLabelWidth, "800");
                    DrawSvgTextLeft(labelWriter, chart, "funnel-value", value, zeroLabelX, centerY + 13, t.MutedText, t.DataLabelFontSize, zeroLabelWidth, "750");
                } else {
                    DrawSvgTextCenteredX(labelWriter, chart, "funnel-label", label, centerX, centerY - 4, labelColor, t.LegendFontSize, labelWidth, "800", labelStroke, 1.8);
                    DrawSvgTextCenteredX(labelWriter, chart, "funnel-value", value, centerX, centerY + 15, labelColor, t.DataLabelFontSize, labelWidth, "750", labelStroke, 1.8);
                }
                writer.Raw(labelWriter.ToString());
            }
            if (showLabels && i > 0) {
                var guideX = Math.Min(metricsX - 10, bottomRight + 8);
                var metricMaxWidth = Math.Max(32, basePlot.Right - metricsX - 6);
                var retentionLabel = FormatPercent(retention) + " retained";
                var dropOffLabel = FormatFunnelDropOffLabel(dropOff, values[i - 1].Y);
                var dropOffColor = values[i - 1].Y <= 0 ? t.MutedText : t.Negative;
                writer
                    .StartElement("line")
                    .Attribute("data-cfx-role", "funnel-dropoff-line")
                    .Attribute("x1", guideX)
                    .Attribute("y1", segmentY + gap * -0.35)
                    .Attribute("x2", guideX)
                    .Attribute("y2", segmentY + segmentDrawHeight * 0.55)
                    .Attribute("stroke", t.Axis.ToCss())
                    .Attribute("stroke-width", ChartVisualPrimitives.FunnelDropoffLineStrokeWidth)
                    .Attribute("stroke-dasharray", "3 4")
                    .Attribute("opacity", ChartVisualPrimitives.FunnelDropoffLineOpacity)
                    .EndEmptyElement()
                    .Line();
                var metricsWriter = new StringBuilder();
                DrawSvgTextLeft(metricsWriter, chart, "funnel-retention", retentionLabel, metricsX, centerY - 3, t.MutedText, t.TickLabelFontSize, metricMaxWidth, "700");
                DrawSvgTextLeft(metricsWriter, chart, "funnel-dropoff", dropOffLabel, metricsX, centerY + 14, dropOffColor, t.TickLabelFontSize, metricMaxWidth, "650");
                writer.Raw(metricsWriter.ToString());
            }

            y += segmentHeight + gap;
        }

        writer.EndElement().Line();
        sb.Append(writer.Build());
    }

    private static double[] FunnelSegmentHeights(ChartPoint[] values, double plotHeight, double gap) {
        var heights = new double[values.Length];
        if (values.Length == 0) return heights;
        var totalGap = gap * Math.Max(0, values.Length - 1);
        var drawableHeight = Math.Max(18 * values.Length, plotHeight - totalGap);
        var zeroCount = values.Count(point => point.Y <= 0);
        if (zeroCount == 0) {
            var evenHeight = Math.Max(18, drawableHeight / values.Length);
            for (var i = 0; i < heights.Length; i++) heights[i] = evenHeight;
            return heights;
        }

        var nonZeroCount = values.Length - zeroCount;
        var zeroSlot = Math.Min(52, Math.Max(30, drawableHeight * 0.11));
        if (nonZeroCount == 0) {
            var evenZeroHeight = Math.Max(18, drawableHeight / values.Length);
            for (var i = 0; i < heights.Length; i++) heights[i] = evenZeroHeight;
            return heights;
        }

        var nonZeroHeight = Math.Max(42, (drawableHeight - zeroSlot * zeroCount) / nonZeroCount);
        for (var i = 0; i < values.Length; i++) heights[i] = values[i].Y <= 0 ? zeroSlot : nonZeroHeight;
        return heights;
    }

    private static double FunnelWidth(double plotWidth, double value, double max) {
        if (value <= 0) return Math.Max(8, Math.Min(14, plotWidth * 0.025));
        var ratio = max <= 0 ? 1 : Clamp(value / max, 0.04, 1);
        return Math.Max(70, plotWidth * (0.22 + ratio * 0.74));
    }

    private static double FunnelRatio(double value, double baseline) =>
        baseline <= 0 ? 0 : value / baseline;

    private static double FunnelDropOff(double value, double baseline) =>
        baseline <= 0 ? 0 : Clamp(1 - value / baseline, 0, 1);

    private static string FormatFunnelDropOffLabel(double dropOff, double baseline) =>
        baseline <= 0 ? "prev stage was 0" :
        dropOff <= 0 ? "0% from prev" : "-" + FormatPercent(dropOff) + " from prev";

    private static void DrawFunnelPointGradients(SvgMarkupWriter writer, string id, ChartSeries series) {
        var wroteDefs = false;
        for (var i = 0; i < series.PointColors.Count; i++) {
            if (!series.PointColors[i].HasValue) continue;
            if (!wroteDefs) {
                writer.StartElement("defs").EndStartElement().Line();
                wroteDefs = true;
            }

            var color = series.PointColors[i]!.Value;
            writer
                .StartElement("linearGradient")
                .Attribute("id", $"{id}-funnelPointFill{i}")
                .Attribute("x1", "0")
                .Attribute("x2", "1")
                .Attribute("y1", "0")
                .Attribute("y2", "1")
                .EndStartElement()
                .StartElement("stop")
                .Attribute("offset", "0%")
                .Attribute("stop-color", color.ToHex())
                .Attribute("stop-opacity", "1")
                .EndEmptyElement()
                .StartElement("stop")
                .Attribute("offset", "100%")
                .Attribute("stop-color", color.ToHex())
                .Attribute("stop-opacity", "0.78")
                .EndEmptyElement()
                .EndElement()
                .Line();
        }

        if (wroteDefs) writer.EndElement().Line();
    }

    private static ChartColor FunnelSegmentColor(Chart chart, ChartSeries series, int pointIndex) =>
        pointIndex < series.PointColors.Count && series.PointColors[pointIndex].HasValue
            ? series.PointColors[pointIndex]!.Value
            : series.Color ?? chart.Options.Theme.Palette[pointIndex % chart.Options.Theme.Palette.Length];

    private static string FunnelSegmentFill(Chart chart, ChartSeries series, int pointIndex, string id) {
        if (pointIndex < series.PointColors.Count && series.PointColors[pointIndex].HasValue) return $"url(#{id}-funnelPointFill{pointIndex})";
        if (series.Color.HasValue) return series.Color.Value.ToCss();
        return $"url(#{id}-sliceFill{pointIndex % chart.Options.Theme.Palette.Length})";
    }

    private static ChartColor FunnelTextColor(ChartColor background) {
        var luminance = (0.2126 * background.R + 0.7152 * background.G + 0.0722 * background.B) / 255;
        return luminance > 0.58 ? ChartColor.FromRgb(15, 23, 42) : ChartColor.White;
    }

    private static ChartColor FunnelTextHalo(ChartColor text, ChartColor cardBackground) =>
        text.R > 240 && text.G > 240 && text.B > 240 ? cardBackground : ChartColor.Transparent;
}
