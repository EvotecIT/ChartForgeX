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
    // Default relationship captions sit on a borderless chip just off the canvas tone. The
    // chip hugs the measured text; the larger layout box remains the placement clearance.
    public const double EdgeLabelQuietTint = 0.045;
    public const double EdgeLabelQuietPaddingX = 7.0;
    public const double EdgeLabelQuietPaddingY = 2.0;

    /// <summary>Monitoring captions keep their framed card; ordinary diagrams use a quiet chip.</summary>
    public static bool UsesQuietEdgeLabelSurface(TopologyRenderOptions options) => !IsMonitoringDashboardStyle(options);

    /// <summary>Primary caption weight shared by label measurement and painting.</summary>
    public static int EdgeLabelPrimaryWeight(TopologyRenderOptions options) => UsesQuietEdgeLabelSurface(options) ? 400 : 600;

    public static double EdgeLabelBackplateX(TopologyEdgeLabelLayout layout, double centerX) =>
        centerX - layout.Width / 2;

    public static double EdgeLabelBackplateY(TopologyEdgeLabelLayout layout, double centerY) =>
        centerY - layout.Height / 2;

    public static double EdgeLabelBackplateRadius(TopologyRenderOptions options) =>
        IsMonitoringDashboardStyle(options) ? EdgeLabelBackplateMonitoringRadius : EdgeLabelBackplateDefaultRadius;

    public static string EdgeLabelBackplateFill(TopologyPaint theme, TopologyRenderOptions options) =>
        IsMonitoringDashboardStyle(options) ? theme.Card : theme.Background;

    public static double EdgeLabelBackplateFillOpacity(TopologyRenderOptions options) =>
        IsMonitoringDashboardStyle(options) ? EdgeLabelBackplateMonitoringFillOpacity : EdgeLabelBackplateDefaultFillOpacity;

    public static double EdgeLabelBackplateStrokeOpacity(TopologyRenderOptions options) =>
        IsMonitoringDashboardStyle(options) ? EdgeLabelBackplateMonitoringStrokeOpacity : EdgeLabelBackplateDefaultStrokeOpacity;

    public static byte EdgeLabelBackplateStrokeAlpha(TopologyRenderOptions options) =>
        (byte)Math.Round(255 * EdgeLabelBackplateStrokeOpacity(options));
}
