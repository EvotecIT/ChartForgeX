using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Topology;

public sealed partial class TopologyCellLayout {
    private static TopologyCellLayout RouteCellEdges(string? title, TopologyChart chart, List<TopologyCellNode> nodes,
        List<TopologyCellGroup> groups, List<TopologyCellEdge> edges, TopologyCellLayoutOptions options, string[] automaticEdges) {
        var columns = Math.Max(1, nodes.Cast<TopologyCellBox>().Concat(groups).Select(box => box.Column + box.Width + 3).DefaultIfEmpty(1).Max());
        var rows = Math.Max(1, nodes.Cast<TopologyCellBox>().Concat(groups).Select(box => box.Row + box.Height + 3).DefaultIfEmpty(1).Max());
        var boxes = nodes.Cast<TopologyCellBox>().Concat(groups).ToDictionary(box => box.Id, StringComparer.Ordinal);
        var occupied = new Dictionary<TopologyCellPoint, string>();
        var routed = new List<TopologyCellEdge>();
        foreach (var edge in edges) {
            var model = chart.Edges.First(item => item.Id == edge.Id);
            if (!boxes.TryGetValue(edge.SourceNodeId, out var source) || !boxes.TryGetValue(edge.TargetNodeId, out var target))
                throw new InvalidOperationException("A terminal edge endpoint is absent: " + edge.Id);
            if (!automaticEdges.Contains(edge.Id)) { routed.Add(edge); continue; }
            var route = FindCellRoute(source, target, nodes, groups, occupied, columns, rows, chart.LayoutDirection,
                routed.Count(item => item.SourceNodeId == source.Id), edges.Count(item => item.SourceNodeId == source.Id),
                routed.Count(item => item.TargetNodeId == target.Id), edges.Count(item => item.TargetNodeId == target.Id));
            if (route == null) {
                var projected = edge.Points.ToList();
                if (CellRouteIsClear(projected, source, target, nodes, groups)) route = projected;
                else throw new InvalidOperationException("No clear terminal route was found within the search budget for edge '" + edge.Id + "'.");
            }
            foreach (var point in CellPath(route)) occupied[point] = source.Id;
            var onLine = false;
            var labelAt = options.IncludeEdgeLabels && !string.IsNullOrWhiteSpace(model.Label) ? CellLabelPosition(route, model.Label!, out onLine) : null;
            routed.Add(new TopologyCellEdge(model, edge.Dashed, route, labelAt, onLine));
        }
        return FitCellCanvas(title, chart, nodes, groups, routed);
    }

