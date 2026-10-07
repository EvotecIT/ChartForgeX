using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

/// <summary>Shares immutable route calculations across native marks and the label scene within one prepared render.</summary>
internal sealed class TopologyRenderRouteCache : IDisposable {
    [ThreadStatic] private static TopologyRenderRouteCache? Current;
    private readonly TopologyRenderRouteCache? _previous;
    private readonly TopologyChart _chart;
    private readonly Dictionary<TopologyEdge, List<ChartPoint>> _routes;
    internal TopologyRenderRouteCache(TopologyChart chart) {
        _previous = Current; _chart = chart;
        _routes = _previous?._chart == chart ? _previous._routes : new Dictionary<TopologyEdge, List<ChartPoint>>();
        Current = this;
    }
    internal static List<ChartPoint> Resolve(TopologyChart chart, TopologyEdge edge, Func<List<ChartPoint>> calculate) {
        if (Current == null || Current._chart != chart) return calculate();
        if (!Current._routes.TryGetValue(edge, out var route)) Current._routes[edge] = route = calculate();
        // Keep the internal caller's mutable-list contract without sharing mutations.
        return new List<ChartPoint>(route);
    }
    public void Dispose() { Current = _previous; }
}
