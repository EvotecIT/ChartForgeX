using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawWaterfall(StringBuilder sb, Chart chart, ChartRect plot, string id) {
        var series = chart.Series.FirstOrDefault(item => item.Kind == ChartSeriesKind.Waterfall);
        if (series == null || series.Points.Count == 0) return;

        var steps = BuildWaterfallSteps(series);
        var bounds = WaterfallBounds(steps);
        var ticks = ChartTicks.Generate(bounds.MinY, bounds.MaxY, chart.Options.TickCount);
        bounds.SetYBounds(ticks[0], ticks[ticks.Count - 1]);
        var t = chart.Options.Theme;
        var slot = plot.Width / steps.Count;
        var barWidth = Math.Max(12, Math.Min(58, slot * 0.58));
        var positive = t.Positive;
        var negative = t.Negative;
        var totalColor = t.Warning;

        sb.AppendLine("<g data-cfx-role=\"waterfall-chart\">");
        DrawWaterfallGrid(sb, chart, plot, bounds, ticks);
        for (var i = 0; i < steps.Count; i++) {
            var step = steps[i];
            var centerX = plot.Left + slot * i + slot / 2;
            var y0 = WaterfallY(plot, bounds, step.Start);
            var y1 = WaterfallY(plot, bounds, step.End);
            var top = Math.Min(y0, y1);
            var height = Math.Max(2, Math.Abs(y1 - y0));
            var status = WaterfallStatus(step);
            var color = step.IsTotal ? totalColor : step.Delta >= 0 ? positive : negative;
            var summary = WaterfallLabel(chart, step) + ": " + (step.IsTotal ? FormatValue(chart, step.End) : FormatSignedValue(chart, step.Delta)) + ", " + status;
            if (i > 0) {
                var connectorY = WaterfallY(plot, bounds, step.Start);
                sb.AppendLine($"<line data-cfx-role=\"waterfall-connector\" x1=\"{F(centerX - slot + barWidth / 2)}\" y1=\"{F(connectorY)}\" x2=\"{F(centerX - barWidth / 2)}\" y2=\"{F(connectorY)}\" stroke=\"{t.Axis.ToCss()}\" stroke-width=\"1.2\" stroke-dasharray=\"4 4\" opacity=\"0.58\"/>");
            }

            sb.AppendLine($"<rect data-cfx-role=\"waterfall-bar\" data-cfx-point=\"{i}\" data-cfx-status=\"{status}\" role=\"img\" aria-label=\"{Escape(summary)}\" x=\"{F(centerX - barWidth / 2)}\" y=\"{F(top)}\" width=\"{F(barWidth)}\" height=\"{F(height)}\" rx=\"6\" fill=\"{color.ToCss()}\"/>");
            if (chart.Options.ShowDataLabels) {
                var label = step.IsTotal ? FormatValue(chart, step.End) : FormatSignedValue(chart, step.Delta);
                var labelY = step.Delta >= 0 || step.IsTotal ? top - 11 : top + height + 13;
                DrawDataLabel(sb, chart, label, centerX, labelY, plot);
            }

            DrawXAxisLabel(sb, chart, plot, WaterfallLabel(chart, step), centerX, plot.Bottom + XAxisLabelOffset(chart), Clamp(chart.Options.XAxisLabelAngle, -80, 80));
        }

        DrawLegend(sb, chart, chart.Options.Size.Width, chart.Options.Size.Height);
        sb.AppendLine("</g>");
    }

    private static void DrawWaterfallGrid(StringBuilder sb, Chart chart, ChartRect plot, ChartRange bounds, IReadOnlyList<double> ticks) {
        var t = chart.Options.Theme;
        foreach (var tick in ticks) {
            var y = WaterfallY(plot, bounds, tick);
            if (chart.Options.ShowGrid) sb.AppendLine($"<line x1=\"{F(plot.Left)}\" y1=\"{F(y)}\" x2=\"{F(plot.Right)}\" y2=\"{F(y)}\" stroke=\"{t.Grid.ToCss()}\" stroke-width=\"1\"/>");
            if (chart.Options.ShowAxes) sb.AppendLine($"<text x=\"{F(plot.Left - 12)}\" y=\"{F(y + 4)}\" text-anchor=\"end\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.TickLabelFontSize)}\">{Escape(FormatValue(chart, tick))}</text>");
        }

        var zeroY = WaterfallY(plot, bounds, 0);
        if (zeroY > plot.Top && zeroY < plot.Bottom) sb.AppendLine($"<line x1=\"{F(plot.Left)}\" y1=\"{F(zeroY)}\" x2=\"{F(plot.Right)}\" y2=\"{F(zeroY)}\" stroke=\"{t.Axis.ToCss()}\" stroke-width=\"1.4\"/>");
        if (!chart.Options.ShowAxes) return;
        sb.AppendLine($"<line x1=\"{F(plot.Left)}\" y1=\"{F(plot.Bottom)}\" x2=\"{F(plot.Right)}\" y2=\"{F(plot.Bottom)}\" stroke=\"{t.Axis.ToCss()}\" stroke-width=\"1.2\"/>");
        sb.AppendLine($"<line x1=\"{F(plot.Left)}\" y1=\"{F(plot.Top)}\" x2=\"{F(plot.Left)}\" y2=\"{F(plot.Bottom)}\" stroke=\"{t.Axis.ToCss()}\" stroke-width=\"1.2\"/>");
        if (!string.IsNullOrWhiteSpace(chart.XAxisTitle)) sb.AppendLine($"<text x=\"{F(plot.Left + plot.Width / 2)}\" y=\"{F(plot.Bottom + XAxisTitleOffset(chart))}\" text-anchor=\"middle\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.AxisTitleFontSize)}\" font-weight=\"600\">{Escape(chart.XAxisTitle)}</text>");
        if (!string.IsNullOrWhiteSpace(chart.YAxisTitle)) sb.AppendLine($"<text transform=\"translate(26 {F(plot.Top + plot.Height / 2)}) rotate(-90)\" text-anchor=\"middle\" fill=\"{t.MutedText.ToCss()}\" font-family=\"{SvgFontFamily(t.FontFamily)}\" font-size=\"{F(t.AxisTitleFontSize)}\" font-weight=\"600\">{Escape(chart.YAxisTitle)}</text>");
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

    private static string WaterfallLabel(Chart chart, WaterfallStep step) => step.IsTotal ? "Total" : FormatX(chart, step.XValue);

    private static string FormatSignedValue(Chart chart, double value) => value >= 0 ? "+" + FormatValue(chart, value) : FormatValue(chart, value);

    private static string WaterfallStatus(WaterfallStep step) => step.IsTotal ? "total" : step.Delta >= 0 ? "positive" : "negative";

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
