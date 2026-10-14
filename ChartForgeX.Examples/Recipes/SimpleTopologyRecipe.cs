/// <summary>Creates a small service request route with two nodes and a directed edge.</summary>
public static class SimpleTopologyRecipe {
    /// <summary>Creates the bounded native topology model.</summary>
    /// <param name="variant">Use wide or feasibility for the simple topology.</param>
    /// <param name="mode">The rendering mode; diagram colors are resolved when the model is prepared.</param>
    public static TopologyChart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "feasibility"))
            throw new ArgumentOutOfRangeException(nameof(variant), variant, "Use wide or feasibility.");

        var chart = TopologyChart.Create();
        chart.Id = "service-request";
        chart.Viewport = new TopologyViewport { Width = 640, Height = 400 };
        chart.Nodes.Add(new TopologyNode {
            Id = "source", Label = "Source\nservice", X = 24, Y = 70, Width = 140, Height = 64,
            Status = TopologyHealthStatus.Healthy
        });
        chart.Nodes.Add(new TopologyNode {
            Id = "target", Label = "Target", X = 300, Y = 70, Width = 140, Height = 64
        });
        chart.Edges.Add(new TopologyEdge {
            Id = "route", SourceNodeId = "source", TargetNodeId = "target", Direction = VisualLinkDirection.Forward,
            Routing = TopologyEdgeRouting.Straight, LineStyle = TopologyEdgeLineStyle.Solid, Label = "Request"
        });
        return chart;
    }
}
