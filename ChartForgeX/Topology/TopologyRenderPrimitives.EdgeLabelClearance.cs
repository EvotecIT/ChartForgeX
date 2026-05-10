namespace ChartForgeX.Topology;

internal static partial class TopologyRenderPrimitives {
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
}
