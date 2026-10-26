using System.Collections.Generic;
using ChartForgeX.Topology;

namespace ChartForgeX.Mermaid;

/// <summary>Projects only qualified finite source surface elements through the native paint and capture owners.</summary>
internal static class MermaidTopologySurfaceElements {
    internal static bool TryOpacity(string value, out double opacity) {
        if (!MermaidNativeStyles.SupportsEdge("opacity", value)) { opacity = 1; return false; }
        return MermaidStyleValues.TryPixels(value, out opacity, allowUnit: false);
    }
    internal static bool DeferredSurface(string property, string value) =>
        property == "opacity" && TryOpacity(value, out _) || MermaidNativeStyles.SupportsSurface(property, value);
    private static bool Classic(MermaidDocument document) {
        var family = document.Kind == MermaidDiagramKind.Agentflow ? "agentflow.look" : "flowchart.look";
        var look = document.Configuration.Last(family) ?? document.Configuration.Last("look");
        return look == null || look.Value == "classic";
    }
    internal static bool Supports(MermaidFlowchartDocument document, MermaidFlowchartNode node) =>
        Classic(document) && document.Kind == MermaidDiagramKind.Flowchart && !node.Properties.ContainsKey("icon") && !node.Properties.ContainsKey("img")
        && node.Shape is MermaidFlowchartNodeShape.Default or MermaidFlowchartNodeShape.Rectangle or MermaidFlowchartNodeShape.Person
            or MermaidFlowchartNodeShape.DoubleCircle or MermaidFlowchartNodeShape.Bucket or MermaidFlowchartNodeShape.Stadium;
    internal static bool Supports(MermaidAgentflowDocument document, MermaidAgentflowNode node) => Classic(document) && !node.IsConnector && node.Shape == "task";
    internal static bool SupportsGroups(MermaidFlowchartDocument document) => Classic(document) && document.Kind == MermaidDiagramKind.Flowchart;
    internal static void Diagnose(MermaidAstNode target, Dictionary<string,string> styles, bool supported,
        string family, MermaidParseResult<MermaidDocument> result) {
        if (!supported && styles.TryGetValue("opacity", out var value) && TryOpacity(value, out _))
            MermaidParserUtilities.Add(result, target.SurfaceOpacitySpan ?? target.Span, MermaidDiagnosticSeverity.Warning,
                family + " node/group style 'opacity' is retained without native application.", MermaidDiagnosticCodes.UnsupportedStatement);
    }
    internal static void Apply(TopologyChart chart, MermaidDocument document) {
        if (document is MermaidFlowchartDocument flow && document.Kind == MermaidDiagramKind.Flowchart) {
            foreach (var source in flow.Nodes) if (Supports(flow, source)) {
                var target = chart.Nodes.Find(node => node.Id == source.Id); if (target != null) Apply(target, source.Styles);
            }
            foreach (var source in flow.Subgraphs) if (SupportsGroups(flow) && source.Styles.TryGetValue("opacity", out var value) && TryOpacity(value, out var opacity)) {
                var target = chart.Groups.Find(group => group.Id == source.Id); if (target != null) target.SurfaceOpacity = opacity;
            }
        } else if (document is MermaidAgentflowDocument agentflow) {
            foreach (var source in agentflow.Nodes) if (Supports(agentflow, source)) {
                var target = chart.Nodes.Find(node => node.Id == source.Id); if (target != null) Apply(target, source.Styles);
            }
        }
    }
    private static void Apply(TopologyNode node, Dictionary<string,string> styles) {
        if (!styles.TryGetValue("opacity", out var value) || !TryOpacity(value, out var opacity)) return;
        node.SurfaceOpacity = opacity;
        if (node.Shape is TopologyNodeShape.DoubleCircle or TopologyNodeShape.Stadium)
            node.SurfaceElementAppearance = new TopologySurfaceElementAppearance(node.Shape.Value, node.DisplayMode, opacity,
                node.Shape == TopologyNodeShape.DoubleCircle ? opacity : (double?)null);
    }
}
