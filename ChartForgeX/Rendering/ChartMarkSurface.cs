using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static class ChartMarkSurface {
    public static ChartColor BarGradientTop(ChartColor color) => GradientTop(color, ChartVisualPrimitives.BarGradientTopBlend);

    public static ChartColor BarGradientBottom(ChartColor color) => GradientBottom(color, ChartVisualPrimitives.BarGradientBottomBlend);

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
