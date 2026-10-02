using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyDenseRoutePlanner {
    /// <summary>Lane spacings tried from the most to the least comfortable.</summary>
    private static readonly double[] LaneSpacings = { 8, 6, 5, 4, 3, 2.5 };
    /// <summary>A run moves at most this far per neighbour it has to make room for.</summary>
    private const double DriftPerNeighbour = 8;
    private const double MaximumDrift = 48;
    /// <summary>
    /// Parallel runs this close, with nothing between them, belong to one corridor. Two runs can each drift towards
    /// the other, so the reach covers both drifts: runs that could meet are always ordered against each other.
    /// </summary>
    private const double CorridorReach = MaximumDrift * 2 + 8;
    /// <summary>The leg between a card and the first bend keeps at least this length, so the end still points at the card.</summary>
    private const double MinimumEndLeg = 4;

    /// <summary>
    /// Moves runs that share a corridor onto parallel lanes. Vertical runs are spread first, then horizontal runs on the
    /// updated geometry. A route whose new position would touch an obstacle keeps the position the search gave it.
    /// </summary>
    private static void SeparateLanes(Scene scene, List<PlannedRoute> routes) {
        if (routes.Count < 2) return;
        var searched = routes.Select(route => new List<ChartPoint>(route.Points)).ToList();
        SeparateAxis(scene, routes, vertical: true);
        SeparateAxis(scene, routes, vertical: false);
        for (var i = 0; i < routes.Count; i++) {
            if (TouchesObstacle(scene, routes[i])) routes[i].Points = searched[i];
        }
    }

    private static bool TouchesObstacle(Scene scene, PlannedRoute route) {
        var points = route.Points;
        for (var i = 0; i + 1 < points.Count; i++) {
            if (!scene.Region.Contains(points[i]) || !scene.Region.Contains(points[i + 1])) return true;
            foreach (var obstacle in scene.Obstacles) {
                if (obstacle.Box.Expand(1).Intersects(points[i], points[i + 1])) return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Works on one direction of run. Geometry is read through <paramref name="vertical"/> so both passes share the
    /// code: <c>Position</c> is the coordinate a run is moved along (x for a vertical run) and <c>From</c>/<c>To</c>
    /// are its extent.
    /// </summary>
    private static void SeparateAxis(Scene scene, List<PlannedRoute> routes, bool vertical) {
        var runs = new List<Run>();
        for (var routeIndex = 0; routeIndex < routes.Count; routeIndex++) {
            var route = routes[routeIndex];
            var points = route.Points;
            for (var i = 0; i + 1 < points.Count; i++) {
                var a = points[i];
                var b = points[i + 1];
                var along = vertical ? Math.Abs(a.X - b.X) < 0.01 && Math.Abs(a.Y - b.Y) > 0.01 : Math.Abs(a.Y - b.Y) < 0.01 && Math.Abs(a.X - b.X) > 0.01;
                if (!along) continue;
                var run = new Run(routeIndex, i, Position(a, vertical), Math.Min(Extent(a, vertical), Extent(b, vertical)), Math.Max(Extent(a, vertical), Extent(b, vertical)));
                var lowIsFirst = Extent(a, vertical) <= Extent(b, vertical);
                var before = i > 0 ? Math.Sign(Position(points[i - 1], vertical) - run.Position) : 0;
                var after = i + 2 < points.Count ? Math.Sign(Position(points[i + 2], vertical) - run.Position) : 0;
                run.LowSide = lowIsFirst ? before : after;
                run.HighSide = lowIsFirst ? after : before;
                Bound(scene, route, run, vertical, i == 0, i + 2 == points.Count);
                if (i > 0) KeepLeg(run, Position(points[i - 1], vertical), i == 1);
                if (i + 2 < points.Count) KeepLeg(run, Position(points[i + 2], vertical), i + 3 == points.Count);
                runs.Add(run);
            }
        }

        if (runs.Count < 2) return;
        runs.Sort(CompareRuns);
        var parent = new int[runs.Count];
        for (var i = 0; i < parent.Length; i++) parent[i] = i;
        var neighbours = new List<int>[runs.Count];
        for (var i = 0; i < runs.Count; i++) {
            for (var j = i + 1; j < runs.Count && runs[j].Position - runs[i].Position <= CorridorReach; j++) {
                if (!SameCorridor(scene, runs[i], runs[j], vertical)) continue;
                (neighbours[i] ??= new List<int>()).Add(j);
                (neighbours[j] ??= new List<int>()).Add(i);
                parent[Find(parent, i)] = Find(parent, j);
            }
        }

        foreach (var corridor in Enumerable.Range(0, runs.Count).Where(i => neighbours[i] != null).GroupBy(i => Find(parent, i))) {
            var members = corridor.ToList();
            var drift = Math.Min(MaximumDrift, DriftPerNeighbour * (members.Count - 1));
            foreach (var index in members) {
                var run = runs[index];
                run.Low = Math.Min(run.Position, Math.Max(run.Low, run.Position - drift));
                run.High = Math.Max(run.Position, Math.Min(run.High, run.Position + drift));
            }

            Place(runs, neighbours, members);
        }

        foreach (var run in runs) {
            if (Math.Abs(run.Placed - run.Position) < 0.01) continue;
            var points = routes[run.Route].Points;
            points[run.Index] = Moved(points[run.Index], run.Placed, vertical);
            points[run.Index + 1] = Moved(points[run.Index + 1], run.Placed, vertical);
        }
    }

    /// <summary>
    /// Gives every run in a corridor a lane. Runs are already in left-to-right (or top-to-bottom) order; the earliest and
    /// latest position each can take while keeping the spacing to its neighbours are computed first. A run that sits
    /// closer than the spacing to a neighbour is then placed halfway between the two, which keeps a bundle centred on
    /// where the search put it; any other run stays where it is unless a neighbour needs the room.
    /// </summary>
    private static void Place(List<Run> runs, List<int>[] neighbours, List<int> members) {
        foreach (var spacing in LaneSpacings) {
            var fits = true;
            foreach (var index in members) {
                var run = runs[index];
                run.Earliest = run.Low;
                foreach (var other in neighbours[index]) {
                    if (other < index) run.Earliest = Math.Max(run.Earliest, runs[other].Earliest + spacing);
                }

                if (run.Earliest > run.High + 0.001) fits = false;
            }

            if (!fits && spacing > LaneSpacings[LaneSpacings.Length - 1]) continue;
            for (var position = members.Count - 1; position >= 0; position--) {
                var index = members[position];
                var run = runs[index];
                run.Earliest = Math.Min(run.Earliest, run.High);
                run.Latest = run.High;
                foreach (var other in neighbours[index]) {
                    if (other > index) run.Latest = Math.Min(run.Latest, runs[other].Latest - spacing);
                }

                run.Latest = Math.Max(run.Latest, run.Earliest);
            }

            foreach (var index in members) {
                var run = runs[index];
                var crowded = false;
                foreach (var other in neighbours[index]) crowded |= Math.Abs(runs[other].Position - run.Position) < spacing;
                var wanted = crowded ? (run.Earliest + run.Latest) / 2 : run.Position;
                foreach (var other in neighbours[index]) {
                    if (other < index) wanted = Math.Max(wanted, runs[other].Placed + spacing);
                }

                run.Placed = Math.Min(Math.Max(wanted, run.Earliest), run.Latest);
            }

            return;
        }
    }

    /// <summary>
    /// Limits how far a run may move: up to the lane clearance from the nearest obstacle on each side, inside the
    /// content area, and, for the first or last run of a route, along the card side it is attached to.
    /// </summary>
    private static void Bound(Scene scene, PlannedRoute route, Run run, bool vertical, bool first, bool last) {
        run.Low = (vertical ? scene.Region.Left : scene.Region.Top) + 1;
        run.High = (vertical ? scene.Region.Right : scene.Region.Bottom) - 1;
        var pinned = false;
        foreach (var obstacle in scene.Obstacles) {
            var box = obstacle.Box;
            var near = vertical ? box.Left : box.Top;
            var far = vertical ? box.Right : box.Bottom;
            var from = vertical ? box.Top : box.Left;
            var to = vertical ? box.Bottom : box.Right;
            if (to < run.From - LaneClearance || from > run.To + LaneClearance) continue;
            if (far <= run.Position) run.Low = Math.Max(run.Low, far + LaneClearance);
            else if (near >= run.Position) run.High = Math.Min(run.High, near - LaneClearance);
            else pinned = true;
        }

        if (first) pinned |= Attach(route.Start, run, vertical);
        if (last) pinned |= Attach(route.End, run, vertical);
        if (pinned || run.Low > run.Position || run.High < run.Position) run.Low = run.High = run.Position;
    }

    /// <summary>
    /// Keeps the leg that joins a run to the rest of its route pointing the same way. The far end of that leg is either
    /// the route end, which does not move in this pass, or the next parallel run, which may move towards this one, so
    /// each run stays on its own side of the midpoint.
    /// </summary>
    private static void KeepLeg(Run run, double other, bool otherIsRouteEnd) {
        var length = Math.Abs(other - run.Position);
        var keep = otherIsRouteEnd ? Math.Min(length, MinimumEndLeg) : length / 2 + 0.5;
        if (run.Position > other) run.Low = Math.Max(run.Low, Math.Min(run.Position, other + keep));
        else run.High = Math.Min(run.High, Math.Max(run.Position, other - keep));
    }

    // Keeps the end of a route on the card side it leaves through. Returns true when the end must not move at all.
    private static bool Attach(Terminal terminal, Run run, bool vertical) {
        if (terminal.IsFixed) return true;
        var node = terminal.Node;
        var start = vertical ? node.X : node.Y;
        var size = vertical ? node.Width : node.Height;
        var padding = Math.Min(SidePadding, size / 4);
        run.Low = Math.Max(run.Low, start + padding);
        run.High = Math.Min(run.High, start + size - padding);
        return false;
    }

    // Two runs share a corridor when they run side by side for a stretch and no obstacle stands between them there.
    private static bool SameCorridor(Scene scene, Run first, Run second, bool vertical) {
        var from = Math.Max(first.From, second.From);
        var to = Math.Min(first.To, second.To);
        if (to - from < 2) return false;
        if (second.Position - first.Position < 0.5) return true;
        foreach (var obstacle in scene.Obstacles) {
            var box = obstacle.Box;
            var near = vertical ? box.Left : box.Top;
            var far = vertical ? box.Right : box.Bottom;
            var boxFrom = vertical ? box.Top : box.Left;
            var boxTo = vertical ? box.Bottom : box.Right;
            if (far > first.Position && near < second.Position && boxTo > from && boxFrom < to) return false;
        }

        return true;
    }

    /// <summary>
    /// Orders runs across a corridor. Runs at the same position are ordered by where their route turns at each end, so a
    /// route that arrives from and leaves to the left takes the left lane, and two routes turning the same way nest
    /// instead of crossing.
    /// </summary>
    private static int CompareRuns(Run a, Run b) {
        var byPosition = (Math.Round(a.Position * 2) / 2).CompareTo(Math.Round(b.Position * 2) / 2);
        if (byPosition != 0) return byPosition;
        var bySide = (a.LowSide + a.HighSide).CompareTo(b.LowSide + b.HighSide);
        if (bySide != 0) return bySide;
        var byNesting = Nesting(a).CompareTo(Nesting(b));
        if (byNesting != 0) return byNesting;
        var byRoute = a.Route.CompareTo(b.Route);
        return byRoute != 0 ? byRoute : a.Index.CompareTo(b.Index);
    }

    private static double Nesting(Run run) =>
        (run.LowSide < 0 ? -run.From : run.LowSide > 0 ? run.From : 0) + (run.HighSide < 0 ? run.To : run.HighSide > 0 ? -run.To : 0);

    private static int Find(int[] parent, int index) {
        while (parent[index] != index) {
            parent[index] = parent[parent[index]];
            index = parent[index];
        }

        return index;
    }

    private static double Position(ChartPoint point, bool vertical) => vertical ? point.X : point.Y;

    private static double Extent(ChartPoint point, bool vertical) => vertical ? point.Y : point.X;

    private static ChartPoint Moved(ChartPoint point, double position, bool vertical) => vertical ? new ChartPoint(position, point.Y) : new ChartPoint(point.X, position);

    /// <summary>One straight stretch of a route.</summary>
    private sealed class Run {
        public Run(int route, int index, double position, double from, double to) {
            Route = route;
            Index = index;
            Position = position;
            From = from;
            To = to;
            Placed = position;
        }

        public int Route { get; }
        /// <summary>Index of the run's first point in the route.</summary>
        public int Index { get; }
        public double Position { get; }
        public double From { get; }
        public double To { get; }
        /// <summary>Which side the route continues on past the low and high ends: -1 before, 1 after, 0 when the run ends at a card.</summary>
        public int LowSide { get; set; }
        public int HighSide { get; set; }
        public double Low { get; set; }
        public double High { get; set; }
        public double Earliest { get; set; }
        public double Latest { get; set; }
        public double Placed { get; set; }
    }
}
