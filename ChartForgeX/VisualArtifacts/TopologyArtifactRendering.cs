using System;
using System.Globalization;
using ChartForgeX.Primitives;
using ChartForgeX.Topology;

namespace ChartForgeX.VisualArtifacts;

/// <summary>Provides product-neutral visual artifact wrappers for topology diagrams.</summary>
public static class TopologyArtifactRendering {
    /// <summary>Wraps a topology diagram in a host-inspectable visual artifact envelope.</summary>
    public static VisualArtifact ToVisualArtifact(this TopologyChart topology, VisualArtifactSourceLanguage sourceLanguage = VisualArtifactSourceLanguage.Native) {
        if (topology == null) throw new ArgumentNullException(nameof(topology));
        var id = string.IsNullOrWhiteSpace(topology.Id) ? "topology" : topology.Id!.Trim();
        var artifact = VisualArtifact.Create(id, VisualArtifactKind.Topology, topology);
        artifact.SourceLanguage = sourceLanguage;
        artifact.Title = topology.Title ?? string.Empty;
        artifact.Subtitle = topology.Subtitle ?? string.Empty;
        artifact.NaturalSize = new VisualArtifactSize(topology.Viewport.Width, topology.Viewport.Height);
        artifact.ExportFormats = VisualArtifactExportFormat.Svg | VisualArtifactExportFormat.Png | VisualArtifactExportFormat.Html | VisualArtifactExportFormat.Office;
        artifact.Metadata["render.model"] = nameof(TopologyChart);
        artifact.Metadata["topology.layout"] = topology.LayoutMode.ToString();
        artifact.Metadata["topology.nodes"] = topology.Nodes.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["topology.edges"] = topology.Edges.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Accessibility.Name = topology.Accessibility.Name;
        artifact.Accessibility.Description = topology.Accessibility.Description;
        artifact.Accessibility.Language = topology.Accessibility.Language;
        artifact.Accessibility.IsDecorative = topology.Accessibility.IsDecorative;

        foreach (var group in topology.Groups) artifact.Regions.Add(Region(group.Id, "topology-group", group.Label, group.X, group.Y, group.Width, group.Height, group.Href, group.Tooltip));
        foreach (var node in topology.Nodes) artifact.Regions.Add(Region(node.Id, "topology-node", node.Label, node.X, node.Y, node.Width, node.Height, node.Href, node.Tooltip));
        foreach (var edge in topology.Edges) {
            var region = new VisualArtifactRegion {
                Id = edge.Id,
                Kind = "topology-edge",
                Label = string.IsNullOrWhiteSpace(edge.Label) ? edge.SourceNodeId + " to " + edge.TargetNodeId : edge.Label!,
                Href = edge.Href,
                AlternativeText = edge.Tooltip
            };
            region.Metadata["source"] = edge.SourceNodeId;
            region.Metadata["target"] = edge.TargetNodeId;
            artifact.Regions.Add(region);
        }
        return artifact;
    }

    private static VisualArtifactRegion Region(string id, string kind, string label, double x, double y, double width, double height, string? href, string? alternativeText) {
        return new VisualArtifactRegion {
            Id = id,
            Kind = kind,
            Label = label,
            Bounds = width > 0 && height > 0 ? new ChartRect(x, y, width, height) : null,
            Href = href,
            AlternativeText = alternativeText
        };
    }
}
