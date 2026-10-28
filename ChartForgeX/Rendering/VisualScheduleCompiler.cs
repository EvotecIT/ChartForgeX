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
        if (ChartSeriesKindTraits.IsMixedSchedule(chart)) {
            var states = chart.Series.Any(series => series.Kind is ChartSeriesKind.StateTimeline or ChartSeriesKind.GanttLane)
                ? chart.Options.StateCategories.Select((state, index) => new VisualLegendEntry(state.Label, state.Color, "state-" + index, state: state, pinStateColors: chart.Options.PinStateColorsInForcedColors))
                : Enumerable.Empty<VisualLegendEntry>();
            return states.Concat(chart.Series.Select((series, index) => new { Series = series, Index = index })
                .Where(item => (item.Series.Kind is ChartSeriesKind.Gantt or ChartSeriesKind.Timeline) && item.Series.ShowInLegend && item.Series.HasSourceData)
                .Select(item => RangeLegendEntry(item.Series, item.Index, colors))).ToArray();
        }
        if (chart.Series.Any(series => series.Kind is ChartSeriesKind.StateTimeline or ChartSeriesKind.GanttLane))
            return chart.Options.StateCategories.Select((state, index) => new VisualLegendEntry(state.Label, state.Color, "state-" + index, state: state, pinStateColors: chart.Options.PinStateColorsInForcedColors)).ToArray();
        return chart.Series.Select((series, index) => new { Series = series, Index = index }).Where(item => item.Series.ShowInLegend && item.Series.HasSourceData)
            .Select(item => RangeLegendEntry(item.Series, item.Index, colors)).ToArray();
    }

    private static VisualLegendEntry RangeLegendEntry(ChartSeries series, int index, VisualThemeColors colors) {
        var marker = IsMarker(series);
        var color = marker ? MarkerColor(series, colors) : VisualStateSceneTools.SeriesColor(series, index, colors);
        var paint = marker ? SvgPaint.Of(color, MarkerColorRole(series)) : VisualChartPaint.Series(series, color);
        return new VisualLegendEntry(series.Name, color, "series-" + index, stateRole: series.StateRole, seriesKey: series.InteractionIdentityKey, paint: paint);
    }

    internal static void Build(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect viewport) {
        if (viewport.Width <= 0 || viewport.Height <= 0 || chart.Series.Count == 0) return;
        if (chart.Options.SecondaryYAxis.Reversed)
            throw new NotSupportedException("Schedule layouts do not own a secondary value axis.");
        if (ChartSeriesKindTraits.IsMixedSchedule(chart)) { MixedSchedules(chart, context, builder, viewport); return; }
        var kind = chart.Series[0].Kind;
        if (chart.Series.Any(series => series.Kind != kind)) throw new InvalidOperationException("Schedule families cannot mix different layout kinds.");
        if ((kind is ChartSeriesKind.Gantt or ChartSeriesKind.Timeline) && chart.Series.All(series => !series.HasSourceData)) {
            builder.AddDiagnostic(new VisualDiagnostic("schedule.no-data", "Range schedules need a schedule item.")); return;
        }
        if (kind == ChartSeriesKind.StateTimeline) StateLanes(chart, context, builder, viewport);
        else if (kind == ChartSeriesKind.GanttLane) GanttLanes(chart, context, builder, viewport);
        else Ranges(chart, context, builder, viewport, kind == ChartSeriesKind.Gantt);
    }

    private static void StateLanes(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect viewport, ScheduleCompositionFrame? frame = null) {
        var colors = context.Theme.Resolve(context.ThemeMode);
        var backdrop = ChartStateMark.Backdrop(chart.Options, colors, context.Frame);
        var model = ChartStateTimelineModel.Build(chart, colors.MutedForeground, frame == null ? null : (frame.Model.Min, frame.Model.Max));
        var layout = frame?.Layout ?? LaneLayout(chart, context, builder, viewport, model.Lanes.Select(lane => lane.Name), model.Lanes.Select(lane => lane.Summary),
            model.HasSummary, false, model.Ticks, model.FormatTick);
        var plot = layout.Plot;
        if (frame == null) {
            builder.DataProjection = ChartDataProjection.TryCreate(plot, model.Min, model.Max, chart.Options.XAxis, linearOnly: true);
            builder.AddRegion(new VisualSemanticRegion("schedule-plot", "schedule-plot", plot));
        }
        using (builder.PushGroup("state-timeline", "state-timeline", Window(model.Min, model.Max))) {
            if (frame == null) Axis(chart, context, builder, viewport, layout, model.Ticks, value => model.X(value, plot), model.FormatTick);
            if (frame == null && chart.Options.ShowAxes && chart.Options.YAxis.Visible) foreach (var group in model.Groups)
                using (builder.PushGroup(null, null, group.Name.Length == 0 ? null : new Dictionary<string, string> { [ChartDomRoles.LaneGroupAttribute] = group.Name }))
                    GroupHeader(chart, context, builder, group.Name, viewport.Left, plot.Right - viewport.Left, model.GroupTop(plot, group), model.GroupHeight(plot, group),
                        ChartDomRoles.StateTimelineGroup, "state-group-" + VisualStateSceneTools.Number(group.Offset));
            for (var laneIndex = 0; laneIndex < model.Lanes.Count; laneIndex++) {
                var lane = model.Lanes[laneIndex]; var series = chart.Series[lane.SeriesIndex];
                var row = frame?.Row(lane.SeriesIndex);
                var band = row.HasValue ? Math.Min(row.Value.Height * .9, Math.Max(ChartStateTimelineModel.LaneBandMinimum,
                    Math.Min(ChartStateTimelineModel.LaneBandMaximum, row.Value.Height * .72))) : Math.Max(0, model.LaneBand(plot));
                var top = row.HasValue ? row.Value.Top + (row.Value.Height - band) / 2 : model.LaneTop(plot, laneIndex);
                if (frame == null) builder.AddRegion(new VisualSemanticRegion("schedule-lane-" + lane.SeriesIndex, "schedule-lane", new ChartRect(plot.Left, top, plot.Width, band), lane.Name));
                if (frame == null) LaneText(chart, context, builder, viewport, layout, lane.Name, lane.Summary, top, band, lane.SeriesIndex, lane.Group, model.LaneSlot(plot));
                foreach (var segment in lane.Segments) {
                    var visible = model.TrySegmentSpan(segment, frame?.Projection ?? plot, out var left, out var width);
                    var bounds = visible ? new ChartRect(left, top, Math.Max(0, Math.Min(width, plot.Right - left)), band) : new ChartRect(plot.Left, top, 0, band);
                    if (visible) frame?.MarkSurfaces.Add(bounds);
                    var metadata = VisualStateSceneTools.StateMetadata(chart, segment.State);
                    metadata["data-cfx-series"] = lane.SeriesIndex.ToString(); metadata["data-cfx-point"] = segment.PointIndex.ToString();
                    metadata["data-cfx-series-key"] = series.InteractionIdentityKey; metadata[ChartDomRoles.LaneIdAttribute] = series.InteractionIdentityKey;
                    metadata["data-cfx-start"] = VisualStateSceneTools.Number(segment.Start); metadata["data-cfx-end"] = VisualStateSceneTools.Number(segment.End);
                    metadata["data-cfx-label"] = lane.Name + " · " + segment.State.Label;
                    metadata["data-cfx-meta-start"] = model.FormatInstant(segment.Start); metadata["data-cfx-meta-end"] = model.FormatInstant(segment.End);
                    metadata["data-cfx-meta-duration"] = ChartStateTimelineModel.FormatDuration(segment.End - segment.Start);
                    if (!string.IsNullOrWhiteSpace(segment.Detail)) metadata["data-cfx-meta-detail"] = segment.Detail!;
                    metadata["data-cfx-source-points"] = string.Join(",", segment.SourceIndices);
                    metadata["data-cfx-derived"] = "timeline-run";
                    var id = VisualStateSceneTools.SourceId(lane.SeriesIndex, segment.PointIndex);
                    using (VisualStateSceneTools.Mark(builder, id, ChartDomRoles.StateTimelineSegment, bounds, model.SegmentSummary(lane, segment), metadata)) {
                        if (!visible) continue;
                        using (builder.PushClip(plot)) VisualStateSceneTools.StateRect(builder, bounds, ChartStateMark.For(segment.State, backdrop), ChartStateTimelineModel.SegmentRadius, ChartDomRoles.StateTimelineSegmentShape);
                    }
                }
            }
        }
        builder.CaptureChartReadouts(chart, (_, value) => layout.FormatTick(value), (_, value, _) => layout.FormatTick(value));
    }

    private static void GanttLanes(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect viewport, ScheduleCompositionFrame? frame = null) {
        var colors = context.Theme.Resolve(context.ThemeMode);
        var backdrop = ChartStateMark.Backdrop(chart.Options, colors, context.Frame);
        // Both builds happen during preparation. The supplied current-time value is read once and never consulted by exporters.
        var model = frame?.Model ?? ChartGanttLaneModel.Build(chart, viewport.Width, colors.MutedForeground);
        var layout = frame?.Layout ?? LaneLayout(chart, context, builder, viewport, model.Rows.Where(row => !row.IsGroup).Select(row => row.Name),
            model.Rows.Where(row => !row.IsGroup).Select(row => row.Summary), model.HasSummary, model.NowVisible, model.Ticks, model.FormatTick);
        var outlined = model.Rows.SelectMany(row => row.Items).Any(item => item.Category.Pattern == ChartStatePattern.Outlined);
        var open = model.Rows.SelectMany(row => row.Items).Any(item => item.Item.IsOpen);
        var outlineExtent = outlined ? ChartStateMark.OutlineWidth / 2 : 0;
        var minimumExtent = outlineExtent;
        var maximumExtent = Math.Max(outlineExtent, open ? GanttLaneOpenEndStrokeWidth / 2 : 0);
        var projection = frame?.Projection ?? GuideProjection(chart, context, layout.Plot, model.Min, model.Max,
            minimumExtent, maximumExtent, model.NowVisible ? model.Now : null, lanes: true);
        if (frame == null) model = model.Repack(projection.Width);
        var plot = layout.Plot; var tops = model.RowTops(plot);
        var minimumCaptionBand = GanttLaneCaptionBand(chart, context, builder, model, plot, projection);
        var items = GanttLaneLabels(chart, context, builder, model, plot, projection, tops, minimumCaptionBand);
        if (frame == null) {
            builder.DataProjection = ChartDataProjection.TryCreate(projection, model.Min, model.Max, chart.Options.XAxis, linearOnly: true);
            builder.AddRegion(new VisualSemanticRegion("schedule-plot", "schedule-plot", plot));
        }
        using (builder.PushGroup("gantt-lanes", "gantt-lanes", Window(model.Min, model.Max))) {
            if (frame == null) Axis(chart, context, builder, viewport, layout, model.Ticks, value => model.X(value, projection), model.FormatTick);
            string? currentGroup = null;
            for (var rowIndex = 0; rowIndex < model.Rows.Count; rowIndex++) {
                var row = model.Rows[rowIndex]; var rowBounds = model.RowBounds(plot, rowIndex, tops);
                if (row.IsGroup) {
                    currentGroup = row.Name.Length == 0 ? null : row.Name;
                    if (frame == null && chart.Options.ShowAxes && chart.Options.YAxis.Visible)
                        using (builder.PushGroup(null, null, currentGroup == null ? null : new Dictionary<string, string> { [ChartDomRoles.LaneGroupAttribute] = currentGroup }))
                            GroupHeader(chart, context, builder, row.Name, viewport.Left, plot.Right - viewport.Left, rowBounds.Top, rowBounds.Height,
                                ChartDomRoles.GanttLaneGroup, "gantt-lane-group-" + rowIndex);
                    continue;
                }
                if (chart.Series[row.SeriesIndex].Kind != ChartSeriesKind.GanttLane) continue;
                if (frame == null) LaneText(chart, context, builder, viewport, layout, row.Name, row.Summary, rowBounds.Top, rowBounds.Height, row.SeriesIndex, currentGroup);
                var series = chart.Series[row.SeriesIndex];
                foreach (var placed in row.Items) {
                    var id = VisualStateSceneTools.SourceId(row.SeriesIndex, placed.PointIndex);
                    var item = items[id]; var bounds = item.Bounds;
                    if (item.Visible) frame?.MarkSurfaces.Add(bounds);
                    var metadata = VisualStateSceneTools.StateMetadata(chart, placed.Category);
                    metadata["data-cfx-series"] = row.SeriesIndex.ToString(); metadata["data-cfx-point"] = placed.PointIndex.ToString();
                    metadata["data-cfx-series-key"] = series.InteractionIdentityKey; metadata[ChartDomRoles.LaneIdAttribute] = series.InteractionIdentityKey;
                    metadata["data-cfx-start"] = VisualStateSceneTools.Number(placed.Item.Start); metadata["data-cfx-end"] = VisualStateSceneTools.Number(placed.End);
                    metadata["data-cfx-meta-start"] = ChartTimeScale.FormatInstant(chart.Options.XAxis, placed.Item.Start);
                    metadata["data-cfx-meta-end"] = ChartTimeScale.FormatInstant(chart.Options.XAxis, placed.End);
                    metadata["data-cfx-meta-duration"] = ChartStateTimelineModel.FormatDuration(placed.End - placed.Item.Start);
                    if (!string.IsNullOrWhiteSpace(placed.Item.Detail)) metadata["data-cfx-meta-detail"] = placed.Item.Detail!;
                    metadata["data-cfx-open"] = placed.Item.IsOpen ? "true" : "false"; metadata["data-cfx-sub-row"] = placed.SubRow.ToString();
                    using (VisualStateSceneTools.Mark(builder, id, ChartDomRoles.GanttLaneItem, bounds, model.ItemSummary(row, placed), metadata)) {
                        if (!item.Visible) continue;
                        var mark = ChartStateMark.For(placed.Category, backdrop);
                        using (builder.PushClip(plot)) {
                            VisualStateSceneTools.StateRect(builder, bounds, mark, ChartGanttLaneModel.BarRadius, "gantt-lane-item-shape");
                            if (placed.Item.IsOpen) {
                                var extent = Math.Min(bounds.Height / 3, bounds.Width / 2);
                                var tip = chart.Options.XAxis.Reversed ? bounds.Left : bounds.Right; var direction = chart.Options.XAxis.Reversed ? -1 : 1;
                                builder.Path(new ChartPath(new[] { ChartPathCommand.MoveTo(tip - direction * extent, bounds.Top + bounds.Height / 2 - extent),
                                    ChartPathCommand.LineTo(tip, bounds.Top + bounds.Height / 2), ChartPathCommand.LineTo(tip - direction * extent, bounds.Top + bounds.Height / 2 + extent) }),
                                    stroke: ChartColorMath.AccessibleTextOnBackground(mark.Surface), strokeWidth: GanttLaneOpenEndStrokeWidth, role: "gantt-lane-open-end",
                                    paint: VisualChartPaint.Stroke(SvgPaint.Literal(ChartColorMath.AccessibleTextOnBackground(mark.Surface))));
                            }
                        }
                        DrawGanttLaneLabel(chart, context, builder, series, placed.PointIndex, item, colors, mark);
                    }
                }
            }
            if (frame == null && model.NowVisible) Now(chart, context, builder, layout, model.X(model.Now!.Value, projection), model.Now.Value);
        }
        builder.CaptureChartReadouts(chart, (_, value) => layout.FormatTick(value), (_, value, _) => layout.FormatTick(value));
    }

    // The lane identity is the series identifier, so hosts that set ChartSeriesId can link lanes by their own keys.
    internal static Dictionary<string, string> LaneMetadata(Chart chart, int seriesIndex, string? group) {
        var metadata = new Dictionary<string, string> {
            // No data-cfx-series here: the row is a label group, not an interaction target.
            [ChartDomRoles.LaneIdAttribute] = chart.Series[seriesIndex].InteractionIdentityKey
        };
        if (!string.IsNullOrEmpty(group)) metadata[ChartDomRoles.LaneGroupAttribute] = group!;
        return metadata;
    }

    private static Dictionary<string, string> Window(double min, double max) => new() {
        ["data-cfx-min"] = VisualStateSceneTools.Number(min), ["data-cfx-max"] = VisualStateSceneTools.Number(max)
    };
}
