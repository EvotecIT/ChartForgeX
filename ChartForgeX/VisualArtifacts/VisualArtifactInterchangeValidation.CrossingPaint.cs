using System;
using System.Linq;
using ChartForgeX.Topology;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeValidation {
    private static void ValidateTopologyCrossingPaint(VisualArtifactInterchangeEnvelope envelope) {
        var policy = envelope.Topology!.CrossingPaint;
        if (policy == null) return;
        // Immutable policy construction validates style, radii and prepared units, including derived extents.
        if (policy.CurveHandling == TopologyCrossingCurveHandling.Reject && envelope.Edges.Any(edge =>
            edge.Topology?.PathGeometry?.Commands.Any(command => command.Command == 'C') == true))
            throw new ArgumentException("Orthogonal crossing paint cannot rewrite authored cubic edge paths; select SkipUnsupported to retain them unchanged.", nameof(envelope));
    }
}
