using System;

namespace ChartForgeX.Topology;

internal static partial class TopologyRenderPrimitives {
    public const double EdgeLabelClearanceHorizontalInset = 3.0;
    public const double EdgeLabelClearanceTopInset = 8.0;
    public const double EdgeLabelClearanceWidthInset = 6.0;
    public const double EdgeLabelClearanceHeightInset = 16.0;
    public const double EdgeLabelClearanceMinHeight = 18.0;
    public const double EdgeLabelClearanceRadius = 5.0;
    public const double EdgeLabelClearanceBackgroundOpacity = 0.66;
    public const double EdgeLabelClearanceGroupOpacity = 0.88;

    public static bool ShouldDrawEdgeLabelClearance(TopologyEdgeLabelLayout layout, TopologyRenderOptions options) {
        if (!IsMonitoringDashboardStyle(options) || options.IncludeEdgeLabelBackplates) return false;
        var lineCount = 0;
        if (!string.IsNullOrWhiteSpace(layout.Label)) lineCount++;
        if (!string.IsNullOrWhiteSpace(layout.SecondaryLabel)) lineCount++;
        if (!string.IsNullOrWhiteSpace(layout.TertiaryLabel)) lineCount++;
        return lineCount > 0;
    }

    public static TopologyGroup? EdgeLabelClearanceGroup(TopologyChart chart, TopologyEdgeLabelLayout layout) {
        for (var i = chart.Groups.Count - 1; i >= 0; i--) {
            var group = chart.Groups[i];
            if (layout.CenterX >= group.X && layout.CenterX <= group.X + group.Width && layout.CenterY >= group.Y && layout.CenterY <= group.Y + group.Height) return group;
        }

        return null;
    }

    public static string EdgeLabelClearanceFill(TopologyGroup? group, TopologyTheme theme, TopologyRenderOptions options) {
        if (group == null || !options.IncludeGroups) return theme.Background;
        var accent = string.IsNullOrWhiteSpace(group.Color) ? theme.StatusColor(group.Status) : group.Color!.Trim();
        return GroupFill(accent, theme, options);
    }

    public static double EdgeLabelClearanceX(TopologyEdgeLabelLayout layout, double centerX) =>
        centerX - layout.Width / 2 + EdgeLabelClearanceHorizontalInset;

    public static double EdgeLabelClearanceY(TopologyEdgeLabelLayout layout, double centerY) =>
        centerY - layout.Height / 2 + EdgeLabelClearanceTopInset;

    public static double EdgeLabelClearanceWidth(TopologyEdgeLabelLayout layout) =>
        layout.Width - EdgeLabelClearanceWidthInset;

    public static double EdgeLabelClearanceHeight(TopologyEdgeLabelLayout layout) =>
        Math.Max(EdgeLabelClearanceMinHeight, layout.Height - EdgeLabelClearanceHeightInset);

    public static double EdgeLabelClearanceOpacity(TopologyGroup? group) =>
        group == null ? EdgeLabelClearanceBackgroundOpacity : EdgeLabelClearanceGroupOpacity;

    public static byte EdgeLabelClearanceAlpha(TopologyGroup? group) =>
        (byte)Math.Round(255 * EdgeLabelClearanceOpacity(group));
}
