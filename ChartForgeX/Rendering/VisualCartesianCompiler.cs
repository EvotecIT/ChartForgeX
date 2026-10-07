using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

/// <summary>Compiles Cartesian geometry, styles and semantic alternatives into shared immutable scene commands.</summary>
internal static partial class VisualCartesianCompiler {
    internal static IReadOnlyList<VisualLegendEntry> LegendEntries(Chart chart, VisualThemeColors colors) {
        var entries = new List<VisualLegendEntry>();
        if (chart.Options.ShowPointLegend && chart.Series.Count == 1 && chart.Series[0].ShowInLegend
            && ChartSeriesKindTraits.SupportsPointLegend(chart.Series[0].Kind) && chart.Series[0].Points.Count > 1) {
            var series = chart.Series[0];
            var stride = ObservationStride(series.Kind);
            for (var point = 0; point < series.Points.Count / stride; point++) {
                var label = ChartAxisValueFormatter.FindExplicitLabel(chart.Options.XAxis.Labels, series.Points[point * stride].X) ?? "Item " + Number(point + 1);
                var pattern = point < series.PointFillPatterns.Count && series.PointFillPatterns[point].HasValue ? series.PointFillPatterns[point]!.Value : series.FillPattern;
                var color = PointColor(series, 0, point, colors);
                if (series.Kind == ChartSeriesKind.Candlestick || series.Kind == ChartSeriesKind.Ohlc)
                    color = FinancialColor(series, 0, point, series.Points[point * stride + 3].Y >= series.Points[point * stride].Y, colors);
                else if (series.Kind == ChartSeriesKind.Waterfall && !series.Color.HasValue && series.StateRole == ChartSeriesState.None
                    && !(point < series.PointColors.Count && series.PointColors[point].HasValue))
                    color = series.Points[point].Y >= 0 ? colors.Status.Pass.Fill : colors.Status.Critical.Fill;
                entries.Add(new VisualLegendEntry(label, color, PointId(0, point), series.Kind, pattern, series.StateRole, series.InteractionIdentityKey, paint: VisualChartPaint.Series(series, color, point)));
            }
            return entries;
        }
        for (var index = 0; index < chart.Series.Count; index++) {
            var series = chart.Series[index];
            if (series.ShowInLegend) entries.Add(new VisualLegendEntry(series.Name, Color(series, index, colors), SeriesId(index),
                series.Kind, series.FillPattern, series.StateRole, series.InteractionIdentityKey, paint: VisualChartPaint.Series(series, Color(series, index, colors))));
        }
        return entries;
    }

    internal static void Build(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, ChartRect? viewport = null) {
        BuildCore(chart, context, builder, plot, viewport ?? new ChartRect(0, 0, builder.Size.Width, builder.Size.Height), false);
    }

    internal static void BuildInViewport(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect viewport) =>
        BuildCore(chart, context, builder, viewport, viewport, true);

