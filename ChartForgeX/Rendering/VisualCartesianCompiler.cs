using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

/// <summary>Compiles the initial Cartesian proof directly into shared, immutable scene commands.</summary>
internal static partial class VisualCartesianCompiler {
    internal static IReadOnlyList<VisualLegendEntry> LegendEntries(Chart chart, VisualThemeColors colors) {
        var entries = new List<VisualLegendEntry>();
        for (var index = 0; index < chart.Series.Count; index++) {
            var series = chart.Series[index];
            if (series.ShowInLegend) entries.Add(new VisualLegendEntry(series.Name, Color(series, index, colors), SeriesId(index)));
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
                context.Theme.Typography.DataLabelSize, colors.MutedForeground, role: "no-data", alignment: TextAlignment.Center);
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
            DrawAnnotations(chart, context, builder, plot, map, colors);
            for (var index = 0; index < chart.Series.Count; index++) {
                var series = chart.Series[index];
                var seriesMap = series.YAxis == ChartAxisSide.Secondary ? secondaryMap! : map;
                using (builder.PushGroup(SeriesId(index), "series", new Dictionary<string, string> {
                    ["data-cfx-series"] = Number(index), ["data-cfx-series-key"] = series.InteractionIdentityKey,
                    ["data-cfx-semantic-role"] = series.SemanticRole ?? string.Empty,
                    ["aria-label"] = series.Name
                })) {
                    if (series.Kind == ChartSeriesKind.Bar) DrawBars(chart, context, builder, plot, coordinates, seriesMap, index, colors, labels, obstacles);
                    else DrawPoints(chart, context, builder, plot, seriesMap, index, colors, labels, obstacles);
                }
            }
        }
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
            if (series.FillPattern != ChartFillPattern.None || series.PointFillPatterns.Any(pattern => pattern.HasValue && pattern != ChartFillPattern.None))
                throw Unsupported("fill patterns");
            if (series.StateRole != ChartSeriesState.None) throw Unsupported("state color treatments; provide an explicit color for this proof");
            if ((series.Kind == ChartSeriesKind.StackedArea || series.Kind == ChartSeriesKind.Bar && chart.Options.BarMode == ChartBarMode.Stacked)
                && series.YAxis == ChartAxisSide.Secondary) throw Unsupported("secondary-axis stacks");
            if (series.Kind == ChartSeriesKind.StackedArea && chart.Options.YAxis.Scale == ChartScaleKind.Logarithmic && series.Points.Any(point => point.Y <= 0))
                throw new InvalidOperationException("Logarithmic stacked areas require positive values.");
            if (series.Kind != ChartSeriesKind.Bar && series.Kind != ChartSeriesKind.Scatter && series.Points.Count > 0 && chart.Options.HasPreparedLineVisualStyle)
                throw Unsupported("explicit line visual styles");
        }
        if (chart.Options.XAxis.LabelAngle != 0 || chart.Options.YAxis.LabelAngle != 0 || chart.Options.SecondaryYAxis.LabelAngle != 0)
            throw Unsupported("rotated axis labels");
        if (chart.Options.HasPreparedSegmentedBars) throw Unsupported("segmented capsule bars");
        if (chart.Options.ShowStackTotals) throw Unsupported("stack total labels");
        if (chart.Options.ShowPointLegend) throw Unsupported("point legends for Cartesian series");
        if (chart.Options.XAxis.LabelDensity == ChartLabelDensity.All || chart.Options.YAxis.LabelDensity == ChartLabelDensity.All || chart.Options.SecondaryYAxis.LabelDensity == ChartLabelDensity.All)
            throw Unsupported("unconditionally visible axis labels; use automatic, dense or relaxed placement");
    }

    private static NotSupportedException Unsupported(string feature) => new("The Phase 1 prepared Cartesian compiler does not support " + feature + ". The legacy export remains available.");
    private static ChartColor Color(ChartSeries series, int index, VisualThemeColors colors) => series.Color ?? colors.Palette[index % colors.Palette.Count];
    private static ChartColor PointColor(ChartSeries series, int seriesIndex, int pointIndex, VisualThemeColors colors) =>
        pointIndex < series.PointColors.Count && series.PointColors[pointIndex].HasValue ? series.PointColors[pointIndex]!.Value : Color(series, seriesIndex, colors);
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
            ["data-cfx-semantic-role"] = series.SemanticRole ?? string.Empty,
            ["data-cfx-label"] = resolvedLabel.DisplayedText, ["aria-label"] = label
        });
    }

    private static void DrawAnnotations(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, ChartMapper map, VisualThemeColors colors) {
        for (var index = 0; index < chart.Annotations.Count; index++) {
            var annotation = chart.Annotations[index];
            var horizontal = annotation.Kind == ChartAnnotationKind.HorizontalLine || annotation.Kind == ChartAnnotationKind.HorizontalBand;
            var position = horizontal ? map.Y(annotation.Value) : map.X(annotation.Value);
            var color = annotation.Color;
            var id = "annotation-" + Number(index);
            var kind = annotation.Kind.ToString();
            var description = kind + ": " + Number(annotation.Value)
                + (annotation.EndValue.HasValue ? " to " + Number(annotation.EndValue.Value) : string.Empty)
                + (annotation.Label.Length > 0 ? ": " + annotation.Label : string.Empty);
            var bounds = horizontal ? new ChartRect(plot.Left, position, plot.Width, 0) : new ChartRect(position, plot.Top, 0, plot.Height);
            if (annotation.EndValue.HasValue) {
                var end = horizontal ? map.Y(annotation.EndValue.Value) : map.X(annotation.EndValue.Value);
                bounds = horizontal ? new ChartRect(plot.Left, Math.Min(position, end), plot.Width, Math.Abs(end - position))
                    : new ChartRect(Math.Min(position, end), plot.Top, Math.Abs(end - position), plot.Height);
            }
            builder.AddRegion(new VisualSemanticRegion(id, "annotation", bounds, description));
            using (builder.PushGroup(id, "annotation", new Dictionary<string, string> {
                ["data-cfx-kind"] = kind, ["data-cfx-value"] = Number(annotation.Value),
                ["data-cfx-end-value"] = annotation.EndValue.HasValue ? Number(annotation.EndValue.Value) : string.Empty,
                ["data-cfx-label"] = annotation.Label, ["aria-label"] = description
            })) {
                if (annotation.EndValue.HasValue) builder.Rect(bounds, color.WithAlpha((byte)Math.Round(color.A * annotation.Opacity)), role: "annotation-band");
                else if (horizontal) builder.Line(plot.Left, position, plot.Right, position, color, context.Theme.AxisStrokeWidth, role: "annotation-line");
                else builder.Line(position, plot.Top, position, plot.Bottom, color, context.Theme.AxisStrokeWidth, role: "annotation-line");
                if (!string.IsNullOrEmpty(annotation.Label)) builder.Text(annotation.Label, horizontal ? plot.Left + context.Theme.Spacing : position + context.Theme.Spacing,
                    horizontal ? position - context.Theme.Spacing : plot.Top + context.Theme.Typography.AxisSize, context.Theme.Typography.AxisSize, colors.MutedForeground, role: "annotation-label");
            }
        }
    }
}
