using System;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void VisNetworkCompatMapsOptionsToGraphScene() {
        var scene = SampleVisNetworkCompatGraph().ToGraphScene("vis-parity", "Vis parity");

        Assert(scene.Options.Layout.Mode == GraphLayoutMode.Hierarchical && scene.Options.Layout.Direction == GraphLayoutDirection.LeftToRight, "Vis-network compatibility should map hierarchical layout options into the reusable GraphScene layout contract.");
        Assert(scene.Options.Physics.Solver == GraphPhysicsSolver.HierarchicalRepulsion, "Vis-network hierarchical compatibility should request the hierarchical repulsion solver when physics remains enabled.");
        Assert(scene.Options.HasFeature(GraphSceneFeatures.Manipulation) && scene.Options.Manipulation.CanAddNodes && scene.Options.Manipulation.CanEditEdges && scene.Options.Manipulation.CanPersistPositions, "Vis-network manipulation options should flow into the host-neutral manipulation contract.");
        Assert(scene.Clusters.Count == 2 && scene.Nodes[0].ClusterId == "identity" && scene.Nodes[0].GroupId == "identity", "Vis-network groups should become GraphScene groups and cluster membership.");
        Assert(scene.Nodes[0].Shape == GraphNodeShape.Star && scene.Nodes[0].Style.BackgroundColor == "#F97316" && scene.Nodes[1].Style.BorderColor == "#1D4ED8", "Vis-network node/group style defaults should map to GraphScene node styling.");
        Assert(scene.Nodes[0].Level == 0 && scene.Nodes[2].Shape == GraphNodeShape.Database, "Vis-network levels and shapes should map to GraphScene hierarchy and node marks.");
        Assert(scene.Edges[0].Shape == GraphEdgeShape.ContinuousCurve && scene.Edges[0].Directed && scene.Edges[0].Style.Width == 3 && !scene.Edges[0].Style.Physics, "Vis-network edge arrows, smoothing, width, and physics flags should map to reusable edge styling.");
        Assert(scene.Edges[0].Metadata["vis.arrows.from"] == "true" && scene.Edges[0].Metadata["vis.smooth"] == "Continuous", "Vis-network edge-specific options should remain available as metadata for host inspectors.");
    }

    private static void VisNetworkCompatRendersHierarchicalStyledHtml() {
        var scene = SampleVisNetworkCompatGraph().ToGraphScene("vis-parity-html", "Vis parity HTML");
        var html = scene.ToGraphExplorerHtmlFragment(options => options.IdScope = "vis-parity-html");

        Assert(html.Contains("data-cfx-graph-layout=\"hierarchical\"", StringComparison.Ordinal) && html.Contains("data-cfx-graph-layout-direction=\"LeftToRight\"", StringComparison.Ordinal), "Graph explorer output should expose the hierarchical layout requested through the compatibility layer.");
        Assert(html.Contains("data-node-id=\"identity\" data-node-label=\"Identity\" data-node-kind=\"service\" data-node-group=\"identity\" data-node-cluster=\"identity\"", StringComparison.Ordinal), "Graph explorer output should preserve converted vis-network node grouping.");
        Assert(html.Contains("data-node-level=\"0\"", StringComparison.Ordinal) && html.Contains("data-node-shape=\"star\"", StringComparison.Ordinal) && html.Contains("data-node-shape=\"database\"", StringComparison.Ordinal), "Graph explorer output should expose hierarchy levels and richer node shapes.");
        Assert(html.Contains("style=\"fill:#F97316;stroke:#7C2D12;filter:drop-shadow(0 5px 10px rgba(15,23,42,.18))\"", StringComparison.Ordinal), "Graph explorer SVG should render vis-network-style node color and shadow hints.");
        Assert(html.Contains("class=\"cfx-graph-node-label-bg\"", StringComparison.Ordinal) && html.Contains("style=\"fill:#FFF7ED\"", StringComparison.Ordinal), "Graph explorer SVG should render label backgrounds for styled nodes.");
        Assert(html.Contains("<text y=\"26\" style=\"fill:#0F172A\">Identity</text>", StringComparison.Ordinal), "Graph explorer SVG should render styled node labels with well-formed text attributes.");
        Assert(!html.Contains("y=\"26 style=", StringComparison.Ordinal), "Graph explorer SVG should not corrupt text attributes when a node label color is set.");
        Assert(html.Contains("data-edge-width=\"3\"", StringComparison.Ordinal) && html.Contains("data-edge-color=\"#DC2626\"", StringComparison.Ordinal) && html.Contains("data-edge-physics=\"false\"", StringComparison.Ordinal), "Graph explorer output should expose vis-network edge width, color, and physics flags.");
        Assert(html.Contains("style=\"stroke:#DC2626;stroke-width:3\"", StringComparison.Ordinal), "Graph explorer SVG should visibly render vis-network-style edge color and width.");
        Assert(HtmlGraphExplorerRenderer.BuildFragmentStyle().Contains("@media (max-width:520px){.cfx-graph-overview{display:none!important}}", StringComparison.Ordinal), "Graph explorer responsive CSS should hide the overview on narrow embeds so hierarchy examples remain inspectable.");

        var rootX = GetAttribute(html, "data-node-id=\"identity\"", "data-node-x");
        var apiX = GetAttribute(html, "data-node-id=\"api\"", "data-node-x");
        var dbX = GetAttribute(html, "data-node-id=\"db\"", "data-node-x");
        Assert(rootX < apiX && apiX < dbX, "Left-to-right hierarchical layout should place increasing levels from left to right.");
    }

    private static VisNetworkGraph SampleVisNetworkCompatGraph() {
        var graph = VisNetworkGraph.Create();
        graph.Options.Layout.Hierarchical.Enabled = true;
        graph.Options.Layout.Hierarchical.Direction = GraphLayoutDirection.LeftToRight;
        graph.Options.Layout.Hierarchical.LevelSeparation = 150;
        graph.Options.Layout.Hierarchical.NodeSpacing = 82;
        graph.Options.Interaction.Hover = true;
        graph.Options.Manipulation.Enabled = true;
        graph.Options.Manipulation.AddNode = true;
        graph.Options.Manipulation.EditEdge = true;
        graph.Options.Manipulation.PersistPositions = true;
        graph.Groups["identity"] = new VisNetworkGroupOptions {
            Label = "Identity",
            Kind = "service",
            Shape = VisNetworkNodeShape.Diamond,
            Icon = "I"
        };
        graph.Groups["data"] = new VisNetworkGroupOptions {
            Label = "Data",
            Kind = "database",
            Shape = VisNetworkNodeShape.Database,
            Icon = "D"
        };
        graph.Groups["identity"].Style.BackgroundColor = "#DBEAFE";
        graph.Groups["identity"].Style.BorderColor = "#1D4ED8";
        graph.Groups["data"].Style.BackgroundColor = "#F5F3FF";
        graph.Groups["data"].Style.BorderColor = "#7C3AED";

        graph.AddNode("identity", "Identity", node => {
            node.Group = "identity";
            node.Level = 0;
            node.Shape = VisNetworkNodeShape.Star;
            node.Style.BackgroundColor = "#F97316";
            node.Style.BorderColor = "#7C2D12";
            node.Style.LabelColor = "#0F172A";
            node.Style.LabelBackgroundColor = "#FFF7ED";
            node.Style.Shadow = true;
        });
        graph.AddNode("api", "API", node => {
            node.Group = "identity";
            node.Level = 1;
        });
        graph.AddNode("db", "Database", node => {
            node.Group = "data";
            node.Level = 2;
        });
        graph.AddEdge("identity-api", "identity", "api", "issues", edge => {
            edge.ArrowsFrom = true;
            edge.Smooth = VisNetworkSmoothType.Continuous;
            edge.Style.Color = "#DC2626";
            edge.Style.Width = 3;
            edge.Style.Physics = false;
        });
        graph.AddEdge("api-db", "api", "db", "queries", edge => {
            edge.Kind = "dataflow";
            edge.Dashes = true;
        });
        return graph;
    }
}
