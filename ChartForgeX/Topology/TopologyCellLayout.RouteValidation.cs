using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Topology;

public sealed partial class TopologyCellLayout {
    private static bool CellRouteIsClear(List<TopologyCellPoint> route, TopologyCellBox source, TopologyCellBox target,
        List<TopologyCellNode> nodes, List<TopologyCellGroup> groups) {
        if (route.Count < 2 || !OnBorder(source, route[0]) || !OnBorder(target, route[route.Count - 1])) return false;
        long cells = 1;
        for (var index = 1; index < route.Count; index++) {
            cells += Math.Abs((long)route[index].Column - route[index - 1].Column) + Math.Abs((long)route[index].Row - route[index - 1].Row);
            if (cells > Math.Min(250000, 2000000 / Math.Max(1, nodes.Count + groups.Count))) return false;
            if (route[index].Column != route[index - 1].Column && route[index].Row != route[index - 1].Row) return false;
        }
        var path = CellPath(route);
        for (var index = 0; index < path.Count; index++) foreach (var node in nodes) {
            if (index == 0 && node.Id == source.Id || index == path.Count - 1 && node.Id == target.Id) continue;
            if (node.Contains(path[index].Column, path[index].Row)) return false;
        }
        foreach (var group in groups) {
            var expected = source is TopologyCellGroup && group.Id == source.Id || target is TopologyCellGroup && group.Id == target.Id || group.Contains(source.Column, source.Row) != group.Contains(target.Column, target.Row) ? 1 : 0;
            var border = path.Where(point => OnBorder(group, point)).ToArray();
            if (border.Length != expected || border.Any(point => point.Row == group.Row)) return false;
        }
        return true;
    }

    private static bool OnBorder(TopologyCellBox box, TopologyCellPoint point) => box.Contains(point.Column, point.Row)
        && (point.Column == box.Column || point.Column == box.Column + box.Width - 1 || point.Row == box.Row || point.Row == box.Row + box.Height - 1);
}