    private static List<TopologyCellPoint>? FindCellRoute(TopologyCellBox source, TopologyCellBox target,
        List<TopologyCellNode> nodes, List<TopologyCellGroup> groups, Dictionary<TopologyCellPoint, string> occupied, int columns, int rows, TopologyLayoutDirection flow, int sourceIndex, int sourceCount, int targetIndex, int targetCount) {
        const int maximumGridCells = 250000;
        if ((long)columns * rows > maximumGridCells || source.Id == target.Id) return null;
        var budget = Math.Min(50000, 2000000 / Math.Max(1, nodes.Count + groups.Count));
        // Dijkstra/A* uses the shared native router's heap; the grid and its obstacles are terminal cells.
        var queue = new TopologyDenseRoutePlanner.MinHeap();
        var costs = new Dictionary<int, double>();
        var parents = new Dictionary<int, int>();
        var starts = new Dictionary<int, TopologyCellPoint>();
        var ends = Ports(target, targetIndex, targetCount).Where(port => flow != TopologyLayoutDirection.TopToBottom || target is TopologyCellGroup || target is TopologyCellNode { GroupId: not null } || port.Border.Row == (target.Row > source.Row ? target.Row : target.Row + target.Height - 1))
            .Where(port => flow != TopologyLayoutDirection.BottomToTop || target is TopologyCellGroup || target is TopologyCellNode { GroupId: not null } || port.Border.Row == (target.Row < source.Row ? target.Row + target.Height - 1 : target.Row))
            .ToDictionary(port => port.Outside, port => (port.Border, port.Direction));
        foreach (var port in Ports(source, sourceIndex, sourceCount)) {
            if (!Allowed(port.Outside) || !Transition(port.Border, port.Outside)) continue;
            var state = State(port.Outside, port.Direction);
            var cost = Distance(port.Outside, new TopologyCellPoint(target.Column + target.Width / 2, target.Row + target.Height / 2));
            // Facing sides win ties, while other sides remain available around group titles and obstacles.
            var initial = cost * .05 + (Preferred(port.Direction, source, target) ? 0 : 24);
            costs[state] = initial; parents[state] = -1; starts[state] = port.Border;
            queue.Push(state, initial + Heuristic(port.Outside), initial);
        }
        while (queue.Count > 0 && budget-- > 0) {
            queue.Pop(out var state, out _, out var cost);
            if (!costs.TryGetValue(state, out var best) || best != cost) continue;
            var cell = Point(state); var direction = state % 4;
            if (ends.TryGetValue(cell, out var end) && direction == (end.Direction + 2) % 4 && Transition(cell, end.Border)) {
                var path = new List<TopologyCellPoint> { end.Border, cell };
                var cursor = state;
                while (parents[cursor] >= 0) { cursor = parents[cursor]; path.Add(Point(cursor)); }
                path.Add(starts[cursor]); path.Reverse();
                return Orthogonalize(path);
            }
            for (var nextDirection = 0; nextDirection < 4; nextDirection++) {
                var next = new TopologyCellPoint(cell.Column + (nextDirection == 0 ? 1 : nextDirection == 2 ? -1 : 0),
                    cell.Row + (nextDirection == 1 ? 1 : nextDirection == 3 ? -1 : 0));
                if (!Allowed(next) || !Transition(cell, next)) continue;
                var extra = occupied.TryGetValue(next, out var owner) && owner != source.Id ? 12 : 0;
                var nextCost = cost + 1 + (direction == nextDirection ? 0 : 3) + extra;
                var nextState = State(next, nextDirection);
                if (costs.TryGetValue(nextState, out var previous) && previous <= nextCost) continue;
                costs[nextState] = nextCost; parents[nextState] = state;
                queue.Push(nextState, nextCost + Heuristic(next), nextCost);
            }
        }
        return null;

        bool Preferred(int direction, TopologyCellBox from, TopologyCellBox to) {
            if (flow == TopologyLayoutDirection.TopToBottom) return direction == 1;
            if (flow == TopologyLayoutDirection.BottomToTop) return direction == 3;
            return direction == (to.Column >= from.Column ? 0 : 2);
        }
        int State(TopologyCellPoint p, int direction) => (p.Row * columns + p.Column) * 4 + direction;
        TopologyCellPoint Point(int state) => new(state / 4 % columns, state / 4 / columns);
        double Heuristic(TopologyCellPoint p) => Math.Max(0, Distance(p, new(target.Column + target.Width / 2, target.Row + target.Height / 2)) - Math.Max(target.Width, target.Height));
        bool Allowed(TopologyCellPoint p) => p.Column >= 0 && p.Row >= 0 && p.Column < columns && p.Row < rows
            && !nodes.Any(node => node.Contains(p.Column, p.Row));
        bool Transition(TopologyCellPoint a, TopologyCellPoint b) {
            foreach (var group in groups) {
                var sourceInside = group.Contains(source.Column, source.Row);
                var targetInside = group.Contains(target.Column, target.Row);
                var insideA = Interior(group, a); var insideB = Interior(group, b);
                if (sourceInside && targetInside && (!group.Contains(b.Column, b.Row))) return false;
                if (!sourceInside && !targetInside && group.Contains(b.Column, b.Row)) return false;
                if (sourceInside && !targetInside && !insideA && insideB) return false;
                if (!sourceInside && targetInside && insideA && !insideB) return false;
                var aBorder = group.Contains(a.Column, a.Row) && !insideA;
                var bBorder = group.Contains(b.Column, b.Row) && !insideB;
                if (aBorder && bBorder) return false;
                // The complete top row belongs to the group caption; enter at the sides or bottom.
                if (bBorder && (b.Row == group.Row || b.Column == group.Column && (b.Row == group.Row + group.Height - 1))) return false;
            }
            return true;
        }
    }

    private static bool Interior(TopologyCellBox box, TopologyCellPoint p) => p.Column > box.Column && p.Column < box.Column + box.Width - 1
        && p.Row > box.Row && p.Row < box.Row + box.Height - 1;
    private static int Distance(TopologyCellPoint a, TopologyCellPoint b) => Math.Abs(a.Column - b.Column) + Math.Abs(a.Row - b.Row);
    private static IEnumerable<(TopologyCellPoint Border, TopologyCellPoint Outside, int Direction)> Ports(TopologyCellBox node, int index, int count) {
        var x = node.Column + 1 + (index + 1) * (node.Width - 2) / (count + 1);
        var y = node.Row + 1 + (index + 1) * (node.Height - 2) / (count + 1);
        yield return (new(node.Column + node.Width - 1, y), new(node.Column + node.Width, y), 0);
        yield return (new(x, node.Row + node.Height - 1), new(x, node.Row + node.Height), 1);
        yield return (new(node.Column, y), new(node.Column - 1, y), 2);
        yield return (new(x, node.Row), new(x, node.Row - 1), 3);
    }

    private static TopologyCellPoint? CellLabelPosition(List<TopologyCellPoint> route, string label, out bool onLine) {
        var width = Terminal.TerminalTextWidth.Measure(label.Trim()) + 2;
        var runs = Enumerable.Range(0, route.Count - 1).Where(i => route[i].Row == route[i + 1].Row)
            .OrderByDescending(i => Math.Abs(route[i + 1].Column - route[i].Column));
        foreach (var i in runs) {
            if (Math.Abs(route[i + 1].Column - route[i].Column) < width + 4) continue;
            onLine = true;
            return new((route[i].Column + route[i + 1].Column - width) / 2, route[i].Row);
        }
        onLine = false;
        return VerticalLabelPosition(route) ?? route[route.Count / 2];
    }
}
