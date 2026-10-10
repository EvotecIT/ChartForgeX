using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    internal static bool Supports(ChartSeriesKind kind) {
        switch (kind) {
            case ChartSeriesKind.Line: case ChartSeriesKind.StepLine: case ChartSeriesKind.Area:
            case ChartSeriesKind.StepArea: case ChartSeriesKind.StackedArea: case ChartSeriesKind.Bar:
            case ChartSeriesKind.Scatter: case ChartSeriesKind.Bubble: case ChartSeriesKind.ErrorBar:
            case ChartSeriesKind.Candlestick: case ChartSeriesKind.RangeBand: case ChartSeriesKind.RangeArea:
            case ChartSeriesKind.Lollipop: case ChartSeriesKind.Dumbbell: case ChartSeriesKind.RangeBar:
            case ChartSeriesKind.BoxPlot: case ChartSeriesKind.HorizontalBar: case ChartSeriesKind.Waterfall:
            case ChartSeriesKind.Slope: case ChartSeriesKind.Ohlc: case ChartSeriesKind.TrendLine: return true;
            default: return false;
        }
    }

    private static bool Horizontal(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.HorizontalBar);

    private static int ObservationStride(ChartSeriesKind kind) {
        switch (kind) {
            case ChartSeriesKind.Bubble: case ChartSeriesKind.RangeBand: case ChartSeriesKind.RangeArea:
            case ChartSeriesKind.Dumbbell: case ChartSeriesKind.RangeBar: return 2;
            case ChartSeriesKind.ErrorBar: return 3;
            case ChartSeriesKind.Candlestick: case ChartSeriesKind.Ohlc: return 4;
            case ChartSeriesKind.BoxPlot: return 5;
            default: return 1;
        }
    }

    private static void DrawExtensionSeries(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot,
        ChartMapper map, ChartStackLayout stacks, int index, VisualThemeColors colors, List<LabelPlacementRequest> labels, List<LabelObstacle> obstacles) {
        var series = chart.Series[index];
        switch (series.Kind) {
            case ChartSeriesKind.RangeBand: case ChartSeriesKind.RangeArea:
                DrawRangeEnvelope(chart, context, builder, plot, map, index, colors, labels, obstacles); break;
            case ChartSeriesKind.Candlestick: case ChartSeriesKind.Ohlc: case ChartSeriesKind.BoxPlot:
                DrawFinancial(chart, context, builder, plot, map, index, colors, labels, obstacles); break;
            case ChartSeriesKind.RangeBar:
                DrawRangeBars(chart, context, builder, plot, map, index, colors, labels, obstacles); break;
            case ChartSeriesKind.HorizontalBar:
                DrawPreparedHorizontalBars(chart, context, builder, plot, map, stacks, index, colors, labels, obstacles); break;
            case ChartSeriesKind.Waterfall:
                DrawPreparedWaterfall(chart, context, builder, plot, map, index, colors, labels, obstacles); break;
            default: DrawExtensionPoints(chart, context, builder, plot, map, index, colors, labels, obstacles); break;
        }
    }

    // Point customizations are indexed by observation, rather than by an encoded tuple member.
    private static ResolvedPointLabel ResolveObservationLabel(Chart chart, VisualRenderContext context, ChartSeries series,
        int observation, VisualThemeColors colors, Func<string> fallback) {
        var style = SeriesLabelStyle(chart, context, series, colors);
        if (observation < series.PointDataLabelStyles.Count && series.PointDataLabelStyles[observation] != null)
            style = series.PointDataLabelStyles[observation]!.Resolve(style);
        var text = observation < series.PointLabels.Count && series.PointLabels[observation] != null ? series.PointLabels[observation]! : fallback();
        return new ResolvedPointLabel(text, TextCaseTransformer.Apply(text, style.TextCase, CultureInfo.InvariantCulture), style);
    }

    private static IDisposable ObservationGroup(VisualSceneBuilder builder, ChartSeries series, int seriesIndex, int observation,
        int rawStart, int rawCount, ChartRect bounds, ResolvedPointLabel label, params (string Name, double Value)[] values) =>
        ObservationGroup(builder, series, seriesIndex, observation, rawStart, rawCount, bounds, label, null, values);

    private static IDisposable ObservationGroup(VisualSceneBuilder builder, ChartSeries series, int seriesIndex, int observation,
        int rawStart, int rawCount, ChartRect bounds, ResolvedPointLabel label, ChartStackPoint? stack, params (string Name, double Value)[] values) {
        var id = PointId(seriesIndex, observation);
        var description = series.Name + ": " + label.DisplayedText;
        var metadata = new Dictionary<string, string> {
            ["data-cfx-series"] = Number(seriesIndex), ["data-cfx-point"] = Number(observation),
            ["data-cfx-x"] = Number(series.Points[rawStart].X), ["data-cfx-y"] = Number(series.Points[rawStart].Y),
            ["data-cfx-label"] = label.DisplayedText, ["data-cfx-axis"] = series.YAxis.ToString().ToLowerInvariant(),
            ["data-cfx-fill-pattern"] = ObservationPattern(series, observation).ToString(),
            ["data-cfx-state"] = series.StateRole.ToString().ToLowerInvariant(), ["data-cfx-source-count"] = Number(rawCount)
        };
        for (var member = 0; member < rawCount; member++) {
            var raw = rawStart + member;
            var point = series.Points[raw];
            var prefix = "data-cfx-source-" + Number(member);
            metadata[prefix + "-index"] = Number(series.TrendLineSourcePoints.Count > 0 || series.BoxPlotSourceSamples.Count > 0 ? -1 : raw < series.SourcePointIndices.Count ? series.SourcePointIndices[raw] : raw);
            metadata[prefix + "-x"] = Number(point.X); metadata[prefix + "-y"] = Number(point.Y);
            metadata[prefix + "-break"] = point.BreakBefore ? "true" : "false";
        }
        if (series.TrendLineSourcePoints.Count > 0) metadata["data-cfx-derived"] = "regression-endpoint";
        if (series.BoxPlotSourceSamples.Count > 0) metadata["data-cfx-derived"] = "five-number-summary";
        foreach (var value in values) {
            metadata["data-cfx-" + value.Name] = Number(value.Value);
            description += " " + value.Name + "=" + Number(value.Value);
        }
        if (stack.HasValue) ChartStackLayout.AddMetadata(metadata, stack.Value);
        metadata["aria-label"] = description;
        builder.AddRegion(new VisualSemanticRegion(id, "point", bounds, description));
        return builder.PushGroup(id, "point", metadata);
    }

    private static ChartFillPattern ObservationPattern(ChartSeries series, int observation) =>
        observation < series.PointFillPatterns.Count && series.PointFillPatterns[observation].HasValue
            ? series.PointFillPatterns[observation]!.Value : series.FillPattern;

    private static double SeriesStroke(ChartSeries series, VisualRenderContext context) =>
        series.HasExplicitStrokeWidth ? series.StrokeWidth : context.Theme.SeriesStrokeWidth;

    private static void DrawLayeredPath(Chart chart, VisualSceneBuilder builder, ChartPath path, ChartColor color, SvgPaint sourcePaint, double width, string role, double[]? dash = null) {
        foreach (var layer in ChartLineVisualLayers.Build(color, width, chart.Options.ResolvePreparedLineVisualStyle()))
            if (layer.IsVisible) builder.Path(path, stroke: layer.ColorWithOpacity(), strokeWidth: layer.StrokeWidth, role: role + layer.RoleSuffix, dash: dash,
                paint: VisualChartPaint.Stroke(layer.IsHighlight ? SvgPaint.Literal(layer.ColorWithOpacity()) : sourcePaint.WithOpacity(layer.ColorWithOpacity(), layer.Opacity)));
    }

    private static void ObservationLabel(Chart chart, VisualRenderContext context, ChartSeries series, int seriesIndex, int observation,
        ChartPoint anchor, ChartRect bounds, double value, ResolvedPointLabel label, List<LabelPlacementRequest> labels, List<LabelObstacle> obstacles, double? barDirection = null) {
        obstacles.Add(new LabelObstacle(PointId(seriesIndex, observation), bounds));
        AddLabel(chart, context, series, seriesIndex, observation, anchor, bounds, label, labels, value, barDirection: barDirection);
    }

    private static ChartRect Extents(double x1, double y1, double x2, double y2, double inflate = 0) =>
        new(Math.Min(x1, x2) - inflate, Math.Min(y1, y2) - inflate, Math.Abs(x2 - x1) + inflate * 2, Math.Abs(y2 - y1) + inflate * 2);

    private static string Value(Chart chart, double value) => ChartNumericFormatter.FormatValue(chart.Options, value);

    private static bool HasSeriesPaint(ChartSeries series, int observation) => series.Color.HasValue || series.StateRole != ChartSeriesState.None
        || observation >= 0 && observation < series.PointColors.Count && series.PointColors[observation].HasValue;

    private static SvgColorRole SemanticMarkPaintRole(ChartSeries series, int observation) =>
        HasSeriesPaint(series, observation) ? VisualChartPaint.SeriesRole(series, observation) : SvgColorRole.Status;

    private static ChartColor FinancialColor(ChartSeries series, int seriesIndex, int observation, bool rising, VisualThemeColors colors) {
        if (HasSeriesPaint(series, observation))
            return observation >= 0 ? PointColor(series, seriesIndex, observation, colors) : Color(series, seriesIndex, colors);
        return rising ? colors.Status.Pass.Fill : colors.Status.Critical.Fill;
    }
}
