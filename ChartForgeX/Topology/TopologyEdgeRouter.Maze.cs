using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyEdgeRouter {
    private const double MazeClearance = 10;
    private const double MazeEndpointGap = 7;
    private const double MazeBendPenalty = 36;
    private const double MazeRegionMargin = 180;
    private const int MazeMaximumGridPoints = 250_000;
    private static readonly ConditionalWeakTable<TopologyChart, Dictionary<string, List<ChartPoint>?>> MazeCache = new();

    /// <summary>
    /// Finds an orthogonal route that avoids every foreign card, caption, and group header by searching a sparse grid
    /// built from the obstacle edges (A* with a bend penalty). Used when the corridor candidates all hit an obstacle.
    /// Returns null when no clear route exists.
    /// </summary>
    /// <remarks>
    /// Routes are cached on the prepared chart instance. Labels, display modes, render options, and text measurement are
    /// fixed once a chart is prepared, so the key only tracks the edge, its ports, and the card and group geometry.
    /// </remarks>
    private static List<ChartPoint>? MazeRoute(TopologyChart chart, TopologyEdge edge, TopologyNode source, TopologyNode target) {
        var key = edge.Id + "|" + edge.SourcePort + "|" + edge.TargetPort + "|" + GeometrySignature(chart).ToString(System.Globalization.CultureInfo.InvariantCulture);
        var cache = MazeCache.GetOrCreateValue(chart);
        lock (cache) {
            if (cache.TryGetValue(key, out var cached)) return cached == null ? null : new List<ChartPoint>(cached);
        }

        var result = ComputeMazeRoute(chart, edge, source, target);
        lock (cache) cache[key] = result;
        return result == null ? null : new List<ChartPoint>(result);
    }

    // Cached routes are reused only while every card and group keeps its geometry.
    private static long GeometrySignature(TopologyChart chart) {
        unchecked {
            long hash = chart.Nodes.Count * 397L + chart.Groups.Count;
            foreach (var node in chart.Nodes) hash = Mix(Mix(Mix(Mix(hash, node.X), node.Y), node.Width), node.Height);
            foreach (var group in chart.Groups) hash = Mix(Mix(Mix(Mix(hash, group.X), group.Y), group.Width), group.Height);
            return hash;
        }
    }

    private static long Mix(long hash, double value) => unchecked(hash * 1_000_003L ^ BitConverter.DoubleToInt64Bits(value));

    private static List<ChartPoint>? ComputeMazeRoute(TopologyChart chart, TopologyEdge edge, TopologyNode source, TopologyNode target) {
        var obstacles = chart.Nodes
            .Where(node => !IsRouteBackdropArtwork(node))
            .Select(node => NodeRouteBox(chart, node).Expand(MazeClearance))
            .ToList();
        foreach (var group in chart.Groups) {
            var header = GroupHeaderBox(group, chart.TextMeasurement).Expand(MazeClearance);
            if (header.Width > 0 && header.Height > 0) obstacles.Add(header);
        }

        foreach (var box in EstimatedLabelBoxes(chart, edge.Id)) obstacles.Add(box.Expand(4));

        // Keep the declared or inferred ports first so endpoint spreading and diagnostics stay consistent; other sides
        // are tried only when no route leaves through them.
        return SearchWithSides(chart, obstacles, source, target, edge, allSides: false)
            ?? ((edge.LayoutInference & (TopologyEdgeLayoutInference.SourcePort | TopologyEdgeLayoutInference.TargetPort)) != 0
                ? SearchWithSides(chart, obstacles, source, target, edge, allSides: true)
                : null);
    }

    private static List<ChartPoint>? SearchWithSides(TopologyChart chart, List<RouteBox> obstacles, TopologyNode source, TopologyNode target, TopologyEdge edge, bool allSides) {
        var starts = MazeTerminals(chart, source, edge.SourcePort, allSides && (edge.LayoutInference & TopologyEdgeLayoutInference.SourcePort) != 0);
        var ends = MazeTerminals(chart, target, edge.TargetPort, allSides && (edge.LayoutInference & TopologyEdgeLayoutInference.TargetPort) != 0);
        starts.RemoveAll(terminal => BlockedStub(terminal, obstacles));
        ends.RemoveAll(terminal => BlockedStub(terminal, obstacles));
        if (starts.Count == 0 || ends.Count == 0) return null;

        var all = starts.Concat(ends).ToList();
        var region = new RouteBox(all.Min(item => item.Stub.X) - MazeRegionMargin, all.Min(item => item.Stub.Y) - MazeRegionMargin, all.Max(item => item.Stub.X) + MazeRegionMargin, all.Max(item => item.Stub.Y) + MazeRegionMargin);
        return SearchMaze(obstacles, starts, ends, region) ?? SearchMaze(obstacles, starts, ends, ContentBox(obstacles, all));
    }

    private static RouteBox ContentBox(IReadOnlyList<RouteBox> obstacles, IReadOnlyList<MazeTerminal> terminals) {
        var left = Math.Min(obstacles.Min(box => box.Left), terminals.Min(item => item.Stub.X)) - MazeClearance * 4;
        var top = Math.Min(obstacles.Min(box => box.Top), terminals.Min(item => item.Stub.Y)) - MazeClearance * 4;
        var right = Math.Max(obstacles.Max(box => box.Right), terminals.Max(item => item.Stub.X)) + MazeClearance * 4;
        var bottom = Math.Max(obstacles.Max(box => box.Bottom), terminals.Max(item => item.Stub.Y)) + MazeClearance * 4;
        return new RouteBox(left, top, right, bottom);
    }

    private static List<MazeTerminal> MazeTerminals(TopologyChart chart, TopologyNode node, TopologyEdgePort port, bool anySide) {
        var box = NodeRouteBox(chart, node);
        var hasCaption = box.Bottom > node.Y + node.Height + 0.5;
        var sides = anySide || port == TopologyEdgePort.Auto
            ? new[] { TopologyEdgePort.Top, TopologyEdgePort.Right, TopologyEdgePort.Bottom, TopologyEdgePort.Left }
            : new[] { port };
        var terminals = new List<MazeTerminal>(sides.Length);
        var cx = node.X + node.Width / 2;
        var cy = node.Y + node.Height / 2;
        foreach (var side in sides) {
            // Leave tile cards from the sides or top so a route never cuts through the caption below the card.
            if (side == TopologyEdgePort.Bottom && hasCaption && sides.Length > 1) continue;
            var (port0, stub) = side switch {
                // Ends stop the same gap short of the card as corridor routes, so arrowheads stay clear of the border.
                TopologyEdgePort.Top => (new ChartPoint(cx, node.Y - MazeEndpointGap), new ChartPoint(cx, box.Top - MazeClearance - 1)),
                TopologyEdgePort.Bottom => (new ChartPoint(cx, node.Y + node.Height + MazeEndpointGap), new ChartPoint(cx, box.Bottom + MazeClearance + 1)),
                TopologyEdgePort.Left => (new ChartPoint(node.X - MazeEndpointGap, cy), new ChartPoint(box.Left - MazeClearance - 1, cy)),
                TopologyEdgePort.Right => (new ChartPoint(node.X + node.Width + MazeEndpointGap, cy), new ChartPoint(box.Right + MazeClearance + 1, cy)),
                _ => (new ChartPoint(cx, cy), new ChartPoint(cx, cy))
            };
            terminals.Add(new MazeTerminal(port0, stub, side == port ? 0 : MazeBendPenalty));
        }

        return terminals;
    }

    private static bool BlockedStub(MazeTerminal terminal, IReadOnlyList<RouteBox> obstacles) {
        foreach (var box in obstacles) {
            if (Inside(box, terminal.Stub)) return true;
        }

        return false;
    }

    private static List<ChartPoint>? SearchMaze(List<RouteBox> allObstacles, List<MazeTerminal> starts, List<MazeTerminal> ends, RouteBox region) {
        var obstacles = allObstacles.Where(box => box.Right >= region.Left && box.Left <= region.Right && box.Bottom >= region.Top && box.Top <= region.Bottom).ToList();
        var xs = new SortedSet<double> { region.Left, region.Right };
        var ys = new SortedSet<double> { region.Top, region.Bottom };
        foreach (var box in obstacles) {
            AddLine(xs, box.Left - 1, region.Left, region.Right);
            AddLine(xs, box.Right + 1, region.Left, region.Right);
            AddLine(ys, box.Top - 1, region.Top, region.Bottom);
            AddLine(ys, box.Bottom + 1, region.Top, region.Bottom);
        }

        foreach (var terminal in starts.Concat(ends)) {
            AddLine(xs, terminal.Stub.X, double.NegativeInfinity, double.PositiveInfinity);
            AddLine(ys, terminal.Stub.Y, double.NegativeInfinity, double.PositiveInfinity);
        }

        var xa = xs.ToArray();
        var ya = ys.ToArray();
        if ((long)xa.Length * ya.Length > MazeMaximumGridPoints) return null;
        var width = xa.Length;
        var blocked = new bool[width * ya.Length];
        foreach (var box in obstacles) {
            var i0 = LowerBound(xa, box.Left);
            var j0 = LowerBound(ya, box.Top);
            for (var j = j0; j < ya.Length && ya[j] <= box.Bottom; j++) {
                for (var i = i0; i < width && xa[i] <= box.Right; i++) blocked[j * width + i] = true;
            }
        }

        // Moving between neighbouring grid points is blocked when the segment passes through an obstacle.
        bool Clear(int i1, int j1, int i2, int j2) {
            var a = new ChartPoint(xa[i1], ya[j1]);
            var b = new ChartPoint(xa[i2], ya[j2]);
            foreach (var box in obstacles) {
                if (box.Intersects(a, b)) return false;
            }

            return true;
        }

        var startIndex = new Dictionary<int, MazeTerminal>();
        foreach (var terminal in starts) {
            var index = Array.BinarySearch(ya, terminal.Stub.Y) * width + Array.BinarySearch(xa, terminal.Stub.X);
            if (!blocked[index] && !startIndex.ContainsKey(index)) startIndex[index] = terminal;
        }

        var endIndex = new Dictionary<int, MazeTerminal>();
        foreach (var terminal in ends) {
            var index = Array.BinarySearch(ya, terminal.Stub.Y) * width + Array.BinarySearch(xa, terminal.Stub.X);
            if (!blocked[index] && !endIndex.ContainsKey(index)) endIndex[index] = terminal;
        }

        if (startIndex.Count == 0 || endIndex.Count == 0) return null;
        var states = width * ya.Length * 5;
        var cost = new double[states];
        var previous = new int[states];
        for (var i = 0; i < states; i++) {
            cost[i] = double.PositiveInfinity;
            previous[i] = -1;
        }

        double Heuristic(int cell) {
            var x = xa[cell % width];
            var y = ya[cell / width];
            var best = double.PositiveInfinity;
            foreach (var end in endIndex.Values) best = Math.Min(best, Math.Abs(end.Stub.X - x) + Math.Abs(end.Stub.Y - y));
            return best;
        }

        var queue = new MazeQueue();
        foreach (var start in startIndex) {
            var state = start.Key * 5 + 4;
            cost[state] = start.Value.Penalty;
            queue.Push(state, cost[state] + Heuristic(start.Key), cost[state]);
        }

        var dx = new[] { 1, -1, 0, 0 };
        var dy = new[] { 0, 0, 1, -1 };
        var goal = -1;
        var goalCost = double.PositiveInfinity;
        while (queue.Count > 0) {
            var (state, priority, pushedCost) = queue.Pop();
            if (priority >= goalCost) break;
            var current = cost[state];
            if (pushedCost > current) continue;
            var cell = state / 5;
            var direction = state % 5;
            if (endIndex.TryGetValue(cell, out var end) && current + end.Penalty < goalCost) {
                goalCost = current + end.Penalty;
                goal = state;
            }

            var ci = cell % width;
            var cj = cell / width;
            for (var d = 0; d < 4; d++) {
                var ni = ci + dx[d];
                var nj = cj + dy[d];
                if (ni < 0 || nj < 0 || ni >= width || nj >= ya.Length) continue;
                var next = nj * width + ni;
                if (blocked[next] || !Clear(ci, cj, ni, nj)) continue;
                var step = Math.Abs(xa[ni] - xa[ci]) + Math.Abs(ya[nj] - ya[cj]) + (direction != 4 && direction != d ? MazeBendPenalty : 0);
                var nextState = next * 5 + d;
                if (current + step >= cost[nextState]) continue;
                cost[nextState] = current + step;
                previous[nextState] = state;
                queue.Push(nextState, cost[nextState] + Heuristic(next), cost[nextState]);
            }
        }

        if (goal < 0) return null;
        var cells = new List<int>();
        for (var state = goal; state >= 0; state = previous[state]) cells.Add(state / 5);
        cells.Reverse();
        var startTerminal = startIndex[cells[0]];
        var endTerminal = endIndex[cells[cells.Count - 1]];
        var points = new List<ChartPoint> { startTerminal.Port };
        foreach (var cell in cells) points.Add(new ChartPoint(xa[cell % width], ya[cell / width]));
        points.Add(endTerminal.Port);
        return SimplifyOrthogonal(points);
    }

    private static int LowerBound(double[] values, double value) {
        var index = Array.BinarySearch(values, value);
        return index >= 0 ? index : ~index;
    }

    private static void AddLine(SortedSet<double> lines, double value, double min, double max) {
        if (value < min || value > max) return;
        lines.Add(Math.Round(value * 2) / 2);
    }

    private static bool Inside(RouteBox box, ChartPoint point) =>
        point.X >= box.Left && point.X <= box.Right && point.Y >= box.Top && point.Y <= box.Bottom;

    private static List<ChartPoint> SimplifyOrthogonal(List<ChartPoint> points) {
        var result = new List<ChartPoint>(points.Count);
        foreach (var point in points) {
            if (result.Count > 0 && Math.Abs(result[result.Count - 1].X - point.X) < 0.01 && Math.Abs(result[result.Count - 1].Y - point.Y) < 0.01) continue;
            if (result.Count >= 2) {
                var a = result[result.Count - 2];
                var b = result[result.Count - 1];
                var collinear = (Math.Abs(a.X - b.X) < 0.01 && Math.Abs(b.X - point.X) < 0.01) || (Math.Abs(a.Y - b.Y) < 0.01 && Math.Abs(b.Y - point.Y) < 0.01);
                if (collinear) result.RemoveAt(result.Count - 1);
            }

            result.Add(point);
        }

        return result;
    }

    private readonly struct MazeTerminal {
        public MazeTerminal(ChartPoint port, ChartPoint stub, double penalty) {
            Stub = new ChartPoint(Math.Round(stub.X * 2) / 2, Math.Round(stub.Y * 2) / 2);
            // Keep the first leg exactly orthogonal: the port slides along the card edge onto the rounded stub axis.
            Port = Math.Abs(port.X - stub.X) < 0.001 ? new ChartPoint(Stub.X, port.Y) : new ChartPoint(port.X, Stub.Y);
            Penalty = penalty;
        }

        public ChartPoint Port { get; }

        public ChartPoint Stub { get; }

        public double Penalty { get; }
    }

    /// <summary>A small binary min-heap; framework priority queues are unavailable on net472 and netstandard2.0.</summary>
    private sealed class MazeQueue {
        private readonly List<(int State, double Priority, double Cost)> _items = new();

        public int Count => _items.Count;

        public void Push(int state, double priority, double cost) {
            _items.Add((state, priority, cost));
            var index = _items.Count - 1;
            while (index > 0) {
                var parent = (index - 1) / 2;
                if (Compare(_items[parent], _items[index]) <= 0) break;
                (_items[parent], _items[index]) = (_items[index], _items[parent]);
                index = parent;
            }
        }

        public (int State, double Priority, double Cost) Pop() {
            var top = _items[0];
            var last = _items[_items.Count - 1];
            _items.RemoveAt(_items.Count - 1);
            if (_items.Count == 0) return top;
            _items[0] = last;
            var index = 0;
            while (true) {
                var left = index * 2 + 1;
                if (left >= _items.Count) break;
                var right = left + 1;
                var smallest = right < _items.Count && Compare(_items[right], _items[left]) < 0 ? right : left;
                if (Compare(_items[smallest], _items[index]) >= 0) break;
                (_items[smallest], _items[index]) = (_items[index], _items[smallest]);
                index = smallest;
            }

            return top;
        }

        // Ties break on the state index so the search is deterministic.
        private static int Compare((int State, double Priority, double Cost) a, (int State, double Priority, double Cost) b) {
            var byPriority = a.Priority.CompareTo(b.Priority);
            return byPriority != 0 ? byPriority : a.State.CompareTo(b.State);
        }
    }
}
