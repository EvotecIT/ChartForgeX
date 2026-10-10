using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

/// <summary>Compiles numeric radial bars and columns from canonical category, stack and value-axis snapshots.</summary>
internal static partial class VisualNumericRadialCompiler {
    internal static void Build(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, ChartAxisValueFormatter.Cache? axisLabels = null) {
        var bars = chart.Series[0].Kind == ChartSeriesKind.RadialBar;
        var colors = context.Theme.Resolve(context.ThemeMode);
        Validate(chart);
        var coordinates = ChartBarCoordinateMap.Create(chart);
        var stacks = ChartStackLayout.Create(chart, coordinates);
        var categories = Categories(chart, coordinates);
        if (categories.Length == 0) {
            builder.AddDiagnostic(new VisualDiagnostic("numeric-radial.no-data", "The chart has no observations."));
            builder.Text(chart.Options.Labels.NoData, plot.Left + plot.Width / 2, plot.Top + plot.Height / 2,
                context.Theme.Typography.DataLabelSize, colors.MutedForeground, role: "no-data", alignment: Typography.TextAlignment.Center,
                paint: SvgPaint.Of(colors.MutedForeground, SvgColorRole.Text));
            return;
        }
        var scales = Scales(chart, stacks);
        axisLabels ??= new ChartAxisValueFormatter.Cache();
        var categoryLabels = categories.Select(category => axisLabels.Format(chart.Options.XAxis, category.Value)).ToArray();
        var tickLabels = scales.ToDictionary(pair => pair.Key, pair => pair.Value.Ticks.Select(value =>
            ChartAxisValueFormatter.Format(Axis(chart, pair.Key), value,
                pair.Key == ChartAxisSide.Secondary ? chart.Options.SecondaryYAxisValueFormatter : chart.Options.ValueFormatter, pair.Value.Ticks)).ToArray());
        var geometry = Layout(chart, context, builder, plot, categoryLabels, tickLabels, bars);
        if (geometry.Outer <= 0) {
            builder.AddDiagnostic(new VisualDiagnostic("numeric-radial.insufficient-space", "The frame leaves no space for numeric radial marks."));
            return;
        }
        var labels = new List<RadialSeriesLabel>();
        // Axis captions and totals also need mark avoidance when point captions are off.
        var obstacles = chart.Options.ShowAxes || chart.Options.ShowStackTotals || chart.Series.Any(series => series.ShowDataLabels ?? chart.Options.ShowDataLabels)
            ? new List<LabelObstacle>() : null;
        foreach (var total in stacks.Totals.Where(total => total.NormalizedTo.HasValue && total.SourceValue == 0))
            builder.AddDiagnostic(new VisualDiagnostic("numeric-radial.stack-zero-total", "A normalized zero-only stack remains at its baseline; its source observations are retained."));
        using (builder.PushClip(plot)) {
            Grid(chart, context, builder, plot, geometry, categoryLabels, categories, scales, tickLabels, bars, labels);
            var participants = Enumerable.Range(0, chart.Series.Count).ToArray();
            for (var seriesIndex = 0; seriesIndex < chart.Series.Count; seriesIndex++) {
                var series = chart.Series[seriesIndex];
                var slot = stacks.Slot(participants, seriesIndex);
                var seriesMetadata = new Dictionary<string, string> {
                    ["data-cfx-series"] = N(seriesIndex), ["data-cfx-series-key"] = series.InteractionIdentityKey,
                    ["data-cfx-label"] = series.Name, ["data-cfx-axis"] = series.YAxis.ToString().ToLowerInvariant(),
                    ["data-cfx-radial-kind"] = bars ? "bar" : "column"
                };
                if (scales.TryGetValue(series.YAxis, out var scale)) {
                    seriesMetadata["data-cfx-min-value"] = N(scale.Minimum); seriesMetadata["data-cfx-max-value"] = N(scale.Maximum);
                }
                using (builder.PushGroup(SeriesId(seriesIndex), "series", seriesMetadata)) {
                    for (var pointIndex = 0; pointIndex < series.Points.Count; pointIndex++) {
                        var key = coordinates.Resolve(seriesIndex, pointIndex);
                        var category = Array.FindIndex(categories, value => value.Id == key.Id);
                        var stack = stacks.Point(seriesIndex, pointIndex);
                        var mark = Mark(chart, geometry, scale!, stack.Base, stack.End, category, categories.Length, slot, bars);
                        DrawPoint(chart, context, builder, plot, geometry, series, seriesIndex, pointIndex, categoryLabels[category],
                            stack, mark, colors, labels, obstacles);
                    }
                }
            }
            if (chart.Options.ShowStackTotals) StackTotals(chart, context, builder, plot, geometry, categories, coordinates, stacks, scales, bars, labels);
            DrawLabels(builder, plot, labels, obstacles, context.Theme.Spacing);
        }
    }

