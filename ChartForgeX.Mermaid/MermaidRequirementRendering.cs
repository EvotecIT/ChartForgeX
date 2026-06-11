using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Provides ChartForgeX rendering adapters for parsed Mermaid requirement diagrams.
/// </summary>
public static class MermaidRequirementRendering {
    /// <summary>
    /// Converts a Mermaid requirement document into a renderer-independent ChartForgeX topology chart.
    /// </summary>
    public static TopologyChart ToTopologyChart(this MermaidRequirementDocument document, MermaidTopologyRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        options ??= new MermaidTopologyRenderOptions();
        var direction = ToLayoutDirection(document.Direction);
        var chart = TopologyChart.Create()
            .WithId(string.IsNullOrWhiteSpace(options.Id) ? "mermaid-requirement" : options.Id!.Trim())
            .WithTitle(string.IsNullOrWhiteSpace(options.Title) ? "Mermaid requirement diagram" : options.Title!)
            .WithSubtitle(string.IsNullOrWhiteSpace(options.Subtitle) ? document.Header : options.Subtitle!)
            .WithViewport(options.Width, options.Height, options.Padding)
            .WithLayout(TopologyLayoutMode.Layered, direction);

        foreach (var requirement in document.Requirements) {
            chart.AddAutoNode(requirement.Name, RequirementLabel(requirement), TopologyNodeKind.Certificate, RiskStatus(requirement.Risk), subtitle: RequirementSubtitle(requirement), width: 170, height: 82, symbol: "R", cssClass: CssClass("cfx-mermaid-requirement", requirement.Classes));
            var node = chart.Nodes[chart.Nodes.Count - 1];
            node.Metadata["mermaid.kind"] = "requirement";
            node.Metadata["mermaid.requirementType"] = requirement.RequirementType;
            if (!string.IsNullOrWhiteSpace(requirement.RequirementId)) node.Metadata["mermaid.requirementId"] = requirement.RequirementId!;
            if (!string.IsNullOrWhiteSpace(requirement.Risk)) node.Metadata["mermaid.risk"] = requirement.Risk!;
            if (!string.IsNullOrWhiteSpace(requirement.VerifyMethod)) node.Metadata["mermaid.verifyMethod"] = requirement.VerifyMethod!;
            if (requirement.Classes.Count > 0) node.Metadata["mermaid.classes"] = string.Join(",", requirement.Classes);
        }

        foreach (var element in document.Elements) {
            chart.AddAutoNode(element.Name, element.Name, TopologyNodeKind.Application, TopologyHealthStatus.Unknown, subtitle: ElementSubtitle(element), width: 160, height: 72, symbol: "E", cssClass: CssClass("cfx-mermaid-requirement-element", element.Classes));
            var node = chart.Nodes[chart.Nodes.Count - 1];
            node.Metadata["mermaid.kind"] = "element";
            if (!string.IsNullOrWhiteSpace(element.ElementType)) node.Metadata["mermaid.elementType"] = element.ElementType!;
            if (!string.IsNullOrWhiteSpace(element.DocumentReference)) node.Metadata["mermaid.docref"] = element.DocumentReference!;
            if (element.Classes.Count > 0) node.Metadata["mermaid.classes"] = string.Join(",", element.Classes);
        }

        for (var index = 0; index < document.Relationships.Count; index++) {
            var relationship = document.Relationships[index];
            chart.AddEdge("mermaid-requirement-edge-" + index.ToString(CultureInfo.InvariantCulture), relationship.SourceName, relationship.TargetName, relationship.RelationshipType, ToEdgeKind(relationship.RelationshipType), TopologyHealthStatus.Unknown, TopologyDirection.Forward, TopologyEdgeRouting.Orthogonal);
            chart.Edges[chart.Edges.Count - 1].Metadata["mermaid.relationship"] = relationship.RelationshipType;
        }

        return chart;
    }

