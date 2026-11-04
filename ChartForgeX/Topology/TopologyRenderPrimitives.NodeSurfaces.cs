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

    /// <summary>The authored node background, or the theme card; tinted surfaces are resolved from parsed colours by the scene compiler.</summary>
    public static string NodeFill(TopologyNode node, TopologyPaint theme) =>
        string.IsNullOrWhiteSpace(node.BackgroundColor) ? theme.Card : node.BackgroundColor!.Trim();
}
