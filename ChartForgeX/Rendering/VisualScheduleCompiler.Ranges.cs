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
        for (var index = 0; index < chart.Series.Count; index++) {
            var series = chart.Series[index];
            if (series.Points.Count < (gantt ? 3 : 1)) throw new InvalidOperationException("Schedule series is missing its range or task metadata.");
            var start = series.Points[0].X; var end = series.Points[0].Y;
            if (!ChartMath.IsFinite(start) || !ChartMath.IsFinite(end) || end < start) throw new InvalidOperationException("Schedule endpoints must be finite and ordered.");
            var progress = gantt ? series.Points[1].X : 0;
            var dependency = gantt ? series.Points[1].Y : -1;
            if (!ChartMath.IsFinite(progress) || progress < 0 || progress > 1 || !ChartMath.IsFinite(dependency) || dependency < -1 || dependency >= index || dependency != Math.Truncate(dependency))
                throw new InvalidOperationException("Gantt progress must be between zero and one, and dependencies must reference an earlier task.");
            items.Add(new RangeItem(index, start, end, progress, (int)dependency, gantt && series.Points[2].X >= .5));
        }
        var axis = chart.Options.XAxis; var now = gantt ? chart.Options.GanttToday : null;
        var min = items.Min(item => item.Start); var max = items.Max(item => item.End);
        if (now.HasValue) { min = Math.Min(min, now.Value); max = Math.Max(max, now.Value); }
        min = axis.Minimum ?? min; max = axis.Maximum ?? max;
        (min, max) = ChartMath.ResolveFiniteLaneWindow(min, max, axis.Minimum.HasValue, axis.Maximum.HasValue);
        var ticks = ChartTicks.ForAxis(axis, min, max);
        var formatTick = ChartAxisValueFormatter.Create(axis, ticks,
            axis.Scale == ChartScaleKind.Time ? tick => ChartTimeScale.Format(axis, tick) : chart.Options.ValueFormatter, chart.Options);
        var tickLabels = ticks.ToDictionary(value => value, formatTick);
        string Format(double value) => tickLabels.TryGetValue(value, out var text) ? text : formatTick(value);
        var layout = LaneLayout(chart, context, builder, viewport, chart.Series.Select(series => series.Name), Array.Empty<string>(), false,
            now.HasValue && now.Value >= min && now.Value <= max, ticks, Format);
        var plot = layout.Plot; var colors = context.Theme.Resolve(context.ThemeMode);
        var slot = plot.Height / items.Count; var height = Math.Max(0, Math.Min(gantt ? 30 : 34, slot * .65));
        var milestoneSize = Math.Min(height, plot.Width);
        var milestoneExtent = items.Any(item => item.Milestone) ? milestoneSize / 2 : 0;
        var minimumExtent = Math.Max(milestoneExtent, now == min ? context.Theme.AxisStrokeWidth / 2 : 0);
        var instantExtent = items.Any(item => !item.Milestone && item.Start == item.End) ? 1 : 0;
        var maximumExtent = Math.Max(Math.Max(milestoneExtent, instantExtent), now == max ? context.Theme.AxisStrokeWidth / 2 : 0);
        var projection = MarkProjection(axis, plot, minimumExtent, maximumExtent);
        double Project(double value) => projection.Left + ChartScaleTransform.Normalize(Math.Max(min, Math.Min(max, value)), min, max, axis) * projection.Width;
        using (builder.PushGroup(gantt ? "gantt" : "timeline", gantt ? "gantt-chart" : "timeline", Window(min, max))) {
            Axis(chart, context, builder, viewport, layout, ticks, Project, Format);
            foreach (var item in items) {
                var center = plot.Top + (item.Index + .5) * slot;
                LaneText(chart, context, builder, viewport, layout, chart.Series[item.Index].Name, null, center - height / 2, height, item.Index);
            }
            if (gantt) {
                using (builder.PushClip(plot)) foreach (var item in items.Where(item => item.Dependency >= 0)) {
                    var previous = items[item.Dependency];
                    var start = new ChartPoint(Project(previous.End), plot.Top + (previous.Index + .5) * slot);
                    var end = new ChartPoint(Project(item.Start), plot.Top + (item.Index + .5) * slot);
                    var elbow = Math.Max(start.X, end.X) + Math.Min(context.Theme.Spacing, plot.Right - Math.Max(start.X, end.X));
                    var id = "gantt-dependency-" + item.Dependency + "-" + item.Index;
                    using (VisualStateSceneTools.Mark(builder, id, "gantt-dependency", new ChartRect(Math.Min(start.X, end.X), Math.Min(start.Y, end.Y),
                        Math.Abs(end.X - start.X), Math.Abs(end.Y - start.Y)), chart.Series[previous.Index].Name + " → " + chart.Series[item.Index].Name,
                        new Dictionary<string, string> { ["data-cfx-source"] = "series-" + previous.Index, ["data-cfx-target"] = "series-" + item.Index })) {
                        builder.Path(new ChartPath(new[] { ChartPathCommand.MoveTo(start.X, start.Y), ChartPathCommand.LineTo(elbow, start.Y),
                            ChartPathCommand.LineTo(elbow, end.Y), ChartPathCommand.LineTo(end.X, end.Y) }), stroke: colors.Border,
                            strokeWidth: context.Theme.AxisStrokeWidth, role: "gantt-dependency-line", paint: VisualChartPaint.Stroke(colors.Border, SvgColorRole.Grid));
                        var direction = end.X < elbow ? -1d : 1d; var arrow = Math.Min(5, height / 3);
                        builder.Path(new ChartPath(new[] { ChartPathCommand.MoveTo(end.X - direction * arrow, end.Y - arrow), ChartPathCommand.LineTo(end.X, end.Y),
                            ChartPathCommand.LineTo(end.X - direction * arrow, end.Y + arrow) }), stroke: colors.Border, strokeWidth: context.Theme.AxisStrokeWidth, role: "gantt-dependency-arrow", paint: VisualChartPaint.Stroke(colors.Border, SvgColorRole.Grid));
                    }
                }
                if (now.HasValue && now.Value >= min && now.Value <= max) Now(chart, context, builder, layout, Project(now.Value), now.Value);
            }
            foreach (var item in items) {
                var series = chart.Series[item.Index]; var center = plot.Top + (item.Index + .5) * slot;
                var visible = item.End >= min && item.Start <= max;
                var left = Project(item.Start); var right = Project(item.End);
                var bounds = new ChartRect(left, center - height / 2, visible ? Math.Min(plot.Right - left, Math.Max(1, right - left)) : 0, height);
                if (item.Milestone) bounds = new ChartRect(left - milestoneSize / 2, center - milestoneSize / 2, milestoneSize, milestoneSize);
                var id = VisualStateSceneTools.SourceId(item.Index, 0);
                var duration = chart.Options.ValueFormatter?.Invoke(item.End - item.Start) ?? ChartStateTimelineModel.FormatDuration(item.End - item.Start);
                var completion = (item.Progress * 100).ToString("0.#", chart.Options.ValueFormat.Culture) + "%";
                var summary = series.Name + ": " + Format(item.Start) + " – " + Format(item.End) + ", " + (gantt ? completion : duration);
                var metadata = new Dictionary<string, string> {
                    ["data-cfx-series"] = item.Index.ToString(), ["data-cfx-point"] = "0", ["data-cfx-start"] = VisualStateSceneTools.Number(item.Start),
                    ["data-cfx-end"] = VisualStateSceneTools.Number(item.End), ["data-cfx-duration"] = duration,
                    ["data-cfx-progress"] = VisualStateSceneTools.Number(item.Progress), ["data-cfx-milestone"] = item.Milestone ? "true" : "false",
                    ["data-cfx-dependency"] = item.Dependency.ToString(), ["data-cfx-series-key"] = series.InteractionIdentityKey
                };
                using (VisualStateSceneTools.Mark(builder, id, item.Milestone ? "gantt-milestone" : gantt ? "gantt-task" : "timeline-item", bounds, summary, metadata)) {
                    if (!visible) continue;
                    var fill = series.PointColors.Count > 0 && series.PointColors[0].HasValue ? series.PointColors[0]!.Value : VisualStateSceneTools.SeriesColor(series, item.Index, colors);
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
        }
    }

    private sealed class RangeItem {
        internal RangeItem(int index, double start, double end, double progress, int dependency, bool milestone) {
            Index = index; Start = start; End = end; Progress = progress; Dependency = dependency; Milestone = milestone;
        }
        internal int Index { get; } internal double Start { get; } internal double End { get; }
        internal double Progress { get; } internal int Dependency { get; } internal bool Milestone { get; }
    }
}
