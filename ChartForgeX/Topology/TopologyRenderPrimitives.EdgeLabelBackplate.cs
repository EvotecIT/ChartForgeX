using System;

namespace ChartForgeX.Topology;

internal static partial class TopologyRenderPrimitives {
    public const double EdgeLabelBackplateMonitoringRadius = 7.0;
    public const double EdgeLabelBackplateDefaultRadius = 9.0;
    public const double EdgeLabelBackplateMonitoringFillOpacity = 0.98;
    public const double EdgeLabelBackplateDefaultFillOpacity = 1.0;
    public const double EdgeLabelBackplateMonitoringStrokeOpacity = 0.72;
    public const double EdgeLabelBackplateDefaultStrokeOpacity = 1.0;
    public const double EdgeLabelBackplateStrokeWidth = 1.0;

    public static double EdgeLabelBackplateX(TopologyEdgeLabelLayout layout, double centerX) =>
        centerX - layout.Width / 2;

    public static double EdgeLabelBackplateY(TopologyEdgeLabelLayout layout, double centerY) =>
        centerY - layout.Height / 2;

    public static double EdgeLabelBackplateRadius(TopologyRenderOptions options) =>
        IsMonitoringDashboardStyle(options) ? EdgeLabelBackplateMonitoringRadius : EdgeLabelBackplateDefaultRadius;

    public static string EdgeLabelBackplateFill(TopologyTheme theme, TopologyRenderOptions options) =>
        IsMonitoringDashboardStyle(options) ? theme.Card : theme.Background;

    public static double EdgeLabelBackplateFillOpacity(TopologyRenderOptions options) =>
        IsMonitoringDashboardStyle(options) ? EdgeLabelBackplateMonitoringFillOpacity : EdgeLabelBackplateDefaultFillOpacity;

    public static double EdgeLabelBackplateStrokeOpacity(TopologyRenderOptions options) =>
        IsMonitoringDashboardStyle(options) ? EdgeLabelBackplateMonitoringStrokeOpacity : EdgeLabelBackplateDefaultStrokeOpacity;

    public static byte EdgeLabelBackplateStrokeAlpha(TopologyRenderOptions options) =>
        (byte)Math.Round(255 * EdgeLabelBackplateStrokeOpacity(options));
}
