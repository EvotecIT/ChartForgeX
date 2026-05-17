namespace ChartForgeX.Topology;

internal static partial class TopologyRenderPrimitives {
    public const double GroupStatusDotOuterRadius = 7.3;
    public const double GroupStatusDotInnerRadius = 5.3;
    public const double GroupStatusDotReserve = 38.0;

    public static bool ShouldDrawGroupStatusDot(TopologyGroup group, TopologyRenderOptions options) =>
        options.IncludeGroupStatusDots && IsMonitoringDashboardStyle(options) && group.Status != TopologyHealthStatus.Unknown;

    public static double GroupStatusDotReserveWidth(TopologyGroup group, TopologyRenderOptions options) =>
        ShouldDrawGroupStatusDot(group, options) ? GroupStatusDotReserve : 0.0;
}