    /// <summary>
    /// Wraps a Mermaid requirement document in a visual artifact envelope backed by a ChartForgeX topology chart.
    /// </summary>
    public static VisualArtifact ToVisualArtifact(this MermaidRequirementDocument document, MermaidTopologyRenderOptions? options = null) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        var topology = document.ToTopologyChart(options);
        var artifact = VisualArtifact.Create(topology.Id ?? "mermaid-requirement", VisualArtifactKind.Mermaid, topology);
        artifact.SourceLanguage = VisualArtifactSourceLanguage.Mermaid;
        artifact.Title = topology.Title ?? string.Empty;
        artifact.Subtitle = topology.Subtitle ?? string.Empty;
        artifact.NaturalSize = new VisualArtifactSize(topology.Viewport.Width, topology.Viewport.Height);
        artifact.ExportFormats = VisualArtifactExportFormat.Svg | VisualArtifactExportFormat.Png | VisualArtifactExportFormat.Html;
        artifact.Metadata["mermaid.kind"] = document.Kind.ToString();
        artifact.Metadata["mermaid.header"] = document.Header;
        artifact.Metadata["mermaid.requirements"] = document.Requirements.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.elements"] = document.Elements.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["mermaid.relationships"] = document.Relationships.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["render.model"] = nameof(TopologyChart);
        return artifact;
    }

    /// <summary>Renders a Mermaid requirement document to static SVG.</summary>
    public static string ToSvg(this MermaidRequirementDocument document, MermaidTopologyRenderOptions? options = null) => document.ToTopologyChart(options).ToSvg();

    /// <summary>Renders a Mermaid requirement document to static PNG.</summary>
    public static byte[] ToPng(this MermaidRequirementDocument document, MermaidTopologyRenderOptions? options = null) => document.ToTopologyChart(options).ToPng();

    private static string RequirementLabel(MermaidRequirementNode requirement) => string.IsNullOrWhiteSpace(requirement.RequirementId) ? requirement.Name : requirement.RequirementId!;

    private static string? RequirementSubtitle(MermaidRequirementNode requirement) {
        if (!string.IsNullOrWhiteSpace(requirement.Text)) return requirement.Text!;
        if (!string.IsNullOrWhiteSpace(requirement.VerifyMethod)) return requirement.VerifyMethod!;
        return requirement.RequirementType;
    }

    private static string? ElementSubtitle(MermaidRequirementElement element) {
        if (!string.IsNullOrWhiteSpace(element.DocumentReference)) return element.DocumentReference!;
        return string.IsNullOrWhiteSpace(element.ElementType) ? null : element.ElementType;
    }

    private static TopologyLayoutDirection ToLayoutDirection(string? direction) {
        if (string.Equals(direction, "LR", StringComparison.OrdinalIgnoreCase)) return TopologyLayoutDirection.LeftToRight;
        if (string.Equals(direction, "RL", StringComparison.OrdinalIgnoreCase)) return TopologyLayoutDirection.RightToLeft;
        if (string.Equals(direction, "BT", StringComparison.OrdinalIgnoreCase)) return TopologyLayoutDirection.BottomToTop;
        return TopologyLayoutDirection.TopToBottom;
    }

    private static TopologyHealthStatus RiskStatus(string? risk) {
        if (string.Equals(risk, "High", StringComparison.OrdinalIgnoreCase)) return TopologyHealthStatus.Critical;
        if (string.Equals(risk, "Medium", StringComparison.OrdinalIgnoreCase)) return TopologyHealthStatus.Warning;
        if (string.Equals(risk, "Low", StringComparison.OrdinalIgnoreCase)) return TopologyHealthStatus.Healthy;
        return TopologyHealthStatus.Unknown;
    }

    private static TopologyEdgeKind ToEdgeKind(string relationshipType) {
        if (string.Equals(relationshipType, "contains", StringComparison.OrdinalIgnoreCase)) return TopologyEdgeKind.Ownership;
        if (string.Equals(relationshipType, "satisfies", StringComparison.OrdinalIgnoreCase) || string.Equals(relationshipType, "verifies", StringComparison.OrdinalIgnoreCase)) return TopologyEdgeKind.Mapping;
        if (string.Equals(relationshipType, "copies", StringComparison.OrdinalIgnoreCase) || string.Equals(relationshipType, "derives", StringComparison.OrdinalIgnoreCase)) return TopologyEdgeKind.Dependency;
        return TopologyEdgeKind.Link;
    }

    private static string CssClass(string baseClass, IReadOnlyList<string> classes) => classes.Count == 0 ? baseClass : baseClass + " " + string.Join(" ", classes);
}
