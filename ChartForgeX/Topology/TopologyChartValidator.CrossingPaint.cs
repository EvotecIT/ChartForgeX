using System.Linq;

namespace ChartForgeX.Topology;

public sealed partial class TopologyChartValidator {
    private static void ValidateCrossingPaint(TopologyChart chart, TopologyValidationResult result) {
        var policy = chart.CrossingPaint;
        if (policy == null || policy.CurveHandling == TopologyCrossingCurveHandling.SkipUnsupported) return;
        foreach (var edge in chart.Edges) {
            // Authored cubics are already final native paint. Interpolation eligibility is decided
            // after resolution by the compiler's one actual-path owner, never guessed from enum names.
            if (edge.PathGeometry?.Commands.Any(command => command.Command == 'C') == true)
                Add(result, "topology-crossing-curve", "Orthogonal crossing paint cannot rewrite unsupported final native curves. Select SkipUnsupported to retain and exclude those paths.", edge.Id);
        }
    }
}
