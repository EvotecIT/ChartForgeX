using System;
using System.IO;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void SharedPremiumRoutePrimitivesStayAdopted() {
        var root = FindRepositoryRoot();
        var routeStyles = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Rendering", "ChartRouteVisualStyles.cs"));
        var labelWrapping = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Rendering", "ChartLabelWrapping.cs"));
        var textFitting = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Rendering", "ChartTextFitting.cs"));
        var treeLayout = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Rendering", "ChartTreeLayout.cs"));
        var textHalo = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Rendering", "ChartTextHalo.cs"));
        var topologyGeographicCalloutPrimitives = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Topology", "TopologyGeographicCalloutPrimitives.cs"));
        var svgTree = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgChartRenderer.Tree.cs"));
        var pngTree = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Raster", "PngChartRenderer.Tree.cs"));
        var svgDottedMap = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgChartRenderer.DottedMap.cs"));
        var pngDottedMap = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Raster", "PngChartRenderer.DottedMap.cs"));
        var pngText = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Raster", "PngChartRenderer.Text.cs"));
        var svgGrid = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Svg", "SvgChartGridRenderer.cs"));
        var pngGrid = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Raster", "PngChartGridRenderer.cs"));
        var topologySvgPolish = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Topology", "TopologySvgRenderer.Polish.cs"));
        var topologyPngPolish = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Topology", "TopologyPngRenderer.Polish.cs"));
        var topologySvgEdgeLabels = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Topology", "TopologySvgRenderer.EdgeLabels.cs"));
        var topologyPngEdgeLabels = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Topology", "TopologyPngRenderer.EdgeLabels.cs"));
        var topologyEdgeLabelBackplate = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Topology", "TopologyRenderPrimitives.EdgeLabelBackplate.cs"));
        var topologyEdgeLabelClearance = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Topology", "TopologyRenderPrimitives.EdgeLabelClearance.cs"));
        var topologyNodeStatusBadge = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Topology", "TopologyRenderPrimitives.NodeStatusBadge.cs"));
        var topologyGroupStatusDot = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Topology", "TopologyRenderPrimitives.GroupStatusDot.cs"));
        var topologyGroupSymbol = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Topology", "TopologyRenderPrimitives.GroupSymbol.cs"));
        var topologySvgNodes = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Topology", "TopologySvgRenderer.Nodes.cs"));
        var topologyPng = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Topology", "TopologyPngRenderer.cs"));
        var topologySvg = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Topology", "TopologySvgRenderer.cs"));
        var topologySvgGroupSymbols = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Topology", "TopologySvgRenderer.GroupSymbols.cs"));
        var topologySvgGeographicCallouts = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Topology", "TopologySvgRenderer.GeographicCallouts.cs"));
        var topologyPngGeographicCallouts = File.ReadAllText(Path.Combine(root, "ChartForgeX", "Topology", "TopologyPngRenderer.GeographicCallouts.cs"));

        Assert(treeLayout.Contains("ChartTreeModel", StringComparison.Ordinal) && svgTree.Contains("ChartTreeLayout.Build", StringComparison.Ordinal) && pngTree.Contains("ChartTreeLayout.Build", StringComparison.Ordinal), "SVG and PNG tree renderers should share the same tree layout model.");
        Assert(labelWrapping.Contains("BalancedTwoLine", StringComparison.Ordinal) && svgTree.Contains("ChartLabelWrapping.BalancedTwoLine", StringComparison.Ordinal) && pngTree.Contains("ChartLabelWrapping.BalancedTwoLine", StringComparison.Ordinal), "SVG and PNG tree labels should share the same wrapping policy.");
        Assert(textFitting.Contains("TrimEnd", StringComparison.Ordinal) && svgGrid.Contains("ChartTextFitting.TrimEnd", StringComparison.Ordinal) && pngGrid.Contains("ChartTextFitting.TrimEnd", StringComparison.Ordinal) && pngGrid.Contains("MeasureEmphasizedText", StringComparison.Ordinal), "SVG and PNG grid headers should share text fitting while preserving emphasized PNG title measurement.");
        Assert(routeStyles.Contains("TreeLink", StringComparison.Ordinal) && svgTree.Contains("ChartRouteVisualStyles.TreeLink", StringComparison.Ordinal) && pngTree.Contains("ChartRouteVisualStyles.TreeLink", StringComparison.Ordinal), "SVG and PNG tree links should use the same premium route visual style.");
        Assert(routeStyles.Contains("TopologyEdge", StringComparison.Ordinal) && topologySvgPolish.Contains("ChartRouteVisualStyles.TopologyEdge", StringComparison.Ordinal) && topologyPngPolish.Contains("ChartRouteVisualStyles.TopologyEdge", StringComparison.Ordinal), "SVG and PNG topology routes should use the same premium edge visual style.");
        Assert(routeStyles.Contains("TopologyEdgeLabelLeader", StringComparison.Ordinal) && topologySvgEdgeLabels.Contains("ChartRouteVisualStyles.TopologyEdgeLabelLeader", StringComparison.Ordinal) && topologyPngEdgeLabels.Contains("ChartRouteVisualStyles.TopologyEdgeLabelLeader", StringComparison.Ordinal), "SVG and PNG topology label leaders should use the same route visual tokens.");
        Assert(topologyEdgeLabelBackplate.Contains("EdgeLabelBackplateRadius", StringComparison.Ordinal) && topologySvgEdgeLabels.Contains("AddEdgeLabelBackplate", StringComparison.Ordinal) && topologyPngEdgeLabels.Contains("DrawEdgeLabelBackplate", StringComparison.Ordinal), "SVG and PNG topology label backplates should use the same geometry and opacity tokens.");
        Assert(topologyEdgeLabelClearance.Contains("EdgeLabelClearanceRadius", StringComparison.Ordinal) && topologySvgEdgeLabels.Contains("EdgeLabelClearanceOpacity", StringComparison.Ordinal) && topologyPngEdgeLabels.Contains("EdgeLabelClearanceAlpha", StringComparison.Ordinal), "SVG and PNG topology label clearance masks should use the same geometry and opacity tokens.");
        Assert(topologyNodeStatusBadge.Contains("NodeStatusBadgeCheckPoints", StringComparison.Ordinal) && topologySvgNodes.Contains("NodeStatusBadgeCenterX", StringComparison.Ordinal) && topologyPng.Contains("NodeStatusBadgeCenterX", StringComparison.Ordinal), "SVG and PNG topology node status badges should use the same geometry and checkmark tokens.");
        Assert(topologyGroupStatusDot.Contains("GroupStatusDotReserveWidth", StringComparison.Ordinal) && topologySvg.Contains("GroupStatusDotOuterRadius", StringComparison.Ordinal) && topologyPng.Contains("GroupStatusDotOuterRadius", StringComparison.Ordinal), "SVG and PNG topology group status dots should use the same geometry and label-reserve tokens.");
        Assert(topologyGroupSymbol.Contains("GroupSymbolGlobeOuterStrokeWidth", StringComparison.Ordinal) && topologySvgGroupSymbols.Contains("GroupSymbolGlobeRadius", StringComparison.Ordinal) && topologyPng.Contains("GroupSymbolGlobeRadius", StringComparison.Ordinal), "SVG and PNG topology group symbols should use the same geometry and stroke tokens.");
        Assert(routeStyles.Contains("TopologyGeographicCalloutLeader", StringComparison.Ordinal) && topologySvgGeographicCallouts.Contains("ChartRouteVisualStyles.TopologyGeographicCalloutLeader", StringComparison.Ordinal) && topologyPngGeographicCallouts.Contains("ChartRouteVisualStyles.TopologyGeographicCalloutLeader", StringComparison.Ordinal), "SVG and PNG topology geographic callout leaders should use the same route visual tokens.");
        Assert(routeStyles.Contains("TopologyGeographicMiniTopologyLeader", StringComparison.Ordinal) && topologySvgGeographicCallouts.Contains("ChartRouteVisualStyles.TopologyGeographicMiniTopologyLeader", StringComparison.Ordinal) && topologyPngGeographicCallouts.Contains("ChartRouteVisualStyles.TopologyGeographicMiniTopologyLeader", StringComparison.Ordinal), "SVG and PNG topology geographic mini-topology leaders should use the same route visual tokens.");
        Assert(topologyGeographicCalloutPrimitives.Contains("PreviewStatuses", StringComparison.Ordinal) && topologyGeographicCalloutPrimitives.Contains("LeaderPoints", StringComparison.Ordinal) && topologyGeographicCalloutPrimitives.Contains("StatusChips", StringComparison.Ordinal) && topologyGeographicCalloutPrimitives.Contains("AnchorHaloRadius", StringComparison.Ordinal) && topologyGeographicCalloutPrimitives.Contains("MiniTopologyPoints", StringComparison.Ordinal) && topologyGeographicCalloutPrimitives.Contains("TitleMaxLength", StringComparison.Ordinal) && topologyGeographicCalloutPrimitives.Contains("StatusChipTextFontSize", StringComparison.Ordinal) && topologySvgGeographicCallouts.Contains("TopologyGeographicCalloutPrimitives.PreviewStatuses", StringComparison.Ordinal) && topologyPngGeographicCallouts.Contains("TopologyGeographicCalloutPrimitives.PreviewStatuses", StringComparison.Ordinal), "SVG and PNG topology geographic callouts should share route, status, text, and geometry helpers.");
        Assert(routeStyles.Contains("DottedMapLeader", StringComparison.Ordinal) && svgDottedMap.Contains("ChartRouteVisualStyles.DottedMapLeader", StringComparison.Ordinal) && pngDottedMap.Contains("ChartRouteVisualStyles.DottedMapLeader", StringComparison.Ordinal), "SVG and PNG dotted-map leaders should use the same premium route visual style.");
        Assert(textHalo.Contains("ChartTextHaloLayer", StringComparison.Ordinal) && textHalo.Contains("SvgStrokeWidth", StringComparison.Ordinal) && pngText.Contains("ChartTextHalo.ReadableRasterLayers", StringComparison.Ordinal) && topologyPngPolish.Contains("ChartTextHalo.CompactRasterLayers", StringComparison.Ordinal) && topologySvgPolish.Contains("ChartTextHalo.SvgStrokeWidth", StringComparison.Ordinal), "SVG and PNG chart and topology labels should share reusable halo primitives.");
    }
}
