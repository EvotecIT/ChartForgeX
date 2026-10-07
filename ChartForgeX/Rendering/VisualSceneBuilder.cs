using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

/// <summary>Collects native geometry and already positioned text without an SVG intermediate.</summary>
internal sealed class VisualSceneBuilder {
    private readonly VisualSize _size;
    private readonly FontSpec _font;
    private readonly List<VisualSceneNode> _nodes = new();
    private readonly Stack<GroupScope> _groups = new();
    private readonly Dictionary<int, VisualSceneTextFace> _faces = new();
    private readonly List<VisualDiagnostic> _diagnostics = new();
    private readonly List<VisualSemanticRegion> _regions = new();

    internal VisualSceneBuilder(VisualSize size, FontSpec font) {
        VisualSize.Positive(size.Width, nameof(size)); VisualSize.Positive(size.Height, nameof(size));
        _size = size;
        _font = (font ?? throw new ArgumentNullException(nameof(font))).Clone();
    }
    internal VisualSize Size => _size;

    internal void Rect(ChartRect bounds, ChartColor? fill, ChartColor? stroke = null, double strokeWidth = 1,
        double radius = 0, string? role = null, string? id = null) {
        ValidateRect(bounds); NonNegative(radius, nameof(radius)); NonNegative(strokeWidth, nameof(strokeWidth));
        _nodes.Add(new VisualSceneRectangle(bounds, Math.Min(radius, Math.Min(bounds.Width, bounds.Height) / 2), fill, stroke, strokeWidth, role, id));
    }

    internal void Line(double x1, double y1, double x2, double y2, ChartColor color, double width = 1,
        string? role = null, string? id = null, double[]? dash = null) {
        NonNegative(width, nameof(width));
        if (dash != null) foreach (var length in dash) {
            ChartGuards.Finite(length, nameof(dash));
            if (length <= 0) throw new ArgumentOutOfRangeException(nameof(dash), "Dash lengths must be positive.");
        }
        _nodes.Add(new VisualSceneLine(new ChartPoint(x1, y1), new ChartPoint(x2, y2), color, width, role, id, dash));
    }

    internal void Ellipse(double cx, double cy, double rx, double ry, ChartColor? fill, ChartColor? stroke = null,
        double strokeWidth = 1, string? role = null, string? id = null) {
        ChartGuards.Finite(cx, nameof(cx)); ChartGuards.Finite(cy, nameof(cy));
        NonNegative(rx, nameof(rx)); NonNegative(ry, nameof(ry)); NonNegative(strokeWidth, nameof(strokeWidth));
        _nodes.Add(new VisualSceneEllipse(cx, cy, rx, ry, fill, stroke, strokeWidth, role, id));
    }

    internal void Path(ChartPath path, ChartColor? fill = null, ChartColor? stroke = null, double strokeWidth = 1,
        string? role = null, string? id = null, bool close = false) {
        if (path == null) throw new ArgumentNullException(nameof(path));
        NonNegative(strokeWidth, nameof(strokeWidth));
        foreach (var command in path.Commands) {
            if (!Enum.IsDefined(typeof(ChartPathCommandKind), command.Kind)) throw new ArgumentException("Unknown path command.", nameof(path));
            ChartGuards.Finite(command.X, nameof(path)); ChartGuards.Finite(command.Y, nameof(path));
            ChartGuards.Finite(command.Control1X, nameof(path)); ChartGuards.Finite(command.Control1Y, nameof(path));
            ChartGuards.Finite(command.Control2X, nameof(path)); ChartGuards.Finite(command.Control2Y, nameof(path));
        }
        _nodes.Add(new VisualScenePath(path, close, fill, stroke, strokeWidth, role, id));
    }

