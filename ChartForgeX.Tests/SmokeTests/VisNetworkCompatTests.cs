using System;
using System.Linq;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void VisNetworkCompatMapsOptionsToGraphScene() {
        var scene = SampleVisNetworkCompatGraph().ToGraphScene("vis-parity", "Vis parity");

        Assert(scene.Options.Layout.Mode == GraphLayoutMode.Hierarchical && scene.Options.Layout.Direction == GraphLayoutDirection.LeftToRight, "Vis-network compatibility should map hierarchical layout options into the reusable GraphScene layout contract.");
        Assert(scene.Options.Physics.Solver == GraphPhysicsSolver.HierarchicalRepulsion && scene.Options.HasFeature(GraphSceneFeatures.RuntimePhysics) && scene.Options.HasFeature(GraphSceneFeatures.Stabilization), "Vis-network hierarchical compatibility should request runtime-capable hierarchical physics when physics remains enabled.");
        Assert(scene.Options.HasFeature(GraphSceneFeatures.Manipulation) && scene.Options.Manipulation.CanAddNodes && scene.Options.Manipulation.CanEditEdges && scene.Options.Manipulation.CanPersistPositions, "Vis-network manipulation options should flow into the host-neutral manipulation contract.");
        Assert(scene.Clusters.Count == 2 && scene.Nodes[0].ClusterId == "identity" && scene.Nodes[0].GroupId == "identity", "Vis-network groups should become GraphScene groups and cluster membership.");
        Assert(scene.Nodes[0].Shape == GraphNodeShape.Star && scene.Nodes[0].Style.BackgroundColor == "#F97316" && scene.Nodes[1].Style.BorderColor == "#1D4ED8", "Vis-network node/group style defaults should map to GraphScene node styling.");
        Assert(scene.Nodes[0].Level == 0 && scene.Nodes[2].Shape == GraphNodeShape.Database, "Vis-network levels and shapes should map to GraphScene hierarchy and node marks.");
        Assert(scene.Edges[0].Shape == GraphEdgeShape.ContinuousCurve && scene.Edges[0].Directed && scene.Edges[0].SourceArrow && !scene.Edges[0].TargetArrow && scene.Edges[0].Style.Width == 3 && !scene.Edges[0].Style.Physics, "Vis-network edge arrows, smoothing, width, and physics flags should map to reusable edge styling.");
        Assert(scene.Edges[0].Metadata["vis.arrows.from"] == "true" && scene.Edges[0].Metadata["vis.smooth"] == "Continuous", "Vis-network edge-specific options should remain available as metadata for host inspectors.");

        var implicitGroup = VisNetworkGraph.Create();
        implicitGroup.AddNode("api", "API", node => node.Group = "implicit");
        var implicitScene = implicitGroup.ToGraphScene("implicit", "Implicit group");
        Assert(implicitScene.Clusters.Count == 1 && implicitScene.Nodes[0].ClusterId == "implicit", "Vis-network groups should create clusters even when callers do not configure explicit group defaults.");

        var plain = VisNetworkGraph.Create()
            .AddNode("partial", "Partial", node => node.X = 42)
            .AddNode("fixed", "Fixed", node => {
                node.X = 12;
                node.Y = 24;
                node.Fixed = true;
            })
            .AddEdge("", "partial", "fixed");
        var plainScene = plain.ToGraphScene("plain-vis", "Plain vis");
        Assert(plainScene.Edges[0].Id == "edge-0" && !plainScene.Edges[0].Directed && !plainScene.Edges[0].TargetArrow, "Vis-network edges without ids should get stable synthetic ids while plain edges remain undirected by default.");
        Assert(!plainScene.Nodes.Single(node => node.Id == "partial").HasExplicitPosition && plainScene.Nodes.Single(node => node.Id == "fixed").HasExplicitPosition && plainScene.Nodes.Single(node => node.Id == "fixed").Fixed, "Vis-network coordinates should only pin graph nodes when callers supply both x and y.");

        var noNavigation = VisNetworkGraph.Create();
        noNavigation.Options.Interaction.NavigationButtons = false;
        noNavigation.AddNode("a", "A").AddNode("b", "B").AddEdge("a-b", "a", "b");
        var noNavigationHtml = noNavigation.ToGraphScene("no-navigation", "No navigation").ToGraphExplorerHtmlFragment();
        Assert(!noNavigation.ToGraphScene("no-navigation-scene", "No navigation").Options.HasFeature(GraphSceneFeatures.Viewport) && !noNavigationHtml.Contains("data-cfx-graph-action=\"fit\"", StringComparison.Ordinal), "Vis-network navigationButtons=false should suppress graph viewport controls.");
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
        Assert(html.Contains("data-edge-source-arrow=\"true\"", StringComparison.Ordinal) && html.Contains("data-edge-target-arrow=\"false\"", StringComparison.Ordinal), "Graph explorer output should preserve source-side vis-network arrow direction.");
        Assert(html.Contains("data-edge-width=\"3\"", StringComparison.Ordinal) && html.Contains("data-edge-color=\"#DC2626\"", StringComparison.Ordinal) && html.Contains("data-edge-label-color=\"#7F1D1D\"", StringComparison.Ordinal) && html.Contains("data-edge-physics=\"false\"", StringComparison.Ordinal), "Graph explorer output should expose vis-network edge width, color, label color, and physics flags.");
        Assert(html.Contains("style=\"stroke:#DC2626;stroke-width:3\"", StringComparison.Ordinal), "Graph explorer SVG should visibly render vis-network-style edge color and width.");
        Assert(html.Contains("style=\"fill:#DC2626;stroke:#DC2626\"", StringComparison.Ordinal), "Graph explorer SVG should render arrow markers with the matching vis-network edge color.");
        Assert(html.Contains("style=\"fill:#7F1D1D\">issues</text>", StringComparison.Ordinal), "Graph explorer SVG should visibly render vis-network-style edge label colors.");
        Assert(html.Contains("data-edge-hidden=\"true\"", StringComparison.Ordinal) && html.Contains("cfx-graph-edge cfx-graph-hidden", StringComparison.Ordinal) && !html.Contains(">queries</text>", StringComparison.Ordinal), "Graph explorer SVG and Canvas state should suppress hidden vis-network-style edges.");
        Assert(html.Contains("physics: attr(edge, 'data-edge-physics') !== 'false'", StringComparison.Ordinal) && html.Contains("state.edges.filter(edge => edge.physics !== false)", StringComparison.Ordinal), "Graph explorer runtime physics should exclude edges that opt out of physics.");
        Assert(html.Contains("style: { backgroundColor: attr(node.el, 'data-node-background-color')", StringComparison.Ordinal) && html.Contains("context.fillStyle = node.backgroundColor || '#2563eb'", StringComparison.Ordinal), "Graph explorer Canvas and PNG paths should consume serialized node styles.");
        Assert(html.Contains("drawNodeShapeMark(context, node)", StringComparison.Ordinal) && html.Contains("node.shape === 'database'", StringComparison.Ordinal), "Graph explorer Canvas and PNG paths should render rich vis-network node shapes instead of falling back to circles.");
        Assert(html.Contains("data-edge-shape=\"continuous\"", StringComparison.Ordinal) && html.Contains("edge.shape === 'line' && Math.abs(edge.curvature) < 0.001", StringComparison.Ordinal), "Graph explorer runtime paths should keep vis-network smoothed edges curved after Canvas redraws, physics, or dragging.");
        Assert(html.Contains("data-node-shape=\"database\"", StringComparison.Ordinal) && html.Contains(" Z M ", StringComparison.Ordinal), "Graph explorer SVG should render database nodes with cylinder-like geometry instead of a plain ellipse.");
        Assert(HtmlGraphExplorerRenderer.BuildFragmentStyle().Contains("@media (max-width:520px){.cfx-graph-overview{display:none!important}}", StringComparison.Ordinal), "Graph explorer responsive CSS should hide the overview on narrow embeds so hierarchy examples remain inspectable.");

        var rootX = GetAttribute(html, "data-node-id=\"identity\"", "data-node-x");
        var apiX = GetAttribute(html, "data-node-id=\"api\"", "data-node-x");
        var dbX = GetAttribute(html, "data-node-id=\"db\"", "data-node-x");
        Assert(rootX < apiX && apiX < dbX, "Left-to-right hierarchical layout should place increasing levels from left to right.");

        var cyclic = GraphScene.Create("cycle", "Cycle")
            .AddNode("s", "Source", node => node.Level = 0)
            .AddNode("a", "A")
            .AddNode("b", "B")
            .AddEdge("s-a", "s", "a")
            .AddEdge("a-b", "a", "b")
            .AddEdge("b-a", "b", "a");
        cyclic.Options.Layout.Mode = GraphLayoutMode.Hierarchical;
        var cyclicHtml = cyclic.ToGraphExplorerHtmlFragment();
        Assert(cyclicHtml.Contains("data-node-id=\"b\"", StringComparison.Ordinal), "Hierarchical layout inference should terminate and render cyclic graphs.");

        var separated = GraphScene.Create("hier-components", "Hierarchical components")
            .AddNode("a-root", "A root", node => node.Level = 0)
            .AddNode("a-child", "A child", node => node.Level = 1)
            .AddNode("b-root", "B root", node => node.Level = 0)
            .AddNode("b-child", "B child", node => node.Level = 1)
            .AddEdge("a-link", "a-root", "a-child")
            .AddEdge("b-link", "b-root", "b-child");
        separated.Options.Layout.Mode = GraphLayoutMode.Hierarchical;
        separated.Options.Layout.Direction = GraphLayoutDirection.LeftToRight;
        separated.Options.Layout.NodeSpacing = 30;
        separated.Options.Layout.ComponentSpacing = 260;
        var separatedHtml = separated.ToGraphExplorerHtmlFragment();
        var aRootY = GetAttribute(separatedHtml, "data-node-id=\"a-root\"", "data-node-y");
        var bRootY = GetAttribute(separatedHtml, "data-node-id=\"b-root\"", "data-node-y");
        Assert(Math.Abs(aRootY - bRootY) > 100, "Hierarchical layout should use ComponentSpacing to separate disconnected components.");
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
            edge.ArrowsTo = false;
            edge.Smooth = VisNetworkSmoothType.Continuous;
            edge.Style.Color = "#DC2626";
            edge.Style.LabelColor = "#7F1D1D";
            edge.Style.Width = 3;
            edge.Style.Physics = false;
        });
        graph.AddEdge("api-db", "api", "db", "queries", edge => {
            edge.Kind = "dataflow";
            edge.Dashes = true;
            edge.Style.Hidden = true;
        });
        return graph;
    }
}
