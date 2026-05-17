using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyRenderPrimitives {
    public const double NodeStatusBadgeOffset = 11.0;
    public const double NodeStatusBadgeOuterRadius = 9.0;
    public const double NodeStatusBadgeInnerRadius = 7.0;
    public const double NodeStatusBadgeGlyphFontSize = 8.5;
    public const double NodeStatusBadgeGlyphYOffset = 3.0;
    public const double NodeStatusBadgeCheckStrokeWidth = 1.8;

    public static double NodeStatusBadgeCenterX(TopologyNode node) =>
        node.X + node.Width - NodeStatusBadgeOffset;

    public static double NodeStatusBadgeCenterY(TopologyNode node) =>
        node.Y + NodeStatusBadgeOffset;

    public static bool ShouldDrawNodeStatusBadgeCheck(TopologyNode node, TopologyRenderOptions options) =>
        IsMonitoringDashboardStyle(options) && node.Status == TopologyHealthStatus.Healthy;

    public static ChartPoint[] NodeStatusBadgeCheckPoints(double centerX, double centerY) => new[] {
        new ChartPoint(centerX - 3.8, centerY),
        new ChartPoint(centerX - 1.0, centerY + 3.0),
        new ChartPoint(centerX + 4.4, centerY - 3.6)
    };
}