    /// <summary>Adds a clockwise slice in radians, including a full-ring case without a radial seam.</summary>
    internal void Slice(double cx, double cy, double outerRadius, double innerRadius, double start, double sweep,
        ChartColor fill, ChartColor? stroke = null, double strokeWidth = 1, string? role = null, string? id = null) {
        ChartGuards.Finite(cx, nameof(cx)); ChartGuards.Finite(cy, nameof(cy)); ChartGuards.Finite(start, nameof(start));
        NonNegative(outerRadius, nameof(outerRadius)); NonNegative(innerRadius, nameof(innerRadius));
        NonNegative(sweep, nameof(sweep)); NonNegative(strokeWidth, nameof(strokeWidth));
        if (innerRadius > outerRadius) throw new ArgumentOutOfRangeException(nameof(innerRadius));
        if (sweep > Math.PI * 2 + 0.000001) throw new ArgumentOutOfRangeException(nameof(sweep));
        _nodes.Add(new VisualSceneSlice(cx, cy, outerRadius, innerRadius, start, Math.Min(sweep, Math.PI * 2), fill, stroke, strokeWidth, role, id));
    }

    internal TextMetrics MeasureText(string text, double size, int weight = 400) => Face(weight).Prepare(text, size).Metrics;
    internal TextMetrics MeasureText(string text, TextStyle style) => new VisualSceneTextFace(style).Prepare(text, style.EffectiveFontSize).Metrics;
    internal double TextAscent(double size, int weight = 400) => Face(weight).Ascent(size);
    internal double TextAscent(TextStyle style) => new VisualSceneTextFace(style).Ascent(style.EffectiveFontSize);

    internal void AddDiagnostic(VisualDiagnostic diagnostic) => _diagnostics.Add(diagnostic ?? throw new ArgumentNullException(nameof(diagnostic)));
    internal void AddRegion(VisualSemanticRegion region) => _regions.Add(region ?? throw new ArgumentNullException(nameof(region)));

    internal void Text(string text, double x, double baseline, double size, ChartColor color, int weight = 400,
        string? role = null, string? id = null, TextAlignment alignment = TextAlignment.Left) {
        ChartGuards.Finite(x, nameof(x)); ChartGuards.Finite(baseline, nameof(baseline));
        if (!Enum.IsDefined(typeof(TextAlignment), alignment)) throw new ArgumentOutOfRangeException(nameof(alignment));
        _nodes.Add(new VisualSceneText(Face(weight).Prepare(text, size), x, baseline, color, alignment, role, id));
    }

    internal void Text(string text, double x, double baseline, TextStyle style, string? role = null, string? id = null) {
        if (style == null) throw new ArgumentNullException(nameof(style));
        ChartGuards.Finite(x, nameof(x)); ChartGuards.Finite(baseline, nameof(baseline));
        var prepared = new VisualSceneTextFace(style).Prepare(text, style.EffectiveFontSize);
        baseline += style.Baseline == TextBaseline.Superscript ? -style.FontSize * 0.35
            : style.Baseline == TextBaseline.Subscript ? style.FontSize * 0.22 : 0;
        _nodes.Add(new VisualSceneText(prepared, x, baseline, style.Color, style.Alignment, role, id));
        AddTextDecorations(prepared, x, baseline, style);
    }

    internal IDisposable PushClip(ChartRect bounds) {
        ValidateRect(bounds);
        return OpenGroup(new VisualSceneGroup(null, null, bounds, null));
    }

    internal IDisposable PushGroup(string? id, string? role, IReadOnlyDictionary<string, string>? metadata = null) {
        if (metadata != null) foreach (var item in metadata) {
            // Native semantic attributes are data, never event handlers, style or markup.
            if (item.Key != "aria-label" && item.Key != "role" && (!item.Key.StartsWith("data-", StringComparison.Ordinal) || item.Key.Length <= 5)
                || item.Key == "data-cfx-role")
                throw new ArgumentException("Scene metadata must use data- attributes, aria-label or role, without overriding the scene role.", nameof(metadata));
            foreach (var c in item.Key) if (!(char.IsLetterOrDigit(c) || c == '-' || c == '_'))
                throw new ArgumentException("Scene metadata has an invalid attribute name.", nameof(metadata));
        }
        return OpenGroup(new VisualSceneGroup(role, id, null, metadata));
    }

