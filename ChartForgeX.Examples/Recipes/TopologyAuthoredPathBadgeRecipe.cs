using ChartForgeX.Primitives;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;

/// <summary>A normal workflow handoff with an authored route and editable detail surfaces.</summary>
public static class TopologyAuthoredPathBadgeRecipe {
    /// <summary>Builds source-independent topology using the same geometry and detail owners as imported diagrams.</summary>
    public static TopologyChart Create() {
        var chart = TopologyChart.Create().WithId("authored-handoff").WithTitle("Queue handoff").WithViewport(800, 440, 28);
        chart.AddNode("gateway", "Gateway", 64, 100, width: 220, height: 180);
        chart.AddNode("worker", "Worker", 510, 100, width: 220, height: 180);
        chart.Nodes[0].FitLabelToContent = true; chart.Nodes[1].FitLabelToContent = true;
        chart.Nodes[0].Details.Add(new TopologyNodeDetail { Text = "Queued", TextStyle = new TextStyleOverride { FontSize = 14 },
            BadgeStyle = new TopologyDetailBadgeStyle { Fill = ChartColor.FromHex("#E3F2FD"), Stroke = ChartColor.FromHex("#315A87") } });
        chart.Nodes[1].Details.Add(new TopologyNodeDetail { Text = "Ready", TextStyle = new TextStyleOverride { FontSize = 14 },
            BadgeStyle = new TopologyDetailBadgeStyle { Fill = ChartColor.FromHex("#E8F5E9"), Stroke = ChartColor.FromHex("#376947") } });
        chart.AddEdge("dispatch", "gateway", "worker", "Dispatch", direction: VisualLinkDirection.Forward);
        chart.Edges[0].PathGeometry = new TopologyPathGeometry("M 284 190 C 350 42 444 42 510 190");
        chart.Edges[0].Color = "#315A87"; chart.Edges[0].StrokeWidth = 2;
        chart.Edges[0].LabelColor = "#F3FAFF"; chart.Edges[0].LabelBackgroundColor = "#193857";
        chart.Edges[0].LabelPosition = new TopologyLabelPosition(new ChartPoint(397, 70), TopologyLabelVerticalAnchor.TextBaseline);
        chart.Edges[0].TargetMarkerStyle = new TopologyMarkerStyle { Scale = 1.5, Offset = 1.2, StrokeWidth = 0 };
        chart.Edges[0].LabelAnchorX = 397; chart.Edges[0].LabelAnchorY = 70; chart.Edges[0].HasLabelAnchorOverride = true;
        return chart;
    }
    /// <summary>Returns the portable workflow capture for editing and restore in another process.</summary>
    public static string Save() => Create().ToVisualArtifact().ToInterchangeJson();
}
