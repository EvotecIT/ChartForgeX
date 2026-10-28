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
        using var sourceScope = MermaidResourceGuard.BeginIfAbsent(document.CapturedResourceLimits);
        if (document.Alignments.Count > 0) MermaidResourceGuard.ValidateDocument(document);
        var chart = TopologyChart.Create()
            .WithId(string.IsNullOrWhiteSpace(options.Id) ? "mermaid-architecture" : options.Id!.Trim())
            .WithTitle(MermaidHeading.Title(document, options.Title, null, "Mermaid architecture"))
            .WithSubtitle(MermaidHeading.Subtitle(options.Subtitle))
            .WithViewport(options.Width, options.Height, options.Padding)
            .WithLayout(document.Alignments.Count > 0 || document.Groups.Any(group => !string.IsNullOrWhiteSpace(group.ParentId))
                ? TopologyLayoutMode.Layered : TopologyLayoutMode.DenseGrouped, TopologyLayoutDirection.LeftToRight);

        foreach (var group in document.Groups) {
            chart.AddAutoGroup(group.Id, string.IsNullOrWhiteSpace(group.Title) ? group.Id : group.Title, TopologyHealthStatus.Unknown, subtitle: group.ParentId == null ? null : "in " + group.ParentId, cssClass: "cfx-mermaid-architecture-group", iconId: MermaidArchitectureIcons.NativeId(group.Icon));
            var topologyGroup = chart.Groups[chart.Groups.Count - 1];
            topologyGroup.ShowLabel = !string.IsNullOrWhiteSpace(group.Title);
            topologyGroup.LayoutPolicy = TopologyGroupLayoutPolicy.Grid;
            topologyGroup.ParentGroupId = group.ParentId;
            topologyGroup.Metadata["mermaid.kind"] = "group";
            if (!string.IsNullOrWhiteSpace(group.Icon)) topologyGroup.Metadata["mermaid.icon"] = group.Icon!;
            if (!string.IsNullOrWhiteSpace(group.ParentId)) topologyGroup.Metadata["mermaid.parent"] = group.ParentId!;
        }

        foreach (var service in document.Services) {
            chart.AddAutoNode(service.Id, string.IsNullOrWhiteSpace(service.Title) ? service.Id : service.Title, ServiceKind(service.Icon), TopologyHealthStatus.Unknown, groupId: service.GroupId, width: 150, height: 72, cssClass: "cfx-mermaid-architecture-service", iconId: MermaidArchitectureIcons.NativeId(service.Icon));
            var node = chart.Nodes[chart.Nodes.Count - 1];
            node.ShowLabel = !string.IsNullOrWhiteSpace(service.Title);
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
        for (var index = 0; index < document.Edges.Count; index++) {
            var item = document.Edges[index];
            var source = ResolveSource(item);
            var target = ResolveTarget(item);
            var sourceId = ResolveEndpointNodeId(source, groupLookup, serviceGroups, junctionGroups);
            var targetId = ResolveEndpointNodeId(target, groupLookup, serviceGroups, junctionGroups);
            chart.AddEdge("mermaid-architecture-edge-" + index.ToString(CultureInfo.InvariantCulture), sourceId, targetId, null, TopologyEdgeKind.Connectivity, TopologyHealthStatus.Unknown, Direction(item.Operator), TopologyEdgeRouting.Orthogonal);
            var edge = chart.Edges[chart.Edges.Count - 1];
            edge.SourcePort = Port(source.Side); edge.TargetPort = Port(target.Side);
            edge.Metadata["mermaid.operator"] = item.Operator;
            ApplyEndpointMetadata(edge.Metadata, "source", source);
            ApplyEndpointMetadata(edge.Metadata, "target", target);
        }

        for (var index = 0; index < document.Alignments.Count; index++) {
            var source = document.Alignments[index];
            var alignment = new TopologyAlignment("mermaid-architecture-alignment-" + index.ToString(CultureInfo.InvariantCulture), source.Kind, source.Members);
            alignment.Metadata["mermaid.kind"] = "alignment";
            alignment.Metadata["mermaid.line"] = source.Span.Line.ToString(CultureInfo.InvariantCulture);
            alignment.Metadata["mermaid.column"] = source.Span.Column.ToString(CultureInfo.InvariantCulture);
            alignment.Metadata["mermaid.length"] = source.Span.Length.ToString(CultureInfo.InvariantCulture);
            chart.Alignments.Add(alignment);
        }
        return MermaidPresentation.Apply(chart, document, options.Presentation);
    }

    /// <summary>
    /// Wraps a Mermaid architecture document in a visual artifact envelope backed by a ChartForgeX topology chart.
    /// </summary>
    public static VisualArtifact ToVisualArtifact(this MermaidArchitectureDocument document, MermaidTopologyRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        var topology = document.ToTopologyChart(options);
        VisualArtifact artifact;
        try { artifact = topology.ToVisualArtifact(VisualArtifactSourceLanguage.Mermaid); }
        catch (TopologyAlignmentLimitException exception) {
            throw new MermaidResourceLimitException(exception.LimitName, exception.Limit, exception.Actual, AlignmentSpan(document, exception.AlignmentId));
        }
        artifact.Kind = VisualArtifactKind.Mermaid;
        artifact.ExportFormats = VisualArtifactExportFormat.Svg | VisualArtifactExportFormat.Png | VisualArtifactExportFormat.Html | VisualArtifactExportFormat.Json;
        artifact.Metadata["mermaid.kind"] = document.Kind.ToString();
        artifact.Metadata["mermaid.header"] = document.Header;
        artifact.Metadata["mermaid.groups"] = document.Groups.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.services"] = document.Services.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.junctions"] = document.Junctions.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.edges"] = document.Edges.Count.ToString(CultureInfo.InvariantCulture);
        if (document.Alignments.Count > 0) artifact.Metadata["mermaid.alignments"] = document.Alignments.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["render.model"] = nameof(TopologyChart);
        return MermaidPresentation.Apply(artifact, document);
    }

    /// <summary>Renders a Mermaid architecture document to static SVG.</summary>
    public static string ToSvg(this MermaidArchitectureDocument document, MermaidTopologyRenderOptions? options = null) => document.ToVisualArtifact(options).ToSvg();

    /// <summary>Renders a Mermaid architecture document to static PNG.</summary>
    public static byte[] ToPng(this MermaidArchitectureDocument document, MermaidTopologyRenderOptions? options = null) => document.ToVisualArtifact(options).ToPng();

    internal static MermaidSourceSpan AlignmentSpan(MermaidArchitectureDocument document, string? id) {
        for (var index = 0; index < document.Alignments.Count; index++)
            if (id == "mermaid-architecture-alignment-" + index.ToString(CultureInfo.InvariantCulture)) return document.Alignments[index].Span;
        return document.Alignments.Count == 0 ? document.HeaderSpan : document.Alignments[0].Span;
    }

    private static MermaidArchitectureEndpoint ResolveSource(MermaidArchitectureEdge edge) => edge.Operator.StartsWith("<", StringComparison.Ordinal) && !edge.Operator.EndsWith(">", StringComparison.Ordinal) ? edge.Right : edge.Left;

    private static MermaidArchitectureEndpoint ResolveTarget(MermaidArchitectureEdge edge) => edge.Operator.StartsWith("<", StringComparison.Ordinal) && !edge.Operator.EndsWith(">", StringComparison.Ordinal) ? edge.Left : edge.Right;

    private static VisualLinkDirection Direction(string op) {
        if (op == "<-->") return VisualLinkDirection.Bidirectional;
        if (op == "--") return VisualLinkDirection.None;
        return VisualLinkDirection.Forward;
    }

    private static void ApplyEndpointMetadata(System.Collections.Generic.IDictionary<string, string> metadata, string prefix, MermaidArchitectureEndpoint endpoint) {
        if (!string.IsNullOrWhiteSpace(endpoint.Side)) metadata["mermaid." + prefix + ".side"] = endpoint.Side!;
        if (endpoint.GroupBoundary) {
            metadata["mermaid." + prefix + ".groupBoundary"] = "true";
            metadata["mermaid." + prefix + ".id"] = endpoint.Id;
        }
    }

    // A modifier belongs to the service/junction's immediate containing group. An ungrouped
    // endpoint remains a real node, matching the source DB's accepted boundary cases.
    private static string ResolveEndpointNodeId(MermaidArchitectureEndpoint endpoint, IReadOnlyDictionary<string, MermaidArchitectureGroup> groups,
        IReadOnlyDictionary<string, string?> serviceGroups, IReadOnlyDictionary<string, string?> junctionGroups) {
        if (!endpoint.GroupBoundary) return endpoint.Id;
        if (!serviceGroups.TryGetValue(endpoint.Id, out var groupId) && !junctionGroups.TryGetValue(endpoint.Id, out groupId)) return endpoint.Id;
        return !string.IsNullOrWhiteSpace(groupId) && groups.ContainsKey(groupId!) ? groupId! : endpoint.Id;
    }

    private static TopologyEdgePort Port(string? side) => side switch {
        "L" => TopologyEdgePort.Left, "R" => TopologyEdgePort.Right,
        "T" => TopologyEdgePort.Top, "B" => TopologyEdgePort.Bottom,
        _ => TopologyEdgePort.Auto
    };

    private static TopologyNodeKind ServiceKind(string? icon) => icon switch {
        "database" => TopologyNodeKind.Database,
        "server" => TopologyNodeKind.Server,
        "cloud" => TopologyNodeKind.Cloud,
        _ => TopologyNodeKind.Service
    };
}
