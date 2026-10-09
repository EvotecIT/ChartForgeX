using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    private static void DrawPreparedHorizontalBars(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot,
        ChartMapper map, ChartStackLayout stacks, int index, VisualThemeColors colors, List<LabelPlacementRequest> labels, List<LabelObstacle> obstacles) {
        var series = chart.Series[index];
        var layout = ResolveHorizontalBarLayout(chart, context, plot, map, stacks, index);
        var height = layout.Height; var offset = layout.Offset;
        for (var item = 0; item < series.Points.Count; item++) {
            var point = series.Points[item]; var stack = stacks.Point(index, item);
            var startX = stack.Base == 0 ? map.XBaseline() : map.X(stack.Base); var endX = map.X(stack.End);
            var y = map.Y(point.X) + offset;
            var bounds = VisibleSegmentBounds(chart, new ChartRect(Math.Min(startX, endX), y - height / 2, Math.Abs(endX - startX), height), stack.Value, horizontal: true);
            var label = ResolveObservationLabel(chart, context, series, item, colors, () => Value(chart, point.Y));
            using (ObservationGroup(builder, series, index, item, item, 1, bounds, label, stack, ("category", point.X), ("value", point.Y), ("base", stack.Base)))
                DrawBarSurface(chart, context, builder, series, item, bounds, PointColor(series, index, item, colors), colors, "horizontal-bar", horizontal: true);
            obstacles.Add(new LabelObstacle(PointId(index, item), bounds));
            AddHorizontalLabel(chart, context, series, index, item, new ChartPoint(endX, y), bounds, point.Y, label, labels);
        }
    }

    private static (double Height, double Offset, double Pitch) ResolveHorizontalBarLayout(Chart chart, VisualRenderContext context, ChartRect plot, ChartMapper map, ChartStackLayout stacks, int index) {
        var centers = chart.Series.SelectMany(s => s.Points.Select(point => map.Y(point.X))).Distinct().OrderBy(value => value).ToArray();
        var spacing = plot.Height;
        for (var i = 1; i < centers.Length; i++) spacing = Math.Min(spacing, centers[i] - centers[i - 1]);
        var slot = stacks.Slot(Enumerable.Range(0, chart.Series.Count).ToArray(), index);
        var count = slot.Count; var occupied = spacing * .68;
        var gap = count > 1 ? Math.Min(context.Theme.Spacing / 2, occupied / (count * 4)) : 0;
        var height = Math.Max(.1, Math.Min(30, (occupied - gap * (count - 1)) / count));
        var offset = (slot.Position - (count - 1) / 2d) * (height + gap);
        return (height, offset, spacing);
    }

    private static void AddHorizontalLabel(Chart chart, VisualRenderContext context, ChartSeries series, int index, int item, ChartPoint anchor,
        ChartRect bounds, double value, ResolvedPointLabel label, List<LabelPlacementRequest> labels) {
        if (!(series.ShowDataLabels ?? chart.Options.ShowDataLabels) || label.Text.Length == 0) return;
        var placement = series.DataLabelPlacement ?? chart.Options.DataLabelPlacement;
        if (placement == ChartDataLabelPlacement.Auto && ChartStackLayout.Participates(chart, series)) placement = ChartDataLabelPlacement.Inside;
        if (placement != ChartDataLabelPlacement.Auto) {
            if (placement == ChartDataLabelPlacement.Left) anchor = new ChartPoint(bounds.Left, anchor.Y);
            if (placement == ChartDataLabelPlacement.Right) anchor = new ChartPoint(bounds.Right, anchor.Y);
            if (placement == ChartDataLabelPlacement.Above) anchor = new ChartPoint(bounds.Left + bounds.Width / 2, bounds.Top);
            if (placement == ChartDataLabelPlacement.Below) anchor = new ChartPoint(bounds.Left + bounds.Width / 2, bounds.Bottom);
            AddLabel(chart, context, series, index, item, anchor, bounds, label, labels, value);
            return;
        }
        var spacing = context.Theme.Spacing;
        labels.Add(new LabelPlacementRequest(label.Text, anchor, label.Style, value >= 0
            ? new[] { new LabelCandidate(spacing, 0, 0, .5), new LabelCandidate(-spacing, 0, 1, .5) }
            : new[] { new LabelCandidate(-spacing, 0, 1, .5), new LabelCandidate(spacing, 0, 0, .5) }) { AssociatedMarkId = PointId(index, item) });
    }

    private static (ChartAxis Value, ChartAxis Category) HorizontalAxes(Chart chart, AxisLabelCache cache) {
        if (cache.HorizontalValueAxis != null) return (cache.HorizontalValueAxis, cache.HorizontalCategoryAxis!);
        var x = chart.Options.XAxis; var y = chart.Options.YAxis;
        var valueAxis = new ChartAxis { Scale = x.Scale, TickCount = x.TickCount, LabelAngle = x.LabelAngle,
            LabelDensity = x.LabelDensity, SymmetricLogarithmThreshold = x.SymmetricLogarithmThreshold,
            TimeZone = x.TimeZone, TimeZoneLabel = x.TimeZoneLabel, ShowTimeZone = x.ShowTimeZone,
            ValueFormat = x.ValueFormat ?? (ReferenceEquals(chart.Options.ValueFormat, ChartValueFormat.ExistingValue) ? null : chart.Options.ValueFormat) };
        var categoryAxis = new ChartAxis { LabelAngle = y.LabelAngle, LabelDensity = y.LabelDensity, ValueFormat = y.ValueFormat ?? x.ValueFormat };
        var categoryLabels = y.Labels.Count > 0 ? y.Labels : x.Labels;
        foreach (var label in categoryLabels) categoryAxis.Labels.Add(label);
        cache.HorizontalValueAxis = valueAxis; cache.HorizontalCategoryAxis = categoryAxis;
        return (valueAxis, categoryAxis);
    }

    private static IReadOnlyList<double> HorizontalCategories(Chart chart, ChartRange range) => chart.Series.SelectMany(series => series.Points)
        .Select(point => point.X).Where(value => value >= range.MinY && value <= range.MaxY).Distinct().OrderBy(value => value).ToArray();

    private static ChartRect MeasureHorizontalPlot(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect viewport,
        ChartRange range, VisualThemeColors colors, AxisLabelCache cache) {
        if (!chart.Options.ShowAxes) return viewport;
        var axes = HorizontalAxes(chart, cache); var spacing = context.Theme.Spacing;
        var style = chart.Options.TickLabelStyle.Resolve(new TextStyle { Font = context.Font, FontSize = context.Theme.Typography.AxisSize, Color = colors.MutedForeground });
        var categories = HorizontalCategories(chart, range); var widest = 0d;
        foreach (var category in categories) widest = Math.Max(widest, RotatedMetrics(builder.MeasureText(cache.Format(axes.Category, category, null, categories), style), axes.Category.LabelAngle).Width);
        var left = chart.Options.YAxis.Visible ? Math.Min(viewport.Width * .35, widest + spacing) : 0;
        var bottom = chart.Options.XAxis.Visible ? TickMetrics(builder, axes.Value, range.MinX, range.MaxX, style, chart.Options.ValueFormatter, cache).Height + spacing : 0;
        var titleStyle = chart.Options.AxisTitleStyle.Resolve(new TextStyle { Font = context.Font, FontSize = context.Theme.Typography.AxisSize, Color = colors.Foreground });
        var xTitle = XAxisTitle(chart);
        if (chart.Options.XAxis.Visible && xTitle.Length > 0) bottom += builder.MeasureText(xTitle, titleStyle).Height + spacing;
        var top = chart.Options.YAxis.Visible && chart.YAxisTitle.Length > 0 ? Math.Min(viewport.Height * .25, builder.MeasureText(chart.YAxisTitle, titleStyle).Height + spacing) : 0;
        bottom = Math.Min(viewport.Height * .45, bottom);
        return new ChartRect(viewport.Left + left, viewport.Top + top, Math.Max(0, viewport.Width - left), Math.Max(0, viewport.Height - top - bottom));
    }

    private static void DrawHorizontalAxes(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot,
        ChartRange range, ChartMapper map, VisualThemeColors colors, ChartRect viewport, AxisLabelCache cache, double categoryLabelRight) {
        var axes = HorizontalAxes(chart, cache); var xTicks = AxisTicks(axes.Value, range.MinX, range.MaxX); var categories = HorizontalCategories(chart, range);
        var spacing = context.Theme.Spacing;
        if (chart.Options.ShowGrid) {
            var grid = chart.Options.ResolvePreparedGridLineStyle(); var width = chart.Options.HasPreparedGridStrokeWidth ? grid.StrokeWidth : context.Theme.GridStrokeWidth;
            var dash = grid.Dash > 0 && grid.Gap > 0 ? new[] { grid.Dash, grid.Gap } : null;
            if (grid.ShowVerticalLines) foreach (var tick in xTicks) builder.Line(map.X(tick), plot.Top, map.X(tick), plot.Bottom,
                ChartColorMath.WithOpacity(colors.Grid, grid.VerticalOpacity), width, role: "grid-x", dash: dash,
                paint: VisualChartPaint.Stroke(SvgPaint.Of(colors.Grid, SvgColorRole.Grid).WithOpacity(ChartColorMath.WithOpacity(colors.Grid, grid.VerticalOpacity), grid.VerticalOpacity)));
            if (grid.ShowHorizontalLines) foreach (var category in categories) builder.Line(plot.Left, map.Y(category), plot.Right, map.Y(category),
                ChartColorMath.WithOpacity(colors.Grid, grid.HorizontalOpacity), width, role: "grid-y", dash: dash,
                paint: VisualChartPaint.Stroke(SvgPaint.Of(colors.Grid, SvgColorRole.Grid).WithOpacity(ChartColorMath.WithOpacity(colors.Grid, grid.HorizontalOpacity), grid.HorizontalOpacity)));
        }
        if (!chart.Options.ShowAxes) return;
        var style = chart.Options.TickLabelStyle.Resolve(new TextStyle { Font = context.Font, FontSize = context.Theme.Typography.AxisSize, Color = colors.MutedForeground });
        if (chart.Options.XAxis.Visible) {
            if (chart.Options.XAxis.ShowLine) builder.Line(plot.Left, plot.Bottom, plot.Right, plot.Bottom, colors.Axis, context.Theme.AxisStrokeWidth, role: "axis-x", paint: VisualChartPaint.Stroke(colors.Axis, SvgColorRole.Axis));
            var tickHeight = TickMetrics(builder, axes.Value, range.MinX, range.MaxX, style, chart.Options.ValueFormatter, cache).Height;
            var bounds = new ChartRect(plot.Left, plot.Bottom + spacing, plot.Width, Math.Max(0, Math.Min(tickHeight, viewport.Bottom - plot.Bottom - spacing)));
            DrawAxisLabels(builder, chart.Options, axes.Value, xTicks, map.X, true, false, bounds, style, spacing, chart.Options.ValueFormatter, cache);
            var title = XAxisTitle(chart);
            if (title.Length > 0) DrawAxisTitle(chart, context, builder, title,
                new ChartRect(plot.Left, bounds.Bottom + spacing, plot.Width, Math.Max(0, viewport.Bottom - bounds.Bottom - spacing)), colors, TextAlignment.Center, "axis-x-title");
        }
        if (chart.Options.YAxis.Visible) {
            if (chart.Options.YAxis.ShowLine) builder.Line(plot.Left, plot.Top, plot.Left, plot.Bottom, colors.Axis, context.Theme.AxisStrokeWidth, role: "axis-y", paint: VisualChartPaint.Stroke(colors.Axis, SvgColorRole.Axis));
            DrawAxisLabels(builder, chart.Options, axes.Category, categories, map.Y, false, false,
                new ChartRect(viewport.Left, plot.Top, Math.Max(0, categoryLabelRight - viewport.Left - spacing), plot.Height), style, spacing, null, cache);
            if (chart.YAxisTitle.Length > 0) DrawAxisTitle(chart, context, builder, chart.YAxisTitle,
                new ChartRect(plot.Left, viewport.Top, plot.Width, Math.Max(0, plot.Top - viewport.Top - spacing)), colors, TextAlignment.Left, "axis-y-title");
        }
    }
}
