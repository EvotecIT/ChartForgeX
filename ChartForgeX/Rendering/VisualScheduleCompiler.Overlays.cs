using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualScheduleCompiler {
    private static bool HasRowState(ChartMarkOptions? options) => options?.Current == true || options?.Selected == true;

    private static TextStyle EmphasizedRowStyle(TextStyle source) {
        var style = source.Clone(); style.Font = style.Font.Clone();
        style.Font.Weight = Math.Max(style.Font.Weight, 600);
        return style;
    }

    private static void ScheduleRowState(Chart chart, VisualRenderContext context, VisualSceneBuilder builder,
        ChartRect viewport, ChartRect plot, double top, double height, double pitch, int index, ChartMarkOptions options) {
        var slot = pitch > 0 ? pitch : height;
        var center = top + height / 2; var inset = Math.Min(3, slot * .1);
        var bandTop = Math.Max(plot.Top, center - slot / 2 + inset);
        var bandBottom = Math.Min(plot.Bottom, center + slot / 2 - inset);
        if (bandBottom <= bandTop) return;
        var series = chart.Series[index];
        var color = ChartSeriesColours.Resolve(series, index, context.Theme.Resolve(context.ThemeMode));
        var role = options.Selected ? "schedule-selected-row" : "schedule-current-row";
        using (builder.PushGroup(role, null, LaneMetadata(chart, index, null))) {
            builder.Rect(new ChartRect(viewport.Left, bandTop, viewport.Width, bandBottom - bandTop), color,
                strokeWidth: 0, role: role + "-band", paint: VisualChartPaint.Fill(VisualChartPaint.Series(series, color)),
                opacity: options.Selected ? .14 : .08);
            builder.Line(viewport.Left + 1.5, bandTop, viewport.Left + 1.5, bandBottom, color, 3,
                role + "-accent", paint: VisualChartPaint.Stroke(VisualChartPaint.Series(series, color)));
        }
    }

    private static void ScheduleBands(Chart chart, VisualRenderContext context, VisualSceneBuilder builder,
        ChartRect plot, Func<double, double> project) {
        var bands = chart.Annotations.Where(annotation => annotation.Kind == ChartAnnotationKind.VerticalBand).ToArray();
        if (bands.Length == 0) return;
        VisualAnnotationCompiler.Draw(bands, context, builder, plot, project, _ => 0,
            context.Theme.Resolve(context.ThemeMode), true, new List<LabelObstacle>(), clipGeometry: true);
    }

    private static void OpenEndFade(Chart chart, VisualSceneBuilder builder, ChartRect bounds,
        ChartColor fill, SvgPaint paint, double radius, string role) {
        var start = new ChartPoint(chart.Options.XAxis.Reversed ? bounds.Right : bounds.Left, bounds.Top);
        var end = new ChartPoint(chart.Options.XAxis.Reversed ? bounds.Left : bounds.Right, bounds.Top);
        var transparent = fill.WithAlpha(0);
        builder.RectGradient(bounds, start, end, new[] {
            new VisualGradientStop(0, fill, paint), new VisualGradientStop(.7, fill, paint),
            new VisualGradientStop(1, transparent, paint)
        }, radius: radius, role: role);
    }

    private static void OpenEndArrow(Chart chart, VisualSceneBuilder builder, ChartRect bounds, ChartColor ink) {
        var extent = Math.Min(bounds.Height / 3, bounds.Width / 2);
        var tip = chart.Options.XAxis.Reversed ? bounds.Left : bounds.Right;
        var direction = chart.Options.XAxis.Reversed ? -1 : 1;
        builder.Path(new ChartPath(new[] {
            ChartPathCommand.MoveTo(tip - direction * extent, bounds.Top + bounds.Height / 2 - extent),
            ChartPathCommand.LineTo(tip, bounds.Top + bounds.Height / 2),
            ChartPathCommand.LineTo(tip - direction * extent, bounds.Top + bounds.Height / 2 + extent)
        }), stroke: ink, strokeWidth: GanttLaneOpenEndStrokeWidth, role: "gantt-lane-open-end",
            paint: VisualChartPaint.Stroke(SvgPaint.Literal(ink)));
    }
}
