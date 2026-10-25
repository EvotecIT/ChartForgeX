using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    private readonly Dictionary<TopologyEdge, TopologyPreparedEdgeGeometry> _numericRoutes = new();
    private readonly Dictionary<TopologyPreparedRouteBasis, bool> _routeBasisMatches = new();

    /// <summary>Resolves one ordinary numeric body and one owner tail before labels, bounds, fitting or crossing paint.</summary>
    private void PrepareInterpolatedRoutes() {
        foreach (var edge in _chart.Edges) {
            if (ReusableRouteGeometry(edge) is { } captured) { _numericRoutes.Add(edge, captured); continue; }
            if (!edge.Interpolation.HasValue) continue;
            var source = _routes[edge]; var body = Interpolate(edge, _paintRoutes[edge]);
            ChartPath? tail = null; int? ownerOrder = null;
            if (_trunks.TryGetValue(edge, out var trunk)) {
                ownerOrder = _chart.Edges.IndexOf(trunk.Owner);
                if (ReferenceEquals(trunk.Owner, edge)) tail = Interpolate(edge, trunk.Tail);
            }
            // Full paths are completed only after every source-order tail owner has been resolved.
            _numericRoutes.Add(edge, new TopologyPreparedEdgeGeometry(edge.Interpolation.Value, edge.InterpolationCornerBlendLength,
                source, body, ownerOrder.HasValue ? body : null, tail, _chart.Edges.IndexOf(edge), ownerOrder));
        }
        foreach (var edge in _chart.Edges) {
            if (!_numericRoutes.TryGetValue(edge, out var geometry)) continue;
            if (geometry.TrunkOwnerOrder is int ownerOrder) {
                var owner = _chart.Edges[ownerOrder];
                if (!_numericRoutes.TryGetValue(owner, out var ownerGeometry) || ownerGeometry.TailPath == null)
                    throw new InvalidOperationException("An interpolated trunk requires its same-policy source-order owner tail.");
                var tail = ownerGeometry.TailPath;
                if (!ReferenceEquals(geometry, ReusableRouteGeometry(edge)))
                    geometry = new TopologyPreparedEdgeGeometry(geometry.Interpolation, geometry.CornerBlendLength, geometry.Input,
                        TopologyInterpolationGeometry.JoinBodyAndTail(geometry.BodyPath!, tail), geometry.BodyPath, geometry.TailPath, geometry.Order, ownerOrder);
                _trunks[edge] = (owner, tail.Flatten(maximumPoints: 10000));
            } else _trunks.Remove(edge);
            _numericRoutes[edge] = geometry;
            _routes[edge] = geometry.Interpolation.HasValue ? geometry.FullPath.Flatten(maximumPoints: 10000) : geometry.Input;
            _paintRoutes[edge] = (geometry.BodyPath ?? geometry.FullPath).Flatten(maximumPoints: 10000);
            TopologyRenderRouteCache.Register(_chart, edge, geometry);
        }
    }

    /// <summary>Admits the exact immutable observation before route search can reinterpret a fitted scene.</summary>
    private bool SeedCapturedRoute(TopologyEdge edge) {
        if (ReusableRouteGeometry(edge) is not { } geometry) return false;
        _routes.Add(edge, geometry.Interpolation.HasValue ? geometry.FullPath.Flatten(maximumPoints: 10000) : geometry.Input);
        _paintRoutes.Add(edge, (geometry.BodyPath ?? geometry.FullPath).Flatten(maximumPoints: 10000));
        if (geometry.TrunkOwnerOrder is int order) {
            var owner = _chart.Edges[order]; var tail = ReusableRouteGeometry(owner)?.TailPath;
            if (tail == null) throw new InvalidOperationException("An admitted captured trunk requires its validated owner tail.");
            _trunks.Add(edge, (owner, tail.Flatten(maximumPoints: 10000)));
        }
        return true;
    }

    private TopologyPreparedEdgeGeometry? ReusableRouteGeometry(TopologyEdge edge) {
        var plan = edge.PreparedEdgePlan;
        if (plan?.Geometry == null || plan.Basis == null) return null;
        if (!_routeBasisMatches.TryGetValue(plan.Basis, out var matches))
            _routeBasisMatches.Add(plan.Basis, matches = plan.Basis.Matches(_source, _options, _context));
        return matches ? plan.Geometry : null;
    }
    private static ChartPath Interpolate(TopologyEdge edge, IReadOnlyList<ChartPoint> points) =>
        TopologyInterpolationGeometry.Build(points, edge.Interpolation!.Value, edge.InterpolationCornerBlendLength, TopologyPreparedEdgePlan.Units(edge));


    private ChartPath? OrdinaryLogicalPath(TopologyEdge edge, bool trunkTail = false) =>
        _numericRoutes.TryGetValue(edge, out var geometry) ? trunkTail ? geometry.TailPath : geometry.BodyPath ?? geometry.FullPath : edge.PathGeometry?.Capture();
}
