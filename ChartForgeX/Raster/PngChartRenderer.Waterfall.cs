using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawWaterfall(RgbaCanvas c, Chart chart, ChartRect plot) {
        ChartSeries? series = null;
        foreach (var candidate in chart.Series) {
            if (candidate.Kind == ChartSeriesKind.Waterfall) {
                series = candidate;
                break;
            }
        }

        if (series == null || series.Points.Count == 0) return;
        var steps = BuildWaterfallSteps(series);
        var bounds = WaterfallBounds(steps);
        var ticks = ChartTicks.Generate(bounds.MinY, bounds.MaxY, chart.Options.TickCount);
        bounds.SetYBounds(ticks[0], ticks[ticks.Count - 1]);
        var tickFontSize = PngTickFontSize(chart);
        var bottomReserve = chart.Options.ShowAxes ? (string.IsNullOrWhiteSpace(chart.XAxisTitle) ? 32.0 : 60.0) : 0.0;
        if (chart.Options.ShowLegend && chart.Series.Count > 0) bottomReserve += 18 + PngLegendRowCount(chart) * (PngLegendFontSize(chart) + 6);
        plot = new ChartRect(plot.X, plot.Y, plot.Width, Math.Max(1, plot.Height - bottomReserve));
        var slot = plot.Width / steps.Count;
        var barWidth = Math.Max(8, Math.Min(46, slot * 0.58));
        var positive = chart.Options.Theme.Positive;
        var negative = chart.Options.Theme.Negative;
        var total = chart.Options.Theme.Warning;
        var reservedLabels = new List<ChartLabelBounds>();

        DrawWaterfallGrid(c, chart, plot, bounds, ticks, tickFontSize);
        for (var i = 0; i < steps.Count; i++) {
            var step = steps[i];
            var centerX = plot.Left + slot * i + slot / 2;
            var y0 = WaterfallY(plot, bounds, step.Start);
            var y1 = WaterfallY(plot, bounds, step.End);
            var top = Math.Min(y0, y1);
            var height = Math.Max(2, Math.Abs(y1 - y0));
            var color = step.IsTotal ? total : step.Delta >= 0 ? positive : negative;
            if (i > 0) {
                var connectorY = WaterfallY(plot, bounds, step.Start);
                c.DrawDashedLine(centerX - slot + barWidth / 2, connectorY, centerX - barWidth / 2, connectorY, ApplyOpacity(chart.Options.Theme.Axis, ChartVisualPrimitives.WaterfallConnectorOpacity), ChartVisualPrimitives.WaterfallConnectorStrokeWidth, ChartVisualPrimitives.WaterfallConnectorDash, ChartVisualPrimitives.WaterfallConnectorGap);
            }

                DrawGradientBar(c, centerX - barWidth / 2, top, barWidth, height, Math.Min(6, barWidth / 4), color);
                if (ShouldDrawDataLabels(chart, series)) {
                    var label = step.IsTotal ? FormatValue(chart, step.End) : FormatSignedValue(chart, step.Delta);
                    var pointIndex = step.IsTotal ? -1 : i;
                    var placement = DataLabelPlacement(chart, series);
                    var dataStyle = DataLabelStyle(chart, series, pointIndex);
                    var dataFontSize = PngDataLabelFontSize(chart, series, pointIndex);
                    if ((placement == ChartDataLabelPlacement.Inside || placement == ChartDataLabelPlacement.Center) && height < dataFontSize + 8) continue;
                    var labelWidth = EstimatePngEmphasizedTextWidth(label, dataFontSize);
                    var labelX = placement == ChartDataLabelPlacement.Left
                        ? centerX - barWidth / 2 - labelWidth - 8
                        : placement == ChartDataLabelPlacement.Right || placement == ChartDataLabelPlacement.Outside
                            ? centerX + barWidth / 2 + 8
                            : centerX - labelWidth / 2.0;
                    var labelY = placement == ChartDataLabelPlacement.Inside || placement == ChartDataLabelPlacement.Center
                        ? top + height / 2 - dataFontSize / 2.0
                        : placement == ChartDataLabelPlacement.Above
                            ? top - 11 - dataFontSize
                            : placement == ChartDataLabelPlacement.Below
                                ? top + height + 13 - dataFontSize
                                : step.Delta >= 0 || step.IsTotal ? top - 11 - dataFontSize : top + height + 13 - dataFontSize;
                    if (!ReservePngLabel(label, labelX, labelY, chart, plot, dataFontSize, reservedLabels)) continue;
                    DrawReadablePngLabel(c, plot, labelX, labelY, label, chart.Options.Theme.Text, ReadableLabelHalo(chart), dataFontSize, dataStyle);
            }

            if (chart.Options.ShowAxes) {
                var categoryLabel = step.IsTotal ? "Total" : FormatX(chart, step.XValue);
                c.DrawText(EdgeAwarePngLabelX(categoryLabel, centerX, plot, tickFontSize), plot.Bottom + PngXAxisLabelOffset(chart) - tickFontSize + 1, categoryLabel, chart.Options.Theme.MutedText, tickFontSize);
            }
        }

        if (chart.Options.ShowAxes) DrawDetailAxisTitles(c, chart, plot, DetailTextScale(chart));
        DrawLegend(c, chart);
    }

    private static void DrawWaterfallGrid(RgbaCanvas c, Chart chart, ChartRect plot, ChartRange bounds, IReadOnlyList<double> ticks, double fontSize) {
        foreach (var tick in ticks) {
            var y = WaterfallY(plot, bounds, tick);
            if (chart.Options.ShowGrid) c.DrawLine(plot.Left, y, plot.Right, y, chart.Options.Theme.Grid, ChartVisualPrimitives.GridStrokeWidth);
            if (chart.Options.ShowAxes) {
                var label = FormatValue(chart, tick);
                c.DrawText(Math.Max(2, plot.Left - EstimatePngTextWidth(label, fontSize) - 8), y - fontSize / 2.0, label, chart.Options.Theme.MutedText, fontSize);
            }
        }

        var zeroY = WaterfallY(plot, bounds, 0);
        if (ShowAxisLines(chart) && zeroY > plot.Top && zeroY < plot.Bottom) c.DrawLine(plot.Left, zeroY, plot.Right, zeroY, chart.Options.Theme.Axis, ChartVisualPrimitives.ZeroAxisStrokeWidth);
        if (!chart.Options.ShowAxes) return;
        if (ShowAxisLines(chart)) {
            c.DrawLine(plot.Left, plot.Bottom, plot.Right, plot.Bottom, chart.Options.Theme.Axis, ChartVisualPrimitives.AxisStrokeWidth);
            c.DrawLine(plot.Left, plot.Top, plot.Left, plot.Bottom, chart.Options.Theme.Axis, ChartVisualPrimitives.AxisStrokeWidth);
        }
    }

    private static bool IsWaterfallChart(Chart chart) => ChartSeriesKindTraits.ContainsKind(chart, ChartSeriesKind.Waterfall);

    private static List<WaterfallStep> BuildWaterfallSteps(ChartSeries series) {
        var steps = new List<WaterfallStep>(series.Points.Count + 1);
        var cumulative = 0.0;
        for (var i = 0; i < series.Points.Count; i++) {
            var start = cumulative;
            cumulative += series.Points[i].Y;
            steps.Add(new WaterfallStep(i, series.Points[i].X, series.Points[i].Y, start, cumulative, false));
        }

        steps.Add(new WaterfallStep(series.Points.Count, series.Points.Count + 1, cumulative, 0, cumulative, true));
        return steps;
    }

    private static ChartRange WaterfallBounds(IReadOnlyList<WaterfallStep> steps) {
        var bounds = new ChartRange();
        foreach (var step in steps) {
            bounds.Include(new ChartPoint(step.Index, step.Start));
            bounds.Include(new ChartPoint(step.Index, step.End));
        }

        return bounds;
    }

    private static string FormatSignedValue(Chart chart, double value) => value >= 0 ? "+" + FormatValue(chart, value) : FormatValue(chart, value);

    private static double WaterfallY(ChartRect plot, ChartRange bounds, double value) {
        var span = bounds.MaxY - bounds.MinY;
        if (Math.Abs(span) < 0.000001) return plot.Top + plot.Height / 2;
        return plot.Bottom - (value - bounds.MinY) / span * plot.Height;
    }

    private readonly struct WaterfallStep {
        public WaterfallStep(int index, double xValue, double delta, double start, double end, bool isTotal) {
            Index = index;
            XValue = xValue;
            Delta = delta;
            Start = start;
            End = end;
            IsTotal = isTotal;
        }

        public int Index { get; }

        public double XValue { get; }

        public double Delta { get; }

        public double Start { get; }

        public double End { get; }

        public bool IsTotal { get; }
    }
}
