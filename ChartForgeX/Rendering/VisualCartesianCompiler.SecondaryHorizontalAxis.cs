using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    /// <summary>Paints the independently measured top value axis of a horizontal Cartesian mixture.</summary>
    private static void DrawSecondaryHorizontalAxis(Chart chart, VisualRenderContext context, VisualSceneBuilder builder,
        ChartRect plot, ChartRect viewport, ChartRange range, ChartMapper map, AxisLabelCache labels, VisualThemeColors colors,
        TextStyle style, double spacing) {
        var axis = chart.Options.SecondaryXAxis;
        if (!axis.Visible) return;
        labels.IncludeValueTicks(axis, range.MinX, range.MaxX);
        var ticks = labels.Ticks(axis, range.MinX, range.MaxX);
        if (axis.ShowLine) builder.Line(plot.Left, plot.Top, plot.Right, plot.Top, colors.Axis, context.Theme.AxisStrokeWidth,
            role: "axis-secondary-x", paint: VisualChartPaint.Stroke(colors.Axis, SvgColorRole.Axis));
        var height = TickMetrics(builder, axis, range.MinX, range.MaxX, style, chart.Options.ValueFormatter, labels).Height;
        var bounds = new ChartRect(viewport.Left, Math.Max(viewport.Top, plot.Top - spacing - height), viewport.Width,
            Math.Max(0, Math.Min(height, plot.Top - viewport.Top - spacing)));
        DrawAxisLabels(builder, chart.Options, axis, ticks, map.X, true, true, bounds, style, spacing,
            chart.Options.ValueFormatter, labels, plot.Width);
        if (chart.SecondaryXAxisTitle.Length > 0) {
            var categoryTitle = chart.Options.YAxis.Visible && chart.YAxisTitle.Length > 0
                ? builder.MeasureText(chart.YAxisTitle, chart.Options.AxisTitleStyle.Resolve(new TextStyle {
                    Font = context.Font, FontSize = context.Theme.Typography.AxisTitleSize, Color = colors.Foreground })).Height + spacing : 0;
            DrawAxisTitle(chart, context, builder, ChartTimeScale.DecorateTitle(axis, chart.SecondaryXAxisTitle),
                new ChartRect(plot.Left, viewport.Top + categoryTitle, plot.Width,
                    Math.Max(0, bounds.Top - spacing - viewport.Top - categoryTitle)), colors, TextAlignment.Center, "axis-secondary-x-title");
        }
    }
}
