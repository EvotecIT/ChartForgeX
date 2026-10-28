using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Topology;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeValidation {
    private static void ValidateTopologyPaths(VisualArtifactInterchangeEnvelope envelope, ISet<string> entityIds) {
        var paths = envelope.Topology?.PathDecorations;
        var edgePaths = envelope.Edges.Where(edge => edge.Topology?.PathGeometry != null).ToArray();
        if ((paths == null || paths.Count == 0) && edgePaths.Length == 0 && !envelope.Edges.Any(edge => edge.Topology?.PreparedEdgePlan?.Geometry != null)) return;
        Count((paths?.Count ?? 0) + edgePaths.Length, TopologyPathDecoration.MaximumPaths, "topology path decorations");
        var pathCount = (paths?.Count ?? 0) + edgePaths.Length;
        var total = 0;
        foreach (var path in paths ?? new System.Collections.Generic.List<TopologyPathDecoration>()) {
            if (path == null) throw new ArgumentException("Topology path decorations cannot be null.", nameof(envelope));
            RequiredId(path.Id, "path decoration id");
            if (path.Id == envelope.Id) throw new ArgumentException("Path decoration ids must differ from the artifact id.", nameof(envelope));
            UniqueEntityId(entityIds, path.Id, "path decoration", envelope);
            total += path.Commands.Count;
            if (total > TopologyPathDecoration.MaximumTotalCommands) throw new ArgumentException("Topology path decorations exceed 65536 commands.", nameof(envelope));
            // The immutable wrapper has already checked finite bounded coordinates, exact arity, subpaths and paint lengths.
        }
        var routePoints = 0;
        foreach (var edge in edgePaths) {
            var native = edge.Topology!; native.PathGeometry!.ValidateEdge();
            if (native.Waypoints.Count != 0 || native.RouteLane.HasValue || native.SourcePort != TopologyEdgePort.Auto || native.TargetPort != TopologyEdgePort.Auto ||
                !string.IsNullOrWhiteSpace(edge.SourcePortId) || !string.IsNullOrWhiteSpace(edge.TargetPortId))
                throw new ArgumentException("Authored edge paths cannot also carry waypoints, route-lane or port overrides.", nameof(envelope));
            if (native.Interpolation.HasValue) throw new ArgumentException("Authored PathGeometry cannot also select native interpolation.", nameof(envelope));
            total += native.PathGeometry.Commands.Count;
            if (total > TopologyPathDecoration.MaximumTotalCommands) throw new ArgumentException("Topology paths exceed 65536 commands.", nameof(envelope));
            routePoints += native.PathGeometry.RoutePoints().Count;
            if (routePoints > 65536) throw new ArgumentException("Authored edge paths exceed 65536 flattened route points.", nameof(envelope));
        }
        ValidateInterpolatedGeometry(envelope, ref pathCount, ref total, ref routePoints);
    }
}
