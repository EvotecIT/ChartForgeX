using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Diagnostics;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualScheduleCompiler {
    /// <summary>Paints complete schedule captions on their own ticks within the reserved viewport gutters.</summary>
    private static void TickLabels(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect viewport,
        ScheduleLayout layout, IReadOnlyList<double> ticks, Func<double, double> project, Func<double, string> format, TextStyle style) {
        var plot = layout.Plot; var gap = context.Theme.Spacing;
        var rotation = Math.Max(-80, Math.Min(80, chart.Options.XAxis.LabelAngle));
        var top = Math.Min(viewport.Bottom, plot.Bottom + gap / 2);
        var selected = ScheduleCaptionStride(chart, context, builder, ticks, project, format, style);
        var omitted = false;
        for (var index = 0; index < ticks.Count; index++) {
            var text = format(ticks[index]); var x = project(ticks[index]); var id = "schedule-tick-" + index;
            if (!selected.Contains(ticks[index])) {
                builder.AddRegion(new VisualSemanticRegion(id, "schedule-tick-label", new ChartRect(x, top, 0, 0), text));
                omitted = true;
                continue;
            }
            var tickStyle = style;
            if (chart.Options.TryGetXAxisLabelHighlight(ticks[index], out var highlight)) { tickStyle = style.Clone(); tickStyle.Color = highlight; }
            var metrics = builder.MeasureText(text, tickStyle);
            var footprint = VisualAxisText.RotatedMetrics(metrics, rotation);
            var centerY = top + footprint.Height / 2;
            var bounds = new ChartRect(x - footprint.Width / 2, top, footprint.Width, footprint.Height);
            var bottom = Math.Min(viewport.Bottom, plot.Bottom + layout.AxisReserve);
            if (bounds.Left < viewport.Left - .0001 || bounds.Right > viewport.Right + .0001 || bounds.Bottom > bottom + .0001) {
                builder.AddRegion(new VisualSemanticRegion(id, "schedule-tick-label", new ChartRect(x, top, 0, 0), text));
                omitted = true;
                continue;
            }
            builder.AddRegion(new VisualSemanticRegion(id, "schedule-tick-label", bounds, text));
            if (text.Length == 0) continue;
            tickStyle = tickStyle.Clone(); tickStyle.Alignment = TextAlignment.Center;
            using (builder.PushClip(viewport))
            using (builder.PushRotation(rotation, x, centerY))
                builder.Text(text, x, centerY - metrics.Height / 2 + builder.TextAscent(tickStyle), tickStyle,
                    "schedule-tick-label", id, paint: VisualChartPaint.Text(tickStyle));
        }
        if (omitted) builder.AddDiagnostic(new VisualDiagnostic("schedule.axis-label-overflow",
            "Some axis captions did not fit their measured strip or regular stride; their complete values remain in descriptive regions."));
    }
}
