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
    private const double CrossingCost = 72;
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

    /// <summary>
    /// Joins two cards that face each other with one straight line when nothing stands between them. The grid search
    /// cannot find this route when the cards are so close that their stubs would pass each other. A direct route is
    /// recorded on the grid by the caller; routes that end up beside it are moved apart by the lane pass.
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
    /// <remarks>
    /// The search is written for speed but keeps the exact arithmetic and expansion order of the plain A*: every cost is
    /// the same expression evaluated in the same order, the heap orders states by priority and then state index, and the
    /// heuristic of a grid point is computed once per search instead of once per push. Routes are therefore unchanged.
    /// </remarks>
    private static PlannedRoute? Search(Grid grid, Request request, Dictionary<(TopologyNode Node, TopologyEdgePort Side), int> sideUse, double sharedRunCost = SharedRunCost) {
        double Penalty(Terminal terminal) => terminal.Penalty + (sideUse.TryGetValue((terminal.Node, terminal.Side), out var used) ? used * SideUseCost : 0);
        var starts = new Dictionary<int, Terminal>();
        foreach (var terminal in request.Starts) {
            var cell = grid.Cell(terminal.Stub);
            if (cell >= 0 && !grid.IsBlocked(cell) && !starts.ContainsKey(cell)) starts.Add(cell, terminal);
        }

        var ends = new Dictionary<int, Terminal>();
        foreach (var terminal in request.Ends) {
            var cell = grid.Cell(terminal.Stub);
            if (cell >= 0 && !grid.IsBlocked(cell) && !ends.ContainsKey(cell)) ends.Add(cell, terminal);
        }

        if (starts.Count == 0 || ends.Count == 0) return null;
        var search = grid.BeginSearch();
        var buffers = grid.Buffers;
        var endStamp = buffers.EndStamp;
        var endSlot = buffers.EndSlot;
        var heuristic = buffers.Heuristic;
        var heuristicStamp = buffers.HeuristicStamp;
        var endTerminals = new Terminal[ends.Count];
        var endX = new double[ends.Count];
        var endY = new double[ends.Count];
        var endHeuristicPenalty = new double[ends.Count];
        var endPenalty = new double[ends.Count];
        var endLength = new double[ends.Count];
        var endInward = new int[ends.Count];
        var slot = 0;
        foreach (var end in ends) {
            endStamp[end.Key] = search;
            endSlot[end.Key] = slot;
            endTerminals[slot] = end.Value;
            endX[slot] = end.Value.Stub.X;
            endY[slot] = end.Value.Stub.Y;
            endHeuristicPenalty[slot] = end.Value.Penalty;
            endPenalty[slot] = Penalty(end.Value);
            endLength[slot] = Length(end.Value.Port, end.Value.Stub);
            endInward[slot] = InwardDirection(end.Value.Side);
            slot++;
        }

        var width = grid.Width;
        var height = grid.Height;
        var xs = grid.Xs;
        var ys = grid.Ys;

        // The smallest distance to any end plus that end's own penalty; computed once per grid point and search.
        double Heuristic(int cell) {
            if (heuristicStamp[cell] == search) return heuristic[cell];
            var x = xs[cell % width];
            var y = ys[cell / width];
            var best = double.PositiveInfinity;
            for (var i = 0; i < endX.Length; i++) best = Math.Min(best, Math.Abs(endX[i] - x) + Math.Abs(endY[i] - y) + endHeuristicPenalty[i]);
            heuristicStamp[cell] = search;
            heuristic[cell] = best;
            return best;
        }

        var queue = buffers.Queue;
        queue.Clear();
        foreach (var start in starts) {
            var state = start.Key * 4 + OutwardDirection(start.Value.Side);
            var cost = Penalty(start.Value) + Length(start.Value.Port, start.Value.Stub) + grid.CrossingsAt(start.Key, OutwardDirection(start.Value.Side)) * CrossingCost;
            if (cost >= grid.CostOf(state)) continue;
            grid.SetCost(state, cost, -1);
            queue.Push(state, cost + Heuristic(start.Key), cost);
        }

        var nodes = buffers.Nodes;
        var flags = grid.Flags;
        var usedRight = grid.UsedRight;
        var usedDown = grid.UsedDown;
        var crossRight = grid.CrossRight;
        var crossDown = grid.CrossDown;
        var gapX = grid.GapX;
        var gapY = grid.GapY;
        var goal = -1;
        var goalCost = double.PositiveInfinity;
        Terminal? goalTerminal = null;
        while (queue.Count > 0) {
            queue.Pop(out var state, out var priority, out var pushedCost);
            if (priority >= goalCost) break;
            // Every queued state has a cost of this search.
            var current = nodes[state].Cost;
            if (pushedCost > current) continue;
            var cell = state >> 2;
            var direction = state & 3;
            if (endStamp[cell] == search) {
                var end = endSlot[cell];
                // Arriving straight into the card costs nothing; any other approach needs one more bend.
                var arrival = current + endPenalty[end] + endLength[end] + (direction == endInward[end] ? 0 : BendPenalty);
                if (arrival < goalCost) {
                    goalCost = arrival;
                    goal = state;
                    goalTerminal = endTerminals[end];
                }
            }

            var row = cell / width;
            var column = cell - row * width;
            var cellFlags = flags[cell];
            // Directions: 0 right, 1 left, 2 down, 3 up. A move never reverses the direction of arrival.
            for (var next = 0; next < 4; next++) {
                if ((next ^ 1) == direction) continue;
                int nextCell, shared, crossings, fixedCrossings;
                double length;
                bool border;
                switch (next) {
                    case 0:
                        if (column + 1 >= width || (cellFlags & Grid.NoRightFlag) != 0) continue;
                        nextCell = cell + 1;
                        if ((flags[nextCell] & Grid.BlockedFlag) != 0) continue;
                        length = gapX[column];
                        shared = usedRight[cell];
                        border = (cellFlags & Grid.BorderRightFlag) != 0;
                        crossings = Math.Min(MaximumSharedRoutes, Math.Max(row > 0 ? usedDown[nextCell - width] : 0, usedDown[nextCell]));
                        fixedCrossings = Math.Min(MaximumSharedRoutes, crossRight[cell]);
                        break;
                    case 1:
                        if (column == 0) continue;
                        nextCell = cell - 1;
                        var leftFlags = flags[nextCell];
                        if ((leftFlags & (Grid.BlockedFlag | Grid.NoRightFlag)) != 0) continue;
                        length = gapX[column - 1];
                        shared = usedRight[nextCell];
                        border = (leftFlags & Grid.BorderRightFlag) != 0;
                        crossings = Math.Min(MaximumSharedRoutes, Math.Max(row > 0 ? usedDown[nextCell - width] : 0, usedDown[nextCell]));
                        fixedCrossings = Math.Min(MaximumSharedRoutes, crossRight[nextCell]);
                        break;
                    case 2:
                        if (row + 1 >= height || (cellFlags & Grid.NoDownFlag) != 0) continue;
                        nextCell = cell + width;
                        if ((flags[nextCell] & Grid.BlockedFlag) != 0) continue;
                        length = gapY[row];
                        shared = usedDown[cell];
                        border = (cellFlags & Grid.BorderDownFlag) != 0;
                        crossings = Math.Min(MaximumSharedRoutes, Math.Max(column > 0 ? usedRight[nextCell - 1] : 0, usedRight[nextCell]));
                        fixedCrossings = Math.Min(MaximumSharedRoutes, crossDown[cell]);
                        break;
                    default:
                        if (row == 0) continue;
                        nextCell = cell - width;
                        var upFlags = flags[nextCell];
                        if ((upFlags & (Grid.BlockedFlag | Grid.NoDownFlag)) != 0) continue;
                        length = gapY[row - 1];
                        shared = usedDown[nextCell];
                        border = (upFlags & Grid.BorderDownFlag) != 0;
                        crossings = Math.Min(MaximumSharedRoutes, Math.Max(column > 0 ? usedRight[nextCell - 1] : 0, usedRight[nextCell]));
                        fixedCrossings = Math.Min(MaximumSharedRoutes, crossDown[nextCell]);
                        break;
                }

                var step = length * (1 + sharedRunCost * Math.Min(MaximumSharedRoutes, shared) + (border ? BorderRunCost : 0))
                    + (next == direction ? 0 : BendPenalty)
                    + (crossings + fixedCrossings) * CrossingCost;
                var nextState = nextCell * 4 + next;
                var total = current + step;
                ref var node = ref nodes[nextState];
                if (node.Stamp == search && total >= node.Cost) continue;
                node.Stamp = search;
                node.Cost = total;
                node.Previous = state;
                // goalCost only falls, and the search stops at the first entry whose priority reaches it, so an entry at
                // or above it now would never be expanded; leaving it out of the queue changes nothing else.
                var nextPriority = total + Heuristic(nextCell);
                if (nextPriority < goalCost) queue.Push(nextState, nextPriority, total);
            }
        }

        if (goal < 0 || goalTerminal == null) return null;
        var cells = new List<int>();
        for (var state = goal; state >= 0; state = grid.PreviousOf(state)) cells.Add(state / 4);
        cells.Reverse();
        var startTerminal = starts[cells[0]];
        var points = new List<ChartPoint>(cells.Count + 2) { startTerminal.Port };
        foreach (var cell in cells) points.Add(new ChartPoint(xs[cell % width], ys[cell / width]));
        points.Add(goalTerminal.Port);
        return new PlannedRoute(request, startTerminal, goalTerminal, Simplify(points));
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

}
