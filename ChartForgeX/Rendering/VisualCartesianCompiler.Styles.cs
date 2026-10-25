using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    // Positive surface direction means toward the right for horizontal bars and
    // toward the top for vertical bars, including when a subpixel extent collapses.
    private static double BarValueDirection(ChartAxis axis, double value) => axis.Reversed ? -value : value;

    private static double MappedBarDirection(ChartAxis axis, double value, double baseline, double endpoint, bool horizontal = false) {
        if (value == 0) return 0;
        var direction = horizontal ? endpoint - baseline : baseline - endpoint;
        return direction != 0 ? direction : BarValueDirection(axis, value);
    }

    // A nonzero capsule must retain colored ink at ordinary output density. Anchor
    // its minimum painted extent at the base without changing the numeric source.
    private static ChartRect VisibleSegmentBounds(Chart chart, ChartRect bounds, double direction, bool horizontal = false) =>
        VisibleSegmentBounds(chart.Options.ResolvePreparedBarVisualStyle(), bounds, direction, horizontal);

    private static ChartRect VisibleSegmentBounds(ChartBarVisualStyle style, ChartRect bounds, double direction, bool horizontal = false) {
        if (direction == 0 || style.Kind != ChartBarStyle.SegmentedCapsule) return bounds;
        if (horizontal && bounds.Width < 1)
            return new ChartRect(direction > 0 ? bounds.Left : bounds.Right - 1, bounds.Top, 1, bounds.Height);
        if (!horizontal && bounds.Height < 1)
            return new ChartRect(bounds.Left, direction > 0 ? bounds.Bottom - 1 : bounds.Top, bounds.Width, 1);
        return bounds;
    }

    private static ChartRect VerticalBarBounds(ChartBarVisualStyle style, double left, double width, double baseline, double endpoint, double direction) =>
        VisibleSegmentBounds(style, new ChartRect(left, Math.Min(endpoint, baseline), width, Math.Abs(baseline - endpoint)), direction);

    private static void DrawBarSurface(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartSeries series,
        int pointIndex, ChartRect bounds, ChartColor color, VisualThemeColors colors, string role = "bar", bool range = false, bool horizontal = false, double direction = 0, SvgColorRole? sourceRole = null, ChartRect? stackBounds = null) =>
        DrawBarSurface(builder, bounds, color, new BarSurfacePolicy(chart, context, series, pointIndex, colors, sourceRole), role, range, horizontal, direction, stackBounds);

    private static void DrawSegmentedCap(VisualSceneBuilder builder, ChartSegmentedBarGeometry geometry, ChartBarVisualStyle style, ChartColor color, string role, SvgPaint? source = null) {
        var sourcePaint = source ?? SvgPaint.Literal(color);
        DrawCap(geometry.SoftShadow, color, geometry.SoftShadowStrokeWidth(style), style.CapShadowOpacity * .36, role + "-cap-shadow-soft");
        DrawCap(geometry.Shadow, color, geometry.CapThickness, style.CapShadowOpacity, role + "-cap-shadow");
        DrawCap(geometry.Cap, color, geometry.CapThickness, style.CapOpacity, role + "-cap");
        DrawCap(geometry.Highlight, ChartColor.White.WithAlpha(color.A), geometry.HighlightStrokeWidth, style.CapHighlightOpacity, role + "-cap-highlight");

        void DrawCap(ChartSegmentedLine line, ChartColor paint, double width, double opacity, string role) {
            if (opacity > 0 && width > 0) builder.Line(line.X1, line.Y1, line.X2, line.Y2, ChartColorMath.WithOpacity(paint, opacity), width, role: role,
                paint: VisualChartPaint.Stroke(role.EndsWith("-cap-highlight", StringComparison.Ordinal) ? SvgPaint.Literal(ChartColorMath.WithOpacity(paint, opacity))
                    : sourcePaint.WithOpacity(ChartColorMath.WithOpacity(paint, opacity), opacity)));
        }
    }

    private static void DrawPattern(VisualSceneBuilder builder, ChartPath path, ChartFillPattern pattern, ChartColor fill, ChartColor backdrop, string role) {
        if (pattern == ChartFillPattern.None) return;
        var background = ChartColorMath.Blend(backdrop, ChartColor.FromRgb(fill.R, fill.G, fill.B), fill.A / 255d);
        var paint = ChartColorMath.AccessibleTextOnBackground(background).WithOpacity(ChartMarkSurface.HatchOpacity(pattern));
        builder.Pattern(path, pattern, paint, role: role);
    }

    private static ChartPath RectanglePath(ChartRect bounds) => new(new[] {
        ChartPathCommand.MoveTo(bounds.Left, bounds.Top), ChartPathCommand.LineTo(bounds.Right, bounds.Top),
        ChartPathCommand.LineTo(bounds.Right, bounds.Bottom), ChartPathCommand.LineTo(bounds.Left, bounds.Bottom)
    });

    private static ChartPath RoundedRectanglePath(ChartRect bounds, double radius) => ChartPathBuilder.RoundedRectangle(bounds, radius);

}