    private static void BuildCore(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, ChartRect viewport, bool measureAxes) {
        Validate(chart);
        var colors = context.Theme.Resolve(context.ThemeMode);
        if (!chart.Series.Any(series => series.Points.Count > 0) && chart.Annotations.Count == 0) {
            builder.AddDiagnostic(new VisualDiagnostic("cartesian.no-data", "The chart has no observations."));
            builder.Text(chart.Options.Labels.NoData, plot.Left + plot.Width / 2, plot.Top + plot.Height / 2,
                context.Theme.Typography.DataLabelSize, colors.MutedForeground, role: "no-data", alignment: TextAlignment.Center, paint: SvgPaint.Of(colors.MutedForeground, SvgColorRole.Text));
            return;
        }
        var coordinates = ChartBarCoordinateMap.Create(chart);
        var range = ChartRange.FromChart(chart, coordinates);
        var hasSecondary = chart.Series.Any(series => series.YAxis == ChartAxisSide.Secondary);
        var secondaryRange = hasSecondary ? ChartRange.FromSecondaryYAxis(chart, range) : null;
        var axisLabels = new AxisLabelCache();
        var horizontal = Horizontal(chart);
        if (!horizontal) {
            axisLabels.IncludeValueTicks(chart.Options.YAxis, range.MinY, range.MaxY);
            if (secondaryRange != null) axisLabels.IncludeValueTicks(chart.Options.SecondaryYAxis, secondaryRange.MinY, secondaryRange.MaxY);
        }
        if (chart.Series.Count == 1 && chart.Series[0].Kind == ChartSeriesKind.Waterfall) {
            var steps = ChartWaterfallSteps.Create(chart.Series[0]);
            axisLabels.SetTicks(chart.Options.XAxis, steps.Select(step => step.X).Distinct().OrderBy(value => value).ToArray());
            if (steps.Count > 0 && chart.Options.XAxis.LabelFormatter == null && ChartAxisValueFormatter.FindExplicitLabel(chart.Options.XAxis.Labels, steps[steps.Count - 1].X) == null)
                axisLabels.Set(chart.Options.XAxis, steps[steps.Count - 1].X, "Total");
        }
        if (measureAxes) plot = horizontal ? MeasureHorizontalPlot(chart, context, builder, viewport, range, colors, axisLabels)
            : MeasurePlot(chart, context, builder, viewport, range, secondaryRange, colors, axisLabels);
        if (plot.Width <= 0 || plot.Height <= 0) {
            builder.AddDiagnostic(new VisualDiagnostic("cartesian.insufficient-space", "No plotting area remains after measuring the frame and axes."));
            return;
        }
        var map = horizontal ? ChartMapper.ForHorizontalBars(plot, range, chart.Options.XAxis) : new ChartMapper(plot, range, chart.Options.XAxis, chart.Options.YAxis);
        var secondaryMap = secondaryRange == null ? null : new ChartMapper(plot, secondaryRange, chart.Options.XAxis, chart.Options.SecondaryYAxis);
        using (builder.PushClip(viewport)) {
            if (horizontal) DrawHorizontalAxes(chart, context, builder, plot, range, map, colors, viewport, axisLabels);
            else DrawAxes(chart, context, builder, plot, range, map, secondaryRange, secondaryMap, colors, viewport, axisLabels);
        }
        var labels = new CartesianLabels();
        var obstacles = new List<LabelObstacle>();
        {
            using (chart.Options.ClipMarksToPlot ? builder.PushClip(plot) : null)
                DrawAnnotations(chart, context, builder, plot, map, colors, true);
            foreach (var index in ChartSeriesColours.DrawingOrder(chart)) {
                var series = chart.Series[index];
                var seriesMap = series.YAxis == ChartAxisSide.Secondary ? secondaryMap! : map;
                var pointSeries = series.Kind == ChartSeriesKind.Line || series.Kind == ChartSeriesKind.StepLine || series.Kind == ChartSeriesKind.Area
                    || series.Kind == ChartSeriesKind.StepArea || series.Kind == ChartSeriesKind.StackedArea || series.Kind == ChartSeriesKind.Scatter;
                using (chart.Options.ClipMarksToPlot && !pointSeries ? builder.PushClip(plot) : null)
                using (builder.PushGroup(SeriesId(index), "series", new Dictionary<string, string> {
                    ["data-cfx-series"] = Number(index), ["data-cfx-series-key"] = series.InteractionIdentityKey,
                    ["data-cfx-series-name"] = series.Name, ["data-cfx-state"] = series.StateRole.ToString().ToLowerInvariant(),
                    ["data-cfx-pin-state-colors"] = chart.Options.PinStateColorsInForcedColors && series.StateRole != ChartSeriesState.None ? "true" : "false",
                    ["data-cfx-kind"] = series.Kind.ToString(), ["data-cfx-semantic-role"] = series.SemanticRole ?? string.Empty,
                    ["data-cfx-source-points"] = Number(series.SourcePointCount), ["data-cfx-rendered-points"] = Number(series.Points.Count),
                    ["data-cfx-decimation"] = series.DecimationMode?.ToString() ?? string.Empty, ["aria-label"] = series.Name
                })) {
                    if (series.Kind == ChartSeriesKind.Bar) DrawBars(chart, context, builder, plot, coordinates, seriesMap, index, colors, labels, obstacles);
                    else if (pointSeries)
                        DrawPoints(chart, context, builder, plot, seriesMap, index, colors, labels, obstacles);
                    else DrawExtensionSeries(chart, context, builder, plot, seriesMap, index, colors, labels, obstacles);
                }
            }
            using (chart.Options.ClipMarksToPlot ? builder.PushClip(plot) : null)
                DrawAnnotations(chart, context, builder, plot, map, colors, false);
        }
        if (chart.Options.ShowStackTotals && chart.Options.BarMode == ChartBarMode.Stacked) {
            if (horizontal) AddHorizontalTotals(chart, context, builder, plot, map, colors, labels);
            else AddStackTotals(chart, context, builder, plot, coordinates, map, secondaryMap, colors, labels);
        }
        DrawDataLabels(context, builder, plot, labels, obstacles);
    }

