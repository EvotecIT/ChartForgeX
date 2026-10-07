using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    private static void DrawBarSurface(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartSeries series,
        int pointIndex, ChartRect bounds, ChartColor color, VisualThemeColors colors, string role = "bar", bool range = false, bool horizontal = false, double? value = null) {
        var style = chart.Options.ResolvePreparedBarVisualStyle();
        var sourcePaint = VisualChartPaint.Series(series, color, pointIndex);
        var radius = Math.Min(chart.Options.HasPreparedBarCornerRadius ? style.CornerRadius : context.Theme.BarRadius, Math.Min(bounds.Width, bounds.Height) / 2);
        var pattern = pointIndex < series.PointFillPatterns.Count && series.PointFillPatterns[pointIndex].HasValue
            ? series.PointFillPatterns[pointIndex]!.Value : series.FillPattern;
        if (style.Kind == ChartBarStyle.SegmentedCapsule) {
            builder.Rect(bounds, ChartColorMath.WithOpacity(color, style.BodyOpacity), radius: radius, role: role,
                paint: VisualChartPaint.Fill(sourcePaint.WithOpacity(ChartColorMath.WithOpacity(color, style.BodyOpacity), style.BodyOpacity)));
            DrawPattern(builder, RoundedRectanglePath(bounds, radius), pattern, ChartColorMath.WithOpacity(color, style.BodyOpacity), ChartStateMark.Backdrop(chart.Options, colors, context.Frame), role + "-pattern");
            if (bounds.Width <= 0 || bounds.Height <= 0) return;
            if (!range) {
                var signedValue = value ?? series.Points[pointIndex].Y;
                var geometry = horizontal ? ChartSegmentedBarGeometry.Horizontal(style, bounds.Left, bounds.Top, bounds.Width, bounds.Height, signedValue)
                    : ChartSegmentedBarGeometry.Vertical(style, bounds.Left, bounds.Top, bounds.Width, bounds.Height, signedValue);
                DrawSegmentedCap(builder, geometry, style, color, role, sourcePaint);
            }
        } else {
            if (style.Kind == ChartBarStyle.Solid) {
                builder.RectGradient(bounds, new ChartPoint(bounds.Left, bounds.Top), horizontal ? new ChartPoint(bounds.Right, bounds.Top) : new ChartPoint(bounds.Left, bounds.Bottom),
                    new[] { new VisualGradientStop(0, ChartColorMath.WithOpacity(ChartMarkSurface.BarGradientTop(color).WithAlpha(color.A), ChartVisualPrimitives.BarFillOpacity),
                            VisualChartPaint.BarGradient(color, VisualChartPaint.SeriesRole(series, pointIndex), true, ChartVisualPrimitives.BarFillOpacity)),
                        new VisualGradientStop(1, ChartColorMath.WithOpacity(ChartMarkSurface.BarGradientBottom(color).WithAlpha(color.A), ChartVisualPrimitives.BarGradientBottomOpacity * ChartVisualPrimitives.BarFillOpacity),
                            VisualChartPaint.BarGradient(color, VisualChartPaint.SeriesRole(series, pointIndex), false, ChartVisualPrimitives.BarGradientBottomOpacity * ChartVisualPrimitives.BarFillOpacity)) },
                    radius: radius, role: role);
                if (ChartMarkSurface.HasBarHighlight(bounds.Width, bounds.Height)) {
                    var inset = ChartVisualPrimitives.BarHighlightInset;
                    builder.Line(bounds.Left + inset, bounds.Top + inset, bounds.Right - inset, bounds.Top + inset,
                        ChartColorMath.WithOpacity(ChartColor.White.WithAlpha(color.A), ChartVisualPrimitives.BarHighlightOpacity), ChartVisualPrimitives.BarHighlightStrokeWidth, role: role + "-highlight",
                        paint: VisualChartPaint.Stroke(SvgPaint.Literal(ChartColorMath.WithOpacity(ChartColor.White.WithAlpha(color.A), ChartVisualPrimitives.BarHighlightOpacity))));
                }
            } else builder.Rect(bounds, color, radius: radius, role: role, paint: VisualChartPaint.Fill(sourcePaint));
            DrawPattern(builder, RoundedRectanglePath(bounds, radius), pattern, color, ChartStateMark.Backdrop(chart.Options, colors, context.Frame), role + "-pattern");
        }
    }

    private static void DrawSegmentedCap(VisualSceneBuilder builder, ChartSegmentedBarGeometry geometry, ChartBarVisualStyle style, ChartColor color, string role, SvgPaint? source = null) {
        var sourcePaint = source ?? SvgPaint.Literal(color);
        DrawCap(geometry.SoftShadow, color, geometry.CapThickness + style.CapShadowSpread * 2, style.CapShadowOpacity * .36, role + "-cap-shadow-soft");
        DrawCap(geometry.Shadow, color, geometry.CapThickness, style.CapShadowOpacity, role + "-cap-shadow");
        DrawCap(geometry.Cap, color, geometry.CapThickness, style.CapOpacity, role + "-cap");
        DrawCap(geometry.Highlight, ChartColor.White.WithAlpha(color.A), Math.Max(1, geometry.CapThickness * ChartVisualPrimitives.SegmentedCapHighlightStrokeRatio), style.CapHighlightOpacity, role + "-cap-highlight");

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
