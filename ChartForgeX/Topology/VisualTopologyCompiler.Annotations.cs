using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    private readonly List<(TopologyAnnotation Note, ChartRect Bounds, IReadOnlyList<ChartPoint> Leader, IReadOnlyList<string> Lines)> _annotations = new();

    /// <summary>Measures notes after graph layout, reserving independent gutters without introducing graph facts.</summary>
    private void PrepareAnnotations() {
        if (_chart.Annotations.Count == 0) return;
        var nodes = _chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var groups = _chart.Groups.ToDictionary(group => group.Id, StringComparer.Ordinal);
        var ids = new HashSet<string>(_chart.Nodes.Select(node => node.Id).Concat(_chart.Groups.Select(group => group.Id)).Concat(_chart.Edges.Select(edge => edge.Id)), StringComparer.Ordinal);
        var graph = _chart.Nodes.Select(node => new ChartRect(node.X, node.Y, node.Width, node.Height))
            .Concat(_chart.Groups.Select(group => new ChartRect(group.X, group.Y, group.Width, group.Height))).ToArray();
        var left = graph.Length == 0 ? _chart.Viewport.Padding : graph.Min(rect => rect.Left);
        var right = graph.Length == 0 ? _chart.Viewport.Padding : graph.Max(rect => rect.Right);
        var top = graph.Length == 0 ? _chart.Viewport.Padding : graph.Min(rect => rect.Top);
        var bottoms = new Dictionary<TopologyAnnotationSide, double>();
        var size = _context.Theme.Typography.DataLabelSize;
        var lineHeight = _builder.MeasureText("Ag", size, 400).LineHeight;
        foreach (var note in _chart.Annotations) {
            if (!ids.Add(note.Id)) throw new ArgumentException("Duplicate topology annotation id '" + note.Id + "'.");
            ChartRect? target = null;
            if (note.TargetId != null) {
                if (nodes.TryGetValue(note.TargetId, out var node)) target = new ChartRect(node.X, node.Y, node.Width, node.Height);
                else if (groups.TryGetValue(note.TargetId, out var group)) target = new ChartRect(group.X, group.Y, group.Width, group.Height);
                else if (_options.View != null && (_source.Nodes.Exists(item => item.Id == note.TargetId) || _source.Groups.Exists(item => item.Id == note.TargetId))) continue;
                else throw new ArgumentException("Topology annotation '" + note.Id + "' references unknown node or group '" + note.TargetId + "'.");
            }
            var paragraphs = note.Text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            var textWidth = Math.Max(1, paragraphs.Max(line => _builder.MeasureText(line, size, 400).Width));
            var width = Math.Min(320, Math.Max(140, textWidth + 24));
            var lines = new List<string>();
            foreach (var paragraph in paragraphs) {
                var text = paragraph;
                if (text.Length == 0) { lines.Add(""); continue; }
                while (text.Length > 0) {
                    var lower = 1; var upper = text.Length;
                    while (lower < upper) {
                        var candidate = lower + (upper - lower + 1) / 2;
                        if (_builder.MeasureText(text.Substring(0, candidate), size, 400).Width <= width - 24) lower = candidate;
                        else upper = candidate - 1;
                    }
                    var length = lower;
                    if (length < text.Length && char.IsHighSurrogate(text[length - 1]) && char.IsLowSurrogate(text[length])) length++;
                    if (length < text.Length) {
                        var space = text.LastIndexOf(' ', length - 1, length);
                        if (space > 0) length = space;
                    }
                    lines.Add(text.Substring(0, length));
                    text = text.Substring(length).TrimStart();
                }
            }
            var height = Math.Max(lineHeight, lines.Count * lineHeight) + 20;
            var y = target?.Y ?? top;
            if (bottoms.TryGetValue(note.Side, out var bottom)) y = Math.Max(y, bottom + 16);
            var x = note.Side == TopologyAnnotationSide.Left ? left - width - 36 : right + 36;
            var bounds = new ChartRect(x, y, width, height);
            IReadOnlyList<ChartPoint> leader = Array.Empty<ChartPoint>();
            if (target.HasValue) {
                var source = new TopologyNode { Id = note.TargetId!, X = target.Value.X, Y = target.Value.Y,
                    Width = target.Value.Width, Height = target.Value.Height, Shape = TopologyNodeShape.Rectangle };
                var destination = new TopologyNode { Id = note.Id, X = x, Y = y, Width = width, Height = height, Shape = TopologyNodeShape.Rectangle };
                var edge = new TopologyEdge { Id = note.Id + "-leader", SourceNodeId = source.Id, TargetNodeId = note.Id,
                    Routing = TopologyEdgeRouting.ObstacleAvoidingOrthogonal,
                    TargetPort = note.Side == TopologyAnnotationSide.Left ? TopologyEdgePort.Right : TopologyEdgePort.Left };
                // Route against existing node/header obstacles without publishing the note as a graph node or edge.
                leader = TopologyEdgeRouter.Route(_chart, edge, source, destination).Points;
            }
            _annotations.Add((note, bounds, leader, lines));
            bottoms[note.Side] = bounds.Bottom;
        }
    }

    private ChartRect IncludeAnnotationBounds(ChartRect content) {
        if (_annotations.Count == 0) return content;
        var points = _annotations.SelectMany(note => note.Leader.Concat(new[] {
            new ChartPoint(note.Bounds.Left, note.Bounds.Top), new ChartPoint(note.Bounds.Right, note.Bounds.Bottom) })).ToArray();
        var left = Math.Min(content.Left, points.Min(point => point.X) - 1);
        var top = Math.Min(content.Top, points.Min(point => point.Y) - 1);
        var right = Math.Max(content.Right, points.Max(point => point.X) + 1);
        var bottom = Math.Max(content.Bottom, points.Max(point => point.Y) + 1);
        return new ChartRect(left, top, right - left, bottom - top);
    }

    private void BuildAnnotations() {
        var size = _context.Theme.Typography.DataLabelSize;
        var lineHeight = _builder.MeasureText("Ag", size * _scale, 400).LineHeight;
        foreach (var annotation in _annotations) {
            var note = annotation.Note;
            var bounds = Bounds(annotation.Bounds.X, annotation.Bounds.Y, annotation.Bounds.Width, annotation.Bounds.Height);
            using var group = _builder.PushGroup(note.Id, "topology-annotation", new Dictionary<string, string> {
                ["data-annotation-id"] = note.Id, ["data-target-id"] = note.TargetId ?? "", ["data-annotation-side"] = note.Side.ToString()
            });
            if (annotation.Leader.Count > 1) {
                var points = annotation.Leader.Select(Point).ToArray();
                var path = new ChartPath(points.Select((point, index) => index == 0 ? ChartPathCommand.MoveTo(point.X, point.Y) : ChartPathCommand.LineTo(point.X, point.Y)).ToArray());
                _builder.Path(path, stroke: _colors.MutedForeground,
                    strokeWidth: _context.Theme.AxisStrokeWidth * _scale, role: "topology-annotation-leader", dash: new[] { 4 * _scale, 3 * _scale },
                    paint: new VisualScenePaintBinding(stroke: MutedPaint(_colors.MutedForeground)));
            }
            _builder.Rect(bounds, _colors.Surface, _colors.Border, _context.Theme.AxisStrokeWidth * _scale, 0,
                "topology-annotation-surface", paint: new VisualScenePaintBinding(CardPaint(_colors.Surface), BorderPaint(_colors.Border)));
            var baseline = bounds.Y + 10 * _scale + _builder.TextAscent(size * _scale, 400);
            foreach (var line in annotation.Lines) {
                if (line.Length > 0) _builder.Text(line, bounds.X + 12 * _scale, baseline, size * _scale, _colors.Foreground, 400,
                    "topology-annotation-text", paint: ForegroundPaint(_colors.Foreground));
                baseline += lineHeight;
            }
            _builder.AddRegion(new VisualSemanticRegion(note.Id, "topology-annotation", bounds, note.Text));
        }
    }
}
