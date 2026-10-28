using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualPolarCompiler {
    private static PolarLayout Layout(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, IReadOnlyList<string> axes) {
        var style = FittedTickStyle(chart, context, builder, plot, axes);
        var width = 0d; var height = 0d;
        if (chart.Options.ShowAxes && chart.Options.XAxis.Visible) for (var index = 0; index < axes.Count; index++) {
            var measured = VisualAxisText.RotatedMetrics(builder.MeasureText(axes[index], style),
                VisualAxisText.RadialRotation(chart.Options.XAxis, AxisAngle(chart, index, axes.Count), true));
            width = Math.Max(width, Math.Min(plot.Width * .25, measured.Width));
            height = Math.Max(height, Math.Min(plot.Height * .2, measured.Height));
        }
        var gap = context.Theme.Spacing;
        var colors = context.Theme.Resolve(context.ThemeMode);
        var markExtent = chart.Series.Select((series, index) => Math.Max(VisualMarkerScene.Extent(series, VisualMarkerScene.Radius(series, context)),
            VisualPolarSeriesScene.StrokeExtent(chart, context, series, ChartSeriesColours.Resolve(series, index, colors)) / 2)).Max();
        if (chart.Options.ShowGrid) markExtent = Math.Max(markExtent, Math.Max(
            VisualReferenceStroke.PaintedWidth(chart.Options.PolarAxisStroke, colors.Axis, context.Theme.AxisStrokeWidth),
            VisualReferenceStroke.PaintedWidth(chart.Options.PolarGridStroke, colors.Grid, context.Theme.GridStrokeWidth)) / 2);
        var radius = Math.Max(0, Math.Min(plot.Width / 2 - width - gap, plot.Height / 2 - height - gap) - markExtent);
        var count = chart.Options.PolarGridRingCount;
        var guideFraction = chart.Options.ShowGrid && count.HasValue && count.Value > 0 ? count.Value / Math.Ceiling(count.Value) : 1;
        var extent = 1d;
        if (chart.Options.ShowGrid) extent = Math.Max(extent, guideFraction * chart.Options.PolarAxisScaleFactor);
        if (chart.Options.ShowAxes && chart.Options.XAxis.Visible) extent = Math.Max(extent, guideFraction * chart.Options.PolarAxisLabelFactor);
        // Divide only after computing the bounded fraction, including subnormal interval counts.
        var axisRadius = radius * guideFraction / extent;
        return new PolarLayout(plot.Left + plot.Width / 2, plot.Top + plot.Height / 2, axisRadius, radius / extent, radius);
    }

    private static void Grid(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, PolarLayout geometry,
        double[] categories, string[] axes, Dictionary<ChartAxisSide, RadialValueScale> scales, bool radar, List<PolarLabel> labels) {
        var colors = context.Theme.Resolve(context.ThemeMode); var style = FittedTickStyle(chart, context, builder, plot, axes);
        var gridStroke = chart.Options.PolarGridStroke; var axisStroke = chart.Options.PolarAxisStroke;
        var gridPaint = VisualReferenceStroke.Paint(gridStroke, colors.Grid, SvgColorRole.Grid, out var gridColor);
        var axisPaint = VisualReferenceStroke.Paint(axisStroke, colors.Axis, SvgColorRole.Axis, out var axisColor);
        var gridWidth = VisualReferenceStroke.PaintedWidth(gridStroke, colors.Grid, context.Theme.GridStrokeWidth);
        var axisWidth = VisualReferenceStroke.PaintedWidth(axisStroke, colors.Axis, context.Theme.AxisStrokeWidth);
        var fillOpacity = chart.Options.PolarGridFillOpacity ?? (chart.Options.PolarGridFill.HasValue ? 1 : 0);
        var fillSource = chart.Options.PolarGridFill ?? gridStroke?.Color ?? colors.Grid;
        ChartColor? fill = fillOpacity > 0 ? ChartColorMath.WithOpacity(fillSource, fillOpacity) : (ChartColor?)null;
        if (fill.HasValue) {
            var fillPaint = chart.Options.PolarGridFill.HasValue || gridStroke?.Color != null
                ? SvgPaint.Literal(fillSource) : SvgPaint.Of(fillSource, SvgColorRole.Grid);
            gridPaint = new VisualScenePaintBinding(fillPaint.WithOpacity(fill.Value, fillOpacity), gridPaint.Stroke);
        }
        var intervalCount = chart.Options.PolarGridRingCount;
        var polygonGrid = chart.Options.PolarGridShape == ChartPolarGridShape.Polygon ||
            chart.Options.PolarGridShape == ChartPolarGridShape.Automatic && radar;
        var ringRole = radar ? "radar-ring" : "polar-ring";
        var gridSide = scales.ContainsKey(ChartAxisSide.Primary) ? ChartAxisSide.Primary : ChartAxisSide.Secondary;
        foreach (var pair in scales.OrderBy(pair => pair.Key)) {
            var scale = pair.Value; var axis = RadialValueScale.Axis(chart, pair.Key);
            var secondary = pair.Key == ChartAxisSide.Secondary;
            using var gridClip = chart.Options.ShowGrid && pair.Key == gridSide ? PolarRadiusClip(chart, builder, geometry, pair.Key) : null;
            var ticks = intervalCount.HasValue ? scale.EqualIntervalTicks(intervalCount.Value) : scale.Ticks;
            var ringCount = intervalCount.HasValue ? (int)Math.Ceiling(intervalCount.Value) : ticks.Count;
            var formatTick = ChartAxisValueFormatter.Create(axis, ticks,
                secondary ? chart.Options.SecondaryYAxisValueFormatter : chart.Options.ValueFormatter);
            for (var index = 0; index < ringCount; index++) {
                if (!intervalCount.HasValue && (radar ? scale.Normalize(ticks[index]) : PolarRadius(chart, pair.Key, scale, ticks[index])) <= 0) continue;
                // Fit the ceiling guide count without rounding the authored interval divisor.
                var r = intervalCount.HasValue ? PolarGuideRadius(chart, geometry, pair.Key, (index + 1d) / ringCount)
                    : geometry.Radius * (radar ? scale.Normalize(ticks[index]) : PolarRadius(chart, pair.Key, scale, ticks[index]));
                if (chart.Options.ShowGrid && pair.Key == gridSide) {
                    if (polygonGrid) builder.Path(ChartPathBuilder.FromPoints(Enumerable.Range(0, categories.Length).Select(i => On(geometry, radar ? CategoryAngle(chart, i, categories.Length) : PolarAngle(chart, categories[i]), r)).ToArray(), ChartInterpolation.Linear),
                        fill: fill, stroke: gridColor, strokeWidth: gridWidth, role: ringRole, close: true, dash: gridStroke?.ResolveDash(Array.Empty<double>()), paint: gridPaint);
                    else if (gridStroke?.Dash != null && gridStroke.Dash.Count > 0)
                        builder.Path(ChartPathBuilder.Ellipse(geometry.Cx, geometry.Cy, r, r), fill, gridColor, gridWidth, ringRole, close: true, dash: gridStroke.ResolveDash(Array.Empty<double>()), paint: gridPaint);
                    else builder.Ellipse(geometry.Cx, geometry.Cy, r, r, fill, gridColor, gridWidth, ringRole, paint: gridPaint);
                }
                if (chart.Options.ShowAxes && axis.Visible && index < ticks.Count) {
                    var labelRadius = geometry.Radius * (radar ? scale.Normalize(ticks[index]) : PolarRadius(chart, pair.Key, scale, ticks[index]));
                    if (labelRadius >= geometry.Radius) continue;
                    var text = formatTick(ticks[index]);
                    var ray = -Math.PI / 2 + Rotation(chart);
                    var target = On(geometry, ray, labelRadius); var side = secondary ? -1 : 1;
                    // Preserve the exact existing anchor arithmetic at the default orientation.
                    var anchor = chart.Options.PolarRotationDegrees == 0
                        ? new ChartPoint(geometry.Cx + side * context.Theme.Spacing / 2, geometry.Cy - labelRadius)
                        : new ChartPoint(target.X - Math.Sin(ray) * side * context.Theme.Spacing / 2, target.Y + Math.Cos(ray) * side * context.Theme.Spacing / 2);
                    var rotation = VisualAxisText.RadialRotation(axis, ray, false);
                    var ringStyle = FitLabel(builder, style, text, plot.Width * .25, geometry.Radius / Math.Max(1, ringCount), rotation);
                    var label = AddLabel(builder, labels, text, anchor, ringStyle, (radar ? "radar" : "polar") + "-radius-label-" + (secondary ? "secondary-" : "") + index,
                        radar ? "radar-ring-label" : "polar-radius-label", plot,
                        new[] { new LabelCandidate(0, 0, secondary ? 1 : 0, 0), new LabelCandidate(0, 0, secondary ? 1 : 0, 1) }, 20);
                    ConfigureAxisLabel(label, axis, ray, false, context.Theme.Spacing);
                }
            }
        }
        using var spokeClip = chart.Options.ShowGrid ? PolarRadiusClip(chart, builder, geometry, gridSide) : null;
        for (var index = 0; index < categories.Length; index++) {
            var angle = radar ? CategoryAngle(chart, index, categories.Length) : PolarAngle(chart, categories[index]);
            if (chart.Options.ShowGrid) {
                var end = On(geometry, angle, geometry.Radius * chart.Options.PolarAxisScaleFactor);
                builder.Line(geometry.Cx, geometry.Cy, end.X, end.Y, axisColor, axisWidth, radar ? "radar-spoke" : "polar-spoke", dash: axisStroke?.ResolveDash(Array.Empty<double>()), paint: axisPaint);
            }
            if (!chart.Options.ShowAxes || !chart.Options.XAxis.Visible) continue;
            var target = On(geometry, angle, geometry.Radius * chart.Options.PolarAxisLabelFactor + context.Theme.Spacing);
            var horizontal = Math.Cos(angle) > .3 ? 0d : Math.Cos(angle) < -.3 ? 1d : .5;
            var vertical = Math.Sin(angle) > .3 ? 0d : Math.Sin(angle) < -.3 ? 1d : .5;
            var label = AddLabel(builder, labels, axes[index], target, style, (radar ? "radar" : "polar") + "-axis-label-" + index,
                radar ? "radar-axis-label" : "polar-angle-label", plot, new[] { new LabelCandidate(0, 0, horizontal, vertical) }, 100);
            ConfigureAxisLabel(label, chart.Options.XAxis, angle, true, context.Theme.Spacing);
        }
    }

    private static void AddDataLabel(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, List<PolarLabel> labels,
        ChartSeries series, int source, string id, string text, ChartPoint point, double angle, bool radar, int seriesIndex, int seriesCount) {
        var style = FitLabel(builder, DataStyle(chart, context, series, source), text, plot.Width * .25, Math.Min(plot.Width, plot.Height) * .1);
        var gap = context.Theme.Spacing; var placement = series.DataLabelPlacement ?? chart.Options.DataLabelPlacement;
        LabelCandidate first;
        if (placement is ChartDataLabelPlacement.Center or ChartDataLabelPlacement.Inside) first = new LabelCandidate(0, 0, .5, .5);
        else if (placement == ChartDataLabelPlacement.Left) first = new LabelCandidate(-gap, 0, 1, .5);
        else if (placement is ChartDataLabelPlacement.Right or ChartDataLabelPlacement.Outside) first = new LabelCandidate(gap, 0, 0, .5);
        else if (placement == ChartDataLabelPlacement.Above) first = new LabelCandidate(0, -gap, .5, 1);
        else if (placement == ChartDataLabelPlacement.Below) first = new LabelCandidate(0, gap, .5, 0);
        else {
            var spread = radar ? (seriesIndex - (seriesCount - 1) / 2d) * gap : 0;
            var inward = radar ? -gap : gap;
            first = new LabelCandidate(Math.Cos(angle) * inward - Math.Sin(angle) * spread, Math.Sin(angle) * inward + Math.Cos(angle) * spread, .5, .5);
        }
        AddLabel(builder, labels, text, point, style, id + "-label", radar ? "radar-data-label" : "polar-data-label", plot,
            new[] { first, new LabelCandidate(0, -gap, .5, 1), new LabelCandidate(0, gap, .5, 0), new LabelCandidate(-gap, 0, 1, .5), new LabelCandidate(gap, 0, 0, .5) }, 40,
            source >= 0 ? id : null);
    }

    private static LabelPlacementRequest AddLabel(VisualSceneBuilder builder, List<PolarLabel> labels, string text, ChartPoint anchor, TextStyle style,
        string id, string role, ChartRect bounds, IReadOnlyList<LabelCandidate> candidates, int priority, string? markId = null) {
        // The viewport describes the retained label; the placed rectangle describes only its displayed text.
        builder.AddRegion(new VisualSemanticRegion(id, role, bounds, text));
        var request = new LabelPlacementRequest(text, anchor, style, candidates, priority) { MeasuredSize = builder.MeasureText(text, style) };
        labels.Add(new PolarLabel(request, id, role, markId)); return request;
    }

    private static void DrawLabels(VisualSceneBuilder builder, ChartRect plot, IReadOnlyList<PolarLabel> labels, double gap) {
        var placed = new LabelPlacementService().Place(labels.Select(label => label.Request).ToArray(), plot, null, Math.Max(1, gap / 3), builder.MeasureText);
        for (var index = 0; index < placed.Count; index++) {
            var result = placed[index]; var label = labels[index];
            using (builder.PushGroup(label.Id + "-source", label.Role + "-source", VisualMarkLabel.Metadata(label.Request.Text, label.MarkId))) {
                if (result.IsDropped || result.IsEllipsized) builder.AddDiagnostic(new VisualDiagnostic("polar.label-overflow", "Polar text was shortened or omitted to fit the fixed viewport; complete text remains in descriptive regions."));
                if (result.IsDropped) continue;
                VisualAxisText.Draw(builder, result, label.Role, label.Id);
            }
        }
    }

    private static TextStyle TickStyle(Chart chart, VisualRenderContext context) => chart.Options.TickLabelStyle.Resolve(new TextStyle {
        Font = context.Font, FontSize = context.Theme.Typography.AxisSize, Color = context.Theme.Resolve(context.ThemeMode).MutedForeground
    });

    private static TextStyle DataStyle(Chart chart, VisualRenderContext context, ChartSeries series, int point) {
        var style = series.DataLabelStyle.Resolve(chart.Options.DataLabelStyle.Resolve(new TextStyle {
            Font = context.Font, FontSize = context.Theme.Typography.DataLabelSize, Color = context.Theme.Resolve(context.ThemeMode).Foreground
        }));
        return point >= 0 && point < series.PointDataLabelStyles.Count && series.PointDataLabelStyles[point] != null ? series.PointDataLabelStyles[point]!.Resolve(style) : style;
    }

    private static TextStyle FittedTickStyle(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, IReadOnlyList<string> axes) =>
        VisualTextFitting.FitOversized(builder, TickStyle(chart, context), plot.Height * .2, fitted =>
            axes.Select((text, index) => VisualAxisText.RotatedMetrics(builder.MeasureText(text, fitted),
                VisualAxisText.RadialRotation(chart.Options.XAxis, AxisAngle(chart, index, axes.Count), true)))
                .All(measured => measured.Width <= plot.Width * .25 && measured.Height <= plot.Height * .2));

    private static double AxisAngle(Chart chart, int index, int count) => chart.Series[0].Kind == ChartSeriesKind.Radar
        ? CategoryAngle(chart, index, count) : PolarAngle(chart, Math.PI * 2 * index / count);

    private static void ConfigureAxisLabel(LabelPlacementRequest request, ChartAxis axis, double angle, bool angular, double spacing) {
        // Default dedicated-polar density keeps its legacy gap. Explicit density and rotation reuse the canonical axis owner.
        if (axis.LabelDensity != ChartLabelDensity.Auto) VisualAxisText.ConfigureRadialLabel(request, axis, angle, angular, spacing);
        else request.RotationDegrees = VisualAxisText.RadialRotation(axis, angle, angular);
    }

    private static TextStyle FitLabel(VisualSceneBuilder builder, TextStyle style, string text, double width, double height, double angle = 0) =>
        VisualTextFitting.FitOversized(builder, style, height, fitted => {
            var measured = VisualAxisText.RotatedMetrics(builder.MeasureText(text, fitted), angle); return measured.Width <= width && measured.Height <= height;
        });

    private sealed class PolarLayout {
        internal PolarLayout(double cx, double cy, double radius, double gridRadius, double availableRadius) { Cx = cx; Cy = cy; Radius = radius; GridRadius = gridRadius; AvailableRadius = availableRadius; }
        internal double Cx { get; } internal double Cy { get; } internal double Radius { get; } internal double GridRadius { get; }
        internal double AvailableRadius { get; }
    }
    private sealed class PolarLabel {
        internal PolarLabel(LabelPlacementRequest request, string id, string role, string? markId) { Request = request; Id = id; Role = role; MarkId = markId; }
        internal LabelPlacementRequest Request { get; } internal string Id { get; } internal string Role { get; }
        internal string? MarkId { get; }
    }
}
