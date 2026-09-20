using System;
using System.Linq;
using ChartForgeX.Interactivity;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlGraphExplorerRenderer {
    private static void ApplyStaticNeighborhoodLayout(GraphScene scene, string rootNodeId) {
        // Authored positions and routes are a document contract. Only lay out views
        // whose geometry is entirely generated, without touching the source scene.
        if (scene.Nodes.Any(node => node.HasExplicitPosition) || scene.Edges.Any(edge => edge.RoutePoints.Count > 0)) return;
        var root = scene.Nodes.Single(node => node.Id == rootNodeId);
        root.X = Width / 2;
        root.Y = Height / 2;
        var neighbors = scene.Nodes.Where(node => node.Id != rootNodeId).OrderBy(node => node.Id, StringComparer.Ordinal).ToArray();
        var spacing = Math.Max(140, neighbors.Select(node => PreparedNodeHalfWidth(node, true) * 2 + 24).DefaultIfEmpty(140).Max());
        for (var offset = 0; offset < neighbors.Length; offset += 12) {
            var count = Math.Min(12, neighbors.Length - offset);
            var ring = offset / 12 + 1;
            var radius = spacing * ring * 2;
            for (var index = 0; index < count; index++) {
                var angle = -Math.PI / 2 + index * Math.PI * 2 / count;
                neighbors[offset + index].X = root.X + Math.Cos(angle) * radius;
                neighbors[offset + index].Y = root.Y + Math.Sin(angle) * radius;
            }
        }
    }
}