    private static void Validate(Chart chart) {
        if (chart.Options.XAxis.Minimum.HasValue || chart.Options.XAxis.Maximum.HasValue)
            throw new NotSupportedException("Numeric radial category axes use observation categories; explicit category bounds are not supported.");
        if (chart.Options.XAxis.Scale is ChartScaleKind.Logarithmic or ChartScaleKind.SymmetricLogarithmic)
            throw new NotSupportedException("Numeric radial categories use ordinal spacing; logarithmic category scales are not supported.");
        foreach (var series in chart.Series) {
            var axis = Axis(chart, series.YAxis);
            if (axis.Scale == ChartScaleKind.Time) throw new NotSupportedException("Numeric radial value axes support linear, logarithmic and symmetric logarithmic scales.");
            foreach (var point in series.Points) {
                if (!ChartMath.IsFinite(point.X) || !ChartMath.IsFinite(point.Y)) throw new InvalidOperationException("Numeric radial observations must be finite.");
                if (axis.Scale == ChartScaleKind.Logarithmic && point.Y <= 0) throw new InvalidOperationException("Logarithmic numeric radial values must be positive.");
            }
        }
    }

    private static ChartBarCoordinateKey[] Categories(Chart chart, ChartBarCoordinateMap coordinates) =>
        Enumerable.Range(0, chart.Series.Count).SelectMany(series => Enumerable.Range(0, chart.Series[series].Points.Count)
            .Select(point => coordinates.Resolve(series, point))).Distinct().OrderBy(key => key.Value).ThenBy(key => key.Id).ToArray();

    private static Dictionary<ChartAxisSide, RadialValueScale> Scales(Chart chart, ChartStackLayout stacks) {
        var result = new Dictionary<ChartAxisSide, RadialValueScale>();
        foreach (var side in chart.Series.Select(series => series.YAxis).Distinct()) {
            var axis = Axis(chart, side);
            var values = Enumerable.Range(0, chart.Series.Count).Where(index => chart.Series[index].YAxis == side)
                .SelectMany(series => Enumerable.Range(0, chart.Series[series].Points.Count)
                    .SelectMany(point => new[] { stacks.Point(series, point).Base, stacks.Point(series, point).End }))
                .Where(value => axis.Scale != ChartScaleKind.Logarithmic || value != 0).ToArray();
            // A series with no observations retains its legend and slot, but does not set another axis's domain.
            if (values.Length == 0) {
                // Without a complete authored domain there is no scale to display or measure.
                if (!axis.Minimum.HasValue || !axis.Maximum.HasValue) continue;
                values = new[] { axis.Minimum.Value, axis.Maximum.Value };
            }
            result.Add(side, RadialValueScale.Create(axis, values, "Numeric radial", true));
        }
        return result;
    }

