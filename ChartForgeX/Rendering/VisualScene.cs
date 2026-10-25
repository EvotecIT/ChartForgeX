using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

/// <summary>A detached, ordered display list. Layout is complete before either backend reads it.</summary>
internal sealed class VisualScene {
    internal VisualScene(VisualSize size, IReadOnlyList<VisualSceneNode> nodes,
        IReadOnlyList<VisualDiagnostic> diagnostics, IReadOnlyList<VisualSemanticRegion> regions, ChartDataProjection? dataProjection = null, VisualValueCapture? valueCapture = null) : this(size, nodes, diagnostics, regions, dataProjection, valueCapture, null) { }
    internal VisualScene(VisualSize size, IReadOnlyList<VisualSceneNode> nodes,
        IReadOnlyList<VisualDiagnostic> diagnostics, IReadOnlyList<VisualSemanticRegion> regions, ChartDataProjection? dataProjection,
        VisualValueCapture? valueCapture, VisualSeriesVisibilityCapture? visibilityCapture) {
        Size = size;
        DataProjection = dataProjection; ValueCapture = valueCapture; VisibilityCapture = visibilityCapture;
        var copy = new VisualSceneNode[nodes.Count];
        for (var i = 0; i < copy.Length; i++) {
            copy[i] = nodes[i];
            if (nodes[i] is VisualSceneGroup { SurfaceShadow: not null }) HasSurfaceEffects = true;
            if (nodes[i] is VisualSceneGroup { RetainedEffect: not null }) HasRetainedEffects = true;
        }
        Nodes = Array.AsReadOnly(copy);
        Annotations = VisualAnnotationIdentity.Capture(Nodes);
        var diagnosticCopy = new VisualDiagnostic[diagnostics.Count];
        for (var i = 0; i < diagnosticCopy.Length; i++) diagnosticCopy[i] = diagnostics[i];
        Diagnostics = Array.AsReadOnly(diagnosticCopy);
        var regionCopy = new VisualSemanticRegion[regions.Count];
        for (var i = 0; i < regionCopy.Length; i++) regionCopy[i] = regions[i];
        Regions = Array.AsReadOnly(regionCopy);
    }

    internal bool HasSurfaceEffects { get; }
    internal bool HasRetainedEffects { get; }
    internal VisualValueCapture? ValueCapture { get; }
    internal VisualSeriesVisibilityCapture? VisibilityCapture { get; }
    internal VisualSize Size { get; }
    internal ChartDataProjection? DataProjection { get; }
    internal IReadOnlyList<VisualSceneNode> Nodes { get; }
    internal IReadOnlyList<VisualAnnotationIdentity> Annotations { get; }
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
        ChartPoint? translation = null, VisualSceneImageResource? imageResource = null, VisualSceneColorScope? colorScope = null)
        : this(role, id, clip, metadata, pathClip, rotation, href, tooltip, translation, imageResource, colorScope, 1) { }
    internal VisualSceneGroup(string? role, string? id, ChartRect? clip, IReadOnlyDictionary<string, string>? metadata,
        VisualScenePath? pathClip, VisualRotation? rotation, string? href, string? tooltip,
        ChartPoint? translation, VisualSceneImageResource? imageResource, VisualSceneColorScope? colorScope, double opacity)
        : this(role, id, clip, metadata, pathClip, rotation, href, tooltip,
            translation, imageResource, colorScope, opacity, null) { }
    internal VisualSceneGroup(string? role, string? id, ChartRect? clip, IReadOnlyDictionary<string, string>? metadata,
        VisualScenePath? pathClip, VisualRotation? rotation, string? href, string? tooltip, ChartPoint? translation,
        VisualSceneImageResource? imageResource, VisualSceneColorScope? colorScope, double opacity,
        VisualSceneSurfaceShadow? surfaceShadow)
        : this(role, id, clip, metadata, pathClip, rotation, href, tooltip, translation,
            imageResource, colorScope, opacity, surfaceShadow, null) { }
    internal VisualSceneGroup(string? role, string? id, ChartRect? clip, IReadOnlyDictionary<string, string>? metadata,
        VisualScenePath? pathClip, VisualRotation? rotation, string? href, string? tooltip, ChartPoint? translation,
        VisualSceneImageResource? imageResource, VisualSceneColorScope? colorScope, double opacity,
        VisualSceneSurfaceShadow? surfaceShadow, VisualSceneRetainedEffect? retainedEffect) : base(role, id) {
        if (surfaceShadow != null && retainedEffect != null) {
            throw new ArgumentException("Surface shadows and completed-object masks use separate ordered scopes.");
        }
        SurfaceShadow = surfaceShadow;
        RetainedEffect = retainedEffect;
        Opacity = VisualSceneOpacity.Validate(opacity);
        Clip = clip;
        PathClip = pathClip; Rotation = rotation; Href = href; Tooltip = tooltip; Translation = translation; ImageResource = imageResource;
        ColorScope = colorScope;
        var copy = new SortedDictionary<string, string>(StringComparer.Ordinal);
        if (metadata != null) foreach (var item in metadata) copy.Add(item.Key, item.Value);
        Metadata = new System.Collections.ObjectModel.ReadOnlyDictionary<string, string>(copy);
    }
    internal VisualSceneSurfaceShadow? SurfaceShadow { get; }
    internal VisualSceneRetainedEffect? RetainedEffect { get; }
    internal double Opacity { get; }
    internal ChartRect? Clip { get; }
    internal IReadOnlyDictionary<string, string> Metadata { get; }
    internal VisualScenePath? PathClip { get; }
    internal VisualRotation? Rotation { get; }
    internal ChartPoint? Translation { get; }
    internal string? Href { get; }
    internal string? Tooltip { get; }
    internal VisualSceneImageResource? ImageResource { get; }
    internal VisualSceneColorScope? ColorScope { get; }
}

