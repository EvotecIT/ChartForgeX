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
                var paint = VisualChartPaint.Series(series, color, point);
                entries.Add(new VisualLegendEntry(label, color, PointId(0, point), series.Kind, pattern, series.StateRole, series.InteractionIdentityKey,
                    marker: FinancialLegend(chart, series, 0, colors, point, pattern)
                        ?? VisualMarkerScene.Legend(chart, series, color, paint, point, pattern), paint: paint));
            }
            return entries;
        }
        for (var index = 0; index < chart.Series.Count; index++) {
            var series = chart.Series[index];
            if (series.ShowInLegend) {
                var color = Color(series, index, colors); var paint = VisualChartPaint.Series(series, color);
                entries.Add(new VisualLegendEntry(series.Name, color, SeriesId(index), series.Kind, series.FillPattern, series.StateRole,
                    series.InteractionIdentityKey, marker: FinancialLegend(chart, series, index, colors, pattern: series.FillPattern)
                        ?? VisualMarkerScene.Legend(chart, series, color, paint, pattern: series.FillPattern), paint: paint));
            }
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
        var coordinates = ChartBarCoordinateMap.Create(chart);
        var stacks = ChartStackLayout.Create(chart, coordinates);
        if (!chart.Series.Any(series => series.Points.Count > 0) && chart.Annotations.Count == 0) {
            builder.AddDiagnostic(new VisualDiagnostic("cartesian.no-data", "The chart has no observations."));
            builder.Text(chart.Options.Labels.NoData, plot.Left + plot.Width / 2, plot.Top + plot.Height / 2,
                context.Theme.Typography.DataLabelSize, colors.MutedForeground, role: "no-data", alignment: TextAlignment.Center, paint: SvgPaint.Of(colors.MutedForeground, SvgColorRole.Text));
            return;
        }
        foreach (var total in stacks.Totals.Where(total => total.NormalizedTo.HasValue && total.SourceValue == 0))
            builder.AddDiagnostic(new VisualDiagnostic("cartesian.stack-zero-total", "Normalized stack '" + (total.Group ?? "default")
                + "' on the " + total.Axis.ToString().ToLowerInvariant() + " axis at " + Number(total.Coordinate)
                + " has a zero " + (total.Positive ? "positive" : "negative") + " total and remains at the baseline with its source observations retained."));
        var range = ChartRange.FromChart(chart, coordinates, stacks);
        var hasSecondary = chart.Series.Any(series => series.YAxis == ChartAxisSide.Secondary);
        var secondaryRange = hasSecondary ? ChartRange.FromSecondaryYAxis(chart, range, stacks) : null;
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
        if (!horizontal && ExpandMarkerRanges(chart, context, plot, range, secondaryRange, stacks)) {
            axisLabels.IncludeValueTicks(chart.Options.YAxis, range.MinY, range.MaxY);
            if (secondaryRange != null) axisLabels.IncludeValueTicks(chart.Options.SecondaryYAxis, secondaryRange.MinY, secondaryRange.MaxY);
            if (measureAxes) plot = MeasurePlot(chart, context, builder, viewport, range, secondaryRange, colors, axisLabels);
        }
        if (plot.Width <= 0 || plot.Height <= 0) {
            builder.AddDiagnostic(new VisualDiagnostic("cartesian.insufficient-space", "No plotting area remains after measuring the frame and axes."));
            return;
        }
        var labelBounds = plot;
        var horizontalTotals = horizontal && chart.Options.ShowStackTotals && stacks.HasBarStacks
            ? ResolveHorizontalTotals(chart, context, builder, colors, stacks) : Array.Empty<HorizontalStackTotal>();
        var verticalTotals = !horizontal && chart.Options.ShowStackTotals && stacks.HasBarStacks
            ? ResolveVerticalTotals(chart, context, builder, colors, stacks) : null;
        if (horizontalTotals.Count > 0) plot = ReserveHorizontalTotalGutters(chart, plot, horizontalTotals, context.Theme.Spacing);
        if (verticalTotals != null && verticalTotals.Count > 0) plot = ReserveVerticalTotalGutters(chart, plot, verticalTotals, context.Theme.Spacing);
        // Axis measurement and total lanes can change the final radius-to-plot ratio.
        if (!horizontal && ExpandMarkerRanges(chart, context, plot, range, secondaryRange, stacks)) {
            axisLabels.IncludeValueTicks(chart.Options.YAxis, range.MinY, range.MaxY);
            if (secondaryRange != null) axisLabels.IncludeValueTicks(chart.Options.SecondaryYAxis, secondaryRange.MinY, secondaryRange.MaxY);
        }
        var map = horizontal ? ChartMapper.ForHorizontalBars(plot, range, chart.Options.XAxis, chart.Options.YAxis) : new ChartMapper(plot, range, chart.Options.XAxis, chart.Options.YAxis);
        var secondaryMap = secondaryRange == null ? null : new ChartMapper(plot, secondaryRange, chart.Options.XAxis, chart.Options.SecondaryYAxis);
        using (builder.PushClip(viewport)) {
            if (horizontal) DrawHorizontalAxes(chart, context, builder, plot, range, map, colors, viewport, axisLabels, labelBounds.Left);
            else DrawAxes(chart, context, builder, plot, range, map, secondaryRange, secondaryMap, colors, viewport, axisLabels);
        }
        var labels = new CartesianLabels();
        var obstacles = new List<LabelObstacle>();
        // Caption placement needs the actual marks before the background band layer is
        // emitted. Collect those marks once; ordinary charts keep the direct builder path.
        var seriesBuilder = chart.Annotations.Any(annotation => annotation.EndValue.HasValue && annotation.ShowLabel && annotation.Label.Length > 0)
            ? new VisualSceneBuilder(builder.Size, context.Font) : builder;
        {
            if (ReferenceEquals(seriesBuilder, builder)) {
                using (chart.Options.ClipMarksToPlot ? builder.PushClip(plot) : null)
                    DrawAnnotations(chart, context, builder, plot, map, colors, true, obstacles);
            }
            foreach (var index in ChartSeriesColours.DrawingOrder(chart)) {
                var series = chart.Series[index];
                var seriesMap = series.YAxis == ChartAxisSide.Secondary ? secondaryMap! : map;
                var pointSeries = IsPointSeries(series.Kind);
                using (chart.Options.ClipMarksToPlot && !pointSeries ? seriesBuilder.PushClip(plot) : null)
                using (seriesBuilder.PushGroup(SeriesId(index), "series", new Dictionary<string, string> {
                    ["data-cfx-coordinate-system"] = "cartesian",
                    ["data-cfx-series"] = Number(index), ["data-cfx-series-key"] = series.InteractionIdentityKey,
                    ["data-cfx-series-name"] = series.Name, ["data-cfx-state"] = series.StateRole.ToString().ToLowerInvariant(),
                    ["data-cfx-pin-state-colors"] = chart.Options.PinStateColorsInForcedColors && series.StateRole != ChartSeriesState.None ? "true" : "false",
                    ["data-cfx-kind"] = series.Kind.ToString(), ["data-cfx-semantic-role"] = series.SemanticRole ?? string.Empty,
                    ["data-cfx-source-points"] = Number(series.SourcePointCount), ["data-cfx-rendered-points"] = Number(series.Points.Count),
                    ["data-cfx-decimation"] = series.DecimationMode?.ToString() ?? string.Empty, ["aria-label"] = series.Name
                })) {
                    if (series.Kind == ChartSeriesKind.Bar) DrawBars(chart, context, seriesBuilder, plot, coordinates, seriesMap, stacks, index, colors, labels, obstacles);
                    else if (pointSeries)
                        DrawPoints(chart, context, seriesBuilder, plot, seriesMap, stacks, index, colors, labels, obstacles);
                    else DrawExtensionSeries(chart, context, seriesBuilder, plot, seriesMap, stacks, index, colors, labels, obstacles);
                }
            }
            if (!ReferenceEquals(seriesBuilder, builder)) {
                using (chart.Options.ClipMarksToPlot ? builder.PushClip(plot) : null)
                    DrawAnnotations(chart, context, builder, plot, map, colors, true, obstacles);
                builder.Append(seriesBuilder.Build());
            }
            using (chart.Options.ClipMarksToPlot ? builder.PushClip(plot) : null)
                DrawAnnotations(chart, context, builder, plot, map, colors, false, obstacles);
        }
        if (chart.Options.ShowStackTotals && stacks.HasBarStacks) {
            if (horizontal) AddHorizontalTotals(chart, horizontalTotals, context, builder, plot, map, stacks, labels);
            else AddStackTotals(chart, context, builder, plot, coordinates, stacks, map, secondaryMap, labels, verticalTotals!);
        }
        DrawDataLabels(context, builder, labelBounds, labels, obstacles);
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

    private static IDisposable PointGroup(VisualSceneBuilder builder, ChartSeries series, int seriesIndex, int pointIndex, ChartRect bounds, ResolvedPointLabel resolvedLabel, ChartStackPoint? stack = null) {
        var point = series.Points[pointIndex];
        var id = PointId(seriesIndex, pointIndex);
        var label = series.Name + ": " + resolvedLabel.DisplayedText + " (" + ChartNumericFormatter.FormatCoordinate(point.X) + ", " + ChartNumericFormatter.FormatCoordinate(point.Y) + ")";
        if (series.HistogramBinLayout != null) label = HistogramPointDescription(series, pointIndex, resolvedLabel.DisplayedText);
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
        if (stack.HasValue) ChartStackLayout.AddMetadata(metadata, stack.Value);
        if (series.HistogramBinLayout != null) AddHistogramMetadata(metadata, series, pointIndex);
        return builder.PushGroup(id, "point", metadata);
    }

}
