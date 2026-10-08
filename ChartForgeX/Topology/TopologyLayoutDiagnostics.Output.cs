using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Topology;

public static partial class TopologyLayoutDiagnostics {
    /// <summary>Projects detached content diagnostics through the resolved common-frame fit without changing layout or routing.</summary>
    internal static TopologyLayoutDiagnosticReport AnalyzeOutput(TopologyChart chart, TopologyRenderOptions options,
        VisualSize size, double scale, double offsetX, double offsetY) {
        var layout = AnalyzePrepared(chart, options);
        var report = new TopologyLayoutDiagnosticReport(size.Width, size.Height, layout.LayoutMode, layout.LayoutDirection);
        ChartPoint Point(ChartPoint point) => new(offsetX + point.X * scale, offsetY + point.Y * scale);
        ChartRect Bounds(ChartRect bounds) => new(offsetX + bounds.X * scale, offsetY + bounds.Y * scale,
            bounds.Width * scale, bounds.Height * scale);
        ChartRect? OptionalBounds(ChartRect? bounds) => bounds.HasValue ? Bounds(bounds.Value) : null;

        foreach (var group in layout.Groups)
            report.Groups.Add(new TopologyLayoutGroupDiagnostic(group.Id, Bounds(group.Bounds), OptionalBounds(group.HeaderBounds)));
        foreach (var node in layout.Nodes) {
            var transformed = new TopologyLayoutNodeDiagnostic(node.Id, Bounds(node.Bounds), OptionalBounds(node.CaptionBounds));
            foreach (var port in node.Ports)
                transformed.Ports.Add(new TopologyLayoutPortDiagnostic(port.Id, port.Side, Point(port.Position)));
            report.Nodes.Add(transformed);
        }
        foreach (var edge in layout.Edges) {
            report.Edges.Add(new TopologyLayoutEdgeDiagnostic(edge.Id, edge.SourceNodeId, edge.TargetNodeId,
                edge.Points.Select(Point).ToArray(), edge.Strategy, edge.Corridor, edge.ObstacleHits, edge.LabelObstacleHits,
                edge.RouteOverlapScore, edge.FallbackReason) { SourceAttached = edge.SourceAttached, TargetAttached = edge.TargetAttached });
        }
        foreach (var collision in layout.Collisions)
            report.Collisions.Add(new TopologyLayoutCollisionDiagnostic(collision.Kind, collision.FirstId, collision.SecondId, Bounds(collision.Bounds)));
        foreach (var crossing in layout.RouteCrossings)
            report.RouteCrossings.Add(new TopologyLayoutRouteCrossingDiagnostic(crossing.Kind, crossing.EdgeId, crossing.ObstacleId));
        foreach (var overlap in layout.RouteOverlaps)
            report.RouteOverlaps.Add(new TopologyLayoutRouteOverlapDiagnostic(overlap.FirstEdgeId, overlap.SecondEdgeId, overlap.Length * scale, overlap.IsIntentional));
        foreach (var label in layout.EdgeLabels)
            report.EdgeLabels.Add(new TopologyLayoutEdgeLabelDiagnostic(label.EdgeId, Bounds(label.Bounds), label.RouteDistance * scale));
        return report;
    }
}
