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
            // Sparkline bars are micro marks on a sample grid: square, unless a radius is authored.
            CornerRadius = chart.Options.HasPreparedBarCornerRadius ? Style.CornerRadius : chart.Options.IsSparkline ? 0 : context.Theme.BarRadius;
            Role = sourceRole ?? VisualChartPaint.SeriesRole(series, pointIndex);
            Pattern = pointIndex < series.PointFillPatterns.Count && series.PointFillPatterns[pointIndex].HasValue
                ? series.PointFillPatterns[pointIndex]!.Value : series.FillPattern;
            Backdrop = ChartStateMark.Backdrop(chart.Options, colors, context.Frame);
            _series = series; _pointIndex = pointIndex; _explicitRole = sourceRole.HasValue;
        }
        private readonly ChartSeries _series;
        private readonly int _pointIndex;
        private readonly bool _explicitRole;
        /// <summary>The source paint, by palette slot for automatic series colours.</summary>
        internal SvgPaint Source(ChartColor color) => _explicitRole ? SvgPaint.Of(color, Role) : VisualChartPaint.Series(_series, color, _pointIndex);
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
        var sourcePaint = policy.Source(color);
        var radius = Math.Min(policy.CornerRadius, Math.Min(bounds.Width, bounds.Height) / 2);
        if (stackBounds.HasValue) {
            var envelope = VisibleSegmentBounds(style, stackBounds.Value, direction, horizontal);
            // A terminal nonzero capsule may paint beyond its mapped total to retain
            // one pixel of ink. Its outer clip must include that painted extent.
            var left = Math.Min(envelope.Left, bounds.Left); var top = Math.Min(envelope.Top, bounds.Top);
            stackBounds = new ChartRect(left, top, Math.Max(envelope.Right, bounds.Right) - left, Math.Max(envelope.Bottom, bounds.Bottom) - top);
            // Stacked segments are separate marks: a surface gap where they meet (design rule 4).
            bounds = ChartMarkSurface.StackSegmentBounds(bounds, stackBounds.Value, horizontal);
        }
        // Every signed stack has one envelope, rounded at its data end only. Segment bodies stay square at shared boundaries.
        using var stackClip = stackBounds.HasValue ? builder.PushClip(ChartMarkSurface.BarPath(stackBounds.Value,
            Math.Min(policy.CornerRadius, Math.Min(stackBounds.Value.Width, stackBounds.Value.Height) / 2), horizontal, direction)) : null;
        if (stackBounds.HasValue) radius = 0;
        var pattern = policy.Pattern;
        var corners = ChartMarkSurface.BarCorners(horizontal, direction, bothEnds: range);
        var outline = ChartMarkSurface.BarPath(bounds, radius, horizontal, direction, bothEnds: range);
        if (style.Kind == ChartBarStyle.SegmentedCapsule) {
            builder.Rect(bounds, ChartColorMath.WithOpacity(color, style.BodyOpacity), radius: radius, role: role, corners: corners,
                paint: VisualChartPaint.Fill(sourcePaint.WithOpacity(ChartColorMath.WithOpacity(color, style.BodyOpacity), style.BodyOpacity)));
            DrawPattern(builder, outline, pattern, ChartColorMath.WithOpacity(color, style.BodyOpacity), policy.Backdrop, role + "-pattern");
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
                            VisualChartPaint.BarGradient(color, resolvedRole, true, ChartVisualPrimitives.BarFillOpacity, sourcePaint)),
                        new VisualGradientStop(1, ChartColorMath.WithOpacity(ChartMarkSurface.BarGradientBottom(color).WithAlpha(color.A), ChartVisualPrimitives.BarGradientBottomOpacity * ChartVisualPrimitives.BarFillOpacity),
                            VisualChartPaint.BarGradient(color, resolvedRole, false, ChartVisualPrimitives.BarGradientBottomOpacity * ChartVisualPrimitives.BarFillOpacity, sourcePaint)) },
                    radius: radius, role: role, corners: corners);
                if (ChartMarkSurface.HasBarHighlight(bounds.Width, bounds.Height)) {
                    var inset = ChartVisualPrimitives.BarHighlightInset;
                    builder.Line(bounds.Left + inset, bounds.Top + inset, bounds.Right - inset, bounds.Top + inset,
                        ChartColorMath.WithOpacity(ChartColor.White.WithAlpha(color.A), ChartVisualPrimitives.BarHighlightOpacity), ChartVisualPrimitives.BarHighlightStrokeWidth, role: role + "-highlight",
                        paint: VisualChartPaint.Stroke(SvgPaint.Literal(ChartColorMath.WithOpacity(ChartColor.White.WithAlpha(color.A), ChartVisualPrimitives.BarHighlightOpacity))));
                }
            } else builder.Rect(bounds, color, radius: radius, role: role, paint: VisualChartPaint.Fill(sourcePaint), corners: corners);
            DrawPattern(builder, outline, pattern, color, policy.Backdrop, role + "-pattern");
        }
    }

}
