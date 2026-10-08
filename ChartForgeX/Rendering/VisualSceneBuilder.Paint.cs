using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.SvgRaster;

namespace ChartForgeX.Rendering;

internal sealed partial class VisualSceneBuilder {
    internal IDisposable PushLink(string href, string? id = null, string? role = null, string? tooltip = null) {
        var safe = Topology.TopologyRenderPrimitives.SafeHref(href);
        if (safe == null) throw new ArgumentException("Scene links require a safe relative, fragment, http(s), mailto or telephone target.", nameof(href));
        return OpenGroup(new VisualSceneGroup(role, id, null, null, href: safe, tooltip: tooltip));
    }

    internal IDisposable PushTooltip(string text, string? id = null, string? role = null) {
        if (text == null) throw new ArgumentNullException(nameof(text));
        return OpenGroup(new VisualSceneGroup(role, id, null, null, tooltip: text));
    }

    internal void Image(RgbaImage image, ChartRect bounds, string? role = null, string? id = null, double opacity = 1, string? preserveAspectRatio = "none") {
        ValidateRect(bounds);
        ImageOpacity(opacity);
        var aspect = SvgRasterPreserveAspectRatio.Parse(preserveAspectRatio);
        var destination = bounds;
        if (!aspect.Stretch) {
            var scaleX = bounds.Width / image.Width; var scaleY = bounds.Height / image.Height;
            var scale = aspect.Slice ? Math.Max(scaleX, scaleY) : Math.Min(scaleX, scaleY);
            var width = image.Width * scale; var height = image.Height * scale;
            destination = new ChartRect(bounds.X + aspect.AlignX(bounds.Width - width), bounds.Y + aspect.AlignY(bounds.Height - height), width, height);
        }
        using (aspect.Slice ? PushClip(bounds) : null) _nodes.Add(new VisualSceneImage(image, destination, role, id, opacity));
    }

    /// <summary>Retains a host-managed SVG image and ordinary native fallback geometry without resolving external I/O.</summary>
    internal IDisposable PushImageResource(string href, ChartRect bounds, string? preserveAspectRatio = null, double opacity = 1, string? role = null, string? id = null) {
        ValidateRect(bounds); ImageOpacity(opacity);
        return OpenGroup(new VisualSceneGroup(null, null, bounds, null,
            imageResource: new VisualSceneImageResource(href, bounds, preserveAspectRatio, opacity, role, id)));
    }

    private static void ImageOpacity(double opacity) {
        ChartGuards.Finite(opacity, nameof(opacity));
        if (opacity < 0 || opacity > 1) throw new ArgumentOutOfRangeException(nameof(opacity));
    }

    internal IDisposable PushRotation(double degrees, double originX, double originY) =>
        OpenGroup(new VisualSceneGroup(null, null, null, null, rotation: new VisualRotation(degrees, originX, originY)));

    internal IDisposable PushTranslation(double x, double y) =>
        OpenGroup(new VisualSceneGroup(null, null, null, null, translation: new ChartPoint(x, y)));

    /// <summary>Appends a completed layer in this scene's coordinates without changing semantic identities.</summary>
    /// <remarks>The child contains balanced groups and already measured text. Callers own identity uniqueness.</remarks>
    internal void Append(VisualScene scene) {
        if (scene == null) throw new ArgumentNullException(nameof(scene));
        foreach (var node in scene.Nodes) _nodes.Add(node);
        foreach (var diagnostic in scene.Diagnostics) _diagnostics.Add(diagnostic);
        foreach (var region in scene.Regions) _regions.Add(region);
    }

    /// <summary>Places an immutable child scene without rendering or reshaping it.</summary>
    internal void Append(VisualScene scene, double x, double y, string panelId) {
        if (scene == null) throw new ArgumentNullException(nameof(scene));
        if (string.IsNullOrWhiteSpace(panelId)) throw new ArgumentException("A panel identity is required.", nameof(panelId));
        using (PushTranslation(x, y))
        using (PushGroup(panelId, "panel")) {
            foreach (var node in scene.Nodes) _nodes.Add(node);
        }
        foreach (var diagnostic in scene.Diagnostics) _diagnostics.Add(diagnostic);
        foreach (var region in scene.Regions) {
            var bounds = region.Bounds;
            _regions.Add(new VisualSemanticRegion(panelId + "/" + region.Id, region.Role,
                new ChartRect(bounds.X + x, bounds.Y + y, bounds.Width, bounds.Height), region.Label));
        }
    }

