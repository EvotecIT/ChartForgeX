using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    private static bool HasCompleteAxisCrossing(Chart chart) => chart.Options.ShowAxes &&
        (chart.Options.XAxis.Visible && chart.Options.XAxis.CrossAt.HasValue || chart.Options.YAxis.Visible && chart.Options.YAxis.CrossAt.HasValue
        || chart.Options.SecondaryXAxis.Visible && chart.Options.SecondaryXAxis.CrossAt.HasValue || chart.Options.SecondaryYAxis.Visible && chart.Options.SecondaryYAxis.CrossAt.HasValue);

    /// <summary>Paints opted-in interior anatomy after collecting actual marks, retaining the existing measured plot and axis text owners.</summary>
    private static void DrawCrossingAxes(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, ChartRect viewport,
        ChartRange primaryRange, ChartMapper primaryMap, ChartRange? secondaryRange, ChartMapper? secondaryMap, VisualThemeColors colors,
        AxisLabelCache labels, List<LabelObstacle> obstacles) {
        var horizontal = ChartCartesianOrientation.IsHorizontal(chart);
        var xAxis = chart.Options.XAxis; var yAxis = chart.Options.YAxis;
        if (horizontal) { var axes = HorizontalAxes(chart, labels); xAxis = axes.Value; yAxis = axes.Category; }
        if (horizontal) labels.IncludeValueTicks(xAxis, primaryRange.MinX, primaryRange.MaxX);
        var xTicks = labels.Ticks(xAxis, primaryRange.MinX, primaryRange.MaxX);
        var yTicks = horizontal && !UsesGeneratedHorizontalCategoryTicks(chart, yAxis)
            ? HorizontalCategoryTicks(chart, primaryRange, yAxis) : labels.Ticks(yAxis, primaryRange.MinY, primaryRange.MaxY);
        DrawCrossingAxis(chart.Options.XAxis, xAxis, xTicks, primaryMap.X, primaryRange.MinX, primaryRange.MaxX,
            XAxisTitle(chart), true, false, !horizontal);
        DrawCrossingAxis(chart.Options.YAxis, yAxis, yTicks, primaryMap.Y, primaryRange.MinY, primaryRange.MaxY,
            horizontal ? ChartTimeScale.DecorateTitle(chart.Options.YAxis, chart.YAxisTitle) : chart.YAxisTitle, false, false, horizontal);
        if (secondaryRange != null && secondaryMap != null) {
            if (horizontal) {
                var secondary = chart.Options.SecondaryXAxis; labels.IncludeValueTicks(secondary, secondaryRange.MinX, secondaryRange.MaxX);
                DrawCrossingAxis(secondary, secondary, labels.Ticks(secondary, secondaryRange.MinX, secondaryRange.MaxX), secondaryMap.X,
                    secondaryRange.MinX, secondaryRange.MaxX, ChartTimeScale.DecorateTitle(secondary, chart.SecondaryXAxisTitle), true, true, false);
            } else {
                var secondary = chart.Options.SecondaryYAxis;
                DrawCrossingAxis(secondary, secondary, labels.Ticks(secondary, secondaryRange.MinY, secondaryRange.MaxY), secondaryMap.Y,
                    secondaryRange.MinY, secondaryRange.MaxY, chart.SecondaryYAxisTitle, false, true, false);
            }
        }

        void DrawCrossingAxis(ChartAxis sourceAxis, ChartAxis tickAxis, IReadOnlyList<double> ticks, Func<double, double> project,
            double minimum, double maximum, string title, bool physicalX, bool secondary, bool category) {
            if (!sourceAxis.Visible || !sourceAxis.CrossAt.HasValue) return;
            var value = sourceAxis.CrossAt.Value; var role = physicalX ? secondary ? "axis-secondary-x" : "axis-x" : secondary ? "axis-secondary-y" : "axis-y";
            var fallback = physicalX ? horizontal ? chart.Options.ValueFormatter : null : horizontal ? null : chart.Options.ValueFormatter;
            var metadata = AxisCrossingMetadata(value, physicalX, true);
            var visible = TryAxisCrossingCoordinate(chart, primaryRange, primaryMap, value, physicalX, out var track);
            if (!visible) metadata["data-cfx-visible"] = "false";
            using (builder.PushGroup(null, "axis-crossing", metadata)) {
                if (!visible) {
                    RetainCrossingAxisFacts(chart, context, builder, colors, labels, tickAxis, ticks, fallback, title, role, plot);
                    if (category) DrawCategoryGroups(chart, context, builder, labels, project, minimum, maximum, default, colors);
                    builder.AddDiagnostic(new VisualDiagnostic("cartesian.axis-crossing-outside-domain", "An authored complete axis crossing is outside the opposite primary physical-axis window; its rule, captions and title were omitted."));
                    return;
                }
                if (sourceAxis.ShowLine) using (builder.PushClip(plot))
                    builder.Line(physicalX ? plot.Left : track, physicalX ? track : plot.Top,
                        physicalX ? plot.Right : track, physicalX ? track : plot.Bottom, colors.Axis, context.Theme.AxisStrokeWidth,
                        role: role, paint: VisualChartPaint.Stroke(colors.Axis, SvgColorRole.Axis));
                if (physicalX) DrawCrossingHorizontalText(chart, context, builder, plot, viewport, tickAxis, ticks, project, minimum, maximum,
                    track, secondary, category, title, colors, labels, obstacles, fallback, role);
                else DrawCrossingVerticalText(chart, context, builder, plot, viewport, tickAxis, ticks, project, minimum, maximum,
                    track, secondary, category, title, colors, labels, obstacles, fallback, role);
            }
        }
    }

    private static void RetainCrossingAxisFacts(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, VisualThemeColors colors, AxisLabelCache labels, ChartAxis axis,
        IReadOnlyList<double> ticks, Func<double, string>? fallback, string title, string role, ChartRect plot) {
        var style = CrossingTickStyle(chart, context, colors); var index = 0;
        foreach (var tick in labels.SemanticTicks(axis, ticks)) {
            var text = labels.Format(axis, tick, fallback, ticks);
            builder.AddRegion(new VisualSemanticRegion(role + "-label-" + Number(index++), role + "-label",
                new ChartRect(plot.Left, plot.Top, 0, 0), TextCaseTransformer.Apply(text, style.TextCase, CultureInfo.InvariantCulture) + " (" + Number(tick) + ")"));
        }
        if (title.Length > 0) builder.AddRegion(new VisualSemanticRegion(role + "-title", role + "-title", new ChartRect(plot.Left, plot.Top, 0, 0),
            TextCaseTransformer.Apply(title, chart.Options.AxisTitleStyle.TextCase ?? TextCaseTransform.None, CultureInfo.InvariantCulture)));
    }
}
