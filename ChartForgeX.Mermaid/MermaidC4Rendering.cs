using System;
using System.Globalization;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Provides ChartForgeX rendering adapters for parsed Mermaid C4 diagrams.
/// </summary>
public static class MermaidC4Rendering {
    /// <summary>
    /// Converts a Mermaid C4 document into a renderer-independent ChartForgeX topology chart.
    /// </summary>
    public static TopologyChart ToTopologyChart(this MermaidC4Document document, MermaidTopologyRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidTopologyRenderOptions();
        var chart = TopologyChart.Create()
            .WithId(string.IsNullOrWhiteSpace(options.Id) ? "mermaid-c4" : options.Id!.Trim())
            .WithTitle(string.IsNullOrWhiteSpace(options.Title) ? ResolveTitle(document) : options.Title!)
            .WithSubtitle(string.IsNullOrWhiteSpace(options.Subtitle) ? document.Header : options.Subtitle!)
            .WithViewport(options.Width, options.Height, options.Padding)
            .WithLayout(TopologyLayoutMode.DenseGrouped, ToLayoutDirection(document.Direction));

        foreach (var boundary in document.Boundaries) {
            chart.AddAutoGroup(boundary.Id, boundary.Label, TopologyHealthStatus.Unknown, subtitle: BoundarySubtitle(boundary), cssClass: "cfx-mermaid-c4-boundary", symbol: BoundarySymbol(boundary), iconId: "mermaid:c4-boundary");
            var group = chart.Groups[chart.Groups.Count - 1];
            group.LayoutPolicy = TopologyGroupLayoutPolicy.Grid;
            group.Metadata["mermaid.kind"] = boundary.Kind;
            if (!string.IsNullOrWhiteSpace(boundary.Type)) group.Metadata["mermaid.type"] = boundary.Type!;
            if (!string.IsNullOrWhiteSpace(boundary.ParentId)) group.Metadata["mermaid.parent"] = boundary.ParentId!;
        }

        foreach (var element in document.Elements) {
            chart.AddAutoNode(element.Alias, element.Label, NodeKind(element), TopologyHealthStatus.Unknown, groupId: element.BoundaryId, subtitle: ElementSubtitle(element), width: NodeWidth(element), height: 76, symbol: NodeSymbol(element), cssClass: "cfx-mermaid-c4-element", iconId: IconId(element));
            var node = chart.Nodes[chart.Nodes.Count - 1];
            node.Metadata["mermaid.kind"] = element.Kind;
            if (!string.IsNullOrWhiteSpace(element.Description)) node.Metadata["mermaid.description"] = element.Description!;
            if (!string.IsNullOrWhiteSpace(element.Technology)) node.Metadata["mermaid.technology"] = element.Technology!;
            if (!string.IsNullOrWhiteSpace(element.Sprite)) node.Metadata["mermaid.sprite"] = element.Sprite!;
            if (!string.IsNullOrWhiteSpace(element.Tags)) node.Metadata["mermaid.tags"] = element.Tags!;
            if (!string.IsNullOrWhiteSpace(element.Link)) node.Metadata["mermaid.link"] = element.Link!;
            if (!string.IsNullOrWhiteSpace(element.BoundaryId)) node.Metadata["mermaid.boundary"] = element.BoundaryId!;
        }

        for (var index = 0; index < document.Relationships.Count; index++) {
            var relationship = document.Relationships[index];
            chart.AddEdge("mermaid-c4-edge-" + index.ToString(CultureInfo.InvariantCulture), relationship.SourceAlias, relationship.TargetAlias, relationship.Label, TopologyEdgeKind.DataFlow, TopologyHealthStatus.Unknown, Direction(relationship), TopologyEdgeRouting.Orthogonal, secondaryLabel: relationship.Technology, href: relationship.Link, cssClass: "cfx-mermaid-c4-rel");
            var edge = chart.Edges[chart.Edges.Count - 1];
            edge.Metadata["mermaid.kind"] = relationship.Kind;
            if (!string.IsNullOrWhiteSpace(relationship.Tags)) edge.Metadata["mermaid.tags"] = relationship.Tags!;
            ApplyPortHints(edge, relationship.Kind);
        }

        return chart;
    }

