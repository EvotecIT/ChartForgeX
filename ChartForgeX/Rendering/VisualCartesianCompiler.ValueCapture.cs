using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    private static void CaptureCartesianValues(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot,
        ChartRange range, ChartRange? secondaryRange, ChartRange fullRange, ChartRange? fullSecondaryRange, ChartMapper map, ChartMapper? secondaryMap,
        ChartBarCoordinateMap coordinates, ChartStackLayout stacks, AxisLabelCache labels,
        VisualSceneBuilder.VisualValueBoundary first, VisualSceneBuilder.VisualValueBoundary last, ChartCartesianViewport? viewport) {
        // This is an optional finite capture, not a new ordinary-render admission rule. Unsupported charts still render normally.
        if (viewport != null || chart.Options.IsSparkline || ChartCartesianOrientation.IsHorizontal(chart) || chart.Series.Count == 0
            || chart.Annotations.Count != 0 || chart.PlotRegions.Count != 0 || chart.Options.ShowPointLegend
            || chart.Series.Any(series => series.Kind is not (ChartSeriesKind.Line or ChartSeriesKind.Bar or ChartSeriesKind.Scatter) || series.Id.IsAutomatic
                || series.IsVisibilityPreparation || series.RenderPoints.Count == 0 || series.DecimationMode != null || series.HistogramBinLayout != null
                || series.ColorScale != null || series.ColorDimension.HasOverrides
                || series.ResolveDataLabels(chart.Options, defaultVisible: false)
                || Enumerable.Range(0, series.RenderPoints.Count).Any(index => !series.ObservationIds.ContainsKey(index)))) return;
        if (Enumerable.Range(0, chart.Series.Count).Any(stacks.IsStacked)) return;
        var axes = new[] { chart.Options.XAxis, chart.Options.YAxis, chart.Options.SecondaryXAxis, chart.Options.SecondaryYAxis };
        if (axes.Any(axis => axis.TimeTickLabelFormatter != null || axis.ValueFormat != null && axis.ValueFormat.CapturedValuePolicy == null)
            || chart.Options.YAxis.Scale == ChartScaleKind.Time || chart.Options.SecondaryYAxis.Scale == ChartScaleKind.Time
            || chart.Options.ValueFormat.CapturedValuePolicy == null) return;
        (string Notation, string Format, string Culture) valueKey;
        try { valueKey = chart.Options.ValueFormat.PortablePolicy(); foreach (var axis in axes) _ = axis.ValueFormat?.PortablePolicy(); }
        catch (NotSupportedException) { return; } catch (ArgumentException) { return; }
        var colors = context.Theme.Resolve(context.ThemeMode); var rows = new List<CartesianValueSeries>();
        for (var index = 0; index < chart.Series.Count; index++) {
            var source = chart.Series[index]; var nativeMap = source.ValueAxis == ChartAxisSide.Secondary ? secondaryMap! : map;
            var nativeRange = source.ValueAxis == ChartAxisSide.Secondary ? secondaryRange! : range;
            var axis = source.ValueAxis == ChartAxisSide.Secondary ? chart.Options.SecondaryYAxis : chart.Options.YAxis;
            var capturedAxis = axis.Clone(); var capturedTicks = labels.Ticks(axis, nativeRange.MinY, nativeRange.MaxY).ToArray();
            var fallbackReadout = ChartAxisValueFormatter.Create(capturedAxis, capturedTicks, retainTimeDateContext: true);
            foreach (var tick in capturedTicks) _ = labels.FormatDisplay(axis, tick, capturedTicks);
            var paintedReadouts = labels.CaptureReadoutFacts(axis);
            string Readout(double value) => paintedReadouts.TryGetValue(value, out var painted) ? painted : fallbackReadout(value);
            var capturedMap = new ChartMapper(plot, nativeRange.Copy(), chart.Options.XAxis.Clone(), capturedAxis);
            var slot = source.Kind == ChartSeriesKind.Bar ? ResolveBarLayout(chart, context, plot, coordinates, nativeMap, stacks, index) : (0d, 0d);
            var style = SeriesLabelStyle(chart, context, source, colors);
            var points = new CartesianValuePoint[source.RenderPoints.Count];
            for (var point = 0; point < points.Length; point++) {
                var value = source.RenderPoints[point]; var marker = source.ResolvePointMarkers(point);
                var radius = VisualMarkerScene.Radius(source, context, marker); var color = PointColor(source, index, point, colors);
                var pattern = point < source.PointFillPatterns.Count && source.PointFillPatterns[point].HasValue ? source.PointFillPatterns[point]!.Value : source.FillPattern;
                var pointStyle = point < source.PointDataLabelStyles.Count && source.PointDataLabelStyles[point] != null ? source.PointDataLabelStyles[point]!.Resolve(style) : style;
                points[point] = new CartesianValuePoint(source.ObservationIds[point].Value, value, nativeMap.X(value.X), nativeMap.Y(value.Y),
                    point < source.PointLabels.Count ? source.PointLabels[point] : null, pointStyle.TextCase, radius, ShowMarker(chart, source, point),
                    VisualMarkerScene.CapturePaint(source, point, color, VisualChartPaint.Series(source, color, point), source.Kind == ChartSeriesKind.Scatter && !string.IsNullOrWhiteSpace(source.SemanticRole) ? source.SemanticRole! : "marker", pattern,
                        ChartStateMark.Backdrop(chart.Options, colors, context.Frame), "marker-pattern", marker),
                    new BarSurfacePolicy(chart, context, source, point, colors), color,
                    source.Kind == ChartSeriesKind.Bar ? nativeMap.X(value.X) + slot.Item2 - slot.Item1 / 2 : 0, slot.Item1,
                    VisualMarkerScene.Bounds(source, nativeMap.X(value.X), nativeMap.Y(value.Y), radius, marker));
            }
            rows.Add(new CartesianValueSeries(source.Id.Value, source.Name, source.Kind, source.ValueAxis, source.Interpolation, source.StepPosition, capturedMap,
                nativeMap.YBaseline(), capturedAxis, Readout, capturedTicks, paintedReadouts, AxisValueKey(capturedAxis), points, chart.Options.ValueUnit));
        }
        var projections = CaptureAxisProjections(chart, plot, fullRange, range, fullSecondaryRange, secondaryRange);
        if (!ChartMath.IsFinite(projections.Y.Maximum - projections.Y.Minimum)
            || projections.SecondaryY != null && !ChartMath.IsFinite(projections.SecondaryY.Maximum - projections.SecondaryY.Minimum)) return;
        var policy = new CartesianValuePolicy(context, plot, rows.ToArray(), projections, chart.Options.ValueFormat,
            valueKey, axes.Select(AxisValueKey).ToArray(), chart.Options.ValueFormatter != null, chart.Options.ClipMarksToPlot);
        // Only a detached descriptor reaches this delegate. The chart and series do not escape preparation.
        builder.CaptureExistingValue(scene => new CartesianValuePlan(policy, scene, first, last), first, last);
    }

    private static string AxisValueKey(ChartAxis axis) {
        var format = axis.ValueFormat?.PortablePolicy();
        string S(string value) => value.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + value;
        string N(double value) => value.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        var scalar = axis.Scale + ":" + axis.Reversed + ":" + axis.LogarithmicDomain + ":" + N(axis.LogarithmBase) + ":" + N(axis.SymmetricLogarithmThreshold)
            + ":" + (axis.Minimum.HasValue ? N(axis.Minimum.Value) : "null") + ":" + (axis.Maximum.HasValue ? N(axis.Maximum.Value) : "null");
        return scalar + (format.HasValue ? S(format.Value.Notation) + S(format.Value.Format) + S(format.Value.Culture) : "default")
            + string.Concat(axis.Labels.Select(label => S(N(label.Value)) + S(label.Text) + S(string.Concat(label.Groups.Select(S)))));
    }
}