    private static void Validate(Chart chart) {
        if (Horizontal(chart) && chart.Series.Any(series => series.Kind != ChartSeriesKind.HorizontalBar || series.YAxis != ChartAxisSide.Primary))
            throw new NotSupportedException("Horizontal bars require a homogeneous chart on the primary horizontal value axis.");
        foreach (var series in chart.Series) {
            if (!Supports(series.Kind)) throw Unsupported("series kind " + series.Kind);
            if (series.Points.Count % ObservationStride(series.Kind) != 0)
                throw new InvalidOperationException(series.Kind + " requires complete observation tuples.");
            if ((series.Kind == ChartSeriesKind.Slope || series.Kind == ChartSeriesKind.TrendLine) && series.Points.Count != 2)
                throw new InvalidOperationException(series.Kind + " requires exactly two endpoints.");
            if (series.Kind == ChartSeriesKind.Waterfall && chart.Series.Count != 1)
                throw new NotSupportedException("A waterfall requires a single cumulative series.");
            var axis = series.YAxis == ChartAxisSide.Secondary ? chart.Options.SecondaryYAxis : chart.Options.YAxis;
            if (series.Kind == ChartSeriesKind.StackedArea && axis.Scale == ChartScaleKind.Logarithmic && series.Points.Any(point => point.Y <= 0))
                throw new InvalidOperationException("Logarithmic stacked areas require positive values.");
        }
    }

    private static NotSupportedException Unsupported(string feature) => new("The Cartesian compiler does not support " + feature + ".");
    private static ChartColor Color(ChartSeries series, int index, VisualThemeColors colors) => ChartSeriesColours.Resolve(series, index, colors);
    private static ChartColor PointColor(ChartSeries series, int seriesIndex, int pointIndex, VisualThemeColors colors) => ChartSeriesColours.Point(series, seriesIndex, pointIndex, colors);
    private static string SeriesId(int index) => "series-" + Number(index);
    private static string PointId(int series, int point) => SeriesId(series) + "-point-" + Number(point);
    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    private static string Number(double value) => value.ToString("G17", CultureInfo.InvariantCulture);

    private static IDisposable PointGroup(VisualSceneBuilder builder, ChartSeries series, int seriesIndex, int pointIndex, ChartRect bounds, ResolvedPointLabel resolvedLabel, double? baseValue = null) {
        var point = series.Points[pointIndex];
        var id = PointId(seriesIndex, pointIndex);
        var label = series.Name + ": " + resolvedLabel.DisplayedText + " (" + Number(point.X) + ", " + Number(point.Y) + ")";
        builder.AddRegion(new VisualSemanticRegion(id, "point", bounds, label));
        var metadata = new Dictionary<string, string> {
            ["data-cfx-series"] = Number(seriesIndex), ["data-cfx-point"] = Number(pointIndex),
            ["data-cfx-source-point"] = Number(pointIndex < series.SourcePointIndices.Count ? series.SourcePointIndices[pointIndex] : pointIndex),
            ["data-cfx-x"] = Number(point.X), ["data-cfx-y"] = Number(point.Y),
            ["data-cfx-state"] = series.StateRole.ToString().ToLowerInvariant(), ["data-cfx-axis"] = series.YAxis.ToString().ToLowerInvariant(),
            ["data-cfx-fill-pattern"] = (pointIndex < series.PointFillPatterns.Count && series.PointFillPatterns[pointIndex].HasValue
                ? series.PointFillPatterns[pointIndex]!.Value : series.FillPattern).ToString(),
            ["data-cfx-semantic-role"] = series.SemanticRole ?? string.Empty,
            ["data-cfx-label"] = resolvedLabel.DisplayedText, ["aria-label"] = label
        };
        if (baseValue.HasValue) metadata["data-cfx-base"] = Number(baseValue.Value);
        return builder.PushGroup(id, "point", metadata);
    }