    internal IDisposable PushClip(ChartPath path) {
        var clip = SnapshotPath(path, close: true);
        return OpenGroup(new VisualSceneGroup(null, null, null, null, pathClip: clip));
    }

    internal void RectGradient(ChartRect bounds, ChartPoint start, ChartPoint end, IReadOnlyList<VisualGradientStop> stops,
        ChartColor? stroke = null, double strokeWidth = 1, double radius = 0, string? role = null, string? id = null, VisualScenePaintBinding? paint = null) {
        Rect(bounds, null, stroke, strokeWidth, radius, role, id, paint);
        ReplaceWithGradient(start, end, stops);
    }

    internal void PathGradient(ChartPath path, ChartPoint start, ChartPoint end, IReadOnlyList<VisualGradientStop> stops,
        ChartColor? stroke = null, double strokeWidth = 1, string? role = null, string? id = null, bool close = true, VisualScenePaintBinding? paint = null) {
        Path(path, stroke: stroke, strokeWidth: strokeWidth, role: role, id: id, close: close, paint: paint);
        ReplaceWithGradient(start, end, stops);
    }

    internal void SliceGradient(double cx, double cy, double outerRadius, double innerRadius, double startAngle, double sweep,
        ChartPoint start, ChartPoint end, IReadOnlyList<VisualGradientStop> stops,
        ChartColor? stroke = null, double strokeWidth = 1, string? role = null, string? id = null, VisualScenePaintBinding? paint = null) {
        Slice(cx, cy, outerRadius, innerRadius, startAngle, sweep, ChartColor.Transparent, stroke,strokeWidth, role, id, paint);
        ReplaceWithGradient(start, end, stops);
    }

    internal void Pattern(ChartPath clip, ChartFillPattern pattern, ChartColor color,
        double spacing = 8, double strokeWidth = 1.5, string? role = null, Themes.SvgPaint? paint = null) {
        if (!Enum.IsDefined(typeof(ChartFillPattern), pattern)) throw new ArgumentOutOfRangeException(nameof(pattern));
        VisualSize.Positive(spacing, nameof(spacing)); NonNegative(strokeWidth, nameof(strokeWidth));
        if (pattern == ChartFillPattern.None || strokeWidth == 0) return;
        var shape = SnapshotPath(clip, close: true);
        var contours = VisualSceneGeometry.Flatten(shape, 1);
        var points = contours.SelectMany(contour => contour).ToArray();
        if (points.Length == 0) return;
        var left = points.Min(point => point.X); var top = points.Min(point => point.Y);
        var width = points.Max(point => point.X) - left; var height = points.Max(point => point.Y) - top;
        using (PushClip(clip)) {
            foreach (var line in ChartPatternLineGeometry.BuildUserSpace(pattern, left, top, width, height, 0, spacing, 0))
                Line(line.X1, line.Y1, line.X2, line.Y2, color, strokeWidth, role,
                    paint: paint.HasValue ? new VisualScenePaintBinding(stroke: paint) : null);
        }
    }

    internal void PatternSlice(double cx, double cy, double outerRadius, double innerRadius, double start, double sweep,
        ChartFillPattern pattern, ChartColor color, double spacing = 8, double strokeWidth = 1.5, string? role = null, Themes.SvgPaint? paint = null) {
        // Retain one numeric contour clip independent of either export backend.
        var slice = new VisualSceneSlice(cx, cy, outerRadius, innerRadius, start, sweep, color, null, 0, null, null);
        var contours = VisualSceneGeometry.Flatten(slice, 4);
        var commands = new List<ChartPathCommand>();
        foreach (var contour in contours) for (var i = 0; i < contour.Count; i++)
            commands.Add(i == 0 ? ChartPathCommand.MoveTo(contour[i].X, contour[i].Y) : ChartPathCommand.LineTo(contour[i].X, contour[i].Y));
        Pattern(new ChartPath(commands), pattern, color, spacing, strokeWidth, role, paint);
    }

    private VisualScenePath SnapshotPath(ChartPath path, bool close) {
        Path(path, close: close);
        var snapshot = (VisualScenePath)_nodes[_nodes.Count - 1];
        _nodes.RemoveAt(_nodes.Count - 1);
        return snapshot;
    }

    private void ReplaceWithGradient(ChartPoint start, ChartPoint end, IReadOnlyList<VisualGradientStop> stops) {
        var index = _nodes.Count - 1;
        var gradient = new VisualSceneGradient((VisualSceneMark)_nodes[index], start, end, stops);
        _nodes[index] = gradient;
    }
}
