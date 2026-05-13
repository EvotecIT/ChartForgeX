namespace ChartForgeX.Topology;

internal static partial class TopologyRenderPrimitives {
    public static TopologyNodeSurfaceStyle EffectiveNodeSurfaceStyle(TopologyRenderOptions options) {
        if (options.NodeSurfaceStyle != TopologyNodeSurfaceStyle.Auto) return options.NodeSurfaceStyle;
        return IsMonitoringDashboardStyle(options) ? TopologyNodeSurfaceStyle.Tinted : TopologyNodeSurfaceStyle.Card;
    }

    public static bool UseNodeAccentBand(TopologyNodeDisplayMode displayMode, TopologyRenderOptions options) {
        if (displayMode is TopologyNodeDisplayMode.Dot or TopologyNodeDisplayMode.Icon or TopologyNodeDisplayMode.Tile) return false;
        return EffectiveNodeSurfaceStyle(options) == TopologyNodeSurfaceStyle.AccentBand;
    }

    public static string NodeFill(TopologyNode node, TopologyTheme theme, string accent, TopologyRenderOptions options) {
        if (!string.IsNullOrWhiteSpace(node.BackgroundColor)) return node.BackgroundColor!.Trim();
        return EffectiveNodeSurfaceStyle(options) == TopologyNodeSurfaceStyle.Card
            ? theme.Card
            : StatusFill(accent, theme.Background, IsMonitoringDashboardStyle(options) ? 0.065 : 0.10);
    }
}
