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
        var radius = Math.Max(spacing * 2, spacing * neighbors.Length / (2 * Math.PI));
        for (var index = 0; index < neighbors.Length; index++) {
            var angle = -Math.PI / 2 + index * Math.PI * 2 / neighbors.Length;
            neighbors[index].X = root.X + Math.Cos(angle) * radius;
            neighbors[index].Y = root.Y + Math.Sin(angle) * radius;
        }
    }
}
