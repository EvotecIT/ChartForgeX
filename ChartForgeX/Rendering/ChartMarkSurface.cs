using ChartForgeX.Primitives;
using ChartForgeX.Core;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static class ChartMarkSurface {
    /// <summary>Design rule 4: a bar or column fills at most this share of its category band.</summary>
    public const double BandFill = 0.62;

    /// <summary>Design rule 4: the surface-coloured gap between stacked segments and touching marks.</summary>
    public const double SurfaceGap = 2;

    /// <summary>Design rule 5: the surface-coloured ring around a marker without an authored outline.</summary>
    public const double MarkerRing = 2;

    /// <summary>Design rule 4: a stacked column or bar is at most this thick unless a maximum width is authored.</summary>
    public const double StackedMaximumThickness = 24;

    /// <summary>The automatic maximum bar thickness: the theme's, capped for stacks so segments read as one column.</summary>
    public static double MaximumThickness(double themeMaximum, bool stacked) => stacked ? System.Math.Min(themeMaximum, StackedMaximumThickness) : themeMaximum;

    /// <summary>Design rule 4: the default radius of a bar's data end.</summary>
    public const double DataEndRadius = 4;

    /// <summary>
    /// Design rule 4: the outline of a bar, column or segment. Only the data end is rounded; the baseline end stays square
    /// so marks sit on their axis. <paramref name="direction"/> is positive toward the top (vertical) or right (horizontal);
    /// zero, or <paramref name="bothEnds"/> for interval marks whose two ends are both data, rounds both ends.
    /// </summary>
    public static ChartPath BarPath(ChartRect bounds, double radius, bool horizontal, double direction, bool bothEnds = false) {
        var corners = BarCorners(horizontal, direction, bothEnds);
        radius = System.Math.Min(radius, System.Math.Min(bounds.Width, bounds.Height) / 2);
        return ChartPathBuilder.RoundedRectangle(bounds, (corners & VisualCorners.TopLeft) != 0 ? radius : 0, (corners & VisualCorners.TopRight) != 0 ? radius : 0,
            (corners & VisualCorners.BottomRight) != 0 ? radius : 0, (corners & VisualCorners.BottomLeft) != 0 ? radius : 0);
    }

    /// <summary>The rounded corners of a bar: its data end only (or both ends of an interval or zero-length mark).</summary>
    public static VisualCorners BarCorners(bool horizontal, double direction, bool bothEnds = false) {
        if (bothEnds || direction == 0) return VisualCorners.All;
        if (horizontal) return direction > 0 ? VisualCorners.Right : VisualCorners.Left;
        return direction > 0 ? VisualCorners.Top : VisualCorners.Bottom;
    }

    /// <summary>
    /// Design rule 4: separates a stacked segment from its neighbours by <see cref="SurfaceGap"/>. Each side that meets
    /// another segment (not the stack's outer envelope) gives up half the gap, so adjacent segments read as distinct marks.
    /// </summary>
    public static ChartRect StackSegmentBounds(ChartRect bounds, ChartRect envelope, bool horizontal) {
        const double half = SurfaceGap / 2, tolerance = .001;
        if (horizontal) {
            var left = bounds.Left > envelope.Left + tolerance ? bounds.Left + half : bounds.Left;
            var right = bounds.Right < envelope.Right - tolerance ? bounds.Right - half : bounds.Right;
            return right - left < .5 ? bounds : new ChartRect(left, bounds.Top, right - left, bounds.Height);
        }
        var top = bounds.Top > envelope.Top + tolerance ? bounds.Top + half : bounds.Top;
        var bottom = bounds.Bottom < envelope.Bottom - tolerance ? bounds.Bottom - half : bounds.Bottom;
        return bottom - top < .5 ? bounds : new ChartRect(bounds.Left, top, bounds.Width, bottom - top);
    }

    public const double FunnelMiddleOpacity = 0.92;
    public const double FunnelBottomOpacity = 0.88;
    public static ChartPoint SliceGradientStart => new(0, 0);

    public static ChartPoint SliceGradientEnd => new(1, 1);

    public static bool HasBarHighlight(double width, double height) =>
        width > ChartVisualPrimitives.BarHighlightInset * 2 + 3 && height > ChartVisualPrimitives.BarHighlightInset * 2 + 1;

    public static double HatchOpacity(ChartFillPattern pattern) => pattern == ChartFillPattern.Crosshatch ? 0.20 : 0.28;

    public static double BulletRangeOpacity(int index) => System.Math.Max(0.10, 0.30 - index * 0.055);

    public static double WaterfallBarWidth(double slot) => System.Math.Max(12, System.Math.Min(58, slot * 0.58));

    public static bool SliceHasPointColor(ChartSeries series, int pointIndex) =>
        pointIndex < series.PointColors.Count && series.PointColors[pointIndex].HasValue;

    public static ChartColor BarGradientTop(ChartColor color) => BarGradientTopBlend(color).Color;

    public static ChartColor BarGradientBottom(ChartColor color) => BarGradientBottomBlend(color).Color;

    /// <summary>Returns the top stop of a bar gradient: white blended towards the bar colour (white stays literal).</summary>
    public static ChartColorBlend BarGradientTopBlend(ChartColor color) => new(ChartColor.White, null, color, SvgColorRole.Series, ChartVisualPrimitives.BarGradientTopBlend);

    /// <summary>Returns the bottom stop of a bar gradient: black blended towards the bar colour (black stays literal).</summary>
    public static ChartColorBlend BarGradientBottomBlend(ChartColor color) => new(ChartColor.Black, null, color, SvgColorRole.Series, ChartVisualPrimitives.BarGradientBottomBlend);

    public static ChartColor TimelineItemGradientTop(ChartColor color) => GradientTop(color, ChartVisualPrimitives.TimelineItemGradientTopBlend);

    public static ChartColor TimelineItemGradientBottom(ChartColor color) => GradientBottom(color, ChartVisualPrimitives.TimelineItemGradientBottomBlend);

    public static ChartColor GanttTaskGradientTop(ChartColor color) => GradientTop(color, ChartVisualPrimitives.GanttTaskGradientTopBlend);

    public static ChartColor GanttTaskGradientBottom(ChartColor color) => GradientBottom(color, ChartVisualPrimitives.GanttTaskGradientBottomBlend);

    public static ChartColor FunnelSegmentGradientTop(ChartColor color) => GradientTop(color, ChartVisualPrimitives.FunnelSegmentGradientTopBlend);

    public static ChartColor FunnelSegmentGradientBottom(ChartColor color) => GradientBottom(color, ChartVisualPrimitives.FunnelSegmentGradientBottomBlend);

    public static ChartColor SankeyNodeGradientTop(ChartColor color) => GradientTop(color, ChartVisualPrimitives.SankeyNodeGradientTopBlend);

    public static ChartColor SankeyNodeGradientBottom(ChartColor color) => GradientBottom(color, ChartVisualPrimitives.SankeyNodeGradientBottomBlend);

    public static ChartColor TreeNodeGradientTop(ChartColor color) => GradientTop(color, ChartVisualPrimitives.TreeNodeGradientTopBlend);

    public static ChartColor TreeNodeGradientBottom(ChartColor color) => GradientBottom(color, ChartVisualPrimitives.TreeNodeGradientBottomBlend);

    public static ChartColor TreemapTileGradientTop(ChartColor color) => GradientTop(color, ChartVisualPrimitives.TreemapTileGradientTopBlend);

    public static ChartColor TreemapTileGradientBottom(ChartColor color) => GradientBottom(color, ChartVisualPrimitives.TreemapTileGradientBottomBlend);

    public static ChartColor GradientTop(ChartColor color, double amount) => ChartColorMath.Blend(ChartColor.White, color, amount);

    public static ChartColor GradientBottom(ChartColor color, double amount) => ChartColorMath.Blend(ChartColor.Black, color, amount);
}
