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
        IReadOnlyList<double> ticks, Func<double, double> project, Func<double, string> format, TextStyle style, bool hasFixedTicks,
        double? minimum = null, double? maximum = null, double? calendarAnchor = null) {
        var axis = chart.Options.XAxis;
        if (ticks.Count < 2 || axis.Labels.Count > 0 || hasFixedTicks || axis.LabelDensity == ChartLabelDensity.All) return ticks;
        var policy = axis.Clone();
        // Lane models use calendar ticks even when their source axis retains the default linear scale.
        if (chart.Series[0].Kind is ChartSeriesKind.StateTimeline or ChartSeriesKind.GanttLane) policy.Scale = ChartScaleKind.Time;
        // Automatic Gantt captions identify dates on day-scale windows, including
        // rejected dense fixed intervals. Keep the generated ticks on whole days;
        // authored clock formats and successful fixed intervals remain authoritative.
        var min = minimum ?? ticks[0]; var max = maximum ?? ticks[ticks.Count - 1];
        var minimumStepDays = chart.Series[0].Kind == ChartSeriesKind.Gantt && max - min >= 1
            && ChartTimeScale.HasDayCaptionPrecision(axis, format, min) ? 1 : 0;
        bool Fits(IReadOnlyList<double> candidates) => ScheduleCaptionsFit(chart, context, builder, candidates, project, format,
            GeneratedScheduleTickStyle(chart, context, builder, candidates, project, format, style),
            comfortable: axis.LabelDensity != ChartLabelDensity.Dense && !axis.HasExplicitTickCount);
        bool Accepts(IReadOnlyList<double> candidates) => ChartTimeScale.HasDistinctAdjacentLabels(candidates.Select(format)) && Fits(candidates);
        // A rejected fixed interval uses the same automatic calendar phase and
        // spacing as an ordinary automatic axis; the authored setting is retained.
        var automaticCount = ChartTimeScale.AutomaticTickCount(axis);
        if (policy.Scale == ChartScaleKind.Time && ChartTimeScale.GenerateFitting(policy, min, max,
            Accepts, allowCompactDates: false, minimumStepDays: minimumStepDays,
            automaticTickCount: automaticCount,
            calendarAnchor: minimumStepDays >= 1 ? calendarAnchor ?? min : null,
            selectTicks: minimumStepDays == 0 && minimum.HasValue && maximum.HasValue
                ? candidates => ScheduleClockEndpoints(candidates, min, max, Fits) : null) is { } calendarTicks)
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
        IReadOnlyList<double> ticks, Func<double, double> project, Func<double, string> format, TextStyle style, bool comfortable = false) {
        if (chart.Options.XAxis.LabelDensity == ChartLabelDensity.All) return new HashSet<double>(ticks);
        var dayInterval = chart.Options.GanttTickInterval;
        var projectStart = chart.Series.Where(series => series.Kind == ChartSeriesKind.Gantt && series.GanttItems.Count > 0)
            .Select(series => series.GanttItems[0].Start).DefaultIfEmpty(double.NaN).Min();
        var anchor = dayInterval?.Unit == ChartTimeTickUnit.Day && dayInterval.Count == 1 && ChartMath.IsFinite(projectStart)
            ? ChartTimeScale.ToDisplayTime(chart.Options.XAxis, projectStart)?.Date : null;
        for (var stride = 1; stride <= ticks.Count; stride++) {
            var selected = ticks.Where((tick, index) => anchor.HasValue && ChartTimeScale.ToDisplayTime(chart.Options.XAxis, tick) is { } local
                ? (local.Date - anchor.Value).Days % stride == 0 : index % stride == 0).ToArray();
            if (ScheduleCaptionsFit(chart, context, builder, selected, project, format, style, comfortable)) return new HashSet<double>(selected);
        }
        return new HashSet<double>();
    }

    private static bool ScheduleCaptionsFit(Chart chart, VisualRenderContext context, VisualSceneBuilder builder,
        IReadOnlyList<double> ticks, Func<double, double> project, Func<double, string> format, TextStyle style, bool comfortable = false) {
        var axis = chart.Options.XAxis;
        var gap = axis.LabelDensity == ChartLabelDensity.Dense ? 0 : axis.LabelDensity == ChartLabelDensity.Relaxed ? context.Theme.Spacing * 2 : context.Theme.Spacing / 2;
        var angle = Math.Max(-80, Math.Min(80, axis.LabelAngle));
        var previousPosition = 0d; var previousExtent = 0d; var hasPrevious = false;
        foreach (var tick in ticks) {
            var text = format(tick);
            if (text.Length == 0) continue;
            var extent = VisualAxisText.RotatedMetrics(builder.MeasureText(text, style), angle).Width;
            var position = project(tick);
            var length = ticks.Count > 1 ? Math.Abs(project(ticks[ticks.Count - 1]) - project(ticks[0])) : 0;
            var requiredGap = comfortable ? ChartTimeScale.ComfortableLabelGap(extent, previousExtent, gap, length, style.EffectiveFontSize,
                axis.LabelDensity == ChartLabelDensity.Auto && !axis.HasExplicitTickCount) : gap;
            if (hasPrevious && Math.Abs(position - previousPosition) + .0001 < (extent + previousExtent) / 2 + requiredGap) return false;
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
