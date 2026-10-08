using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

/// <summary>Native preparation for range schedules, state lanes and packed Gantt lanes.</summary>
internal static partial class VisualScheduleCompiler {
    internal static IReadOnlyList<VisualLegendEntry> LegendEntries(Chart chart, VisualThemeColors colors) {
        if (chart.Series.Any(series => series.Kind is ChartSeriesKind.StateTimeline or ChartSeriesKind.GanttLane))
            return chart.Options.StateCategories.Select((state, index) => new VisualLegendEntry(state.Label, state.Color, "state-" + index, state: state, pinStateColors: chart.Options.PinStateColorsInForcedColors)).ToArray();
        return chart.Series.Select((series, index) => new { Series = series, Index = index }).Where(item => item.Series.ShowInLegend)
            .Select(item => new VisualLegendEntry(item.Series.Name, VisualStateSceneTools.SeriesColor(item.Series, item.Index, colors), "series-" + item.Index,
                paint: VisualChartPaint.Series(item.Series, VisualStateSceneTools.SeriesColor(item.Series, item.Index, colors)))).ToArray();
    }

    internal static void Build(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect viewport) {
        if (viewport.Width <= 0 || viewport.Height <= 0 || chart.Series.Count == 0) return;
        var kind = chart.Series[0].Kind;
        if (chart.Series.Any(series => series.Kind != kind)) throw new InvalidOperationException("Schedule families cannot mix different layout kinds.");
        if (kind == ChartSeriesKind.StateTimeline) StateLanes(chart, context, builder, viewport);
        else if (kind == ChartSeriesKind.GanttLane) GanttLanes(chart, context, builder, viewport);
        else Ranges(chart, context, builder, viewport, kind == ChartSeriesKind.Gantt);
    }

    private static void StateLanes(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect viewport) {
        var colors = context.Theme.Resolve(context.ThemeMode);
        var backdrop = ChartStateMark.Backdrop(chart.Options, colors, context.Frame);
        var model = ChartStateTimelineModel.Build(chart, colors.MutedForeground);
        var layout = LaneLayout(chart, context, builder, viewport, model.Lanes.Select(lane => lane.Name), model.Lanes.Select(lane => lane.Summary),
            model.HasSummary, false, model.Ticks, model.FormatTick);
        var plot = layout.Plot;
        builder.AddRegion(new VisualSemanticRegion("schedule-plot", "schedule-plot", plot));
        using (builder.PushGroup("state-timeline", "state-timeline", Window(model.Min, model.Max))) {
            Axis(chart, context, builder, viewport, layout, model.Ticks, value => model.X(value, plot), model.FormatTick);
            if (chart.Options.ShowAxes && chart.Options.YAxis.Visible) foreach (var group in model.Groups)
                VisualStateSceneTools.Text(builder, group.Name, new ChartRect(viewport.Left, model.GroupTop(plot, group), plot.Right - viewport.Left, model.GroupHeight(plot, group)),
                    VisualStateSceneTools.TickStyle(chart, context), "state-timeline-group", "state-group-" + VisualStateSceneTools.Number(group.Offset));
            for (var laneIndex = 0; laneIndex < model.Lanes.Count; laneIndex++) {
                var lane = model.Lanes[laneIndex]; var series = chart.Series[lane.SeriesIndex];
                var top = model.LaneTop(plot, laneIndex); var band = Math.Max(0, model.LaneBand(plot));
                builder.AddRegion(new VisualSemanticRegion("schedule-lane-" + lane.SeriesIndex, "schedule-lane", new ChartRect(plot.Left, top, plot.Width, band), lane.Name));
                LaneText(chart, context, builder, viewport, layout, lane.Name, lane.Summary, top, band, lane.SeriesIndex);
                foreach (var segment in lane.Segments) {
                    var visible = model.TrySegmentSpan(segment, plot, out var left, out var width);
                    var bounds = visible ? new ChartRect(left, top, Math.Max(0, Math.Min(width, plot.Right - left)), band) : new ChartRect(plot.Left, top, 0, band);
                    var metadata = VisualStateSceneTools.StateMetadata(chart, segment.State);
                    metadata["data-cfx-series"] = lane.SeriesIndex.ToString(); metadata["data-cfx-point"] = segment.PointIndex.ToString();
                    metadata["data-cfx-series-key"] = series.InteractionIdentityKey;
                    metadata["data-cfx-start"] = VisualStateSceneTools.Number(segment.Start); metadata["data-cfx-end"] = VisualStateSceneTools.Number(segment.End);
                    metadata["data-cfx-label"] = lane.Name + " · " + segment.State.Label;
                    metadata["data-cfx-meta-start"] = model.FormatInstant(segment.Start); metadata["data-cfx-meta-end"] = model.FormatInstant(segment.End);
                    metadata["data-cfx-meta-duration"] = ChartStateTimelineModel.FormatDuration(segment.End - segment.Start);
                    if (!string.IsNullOrWhiteSpace(segment.Detail)) metadata["data-cfx-meta-detail"] = segment.Detail!;
                    var sources = Enumerable.Range(0, series.Points.Count).Where(index => series.Points[index].X >= segment.Start && series.Points[index].Y <= segment.End &&
                        series.PointLabels[index] == segment.State.Key).ToArray();
                    metadata["data-cfx-source-points"] = string.Join(",", sources);
                    var id = VisualStateSceneTools.SourceId(lane.SeriesIndex, segment.PointIndex);
                    using (VisualStateSceneTools.Mark(builder, id, "state-timeline-segment", bounds, model.SegmentSummary(lane, segment), metadata)) {
                        if (!visible) continue;
                        using (builder.PushClip(plot)) VisualStateSceneTools.StateRect(builder, bounds, ChartStateMark.For(segment.State, backdrop), ChartStateTimelineModel.SegmentRadius, "state-timeline-segment-shape");
                    }
                }
            }
        }
    }

