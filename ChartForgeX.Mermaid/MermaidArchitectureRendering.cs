using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Provides ChartForgeX rendering adapters for parsed Mermaid architecture diagrams.
/// </summary>
public static class MermaidArchitectureRendering {
    /// <summary>
    /// Converts a Mermaid architecture document into a renderer-independent ChartForgeX topology chart.
    /// </summary>
    public static TopologyChart ToTopologyChart(this MermaidArchitectureDocument document, MermaidTopologyRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidTopologyRenderOptions();
        var chart = TopologyChart.Create()
            .WithId(string.IsNullOrWhiteSpace(options.Id) ? "mermaid-architecture" : options.Id!.Trim())
            .WithTitle(string.IsNullOrWhiteSpace(options.Title) ? "Mermaid architecture" : options.Title!)
            .WithSubtitle(string.IsNullOrWhiteSpace(options.Subtitle) ? document.Header : options.Subtitle!)
            .WithViewport(options.Width, options.Height, options.Padding)
            .WithLayout(TopologyLayoutMode.DenseGrouped, TopologyLayoutDirection.LeftToRight);

        foreach (var group in document.Groups) {
            chart.AddAutoGroup(group.Id, group.Title, TopologyHealthStatus.Unknown, subtitle: group.ParentId == null ? null : "in " + group.ParentId, cssClass: "cfx-mermaid-architecture-group", symbol: Symbol(group.Icon), iconId: IconId(group.Icon));
            var topologyGroup = chart.Groups[chart.Groups.Count - 1];
            topologyGroup.LayoutPolicy = TopologyGroupLayoutPolicy.Grid;
            topologyGroup.Metadata["mermaid.kind"] = "group";
            if (!string.IsNullOrWhiteSpace(group.Icon)) topologyGroup.Metadata["mermaid.icon"] = group.Icon!;
            if (!string.IsNullOrWhiteSpace(group.ParentId)) topologyGroup.Metadata["mermaid.parent"] = group.ParentId!;
        }

        foreach (var service in document.Services) {
            chart.AddAutoNode(service.Id, service.Title, ServiceKind(service.Icon), TopologyHealthStatus.Unknown, groupId: service.GroupId, width: 150, height: 72, symbol: Symbol(service.Icon), cssClass: "cfx-mermaid-architecture-service", iconId: IconId(service.Icon));
            var node = chart.Nodes[chart.Nodes.Count - 1];
            node.Metadata["mermaid.kind"] = "service";
            if (!string.IsNullOrWhiteSpace(service.Icon)) node.Metadata["mermaid.icon"] = service.Icon!;
            if (!string.IsNullOrWhiteSpace(service.GroupId)) node.Metadata["mermaid.group"] = service.GroupId!;
        }

        foreach (var junction in document.Junctions) {
            chart.AddAutoNode(junction.Id, junction.Id, TopologyNodeKind.Gateway, TopologyHealthStatus.Unknown, groupId: junction.GroupId, width: 58, height: 44, symbol: "J", cssClass: "cfx-mermaid-architecture-junction");
            var node = chart.Nodes[chart.Nodes.Count - 1];
            node.Metadata["mermaid.kind"] = "junction";
            if (!string.IsNullOrWhiteSpace(junction.GroupId)) node.Metadata["mermaid.group"] = junction.GroupId!;
        }

        var groupLookup = document.Groups.ToDictionary(group => group.Id, StringComparer.Ordinal);
        var serviceGroups = document.Services.ToDictionary(service => service.Id, service => service.GroupId, StringComparer.Ordinal);
        var junctionGroups = document.Junctions.ToDictionary(junction => junction.Id, junction => junction.GroupId, StringComparer.Ordinal);
        var boundaryAnchors = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var index = 0; index < document.Edges.Count; index++) {
            var item = document.Edges[index];
            var source = ResolveSource(item);
            var target = ResolveTarget(item);
            var sourceId = ResolveEndpointNodeId(chart, source, groupLookup, serviceGroups, junctionGroups, boundaryAnchors);
            var targetId = ResolveEndpointNodeId(chart, target, groupLookup, serviceGroups, junctionGroups, boundaryAnchors);
            chart.AddEdge("mermaid-architecture-edge-" + index.ToString(CultureInfo.InvariantCulture), sourceId, targetId, null, TopologyEdgeKind.Connectivity, TopologyHealthStatus.Unknown, Direction(item.Operator), TopologyEdgeRouting.Orthogonal);
            var edge = chart.Edges[chart.Edges.Count - 1];
            edge.Metadata["mermaid.operator"] = item.Operator;
            ApplyEndpointMetadata(edge.Metadata, "source", source);
            ApplyEndpointMetadata(edge.Metadata, "target", target);
        }

