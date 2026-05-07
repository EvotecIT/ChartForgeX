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

        var body = new StringBuilder();
        DrawWaterfallGrid(body, chart, plot, bounds, ticks);
        var reservedLabels = new List<ChartLabelBounds>();
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
                WriteWaterfallConnector(body, centerX - slot + barWidth / 2, connectorY, centerX - barWidth / 2, connectorY, t.Axis);
            }

            WriteWaterfallBar(body, chart, step, i, status, summary, color, centerX - barWidth / 2, top, barWidth, height);
            if (ShouldDrawDataLabels(chart, series)) {
                var label = step.IsTotal ? FormatValue(chart, step.End) : FormatSignedValue(chart, step.Delta);
                var pointIndex = step.IsTotal ? -1 : i;
                var placement = DataLabelPlacement(chart, series);
                if (placement == ChartDataLabelPlacement.Left || placement == ChartDataLabelPlacement.Right || placement == ChartDataLabelPlacement.Outside) {
                    var labelX = placement == ChartDataLabelPlacement.Left ? centerX - barWidth / 2 - 8 : centerX + barWidth / 2 + 8;
                    var anchor = placement == ChartDataLabelPlacement.Left ? "end" : "start";
                    if (ReserveSvgHorizontalLabel(label, labelX, top + height / 2, anchor, chart, plot, reservedLabels)) DrawHorizontalValueLabel(body, chart, label, labelX, top + height / 2, anchor, plot, series, pointIndex);
                } else {
                    if ((placement == ChartDataLabelPlacement.Inside || placement == ChartDataLabelPlacement.Center) && height < t.DataLabelFontSize + 8) continue;
                    var labelY = placement == ChartDataLabelPlacement.Inside || placement == ChartDataLabelPlacement.Center
                        ? top + height / 2
                        : placement == ChartDataLabelPlacement.Above
                            ? top - 11
                            : placement == ChartDataLabelPlacement.Below
                                ? top + height + 13
                                : step.Delta >= 0 || step.IsTotal ? top - 11 : top + height + 13;
                    if (ReserveSvgLabel(label, centerX, labelY, chart, plot, reservedLabels)) DrawDataLabel(body, chart, label, centerX, labelY, plot, series: series, pointIndex: pointIndex);
                }
            }

            if (chart.Options.ShowAxes) DrawXAxisLabel(body, chart, plot, WaterfallLabel(chart, step), centerX, plot.Bottom + XAxisLabelOffset(chart), Clamp(chart.Options.XAxisLabelAngle, -80, 80), "waterfall-x-axis-label");
        }

        DrawLegend(body, chart, chart.Options.Size.Width, chart.Options.Size.Height);
        var writer = new SvgMarkupWriter(body.Length + 128);
        writer
            .StartElement("g")
            .Attribute("data-cfx-role", "waterfall-chart")
            .Raw(Environment.NewLine)
            .Raw(body.ToString())
            .EndElement()
            .Line();
        sb.Append(writer.Build());
    }

    private static void DrawWaterfallGrid(StringBuilder sb, Chart chart, ChartRect plot, ChartRange bounds, IReadOnlyList<double> ticks) {
        var t = chart.Options.Theme;
        foreach (var tick in ticks) {
            var y = WaterfallY(plot, bounds, tick);
            if (chart.Options.ShowGrid) WriteWaterfallGridLine(sb, plot.Left, y, plot.Right, y, t.Grid, ChartVisualPrimitives.GridStrokeWidth);
            if (chart.Options.ShowAxes) WriteWaterfallYAxisLabel(sb, chart, plot.Left - 12, y + 4, FormatValue(chart, tick));
        }

        var zeroY = WaterfallY(plot, bounds, 0);
        if (ShowAxisLines(chart) && zeroY > plot.Top && zeroY < plot.Bottom) WriteWaterfallAxisLine(sb, "waterfall-zero-axis", plot.Left, zeroY, plot.Right, zeroY, t.Axis, ChartVisualPrimitives.ZeroAxisStrokeWidth);
        if (!chart.Options.ShowAxes) return;
        if (ShowAxisLines(chart)) {
            WriteWaterfallAxisLine(sb, null, plot.Left, plot.Bottom, plot.Right, plot.Bottom, t.Axis, ChartVisualPrimitives.AxisStrokeWidth);
            WriteWaterfallAxisLine(sb, null, plot.Left, plot.Top, plot.Left, plot.Bottom, t.Axis, ChartVisualPrimitives.AxisStrokeWidth);
        }
        DrawSvgXAxisTitle(sb, chart, plot, plot.Bottom + XAxisTitleOffset(chart), "waterfall-x-axis-title");
        if (!string.IsNullOrWhiteSpace(chart.YAxisTitle)) {
            var widestTick = ticks.Max(tick => EstimateTextWidth(FormatValue(chart, tick), t.TickLabelFontSize));
            var axisX = Math.Max(24, plot.Left - widestTick - 48);
            DrawSvgYAxisTitle(sb, chart, plot, axisX, "waterfall-y-axis-title");
        }
    }

    private static void WriteWaterfallConnector(StringBuilder sb, double x1, double y1, double x2, double y2, ChartColor axisColor) {
        var writer = new SvgMarkupWriter(384);
        writer
            .StartElement("line")
            .Attribute("data-cfx-role", "waterfall-connector")
            .Attribute("x1", x1)
            .Attribute("y1", y1)
            .Attribute("x2", x2)
            .Attribute("y2", y2)
            .Attribute("stroke", axisColor.ToCss())
            .Attribute("stroke-width", ChartVisualPrimitives.WaterfallConnectorStrokeWidth)
            .Attribute("stroke-dasharray", SvgMarkupWriter.FormatNumber(ChartVisualPrimitives.WaterfallConnectorDash) + " " + SvgMarkupWriter.FormatNumber(ChartVisualPrimitives.WaterfallConnectorGap))
            .Attribute("opacity", ChartVisualPrimitives.WaterfallConnectorOpacity)
            .EndEmptyElement()
            .Line();
        sb.Append(writer.Build());
    }

    private static void WriteWaterfallBar(StringBuilder sb, Chart chart, WaterfallStep step, int pointIndex, string status, string summary, ChartColor color, double x, double y, double width, double height) {
        var writer = new SvgMarkupWriter(512);
        writer
            .StartElement("rect")
            .Attribute("data-cfx-role", "waterfall-bar")
            .Attribute("data-cfx-point", pointIndex)
            .Attribute("data-cfx-label", WaterfallLabel(chart, step))
            .Attribute("data-cfx-start", step.Start)
            .Attribute("data-cfx-end", step.End)
            .Attribute("data-cfx-delta", step.Delta)
            .Attribute("data-cfx-status", status)
            .Attribute("role", "img")
            .Attribute("aria-label", summary)
            .Attribute("x", x)
            .Attribute("y", y)
            .Attribute("width", width)
            .Attribute("height", height)
            .Attribute("rx", 6)
            .Attribute("fill", color.ToCss())
            .EndEmptyElement()
            .Line();
        sb.Append(writer.Build());
    }

    private static void WriteWaterfallGridLine(StringBuilder sb, double x1, double y1, double x2, double y2, ChartColor color, double strokeWidth) {
        var writer = new SvgMarkupWriter(256);
        writer
            .StartElement("line")
            .Attribute("x1", x1)
            .Attribute("y1", y1)
            .Attribute("x2", x2)
            .Attribute("y2", y2)
            .Attribute("stroke", color.ToCss())
            .Attribute("stroke-width", strokeWidth)
            .EndEmptyElement()
            .Line();
        sb.Append(writer.Build());
    }

    private static void WriteWaterfallYAxisLabel(StringBuilder sb, Chart chart, double x, double y, string label) {
        var t = chart.Options.Theme;
        var writer = new SvgMarkupWriter(384);
        writer
            .StartElement("text")
            .Attribute("data-cfx-role", "waterfall-y-axis-label")
            .Attribute("x", x)
            .Attribute("y", y)
            .Attribute("text-anchor", "end")
            .Attribute("fill", t.MutedText.ToCss())
            .Attribute("font-family", SvgFontFamily(t.FontFamily))
            .Attribute("font-size", t.TickLabelFontSize)
            .Text(label)
            .EndElement()
            .Line();
        sb.Append(writer.Build());
    }

    private static void WriteWaterfallAxisLine(StringBuilder sb, string? role, double x1, double y1, double x2, double y2, ChartColor color, double strokeWidth) {
        var writer = new SvgMarkupWriter(256);
        writer.StartElement("line");
        writer.Attribute("data-cfx-role", role);
        writer
            .Attribute("x1", x1)
            .Attribute("y1", y1)
            .Attribute("x2", x2)
            .Attribute("y2", y2)
            .Attribute("stroke", color.ToCss())
            .Attribute("stroke-width", strokeWidth)
            .EndEmptyElement()
            .Line();
        sb.Append(writer.Build());
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
