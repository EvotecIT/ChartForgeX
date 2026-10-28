using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Topology;

/// <summary>Shares immutable route calculations across native marks and the label scene within one prepared render.</summary>
internal sealed class TopologyRenderRouteCache : IDisposable {
    [ThreadStatic] private static TopologyRenderRouteCache? Current;
    private readonly TopologyRenderRouteCache? _previous;
    private readonly TopologyChart _chart;
    private readonly Dictionary<TopologyEdge, List<ChartPoint>> _routes;
    private readonly Dictionary<TopologyEdge, TopologyPreparedEdgeGeometry> _geometry;
    internal TopologyRenderRouteCache(TopologyChart chart) {
        _previous = Current; _chart = chart;
        _routes = _previous?._chart == chart ? _previous._routes : new Dictionary<TopologyEdge, List<ChartPoint>>();
        _geometry = _previous?._chart == chart ? _previous._geometry : new Dictionary<TopologyEdge, TopologyPreparedEdgeGeometry>();
        Current = this;
    }
    internal static List<ChartPoint> Resolve(TopologyChart chart, TopologyEdge edge, Func<List<ChartPoint>> calculate) {
        if (Current == null || Current._chart != chart) return calculate();
        if (!Current._routes.TryGetValue(edge, out var route)) Current._routes[edge] = route = calculate();
        // Keep the internal caller's mutable-list contract without sharing mutations.
        return new List<ChartPoint>(route);
    }
    internal static void Register(TopologyChart chart, TopologyEdge edge, TopologyPreparedEdgeGeometry geometry) {
        if (Current == null || Current._chart != chart) throw new InvalidOperationException("Numeric topology paths require their existing prepared route scope.");
        Current._geometry[edge] = geometry;
        if (!geometry.Interpolation.HasValue) Current._routes[edge] = new List<ChartPoint>(geometry.Input);
    }
    internal static TopologyPreparedEdgeGeometry? Geometry(TopologyChart chart, TopologyEdge edge) =>
        Current != null && Current._chart == chart && Current._geometry.TryGetValue(edge, out var geometry) ? geometry : null;
    internal static ChartPath? FullPath(TopologyChart chart, TopologyEdge edge) => Geometry(chart, edge)?.FullPath;
    public void Dispose() { Current = _previous; }
}