    private static void DrawAnnotations(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, ChartMapper map, VisualThemeColors colors, bool bands) {
        for (var index = 0; index < chart.Annotations.Count; index++) {
            var annotation = chart.Annotations[index];
            if (annotation.EndValue.HasValue != bands) continue;
            var horizontal = annotation.Kind == ChartAnnotationKind.HorizontalLine || annotation.Kind == ChartAnnotationKind.HorizontalBand;
            var position = horizontal ? Math.Max(plot.Top, Math.Min(plot.Bottom, map.Y(annotation.Value))) : Math.Max(plot.Left, Math.Min(plot.Right, map.X(annotation.Value)));
            var color = annotation.Color;
            var bounds = horizontal ? new ChartRect(plot.Left, position, plot.Width, 0) : new ChartRect(position, plot.Top, 0, plot.Height);
            var id = "annotation-" + Number(index);
            var description = annotation.Kind + ": " + Number(annotation.Value)
                + (annotation.EndValue.HasValue ? " to " + Number(annotation.EndValue.Value) : string.Empty)
                + (annotation.Label.Length > 0 ? ": " + annotation.Label : string.Empty);
            using (builder.PushGroup(id, "annotation", new Dictionary<string, string> {
                ["data-cfx-kind"] = annotation.Kind.ToString(), ["data-cfx-value"] = Number(annotation.Value),
                ["data-cfx-end-value"] = annotation.EndValue.HasValue ? Number(annotation.EndValue.Value) : string.Empty,
                ["data-cfx-label"] = annotation.Label, ["aria-label"] = description
            })) {
            if (annotation.EndValue.HasValue) {
                var end = horizontal ? Math.Max(plot.Top, Math.Min(plot.Bottom, map.Y(annotation.EndValue.Value))) : Math.Max(plot.Left, Math.Min(plot.Right, map.X(annotation.EndValue.Value)));
                bounds = horizontal ? new ChartRect(plot.Left, Math.Min(position, end), plot.Width, Math.Abs(end - position))
                    : new ChartRect(Math.Min(position, end), plot.Top, Math.Abs(end - position), plot.Height);
                builder.Rect(bounds, ChartColorMath.WithOpacity(color, annotation.Opacity), role: "annotation-band",
                    paint: VisualChartPaint.Fill(SvgPaint.Of(color, SvgColorRole.Axis).WithOpacity(ChartColorMath.WithOpacity(color, annotation.Opacity), annotation.Opacity)));
            } else {
                var start = horizontal ? new ChartPoint(plot.Left, position) : new ChartPoint(position, plot.Top);
                var end = horizontal ? new ChartPoint(plot.Right, position) : new ChartPoint(position, plot.Bottom);
                builder.Path(new ChartPath(new[] { ChartPathCommand.MoveTo(start.X, start.Y), ChartPathCommand.LineTo(end.X, end.Y) }),
                    stroke: color, strokeWidth: context.Theme.AxisStrokeWidth, role: "annotation-line", cap: VisualStrokeCap.Butt,
                    dash: new[] { ChartVisualPrimitives.AnnotationLineDash, ChartVisualPrimitives.AnnotationLineGap }, paint: VisualChartPaint.Stroke(color, SvgColorRole.Axis));
            }
            builder.AddRegion(new VisualSemanticRegion(id, "annotation", bounds, description));
            if (!string.IsNullOrEmpty(annotation.Label)) {
                var plate = new ChartColorBlend(colors.Surface, SvgColorRole.Surface, color, SvgColorRole.Axis, .12);
                var backplate = plate.Color;
                var ink = ChartColorBlend.Contrast(plate);
                var style = new TextStyle { Font = context.Font, FontSize = context.Theme.Typography.AxisSize, Color = ink.Color };
                var anchor = new ChartPoint(horizontal ? plot.Left + context.Theme.Spacing : position,
                    horizontal ? position : plot.Top + context.Theme.Spacing);
                var candidates = horizontal
                    ? new[] { new LabelCandidate(0, bands ? context.Theme.Spacing : -context.Theme.Spacing, 0, bands ? 0 : 1), new LabelCandidate(0, context.Theme.Spacing, 0, 0) }
                    : new[] { new LabelCandidate(context.Theme.Spacing, 0), new LabelCandidate(-context.Theme.Spacing, 0, 1, 0) };
                const double labelPadding = 4;
                var request = new LabelPlacementRequest(annotation.Label, anchor, style, candidates) { Padding = labelPadding };
                var label = new LabelPlacementService().Place(new[] { request }, plot, null, 0, builder.MeasureText)[0];
                if (label.IsDropped || label.IsEllipsized) builder.AddDiagnostic(new VisualDiagnostic("cartesian.annotation-label-overflow", "An annotation label was shortened or omitted within the plot."));
                if (!label.IsDropped) {
                    builder.Rect(label.Bounds, backplate, ChartColorMath.WithOpacity(color, .36), radius: Math.Min(4, context.Theme.BarRadius),
                        role: "annotation-label-backplate", paint: new VisualScenePaintBinding(
                            fill: plate.Paint,
                            stroke: SvgPaint.Of(color, SvgColorRole.Axis).WithOpacity(ChartColorMath.WithOpacity(color, .36), .36)));
                    builder.Text(label.Text, label.Bounds.Left + labelPadding, label.Bounds.Top + labelPadding + builder.TextAscent(style), style,
                        role: "annotation-label", paint: ink.Paint);
                }
            }
            }
        }
    }
}
