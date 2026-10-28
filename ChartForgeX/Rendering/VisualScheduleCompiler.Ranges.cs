using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualScheduleCompiler {
    private static void Ranges(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect viewport, bool gantt, ScheduleCompositionFrame? frame = null) {
        var items = new List<RangeItem>();
        var row = 0;
        for (var index = 0; index < chart.Series.Count; index++) {
            var series = chart.Series[index];
            if (series.Kind is not (ChartSeriesKind.Timeline or ChartSeriesKind.Gantt)) continue;
            var sourceGantt = series.Kind == ChartSeriesKind.Gantt;
            if (sourceGantt ? series.GanttItems.Count == 0 : series.RenderPoints.Count == 0) continue;
            var task = sourceGantt ? series.GanttItems[0] : default;
            var start = sourceGantt ? task.Start : series.RenderPoints[0].X;
            var end = sourceGantt ? task.End : series.RenderPoints[0].Y;
            if (!ChartMath.IsFinite(start) || !ChartMath.IsFinite(end) || end < start) throw new InvalidOperationException("Schedule endpoints must be finite and ordered.");
            var progress = sourceGantt ? task.Progress : 0;
            if (!ChartMath.IsFinite(progress) || progress < 0 || progress > 1)
                throw new InvalidOperationException("Gantt progress must be between zero and one.");
            var marker = IsMarker(series);
            items.Add(new RangeItem(index, marker ? -1 : frame?.Model.RowOffset(index) ?? row++, start, end, progress, sourceGantt && task.IsMilestone, marker, sourceGantt));
        }
        if (items.Count == 0) return;
        var bySeries = items.ToDictionary(item => item.Index);
        var links = gantt ? chart.ResolveGanttDependencies() : new List<ChartGanttOrdinalDependency>();
        var renderedLinks = links.Where(link => bySeries.TryGetValue(link.PredecessorIndex, out var predecessor)
            && bySeries.TryGetValue(link.SuccessorIndex, out var successor) && !predecessor.Marker && !successor.Marker).ToArray();
        var predecessors = links.GroupBy(link => link.SuccessorIndex).ToDictionary(group => group.Key, group => group.Select(link => link.PredecessorIndex).ToArray());
        var axis = chart.Options.XAxis; var now = gantt ? chart.Options.GanttToday : null;
        var min = frame?.Model.Min ?? items.Min(item => item.Start); var max = frame?.Model.Max ?? items.Max(item => item.End);
        foreach (var annotation in chart.Annotations.Where(annotation => annotation.Kind == ChartAnnotationKind.VerticalLine)) {
            min = Math.Min(min, annotation.Value); max = Math.Max(max, annotation.Value);
        }
        if (now.HasValue) { min = Math.Min(min, now.Value); max = Math.Max(max, now.Value); }
        min = axis.Minimum ?? min; max = axis.Maximum ?? max;
        (min, max) = ChartMath.ResolveFiniteLaneWindow(min, max, axis.Minimum.HasValue, axis.Maximum.HasValue);
        var fixedTicks = frame == null && axis.Labels.Count == 0 && gantt && axis.Scale == ChartScaleKind.Time && chart.Options.GanttTickInterval != null
            ? ChartTimeScale.GenerateFixed(axis, min, max, chart.Options.GanttTickInterval) : null;
        var ticks = frame?.Model.Ticks ?? fixedTicks ?? ChartTicks.ForAxis(axis, min, max);
        if (gantt && axis.Scale == ChartScaleKind.Time && axis.Labels.Count == 0 && fixedTicks == null
            && (max - min < 1 || axis.LabelDensity == ChartLabelDensity.All))
            ticks = ScheduleProjectWindowTicks(ticks, min, max);
        var milliseconds = fixedTicks != null && chart.Options.GanttTickInterval?.Unit == ChartTimeTickUnit.Millisecond;
        var effectiveInterval = fixedTicks == null ? (ChartTimeTickUnit?)null : chart.Options.GanttTickInterval?.Unit;
        var projectDates = gantt && axis.Scale == ChartScaleKind.Time && (fixedTicks == null || effectiveInterval >= ChartTimeTickUnit.Day) && axis.Labels.Count == 0
            && axis.ValueFormat == null && axis.LabelDensity != ChartLabelDensity.All && max - min >= 1;
        var formatTick = ChartAxisValueFormatter.Create(axis, ticks,
            axis.Scale == ChartScaleKind.Time ? tick => gantt && axis.TimeTickLabelFormatter != null
                ? axis.TimeTickLabelFormatter(tick, effectiveInterval)
                : projectDates && ChartTimeScale.ToDisplayTime(axis, tick) is { } date
                    ? date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                    : ChartTimeScale.Format(axis, tick, milliseconds) : chart.Options.ValueFormatter, chart.Options);
        var formatCache = new ChartAxisValueFormatter.Cache();
        string Format(double value) => frame != null ? frame.Layout.FormatTick(value) : formatCache.Format(axis, value, formatTick);
        // A date caption identifies the day's calendar line, rather than a
        // task's arbitrary clock time. An automatic window includes that first
        // midnight; exact task instants and authored clipping bounds are retained.
        if (frame == null && gantt && axis.Scale == ChartScaleKind.Time && axis.Labels.Count == 0 && !axis.Minimum.HasValue
            && max - min >= 1 && axis.LabelDensity != ChartLabelDensity.All
            && (fixedTicks == null || effectiveInterval >= ChartTimeTickUnit.Day)
            && ChartTimeScale.HasDayCaptionPrecision(axis, Format, min)
            && ChartTimeScale.DisplayDayStart(axis, min) is { } dayStart && dayStart < min) {
            min = dayStart;
            if (fixedTicks != null && chart.Options.GanttTickInterval != null)
                fixedTicks = ChartTimeScale.GenerateFixed(axis, min, max, chart.Options.GanttTickInterval);
            ticks = fixedTicks ?? ChartTicks.ForAxis(axis, min, max);
        }
        var layout = frame?.Layout ?? LaneLayout(chart, context, builder, viewport, items.Where(item => !item.Marker).Select(item => chart.Series[item.Index].Name), Array.Empty<string>(), false,
            now.HasValue && now.Value >= min && now.Value <= max, ticks, Format, hasFixedTicks: fixedTicks != null);
        var plot = layout.Plot; var colors = context.Theme.Resolve(context.ThemeMode);
        var slot = frame?.Model.UnitHeight(plot) ?? plot.Height / Math.Max(1, row); var height = Math.Max(0, Math.Min(gantt ? 30 : 34, slot * .65));
        // Linked rows need a gutter at least as wide as the connector's painted stroke. Keep the ordinary
        // mark height when it fits, and retain marks when the declared stroke cannot fit even an empty row.
        if (renderedLinks.Length > 0 && slot > context.Theme.AxisStrokeWidth)
            height = Math.Min(height, slot - context.Theme.AxisStrokeWidth);
        var milestoneSize = Math.Min(height, plot.Width);
        var milestoneExtent = items.Any(item => item.Milestone) ? milestoneSize / 2 : 0;
        var markerAtMin = items.Any(item => item.Marker && item.Start == min);
        var markerAtMax = items.Any(item => item.Marker && item.Start == max);
        var minimumExtent = Math.Max(milestoneExtent, markerAtMin ? context.Theme.AxisStrokeWidth / 2 : 0);
        var instantExtent = items.Any(item => !item.Milestone && item.Start == item.End) ? 1 : 0;
        var maximumExtent = Math.Max(Math.Max(milestoneExtent, instantExtent), markerAtMax ? context.Theme.AxisStrokeWidth / 2 : 0);
        if (renderedLinks.Length > 0) {
            // Automatic windows contain the incoming arrow and outgoing routing stub as well as the marks.
            // Authored windows keep their exact dates and retain the normal plot clip.
            var clearance = DependencyClearance(context); var arrow = DependencyArrowExtent(height);
            minimumExtent = Math.Max(minimumExtent, milestoneExtent + clearance + arrow + context.Theme.AxisStrokeWidth / 2);
            maximumExtent = Math.Max(maximumExtent, milestoneExtent + clearance + DependencyStubLength(context, arrow) + context.Theme.AxisStrokeWidth / 2);
        }
        var projection = frame?.Projection ?? GuideProjection(chart, context, plot, min, max, minimumExtent, maximumExtent, now);
        if (frame == null) builder.DataProjection = ChartDataProjection.TryCreate(projection, min, max, axis);
        double Project(double value) => ScheduleX(axis, value, min, max, projection);
        using (builder.PushGroup(gantt ? "gantt" : "timeline", gantt ? "gantt-chart" : "timeline", Window(min, max))) {
            var obstacles = new List<LabelObstacle>();
            if (frame == null) Axis(chart, context, builder, viewport, layout, ticks, Project, Format,
                minimum: gantt ? min : null, maximum: gantt ? max : null, calendarAnchor: gantt ? items.Min(item => item.Start) : null);
            foreach (var item in items) {
                if (item.Marker || frame != null) continue;
                var center = ScheduleRowCenter(chart, plot, (item.Row + .5) * slot);
                LaneText(chart, context, builder, viewport, layout, chart.Series[item.Index].Name, null, center - height / 2, height, item.Index);
            }
            foreach (var item in items) {
                if (item.Marker) continue;
                var series = chart.Series[item.Index]; var center = ScheduleRowCenter(chart, plot, (item.Row + .5) * slot);
                var visible = item.End >= min && item.Start <= max;
                var left = Project(item.Start);
                var bounds = RangeBounds(chart, item, plot, Project, slot, height, visible);
                var id = VisualStateSceneTools.SourceId(item.Index, 0);
                if (visible) obstacles.Add(new LabelObstacle(id, bounds));
                var duration = chart.Options.ValueFormatter?.Invoke(item.End - item.Start) ?? ChartStateTimelineModel.FormatDuration(item.End - item.Start);
                var completion = (item.Progress * 100).ToString("0.#", chart.Options.ValueFormat.Culture) + "%";
                var summary = series.Name + ": " + Format(item.Start) + " – " + Format(item.End) + ", " + (item.Gantt ? completion : duration);
                var metadata = new Dictionary<string, string> {
                    ["data-cfx-series"] = item.Index.ToString(), ["data-cfx-point"] = "0", ["data-cfx-source-point"] = "0", ["data-cfx-source-count"] = "1",
                    ["data-cfx-start"] = VisualStateSceneTools.Number(item.Start),
                    ["data-cfx-end"] = VisualStateSceneTools.Number(item.End), ["data-cfx-duration"] = duration,
                    ["data-cfx-progress"] = VisualStateSceneTools.Number(item.Progress), ["data-cfx-milestone"] = item.Milestone ? "true" : "false",
                    ["data-cfx-series-key"] = series.InteractionIdentityKey
                };
                DependencyMetadata(metadata, predecessors, item.Index);
                using (VisualStateSceneTools.Mark(builder, id, item.Milestone ? "gantt-milestone" : item.Gantt ? "gantt-task" : "timeline-item", bounds, summary, metadata)) {
                    if (!visible) continue;
                    var fill = ChartSeriesColours.Point(series, item.Index, 0, colors);
                    var fillRole = VisualChartPaint.SeriesRole(series, 0); var fillPaint = SvgPaint.Of(fill, fillRole);
                    using (builder.PushClip(plot)) {
                        ChartPath shape;
                        if (item.Milestone) {
                            shape = new ChartPath(new[] { ChartPathCommand.MoveTo(left, center - milestoneSize / 2), ChartPathCommand.LineTo(left + milestoneSize / 2, center),
                                ChartPathCommand.LineTo(left, center + milestoneSize / 2), ChartPathCommand.LineTo(left - milestoneSize / 2, center) });
                            builder.Path(shape, fill, role: "gantt-milestone-shape", close: true, paint: VisualChartPaint.Fill(fillPaint));
                        } else {
                            var radius = Math.Min(context.Theme.BarRadius, height / 2); shape = VisualStateSceneTools.RoundedRect(bounds, radius);
                            var style = chart.Options.ResolvePreparedBarVisualStyle();
                            var bodyColor = item.Gantt ? ChartColorMath.Blend(colors.Surface, fill, .25) : fill;
                            var bodyPaint = item.Gantt ? SvgPaint.Mix(bodyColor, colors.Surface, SvgColorRole.Surface, fill, fillRole, .25) : fillPaint;
                            if (style.Kind == ChartBarStyle.Solid)
                                builder.RectGradient(bounds, new ChartPoint(bounds.Left, bounds.Top), new ChartPoint(bounds.Left, bounds.Bottom),
                                    new[] { new VisualGradientStop(0, ChartMarkSurface.BarGradientTop(bodyColor),
                                        item.Gantt ? SvgPaint.Literal(ChartMarkSurface.BarGradientTop(bodyColor)) : VisualChartPaint.BarGradient(fill, fillRole, true, 1)),
                                        new VisualGradientStop(1, ChartMarkSurface.BarGradientBottom(bodyColor),
                                        item.Gantt ? SvgPaint.Literal(ChartMarkSurface.BarGradientBottom(bodyColor)) : VisualChartPaint.BarGradient(fill, fillRole, false, 1)) },
                                    radius: radius, role: item.Gantt ? "gantt-task-shape" : "timeline-item-shape");
                            else builder.Rect(bounds, bodyColor, radius: radius, role: item.Gantt ? "gantt-task-shape" : "timeline-item-shape", paint: VisualChartPaint.Fill(bodyPaint));
                            if (item.Gantt && item.Progress > 0) {
                                var progressEnd = ChartHeatmapSurface.InterpolateObservedRange(item.Start, item.End, item.Progress);
                                var progressLeft = axis.Reversed ? Project(progressEnd) : bounds.Left;
                                var progressWidth = axis.Reversed ? Math.Max(0, Project(item.Start) - progressLeft) : Math.Max(0, Project(progressEnd) - bounds.Left);
                                using (builder.PushClip(shape)) builder.Rect(new ChartRect(progressLeft, bounds.Top, progressWidth, bounds.Height), fill, role: "gantt-progress", paint: VisualChartPaint.Fill(fillPaint));
                            }
                        }
                        var pattern = series.PointFillPatterns.Count > 0 ? series.PointFillPatterns[0] ?? series.FillPattern : series.FillPattern;
                        builder.Pattern(shape, pattern, colors.Surface.WithOpacity(.45),
                            paint: SvgPaint.Of(colors.Surface, SvgColorRole.Surface).WithOpacity(colors.Surface.WithOpacity(.45), .45));
                    }
                    var label = series.PointLabels.Count > 0 && series.PointLabels[0] != null ? series.PointLabels[0]! : item.Gantt ? completion : duration;
                    DataLabel(chart, context, builder, series, 0, label, bounds, viewport, colors, fill);
                }
            }
            Markers(chart, context, builder, plot, items, Project, Format, min, max, colors, obstacles, predecessors);
            if (frame == null && gantt && now.HasValue && now.Value >= min && now.Value <= max) Now(chart, context, builder, layout, Project(now.Value), now.Value);
            // Links are foreground annotations: successor fills must not cover their arrowheads.
            if (gantt) Dependencies(chart, context, builder, plot, bySeries, renderedLinks, Project, slot, height, min, max, frame?.MarkSurfaces);
        }
        builder.CaptureChartReadouts(chart, (_, value) => layout.FormatTick(value), (_, value, _) => layout.FormatTick(value));
    }

    /// <summary>Shares the exact task/diamond surface between painting, semantics and dependency anchoring.</summary>
    private static ChartRect RangeBounds(Chart chart, RangeItem item, ChartRect plot, Func<double, double> project, double slot, double height, bool visible = true) {
        height = item.Gantt ? height : Math.Min(34, slot * .65);
        var center = ScheduleRowCenter(chart, plot, (item.Row + .5) * slot); var left = project(item.Start);
        if (item.Milestone) {
            var size = Math.Min(height, plot.Width);
            return new ChartRect(left - size / 2, center - size / 2, size, size);
        }
        if (chart.Options.XAxis.Reversed) {
            var right = left; var end = project(item.End); var width = visible ? Math.Max(1, right - end) : 0;
            left = Math.Max(plot.Left, right - width);
            return new ChartRect(left, center - height / 2, Math.Max(0, Math.Min(plot.Right, right) - left), height);
        }
        return new ChartRect(left, center - height / 2,
            visible ? Math.Min(plot.Right - left, Math.Max(1, project(item.End) - left)) : 0, height);
    }

    private sealed class RangeItem {
        internal RangeItem(int index, double row, double start, double end, double progress, bool milestone, bool marker, bool gantt) {
            Index = index; Row = row; Start = start; End = end; Progress = progress; Milestone = milestone; Marker = marker; Gantt = gantt;
        }
        internal int Index { get; } internal double Start { get; } internal double End { get; }
        internal double Row { get; } internal double Progress { get; } internal bool Milestone { get; } internal bool Marker { get; } internal bool Gantt { get; }
    }
}
