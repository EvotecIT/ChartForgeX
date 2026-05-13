namespace ChartForgeX.Topology;

internal static partial class TopologyRenderPrimitives {
    public static string EdgeDash(TopologyEdgeLineStyle lineStyle) {
        return lineStyle switch {
            TopologyEdgeLineStyle.Dashed => "8 5",
            TopologyEdgeLineStyle.Dotted => "2 5",
            _ => "none"
        };
    }

    public static (bool Dashed, double Dash, double Gap) EdgePngDash(TopologyEdgeLineStyle lineStyle) {
        return lineStyle switch {
            TopologyEdgeLineStyle.Dashed => (true, 8, 5),
            TopologyEdgeLineStyle.Dotted => (true, 2, 5),
            _ => (false, 0, 0)
        };
    }
}
