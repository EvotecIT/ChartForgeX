namespace ChartForgeX.Topology;

internal static partial class TopologyRenderPrimitives {
    public static string EdgeDash(TopologyEdgeLineStyle lineStyle) {
        return lineStyle switch {
            TopologyEdgeLineStyle.Solid => "none",
            TopologyEdgeLineStyle.Dashed => "8 5",
            TopologyEdgeLineStyle.Dotted => "2 5",
            _ => "8 5"
        };
    }

    /// <summary>
    /// Returns the line style a legend item for an edge kind is drawn with. A style set on the item is used as is.
    /// Otherwise the sample follows the edges of that kind: dashed or dotted when every one of them is drawn that way,
    /// and solid in every other case, including when the chart has no such edge or the edges differ (for example by
    /// status).
    /// </summary>
    public static TopologyEdgeLineStyle LegendLineStyle(TopologyChart chart, TopologyLegendItem item) {
        if (item.LineStyle != TopologyEdgeLineStyle.Auto) return item.LineStyle;
        TopologyEdgeLineStyle? shared = null;
        foreach (var edge in chart.Edges) {
            if (item.EdgeKind.HasValue && edge.Kind != item.EdgeKind.Value) continue;
            // A custom dash pattern wins over the line style when the edge is drawn, so it does here too.
            var style = edge.DashPattern.Count > 0 ? TopologyEdgeLineStyle.Dashed
                : edge.LineStyle != TopologyEdgeLineStyle.Auto ? edge.LineStyle
                : EffectiveEdgeDash(edge) == "none" ? TopologyEdgeLineStyle.Solid : TopologyEdgeLineStyle.Dashed;
            if (shared.HasValue && shared.Value != style) return TopologyEdgeLineStyle.Solid;
            shared = style;
        }

        return shared ?? TopologyEdgeLineStyle.Solid;
    }

    public static (bool Dashed, double Dash, double Gap) EdgePngDash(TopologyEdgeLineStyle lineStyle) {
        return lineStyle switch {
            TopologyEdgeLineStyle.Solid => (false, 0, 0),
            TopologyEdgeLineStyle.Dashed => (true, 8, 5),
            TopologyEdgeLineStyle.Dotted => (true, 2, 5),
            _ => (true, 8, 5)
        };
    }
}
