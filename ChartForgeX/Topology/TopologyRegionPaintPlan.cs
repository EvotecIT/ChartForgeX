using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Topology;

/// <summary>Frozen original-unit template placements and native text facts for one region preparation.</summary>
internal sealed class TopologyRegionPaintPlan {
    private readonly Action<Replay>[] _steps;
    internal VisualSize Size { get; }
    internal IReadOnlyDictionary<string, ChartRect> Labels { get; }
    internal TopologyRegionPaintPlan(VisualSize size, IEnumerable<Action<Replay>> steps, Dictionary<string, ChartRect> labels) {
        Size = size; _steps = steps.ToArray(); Labels = new ReadOnlyDictionary<string, ChartRect>(new Dictionary<string, ChartRect>(labels));
    }
    internal void Paint(VisualSceneBuilder builder, Func<ChartPoint, ChartPoint> point, double scale) {
        var replay = new Replay(builder, point, scale);
        try { foreach (var step in _steps) step(replay); }
        finally { while (replay.Scopes.Count > 0) replay.Scopes.Pop().Dispose(); }
    }
    internal sealed class Replay {
        internal readonly VisualSceneBuilder Builder;
        internal readonly Func<ChartPoint, ChartPoint> Point;
        internal readonly double Scale;
        internal readonly Stack<IDisposable> Scopes = new();
        internal Replay(VisualSceneBuilder builder, Func<ChartPoint, ChartPoint> point, double scale) { Builder = builder; Point = point; Scale = scale; }
        internal ChartRect Bounds(ChartRect bounds) {
            var origin = Point(new ChartPoint(bounds.X, bounds.Y));
            return new ChartRect(origin.X, origin.Y, bounds.Width * Scale, bounds.Height * Scale);
        }
    }
}

/// <summary>Records only the bounded native primitives emitted by the region templates and connector plan.</summary>
internal sealed class TopologyRegionPaintBuilder {
    private readonly VisualSceneBuilder _unitText;
    private readonly List<Action<TopologyRegionPaintPlan.Replay>> _steps = new();
    private int _depth;
    internal TopologyRegionPaintBuilder(VisualSceneBuilder unitText) => _unitText = unitText;
    internal TextMetrics MeasureText(string text, double size, int weight = 400) => _unitText.MeasureText(text, size, weight);
    internal double TextAscent(double size, int weight = 400) => _unitText.TextAscent(size, weight);
    internal void Rect(ChartRect bounds, ChartColor? fill, ChartColor? stroke = null, double strokeWidth = 1,
        double radius = 0, string? role = null, string? id = null, VisualScenePaintBinding? paint = null) =>
        _steps.Add(replay => replay.Builder.Rect(replay.Bounds(bounds), fill, stroke, strokeWidth * replay.Scale, radius * replay.Scale, role, id, paint));
    internal void Ellipse(double cx, double cy, double rx, double ry, ChartColor? fill, ChartColor? stroke = null,
        double strokeWidth = 1, string? role = null, string? id = null, VisualScenePaintBinding? paint = null) => _steps.Add(replay => {
            var center = replay.Point(new ChartPoint(cx, cy)); replay.Builder.Ellipse(center.X, center.Y, rx * replay.Scale, ry * replay.Scale, fill, stroke, strokeWidth * replay.Scale, role, id, paint);
        });
    internal void Line(double x1, double y1, double x2, double y2, ChartColor color, double width = 1, string? role = null,
        string? id = null, double[]? dash = null, VisualScenePaintBinding? paint = null) {
        var pattern = dash?.ToArray();
        _steps.Add(replay => { var a = replay.Point(new ChartPoint(x1, y1)); var b = replay.Point(new ChartPoint(x2, y2));
            replay.Builder.Line(a.X, a.Y, b.X, b.Y, color, width * replay.Scale, role, id, pattern?.Select(value => value * replay.Scale).ToArray(), paint); });
    }
    internal void Path(ChartPath path, ChartColor? fill = null, ChartColor? stroke = null, double strokeWidth = 1,
        string? role = null, string? id = null, bool close = false, double[]? dash = null, VisualScenePaintBinding? paint = null) {
        var commands = path.Commands.ToArray(); var pattern = dash?.ToArray();
        _steps.Add(replay => {
            var projected = commands.Select(command => {
                var end = replay.Point(new ChartPoint(command.X, command.Y));
                if (command.Kind == ChartPathCommandKind.MoveTo) return ChartPathCommand.MoveTo(end.X, end.Y);
                if (command.Kind == ChartPathCommandKind.LineTo) return ChartPathCommand.LineTo(end.X, end.Y);
                throw new InvalidOperationException("Region paths must contain their resolved line samples.");
            }).ToArray();
            replay.Builder.Path(new ChartPath(projected), fill, stroke, strokeWidth * replay.Scale, role, id, close,
                pattern?.Select(value => value * replay.Scale).ToArray(), paint: paint);
        });
    }
    internal void Text(string text, double x, double baseline, double size, ChartColor color, int weight = 400,
        string? role = null, string? id = null, TextAlignment alignment = TextAlignment.Left, SvgPaint? paint = null,
        ChartColor? stroke = null, double strokeWidth = 0, SvgPaint? strokePaint = null) {
        var prepared = _unitText.PrepareText(text, size, weight);
        _steps.Add(replay => { var origin = replay.Point(new ChartPoint(x, baseline));
            replay.Builder.PreparedText(prepared, origin.X, origin.Y, replay.Scale, color, role, id, alignment, paint, stroke, strokeWidth * replay.Scale, strokePaint); });
    }
    internal IDisposable PushGroup(string? id, string? role, IReadOnlyDictionary<string, string>? metadata = null) {
        var attributes = metadata == null ? null : new ReadOnlyDictionary<string, string>(metadata.ToDictionary(pair => pair.Key, pair => pair.Value));
        return Scope(replay => replay.Builder.PushGroup(id, role, attributes));
    }
    internal IDisposable PushTooltip(string text) => Scope(replay => replay.Builder.PushTooltip(text));
    internal IDisposable PushTranslation(double x, double y) => Scope(replay => replay.Builder.PushTranslation(x * replay.Scale, y * replay.Scale));
    private IDisposable Scope(Func<TopologyRegionPaintPlan.Replay, IDisposable> start) {
        _steps.Add(replay => replay.Scopes.Push(start(replay))); _depth++; return new EndScope(this);
    }
    internal TopologyRegionPaintPlan Freeze(VisualSize size, Dictionary<string, ChartRect> labels) {
        if (_depth != 0) throw new InvalidOperationException("Unbalanced region template scopes.");
        return new TopologyRegionPaintPlan(size, _steps, labels);
    }
    private sealed class EndScope : IDisposable {
        private TopologyRegionPaintBuilder? _owner;
        internal EndScope(TopologyRegionPaintBuilder owner) => _owner = owner;
        public void Dispose() { if (_owner == null) return; _owner._steps.Add(replay => replay.Scopes.Pop().Dispose()); _owner._depth--; _owner = null; }
    }
}
