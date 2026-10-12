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
        if (chart.Options.ShowAxes && chart.Options.XAxis.Visible) foreach (var axis in axes) {
            var measured = builder.MeasureText(axis, style);
            width = Math.Max(width, Math.Min(plot.Width * .25, measured.Width));
            height = Math.Max(height, Math.Min(plot.Height * .2, measured.Height));
        }
        var gap = context.Theme.Spacing;
        var markExtent = chart.Series.Max(series => Math.Max(VisualMarkerScene.Extent(series, VisualMarkerScene.Radius(series, context)),
            ChartLineVisualLayers.Build(context.Theme.Resolve(context.ThemeMode).Accent,
                series.HasExplicitStrokeWidth ? series.StrokeWidth : context.Theme.SeriesStrokeWidth, chart.Options.LineVisualStyle).Max(layer => layer.StrokeWidth) / 2));
        var radius = Math.Max(0, Math.Min(plot.Width / 2 - width - gap, plot.Height / 2 - height - gap) - markExtent);
        var count = chart.Options.PolarGridRingCount;
        var axisRadius = chart.Options.ShowGrid && count.HasValue && count.Value > 0 ? radius * (count.Value / Math.Ceiling(count.Value)) : radius;
        return new PolarLayout(plot.Left + plot.Width / 2, plot.Top + plot.Height / 2, axisRadius, radius);
    }

    private static void Grid(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, PolarLayout geometry,
        double[] categories, string[] axes, Dictionary<ChartAxisSide, RadialValueScale> scales, bool radar, List<PolarLabel> labels) {
        var colors = context.Theme.Resolve(context.ThemeMode); var style = FittedTickStyle(chart, context, builder, plot, axes);
        var intervalCount = chart.Options.PolarGridRingCount;
        var polygonGrid = chart.Options.PolarGridShape == ChartPolarGridShape.Polygon ||
            chart.Options.PolarGridShape == ChartPolarGridShape.Automatic && radar;
        var ringRole = radar ? "radar-ring" : "polar-ring";
        var gridSide = scales.ContainsKey(ChartAxisSide.Primary) ? ChartAxisSide.Primary : ChartAxisSide.Secondary;
        foreach (var pair in scales.OrderBy(pair => pair.Key)) {
            var scale = pair.Value; var axis = RadialValueScale.Axis(chart, pair.Key);
            var secondary = pair.Key == ChartAxisSide.Secondary;
            var ticks = intervalCount.HasValue ? scale.EqualIntervalTicks(intervalCount.Value) : scale.Ticks;
            var ringCount = intervalCount.HasValue ? (int)Math.Ceiling(intervalCount.Value) : ticks.Count;
            var formatTick = ChartAxisValueFormatter.Create(axis, ticks,
                secondary ? chart.Options.SecondaryYAxisValueFormatter : chart.Options.ValueFormatter);
            for (var index = 0; index < ringCount; index++) {
                if (!intervalCount.HasValue && scale.Normalize(ticks[index]) <= 0) continue;
                // Fit the ceiling guide count without rounding the authored interval divisor.
                var r = intervalCount.HasValue ? geometry.GridRadius * ((index + 1d) / ringCount) : geometry.Radius * scale.Normalize(ticks[index]);
                if (chart.Options.ShowGrid && pair.Key == gridSide) {
                    if (polygonGrid) builder.Path(ChartPathBuilder.FromPoints(Enumerable.Range(0, categories.Length).Select(i => On(geometry, radar ? CategoryAngle(chart.Options.XAxis, i, categories.Length) : PolarAngle(chart.Options.XAxis, categories[i]), r)).ToArray(), ChartInterpolation.Linear),
                        stroke: colors.Border, strokeWidth: context.Theme.GridStrokeWidth, role: ringRole, close: true, paint: VisualChartPaint.Stroke(colors.Border, SvgColorRole.Grid));
                    else builder.Ellipse(geometry.Cx, geometry.Cy, r, r, null, colors.Border, context.Theme.GridStrokeWidth, ringRole, paint: VisualChartPaint.Stroke(colors.Border, SvgColorRole.Grid));
                }
                if (chart.Options.ShowAxes && axis.Visible && index < ticks.Count) {
                    var labelRadius = geometry.Radius * scale.Normalize(ticks[index]);
                    if (labelRadius >= geometry.Radius) continue;
                    var text = formatTick(ticks[index]);
                    var anchor = new ChartPoint(geometry.Cx + (secondary ? -1 : 1) * context.Theme.Spacing / 2, geometry.Cy - labelRadius);
                    var ringStyle = FitLabel(builder, style, text, plot.Width * .25, geometry.Radius / Math.Max(1, ringCount));
                    AddLabel(builder, labels, text, anchor, ringStyle, (radar ? "radar" : "polar") + "-radius-label-" + (secondary ? "secondary-" : "") + index,
                        radar ? "radar-ring-label" : "polar-radius-label", plot,
                        new[] { new LabelCandidate(0, 0, secondary ? 1 : 0, 0), new LabelCandidate(0, 0, secondary ? 1 : 0, 1) }, 20);
                }
            }
        }
        for (var index = 0; index < categories.Length; index++) {
            var angle = radar ? CategoryAngle(chart.Options.XAxis, index, categories.Length) : PolarAngle(chart.Options.XAxis, categories[index]);
            var end = On(geometry, angle, geometry.Radius);
            if (chart.Options.ShowGrid) builder.Line(geometry.Cx, geometry.Cy, end.X, end.Y, colors.Border, context.Theme.GridStrokeWidth, radar ? "radar-spoke" : "polar-spoke", paint: VisualChartPaint.Stroke(colors.Border, SvgColorRole.Grid));
            if (!chart.Options.ShowAxes || !chart.Options.XAxis.Visible) continue;
            var target = On(geometry, angle, geometry.Radius + context.Theme.Spacing);
            var horizontal = Math.Cos(angle) > .3 ? 0d : Math.Cos(angle) < -.3 ? 1d : .5;
            var vertical = Math.Sin(angle) > .3 ? 0d : Math.Sin(angle) < -.3 ? 1d : .5;
            AddLabel(builder, labels, axes[index], target, style, (radar ? "radar" : "polar") + "-axis-label-" + index,
                radar ? "radar-axis-label" : "polar-angle-label", plot, new[] { new LabelCandidate(0, 0, horizontal, vertical) }, 100);
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

    private static void AddLabel(VisualSceneBuilder builder, List<PolarLabel> labels, string text, ChartPoint anchor, TextStyle style,
        string id, string role, ChartRect bounds, IReadOnlyList<LabelCandidate> candidates, int priority, string? markId = null) {
        // The viewport describes the retained label; the placed rectangle describes only its displayed text.
        builder.AddRegion(new VisualSemanticRegion(id, role, bounds, text));
        labels.Add(new PolarLabel(new LabelPlacementRequest(text, anchor, style, candidates, priority) { MeasuredSize = builder.MeasureText(text, style) }, id, role, markId));
    }

    private static void DrawLabels(VisualSceneBuilder builder, ChartRect plot, IReadOnlyList<PolarLabel> labels, double gap) {
        var placed = new LabelPlacementService().Place(labels.Select(label => label.Request).ToArray(), plot, null, Math.Max(1, gap / 3), builder.MeasureText);
        for (var index = 0; index < placed.Count; index++) {
            var result = placed[index]; var label = labels[index];
            using (builder.PushGroup(label.Id + "-source", label.Role + "-source", VisualMarkLabel.Metadata(label.Request.Text, label.MarkId))) {
                if (result.IsDropped || result.IsEllipsized) builder.AddDiagnostic(new VisualDiagnostic("polar.label-overflow", "Polar text was shortened or omitted to fit the fixed viewport; complete text remains in descriptive regions."));
                if (result.IsDropped) continue;
                var style = result.Request.Style.Clone(); style.FontSize = style.EffectiveFontSize; style.Baseline = TextBaseline.Normal;
                style.TextCase = TextCaseTransform.None; style.Alignment = TextAlignment.Left;
                builder.Text(result.Text, result.Bounds.Left, result.Bounds.Top + builder.TextAscent(style), style, label.Role, label.Id, paint: VisualChartPaint.Text(style));
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
            axes.All(text => { var measured = builder.MeasureText(text, fitted); return measured.Width <= plot.Width * .25 && measured.Height <= plot.Height * .2; }));

    private static TextStyle FitLabel(VisualSceneBuilder builder, TextStyle style, string text, double width, double height) =>
        VisualTextFitting.FitOversized(builder, style, height, fitted => {
            var measured = builder.MeasureText(text, fitted); return measured.Width <= width && measured.Height <= height;
        });

    private sealed class PolarLayout {
        internal PolarLayout(double cx, double cy, double radius, double gridRadius) { Cx = cx; Cy = cy; Radius = radius; GridRadius = gridRadius; }
        internal double Cx { get; } internal double Cy { get; } internal double Radius { get; } internal double GridRadius { get; }
    }
    private sealed class PolarLabel {
        internal PolarLabel(LabelPlacementRequest request, string id, string role, string? markId) { Request = request; Id = id; Role = role; MarkId = markId; }
        internal LabelPlacementRequest Request { get; } internal string Id { get; } internal string Role { get; }
        internal string? MarkId { get; }
    }
}