    internal VisualScene Build() {
        if (_groups.Count != 0) throw new InvalidOperationException("Close all scene group scopes before preparing the scene.");
        return new VisualScene(_size, _nodes, _diagnostics, _regions);
    }

    private VisualSceneTextFace Face(int weight) {
        if (weight < 100 || weight > 900) throw new ArgumentOutOfRangeException(nameof(weight));
        if (!_faces.TryGetValue(weight, out var face)) {
            face = new VisualSceneTextFace(_font, weight);
            _faces.Add(weight, face);
        }
        return face;
    }

    private IDisposable OpenGroup(VisualSceneGroup group) {
        var scope = new GroupScope(this); _nodes.Add(group); _groups.Push(scope); return scope;
    }

    private void AddTextDecorations(VisualScenePreparedText text, double x, double baseline, TextStyle style) {
        var thickness = Math.Max(1, text.Size / 13);
        for (var i = 0; i < text.Lines.Count; i++) {
            var line = text.Lines[i];
            var left = x - (style.Alignment == TextAlignment.Center ? line.Width / 2 : style.Alignment == TextAlignment.Right ? line.Width : 0);
            var y = baseline + i * text.Metrics.LineHeight;
            AddDecoration(left, left + line.Width, y + Math.Max(2, text.Size * 0.12), thickness, style.UnderlineStyle, style.Color);
            AddDecoration(left, left + line.Width, y - text.Size * 0.3, thickness, style.StrikethroughStyle, style.Color);
        }
    }

    private void AddDecoration(double left, double right, double y, double thickness, TextDecorationStyle style, ChartColor color) {
        if (style == TextDecorationStyle.None || right <= left) return;
        if (style == TextDecorationStyle.Double) {
            Line(left, y - thickness, right, y - thickness, color, thickness, role: "text-decoration");
            Line(left, y + thickness, right, y + thickness, color, thickness, role: "text-decoration");
        } else if (style == TextDecorationStyle.Wavy) {
            var commands = new List<ChartPathCommand> { ChartPathCommand.MoveTo(left, y) };
            var step = Math.Max(2, thickness * 2.2); var index = 1;
            for (var x = Math.Min(right, left + step); x <= right; x = Math.Min(right, x + step)) {
                commands.Add(ChartPathCommand.LineTo(x, y + (index++ % 2 == 0 ? -1 : 1) * Math.Max(1, thickness * 1.4)));
                if (x >= right) break;
            }
            Path(new ChartPath(commands), stroke: color, strokeWidth: thickness, role: "text-decoration");
        } else {
            var dash = style == TextDecorationStyle.Dotted ? new[] { Math.Max(1, thickness), Math.Max(1, thickness * 1.8) }
                : style == TextDecorationStyle.Dashed ? new[] { Math.Max(2, thickness * 4), Math.Max(1, thickness * 2.5) } : null;
            Line(left, y, right, y, color, thickness, role: "text-decoration", dash: dash);
        }
    }

    private static void NonNegative(double value, string name) {
        ChartGuards.Finite(value, name);
        if (value < 0) throw new ArgumentOutOfRangeException(name);
    }

    private static void ValidateRect(ChartRect bounds) {
        ChartGuards.Finite(bounds.X, nameof(bounds)); ChartGuards.Finite(bounds.Y, nameof(bounds));
        NonNegative(bounds.Width, nameof(bounds)); NonNegative(bounds.Height, nameof(bounds));
    }

    private sealed class GroupScope : IDisposable {
        private VisualSceneBuilder? _builder;
        internal GroupScope(VisualSceneBuilder builder) => _builder = builder;
        public void Dispose() {
            if (_builder == null) return;
            if (_builder._groups.Count == 0 || !ReferenceEquals(_builder._groups.Peek(), this))
                throw new InvalidOperationException("Scene groups must be closed in reverse order.");
            _builder._groups.Pop(); _builder._nodes.Add(new VisualSceneEndGroup()); _builder = null;
        }
    }
}
