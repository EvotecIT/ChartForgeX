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
            for (var point = 0; point < series.Points.Count; point++) {
                var label = ChartAxisValueFormatter.FindExplicitLabel(chart.Options.XAxis.Labels, series.Points[point].X) ?? "Item " + Number(point + 1);
                var pattern = point < series.PointFillPatterns.Count && series.PointFillPatterns[point].HasValue ? series.PointFillPatterns[point]!.Value : series.FillPattern;
                var color = PointColor(series, 0, point, colors);
                entries.Add(new VisualLegendEntry(label, color, PointId(0, point), series.Kind, pattern, series.StateRole, series.InteractionIdentityKey, VisualChartPaint.Series(series, color, point)));
            }
            return entries;
        }
        for (var index = 0; index < chart.Series.Count; index++) {
            var series = chart.Series[index];
            if (series.ShowInLegend) entries.Add(new VisualLegendEntry(series.Name, Color(series, index, colors), SeriesId(index),
                series.Kind, series.FillPattern, series.StateRole, series.InteractionIdentityKey, VisualChartPaint.Series(series, Color(series, index, colors))));
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
        if (measureAxes) plot = MeasurePlot(chart, context, builder, viewport, range, secondaryRange, colors, axisLabels);
        if (plot.Width <= 0 || plot.Height <= 0) {
            builder.AddDiagnostic(new VisualDiagnostic("cartesian.insufficient-space", "No plotting area remains after measuring the frame and axes."));
            return;
        }
        var map = new ChartMapper(plot, range, chart.Options.XAxis, chart.Options.YAxis);
        var secondaryMap = secondaryRange == null ? null : new ChartMapper(plot, secondaryRange, chart.Options.XAxis, chart.Options.SecondaryYAxis);
        using (builder.PushClip(viewport)) DrawAxes(chart, context, builder, plot, range, map, secondaryRange, secondaryMap, colors, viewport, axisLabels);
        var labels = new List<LabelPlacementRequest>();
        var obstacles = new List<LabelObstacle>();
        using (chart.Options.ClipMarksToPlot ? builder.PushClip(plot) : null) {
            DrawAnnotations(chart, context, builder, plot, map, colors, true);
            foreach (var index in ChartSeriesColours.DrawingOrder(chart)) {
                var series = chart.Series[index];
                var seriesMap = series.YAxis == ChartAxisSide.Secondary ? secondaryMap! : map;
                using (builder.PushGroup(SeriesId(index), "series", new Dictionary<string, string> {
                    ["data-cfx-series"] = Number(index), ["data-cfx-series-key"] = series.InteractionIdentityKey,
                    ["data-cfx-series-name"] = series.Name, ["data-cfx-state"] = series.StateRole.ToString().ToLowerInvariant(),
                    ["data-cfx-kind"] = series.Kind.ToString(), ["data-cfx-semantic-role"] = series.SemanticRole ?? string.Empty,
                    ["data-cfx-source-points"] = Number(series.SourcePointCount), ["data-cfx-rendered-points"] = Number(series.Points.Count),
                    ["data-cfx-decimation"] = series.DecimationMode?.ToString() ?? string.Empty, ["aria-label"] = series.Name
                })) {
                    if (series.Kind == ChartSeriesKind.Bar) DrawBars(chart, context, builder, plot, coordinates, seriesMap, index, colors, labels, obstacles);
                    else DrawPoints(chart, context, builder, plot, seriesMap, index, colors, labels, obstacles);
                }
            }
            DrawAnnotations(chart, context, builder, plot, map, colors, false);
        }
        if (chart.Options.ShowStackTotals && chart.Options.BarMode == ChartBarMode.Stacked)
            AddStackTotals(chart, context, builder, plot, coordinates, map, secondaryMap, colors, labels);
        DrawDataLabels(context, builder, plot, labels, obstacles);
    }

    private static void Validate(Chart chart) {
        foreach (var series in chart.Series) {
            switch (series.Kind) {
                case ChartSeriesKind.Line: case ChartSeriesKind.StepLine: case ChartSeriesKind.Area:
                case ChartSeriesKind.StepArea: case ChartSeriesKind.StackedArea: case ChartSeriesKind.Bar: case ChartSeriesKind.Scatter:
                    break;
                default: throw Unsupported("series kind " + series.Kind);
            }
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

    private static IDisposable PointGroup(VisualSceneBuilder builder, ChartSeries series, int seriesIndex, int pointIndex, ChartRect bounds, ResolvedPointLabel resolvedLabel) {
        var point = series.Points[pointIndex];
        var id = PointId(seriesIndex, pointIndex);
        var label = series.Name + ": " + resolvedLabel.DisplayedText + " (" + Number(point.X) + ", " + Number(point.Y) + ")";
        builder.AddRegion(new VisualSemanticRegion(id, "point", bounds, label));
        return builder.PushGroup(id, "point", new Dictionary<string, string> {
            ["data-cfx-series"] = Number(seriesIndex), ["data-cfx-point"] = Number(pointIndex),
            ["data-cfx-source-point"] = Number(pointIndex < series.SourcePointIndices.Count ? series.SourcePointIndices[pointIndex] : pointIndex),
            ["data-cfx-x"] = Number(point.X), ["data-cfx-y"] = Number(point.Y),
            ["data-cfx-state"] = series.StateRole.ToString().ToLowerInvariant(), ["data-cfx-axis"] = series.YAxis.ToString().ToLowerInvariant(),
            ["data-cfx-semantic-role"] = series.SemanticRole ?? string.Empty,
            ["data-cfx-label"] = resolvedLabel.DisplayedText, ["aria-label"] = label
        });
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
            } else if (horizontal) builder.Line(plot.Left, position, plot.Right, position, color, context.Theme.AxisStrokeWidth, role: "annotation-line",
                dash: new[] { ChartVisualPrimitives.AnnotationLineDash, ChartVisualPrimitives.AnnotationLineGap }, paint: VisualChartPaint.Stroke(color, SvgColorRole.Axis));
            else builder.Line(position, plot.Top, position, plot.Bottom, color, context.Theme.AxisStrokeWidth, role: "annotation-line",
                dash: new[] { ChartVisualPrimitives.AnnotationLineDash, ChartVisualPrimitives.AnnotationLineGap }, paint: VisualChartPaint.Stroke(color, SvgColorRole.Axis));
            builder.AddRegion(new VisualSemanticRegion(id, "annotation", bounds, description));
            if (!string.IsNullOrEmpty(annotation.Label)) {
                var style = new TextStyle { Font = context.Font, FontSize = context.Theme.Typography.AxisSize, Color = bands ? colors.MutedForeground : color };
                var anchor = new ChartPoint(horizontal ? plot.Left + context.Theme.Spacing : position + context.Theme.Spacing,
                    horizontal ? position + (bands ? context.Theme.Spacing : -context.Theme.Spacing) : plot.Top + context.Theme.Spacing);
                var request = new LabelPlacementRequest(annotation.Label, anchor, style, new[] { new LabelCandidate(0, 0), new LabelCandidate(0, 0, 1, 1) });
                var label = new LabelPlacementService().Place(new[] { request }, plot, null, 0, builder.MeasureText)[0];
                if (label.IsDropped || label.IsEllipsized) builder.AddDiagnostic(new VisualDiagnostic("cartesian.annotation-label-overflow", "An annotation label was shortened or omitted within the plot."));
                if (!label.IsDropped) builder.Text(label.Text, label.Bounds.Left, label.Bounds.Top + builder.TextAscent(style), style, role: "annotation-label", paint: VisualChartPaint.Text(style));
            }
            }
        }
    }
}
