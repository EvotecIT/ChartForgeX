using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualScheduleCompiler {
    private static void RangeSectionBands(Chart chart, VisualRenderContext context, VisualSceneBuilder builder,
        ChartRect viewport, ChartRect plot, IReadOnlyList<RangeItem> items, double slot) {
        var colors = context.Theme.Resolve(context.ThemeMode);
        var section = 0;
        for (var index = 0; index < items.Count; index++) {
            var first = items[index];
            var group = chart.Series[first.Index].LaneGroup;
            if (first.Marker || string.IsNullOrWhiteSpace(group)) continue;
            var last = first;
            while (index + 1 < items.Count && !items[index + 1].Marker && chart.Series[items[index + 1].Index].LaneGroup == group)
                last = items[++index];
            var a = ScheduleRowCenter(chart, plot, (first.Row + .5) * slot);
            var b = ScheduleRowCenter(chart, plot, (last.Row + .5) * slot);
            var fill = DiagramTheme.SectionBandFill(colors, section++);
            using (builder.PushGroup("gantt-section-" + section, "gantt-section", new Dictionary<string, string> { ["data-section"] = group! }))
                builder.Rect(new ChartRect(viewport.Left, Math.Min(a, b) - slot / 2, viewport.Width, Math.Abs(b - a) + slot), fill,
                    role: "gantt-section-band", paint: VisualChartPaint.Fill(SvgPaint.Literal(fill)));
        }
    }
}
