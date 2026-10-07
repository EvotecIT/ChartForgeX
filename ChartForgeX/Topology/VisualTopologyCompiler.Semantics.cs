using ChartForgeX.Primitives;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    private VisualArtifactInterchangeEnvelope SemanticSnapshot(ChartForgeX.Accessibility.VisualAccessibility accessibility) {
        var snapshot = TopologyLayoutEngine.Clone(_chart);
        snapshot.Legend = _legend;
        snapshot.Title = HeadingOrSource(_context.Frame.Title, SourceTitle); snapshot.Subtitle = HeadingOrSource(_context.Frame.Subtitle, SourceSubtitle);
        snapshot.Viewport = new TopologyViewport { Width = _context.Layout.Size.Width, Height = _context.Layout.Size.Height, Padding = _context.Layout.Padding };
        foreach (var group in snapshot.Groups) {
            var b = Bounds(group.X, group.Y, group.Width, group.Height);
            group.X = b.X; group.Y = b.Y; group.Width = b.Width; group.Height = b.Height;
            PreserveCssClass(group.Metadata, group.CssClass);
        }
        foreach (var node in snapshot.Nodes) {
            var b = Bounds(node.X, node.Y, node.Width, node.Height);
            node.X = b.X; node.Y = b.Y; node.Width = b.Width; node.Height = b.Height;
            PreserveCssClass(node.Metadata, node.CssClass);
        }
        foreach (var edge in snapshot.Edges) {
            PreserveCssClass(edge.Metadata, edge.CssClass);
            for (var i = 0; i < edge.Waypoints.Count; i++) edge.Waypoints[i] = Point(edge.Waypoints[i]);
            if (edge.HasLabelAnchorOverride) {
                var p = Point(new ChartPoint(edge.LabelAnchorX, edge.LabelAnchorY)); edge.LabelAnchorX = p.X; edge.LabelAnchorY = p.Y;
            }
            if (edge.StrokeWidth.HasValue) edge.StrokeWidth *= _scale;
            edge.LabelOffsetX *= _scale; edge.LabelOffsetY *= _scale;
            for (var i = 0; i < edge.DashPattern.Count; i++) edge.DashPattern[i] *= _scale;
        }
        var envelope = VisualArtifactInterchangeMapping.FromPreparedTopology(snapshot, _options);
        for (var i = 0; i < _chart.Edges.Count; i++) {
            if (_resolvedLabelBounds.TryGetValue(_chart.Edges[i], out var labelBounds)) envelope.Edges[i].ResolvedLabelBounds = labelBounds;
            foreach (var point in _routes[_chart.Edges[i]]) {
                var p = Point(point);
                envelope.Edges[i].ResolvedRoute.Add(new VisualArtifactInterchangePoint { X = p.X, Y = p.Y });
            }
        }
        envelope.AccessibleName = accessibility.Name;
        envelope.AccessibleDescription = accessibility.Description;
        envelope.Extensions["chartforgex.geometry"] = "prepared-scene";
        return envelope;
    }

    private static void PreserveCssClass(System.Collections.Generic.Dictionary<string, string> metadata, string? cssClass) {
        if (!string.IsNullOrWhiteSpace(cssClass)) metadata["chartforgex.sourceCssClass"] = cssClass!;
    }
}
