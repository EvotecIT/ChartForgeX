namespace ChartForgeX.Topology;

public sealed partial class TopologyChartValidator {
    internal TopologyValidationResult ValidateInterpolationInputs(TopologyChart chart) {
        var result = new TopologyValidationResult();
        foreach (var edge in chart.Edges) ValidateInterpolationPolicy(edge, result);
        return result;
    }
    private static void ValidateInterpolationPolicy(TopologyEdge edge, TopologyValidationResult result) {
        if (edge.Interpolation.HasValue && edge.PathGeometry != null)
            Add(result, "edge-interpolation-path-conflict", "Authored PathGeometry cannot also select native interpolation.", edge.Id);
        if (edge.InterpolationCornerBlendLength.HasValue && !edge.Interpolation.HasValue)
            Add(result, "edge-interpolation-blend-policy", "InterpolationCornerBlendLength requires an explicit Interpolation choice.", edge.Id);
    }
}
