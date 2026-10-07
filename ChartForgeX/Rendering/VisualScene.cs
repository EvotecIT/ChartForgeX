using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

/// <summary>A detached, ordered display list. Layout is complete before either backend reads it.</summary>
internal sealed class VisualScene {
    internal VisualScene(VisualSize size, IReadOnlyList<VisualSceneNode> nodes,
        IReadOnlyList<VisualDiagnostic> diagnostics, IReadOnlyList<VisualSemanticRegion> regions) {
        Size = size;
        var copy = new VisualSceneNode[nodes.Count];
        for (var i = 0; i < copy.Length; i++) copy[i] = nodes[i];
        Nodes = Array.AsReadOnly(copy);
        var diagnosticCopy = new VisualDiagnostic[diagnostics.Count];
        for (var i = 0; i < diagnosticCopy.Length; i++) diagnosticCopy[i] = diagnostics[i];
        Diagnostics = Array.AsReadOnly(diagnosticCopy);
        var regionCopy = new VisualSemanticRegion[regions.Count];
        for (var i = 0; i < regionCopy.Length; i++) regionCopy[i] = regions[i];
        Regions = Array.AsReadOnly(regionCopy);
    }

    internal VisualSize Size { get; }
    internal IReadOnlyList<VisualSceneNode> Nodes { get; }
    internal IReadOnlyList<VisualDiagnostic> Diagnostics { get; }
    internal IReadOnlyList<VisualSemanticRegion> Regions { get; }
}

internal abstract class VisualSceneNode {
    protected VisualSceneNode(string? role, string? id) { Role = role; Id = id; }
    internal string? Role { get; }
    internal string? Id { get; }
}

internal sealed class VisualSceneGroup : VisualSceneNode {
    internal VisualSceneGroup(string? role, string? id, ChartRect? clip, IReadOnlyDictionary<string, string>? metadata,
        VisualScenePath? pathClip = null, VisualRotation? rotation = null, string? href = null, string? tooltip = null,
        ChartPoint? translation = null, VisualSceneImageResource? imageResource = null)
        : base(role, id) {
        Clip = clip;
        PathClip = pathClip; Rotation = rotation; Href = href; Tooltip = tooltip; Translation = translation; ImageResource = imageResource;
        var copy = new SortedDictionary<string, string>(StringComparer.Ordinal);
        if (metadata != null) foreach (var item in metadata) copy.Add(item.Key, item.Value);
        Metadata = new System.Collections.ObjectModel.ReadOnlyDictionary<string, string>(copy);
    }
    internal ChartRect? Clip { get; }
    internal IReadOnlyDictionary<string, string> Metadata { get; }
    internal VisualScenePath? PathClip { get; }
    internal VisualRotation? Rotation { get; }
    internal ChartPoint? Translation { get; }
    internal string? Href { get; }
    internal string? Tooltip { get; }
    internal VisualSceneImageResource? ImageResource { get; }
}

internal sealed class VisualSceneEndGroup : VisualSceneNode {
    internal VisualSceneEndGroup() : base(null, null) { }
}

internal abstract class VisualSceneMark : VisualSceneNode {
    protected VisualSceneMark(ChartColor? fill, ChartColor? stroke, double strokeWidth, string? role, string? id, VisualScenePaintBinding? paint = null)
        : base(role, id) { Fill = fill; Stroke = stroke; StrokeWidth = strokeWidth; Paint = paint; }
    internal ChartColor? Fill { get; }
    internal ChartColor? Stroke { get; }
    internal double StrokeWidth { get; }
    internal VisualScenePaintBinding? Paint { get; }
}

internal sealed class VisualSceneRectangle : VisualSceneMark {
    internal VisualSceneRectangle(ChartRect bounds, double radius, ChartColor? fill, ChartColor? stroke,
        double strokeWidth, string? role, string? id, VisualScenePaintBinding? paint = null) : base(fill, stroke, strokeWidth, role, id, paint) {
        Bounds = bounds; Radius = radius;
    }
    internal ChartRect Bounds { get; }
    internal double Radius { get; }
}

internal sealed class VisualSceneEllipse : VisualSceneMark {
    internal VisualSceneEllipse(double cx, double cy, double rx, double ry, ChartColor? fill, ChartColor? stroke,
        double strokeWidth, string? role, string? id, VisualScenePaintBinding? paint = null) : base(fill, stroke, strokeWidth, role, id, paint) {
        Cx = cx; Cy = cy; Rx = rx; Ry = ry;
    }
    internal double Cx { get; }
    internal double Cy { get; }
    internal double Rx { get; }
    internal double Ry { get; }
}

internal sealed class VisualSceneLine : VisualSceneMark {
    internal VisualSceneLine(ChartPoint start, ChartPoint end, ChartColor color, double width,
        string? role, string? id, double[]? dash, VisualScenePaintBinding? paint = null) : base(null, color, width, role, id, paint) {
        Start = start; End = end;
        Dash = dash == null ? null : Array.AsReadOnly((double[])dash.Clone());
    }
    internal ChartPoint Start { get; }
    internal ChartPoint End { get; }
    internal IReadOnlyList<double>? Dash { get; }
}

internal sealed class VisualScenePath : VisualSceneMark {
    internal VisualScenePath(ChartPath path, bool close, ChartColor? fill, ChartColor? stroke,
        double strokeWidth, string? role, string? id, double[]? dash = null,
        VisualStrokeCap cap = VisualStrokeCap.Round, VisualStrokeJoin join = VisualStrokeJoin.Round,
        VisualScenePaintBinding? paint = null) : base(fill, stroke, strokeWidth, role, id, paint) {
        var copy = new ChartPathCommand[path.Commands.Count];
        for (var i = 0; i < copy.Length; i++) copy[i] = path.Commands[i];
        Commands = Array.AsReadOnly(copy); Close = close;
        Dash = dash == null ? null : Array.AsReadOnly((double[])dash.Clone()); Cap = cap; Join = join;
    }
    internal IReadOnlyList<ChartPathCommand> Commands { get; }
    internal bool Close { get; }
    internal IReadOnlyList<double>? Dash { get; }
    internal VisualStrokeCap Cap { get; }
    internal VisualStrokeJoin Join { get; }
}

internal sealed class VisualSceneSlice : VisualSceneMark {
    internal VisualSceneSlice(double cx, double cy, double outer, double inner, double start, double sweep,
        ChartColor fill, ChartColor? stroke, double strokeWidth, string? role, string? id, VisualScenePaintBinding? paint = null)
        : base(fill, stroke, strokeWidth, role, id, paint) {
        Cx = cx; Cy = cy; Outer = outer; Inner = inner; Start = start; Sweep = sweep;
    }
    internal double Cx { get; }
    internal double Cy { get; }
    internal double Outer { get; }
    internal double Inner { get; }
    internal double Start { get; }
    internal double Sweep { get; }
}
