using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualScheduleCompiler {
    // A section name may take at most this share of the row-label gutter; task titles keep the rest.
    private const double SectionColumnShare = .45;
    // With a section column the row-label gutter may take this share of the chart instead of the usual one.
    private const double SectionedLabelShare = .45;

    /// <summary>The width a section column asks for: its widest name plus the gap before the task titles.</summary>
    private static double RangeSectionWidth(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, IReadOnlyList<RangeItem> items) {
        if (!chart.Options.ShowAxes || !chart.Options.YAxis.Visible) return 0;
        var style = SectionStyle(chart, context);
        var widest = items.Where(item => !item.Marker).Select(item => chart.Series[item.Index].LaneGroup)
            .Where(group => !string.IsNullOrWhiteSpace(group)).Select(group => builder.MeasureText(group!, style).Width).DefaultIfEmpty(0).Max();
        return widest > 0 ? Math.Ceiling(widest) + context.Theme.Spacing : 0;
    }

    /// <summary>The section column inside the final gutter. A squeezed gutter gives the section at most its share and the titles the rest.</summary>
    private static double RangeSectionColumn(VisualRenderContext context, ChartRect viewport, ChartRect plot, double requested, double titles) {
        if (requested <= 0) return 0;
        var gutter = Math.Max(0, plot.Left - viewport.Left - context.Theme.Spacing);
        return Math.Min(requested, Math.Max(gutter * SectionColumnShare, gutter - titles));
    }

    private static TextStyle SectionStyle(Chart chart, VisualRenderContext context) {
        var style = VisualStateSceneTools.TickStyle(chart, context).Clone();
        style.Font = style.Font.Clone(); style.Font.Weight = Math.Max(style.Font.Weight, 600);
        return style;
    }

    private static void RangeSectionBands(Chart chart, VisualRenderContext context, VisualSceneBuilder builder,
        ChartRect viewport, ChartRect plot, IReadOnlyList<RangeItem> items, double slot, double column) {
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
            var band = new ChartRect(viewport.Left, Math.Min(a, b) - slot / 2, viewport.Width, Math.Abs(b - a) + slot);
            using (builder.PushGroup("gantt-section-" + section, "gantt-section", new Dictionary<string, string> { ["data-section"] = group! })) {
                builder.Rect(band, fill, role: "gantt-section-band", paint: VisualChartPaint.Fill(SvgPaint.Literal(fill)));
                if (column > 0) SectionLabel(chart, context, builder, group!, new ChartRect(viewport.Left, band.Top, Math.Max(0, column - context.Theme.Spacing), band.Height), section);
            }
        }
    }

    /// <summary>Names a section once, beside its band; a narrow column wraps the name at whole words.</summary>
    private static void SectionLabel(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, string name, ChartRect bounds, int section) {
        var style = SectionStyle(chart, context);
        var wrapped = TextLayoutEngine.Layout(name, Math.Max(1, bounds.Width), style);
        var displayed = wrapped.Lines.Count > 1 && WrapsWholeWords(name, wrapped) && wrapped.Metrics.Height <= bounds.Height
            ? string.Join("\n", wrapped.Lines.Select(line => line.Text)) : null;
        VisualStateSceneTools.Text(builder, name, bounds, style, "gantt-section-label", "gantt-section-label-" + section, displayedText: displayed);
    }
}
