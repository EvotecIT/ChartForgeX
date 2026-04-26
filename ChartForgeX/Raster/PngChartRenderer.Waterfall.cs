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
        var slot = plot.Width / steps.Count;
        var barWidth = Math.Max(8, Math.Min(46, slot * 0.58));
        var positive = chart.Options.Theme.Positive;
        var negative = chart.Options.Theme.Negative;
        var total = chart.Options.Theme.Warning;

        DrawWaterfallGrid(c, chart, plot, bounds, ticks);
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
                c.DrawLine(centerX - slot + barWidth / 2, connectorY, centerX - barWidth / 2, connectorY, chart.Options.Theme.Axis, 1);
            }

            c.FillRect(centerX - barWidth / 2, top, barWidth, height, color);
            if (chart.Options.ShowDataLabels) {
                var label = step.IsTotal ? FormatValue(chart, step.End) : FormatSignedValue(chart, step.Delta);
                var labelY = step.Delta >= 0 || step.IsTotal ? top - 12 : top + height + 6;
                c.DrawTextTiny(centerX - EstimateTinyTextWidth(label) / 2.0, Clamp(labelY, plot.Top + 2, plot.Bottom - 10), label, chart.Options.Theme.Text, 1);
            }

            var categoryLabel = step.IsTotal ? "Total" : FormatX(chart, step.XValue);
            c.DrawTextTiny(centerX - EstimateTinyTextWidth(categoryLabel) / 2.0, plot.Bottom + 10, categoryLabel, chart.Options.Theme.MutedText, 1);
        }
    }

    private static void DrawWaterfallGrid(RgbaCanvas c, Chart chart, ChartRect plot, ChartRange bounds, IReadOnlyList<double> ticks) {
        foreach (var tick in ticks) {
            var y = WaterfallY(plot, bounds, tick);
            if (chart.Options.ShowGrid) c.DrawLine(plot.Left, y, plot.Right, y, chart.Options.Theme.Grid, 1);
            if (chart.Options.ShowAxes) c.DrawTextTiny(12, y - 5, FormatValue(chart, tick), chart.Options.Theme.MutedText, 1);
        }

        var zeroY = WaterfallY(plot, bounds, 0);
        if (zeroY > plot.Top && zeroY < plot.Bottom) c.DrawLine(plot.Left, zeroY, plot.Right, zeroY, chart.Options.Theme.Axis, 1);
        if (!chart.Options.ShowAxes) return;
        c.DrawLine(plot.Left, plot.Bottom, plot.Right, plot.Bottom, chart.Options.Theme.Axis, 1);
        c.DrawLine(plot.Left, plot.Top, plot.Left, plot.Bottom, chart.Options.Theme.Axis, 1);
    }

    private static bool IsWaterfallChart(Chart chart) {
        foreach (var series in chart.Series) if (series.Kind == ChartSeriesKind.Waterfall) return true;
        return false;
    }

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
