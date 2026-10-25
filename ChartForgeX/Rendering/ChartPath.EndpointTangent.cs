using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal sealed partial class ChartPath {
    internal ChartPoint AdjacentToEndpoint(bool start) {
        var path = Commands;
        var end = start ? new ChartPoint(path[0].X, path[0].Y) : new ChartPoint(path[path.Count - 1].X, path[path.Count - 1].Y);
        if (start) {
            for (var i = 1; i < path.Count; i++) {
                var c = path[i];
                if (c.Kind == ChartPathCommandKind.CubicTo) {
                    var p = new ChartPoint(c.Control1X, c.Control1Y); if (!SameEndpointPoint(p, end)) return p;
                    p = new ChartPoint(c.Control2X, c.Control2Y); if (!SameEndpointPoint(p, end)) return p;
                }
                var next = new ChartPoint(c.X, c.Y); if (!SameEndpointPoint(next, end)) return next;
            }
        } else {
            for (var i = path.Count - 1; i > 0; i--) {
                var c = path[i];
                if (c.Kind == ChartPathCommandKind.CubicTo) {
                    var p = new ChartPoint(c.Control2X, c.Control2Y); if (!SameEndpointPoint(p, end)) return p;
                    p = new ChartPoint(c.Control1X, c.Control1Y); if (!SameEndpointPoint(p, end)) return p;
                }
                var previous = new ChartPoint(path[i - 1].X, path[i - 1].Y); if (!SameEndpointPoint(previous, end)) return previous;
            }
        }
        return end;
    }
    private static bool SameEndpointPoint(ChartPoint a, ChartPoint b) => a.X == b.X && a.Y == b.Y;
}
