using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    private readonly List<(TopologyPathDecoration Source, ChartPath Path, ChartRect Bounds)> _pathDecorations = new();

    /// <summary>Captures bounded independent ink after layout without inserting synthetic graph entities.</summary>
    private void PreparePathDecorations() {
        PrepareAuthoredEdgePaths();
        if (_chart.PathDecorations.Count == 0) return;
        if (_chart.PathDecorations.Count > TopologyPathDecoration.MaximumPaths) throw new ArgumentException("Topology path decorations exceed 1024 paths.");
        var ids = new HashSet<string>(_chart.Nodes.Select(node => node.Id).Concat(_chart.Groups.Select(group => group.Id))
            .Concat(_chart.Edges.Select(edge => edge.Id)).Concat(_chart.Annotations.Select(note => note.Id)), StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(_chart.Id)) ids.Add(_chart.Id!);
        var count = _chart.Edges.Where(edge => edge.PathGeometry != null).Sum(edge => edge.PathGeometry!.Commands.Count);
        foreach (var item in _chart.PathDecorations) {
            if (item == null) throw new ArgumentException("Topology path decorations cannot contain null.");
            if (!ids.Add(item.Id)) throw new ArgumentException("Duplicate topology path decoration id '" + item.Id + "'.");
            var path = item.Capture();
            count += path.Commands.Count;
            if (count > TopologyPathDecoration.MaximumTotalCommands) throw new ArgumentException("Topology path decorations exceed 65536 commands.");
            var bounds = path.Bounds();
            var half = item.Stroke.HasValue ? item.StrokeWidth / 2 : 0;
            _pathDecorations.Add((item, path, new ChartRect(bounds.X - half, bounds.Y - half,
                bounds.Width + half * 2, bounds.Height + half * 2)));
        }
    }

    private ChartRect IncludePathDecorationBounds(ChartRect content) {
        content = IncludeAuthoredEdgePathBounds(content);
        content=IncludeSurfaceStrokeBounds(content);
        content=IncludeExternalGlyphBounds(content);
        if (_pathDecorations.Count == 0) return content;
        var left = content.Left; var top = content.Top; var right = content.Right; var bottom = content.Bottom;
        foreach (var item in _pathDecorations) {
            left = Math.Min(left, item.Bounds.Left); top = Math.Min(top, item.Bounds.Top);
            right = Math.Max(right, item.Bounds.Right); bottom = Math.Max(bottom, item.Bounds.Bottom);
        }
        return new ChartRect(left, top, right - left, bottom - top);
    }

    private void BuildPathDecorations() {
        if (_pathDecorations.Count == 0) return;
        foreach (var item in _pathDecorations) {
            using var group = _builder.PushGroup(item.Source.Id, "topology-path-decoration",
                new Dictionary<string, string> { ["data-path-decoration-id"] = item.Source.Id });
            _builder.Path(Transform(item.Path), item.Source.Fill, item.Source.Stroke, item.Source.StrokeWidth * _scale,
                "topology-path-decoration-surface", close: item.Source.Close,
                dash: item.Source.DashPattern?.Select(length => length * _scale).ToArray());
        }
    }
}
