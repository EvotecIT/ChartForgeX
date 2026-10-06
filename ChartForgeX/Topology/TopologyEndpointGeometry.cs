using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

/// <summary>Shared endpoint paths in a twenty by twelve unit box; the tip is at (20,6).</summary>
internal static class TopologyEndpointGeometry {
    internal static string? Path(TopologyMarkerKind kind) => kind switch {
        TopologyMarkerKind.OpenTriangle => "M 20 6 L 6 0 L 6 12 Z",
        TopologyMarkerKind.OpenDiamond => "M 20 6 L 12 1 L 4 6 L 12 11 Z",
        TopologyMarkerKind.ExactlyOne => "M 13 1 L 13 11 M 17 1 L 17 11",
        TopologyMarkerKind.ZeroOrOne => "M 17 1 L 17 11",
        TopologyMarkerKind.OneOrMany => "M 5 1 L 5 11 M 10 6 L 20 0 M 10 6 L 20 6 M 10 6 L 20 12",
        TopologyMarkerKind.ZeroOrMany => "M 10 6 L 20 0 M 10 6 L 20 6 M 10 6 L 20 12",
        _ => null
    };

    internal static bool HasCircle(TopologyMarkerKind kind) => kind is TopologyMarkerKind.ZeroOrOne or TopologyMarkerKind.ZeroOrMany;
    internal static bool IsClosed(TopologyMarkerKind kind) => kind is TopologyMarkerKind.OpenTriangle or TopologyMarkerKind.OpenDiamond;
    internal static ChartPoint Project(ChartPoint point, ChartPoint from, ChartPoint to) {
        var angle = Math.Atan2(to.Y - from.Y, to.X - from.X);
        var x = point.X - 20;
        var y = point.Y - 6;
        return new ChartPoint(to.X + Math.Cos(angle) * x - Math.Sin(angle) * y, to.Y + Math.Sin(angle) * x + Math.Cos(angle) * y);
    }
}
