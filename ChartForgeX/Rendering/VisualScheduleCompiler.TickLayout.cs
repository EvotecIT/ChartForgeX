using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualScheduleCompiler {
    /// <summary>Chooses the shared numeric/calendar step before drawing either schedule grids or captions.</summary>
    private static IReadOnlyList<double> ScheduleTicks(Chart chart, VisualRenderContext context, VisualSceneBuilder builder,
        IReadOnlyList<double> ticks, Func<double, double> project, Func<double, string> format, TextStyle style, bool hasFixedTicks) {
        var axis = chart.Options.XAxis;
        if (ticks.Count < 2 || axis.Labels.Count > 0 || hasFixedTicks || axis.LabelDensity == ChartLabelDensity.All) return ticks;
        var policy = axis.Clone();
        // Lane models use calendar ticks even when their source axis retains the default linear scale.
        if (chart.Series[0].Kind is ChartSeriesKind.StateTimeline or ChartSeriesKind.GanttLane) policy.Scale = ChartScaleKind.Time;
        // Automatic Gantt captions identify dates on day-scale windows, including
        // rejected dense fixed intervals. Keep the generated ticks on whole days;
        // authored clock formats and successful fixed intervals remain authoritative.
        var minimumStepDays = chart.Series[0].Kind == ChartSeriesKind.Gantt
            && axis.ValueFormat == null && ticks[ticks.Count - 1] - ticks[0] >= 1 ? 1 : 0;
        if (policy.Scale == ChartScaleKind.Time && ChartTimeScale.GenerateFitting(policy, ticks[0], ticks[ticks.Count - 1],
            candidates => ChartTimeScale.HasDistinctAdjacentLabels(candidates.Select(format))
                && ScheduleCaptionsFit(chart, context, builder, candidates, project, format,
                GeneratedScheduleTickStyle(chart, context, builder, candidates, project, format, style)), allowCompactDates: false,
            minimumStepDays: minimumStepDays) is { } calendarTicks)
            return calendarTicks;
        var selected = ticks;
        for (var count = axis.TickCount; count >= 2; count--) {
            policy.TickCount = count;
            selected = ChartTicks.GenerateInside(policy, ticks[0], ticks[ticks.Count - 1]);
            if (ScheduleCaptionsFit(chart, context, builder, selected, project, format,
                GeneratedScheduleTickStyle(chart, context, builder, selected, project, format, style))) break;
        }
        return selected;
    }

    /// <summary>Fits oversized generated type to the selected pitch while ordinary and authored captions retain their style.</summary>
    private static TextStyle GeneratedScheduleTickStyle(Chart chart, VisualRenderContext context, VisualSceneBuilder builder,
        IReadOnlyList<double> ticks, Func<double, double> project, Func<double, string> format, TextStyle style) {
        if (ticks.Count < 2 || style.EffectiveFontSize <= VisualTextFitting.MaximumOrdinaryFontSize) return style;
        var gap = chart.Options.XAxis.LabelDensity == ChartLabelDensity.Dense ? 0
            : chart.Options.XAxis.LabelDensity == ChartLabelDensity.Relaxed ? context.Theme.Spacing * 2 : context.Theme.Spacing / 2;
        var pitch = double.PositiveInfinity;
        for (var index = 1; index < ticks.Count; index++) pitch = Math.Min(pitch, Math.Abs(project(ticks[index]) - project(ticks[index - 1])));
        var angle = Math.Max(-80, Math.Min(80, chart.Options.XAxis.LabelAngle));
        var texts = ticks.Select(format).ToArray();
        var height = texts.Select(text => VisualAxisText.RotatedMetrics(builder.MeasureText(text, style), angle).Height).DefaultIfEmpty(0).Max();
        return VisualAxisText.FitOversized(builder, style, new ChartRect(0, 0, Math.Max(0, pitch - gap), height), texts, angle);
    }

    /// <summary>Authored labels and fixed intervals keep their exact ticks; only their captions use an even stride.</summary>
    private static HashSet<double> ScheduleCaptionStride(Chart chart, VisualRenderContext context, VisualSceneBuilder builder,
        IReadOnlyList<double> ticks, Func<double, double> project, Func<double, string> format, TextStyle style) {
        if (chart.Options.XAxis.LabelDensity == ChartLabelDensity.All) return new HashSet<double>(ticks);
        for (var stride = 1; stride <= ticks.Count; stride++) {
            var selected = ticks.Where((_, index) => index % stride == 0).ToArray();
            if (ScheduleCaptionsFit(chart, context, builder, selected, project, format, style)) return new HashSet<double>(selected);
        }
        return new HashSet<double>();
    }

    private static bool ScheduleCaptionsFit(Chart chart, VisualRenderContext context, VisualSceneBuilder builder,
        IReadOnlyList<double> ticks, Func<double, double> project, Func<double, string> format, TextStyle style) {
        var axis = chart.Options.XAxis;
        var gap = axis.LabelDensity == ChartLabelDensity.Dense ? 0 : axis.LabelDensity == ChartLabelDensity.Relaxed ? context.Theme.Spacing * 2 : context.Theme.Spacing / 2;
        var angle = Math.Max(-80, Math.Min(80, axis.LabelAngle));
        var previousPosition = 0d; var previousExtent = 0d; var hasPrevious = false;
        foreach (var tick in ticks) {
            var text = format(tick);
            if (text.Length == 0) continue;
            var extent = VisualAxisText.RotatedMetrics(builder.MeasureText(text, style), angle).Width;
            var position = project(tick);
            if (hasPrevious && Math.Abs(position - previousPosition) + .0001 < (extent + previousExtent) / 2 + gap) return false;
            previousPosition = position; previousExtent = extent; hasPrevious = true;
        }
        return true;
    }

    private static ChartRect ScheduleEndGutters(Chart chart, VisualSceneBuilder builder, ChartRect viewport, ChartRect plot,
        IReadOnlyList<double> ticks, Func<double, string> format, TextStyle style) {
        if (!chart.Options.ShowAxes || !chart.Options.XAxis.Visible) return plot;
        var angle = Math.Max(-80, Math.Min(80, chart.Options.XAxis.LabelAngle));
        var extent = ticks.Select(tick => VisualAxisText.RotatedMetrics(builder.MeasureText(format(tick), style), angle).Width / 2).DefaultIfEmpty(0).Max();
        var left = Math.Min(plot.Width * .35, Math.Max(0, extent - (plot.Left - viewport.Left)));
        var right = Math.Min(plot.Width * .35, Math.Max(0, extent - (viewport.Right - plot.Right)));
        return new ChartRect(plot.Left + left, plot.Top, Math.Max(0, plot.Width - left - right), plot.Height);
    }
}
