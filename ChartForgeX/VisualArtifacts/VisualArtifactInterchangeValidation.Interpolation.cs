using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeValidation {
    private static void ValidateInterpolationPolicy(TopologyEdgeInterpolation? interpolation, double? blend, TopologyPathGeometry? authored) {
        if (interpolation.HasValue) TopologyModelGuards.EnumDefined(interpolation.Value, nameof(interpolation));
        if (blend.HasValue) { TopologyModelGuards.NonNegativeFinite(blend.Value, nameof(blend)); BoundedPreparedEdgeCoordinate(blend.Value); }
        if (interpolation.HasValue && authored != null) throw new ArgumentException("Authored PathGeometry cannot also select native interpolation.");
        if (blend.HasValue && !interpolation.HasValue) throw new ArgumentException("InterpolationCornerBlendLength requires an explicit Interpolation choice.");
    }

    /// <summary>Validates finite captured paint facts through the same path, endpoint and aggregate budget owners.</summary>
    private static void ValidateInterpolatedGeometry(VisualArtifactInterchangeEnvelope envelope, ref int existingPathCount, ref int existingCommands, ref int existingVertices) {
        var pathCount = existingPathCount; var commands = existingCommands; var vertices = existingVertices;
        var ordered = envelope.Edges.Where(edge => edge.Topology?.PreparedEdgePlan?.Geometry != null).ToArray();
        var orders = new Dictionary<int, VisualArtifactInterchangeEdge>();
        foreach (var edge in ordered) {
            if (orders.ContainsKey(edge.Order)) throw new ArgumentException("Captured interpolation requires unique source edge orders.");
            orders.Add(edge.Order, edge);
        }
        foreach (var edge in ordered) {
            var native = edge.Topology!; var geometry = native.PreparedEdgePlan!.Geometry!;
            if (native.Interpolation != geometry.Interpolation || native.InterpolationCornerBlendLength != geometry.CornerBlendLength)
                throw new ArgumentException("Captured numeric path policy must equal the authored interpolation and corner blend.");
            if (geometry.Order != edge.Order || geometry.Order < 0 || geometry.Order >= envelope.Edges.Count)
                throw new ArgumentException("Captured numeric paths must retain their exact source collection order.");
            if (geometry.Input.Count < 2 || geometry.Input.Count > 10000) throw new ArgumentException("Captured interpolation requires 2 through 10000 input vertices.");
            vertices = checked(vertices + geometry.Input.Count);
            foreach (var point in geometry.Input) { BoundedPreparedEdgeCoordinate(point.X); BoundedPreparedEdgeCoordinate(point.Y);
                if (point.BreakBefore) throw new ArgumentException("Captured interpolation requires continuous input vertices."); }
            var full = geometry.FullPath;
            Same(geometry.Input[0], Start(full), "Captured path start must match the first resolved input vertex.");
            Same(geometry.Input[geometry.Input.Count - 1], End(full), "Captured path end must match the final resolved input vertex.");
            if ((geometry.Interpolation is null or TopologyEdgeInterpolation.Linear or TopologyEdgeInterpolation.Step or TopologyEdgeInterpolation.StepAfter or TopologyEdgeInterpolation.StepBefore)
                && full.Commands.Any(command => command.Kind == ChartPathCommandKind.CubicTo))
                throw new ArgumentException("Linear and stepped interpolation must retain continuous M/L numeric paint.");
            AddPath(full);
            if (geometry.TrunkOwnerOrder is int ownerOrder) {
                if (!orders.TryGetValue(ownerOrder, out var owner) || owner.Topology!.PreparedEdgePlan!.Geometry is not { } ownerGeometry
                    || ownerGeometry.TrunkOwnerOrder != ownerOrder || ownerGeometry.TailPath == null || geometry.BodyPath == null)
                    throw new ArgumentException("A captured trunk requires its same-policy owner tail and distinct body path.");
                if (ownerGeometry.Interpolation != geometry.Interpolation || ownerGeometry.CornerBlendLength != geometry.CornerBlendLength)
                    throw new ArgumentException("Shared owner tails must retain one interpolation policy.");
                var body = geometry.BodyPath; var tail = ownerGeometry.TailPath;
                Same(End(body), Start(tail), "Captured body and shared owner tail must meet exactly.");
                var composed = body.Commands.Concat(tail.Commands.Skip(1)).ToArray();
                if (!SameCommands(full.Commands, composed)) throw new ArgumentException("A captured interpolated full path must be its exact body plus the shared owner tail.");
                AddPath(body);
                if (geometry.TailPath != null) {
                    if (ownerOrder != edge.Order) throw new ArgumentException("Only the source-order trunk owner carries a tail path.");
                    AddPath(geometry.TailPath);
                }
            } else if (geometry.BodyPath != null || geometry.TailPath != null)
                throw new ArgumentException("A captured standalone path cannot carry shared trunk parts.");
            if (edge.ResolvedRoute.Count < 2) throw new ArgumentException("Captured interpolation requires resolved route observations.");
            Same(Start(full), new ChartPoint(edge.ResolvedRoute[0].X, edge.ResolvedRoute[0].Y), "Resolved observations must retain the captured start.");
            var last = edge.ResolvedRoute[edge.ResolvedRoute.Count - 1]; Same(End(full), new ChartPoint(last.X, last.Y), "Resolved observations must retain the captured end.");
            if (geometry.MainLabel is { } main) { Rect(main.Bounds); Point(main.Anchor); }
            Endpoint(geometry.SourceLabel); Endpoint(geometry.TargetLabel);
            if (pathCount > TopologyPathDecoration.MaximumPaths || commands > TopologyPathDecoration.MaximumTotalCommands || vertices > 65536)
                throw new ArgumentException("Topology numeric paths exceed 1024 paths, 65536 commands or 65536 aggregate input/flattened vertices.");
        }

        existingPathCount = pathCount; existingCommands = commands; existingVertices = vertices;

        void AddPath(ChartPath path) {
            var checkedPath = TopologyPathGeometry.FromNativePath(path); checkedPath.ValidateEdge();
            pathCount = checked(pathCount + 1); commands = checked(commands + path.Commands.Count);
            vertices = checked(vertices + checkedPath.RoutePoints().Count);
            // The authored kind remains separate from numeric output. Linear/step algorithms may never smuggle cubic commands.
        }
        void Point(ChartPoint point) { BoundedPreparedEdgeCoordinate(point.X); BoundedPreparedEdgeCoordinate(point.Y); }
        void Rect(ChartRect bounds) { BoundedPreparedEdgeCoordinate(bounds.X); BoundedPreparedEdgeCoordinate(bounds.Y);
            if (bounds.Width < 0 || bounds.Height < 0) throw new ArgumentException("Captured caption allocations must be nonnegative.");
            BoundedPreparedEdgeCoordinate(bounds.Width); BoundedPreparedEdgeCoordinate(bounds.Height); BoundedPreparedEdgeCoordinate(bounds.Right); BoundedPreparedEdgeCoordinate(bounds.Bottom); }
        void Endpoint(TopologyPreparedEndpointLabel? label) { if (label == null) return; Rect(label.Bounds);
            TopologyModelGuards.PositiveFinite(label.FontSize, nameof(label.FontSize)); BoundedPreparedEdgeCoordinate(label.FontSize); }
    }
    private static ChartPoint Start(ChartPath path) => new(path.Commands[0].X, path.Commands[0].Y);
    private static ChartPoint End(ChartPath path) => new(path.Commands[path.Commands.Count - 1].X, path.Commands[path.Commands.Count - 1].Y);
    private static void Same(ChartPoint left, ChartPoint right, string message) { if (left.X != right.X || left.Y != right.Y) throw new ArgumentException(message); }
    private static bool SameCommands(IReadOnlyList<ChartPathCommand> left, IReadOnlyList<ChartPathCommand> right) {
        if (left.Count != right.Count) return false;
        for (var i = 0; i < left.Count; i++) if (left[i].Kind != right[i].Kind || left[i].X != right[i].X || left[i].Y != right[i].Y
            || left[i].Control1X != right[i].Control1X || left[i].Control1Y != right[i].Control1Y || left[i].Control2X != right[i].Control2X || left[i].Control2Y != right[i].Control2Y) return false;
        return true;
    }
}
