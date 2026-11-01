using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.Markup;

public sealed partial class MarkupChartParser {
    private static Chart BuildChart(ChartState state) {
        var chart = Chart.Create()
            .WithTitle(state.Title)
            .WithSubtitle(state.Subtitle)
            .WithSize(state.Width, state.Height);
        if (state.Labels.Count > 0) chart.WithXLabels(state.Labels.ToArray());

        if (!BuildTypedChart(chart, state)) {
            if (state.Series.Count == 0) {
                var points = BuildSeriesPoints(state.Type, state.Values);
                AddSeries(chart, state.SeriesName, state.Type, points, null, state);
            } else {
                foreach (var series in state.Series) {
                    var type = string.IsNullOrWhiteSpace(series.Type) ? state.Type : series.Type;
                    var points = BuildSeriesPoints(type, series.Values);
                    AddSeries(chart, series.Name, type, points, series.Color, state);
                }
            }
        }

        if (chart.Series.Any(series => series.Kind == ChartSeriesKind.Gauge) && chart.Series.Count != 1)
            throw new ArgumentException("Gauge charts support exactly one series.");
        if (chart.Series.Any(series => series.Kind == ChartSeriesKind.Heatmap) && chart.Series.Any(series => series.Kind != ChartSeriesKind.Heatmap))
            throw new ArgumentException("Heatmap charts cannot be mixed with other series kinds in the same chart.");
        // Markup has one default stack per kind; source-unit and percentage members cannot share it.
        foreach (var stack in chart.Series.Where(series => series.Kind is ChartSeriesKind.Bar or ChartSeriesKind.HorizontalBar or ChartSeriesKind.StackedArea).GroupBy(series => series.Kind)) {
            if (stack.Select(series => series.NormalizedTo).Distinct().Count() > 1)
                throw new ArgumentException("Every series in the same stack must use the same NormalizedTo target, including null for source units.");
        }
        ApplyChartOptions(chart, state);
        ApplyAnnotations(chart, state);
        return chart;
    }

    private static void AddSeries(Chart chart, string name, string type, IEnumerable<ChartPoint> points, string? color, ChartState state) {
        var before = chart.Series.Count;
        switch (NormalizeKey(type)) {
            case "line":
                chart.AddLine(name, points, ParseColor(color));
                break;
            case "smoothline":
                chart.AddSmoothLine(name, points, ParseColor(color));
                break;
            case "stepline":
                chart.AddStepLine(name, points, ParseColor(color));
                break;
            case "area":
                chart.AddArea(name, points, ParseColor(color));
                break;
            case "smootharea":
                chart.AddSmoothArea(name, points, ParseColor(color));
                break;
            case "steparea":
                chart.AddStepArea(name, points, ParseColor(color));
                break;
            case "percentstackedarea":
                chart.AddStackedArea(name, points, ParseColor(color));
                chart.Series[chart.Series.Count - 1].WithNormalization();
                break;
            case "smoothpercentstackedarea":
                chart.AddSmoothStackedArea(name, points, ParseColor(color));
                chart.Series[chart.Series.Count - 1].WithNormalization();
                break;
            case "stackedbar":
            case "percentstackedbar":
                chart.WithStackedBars().AddBar(name, points, ParseColor(color));
                if (NormalizeKey(type) == "percentstackedbar") chart.Series[chart.Series.Count - 1].WithNormalization();
                break;
            case "stackedhorizontalbar":
            case "percentstackedhorizontalbar":
                chart.WithStackedHorizontalBars().AddHorizontalBar(name, points, ParseColor(color));
                if (NormalizeKey(type) == "percentstackedhorizontalbar") chart.Series[chart.Series.Count - 1].WithNormalization();
                break;
            case "heatmap":
                chart.AddHeatmapRow(name, points, ParseColor(color));
                break;
            case "gauge":
            case "lineargauge":
                var values = points.ToArray();
                if (values.Length != 1) throw new ArgumentException("Gauge series must declare exactly one numeric value.");
                if (NormalizeKey(type) == "lineargauge") chart.AddLinearGauge(name, values[0].Y, state.GaugeMinimum, state.GaugeMaximum, ParseColor(color));
                else chart.AddGauge(name, values[0].Y, state.GaugeMinimum, state.GaugeMaximum, ParseColor(color));
                break;
            case "stackedarea":
                chart.AddStackedArea(name, points, ParseColor(color));
                break;
            case "smoothstackedarea":
                chart.AddSmoothStackedArea(name, points, ParseColor(color));
                break;
            case "scatter":
                chart.AddScatter(name, points, ParseColor(color));
                break;
            case "lollipop":
                chart.AddLollipop(name, points, ParseColor(color));
                break;
            case "dumbbell":
                AddDumbbellMarkupSeries(chart, name, points, color);
                break;
            case "radar":
                chart.AddRadar(name, points, ParseColor(color));
                break;
            case "funnel":
                chart.AddFunnel(name, points, ParseColor(color));
                break;
            case "pyramid":
                chart.AddPyramid(name, points, ParseColor(color));
                break;
            case "polararea":
                chart.AddPolarArea(name, points);
                break;
            case "polar":
                chart.AddPolar(name, points, ParseColor(color));
                break;
            case "donut":
                chart.WithPointLegend().WithDataLabels().WithPieSliceLabelContent(ChartPieSliceLabelContent.LabelAndPercent).AddDonut(name, points);
                break;
            case "pie":
                chart.WithPointLegend().WithDataLabels().WithPieSliceLabelContent(ChartPieSliceLabelContent.LabelAndPercent).AddPie(name, points);
                break;
            case "horizontalbar":
            case "hbar":
                chart.AddHorizontalBar(name, points, ParseColor(color));
                break;
            case "waterfall":
                chart.AddWaterfall(name, points, ParseColor(color));
                break;
            case "bar":
            case "column":
                chart.AddBar(name, points, ParseColor(color));
                break;
            default:
                throw new ArgumentException("Unknown chart type '" + type + "'.");
        }

        if (!string.IsNullOrWhiteSpace(color) && chart.Series.Count > before && chart.Series[chart.Series.Count - 1].Color == null) {
            chart.Series[chart.Series.Count - 1].Color = ParseColor(color);
        }
    }

}
