using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    private static void DrawBarSurface(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartSeries series,
        int pointIndex, ChartRect bounds, ChartColor color, VisualThemeColors colors) {
        var style = chart.Options.ResolvePreparedBarVisualStyle();
        var radius = Math.Min(chart.Options.HasPreparedBarCornerRadius ? style.CornerRadius : context.Theme.BarRadius, bounds.Width / 2);
        var pattern = pointIndex < series.PointFillPatterns.Count && series.PointFillPatterns[pointIndex].HasValue
            ? series.PointFillPatterns[pointIndex]!.Value : series.FillPattern;
        if (style.Kind == ChartBarStyle.SegmentedCapsule) {
            builder.Rect(bounds, ChartColorMath.WithOpacity(color, style.BodyOpacity), radius: radius, role: "bar");
            DrawPattern(builder, RoundedRectanglePath(bounds, radius), pattern, ChartColorMath.WithOpacity(color, style.BodyOpacity), colors.Surface, "bar-pattern");
            if (bounds.Width <= 0 || bounds.Height <= 0) return;
            var geometry = ChartSegmentedBarGeometry.Vertical(style, bounds.Left, bounds.Top, bounds.Width, bounds.Height, series.Points[pointIndex].Y);
            DrawCap(geometry.SoftShadow, color, geometry.CapThickness + style.CapShadowSpread * 2, style.CapShadowOpacity * .36, "bar-cap-shadow-soft");
            DrawCap(geometry.Shadow, color, geometry.CapThickness, style.CapShadowOpacity, "bar-cap-shadow");
            DrawCap(geometry.Cap, color, geometry.CapThickness, style.CapOpacity, "bar-cap");
            DrawCap(geometry.Highlight, ChartColor.White.WithAlpha(color.A), Math.Max(1, geometry.CapThickness * ChartVisualPrimitives.SegmentedCapHighlightStrokeRatio), style.CapHighlightOpacity, "bar-cap-highlight");
        } else {
            if (style.Kind == ChartBarStyle.Solid) {
                builder.RectGradient(bounds, new ChartPoint(bounds.Left, bounds.Top), new ChartPoint(bounds.Left, bounds.Bottom),
                    new[] { new VisualGradientStop(0, ChartColorMath.WithOpacity(ChartMarkSurface.BarGradientTop(color).WithAlpha(color.A), ChartVisualPrimitives.BarFillOpacity)),
                        new VisualGradientStop(1, ChartColorMath.WithOpacity(ChartMarkSurface.BarGradientBottom(color).WithAlpha(color.A), ChartVisualPrimitives.BarGradientBottomOpacity * ChartVisualPrimitives.BarFillOpacity)) },
                    radius: radius, role: "bar");
                if (ChartMarkSurface.HasBarHighlight(bounds.Width, bounds.Height)) {
                    var inset = ChartVisualPrimitives.BarHighlightInset;
                    builder.Line(bounds.Left + inset, bounds.Top + inset, bounds.Right - inset, bounds.Top + inset,
                        ChartColorMath.WithOpacity(ChartColor.White.WithAlpha(color.A), ChartVisualPrimitives.BarHighlightOpacity), ChartVisualPrimitives.BarHighlightStrokeWidth, role: "bar-highlight");
                }
            } else builder.Rect(bounds, color, radius: radius, role: "bar");
            DrawPattern(builder, RoundedRectanglePath(bounds, radius), pattern, color, colors.Surface, "bar-pattern");
        }

        void DrawCap(ChartSegmentedLine line, ChartColor paint, double width, double opacity, string role) {
            if (opacity > 0 && width > 0) builder.Line(line.X1, line.Y1, line.X2, line.Y2, ChartColorMath.WithOpacity(paint, opacity), width, role: role);
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

    private static ChartPath RoundedRectanglePath(ChartRect bounds, double radius) {
        radius = Math.Min(radius, Math.Min(bounds.Width, bounds.Height) / 2);
        if (radius <= 0) return RectanglePath(bounds);
        const double k = .5522847498307936;
        var r = radius; var c = r * k;
        var x = bounds.Left; var y = bounds.Top; var right = bounds.Right; var bottom = bounds.Bottom;
        return new ChartPath(new[] {
            ChartPathCommand.MoveTo(x + r, y), ChartPathCommand.LineTo(right - r, y),
            ChartPathCommand.CubicTo(right - r + c, y, right, y + r - c, right, y + r), ChartPathCommand.LineTo(right, bottom - r),
            ChartPathCommand.CubicTo(right, bottom - r + c, right - r + c, bottom, right - r, bottom), ChartPathCommand.LineTo(x + r, bottom),
            ChartPathCommand.CubicTo(x + r - c, bottom, x, bottom - r + c, x, bottom - r), ChartPathCommand.LineTo(x, y + r),
            ChartPathCommand.CubicTo(x, y + r - c, x + r - c, y, x + r, y)
        });
    }

    private static ChartPath EllipsePath(double x, double y, double rx, double ry) {
        const double k = .5522847498307936;
        return new ChartPath(new[] {
            ChartPathCommand.MoveTo(x + rx, y), ChartPathCommand.CubicTo(x + rx, y + ry * k, x + rx * k, y + ry, x, y + ry),
            ChartPathCommand.CubicTo(x - rx * k, y + ry, x - rx, y + ry * k, x - rx, y),
            ChartPathCommand.CubicTo(x - rx, y - ry * k, x - rx * k, y - ry, x, y - ry),
            ChartPathCommand.CubicTo(x + rx * k, y - ry, x + rx, y - ry * k, x + rx, y)
        });
    }
}
