using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

/// <summary>Maps inert Agentflow semantics to the canonical native topology scene.</summary>
public static class MermaidAgentflowRendering {
    /// <summary>Creates a native workflow layout without invoking instructions, connectors or external engines.</summary>
    public static TopologyChart ToTopologyChart(this MermaidAgentflowDocument document, MermaidTopologyRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidTopologyRenderOptions();
        var padding=!options.HasExplicitPadding&&document.Configuration.Graph?.Padding is double sourcePadding?sourcePadding:options.Padding;
        var chart = TopologyChart.Create().WithId(options.Id ?? "mermaid-agentflow")
            .WithTitle(MermaidHeading.Title(document, options.Title, null, "Agent workflow"))
            .WithSubtitle(MermaidHeading.Subtitle(options.Subtitle)).WithViewport(options.Width, options.Height, padding)
            .WithLayout(TopologyLayoutMode.Layered, Direction(document.Direction));
        var flows = new Dictionary<string, MermaidAgentflowFlow>(StringComparer.Ordinal);
        foreach (var flow in document.Flows) flows.Add(flow.Id, flow);
        var parents = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var flow in document.Flows) parents[flow.Id] = flow.ParentId;
        foreach (var node in document.Nodes) parents[node.Id] = node.IsGlobal ? null : node.ParentFlowId;
        string Visible(string id) {
            var current = id; var seen = new HashSet<string>(StringComparer.Ordinal);
            var visible = id;
            while (seen.Add(current)) {
                if (flows.TryGetValue(current, out var flow) && flow.Collapsed) visible = current;
                if (!parents.TryGetValue(current, out var parent) || parent == null) return visible;
                current = parent;
                MermaidResourceGuard.CheckDepth(seen.Count + 1, document.HeaderSpan);
            }
            throw new ArgumentException("Agentflow containment must be acyclic.", nameof(document));
        }
        foreach (var flow in document.Flows) {
            MermaidResourceGuard.Checkpoint();
            if (Visible(flow.Id) != flow.Id) continue;
            if (flow.Collapsed) {
                chart.AddAutoNode(flow.Id, flow.Label ?? flow.Id, TopologyNodeKind.Generic, TopologyHealthStatus.Unknown,
                    groupId: flow.ParentId, width: 180, height: 80);
                var node = chart.Nodes[chart.Nodes.Count - 1];
                node.Shape = TopologyNodeShape.CollapsedContainer; node.ShowStatusBadge = false; node.FitLabelToContent = true;
                node.PreserveDisplayModeSize = true; node.BackgroundColor = "transparent";
                node.Metadata["mermaid.collapsed"] = "true";
                Copy(flow.Metadata, node.Metadata);
                MermaidNativeStyles.Node(node,flow.Classes,flow.Styles);
            } else {
                chart.AddAutoGroup(flow.Id, flow.Label ?? flow.Id);
                var group = chart.Groups[chart.Groups.Count - 1]; group.ParentGroupId = flow.ParentId;
                group.BackgroundColor = "transparent";
                group.LayoutDirection = flow.Direction == null ? null : Direction(flow.Direction);
                Copy(flow.Metadata, group.Metadata);
                MermaidNativeStyles.Group(group,flow.Classes,flow.Styles);
            }
        }
        foreach (var item in document.Nodes) {
            MermaidResourceGuard.Checkpoint();
            if (Visible(item.Id) != item.Id) continue;
            if (!TryNativeShape(item.Shape, out var shape)) throw new ArgumentException("Agentflow node shape has no native mapping: " + item.Shape, nameof(document));
            chart.AddAutoNode(item.Id, item.Label ?? item.Id, TopologyNodeKind.Generic, TopologyHealthStatus.Unknown,
                groupId: item.IsGlobal ? null : item.ParentFlowId, width: 160, height: 64);
            var node = chart.Nodes[chart.Nodes.Count - 1]; node.Shape = shape; node.ShowStatusBadge = false;
            node.FitLabelToContent = true; node.PreserveDisplayModeSize = true;
            node.Metadata["mermaid.agentflow.shape"] = item.Shape;
            node.Metadata["mermaid.agentflow.connector"] = item.IsConnector ? "true" : "false";
            node.Metadata["mermaid.agentflow.global"] = item.IsGlobal ? "true" : "false";
            Copy(item.Metadata, node.Metadata);
            MermaidNativeStyles.Node(node,item.Classes,item.Styles);
        }
        foreach (var item in document.Edges) {
            MermaidResourceGuard.Checkpoint();
            var source = Visible(item.SourceId); var target = Visible(item.TargetId);
            if (source == target && (source != item.SourceId || target != item.TargetId)) continue;
            chart.AddEdge(item.Id, source, target, item.Label, item.Kind == MermaidAgentflowEdgeKind.Reference ? TopologyEdgeKind.Dependency : TopologyEdgeKind.Generic,
                TopologyHealthStatus.Unknown, item.Kind == MermaidAgentflowEdgeKind.Reference ? VisualLinkDirection.None : VisualLinkDirection.Forward,
                TopologyEdgeRouting.Orthogonal);
            var edge = chart.Edges[chart.Edges.Count - 1];
            edge.LineStyle = item.Kind == MermaidAgentflowEdgeKind.Reference ? TopologyEdgeLineStyle.Dashed : TopologyEdgeLineStyle.Solid;
            edge.TargetMarker = item.Kind == MermaidAgentflowEdgeKind.Failure ? TopologyMarkerKind.Cross : item.Kind == MermaidAgentflowEdgeKind.Reference ? TopologyMarkerKind.None : TopologyMarkerKind.Arrow;
            edge.Metadata["mermaid.agentflow.kind"] = item.Kind.ToString();
            edge.Metadata["mermaid.agentflow.source"] = item.SourceId; edge.Metadata["mermaid.agentflow.target"] = item.TargetId;
            Copy(item.Metadata, edge.Metadata);
            MermaidNativeStyles.Edge(edge,item.Classes,item.Styles);
        }
        MermaidPresentation.Apply(chart,document,options.Presentation);
        document.Configuration.Graph?.Apply(chart,options.Presentation);
        return chart;
    }
    /// <summary>Creates an SVG/PNG/HTML/JSON artifact backed by one native workflow.</summary>
    public static VisualArtifact ToVisualArtifact(this MermaidAgentflowDocument document, MermaidTopologyRenderOptions? options = null) {
        var artifact = document.ToTopologyChart(options).ToVisualArtifact(VisualArtifactSourceLanguage.Mermaid);
        artifact.Kind = VisualArtifactKind.Mermaid;
        artifact.Metadata["mermaid.kind"] = document.Kind.ToString(); artifact.Metadata["mermaid.header"] = document.Header;
        artifact.Metadata["mermaid.nodes"] = document.Nodes.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.flows"] = document.Flows.Count.ToString(CultureInfo.InvariantCulture);
        return MermaidPresentation.Apply(artifact, document);
    }
    /// <summary>Exports the native workflow as script-free SVG.</summary>
    public static string ToSvg(this MermaidAgentflowDocument document, MermaidTopologyRenderOptions? options = null) => document.ToVisualArtifact(options).ToSvg();
    /// <summary>Exports the same native workflow as PNG.</summary>
    public static byte[] ToPng(this MermaidAgentflowDocument document, MermaidTopologyRenderOptions? options = null) => document.ToVisualArtifact(options).ToPng();
    internal static bool TryNativeShape(string shape, out TopologyNodeShape native) => MermaidAgentflowShapes.TryResolve(shape, out native);
    private static TopologyLayoutDirection Direction(string? value) => value switch {
        "LR" => TopologyLayoutDirection.LeftToRight, "RL" => TopologyLayoutDirection.RightToLeft,
        "BT" => TopologyLayoutDirection.BottomToTop, _ => TopologyLayoutDirection.TopToBottom
    };
    private static void Copy(Dictionary<string, MermaidMetadataValue> source, Dictionary<string, string> target) {
        foreach (var pair in source) { MermaidResourceGuard.Checkpoint(); target["mermaid.metadata." + pair.Key] = pair.Value.ToJson(); }
    }
}