    private static void GanttLanes(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect viewport) {
        var colors = context.Theme.Resolve(context.ThemeMode);
        var backdrop = ChartStateMark.Backdrop(chart.Options, colors, context.Frame);
        // Both builds happen during preparation. The supplied current-time value is read once and never consulted by exporters.
        var model = ChartGanttLaneModel.Build(chart, viewport.Width, colors.MutedForeground);
        var layout = LaneLayout(chart, context, builder, viewport, model.Rows.Where(row => !row.IsGroup).Select(row => row.Name),
            model.Rows.Where(row => !row.IsGroup).Select(row => row.Summary), model.HasSummary, model.NowVisible, model.Ticks, model.FormatTick);
        var outlined = model.Rows.SelectMany(row => row.Items).Any(item => item.Category.Pattern == ChartStatePattern.Outlined);
        var open = model.Rows.SelectMany(row => row.Items).Any(item => item.Item.IsOpen);
        var outlineExtent = outlined ? ChartStateMark.OutlineWidth / 2 : 0;
        var minimumExtent = Math.Max(outlineExtent, model.Now == model.Min ? context.Theme.AxisStrokeWidth / 2 : 0);
        var maximumExtent = Math.Max(Math.Max(outlineExtent, open ? GanttLaneOpenEndStrokeWidth / 2 : 0),
            model.Now == model.Max ? context.Theme.AxisStrokeWidth / 2 : 0);
        var projection = MarkProjection(chart.Options.XAxis, layout.Plot, minimumExtent, maximumExtent);
        model = model.Repack(projection.Width);
        var plot = layout.Plot; var tops = model.RowTops(plot); var band = model.Band(plot);
        builder.AddRegion(new VisualSemanticRegion("schedule-plot", "schedule-plot", plot));
        using (builder.PushGroup("gantt-lanes", "gantt-lanes", Window(model.Min, model.Max))) {
            Axis(chart, context, builder, viewport, layout, model.Ticks, value => model.X(value, projection), model.FormatTick);
            if (model.NowVisible) Now(chart, context, builder, layout, model.X(model.Now!.Value, projection), model.Now.Value);
            for (var rowIndex = 0; rowIndex < model.Rows.Count; rowIndex++) {
                var row = model.Rows[rowIndex];
                if (row.IsGroup) {
                    if (chart.Options.ShowAxes && chart.Options.YAxis.Visible) VisualStateSceneTools.Text(builder, row.Name,
                        new ChartRect(viewport.Left, tops[rowIndex], plot.Right - viewport.Left, Math.Max(0, tops[rowIndex + 1] - tops[rowIndex])),
                        VisualStateSceneTools.TickStyle(chart, context), "gantt-lane-group", "gantt-lane-group-" + rowIndex);
                    continue;
                }
                LaneText(chart, context, builder, viewport, layout, row.Name, row.Summary, tops[rowIndex], tops[rowIndex + 1] - tops[rowIndex], row.SeriesIndex);
                var series = chart.Series[row.SeriesIndex];
                foreach (var placed in row.Items) {
                    var visible = model.TrySpan(placed, projection, out var left, out var width);
                    var top = visible ? model.BarTop(plot, tops[rowIndex], placed.SubRow) : tops[rowIndex];
                    var bounds = new ChartRect(visible ? left : plot.Left, top, visible ? Math.Max(0, Math.Min(width, plot.Right - left)) : 0, Math.Max(0, band));
                    var metadata = VisualStateSceneTools.StateMetadata(chart, placed.Category);
                    metadata["data-cfx-series"] = row.SeriesIndex.ToString(); metadata["data-cfx-point"] = placed.PointIndex.ToString();
                    metadata["data-cfx-series-key"] = series.InteractionIdentityKey;
                    metadata["data-cfx-start"] = VisualStateSceneTools.Number(placed.Item.Start); metadata["data-cfx-end"] = VisualStateSceneTools.Number(placed.End);
                    metadata["data-cfx-meta-start"] = ChartTimeScale.FormatInstant(chart.Options.XAxis, placed.Item.Start);
                    metadata["data-cfx-meta-end"] = ChartTimeScale.FormatInstant(chart.Options.XAxis, placed.End);
                    metadata["data-cfx-meta-duration"] = ChartStateTimelineModel.FormatDuration(placed.End - placed.Item.Start);
                    if (!string.IsNullOrWhiteSpace(placed.Item.Detail)) metadata["data-cfx-meta-detail"] = placed.Item.Detail!;
                    metadata["data-cfx-open"] = placed.Item.IsOpen ? "true" : "false"; metadata["data-cfx-sub-row"] = placed.SubRow.ToString();
                    var id = VisualStateSceneTools.SourceId(row.SeriesIndex, placed.PointIndex);
                    using (VisualStateSceneTools.Mark(builder, id, "gantt-lane-item", bounds, model.ItemSummary(row, placed), metadata)) {
                        if (!visible) continue;
                        var mark = ChartStateMark.For(placed.Category, backdrop);
                        using (builder.PushClip(plot)) {
                            VisualStateSceneTools.StateRect(builder, bounds, mark, ChartGanttLaneModel.BarRadius, "gantt-lane-item-shape");
                            if (placed.Item.IsOpen) {
                                var extent = Math.Min(bounds.Height / 3, bounds.Width / 2);
                                builder.Path(new ChartPath(new[] { ChartPathCommand.MoveTo(bounds.Right - extent, bounds.Top + bounds.Height / 2 - extent),
                                    ChartPathCommand.LineTo(bounds.Right, bounds.Top + bounds.Height / 2), ChartPathCommand.LineTo(bounds.Right - extent, bounds.Top + bounds.Height / 2 + extent) }),
                                    stroke: ChartColorMath.AccessibleTextOnBackground(mark.Surface), strokeWidth: GanttLaneOpenEndStrokeWidth, role: "gantt-lane-open-end",
                                    paint: VisualChartPaint.Stroke(SvgPaint.Literal(ChartColorMath.AccessibleTextOnBackground(mark.Surface))));
                            }
                        }
                        if (!string.IsNullOrWhiteSpace(placed.Item.Label)) DataLabel(chart, context, builder, series, placed.PointIndex, placed.Item.Label!, bounds, plot, colors, mark.Surface,
                            ChartMarkText.OnStateMark(chart, colors, context.Frame, mark));
                    }
                }
            }
        }
    }

    private static Dictionary<string, string> Window(double min, double max) => new() {
        ["data-cfx-min"] = VisualStateSceneTools.Number(min), ["data-cfx-max"] = VisualStateSceneTools.Number(max)
    };
}