    /// <summary>
    /// Wraps a Mermaid C4 document in a visual artifact envelope backed by a ChartForgeX topology chart.
    /// </summary>
    public static VisualArtifact ToVisualArtifact(this MermaidC4Document document, MermaidTopologyRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        var topology = document.ToTopologyChart(options);
        var artifact = VisualArtifact.Create(topology.Id ?? "mermaid-c4", VisualArtifactKind.Mermaid, topology);
        artifact.SourceLanguage = VisualArtifactSourceLanguage.Mermaid;
        artifact.Title = topology.Title ?? string.Empty;
        artifact.Subtitle = topology.Subtitle ?? string.Empty;
        artifact.NaturalSize = new VisualArtifactSize(topology.Viewport.Width, topology.Viewport.Height);
        artifact.ExportFormats = VisualArtifactExportFormat.Svg | VisualArtifactExportFormat.Png | VisualArtifactExportFormat.Html;
        artifact.Metadata["mermaid.kind"] = document.Kind.ToString();
        artifact.Metadata["mermaid.header"] = document.Header;
        artifact.Metadata["mermaid.c4Type"] = document.DiagramType;
        artifact.Metadata["mermaid.boundaries"] = document.Boundaries.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.elements"] = document.Elements.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.relationships"] = document.Relationships.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.retained"] = document.RetainedStatements.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["render.model"] = nameof(TopologyChart);
        return artifact;
    }

    /// <summary>Renders a Mermaid C4 document to static SVG.</summary>
    public static string ToSvg(this MermaidC4Document document, MermaidTopologyRenderOptions? options = null) => document.ToTopologyChart(options).ToSvg();

    /// <summary>Renders a Mermaid C4 document to static PNG.</summary>
    public static byte[] ToPng(this MermaidC4Document document, MermaidTopologyRenderOptions? options = null) => document.ToTopologyChart(options).ToPng();

    private static string ResolveTitle(MermaidC4Document document) => string.IsNullOrWhiteSpace(document.Title) ? "Mermaid C4 diagram" : document.Title!;

    private static string? BoundarySubtitle(MermaidC4Boundary boundary) {
        if (!string.IsNullOrWhiteSpace(boundary.Type)) return boundary.Type!;
        return string.IsNullOrWhiteSpace(boundary.ParentId) ? boundary.Kind : "in " + boundary.ParentId;
    }

    private static string? ElementSubtitle(MermaidC4Element element) {
        if (!string.IsNullOrWhiteSpace(element.Technology) && !string.IsNullOrWhiteSpace(element.Description)) return element.Technology + " - " + element.Description;
        if (!string.IsNullOrWhiteSpace(element.Technology)) return element.Technology;
        return element.Description;
    }

    private static TopologyNodeKind NodeKind(MermaidC4Element element) {
        var kind = element.Kind;
        if (Has(kind, "person")) return TopologyNodeKind.Person;
        if (Has(kind, "queue")) return TopologyNodeKind.Queue;
        if (Has(kind, "db")) return TopologyNodeKind.Database;
        if (Has(kind, "container")) return TopologyNodeKind.Service;
        if (Has(kind, "component")) return TopologyNodeKind.Application;
        if (Has(kind, "external")) return TopologyNodeKind.Cloud;
        return TopologyNodeKind.Application;
    }

    private static string NodeSymbol(MermaidC4Element element) {
        if (Has(element.Kind, "person")) return "P";
        if (Has(element.Kind, "container")) return "C";
        if (Has(element.Kind, "component")) return "M";
        return "S";
    }

    private static string BoundarySymbol(MermaidC4Boundary boundary) {
        if (Has(boundary.Kind, "enterprise")) return "E";
        if (Has(boundary.Kind, "container")) return "C";
        if (Has(boundary.Kind, "node")) return "N";
        return "B";
    }

    private static string IconId(MermaidC4Element element) {
        if (Has(element.Kind, "person")) return "mermaid:c4-person";
        if (Has(element.Kind, "queue")) return "mermaid:c4-queue";
        if (Has(element.Kind, "db")) return "mermaid:c4-database";
        if (Has(element.Kind, "component")) return "mermaid:c4-component";
        if (Has(element.Kind, "container")) return "mermaid:c4-container";
        return "mermaid:c4-system";
    }

    private static double NodeWidth(MermaidC4Element element) => Has(element.Kind, "person") ? 148 : 174;

    private static TopologyDirection Direction(MermaidC4Relationship relationship) {
        if (relationship.Kind == "birel") return TopologyDirection.Bidirectional;
        if (relationship.Kind == "relback") return TopologyDirection.Backward;
        return TopologyDirection.Forward;
    }

    private static TopologyLayoutDirection ToLayoutDirection(string? direction) {
        if (string.Equals(direction, "BT", StringComparison.OrdinalIgnoreCase)) return TopologyLayoutDirection.BottomToTop;
        if (string.Equals(direction, "RL", StringComparison.OrdinalIgnoreCase)) return TopologyLayoutDirection.RightToLeft;
        if (string.Equals(direction, "LR", StringComparison.OrdinalIgnoreCase)) return TopologyLayoutDirection.LeftToRight;
        return TopologyLayoutDirection.TopToBottom;
    }

    private static void ApplyPortHints(TopologyEdge edge, string kind) {
        if (kind == "relu" || kind == "relup") edge.TargetPort = TopologyEdgePort.Top;
        if (kind == "reld" || kind == "reldown") edge.TargetPort = TopologyEdgePort.Bottom;
        if (kind == "rell" || kind == "relleft") edge.TargetPort = TopologyEdgePort.Left;
        if (kind == "relr" || kind == "relright") edge.TargetPort = TopologyEdgePort.Right;
    }

    private static bool Has(string value, string token) => value.IndexOf(token, StringComparison.Ordinal) >= 0;
}