/// <summary>Isolates a composed child's host paint mapping; null variables retain literal native colors.</summary>
internal sealed class VisualSceneColorScope {
    internal VisualSceneColorScope(SvgColorVariables? variables) { Variables = variables?.Clone(); }
    internal SvgColorVariables? Variables { get; }
}

internal sealed class VisualSceneEndGroup : VisualSceneNode {
    internal VisualSceneEndGroup() : base(null, null) { }
}

internal abstract class VisualSceneMark : VisualSceneNode {
    protected VisualSceneMark(ChartColor? fill, ChartColor? stroke, double strokeWidth, string? role, string? id, VisualScenePaintBinding? paint = null)
        : this(fill, stroke, strokeWidth, role, id, paint, 1) { }
    protected VisualSceneMark(ChartColor? fill, ChartColor? stroke, double strokeWidth, string? role, string? id, VisualScenePaintBinding? paint, double opacity)
        : base(role, id) { Opacity = VisualSceneOpacity.Validate(opacity); Fill = fill; Stroke = stroke; StrokeWidth = strokeWidth; Paint = paint; }
    internal double Opacity { get; }
    internal ChartColor? Fill { get; }
    internal ChartColor? Stroke { get; }
    internal double StrokeWidth { get; }
    internal VisualScenePaintBinding? Paint { get; }
}

internal sealed class VisualSceneRectangle : VisualSceneMark {
    internal VisualSceneRectangle(ChartRect bounds, double radius, ChartColor? fill, ChartColor? stroke,
        double strokeWidth, string? role, string? id, VisualScenePaintBinding? paint = null) : this(bounds, radius, fill, stroke, strokeWidth, role, id, paint, 1) { }
    internal VisualSceneRectangle(ChartRect bounds, double radius, ChartColor? fill, ChartColor? stroke,
        double strokeWidth, string? role, string? id, VisualScenePaintBinding? paint, double opacity)
        : base(fill, stroke, strokeWidth, role, id, paint, opacity) {
        Bounds = bounds; Radius = radius;
    }
    internal ChartRect Bounds { get; }
    internal double Radius { get; }
}

internal sealed class VisualSceneEllipse : VisualSceneMark {
    internal VisualSceneEllipse(double cx, double cy, double rx, double ry, ChartColor? fill, ChartColor? stroke,
        double strokeWidth, string? role, string? id, VisualScenePaintBinding? paint = null) : this(cx, cy, rx, ry, fill, stroke, strokeWidth, role, id, paint, 1) { }
    internal VisualSceneEllipse(double cx, double cy, double rx, double ry, ChartColor? fill, ChartColor? stroke,
        double strokeWidth, string? role, string? id, VisualScenePaintBinding? paint, double opacity)
        : base(fill, stroke, strokeWidth, role, id, paint, opacity) {
        Cx = cx; Cy = cy; Rx = rx; Ry = ry;
    }
    internal double Cx { get; }
    internal double Cy { get; }
    internal double Rx { get; }
    internal double Ry { get; }
}

