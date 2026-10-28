using System;
using ChartForgeX.Primitives;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

public sealed partial class TopologyEdge {
    internal TopologyPreparedEdgePlan? PreparedEdgePlan { get; set; }
}

/// <summary>One immutable prepared-edge owner for retained default appearance units and captured route facts.</summary>
/// <remarks>Manual geometry edits retain appearance units; automatic layout or leaving prepared coordinates resets them.
/// Explicit authored output-unit fields remain authoritative. Numeric route reuse has its own exact graph guard.</remarks>
internal sealed partial class TopologyPreparedEdgePlan {
    internal TopologyPreparedEdgePlan(double defaultUnits) {
        TopologyModelGuards.PositiveFinite(defaultUnits, nameof(defaultUnits)); DefaultUnits = defaultUnits;
    }
    internal double DefaultUnits { get; }
    internal static double Units(TopologyEdge edge) => edge.PreparedEdgePlan?.DefaultUnits ?? 1;
    internal static TopologyPreparedEdgePlan? Capture(TopologyEdge edge, double scale, TopologyRenderOptions options) {
        var units = Units(edge) * scale;
        if (units == 1 || !UsesDefaultAppearance(edge, options)) return null;
        return new TopologyPreparedEdgePlan(units);
    }
    internal static void ApplyLifecycle(TopologyChart chart) {
        if (chart.UsesPreparedSceneCoordinates && chart.LayoutMode == TopologyLayoutMode.Manual) return;
        foreach (var edge in chart.Edges) edge.PreparedEdgePlan = null;
    }
    internal void ValidatePortable() {
        // A static scene may paint a larger clipped unit basis. The explicit portable boundary remains bounded.
        TopologyModelGuards.PositiveFinite(DefaultUnits, nameof(DefaultUnits));
        if (DefaultUnits > TopologyPathGeometry.MaximumCoordinate)
            throw new ArgumentOutOfRangeException(nameof(DefaultUnits), "Captured edge units exceed the portable coordinate limit.");
    }
    private static bool UsesDefaultAppearance(TopologyEdge edge, TopologyRenderOptions options) {
        if (edge.StrokeWidth == null || edge.DashPattern.Count == 0 && EffectiveEdgePngDashArray(edge) != null) return true;
        if (RenderedSourceMarker(edge, options.IncludeDirectionMarkers) != TopologyMarkerKind.None && edge.SourceMarkerStyle == null
            || RenderedTargetMarker(edge, options.IncludeDirectionMarkers) != TopologyMarkerKind.None && edge.TargetMarkerStyle == null) return true;
        var style = EdgeVisualStyle(edge, options.SelectedEdgeIds.Contains(edge.Id), options);
        if (style.AmbientHaloOpacity > 0 && style.AmbientHaloStrokeExtra > 0
            || style.HaloOpacity > 0 && style.HaloStrokeExtra > 0 || style.HighlightOpacity > 0) return true;
        return options.IncludeEdgeLabels && (!string.IsNullOrWhiteSpace(EdgeLabel(edge, options.EdgeLabelMetricKey, edge.Label))
                || !string.IsNullOrWhiteSpace(EdgeLabel(edge, options.EdgeSecondaryLabelMetricKey, edge.SecondaryLabel))
                || !string.IsNullOrWhiteSpace(EdgeLabel(edge, options.EdgeTertiaryLabelMetricKey, edge.TertiaryLabel)))
            || options.IncludeEndpointLabels && (!string.IsNullOrWhiteSpace(edge.SourceLabel) || !string.IsNullOrWhiteSpace(edge.TargetLabel));
    }
}
