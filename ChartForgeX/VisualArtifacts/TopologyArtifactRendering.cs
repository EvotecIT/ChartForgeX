using System;
using System.Globalization;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Topology;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

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
        artifact.ExportFormats = VisualArtifactExportFormat.Svg | VisualArtifactExportFormat.Png | VisualArtifactExportFormat.Html | VisualArtifactExportFormat.Json | VisualArtifactExportFormat.Office;
        artifact.Metadata["render.model"] = nameof(TopologyChart);
        artifact.Metadata["topology.layout"] = topology.LayoutMode.ToString();
        artifact.Metadata["topology.nodes"] = topology.Nodes.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Metadata["topology.edges"] = topology.Edges.Count.ToString(CultureInfo.InvariantCulture);
        artifact.Accessibility.Name = topology.Accessibility.Name;
        artifact.Accessibility.Description = topology.Accessibility.Description;
        artifact.Accessibility.Language = topology.Accessibility.Language;
        artifact.Accessibility.IsDecorative = topology.Accessibility.IsDecorative;
        artifact.ModelAccessibilitySnapshot.Name = topology.Accessibility.Name;
        artifact.ModelAccessibilitySnapshot.Description = topology.Accessibility.Description;
        artifact.ModelAccessibilitySnapshot.Language = topology.Accessibility.Language;
        artifact.ModelAccessibilitySnapshot.IsDecorative = topology.Accessibility.IsDecorative;
        artifact.HasModelAccessibilitySnapshot = true;

        var prepared = RefreshRegions(artifact, topology, null);
        artifact.NaturalSize = new VisualArtifactSize(prepared.Width, prepared.Height);
        artifact.TopologyNaturalSizeSnapshot = artifact.NaturalSize;
        return artifact;
    }

    internal static PreparedTopology RefreshRegions(VisualArtifact artifact, TopologyChart topology, TopologyRenderOptions? renderOptions) {
        var prepared = topology.Prepare(renderOptions);
        RefreshRegions(artifact, prepared);
        return prepared;
    }

    internal static void RefreshRegions(VisualArtifact artifact, PreparedTopology prepared) {
        var semantic = prepared.ToInterchangeEnvelope();
        artifact.Regions.Clear();
        foreach (var source in prepared.Visual.Regions) {
            var node = semantic.Nodes.FirstOrDefault(item => item.Id == source.Id);
            var group = semantic.Groups.FirstOrDefault(item => item.Id == source.Id);
            var edge = semantic.Edges.FirstOrDefault(item => item.Id == source.Id);
            var region = new VisualArtifactRegion {
                Id = source.Id, Kind = source.Role, Label = source.Label ?? string.Empty, Bounds = source.Bounds,
                Href = SafeHref(node?.Href ?? group?.Href ?? edge?.Href),
                AlternativeText = node?.Tooltip ?? group?.Tooltip ?? edge?.Tooltip ?? source.Label
            };
            if (edge != null) { region.Metadata["source"] = edge.SourceId; region.Metadata["target"] = edge.TargetId; }
            artifact.Regions.Add(region);
        }
    }
}
