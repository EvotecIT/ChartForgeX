using System;
using System.Linq;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void VisNetworkCompatMapsOptionsToGraphScene() {
        var scene = SampleVisNetworkCompatGraph().ToGraphScene("vis-parity", "Vis parity");

        Assert(scene.Options.Layout.Mode == GraphLayoutMode.Hierarchical && scene.Options.Layout.Direction == GraphLayoutDirection.LeftToRight, "Vis-network compatibility should map hierarchical layout options into the reusable GraphScene layout contract.");
        Assert(scene.Options.Physics.Solver == GraphPhysicsSolver.StaticPrepared && !scene.Options.HasFeature(GraphSceneFeatures.RuntimePhysics) && !scene.Options.HasFeature(GraphSceneFeatures.Stabilization), "Vis-network hierarchical compatibility should keep prepared hierarchical layouts static by default so serialized levels and direction do not drift.");
        Assert(scene.Options.HasFeature(GraphSceneFeatures.Manipulation) && scene.Options.Manipulation.CanAddNodes && scene.Options.Manipulation.CanEditEdges && scene.Options.Manipulation.CanPersistPositions, "Vis-network manipulation options should flow into the host-neutral manipulation contract.");
        Assert(scene.Clusters.Count == 2 && scene.Nodes[0].ClusterId == "identity" && scene.Nodes[0].GroupId == "identity", "Vis-network groups should become GraphScene groups and cluster membership.");
        Assert(scene.Nodes[0].Shape == GraphNodeShape.Star && scene.Nodes[0].Style.BackgroundColor == "#F97316" && scene.Nodes[1].Style.BorderColor == "#1D4ED8", "Vis-network node/group style defaults should map to GraphScene node styling.");
        Assert(scene.Nodes[0].Level == 0 && scene.Nodes[2].Shape == GraphNodeShape.Database, "Vis-network levels and shapes should map to GraphScene hierarchy and node marks.");
        Assert(scene.Edges[0].Shape == GraphEdgeShape.ContinuousCurve && !scene.Edges[0].Directed && scene.Edges[0].SourceArrow && !scene.Edges[0].TargetArrow && scene.Edges[0].Style.Width == 3 && !scene.Edges[0].Style.Physics, "Vis-network source-only arrows, smoothing, width, and physics flags should map to reusable edge styling without implying target arrows.");
        Assert(scene.Edges[0].Metadata["vis.arrows.from"] == "true" && scene.Edges[0].Metadata["vis.smooth"] == "Continuous", "Vis-network edge-specific options should remain available as metadata for host inspectors.");

        var defaultManipulation = VisNetworkGraph.Create();
        defaultManipulation.Options.Manipulation.Enabled = true;
        defaultManipulation.AddNode("a", "A").AddNode("b", "B").AddEdge("a-b", "a", "b");
        var defaultManipulationScene = defaultManipulation.ToGraphScene("default-manipulation", "Default manipulation");
        Assert(defaultManipulationScene.Options.Manipulation.CanAddNodes && defaultManipulationScene.Options.Manipulation.CanDeleteNodes && defaultManipulationScene.Options.Manipulation.CanAddEdges && defaultManipulationScene.Options.Manipulation.CanEditEdges && defaultManipulationScene.Options.Manipulation.CanDeleteEdges && !defaultManipulationScene.Options.Manipulation.CanEditNodes, "Vis-network manipulation defaults should expose the standard add/edit/delete surface when manipulation is enabled without per-action overrides.");

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
        Assert(plainScene.Edges[0].Id == "edge-0" && !plainScene.Edges[0].Directed && !plainScene.Edges[0].TargetArrow && plainScene.Edges[0].LayoutDirected, "Vis-network edges without ids should get stable synthetic ids while plain edges remain visually undirected but usable for hierarchy inference.");
        Assert(!plainScene.Nodes.Single(node => node.Id == "partial").HasExplicitPosition && plainScene.Nodes.Single(node => node.Id == "fixed").HasExplicitPosition && plainScene.Nodes.Single(node => node.Id == "fixed").Fixed, "Vis-network coordinates should only pin graph nodes when callers supply both x and y.");

        var friendly = VisNetworkGraph.Create()
            .AddNode("app server", "App Server", node => node.Group = "core services")
            .AddNode("sql db", "SQL DB", node => node.Fixed = true)
            .AddEdge("app link", "app server", "sql db");
        var friendlyScene = friendly.ToGraphScene("friendly-vis", "Friendly vis");
        friendlyScene.Validate();
        Assert(friendlyScene.Nodes.Any(node => node.Id == "app-server" && node.Metadata["vis.id"] == "app server" && node.GroupId == "core-services"), "Vis-network compatibility should normalize friendly node and group ids while preserving original ids in metadata.");
        Assert(friendlyScene.Nodes.Single(node => node.Id == "sql-db").Fixed && !friendlyScene.Nodes.Single(node => node.Id == "sql-db").HasExplicitPosition, "Vis-network fixed nodes without coordinates should remain fixed after the prepared layout chooses an initial position.");
        Assert(friendlyScene.Edges.Single().Id == "app-link" && friendlyScene.Edges.Single().SourceNodeId == "app-server" && friendlyScene.Edges.Single().TargetNodeId == "sql-db", "Vis-network compatibility should normalize friendly edge ids and rewrite edge endpoints to normalized node ids.");

        var middleArrow = VisNetworkGraph.Create()
            .AddNode("a", "A")
            .AddNode("b", "B")
            .AddEdge("middle", "a", "b", configure: edge => edge.ArrowsMiddle = true);
        var middleArrowScene = middleArrow.ToGraphScene("middle-arrow", "Middle arrow");
        Assert(!middleArrowScene.Edges[0].Directed && !middleArrowScene.Edges[0].TargetArrow && middleArrowScene.Edges[0].Metadata["vis.arrows.middle"] == "true", "Vis-network middle-only arrows should remain metadata-only until the graph contract models middle markers explicitly.");

        var runtimeHierarchy = SampleVisNetworkCompatGraph();
        runtimeHierarchy.Options.Physics.Solver = GraphPhysicsSolver.HierarchicalRepulsion;
        var runtimeHierarchyScene = runtimeHierarchy.ToGraphScene("runtime-hierarchy", "Runtime hierarchy");
        Assert(runtimeHierarchyScene.Options.HasFeature(GraphSceneFeatures.RuntimePhysics) && runtimeHierarchyScene.Options.HasFeature(GraphSceneFeatures.Stabilization), "Vis-network compatibility should still enable runtime physics when callers choose a runtime-capable hierarchical solver.");

        var dashPattern = VisNetworkGraph.Create()
            .AddNode("a", "A")
            .AddNode("b", "B")
            .AddEdge("dash", "a", "b", configure: edge => edge.Style.DashPattern = "4 2");
        var dashPatternScene = dashPattern.ToGraphScene("dash-pattern", "Dash pattern");
        Assert(dashPatternScene.Edges[0].Dashed && dashPatternScene.Edges[0].Style.DashPattern == "4 2", "Vis-network compatibility should preserve explicit edge dash patterns even when Dashes is not separately enabled.");

        var idOnly = VisNetworkGraph.Create();
        idOnly.Nodes.Add(new VisNetworkNode { Id = "id-only" });
        var idOnlyScene = idOnly.ToGraphScene("id-only", "Id only");
        Assert(idOnlyScene.Nodes.Single().Label == "id-only", "Vis-network compatibility should fall back to the id for id-only node records.");
        idOnly.Nodes.Add(new VisNetworkNode { Id = "app server" });
        var friendlyIdOnlyScene = idOnly.ToGraphScene("friendly-id-only", "Friendly id only");
        Assert(friendlyIdOnlyScene.Nodes.Single(node => node.Id == "app-server").Label == "app server", "Vis-network compatibility should preserve original id text as the fallback label when graph ids are normalized.");

        var imageShapes = VisNetworkGraph.Create()
            .AddNode("rect", "Rectangle", node => {
                node.Shape = VisNetworkNodeShape.Image;
                node.Image = "data:image/svg+xml,%3Csvg viewBox='0 0 10 6'%3E%3Crect width='10' height='6' fill='%23f00'/%3E%3C/svg%3E";
            })
            .AddNode("circle", "Circle", node => {
                node.Shape = VisNetworkNodeShape.CircularImage;
                node.Image = "data:image/svg+xml,%3Csvg viewBox='0 0 10 10'%3E%3Ccircle cx='5' cy='5' r='5' fill='%2300f'/%3E%3C/svg%3E";
            });
        var imageShapeScene = imageShapes.ToGraphScene("image-shapes", "Image shapes");
        var imageShapeHtml = imageShapeScene.ToGraphExplorerHtmlFragment();
        Assert(imageShapeScene.Nodes.Single(node => node.Id == "rect").Shape == GraphNodeShape.RectangularImage && imageShapeScene.Nodes.Single(node => node.Id == "circle").Shape == GraphNodeShape.Image, "Vis-network compatibility should keep image and circularImage as distinct graph node shapes.");
        Assert(imageShapeHtml.Contains("data-node-shape=\"imageRect\"", StringComparison.Ordinal) && imageShapeHtml.Contains("data-node-shape=\"image\"", StringComparison.Ordinal) && imageShapeHtml.Contains("cfx-graph-node-image-rect", StringComparison.Ordinal) && imageShapeHtml.Contains("image:not(.cfx-graph-node-image-rect)", StringComparison.Ordinal), "Graph explorer output should serialize and render rectangular and circular vis image nodes distinctly.");

        var noNavigation = VisNetworkGraph.Create();
        noNavigation.Options.Interaction.NavigationButtons = false;
        noNavigation.AddNode("a", "A").AddNode("b", "B").AddEdge("a-b", "a", "b");
        var noNavigationScene = noNavigation.ToGraphScene("no-navigation-scene", "No navigation");
        var noNavigationHtml = noNavigationScene.ToGraphExplorerHtmlFragment();
        Assert(noNavigationScene.Options.HasFeature(GraphSceneFeatures.Viewport) && noNavigationScene.Metadata["vis.interaction.navigationButtons"] == "false" && !noNavigationHtml.Contains("data-cfx-graph-action=\"fit\"", StringComparison.Ordinal) && noNavigationHtml.Contains("hasFeature(root, 'Viewport')", StringComparison.Ordinal), "Vis-network navigationButtons=false should suppress toolbar buttons without disabling viewport pan and zoom behavior.");
    }

    private static void VisNetworkCompatRendersHierarchicalStyledHtml() {
        var scene = SampleVisNetworkCompatGraph().ToGraphScene("vis-parity-html", "Vis parity HTML");
        var html = scene.ToGraphExplorerHtmlFragment(options => options.IdScope = "vis-parity-html");

        Assert(html.Contains("data-cfx-graph-layout=\"hierarchical\"", StringComparison.Ordinal) && html.Contains("data-cfx-graph-layout-direction=\"LeftToRight\"", StringComparison.Ordinal), "Graph explorer output should expose the hierarchical layout requested through the compatibility layer.");
        Assert(html.Contains("data-node-id=\"identity\" data-node-label=\"Identity\" data-node-kind=\"service\" data-node-group=\"identity\" data-node-cluster=\"identity\"", StringComparison.Ordinal), "Graph explorer output should preserve converted vis-network node grouping.");
        Assert(html.Contains("data-node-level=\"0\"", StringComparison.Ordinal) && html.Contains("data-node-shape=\"star\"", StringComparison.Ordinal) && html.Contains("data-node-shape=\"database\"", StringComparison.Ordinal), "Graph explorer output should expose hierarchy levels and richer node shapes.");
        Assert(html.Contains("style=\"--cfx-node-fill:#F97316;--cfx-node-stroke:#7C2D12;filter:drop-shadow(0 5px 10px rgba(15,23,42,.18))\"", StringComparison.Ordinal), "Graph explorer SVG should render vis-network-style node color and shadow hints without hiding selected-node feedback.");
        Assert(html.Contains("class=\"cfx-graph-node-label-bg\"", StringComparison.Ordinal) && html.Contains("style=\"fill:#FFF7ED;stroke:none;stroke-width:0;pointer-events:none\"", StringComparison.Ordinal), "Graph explorer SVG should render label backgrounds for styled nodes without treating them as selectable node marks.");
        Assert(html.Contains("<text class=\"cfx-graph-node-label\" y=\"26\" style=\"--cfx-node-label-explicit:#0F172A\">Identity</text>", StringComparison.Ordinal), "Graph explorer SVG should retain styled node labels through the adaptive theme color seam with well-formed semantic classes and text attributes.");
        Assert(!html.Contains("y=\"26 style=", StringComparison.Ordinal), "Graph explorer SVG should not corrupt text attributes when a node label color is set.");
        Assert(html.Contains("data-edge-source-arrow=\"true\"", StringComparison.Ordinal) && html.Contains("data-edge-target-arrow=\"false\"", StringComparison.Ordinal), "Graph explorer output should preserve source-side vis-network arrow direction.");
        Assert(html.Contains("data-edge-width=\"3\"", StringComparison.Ordinal) && html.Contains("data-edge-color=\"#DC2626\"", StringComparison.Ordinal) && html.Contains("data-edge-label-color=\"#7F1D1D\"", StringComparison.Ordinal) && html.Contains("data-edge-physics=\"false\"", StringComparison.Ordinal), "Graph explorer output should expose vis-network edge width, color, label color, and physics flags.");
        Assert(html.Contains("style=\"--cfx-edge-stroke:#DC2626;--cfx-edge-width:3\"", StringComparison.Ordinal), "Graph explorer SVG should visibly render vis-network-style edge color and width while preserving selected-edge CSS feedback.");
        Assert(html.Contains("style=\"fill:#DC2626;stroke:#DC2626\"", StringComparison.Ordinal), "Graph explorer SVG should render arrow markers with the matching vis-network edge color.");
        Assert(html.Contains("style=\"--cfx-edge-label-explicit:#7F1D1D\">issues</text>", StringComparison.Ordinal), "Graph explorer SVG should preserve vis-network-style edge label colors through theme-aware contrast resolution.");
        Assert(html.Contains("data-edge-hidden=\"true\"", StringComparison.Ordinal) && html.Contains("cfx-graph-edge cfx-graph-hidden", StringComparison.Ordinal) && !html.Contains(">queries</text>", StringComparison.Ordinal), "Graph explorer SVG and Canvas state should suppress hidden vis-network-style edges.");
        Assert(html.Contains("physics: attr(edge, 'data-edge-physics') !== 'false'", StringComparison.Ordinal) && html.Contains("edges.filter(edge => edge.physics !== false).forEach(edge =>", StringComparison.Ordinal) && html.Contains("state.edges.filter(edge => edge.physics !== false)", StringComparison.Ordinal), "Graph explorer runtime physics should exclude edges that opt out of physics from spring, worker, and degree-derived forces.");
        Assert(html.Contains("style: { backgroundColor: attr(node.el, 'data-node-background-color')", StringComparison.Ordinal) && html.Contains("context.fillStyle = node.backgroundColor || '#2563eb'", StringComparison.Ordinal), "Graph explorer Canvas and PNG paths should consume serialized node styles.");
        Assert(html.Contains("if (node.labelBackgroundColor)", StringComparison.Ordinal) && html.Contains("context.shadowColor = 'rgba(15,23,42,.18)'", StringComparison.Ordinal), "Graph explorer Canvas and PNG paths should render node label backgrounds and shadows when style data is serialized.");
        Assert(html.Contains("drawNodeShapeMark(context, node)", StringComparison.Ordinal) && html.Contains("node.shape === 'database'", StringComparison.Ordinal), "Graph explorer Canvas and PNG paths should render rich vis-network node shapes instead of falling back to circles.");
        Assert(html.Contains("node?.shape === 'text'", StringComparison.Ordinal) && html.Contains("'database', 'text'", StringComparison.Ordinal), "Graph explorer Canvas hit testing should use label-sized rectangular geometry for text-only nodes.");
        Assert(html.Contains("context.ellipse(node.x, node.y, size * 1.55, size, 0, 0, Math.PI * 2)", StringComparison.Ordinal), "Graph explorer Canvas and PNG ellipse nodes should match SVG ellipse geometry.");
        Assert(html.Contains("data-edge-shape=\"continuous\"", StringComparison.Ordinal) && html.Contains("(edge.shape === 'line' || edge.shape === 'polyline') && Math.abs(edge.curvature) < 0.001", StringComparison.Ordinal), "Graph explorer runtime paths should keep vis-network smoothed edges curved after Canvas redraws, physics, or dragging while empty polylines stay straight.");
        Assert(html.Contains("const targetArrow = edge.targetArrow || edge.directed", StringComparison.Ordinal), "Graph explorer Canvas and PNG endpoints should shorten targets whenever the reusable graph contract renders a target arrow.");
        Assert(html.Contains("data-node-shape=\"database\"", StringComparison.Ordinal) && html.Contains(" Z M ", StringComparison.Ordinal), "Graph explorer SVG should render database nodes with cylinder-like geometry instead of a plain ellipse.");
        Assert(HtmlGraphExplorerRenderer.BuildFragmentStyle().Contains("@media (max-width: 520px)", StringComparison.Ordinal) && HtmlGraphExplorerRenderer.BuildFragmentStyle().Contains(".cfx-graph-overview", StringComparison.Ordinal) && HtmlGraphExplorerRenderer.BuildFragmentStyle().Contains("display: none !important", StringComparison.Ordinal), "Graph explorer responsive CSS should hide the overview on narrow embeds so hierarchy examples remain inspectable.");

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

        var peers = GraphScene.Create("peers", "Peers")
            .AddNode("left", "Left")
            .AddNode("right", "Right")
            .AddEdge("left-right", "left", "right");
        peers.Options.Layout.Mode = GraphLayoutMode.Hierarchical;
        var peerHtml = peers.ToGraphExplorerHtmlFragment();
        Assert(Math.Abs(GetAttribute(peerHtml, "data-node-id=\"left\"", "data-node-y") - GetAttribute(peerHtml, "data-node-id=\"right\"", "data-node-y")) < 0.01, "Hierarchical level inference should ignore undirected peer edges unless callers set levels explicitly.");

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

        var wide = GraphScene.Create("wide-hier-components", "Wide hierarchical components");
        wide.AddNode("a-root", "A root", node => node.Level = 0);
        wide.AddNode("b-root", "B root", node => node.Level = 0);
        for (var index = 0; index < 10; index++) {
            wide.AddNode("a-" + index, "A " + index, node => node.Level = 1);
            wide.AddNode("b-" + index, "B " + index, node => node.Level = 1);
            wide.AddEdge("a-link-" + index, "a-root", "a-" + index);
            wide.AddEdge("b-link-" + index, "b-root", "b-" + index);
        }

        wide.Options.Layout.Mode = GraphLayoutMode.Hierarchical;
        wide.Options.Layout.Direction = GraphLayoutDirection.LeftToRight;
        wide.Options.Layout.NodeSpacing = 30;
        wide.Options.Layout.ComponentSpacing = 120;
        var wideHtml = wide.ToGraphExplorerHtmlFragment();
        var aBottom = Enumerable.Range(0, 10).Select(index => GetAttribute(wideHtml, "data-node-id=\"a-" + index + "\"", "data-node-y")).Max();
        var bTop = Enumerable.Range(0, 10).Select(index => GetAttribute(wideHtml, "data-node-id=\"b-" + index + "\"", "data-node-y")).Min();
        Assert(aBottom < bTop, "Hierarchical layout should add each disconnected component's breadth before ComponentSpacing so wide levels do not overlap.");

        var visHierarchy = VisNetworkGraph.Create();
        visHierarchy.Options.Layout.Hierarchical.Enabled = true;
        visHierarchy.Options.Layout.Hierarchical.Direction = GraphLayoutDirection.LeftToRight;
        visHierarchy.AddNode("root", "Root").AddNode("child", "Child").AddEdge("root-child", "root", "child");
        var visHierarchyHtml = visHierarchy.ToGraphScene("vis-hierarchy", "Vis hierarchy").ToGraphExplorerHtmlFragment();
        Assert(GetAttribute(visHierarchyHtml, "data-node-id=\"root\"", "data-node-x") < GetAttribute(visHierarchyHtml, "data-node-id=\"child\"", "data-node-x"), "Vis-network hierarchical layouts should infer levels from from/to edges without requiring visible arrows.");

        var textNodeHtml = GraphScene.Create("text-node", "Text node").AddNode("note", "Anchor note", node => { node.Shape = GraphNodeShape.Text; node.Style.LabelBackgroundColor = "#E0F2FE"; }).ToGraphExplorerHtmlFragment();
        Assert(textNodeHtml.Contains("data-node-shape=\"text\"", StringComparison.Ordinal) && textNodeHtml.Contains("class=\"cfx-graph-node-label-bg\" x=\"-41.8\" y=\"-9\"", StringComparison.Ordinal) && textNodeHtml.Contains("<text class=\"cfx-graph-node-label\" y=\"4\">Anchor note</text>", StringComparison.Ordinal) && HtmlGraphExplorerRenderer.BuildInteractionScript().Contains("const labelY = node.shape === 'text' ? node.y", StringComparison.Ordinal) && HtmlGraphExplorerRenderer.BuildFragmentStyle().Contains(".cfx-graph-lod-compact .cfx-graph-node:not([data-node-shape=\"text\"]) text", StringComparison.Ordinal), "Graph explorer should render text-shaped nodes as premium label marks anchored at the node coordinates across SVG, Canvas, and compact modes.");
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
