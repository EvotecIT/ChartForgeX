using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawFunnel(RgbaCanvas c, Chart chart, ChartRect basePlot) {
        ChartSeries? series = null;
        foreach (var candidate in chart.Series) {
            if (candidate.Kind == ChartSeriesKind.Funnel) {
                series = candidate;
                break;
            }
        }

        if (series == null) return;
        var values = new List<ChartPoint>();
        foreach (var point in series.Points) values.Add(point);
        if (values.Count == 0) return;

        var max = 0.0;
        foreach (var point in values) max = Math.Max(max, point.Y);
        var showLabels = series.ShowDataLabels != false;
        var metricsReserve = showLabels && values.Count > 1 ? Math.Min(168, Math.Max(126, basePlot.Width * 0.22)) : 0;
        var innerLeft = basePlot.Left + 44;
        var innerRight = basePlot.Right - 44 - metricsReserve;
        var plot = new ChartRect(innerLeft, basePlot.Top + 18, Math.Max(120, innerRight - innerLeft), Math.Max(90, basePlot.Height - 62));
        var metricsX = Math.Min(basePlot.Right - metricsReserve + 10, plot.Right + 18);
        var gap = Math.Min(10, Math.Max(4, plot.Height / values.Count * 0.08));
        var segmentHeights = FunnelSegmentHeights(values, plot.Height, gap);
        var y = plot.Top;

        for (var i = 0; i < values.Count; i++) {
            var segmentHeight = segmentHeights[i];
            var segmentY = y;
            var segmentDrawHeight = segmentHeight;
            if (values[i].Y <= 0) {
                segmentDrawHeight = Math.Max(8, Math.Min(16, segmentHeight * 0.24));
                segmentY = y + (segmentHeight - segmentDrawHeight) / 2;
            }
            var topWidth = FunnelWidth(plot.Width, values[i].Y, max);
            var nextValue = i + 1 < values.Count ? values[i + 1].Y : values[i].Y * 0.82;
            var bottomWidth = values[i].Y <= 0 ? topWidth : FunnelWidth(plot.Width, nextValue, max);
            var topLeft = plot.Left + (plot.Width - topWidth) / 2;
            var topRight = topLeft + topWidth;
            var bottomLeft = plot.Left + (plot.Width - bottomWidth) / 2;
            var bottomRight = bottomLeft + bottomWidth;
            var color = PngFunnelSegmentColor(chart, series, i);
            var segment = new[] {
                new ChartPoint(topLeft, segmentY),
                new ChartPoint(topRight, segmentY),
                new ChartPoint(bottomRight, segmentY + segmentDrawHeight),
                new ChartPoint(bottomLeft, segmentY + segmentDrawHeight)
            };
            c.FillPolygonVerticalGradient(segment, ChartMarkSurface.FunnelSegmentGradientTop(color), ChartMarkSurface.FunnelSegmentGradientBottom(color));
            DrawFunnelSegmentStroke(c, chart, segment);

            var label = FormatX(chart, values[i].X);
            var value = FormatValue(chart, values[i].Y);
            var centerX = plot.Left + plot.Width / 2;
            var centerY = segmentY + segmentDrawHeight / 2;
            var labelColor = ChartColorMath.TextOnBackground(color, 0.58);
            var labelWidth = FunnelLabelWidth(topWidth, bottomWidth);
            var labelFontSize = TextFontSizeForEmphasizedWidth(label, labelWidth, FunnelLabelFontSize(chart.Options.Theme.LegendFontSize));
            var valueFontSize = TextFontSizeForEmphasizedWidth(value, labelWidth, FunnelValueFontSize(chart.Options.Theme.DataLabelFontSize));
            var labelY = centerY - labelFontSize - 2;
            var valueY = centerY + 4;
            var halo = FunnelTextHalo(labelColor, chart.Options.Theme.CardBackground);
            if (showLabels) {
                if (values[i].Y <= 0) {
                    var zeroLabelX = topRight + 12;
                    var zeroLabelWidth = Math.Max(44, Math.Min(metricsX - zeroLabelX - 12, plot.Right - zeroLabelX));
                    var zeroLabelFontSize = TextFontSizeForEmphasizedWidth(label, zeroLabelWidth, FunnelLabelFontSize(chart.Options.Theme.LegendFontSize));
                    var zeroValueFontSize = TextFontSizeForEmphasizedWidth(value, zeroLabelWidth, FunnelValueFontSize(chart.Options.Theme.DataLabelFontSize));
                    var zeroStackHeight = zeroLabelFontSize + zeroValueFontSize + 18;
                    var zeroBounds = new ChartRect(zeroLabelX, centerY - zeroStackHeight / 2.0, zeroLabelWidth, zeroStackHeight);
                    DrawReadablePngLabel(c, zeroBounds, zeroBounds.Left, zeroBounds.Top, label, chart.Options.Theme.Text, ReadableLabelHalo(chart), zeroLabelFontSize);
                    DrawReadablePngLabel(c, zeroBounds, zeroBounds.Left, zeroBounds.Top + zeroLabelFontSize + 12, value, chart.Options.Theme.MutedText, ReadableLabelHalo(chart), zeroValueFontSize);
                } else {
                    DrawReadablePngLabel(c, centerX - EstimatePngEmphasizedTextWidth(label, labelFontSize) / 2.0, labelY, label, labelColor, halo, labelFontSize);
                    DrawReadablePngLabel(c, centerX - EstimatePngEmphasizedTextWidth(value, valueFontSize) / 2.0, valueY, value, labelColor, halo, valueFontSize);
                }
            }
            if (showLabels && i > 0) {
                var retention = FunnelRatio(values[i].Y, values[0].Y);
                var dropOff = FunnelDropOff(values[i].Y, values[i - 1].Y);
                var guideX = Math.Min(metricsX - 10, bottomRight + 8);
                var retentionLabel = FormatPercent(retention) + " retained";
                var dropOffLabel = FormatFunnelDropOffLabel(dropOff, values[i - 1].Y);
                var dropOffColor = values[i - 1].Y <= 0 ? chart.Options.Theme.MutedText : chart.Options.Theme.Negative;
                var metricMaxWidth = Math.Max(32, basePlot.Right - metricsX - 6);
                var metricFontSize = Math.Min(
                    TextFontSizeForEmphasizedWidth(retentionLabel, metricMaxWidth, PngTickFontSize(chart)),
                    TextFontSizeForEmphasizedWidth(dropOffLabel, metricMaxWidth, PngTickFontSize(chart)));
                retentionLabel = TrimReadablePngLabelToWidth(retentionLabel, metricFontSize, metricMaxWidth);
                dropOffLabel = TrimReadablePngLabelToWidth(dropOffLabel, metricFontSize, metricMaxWidth);
                c.DrawDashedLine(guideX, segmentY - gap * 0.35, guideX, segmentY + segmentDrawHeight * 0.55, ApplyOpacity(chart.Options.Theme.Axis, ChartVisualPrimitives.FunnelDropoffLineOpacity), ChartVisualPrimitives.FunnelDropoffLineStrokeWidth, 3, 4);
                if (retentionLabel.Length > 0) c.DrawTextEmphasized(metricsX, centerY - metricFontSize - 4, retentionLabel, chart.Options.Theme.MutedText, metricFontSize);
                if (dropOffLabel.Length > 0) c.DrawTextEmphasized(metricsX, centerY + 4, dropOffLabel, dropOffColor, metricFontSize);
            }

            y += segmentHeight + gap;
        }
    }

    private static bool IsFunnelChart(Chart chart) => ChartSeriesKindTraits.ContainsKind(chart, ChartSeriesKind.Funnel);

    private static double[] FunnelSegmentHeights(IReadOnlyList<ChartPoint> values, double plotHeight, double gap) {
        var heights = new double[values.Count];
        if (values.Count == 0) return heights;
        var totalGap = gap * Math.Max(0, values.Count - 1);
        var drawableHeight = Math.Max(18 * values.Count, plotHeight - totalGap);
        var zeroCount = 0;
        foreach (var point in values) {
            if (point.Y <= 0) zeroCount++;
        }

        if (zeroCount == 0) {
            var evenHeight = Math.Max(18, drawableHeight / values.Count);
            for (var i = 0; i < heights.Length; i++) heights[i] = evenHeight;
            return heights;
        }

        var nonZeroCount = values.Count - zeroCount;
        var zeroSlot = Math.Min(52, Math.Max(30, drawableHeight * 0.11));
        if (nonZeroCount == 0) {
            var evenZeroHeight = Math.Max(18, drawableHeight / values.Count);
            for (var i = 0; i < heights.Length; i++) heights[i] = evenZeroHeight;
            return heights;
        }

        var nonZeroHeight = Math.Max(42, (drawableHeight - zeroSlot * zeroCount) / nonZeroCount);
        for (var i = 0; i < values.Count; i++) heights[i] = values[i].Y <= 0 ? zeroSlot : nonZeroHeight;
        return heights;
    }

    private static double FunnelWidth(double plotWidth, double value, double max) {
        if (value <= 0) return Math.Max(8, Math.Min(14, plotWidth * 0.025));
        var ratio = max <= 0 ? 1 : Clamp(value / max, 0.04, 1);
        return Math.Max(70, plotWidth * (0.22 + ratio * 0.74));
    }

    private static double FunnelLabelWidth(double topWidth, double bottomWidth) =>
        Math.Max(64, (topWidth + bottomWidth) / 2.0 - 28);

    private static double FunnelLabelFontSize(double preferred) =>
        Math.Max(ChartVisualPrimitives.FunnelLabelFontSizeMinimum, preferred);

    private static double FunnelValueFontSize(double preferred) =>
        Math.Max(ChartVisualPrimitives.FunnelValueFontSizeMinimum, preferred);

    private static double FunnelRatio(double value, double baseline) =>
        baseline <= 0 ? 0 : value / baseline;

    private static double FunnelDropOff(double value, double baseline) =>
        baseline <= 0 ? 0 : Clamp(1 - value / baseline, 0, 1);

    private static string FormatFunnelDropOffLabel(double dropOff, double baseline) =>
        baseline <= 0 ? "prev stage was 0" :
        dropOff <= 0 ? "0% from prev" : "-" + FormatPercent(dropOff) + " from prev";

    private static ChartColor PngFunnelSegmentColor(Chart chart, ChartSeries series, int pointIndex) =>
        pointIndex < series.PointColors.Count && series.PointColors[pointIndex].HasValue
            ? series.PointColors[pointIndex]!.Value
            : series.Color ?? chart.Options.Theme.Palette[pointIndex % chart.Options.Theme.Palette.Length];

    private static void DrawFunnelSegmentStroke(RgbaCanvas c, Chart chart, IReadOnlyList<ChartPoint> segment) {
        var border = ApplyOpacity(chart.Options.Theme.CardBackground, ChartVisualPrimitives.FunnelSegmentStrokeOpacity);
        var highlight = ApplyOpacity(ChartColor.White, chart.Options.Theme.Background.R < 80 ? ChartVisualPrimitives.FunnelHighlightOpacityDark : ChartVisualPrimitives.FunnelHighlightOpacityLight);
        for (var i = 0; i < segment.Count; i++) {
            var next = segment[(i + 1) % segment.Count];
            c.DrawLine(segment[i].X, segment[i].Y, next.X, next.Y, border, ChartVisualPrimitives.FunnelSegmentStrokeWidth);
        }

        c.DrawLine(segment[0].X + 2, segment[0].Y + 1, segment[1].X - 2, segment[1].Y + 1, highlight, ChartVisualPrimitives.GridStrokeWidth);
    }

    private static ChartColor FunnelTextHalo(ChartColor text, ChartColor cardBackground) =>
        text.R > 240 && text.G > 240 && text.B > 240 ? ChartColor.FromRgba(15, 23, 42, 150) : ChartColor.Transparent;
}
