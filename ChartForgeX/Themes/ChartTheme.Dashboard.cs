using ChartForgeX.Primitives;

namespace ChartForgeX.Themes;

/// <inheritdoc />
public sealed partial class ChartTheme {
    /// <summary>
    /// Creates a bright operational dashboard theme for compact KPI cards, progress rows, and utilization heatmaps.
    /// </summary>
    /// <returns>A bright dashboard chart theme.</returns>
    public static ChartTheme DashboardLight() => Minimal()
        .WithSurfaceColors(ChartColor.FromHex("#F2F5EC"), ChartColor.FromHex("#FFFFFF"), ChartColor.FromHex("#F6F8FB"), ChartColor.FromHex("#DDE3D1"), ChartColor.FromHex("#DDE3D1"))
        .WithTextColors(ChartColor.FromHex("#1D2430"), ChartColor.FromHex("#8B93A3"))
        .WithGuideColors(ChartColor.FromHex("#DDE3D1"), ChartColor.FromHex("#BFC8B2"))
        .WithSemanticColors(ChartColor.FromHex("#27C26A"), ChartColor.FromHex("#F6C445"), ChartColor.FromHex("#FF3B4F"))
        .WithPalette("#DDFB20", "#27C26A", "#356AF4", "#FF3B4F", "#667085")
        .WithTypography(26, 14, 12, 16, 18, 18)
        .WithCornerRadius(24, 14)
        .WithShadowOpacity(0.05);

    /// <summary>
    /// Creates a clean SaaS dashboard theme for recurring-revenue trends, targets, and driver cards.
    /// </summary>
    /// <returns>A SaaS dashboard chart theme.</returns>
    public static ChartTheme SaasDashboardLight() => Minimal()
        .WithSurfaceColors(ChartColor.FromHex("#F3F6FA"), ChartColor.FromHex("#FCFDFE"), ChartColor.FromHex("#F6F8FC"), ChartColor.FromRgba(199, 210, 226, 46), ChartColor.FromRgba(210, 218, 232, 52))
        .WithTextColors(ChartColor.FromHex("#1D2430"), ChartColor.FromHex("#8B93A3"))
        .WithGuideColors(ChartColor.FromRgba(214, 222, 234, 142), ChartColor.FromHex("#A6B0C0"))
        .WithSemanticColors(ChartColor.FromHex("#27C26A"), ChartColor.FromHex("#F6C445"), ChartColor.FromHex("#FF3B4F"))
        .WithPalette("#356AF4", "#667085", "#27C26A", "#FF3B4F")
        .WithTypography(26, 14, 12, 16, 19, 18)
        .WithCornerRadius(24, 12)
        .WithMarkerRadius(4.2)
        .WithShadowOpacity(0.09);

    /// <summary>
    /// Creates a soft restaurant operations dashboard theme for earnings, order, customer, and occupancy panels.
    /// </summary>
    /// <returns>A restaurant operations dashboard chart theme.</returns>
    public static ChartTheme RestaurantDashboardLight() => Minimal()
        .WithSurfaceColors(ChartColor.FromHex("#EFEFEF"), ChartColor.FromHex("#FBFBFA"), ChartColor.FromHex("#F5F5F3"), ChartColor.FromRgba(207, 207, 202, 70), ChartColor.FromRgba(219, 219, 214, 92))
        .WithTextColors(ChartColor.FromHex("#161616"), ChartColor.FromHex("#777776"))
        .WithGuideColors(ChartColor.FromRgba(222, 222, 218, 138), ChartColor.FromHex("#A4A29E"))
        .WithSemanticColors(ChartColor.FromHex("#2CA36B"), ChartColor.FromHex("#D0A400"), ChartColor.FromHex("#E66A1F"))
        .WithPalette("#C46A23", "#EF7F22", "#FFB074", "#A38300", "#545454")
        .WithTypography(26, 14, 12, 16, 20, 18)
        .WithCornerRadius(24, 14)
        .WithMarkerRadius(4)
        .WithShadowOpacity(0.05);
}
