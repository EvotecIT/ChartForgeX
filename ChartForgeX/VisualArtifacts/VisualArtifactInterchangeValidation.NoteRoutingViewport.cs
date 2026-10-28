using System;
using ChartForgeX.Topology;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeValidation {
    private static void ValidateNoteRoutingViewport(VisualArtifactInterchangeAnnotation note, VisualArtifactInterchangeEnvelope envelope) {
        if (note.TopologyRoutingViewport is not {} value) return;
        if (envelope.Family != VisualArtifactInterchangeFamily.Topology || note.Kind != "TopologyNote" || note.TargetIds.Count != 1
            || note.TopologyDefaultUnits == null || note.TopologyDefaultUnits == 1)
            throw new ArgumentException("A captured note routing viewport requires an attached nonunit native TopologyNote.");
        foreach (var coordinate in new[] { value.X, value.Y, value.Width, value.Height, value.Right, value.Bottom })
            if (double.IsNaN(coordinate) || double.IsInfinity(coordinate) || Math.Abs(coordinate) > TopologyPathGeometry.MaximumCoordinate)
                throw new ArgumentException("Captured note routing viewports require finite bounded coordinates and extents.");
        if (value.Width < 0 || value.Height < 0) throw new ArgumentException("Captured note routing viewports require nonnegative extents; zero retains a legacy constant clamp domain.");
    }
}
