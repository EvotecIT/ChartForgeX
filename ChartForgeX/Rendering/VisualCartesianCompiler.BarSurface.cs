using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    /// <summary>Closed native bar paint policy shared by ordinary and detached value rendering.</summary>
    private sealed class BarSurfacePolicy {
        internal BarSurfacePolicy(Chart chart, VisualRenderContext context, ChartSeries series, int pointIndex, VisualThemeColors colors, SvgColorRole? sourceRole = null) {
            Style = chart.Options.ResolvePreparedBarVisualStyle().Clone();
            CornerRadius = chart.Options.HasPreparedBarCornerRadius ? Style.CornerRadius : context.Theme.BarRadius;
            Role = sourceRole ?? VisualChartPaint.SeriesRole(series, pointIndex);
            Pattern = pointIndex < series.PointFillPatterns.Count && series.PointFillPatterns[pointIndex].HasValue
                ? series.PointFillPatterns[pointIndex]!.Value : series.FillPattern;
            Backdrop = ChartStateMark.Backdrop(chart.Options, colors, context.Frame);
        }
        internal ChartBarVisualStyle Style { get; }
        internal double CornerRadius { get; }
        internal SvgColorRole Role { get; }
        internal ChartFillPattern Pattern { get; }
        internal ChartColor Backdrop { get; }
    }

    private static void DrawBarSurface(VisualSceneBuilder builder, ChartRect bounds, ChartColor color, BarSurfacePolicy policy,
        string role = "bar", bool range = false, bool horizontal = false, double direction = 0, ChartRect? stackBounds = null) {
        var style = policy.Style;
        var resolvedRole = policy.Role;
        var sourcePaint = SvgPaint.Of(color, resolvedRole);
        var radius = Math.Min(policy.CornerRadius, Math.Min(bounds.Width, bounds.Height) / 2);
        if (stackBounds.HasValue) {
            var envelope = VisibleSegmentBounds(style, stackBounds.Value, direction, horizontal);
            // A terminal nonzero capsule may paint beyond its mapped total to retain
            // one pixel of ink. Its outer clip must include that painted extent.
            var left = Math.Min(envelope.Left, bounds.Left); var top = Math.Min(envelope.Top, bounds.Top);
            stackBounds = new ChartRect(left, top, Math.Max(envelope.Right, bounds.Right) - left, Math.Max(envelope.Bottom, bounds.Bottom) - top);
        }
        // Every signed stack has one rounded envelope. Segment bodies stay square at shared boundaries.
        using var stackClip = stackBounds.HasValue ? builder.PushClip(RoundedRectanglePath(stackBounds.Value,
            Math.Min(policy.CornerRadius,
                Math.Min(stackBounds.Value.Width, stackBounds.Value.Height) / 2))) : null;
        if (stackBounds.HasValue) radius = 0;
        var pattern = policy.Pattern;
        if (style.Kind == ChartBarStyle.SegmentedCapsule) {
            builder.Rect(bounds, ChartColorMath.WithOpacity(color, style.BodyOpacity), radius: radius, role: role,
                paint: VisualChartPaint.Fill(sourcePaint.WithOpacity(ChartColorMath.WithOpacity(color, style.BodyOpacity), style.BodyOpacity)));
            DrawPattern(builder, RoundedRectanglePath(bounds, radius), pattern, ChartColorMath.WithOpacity(color, style.BodyOpacity), policy.Backdrop, role + "-pattern");
            if (bounds.Width <= 0 || bounds.Height <= 0) return;
            var outerEnd = !stackBounds.HasValue || (horizontal
                ? direction > 0 ? bounds.Right >= stackBounds.Value.Right - .001 : bounds.Left <= stackBounds.Value.Left + .001
                : direction > 0 ? bounds.Top <= stackBounds.Value.Top + .001 : bounds.Bottom >= stackBounds.Value.Bottom - .001);
            if (!range && outerEnd) {
                var geometry = horizontal ? ChartSegmentedBarGeometry.Horizontal(style, bounds.Left, bounds.Top, bounds.Width, bounds.Height, direction)
                    : ChartSegmentedBarGeometry.Vertical(style, bounds.Left, bounds.Top, bounds.Width, bounds.Height, direction);
                DrawSegmentedCap(builder, geometry, style, color, role, sourcePaint);
            }
        } else {
            if (style.Kind == ChartBarStyle.Solid) {
                builder.RectGradient(bounds, new ChartPoint(bounds.Left, bounds.Top), horizontal ? new ChartPoint(bounds.Right, bounds.Top) : new ChartPoint(bounds.Left, bounds.Bottom),
                    new[] { new VisualGradientStop(0, ChartColorMath.WithOpacity(ChartMarkSurface.BarGradientTop(color).WithAlpha(color.A), ChartVisualPrimitives.BarFillOpacity),
                            VisualChartPaint.BarGradient(color, resolvedRole, true, ChartVisualPrimitives.BarFillOpacity)),
                        new VisualGradientStop(1, ChartColorMath.WithOpacity(ChartMarkSurface.BarGradientBottom(color).WithAlpha(color.A), ChartVisualPrimitives.BarGradientBottomOpacity * ChartVisualPrimitives.BarFillOpacity),
                            VisualChartPaint.BarGradient(color, resolvedRole, false, ChartVisualPrimitives.BarGradientBottomOpacity * ChartVisualPrimitives.BarFillOpacity)) },
                    radius: radius, role: role);
                if (ChartMarkSurface.HasBarHighlight(bounds.Width, bounds.Height)) {
                    var inset = ChartVisualPrimitives.BarHighlightInset;
                    builder.Line(bounds.Left + inset, bounds.Top + inset, bounds.Right - inset, bounds.Top + inset,
                        ChartColorMath.WithOpacity(ChartColor.White.WithAlpha(color.A), ChartVisualPrimitives.BarHighlightOpacity), ChartVisualPrimitives.BarHighlightStrokeWidth, role: role + "-highlight",
                        paint: VisualChartPaint.Stroke(SvgPaint.Literal(ChartColorMath.WithOpacity(ChartColor.White.WithAlpha(color.A), ChartVisualPrimitives.BarHighlightOpacity))));
                }
            } else builder.Rect(bounds, color, radius: radius, role: role, paint: VisualChartPaint.Fill(sourcePaint));
            DrawPattern(builder, RoundedRectanglePath(bounds, radius), pattern, color, policy.Backdrop, role + "-pattern");
        }
    }

}
