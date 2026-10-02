using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyDenseRoutePlanner {
    /// <summary>Cost of a bend, in pixels of extra length a route accepts to avoid it.</summary>
    private const double BendPenalty = 36;
    /// <summary>Cost of leaving a card through a side that does not face the other end.</summary>
    private const double SidePenalty = 24;
    /// <summary>Extra cost per pixel, per route already there, of running along a corridor another route uses.</summary>
    private const double SharedRunCost = 0.5;
    /// <summary>Cost of crossing another route.</summary>
    private const double CrossingCost = 14;
    /// <summary>Cost of the short stub that leaves little room for a direction marker.</summary>
    private const double ShortStubPenalty = 6;
    /// <summary>Extra cost, per route already attached there, of using a card side, so ends spread over the sides.</summary>
    private const double SideUseCost = 18;
    /// <summary>Extra cost per pixel of running along a group border instead of beside it.</summary>
    private const double BorderRunCost = 1.5;
    /// <summary>Runs this close to a group border read as part of it.</summary>
    private const double BorderReach = 7;
    /// <summary>Distance from a group border of the grid lines offered on both sides of it.</summary>
    private const double BorderLane = 13;
    private const int MaximumSharedRoutes = 16;
    private const int MaximumGridPoints = 400_000;
    private static readonly double[] LineMergeDistances = { 4, 8, 16, 32 };

    // Directions: 0 right, 1 left, 2 down, 3 up.
    private static readonly int[] StepX = { 1, -1, 0, 0 };
    private static readonly int[] StepY = { 0, 0, 1, -1 };

    /// <summary>
    /// Joins two cards that face each other with one straight line when nothing stands between them. The grid search
    /// cannot find this route when the cards are so close that their stubs would pass each other. A direct route is
    /// not recorded on the grid; routes that end up beside it are moved apart by the lane pass.
    /// </summary>
    private static PlannedRoute? Direct(Scene scene, Request request, Dictionary<(TopologyNode Node, TopologyEdgePort Side), int> sideUse) {
        PlannedRoute? best = null;
        var bestCost = double.PositiveInfinity;
        foreach (var start in request.Starts) {
            foreach (var end in request.Ends) {
                if (OutwardDirection(start.Side) != InwardDirection(end.Side)) continue;
                var horizontal = start.Side is TopologyEdgePort.Left or TopologyEdgePort.Right;
                var offset = horizontal ? end.Port.Y - start.Port.Y : end.Port.X - start.Port.X;
                var advance = horizontal ? end.Port.X - start.Port.X : end.Port.Y - start.Port.Y;
                var forward = start.Side is TopologyEdgePort.Right or TopologyEdgePort.Bottom ? advance : -advance;
                if (Math.Abs(offset) > 0.75 || forward < 1) continue;
                var target = horizontal ? new ChartPoint(end.Port.X, start.Port.Y) : new ChartPoint(start.Port.X, end.Port.Y);
                if (scene.Blocks(start.Port, target, start.Node.Id, end.Node.Id)) continue;
                var cost = forward + start.Penalty + end.Penalty
                    + (sideUse.TryGetValue((start.Node, start.Side), out var startUse) ? startUse * SideUseCost : 0)
                    + (sideUse.TryGetValue((end.Node, end.Side), out var endUse) ? endUse * SideUseCost : 0);
                if (cost >= bestCost) continue;
                bestCost = cost;
                best = new PlannedRoute(request, start, end, new List<ChartPoint> { start.Port, target });
            }
        }

        return best;
    }

    /// <summary>
    /// Finds the cheapest orthogonal route between any start and any end terminal (A* with a bend penalty), counting
    /// length, bends, corridors shared with earlier routes, and crossings of earlier routes. Returns null when the
    /// ends cannot be joined without passing an obstacle.
    /// </summary>
    private static PlannedRoute? Search(Grid grid, Request request, Dictionary<(TopologyNode Node, TopologyEdgePort Side), int> sideUse) {
        double Penalty(Terminal terminal) => terminal.Penalty + (sideUse.TryGetValue((terminal.Node, terminal.Side), out var used) ? used * SideUseCost : 0);
        var starts = new Dictionary<int, Terminal>();
        foreach (var terminal in request.Starts) {
            var cell = grid.Cell(terminal.Stub);
            if (cell >= 0 && !grid.Blocked[cell] && !starts.ContainsKey(cell)) starts.Add(cell, terminal);
        }

        var ends = new Dictionary<int, Terminal>();
        foreach (var terminal in request.Ends) {
            var cell = grid.Cell(terminal.Stub);
            if (cell >= 0 && !grid.Blocked[cell] && !ends.ContainsKey(cell)) ends.Add(cell, terminal);
        }

        if (starts.Count == 0 || ends.Count == 0) return null;
        grid.BeginSearch();
        var queue = new MinHeap();
        foreach (var start in starts) {
            var state = start.Key * 4 + OutwardDirection(start.Value.Side);
            var cost = Penalty(start.Value) + Length(start.Value.Port, start.Value.Stub) + grid.CrossingsAt(start.Key, OutwardDirection(start.Value.Side)) * CrossingCost;
            if (cost >= grid.CostOf(state)) continue;
            grid.SetCost(state, cost, -1);
            queue.Push(state, cost + Heuristic(grid, start.Key, ends), cost);
        }

        var goal = -1;
        var goalCost = double.PositiveInfinity;
        Terminal? goalTerminal = null;
        while (queue.Count > 0) {
            var (state, priority, pushedCost) = queue.Pop();
            if (priority >= goalCost) break;
            var current = grid.CostOf(state);
            if (pushedCost > current) continue;
            var cell = state / 4;
            var direction = state % 4;
            if (ends.TryGetValue(cell, out var end)) {
                // Arriving straight into the card costs nothing; any other approach needs one more bend.
                var arrival = current + Penalty(end) + Length(end.Port, end.Stub) + (direction == InwardDirection(end.Side) ? 0 : BendPenalty);
                if (arrival < goalCost) {
                    goalCost = arrival;
                    goal = state;
                    goalTerminal = end;
                }
            }

            var column = cell % grid.Width;
            var row = cell / grid.Width;
            for (var next = 0; next < 4; next++) {
                if ((next ^ 1) == direction) continue;
                var nextColumn = column + StepX[next];
                var nextRow = row + StepY[next];
                if (nextColumn < 0 || nextRow < 0 || nextColumn >= grid.Width || nextRow >= grid.Height) continue;
                var nextCell = nextRow * grid.Width + nextColumn;
                if (grid.Blocked[nextCell] || !grid.CanMove(cell, nextCell, next)) continue;
                var length = Math.Abs(grid.Xs[nextColumn] - grid.Xs[column]) + Math.Abs(grid.Ys[nextRow] - grid.Ys[row]);
                var step = length * (1 + SharedRunCost * Math.Min(MaximumSharedRoutes, grid.SharedRoutes(cell, nextCell, next)) + (grid.AlongBorder(cell, nextCell, next) ? BorderRunCost : 0))
                    + (next == direction ? 0 : BendPenalty)
                    + grid.CrossingsAt(nextCell, next) * CrossingCost;
                var nextState = nextCell * 4 + next;
                if (current + step >= grid.CostOf(nextState)) continue;
                grid.SetCost(nextState, current + step, state);
                queue.Push(nextState, current + step + Heuristic(grid, nextCell, ends), current + step);
            }
        }

        if (goal < 0 || goalTerminal == null) return null;
        var cells = new List<int>();
        for (var state = goal; state >= 0; state = grid.PreviousOf(state)) cells.Add(state / 4);
        cells.Reverse();
        grid.MarkUsed(cells);
        var startTerminal = starts[cells[0]];
        var points = new List<ChartPoint>(cells.Count + 2) { startTerminal.Port };
        foreach (var cell in cells) points.Add(new ChartPoint(grid.Xs[cell % grid.Width], grid.Ys[cell / grid.Width]));
        points.Add(goalTerminal.Port);
        return new PlannedRoute(request, startTerminal, goalTerminal, Simplify(points));
    }

    private static double Heuristic(Grid grid, int cell, Dictionary<int, Terminal> ends) {
        var x = grid.Xs[cell % grid.Width];
        var y = grid.Ys[cell / grid.Width];
        var best = double.PositiveInfinity;
        foreach (var end in ends.Values) best = Math.Min(best, Math.Abs(end.Stub.X - x) + Math.Abs(end.Stub.Y - y) + end.Penalty);
        return best;
    }

    private static int OutwardDirection(TopologyEdgePort side) => side switch {
        TopologyEdgePort.Right => 0,
        TopologyEdgePort.Left => 1,
        TopologyEdgePort.Bottom => 2,
        _ => 3
    };

    private static int InwardDirection(TopologyEdgePort side) => OutwardDirection(side) ^ 1;

    private static double Length(ChartPoint a, ChartPoint b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);

    /// <summary>Drops repeated points and the middle point of every straight run.</summary>
    private static List<ChartPoint> Simplify(List<ChartPoint> points) {
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

    /// <summary>
    /// A sparse grid whose lines run just outside every obstacle and through every terminal stub. It records which
    /// moves an obstacle blocks and how many routes already use each grid segment.
    /// </summary>
    private sealed class Grid {
        private readonly bool[] _noRight;
        private readonly bool[] _noDown;
        private readonly int[] _usedRight;
        private readonly int[] _usedDown;
        private readonly bool[] _borderRight;
        private readonly bool[] _borderDown;
        private readonly double[] _cost;
        private readonly int[] _previous;
        private readonly int[] _stamp;
        private int _search;

        private Grid(double[] xs, double[] ys) {
            Xs = xs;
            Ys = ys;
            Width = xs.Length;
            Height = ys.Length;
            var points = Width * Height;
            Blocked = new bool[points];
            _noRight = new bool[points];
            _noDown = new bool[points];
            _usedRight = new int[points];
            _usedDown = new int[points];
            _borderRight = new bool[points];
            _borderDown = new bool[points];
            _cost = new double[points * 4];
            _previous = new int[points * 4];
            _stamp = new int[points * 4];
        }

        public double[] Xs { get; }
        public double[] Ys { get; }
        public int Width { get; }
        public int Height { get; }
        public bool[] Blocked { get; }

        public static Grid? Create(Scene scene, List<Request> requests) {
            var region = scene.Region;
            var xs = new SortedSet<double> { region.Left, region.Right };
            var ys = new SortedSet<double> { region.Top, region.Bottom };
            foreach (var obstacle in scene.Obstacles) {
                var box = obstacle.Box.Expand(Clearance);
                AddLine(xs, box.Left - 1, region.Left, region.Right);
                AddLine(xs, box.Right + 1, region.Left, region.Right);
                AddLine(ys, box.Top - 1, region.Top, region.Bottom);
                AddLine(ys, box.Bottom + 1, region.Top, region.Bottom);
            }

            // A lane on each side of every group border, so a route can run beside a border instead of on it.
            foreach (var group in scene.Groups) {
                AddLine(xs, group.Left - BorderLane, region.Left, region.Right);
                AddLine(xs, group.Left + BorderLane, region.Left, region.Right);
                AddLine(xs, group.Right - BorderLane, region.Left, region.Right);
                AddLine(xs, group.Right + BorderLane, region.Left, region.Right);
                AddLine(ys, group.Top - BorderLane, region.Top, region.Bottom);
                AddLine(ys, group.Top + BorderLane, region.Top, region.Bottom);
                AddLine(ys, group.Bottom - BorderLane, region.Top, region.Bottom);
                AddLine(ys, group.Bottom + BorderLane, region.Top, region.Bottom);
            }

            foreach (var request in requests) {
                foreach (var terminal in request.Starts) { xs.Add(terminal.Stub.X); ys.Add(terminal.Stub.Y); }
                foreach (var terminal in request.Ends) { xs.Add(terminal.Stub.X); ys.Add(terminal.Stub.Y); }
            }

            var stubXs = new HashSet<double> { region.Left, region.Right };
            var stubYs = new HashSet<double> { region.Top, region.Bottom };
            foreach (var request in requests) {
                foreach (var terminal in request.Starts) { stubXs.Add(terminal.Stub.X); stubYs.Add(terminal.Stub.Y); }
                foreach (var terminal in request.Ends) { stubXs.Add(terminal.Stub.X); stubYs.Add(terminal.Stub.Y); }
            }

            // Layouts whose cards do not share rows and columns produce many nearly identical lines. Merging lines that
            // are close together loses little and keeps the search bounded; blocking does not depend on which lines exist.
            var columns = ToArray(xs);
            var rows = ToArray(ys);
            foreach (var merge in LineMergeDistances) {
                if ((long)columns.Length * rows.Length <= MaximumGridPoints) break;
                columns = Thin(ToArray(xs), stubXs, merge);
                rows = Thin(ToArray(ys), stubYs, merge);
            }

            if ((long)columns.Length * rows.Length > MaximumGridPoints) return null;
            var grid = new Grid(columns, rows);
            foreach (var obstacle in scene.Obstacles) grid.Block(obstacle.Box.Expand(Clearance));
            foreach (var group in scene.Groups) grid.MarkBorder(group);
            return grid;
        }

        // Keeps every required line and drops the others that follow a kept line too closely.
        private static double[] Thin(double[] lines, HashSet<double> required, double distance) {
            var kept = new List<double>(lines.Length);
            foreach (var line in lines) {
                if (required.Contains(line) || kept.Count == 0 || line - kept[kept.Count - 1] >= distance) kept.Add(line);
            }

            return kept.ToArray();
        }

        private static double[] ToArray(SortedSet<double> values) {
            var array = new double[values.Count];
            values.CopyTo(array);
            return array;
        }

        private static void AddLine(SortedSet<double> lines, double value, double min, double max) {
            if (value < min || value > max) return;
            lines.Add(Math.Round(value * 2) / 2);
        }

        private void Block(Box box) {
            var firstColumn = LowerBound(Xs, box.Left);
            var lastColumn = UpperBound(Xs, box.Right) - 1;
            var firstRow = LowerBound(Ys, box.Top);
            var lastRow = UpperBound(Ys, box.Bottom) - 1;
            for (var row = firstRow; row <= lastRow; row++) {
                for (var column = firstColumn; column <= lastColumn; column++) Blocked[row * Width + column] = true;
                // A move is blocked when it enters, leaves, or jumps across the box.
                for (var column = Math.Max(0, firstColumn - 1); column <= lastColumn && column + 1 < Width; column++) _noRight[row * Width + column] = true;
            }

            for (var column = firstColumn; column <= lastColumn; column++) {
                for (var row = Math.Max(0, firstRow - 1); row <= lastRow && row + 1 < Height; row++) _noDown[row * Width + column] = true;
            }
        }

        private void MarkBorder(Box group) {
            var firstColumn = LowerBound(Xs, group.Left - BorderReach);
            var lastColumn = UpperBound(Xs, group.Right + BorderReach) - 1;
            var firstRow = LowerBound(Ys, group.Top - BorderReach);
            var lastRow = UpperBound(Ys, group.Bottom + BorderReach) - 1;
            for (var row = firstRow; row <= lastRow; row++) {
                if (Math.Abs(Ys[row] - group.Top) > BorderReach && Math.Abs(Ys[row] - group.Bottom) > BorderReach) continue;
                for (var column = firstColumn; column < lastColumn; column++) _borderRight[row * Width + column] = true;
            }

            for (var column = firstColumn; column <= lastColumn; column++) {
                if (Math.Abs(Xs[column] - group.Left) > BorderReach && Math.Abs(Xs[column] - group.Right) > BorderReach) continue;
                for (var row = firstRow; row < lastRow; row++) _borderDown[row * Width + column] = true;
            }
        }

        /// <summary>Returns true when a move runs along a group border.</summary>
        public bool AlongBorder(int from, int to, int direction) => direction switch {
            0 => _borderRight[from],
            1 => _borderRight[to],
            2 => _borderDown[from],
            _ => _borderDown[to]
        };

        /// <summary>Returns the grid point at the given position, or -1 when no grid lines meet there.</summary>
        public int Cell(ChartPoint point) {
            var column = Array.BinarySearch(Xs, point.X);
            var row = Array.BinarySearch(Ys, point.Y);
            return column < 0 || row < 0 ? -1 : row * Width + column;
        }

        public bool CanMove(int from, int to, int direction) => direction switch {
            0 => !_noRight[from],
            1 => !_noRight[to],
            2 => !_noDown[from],
            _ => !_noDown[to]
        };

        /// <summary>Returns how many earlier routes run along the grid segment of a move.</summary>
        public int SharedRoutes(int from, int to, int direction) => direction switch {
            0 => _usedRight[from],
            1 => _usedRight[to],
            2 => _usedDown[from],
            _ => _usedDown[to]
        };

        /// <summary>Returns how many earlier routes pass through a grid point across the given direction of travel.</summary>
        public int CrossingsAt(int cell, int direction) {
            if (direction < 2) {
                var above = cell >= Width ? _usedDown[cell - Width] : 0;
                return Math.Min(MaximumSharedRoutes, Math.Max(above, _usedDown[cell]));
            }

            var before = cell % Width > 0 ? _usedRight[cell - 1] : 0;
            return Math.Min(MaximumSharedRoutes, Math.Max(before, _usedRight[cell]));
        }

        public void MarkUsed(List<int> cells) {
            for (var i = 0; i + 1 < cells.Count; i++) {
                var from = Math.Min(cells[i], cells[i + 1]);
                if (Width > 1 && Math.Abs(cells[i] - cells[i + 1]) == 1) _usedRight[from]++;
                else _usedDown[from]++;
            }
        }

        public void BeginSearch() => _search++;

        public double CostOf(int state) => _stamp[state] == _search ? _cost[state] : double.PositiveInfinity;

        public int PreviousOf(int state) => _stamp[state] == _search ? _previous[state] : -1;

        public void SetCost(int state, double cost, int previous) {
            _stamp[state] = _search;
            _cost[state] = cost;
            _previous[state] = previous;
        }

        private static int LowerBound(double[] values, double value) {
            var index = Array.BinarySearch(values, value);
            return index >= 0 ? index : ~index;
        }

        private static int UpperBound(double[] values, double value) {
            var index = Array.BinarySearch(values, value);
            return index >= 0 ? index + 1 : ~index;
        }
    }

    /// <summary>A small binary min-heap; framework priority queues are unavailable on net472 and netstandard2.0.</summary>
    private sealed class MinHeap {
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