        return chart;
    }

    /// <summary>
    /// Wraps a Mermaid architecture document in a visual artifact envelope backed by a ChartForgeX topology chart.
    /// </summary>
    public static VisualArtifact ToVisualArtifact(this MermaidArchitectureDocument document, MermaidTopologyRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        var topology = document.ToTopologyChart(options);
        var artifact = VisualArtifact.Create(topology.Id ?? "mermaid-architecture", VisualArtifactKind.Mermaid, topology);
        artifact.SourceLanguage = VisualArtifactSourceLanguage.Mermaid;
        artifact.Title = topology.Title ?? string.Empty;
        artifact.Subtitle = topology.Subtitle ?? string.Empty;
        artifact.NaturalSize = new VisualArtifactSize(topology.Viewport.Width, topology.Viewport.Height);
        artifact.ExportFormats = VisualArtifactExportFormat.Svg | VisualArtifactExportFormat.Png | VisualArtifactExportFormat.Html;
        artifact.Metadata["mermaid.kind"] = document.Kind.ToString();
        artifact.Metadata["mermaid.header"] = document.Header;
        artifact.Metadata["mermaid.groups"] = document.Groups.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.services"] = document.Services.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.junctions"] = document.Junctions.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.edges"] = document.Edges.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["render.model"] = nameof(TopologyChart);
        return artifact;
    }

    /// <summary>Renders a Mermaid architecture document to static SVG.</summary>
    public static string ToSvg(this MermaidArchitectureDocument document, MermaidTopologyRenderOptions? options = null) => document.ToTopologyChart(options).ToSvg();

    /// <summary>Renders a Mermaid architecture document to static PNG.</summary>
    public static byte[] ToPng(this MermaidArchitectureDocument document, MermaidTopologyRenderOptions? options = null) => document.ToTopologyChart(options).ToPng();

    private static MermaidArchitectureEndpoint ResolveSource(MermaidArchitectureEdge edge) => edge.Operator.StartsWith("<", StringComparison.Ordinal) && !edge.Operator.EndsWith(">", StringComparison.Ordinal) ? edge.Right : edge.Left;

    private static MermaidArchitectureEndpoint ResolveTarget(MermaidArchitectureEdge edge) => edge.Operator.StartsWith("<", StringComparison.Ordinal) && !edge.Operator.EndsWith(">", StringComparison.Ordinal) ? edge.Left : edge.Right;

    private static TopologyDirection Direction(string op) {
        if (op == "<-->") return TopologyDirection.Bidirectional;
        if (op == "--") return TopologyDirection.None;
        return TopologyDirection.Forward;
    }

    private static void ApplyEndpointMetadata(System.Collections.Generic.IDictionary<string, string> metadata, string prefix, MermaidArchitectureEndpoint endpoint) {
        if (!string.IsNullOrWhiteSpace(endpoint.Side)) metadata["mermaid." + prefix + ".side"] = endpoint.Side!;
        if (endpoint.GroupBoundary) metadata["mermaid." + prefix + ".groupBoundary"] = "true";
    }

    private static string ResolveEndpointNodeId(TopologyChart chart, MermaidArchitectureEndpoint endpoint, IReadOnlyDictionary<string, MermaidArchitectureGroup> groups, IReadOnlyDictionary<string, string?> serviceGroups, IReadOnlyDictionary<string, string?> junctionGroups, Dictionary<string, string> boundaryAnchors) {
        if (!endpoint.GroupBoundary) return endpoint.Id;
        if (!serviceGroups.TryGetValue(endpoint.Id, out var groupId) && !junctionGroups.TryGetValue(endpoint.Id, out groupId)) return endpoint.Id;
        if (string.IsNullOrWhiteSpace(groupId) || !groups.ContainsKey(groupId!)) return endpoint.Id;
        var side = string.IsNullOrWhiteSpace(endpoint.Side) ? "boundary" : endpoint.Side!.Trim();
        var key = groupId + "|" + side;
        if (boundaryAnchors.TryGetValue(key, out var existing)) return existing;

        var anchorId = "mermaid-architecture-group-anchor-" + groupId + "-" + side.ToLowerInvariant();
        chart.AddAutoNode(anchorId, groups[groupId!].Title + " boundary", TopologyNodeKind.Generic, TopologyHealthStatus.Unknown, groupId: groupId, width: 1, height: 1, symbol: string.Empty, cssClass: "cfx-mermaid-architecture-group-anchor");
        var node = chart.Nodes[chart.Nodes.Count - 1];
        node.DisplayMode = TopologyNodeDisplayMode.Hidden;
        node.Metadata["mermaid.kind"] = "group-boundary-anchor";
        node.Metadata["mermaid.group"] = groupId!;
        node.Metadata["mermaid.side"] = side;
        boundaryAnchors[key] = anchorId;
        return anchorId;
    }

    private static TopologyNodeKind ServiceKind(string? icon) {
        if (icon == null) return TopologyNodeKind.Service;
        if (icon.IndexOf("database", StringComparison.OrdinalIgnoreCase) >= 0 || icon.IndexOf("db", StringComparison.OrdinalIgnoreCase) >= 0) return TopologyNodeKind.Database;
        if (icon.IndexOf("server", StringComparison.OrdinalIgnoreCase) >= 0) return TopologyNodeKind.Server;
        if (icon.IndexOf("cloud", StringComparison.OrdinalIgnoreCase) >= 0) return TopologyNodeKind.Cloud;
        if (icon.IndexOf("gateway", StringComparison.OrdinalIgnoreCase) >= 0) return TopologyNodeKind.Gateway;
        if (icon.IndexOf("disk", StringComparison.OrdinalIgnoreCase) >= 0 || icon.IndexOf("storage", StringComparison.OrdinalIgnoreCase) >= 0) return TopologyNodeKind.Storage;
        return TopologyNodeKind.Service;
    }

    private static string? Symbol(string? icon) {
        if (string.IsNullOrWhiteSpace(icon)) return null;
        return icon!.Length <= 2 ? icon.ToUpperInvariant() : icon.Substring(0, 1).ToUpperInvariant();
    }

    private static string? IconId(string? icon) => string.IsNullOrWhiteSpace(icon) ? null : "mermaid:" + icon;
}
