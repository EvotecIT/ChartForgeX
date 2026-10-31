using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static partial class VisualScheduleCompiler {
    /// <summary>Allocates real heterogeneous source rows through the incumbent packed-lane planner and one native axis.</summary>
    private static void MixedSchedules(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect viewport) {
        var colors = context.Theme.Resolve(context.ThemeMode);
        var model = ChartGanttLaneModel.Build(chart, viewport.Width, colors.MutedForeground, mixed: true);
        var layout = LaneLayout(chart, context, builder, viewport, model.Rows.Where(row => !row.IsGroup).Select(row => row.Name),
            model.Rows.Where(row => !row.IsGroup).Select(row => row.Summary), model.HasSummary, model.NowVisible, model.Ticks, model.FormatTick,
            hasFixedTicks: model.HasFixedTicks);
        var plot = layout.Plot; var height = Math.Min(30, model.UnitHeight(plot) * .65);
        var milestones = chart.Series.Any(series => series.GanttItems.Any(item => item.IsMilestone));
        var outline = model.Rows.SelectMany(row => row.Items).Any(item => item.Category.Pattern == ChartStatePattern.Outlined)
            ? ChartStateMark.OutlineWidth / 2 : 0;
        var minimumExtent = Math.Max(outline, milestones ? Math.Min(height, plot.Width) / 2 : 0);
        var maximumExtent = Math.Max(minimumExtent, model.Rows.SelectMany(row => row.Items).Any(item => item.Item.IsOpen) ? GanttLaneOpenEndStrokeWidth / 2 : 0);
        foreach (var series in chart.Series) {
            if (IsMarker(series)) {
                if (series.GanttItems[0].Start == model.Min) minimumExtent = Math.Max(minimumExtent, context.Theme.AxisStrokeWidth / 2);
                if (series.GanttItems[0].Start == model.Max) maximumExtent = Math.Max(maximumExtent, context.Theme.AxisStrokeWidth / 2);
            } else if (series.GanttItems.Any(item => item.Start == item.End)
                || series.Kind == ChartSeriesKind.Timeline && series.RenderPoints.Any(item => item.X == item.Y)) maximumExtent = Math.Max(maximumExtent, 1);
        }
        if (chart.ResolveGanttDependencies().Count > 0) {
            var arrow = DependencyArrowExtent(height); var clearance = DependencyClearance(context);
            minimumExtent = Math.Max(minimumExtent, (milestones ? height / 2 : 0) + clearance + arrow + context.Theme.AxisStrokeWidth / 2);
            maximumExtent = Math.Max(maximumExtent, (milestones ? height / 2 : 0) + clearance + DependencyStubLength(context, arrow) + context.Theme.AxisStrokeWidth / 2);
        }
        var projection = GuideProjection(chart, context, plot, model.Min, model.Max, minimumExtent, maximumExtent,
            model.NowVisible ? model.Now : null);
        model = model.Repack(projection.Width);
        var frame = new ScheduleCompositionFrame(model, layout, projection);
        builder.DataProjection = ChartDataProjection.TryCreate(projection, model.Min, model.Max, chart.Options.XAxis, linearOnly: true);
        builder.AddRegion(new VisualSemanticRegion("schedule-plot", "schedule-plot", plot));
        using (builder.PushGroup("schedule-composition", "schedule-composition", Window(model.Min, model.Max))) {
            ScheduleBands(chart, context, builder, plot, value => model.X(value, projection));
            Axis(chart, context, builder, viewport, layout, model.Ticks, value => model.X(value, projection), model.FormatTick,
                minimum: model.Min, maximum: model.Max);
            var tops = model.RowTops(plot); string? currentGroup = null;
            for (var index = 0; index < model.Rows.Count; index++) {
                var row = model.Rows[index]; var bounds = model.RowBounds(plot, index, tops);
                if (row.IsGroup) {
                    currentGroup = row.Name.Length == 0 ? null : row.Name;
                    if (chart.Options.ShowAxes && chart.Options.YAxis.Visible)
                        using (builder.PushGroup(null, null, currentGroup == null ? null : new Dictionary<string, string> { [ChartDomRoles.LaneGroupAttribute] = currentGroup }))
                            GroupHeader(chart, context, builder, row.Name, viewport.Left, plot.Right - viewport.Left, bounds.Top, bounds.Height,
                                ChartDomRoles.ScheduleGroup, "schedule-group-" + index);
                    continue;
                }
                LaneText(chart, context, builder, viewport, layout, row.Name, row.Summary, bounds.Top, bounds.Height, row.SeriesIndex, currentGroup, bounds.Height);
                builder.AddRegion(new VisualSemanticRegion("schedule-lane-" + row.SeriesIndex, "schedule-lane", bounds, row.Name));
            }
            if (chart.Series.Any(series => series.Kind == ChartSeriesKind.StateTimeline)) StateLanes(chart, context, builder, viewport, frame);
            if (chart.Series.Any(series => series.Kind == ChartSeriesKind.GanttLane)) GanttLanes(chart, context, builder, viewport, frame);
            if (chart.Series.Any(series => series.Kind is ChartSeriesKind.Timeline or ChartSeriesKind.Gantt))
                Ranges(chart, context, builder, viewport, chart.Series.Any(series => series.Kind == ChartSeriesKind.Gantt), frame);
            if (model.NowVisible) Now(chart, context, builder, layout, model.X(model.Now!.Value, projection), model.Now.Value);
        }
        builder.CaptureChartReadouts(chart, (_, value) => layout.FormatTick(value), (_, value, _) => layout.FormatTick(value));
    }

    private sealed class ScheduleCompositionFrame {
        internal ScheduleCompositionFrame(ChartGanttLaneModel model, ScheduleLayout layout, ChartRect projection) {
            Model = model; Layout = layout; Projection = projection; _tops = model.RowTops(layout.Plot);
        }
        private readonly double[] _tops;
        internal List<ChartRect> MarkSurfaces { get; } = new();
        internal ChartGanttLaneModel Model { get; }
        internal ScheduleLayout Layout { get; }
        internal ChartRect Projection { get; }
        internal ChartRect Row(int seriesIndex) {
            for (var index = 0; index < Model.Rows.Count; index++)
                if (!Model.Rows[index].IsGroup && Model.Rows[index].SeriesIndex == seriesIndex) return Model.RowBounds(Layout.Plot, index, _tops);
            throw new InvalidOperationException("The retained schedule source has no prepared row.");
        }
    }
}
