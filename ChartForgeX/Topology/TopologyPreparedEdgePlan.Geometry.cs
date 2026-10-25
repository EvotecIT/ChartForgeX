using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal sealed partial class TopologyPreparedEdgePlan {
    private TopologyPreparedEdgePlan(double defaultUnits, TopologyPreparedEdgeGeometry? geometry, TopologyPreparedRouteBasis? basis,
        bool selectedInput, bool highlightedInput) : this(defaultUnits) {
        Geometry = geometry; Basis = basis; SelectedInput = selectedInput; HighlightedInput = highlightedInput;
    }
    internal TopologyPreparedEdgeGeometry? Geometry { get; }
    internal TopologyPreparedRouteBasis? Basis { get; }
    internal bool SelectedInput { get; }
    internal bool HighlightedInput { get; } = true;
    internal static TopologyPreparedEdgePlan Restore(double units, TopologyPreparedEdgeGeometry? geometry, bool selected, bool highlighted) =>
        new(units, geometry, null, selected, highlighted);
    internal TopologyPreparedEdgePlan WithBasis(TopologyPreparedRouteBasis basis) => new(DefaultUnits, Geometry, basis, SelectedInput, HighlightedInput);
    internal TopologyPreparedEdgePlan WithoutGeometry() => new(DefaultUnits, null, null, SelectedInput, HighlightedInput);
    internal static TopologyPreparedEdgePlan? CaptureResolved(TopologyEdge edge, double scale, TopologyRenderOptions options,
        TopologyHighlightState highlight, bool interpolatedGraph, TopologyPreparedEdgeGeometry? geometry) {
        var selected = options.SelectedEdgeIds.Contains(edge.Id); var highlighted = highlight.IsEdgeHighlighted(edge);
        if (geometry != null) return new TopologyPreparedEdgePlan(Units(edge) * scale, geometry, null, selected, highlighted);
        var ordinary = Capture(edge, scale, options);
        if (!interpolatedGraph || !selected && highlighted) return ordinary;
        return new TopologyPreparedEdgePlan(ordinary?.DefaultUnits ?? 1, null, null, selected, highlighted);
    }
}
