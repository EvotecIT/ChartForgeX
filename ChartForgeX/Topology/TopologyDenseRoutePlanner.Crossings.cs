using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyDenseRoutePlanner {
    private const int MaximumReroutes = 64;

    // A different search can leave a tight corridor whose lane pass cannot separate every route. Repair only that
    // observed residual, keeping the complete before/after geometry so a local reroute cannot worsen another pair.
    private static void RepairLaneOverlaps(Scene scene, List<Request> requests, List<PlannedRoute> routes,
        List<List<ChartPoint>> fixedRoutes, Dictionary<(TopologyNode Node, TopologyEdgePort Side), int> sideUse, GridBuffers buffers) {
        var attempts = 0;
        foreach (var route in routes.OrderByDescending(item => ResidualOverlap(item, routes, fixedRoutes)).ThenBy(item => item.Request.Order)) {
            if (attempts >= 8) break;
            if (ResidualOverlap(route, routes, fixedRoutes) < 6) continue;
            attempts++;
            var originals = routes.Select(item => new List<ChartPoint>(item.Points)).ToList();
            var before = TotalInteraction(routes, fixedRoutes);
            var occupied = fixedRoutes.Concat(routes.Where(item => !ReferenceEquals(item, route)).Select(item => item.Points)).ToList();
            var grid = Grid.Create(scene, requests, occupied, buffers);
            if (grid == null) continue;
            foreach (var points in occupied) grid.Record(points, 1);
            var candidate = Search(grid, route.Request, sideUse, sharedRunCost: 4);
            if (candidate == null || TouchesObstacle(scene, candidate)) continue;
            route.Points = candidate.Points;
            SeparateLanes(scene, routes, fixedRoutes);
            var after = TotalInteraction(routes, fixedRoutes);
            // A few explicit crossings are easier to follow than a long overdrawn relationship. Keep that exchange
            // bounded: clear at least 6px of ambiguous corridor and introduce at most four crossings in a repair.
            if (after.Shared + 6 <= before.Shared && after.Crossings <= before.Crossings + 4) continue;
            for (var i = 0; i < routes.Count; i++) routes[i].Points = originals[i];
        }
    }

    private static double ResidualOverlap(PlannedRoute route, List<PlannedRoute> routes, List<List<ChartPoint>> fixedRoutes) =>
        routes.Where(item => !ReferenceEquals(item, route)).Select(item => Interaction(route.Points, item.Points).Shared)
            .Concat(fixedRoutes.Select(points => Interaction(route.Points, points).Shared)).Where(length => length >= 6).Sum();

    private static (int Crossings, double Shared) TotalInteraction(List<PlannedRoute> routes, List<List<ChartPoint>> fixedRoutes) {
        var crossings = 0;
        var shared = 0.0;
        for (var i = 0; i < routes.Count; i++) {
            for (var j = i + 1; j < routes.Count; j++) {
                var result = Interaction(routes[i].Points, routes[j].Points);
                crossings += result.Crossings;
                shared += result.Shared;
            }
            foreach (var fixedRoute in fixedRoutes) {
                var result = Interaction(routes[i].Points, fixedRoute);
                crossings += result.Crossings;
                shared += result.Shared;
            }
        }
        return (crossings, shared);
    }

    private static PlannedRoute? FindRoute(Scene scene, Grid? grid, Request request,
        Dictionary<(TopologyNode Node, TopologyEdgePort Side), int> sideUse, List<PlannedRoute> planned, List<List<ChartPoint>> fixedRoutes) {
        var direct = Direct(scene, request, sideUse);
        if (grid == null || direct != null && Interaction(direct.Points, planned, fixedRoutes, null).Cost < 0.01) return direct;
        var searched = Search(grid, request, sideUse);
        if (searched == null) return direct;
        return direct != null && RouteCost(direct.Points, planned, fixedRoutes, null) <= RouteCost(searched.Points, planned, fixedRoutes, null) ? direct : searched;
    }

    /// <summary>
    /// Reconsiders crossing routes against the complete plan, with a fixed attempt budget. A replacement must reduce
    /// crossings, retain obstacle clearance and avoid increasing shared corridor length or excessive detours.
    /// </summary>
    private static void ImproveCrossings(Scene scene, Grid? grid, List<PlannedRoute> routes, List<List<ChartPoint>> fixedRoutes,
        Dictionary<(TopologyNode Node, TopologyEdgePort Side), int> sideUse) {
        if (grid == null) return;
        var attempts = 0;
        for (var pass = 0; pass < 2 && attempts < MaximumReroutes; pass++) {
            var changed = false;
            var crowded = routes.Select(route => (Route: route, Quality: Interaction(route.Points, routes, fixedRoutes, route)))
                .Where(item => item.Quality.Crossings > 0).OrderByDescending(item => item.Quality.Crossings).ThenBy(item => item.Route.Request.Order).ToList();
            foreach (var item in crowded) {
                if (attempts++ >= MaximumReroutes) break;
                var route = item.Route;
                var before = Interaction(route.Points, routes, fixedRoutes, route);
                grid.Record(route.Points, -1);
                var candidate = Search(grid, route.Request, sideUse);
                if (candidate != null && !TouchesObstacle(scene, candidate)) {
                    var after = Interaction(candidate.Points, routes, fixedRoutes, route);
                    if (after.Crossings < before.Crossings && after.Shared <= before.Shared + 0.01 &&
                        RouteLength(candidate.Points) <= RouteLength(route.Points) * 1.5 + 96) {
                        route.Points = candidate.Points;
                        changed = true;
                    }
                }
                grid.Record(route.Points, 1);
            }
            if (!changed) break;
        }
    }

    private static double RouteCost(IReadOnlyList<ChartPoint> points, List<PlannedRoute> routes, List<List<ChartPoint>> fixedRoutes, PlannedRoute? except) =>
        RouteLength(points) + Math.Max(0, points.Count - 2) * BendPenalty + Interaction(points, routes, fixedRoutes, except).Cost;

    private static double RouteLength(IReadOnlyList<ChartPoint> points) {
        var length = 0.0;
        for (var i = 0; i + 1 < points.Count; i++) length += Length(points[i], points[i + 1]);
        return length;
    }

    private static (int Crossings, double Shared, double Cost) Interaction(IReadOnlyList<ChartPoint> points,
        List<PlannedRoute> routes, List<List<ChartPoint>> fixedRoutes, PlannedRoute? except) {
        var crossings = 0;
        var shared = 0.0;
        foreach (var route in routes) {
            if (ReferenceEquals(route, except)) continue;
            var result = Interaction(points, route.Points);
            crossings += result.Crossings;
            shared += result.Shared;
        }
        foreach (var route in fixedRoutes) {
            var result = Interaction(points, route);
            crossings += result.Crossings;
            shared += result.Shared;
        }
        return (crossings, shared, crossings * CrossingCost + shared * SharedRunCost);
    }

    private static (int Crossings, double Shared) Interaction(IReadOnlyList<ChartPoint> first, IReadOnlyList<ChartPoint> second) {
        HashSet<(long X, long Y)>? crossings = null;
        var shared = 0.0;
        for (var i = 0; i + 1 < first.Count; i++) {
            var a = first[i];
            var b = first[i + 1];
            for (var j = 0; j + 1 < second.Count; j++) {
                var c = second[j];
                var d = second[j + 1];
                if (Math.Abs(a.Y - b.Y) < 0.01 && Math.Abs(c.Y - d.Y) < 0.01 && Math.Abs(a.Y - c.Y) < 2)
                    shared += AxisOverlap(a.X, b.X, c.X, d.X);
                else if (Math.Abs(a.X - b.X) < 0.01 && Math.Abs(c.X - d.X) < 0.01 && Math.Abs(a.X - c.X) < 2)
                    shared += AxisOverlap(a.Y, b.Y, c.Y, d.Y);
                var denominator = (b.X - a.X) * (d.Y - c.Y) - (b.Y - a.Y) * (d.X - c.X);
                if (Math.Abs(denominator) < 1e-9) continue;
                var t = ((c.X - a.X) * (d.Y - c.Y) - (c.Y - a.Y) * (d.X - c.X)) / denominator;
                var u = ((c.X - a.X) * (b.Y - a.Y) - (c.Y - a.Y) * (b.X - a.X)) / denominator;
                if (t < -1e-6 || t > 1 + 1e-6 || u < -1e-6 || u > 1 + 1e-6) continue;
                var point = new ChartPoint(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
                // Route endpoints meeting at a card or authored junction are not proper crossings.
                if (AtEnd(point, first) || AtEnd(point, second)) continue;
                (crossings ??= new HashSet<(long X, long Y)>()).Add(((long)Math.Round(point.X * 100), (long)Math.Round(point.Y * 100)));
            }
        }
        return (crossings?.Count ?? 0, shared);
    }

    private static bool AtEnd(ChartPoint point, IReadOnlyList<ChartPoint> route) =>
        Length(point, route[0]) < 0.01 || Length(point, route[route.Count - 1]) < 0.01;

    private static double AxisOverlap(double a, double b, double c, double d) =>
        Math.Max(0, Math.Min(Math.Max(a, b), Math.Max(c, d)) - Math.Max(Math.Min(a, b), Math.Min(c, d)));
}