internal sealed class VisualSceneLine : VisualSceneMark {
    internal VisualSceneLine(ChartPoint start, ChartPoint end, ChartColor color, double width,
        string? role, string? id, double[]? dash, VisualScenePaintBinding? paint = null, double dashOffset = 0) : this(start, end, color, width, role, id, dash, paint, dashOffset, 1) { }
    internal VisualSceneLine(ChartPoint start, ChartPoint end, ChartColor color, double width,
        string? role, string? id, double[]? dash, VisualScenePaintBinding? paint, double dashOffset, double opacity)
        : base(null, color, width, role, id, paint, opacity) {
        Start = start; End = end;
        Dash = dash == null ? null : Array.AsReadOnly((double[])dash.Clone());
        DashOffset = dashOffset;
    }
    internal ChartPoint Start { get; }
    internal ChartPoint End { get; }
    internal IReadOnlyList<double>? Dash { get; }
    internal double DashOffset { get; }
}

internal sealed class VisualScenePath : VisualSceneMark {
    internal VisualScenePath(ChartPath path, bool close, ChartColor? fill, ChartColor? stroke,
        double strokeWidth, string? role, string? id, double[]? dash = null,
        VisualStrokeCap cap = VisualStrokeCap.Round, VisualStrokeJoin join = VisualStrokeJoin.Round,
        VisualScenePaintBinding? paint = null, double dashOffset = 0) : this(path, close, fill, stroke, strokeWidth, role, id, dash, cap, join, paint, dashOffset, 1) { }
    internal VisualScenePath(ChartPath path, bool close, ChartColor? fill, ChartColor? stroke,
        double strokeWidth, string? role, string? id, double[]? dash,
        VisualStrokeCap cap, VisualStrokeJoin join,
        VisualScenePaintBinding? paint, double dashOffset, double opacity)
        : base(fill, stroke, strokeWidth, role, id, paint, opacity) {
        var copy = new ChartPathCommand[path.Commands.Count];
        for (var i = 0; i < copy.Length; i++) copy[i] = path.Commands[i];
        Commands = Array.AsReadOnly(copy); Close = close;
        Dash = dash == null ? null : Array.AsReadOnly((double[])dash.Clone()); Cap = cap; Join = join;
        DashOffset = dashOffset;
    }
    internal IReadOnlyList<ChartPathCommand> Commands { get; }
    internal bool Close { get; }
    internal IReadOnlyList<double>? Dash { get; }
    internal double DashOffset { get; }
    internal VisualStrokeCap Cap { get; }
    internal VisualStrokeJoin Join { get; }
}

internal sealed class VisualSceneSlice : VisualSceneMark {
    internal VisualSceneSlice(double cx, double cy, double outer, double inner, double start, double sweep,
        ChartColor fill, ChartColor? stroke, double strokeWidth, string? role, string? id, VisualScenePaintBinding? paint = null) : this(cx, cy, outer, inner, start, sweep, fill, stroke, strokeWidth, role, id, paint, 1) { }
    internal VisualSceneSlice(double cx, double cy, double outer, double inner, double start, double sweep,
        ChartColor fill, ChartColor? stroke, double strokeWidth, string? role, string? id, VisualScenePaintBinding? paint, double opacity)
        : base(fill, stroke, strokeWidth, role, id, paint, opacity) {
        Cx = cx; Cy = cy; Outer = outer; Inner = inner; Start = start; Sweep = sweep;
    }
    internal double Cx { get; }
    internal double Cy { get; }
    internal double Outer { get; }
    internal double Inner { get; }
    internal double Start { get; }
    internal double Sweep { get; }
}

/// <summary>One finite opacity contract for native scene groups and complete mark elements.</summary>
internal static class VisualSceneOpacity {
    internal static double Validate(double opacity) {
        if (double.IsNaN(opacity) || double.IsInfinity(opacity) || opacity < 0 || opacity > 1)
            throw new ArgumentOutOfRangeException(nameof(opacity), "Native opacity must be finite within0..1.");
        return opacity;
    }
}
