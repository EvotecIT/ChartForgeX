using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualScheduleCompiler {
    private static void Ranges(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect viewport, bool gantt) {
        var items = new List<RangeItem>();
        var row = 0;
        for (var index = 0; index < chart.Series.Count; index++) {
            var series = chart.Series[index];
            if (series.Points.Count < (gantt ? 3 : 1)) throw new InvalidOperationException("Schedule series is missing its range or task metadata.");
            var start = series.Points[0].X; var end = series.Points[0].Y;
            if (!ChartMath.IsFinite(start) || !ChartMath.IsFinite(end) || end < start) throw new InvalidOperationException("Schedule endpoints must be finite and ordered.");
            var progress = gantt ? series.Points[1].X : 0;
            var dependency = gantt ? series.Points[1].Y : -1;
            if (!ChartMath.IsFinite(progress) || progress < 0 || progress > 1 || !ChartMath.IsFinite(dependency) || dependency < -1 || dependency >= index || dependency != Math.Truncate(dependency))
                throw new InvalidOperationException("Gantt progress must be between zero and one, and dependencies must reference an earlier task.");
            var marker = IsMarker(series);
            items.Add(new RangeItem(index, marker ? -1 : row++, start, end, progress, gantt && series.Points[2].X >= .5, marker));
        }
        var links = gantt ? chart.ResolveGanttDependencies() : new List<ChartGanttDependency>();
        var renderedLinks = links.Where(link => !items[link.PredecessorIndex].Marker && !items[link.SuccessorIndex].Marker).ToArray();
        var predecessors = links.GroupBy(link => link.SuccessorIndex).ToDictionary(group => group.Key, group => group.Select(link => link.PredecessorIndex).ToArray());
        var axis = chart.Options.XAxis; var now = gantt ? chart.Options.GanttToday : null;
        var min = items.Min(item => item.Start); var max = items.Max(item => item.End);
        foreach (var annotation in chart.Annotations.Where(annotation => annotation.Kind == ChartAnnotationKind.VerticalLine)) {
            min = Math.Min(min, annotation.Value); max = Math.Max(max, annotation.Value);
        }
        if (now.HasValue) { min = Math.Min(min, now.Value); max = Math.Max(max, now.Value); }
        min = axis.Minimum ?? min; max = axis.Maximum ?? max;
        (min, max) = ChartMath.ResolveFiniteLaneWindow(min, max, axis.Minimum.HasValue, axis.Maximum.HasValue);
        var fixedTicks = axis.Labels.Count == 0 && gantt && axis.Scale == ChartScaleKind.Time && chart.Options.GanttTickInterval != null
            ? ChartTimeScale.GenerateFixed(axis, min, max, chart.Options.GanttTickInterval) : null;
        var ticks = axis.Labels.Count > 0 ? axis.Labels.Select(label => label.Value).Where(value => value >= min && value <= max).Distinct().OrderBy(value => value).ToArray()
            : fixedTicks ?? ChartTicks.GenerateInside(axis, min, max);
        var milliseconds = fixedTicks != null && chart.Options.GanttTickInterval?.Unit == ChartTimeTickUnit.Millisecond;
        var tickLabels = ticks.ToDictionary(value => value, value => ChartAxisValueFormatter.Format(axis, value,
            tick => axis.Scale == ChartScaleKind.Time ? ChartTimeScale.Format(axis, tick, milliseconds) : ChartNumericFormatter.FormatValue(chart.Options, tick), ticks));
        string Format(double value) => tickLabels.TryGetValue(value, out var text) ? text : ChartAxisValueFormatter.Format(axis, value,
            tick => axis.Scale == ChartScaleKind.Time ? ChartTimeScale.Format(axis, tick, milliseconds) : ChartNumericFormatter.FormatValue(chart.Options, tick), ticks);
        var layout = LaneLayout(chart, context, builder, viewport, items.Where(item => !item.Marker).Select(item => chart.Series[item.Index].Name), Array.Empty<string>(), false,
            now.HasValue && now.Value >= min && now.Value <= max, ticks, Format);
        var plot = layout.Plot; var colors = context.Theme.Resolve(context.ThemeMode);
        var slot = plot.Height / Math.Max(1, row); var height = Math.Max(0, Math.Min(gantt ? 30 : 34, slot * .65));
        var milestoneSize = Math.Min(height, plot.Width);
        var milestoneExtent = items.Any(item => item.Milestone) ? milestoneSize / 2 : 0;
        var markerAtMin = items.Any(item => item.Marker && item.Start == min) || chart.Annotations.Any(annotation => annotation.Kind == ChartAnnotationKind.VerticalLine && annotation.Value == min);
        var markerAtMax = items.Any(item => item.Marker && item.Start == max) || chart.Annotations.Any(annotation => annotation.Kind == ChartAnnotationKind.VerticalLine && annotation.Value == max);
        var minimumExtent = Math.Max(milestoneExtent, now == min || markerAtMin ? context.Theme.AxisStrokeWidth / 2 : 0);
        var instantExtent = items.Any(item => !item.Milestone && item.Start == item.End) ? 1 : 0;
        var maximumExtent = Math.Max(Math.Max(milestoneExtent, instantExtent), now == max || markerAtMax ? context.Theme.AxisStrokeWidth / 2 : 0);
        if (renderedLinks.Length > 0) maximumExtent = Math.Max(maximumExtent,
            (renderedLinks.Any(link => items[link.SuccessorIndex].Milestone) ? milestoneExtent : 0) + Math.Min(5, height / 3) + context.Theme.AxisStrokeWidth / 2);
        var projection = MarkProjection(axis, plot, minimumExtent, maximumExtent);
        double Project(double value) => projection.Left + ChartScaleTransform.Normalize(Math.Max(min, Math.Min(max, value)), min, max, axis) * projection.Width;
        using (builder.PushGroup(gantt ? "gantt" : "timeline", gantt ? "gantt-chart" : "timeline", Window(min, max))) {
            var obstacles = new List<LabelObstacle>();
            Axis(chart, context, builder, viewport, layout, ticks, Project, Format);
            foreach (var item in items) {
                if (item.Marker) continue;
                var center = plot.Top + (item.Row + .5) * slot;
                LaneText(chart, context, builder, viewport, layout, chart.Series[item.Index].Name, null, center - height / 2, height, item.Index);
            }
            foreach (var item in items) {
                if (item.Marker) continue;
                var series = chart.Series[item.Index]; var center = plot.Top + (item.Row + .5) * slot;
                var visible = item.End >= min && item.Start <= max;
                var left = Project(item.Start); var right = Project(item.End);
                var bounds = new ChartRect(left, center - height / 2, visible ? Math.Min(plot.Right - left, Math.Max(1, right - left)) : 0, height);
                if (item.Milestone) bounds = new ChartRect(left - milestoneSize / 2, center - milestoneSize / 2, milestoneSize, milestoneSize);
                var id = VisualStateSceneTools.SourceId(item.Index, 0);
                if (visible) obstacles.Add(new LabelObstacle(id, bounds));
                var duration = chart.Options.ValueFormatter?.Invoke(item.End - item.Start) ?? ChartStateTimelineModel.FormatDuration(item.End - item.Start);
                var completion = (item.Progress * 100).ToString("0.#", chart.Options.ValueFormat.Culture) + "%";
                var summary = series.Name + ": " + Format(item.Start) + " – " + Format(item.End) + ", " + (gantt ? completion : duration);
                var metadata = new Dictionary<string, string> {
                    ["data-cfx-series"] = item.Index.ToString(), ["data-cfx-point"] = "0", ["data-cfx-start"] = VisualStateSceneTools.Number(item.Start),
                    ["data-cfx-end"] = VisualStateSceneTools.Number(item.End), ["data-cfx-duration"] = duration,
                    ["data-cfx-progress"] = VisualStateSceneTools.Number(item.Progress), ["data-cfx-milestone"] = item.Milestone ? "true" : "false",
                    ["data-cfx-series-key"] = series.InteractionIdentityKey
                };
                DependencyMetadata(metadata, predecessors, item.Index);
                using (VisualStateSceneTools.Mark(builder, id, item.Milestone ? "gantt-milestone" : gantt ? "gantt-task" : "timeline-item", bounds, summary, metadata)) {
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
                            var bodyColor = gantt ? ChartColorMath.Blend(colors.Surface, fill, .25) : fill;
                            var bodyPaint = gantt ? SvgPaint.Mix(bodyColor, colors.Surface, SvgColorRole.Surface, fill, fillRole, .25) : fillPaint;
                            if (style.Kind == ChartBarStyle.Solid)
                                builder.RectGradient(bounds, new ChartPoint(bounds.Left, bounds.Top), new ChartPoint(bounds.Left, bounds.Bottom),
                                    new[] { new VisualGradientStop(0, ChartMarkSurface.BarGradientTop(bodyColor),
                                        gantt ? SvgPaint.Literal(ChartMarkSurface.BarGradientTop(bodyColor)) : VisualChartPaint.BarGradient(fill, fillRole, true, 1)),
                                        new VisualGradientStop(1, ChartMarkSurface.BarGradientBottom(bodyColor),
                                        gantt ? SvgPaint.Literal(ChartMarkSurface.BarGradientBottom(bodyColor)) : VisualChartPaint.BarGradient(fill, fillRole, false, 1)) },
                                    radius: radius, role: gantt ? "gantt-task-shape" : "timeline-item-shape");
                            else builder.Rect(bounds, bodyColor, radius: radius, role: gantt ? "gantt-task-shape" : "timeline-item-shape", paint: VisualChartPaint.Fill(bodyPaint));
                            if (gantt && item.Progress > 0) {
                                var progressEnd = ChartHeatmapSurface.InterpolateObservedRange(item.Start, item.End, item.Progress);
                                var progressWidth = Math.Max(0, Project(progressEnd) - bounds.Left);
                                using (builder.PushClip(shape)) builder.Rect(new ChartRect(bounds.Left, bounds.Top, progressWidth, bounds.Height), fill, role: "gantt-progress", paint: VisualChartPaint.Fill(fillPaint));
                            }
                        }
                        var pattern = series.PointFillPatterns.Count > 0 ? series.PointFillPatterns[0] ?? series.FillPattern : series.FillPattern;
                        builder.Pattern(shape, pattern, colors.Surface.WithOpacity(.45),
                            paint: SvgPaint.Of(colors.Surface, SvgColorRole.Surface).WithOpacity(colors.Surface.WithOpacity(.45), .45));
                    }
                    var label = series.PointLabels.Count > 0 && series.PointLabels[0] != null ? series.PointLabels[0]! : gantt ? completion : duration;
                    DataLabel(chart, context, builder, series, 0, label, bounds, viewport, colors, fill);
                }
            }
            Markers(chart, context, builder, plot, items, Project, Format, min, max, colors, obstacles, predecessors);
            if (gantt && now.HasValue && now.Value >= min && now.Value <= max) Now(chart, context, builder, layout, Project(now.Value), now.Value);
            // Links are foreground annotations: successor fills must not cover their arrowheads.
            if (gantt) Dependencies(chart, context, builder, plot, items, renderedLinks, Project, slot, height);
        }
    }

    private sealed class RangeItem {
        internal RangeItem(int index, int row, double start, double end, double progress, bool milestone, bool marker) {
            Index = index; Row = row; Start = start; End = end; Progress = progress; Milestone = milestone; Marker = marker;
        }
        internal int Index { get; } internal double Start { get; } internal double End { get; }
        internal int Row { get; } internal double Progress { get; } internal bool Milestone { get; } internal bool Marker { get; }
    }
}
