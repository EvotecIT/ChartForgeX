using System;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Topology;

/// <summary>Constrains the finite table slot without changing ordinary node presentation.</summary>
internal static class TopologyTableContentRules {
    internal static bool Supported(TopologyNodeKind kind, TopologyNodeShape? shape, TopologyNodeDisplayMode mode, bool glyph, int details) =>
        kind == TopologyNodeKind.Generic && (shape is null or TopologyNodeShape.Rectangle or TopologyNodeShape.Rounded)
        && (mode is TopologyNodeDisplayMode.Card or TopologyNodeDisplayMode.CompactCard or TopologyNodeDisplayMode.Hidden) && !glyph && details == 0;
    internal static string? Error(TopologyNode node, TopologyNodeDisplayMode mode) {
        if (node.TableContent == null) return null;
        if (!Supported(node.Kind, node.Shape, mode, TopologyRenderPrimitives.HasExplicitIconShape(node.IconShape) || !string.IsNullOrWhiteSpace(node.Symbol) || !string.IsNullOrWhiteSpace(node.IconId) || node.Artwork != null, node.Details.Count))
            return "Embedded tables require Generic Card/CompactCard rectangular or rounded nodes without inline glyph/artwork or detail rows; label, subtitle and footer remain supported.";
        try { VisualArtifactInterchangeMapping.MapTableContent(node.TableContent); }
        catch (Exception error) when (error is ArgumentException or NotSupportedException) { return error.Message; }
        return null;
    }
}