    private static void DrawPoint(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, RadialSeriesGeometry geometry,
        ChartSeries series, int seriesIndex, int pointIndex, string category, ChartStackPoint stack, RadialSeriesMark mark,
        VisualThemeColors colors, List<RadialSeriesLabel> labels, List<LabelObstacle>? obstacles) {
        var point = series.Points[pointIndex]; var id = PointId(seriesIndex, pointIndex);
        var text = pointIndex < series.PointLabels.Count && series.PointLabels[pointIndex] != null ? series.PointLabels[pointIndex]!
            : ChartNumericFormatter.FormatValue(chart.Options, point.Y);
        var description = series.Name + ", " + category + ": " + text;
        var metadata = new Dictionary<string, string> {
            ["data-cfx-series"] = N(seriesIndex), ["data-cfx-series-key"] = series.InteractionIdentityKey,
            ["data-cfx-point"] = N(pointIndex), ["data-cfx-source-point"] = N(pointIndex < series.SourcePointIndices.Count ? series.SourcePointIndices[pointIndex] : pointIndex),
            ["data-cfx-x"] = N(point.X), ["data-cfx-y"] = N(point.Y), ["data-cfx-value"] = N(point.Y),
            ["data-cfx-label"] = text, ["data-cfx-category"] = category, ["data-cfx-full-label"] = description,
            ["data-cfx-state"] = series.StateRole.ToString().ToLowerInvariant(), ["data-cfx-axis"] = series.YAxis.ToString().ToLowerInvariant(),
            ["data-cfx-semantic-role"] = series.SemanticRole ?? string.Empty, ["aria-label"] = description,
            ["data-cfx-start-angle"] = N(mark.Start), ["data-cfx-sweep-angle"] = N(mark.Sweep),
            ["data-cfx-inner-radius"] = N(mark.Inner), ["data-cfx-outer-radius"] = N(mark.Outer),
            ["data-cfx-clipped"] = mark.Clipped ? "true" : "false",
            ["data-cfx-pin-state-colors"] = chart.Options.PinStateColorsInForcedColors && series.StateRole != ChartSeriesState.None ? "true" : "false"
        };
        if (!mark.Painted && !mark.Clipped)
            metadata["data-cfx-geometry-status"] = point.Y == 0 ? "zero" : "precision-collapse";
        ChartStackLayout.AddMetadata(metadata, stack);
        var bounds = mark.Painted ? ChartSlicePathGeometry.Bounds(geometry.Cx, geometry.Cy, mark.Outer, mark.Inner, mark.Start, mark.Sweep)
            : new ChartRect(mark.End.X, mark.End.Y, 0, 0);
        builder.AddRegion(new VisualSemanticRegion(id, "point", bounds, description));
        var color = ChartSeriesColours.Point(series, seriesIndex, pointIndex, colors);
        var pattern = pointIndex < series.PointFillPatterns.Count && series.PointFillPatterns[pointIndex].HasValue
            ? series.PointFillPatterns[pointIndex]!.Value : series.FillPattern;
        metadata["data-cfx-fill-pattern"] = pattern.ToString();
        if (mark.Painted && obstacles != null) {
            var outline = new VisualSceneSlice(geometry.Cx, geometry.Cy, mark.Outer, mark.Inner, mark.Start, mark.Sweep, color, null, 0, null, null);
            obstacles.Add(new LabelObstacle(id, new LabelMarkShape(VisualSceneGeometry.Flatten(outline, 8), true, 0, plot)));
        }
        using (builder.PushGroup(id, "point", metadata)) {
            if (mark.Painted) {
                builder.Slice(geometry.Cx, geometry.Cy, mark.Outer, mark.Inner, mark.Start, mark.Sweep, color,
                    role: series.Kind == ChartSeriesKind.RadialBar ? "radial-bar" : "radial-column", id: id + "-mark",
                    paint: VisualChartPaint.Fill(VisualChartPaint.Series(series, color, pointIndex)));
                if (pattern != ChartFillPattern.None) builder.PatternSlice(geometry.Cx, geometry.Cy, mark.Outer, mark.Inner, mark.Start, mark.Sweep,
                    pattern, ChartColorMath.AccessibleTextOnBackground(color).WithAlpha(110), role: "radial-fill-pattern");
            }
        }
        if (series.ShowDataLabels ?? chart.Options.ShowDataLabels) DataLabel(chart, context, builder, plot, series, pointIndex, id, text, mark, bounds, color, labels);
    }

    private static ChartAxis Axis(Chart chart, ChartAxisSide side) => side == ChartAxisSide.Secondary ? chart.Options.SecondaryYAxis : chart.Options.YAxis;
    private static string SeriesId(int index) => "series-" + N(index);
    private static string PointId(int series, int point) => SeriesId(series) + "-point-" + N(point);
    private static string N(double value) => value.ToString("G17", CultureInfo.InvariantCulture);
}
