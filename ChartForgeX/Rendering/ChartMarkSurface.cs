using ChartForgeX.Primitives;
using ChartForgeX.Core;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static class ChartMarkSurface {
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
