using System;
using System.Collections.Generic;

namespace ChartForgeX.Topology;

/// <summary>Immutable finite element composition over the existing native double-circle or stadium paths.</summary>
internal sealed class TopologySurfaceElementAppearance {
    internal TopologySurfaceElementAppearance(TopologyNodeShape shape, TopologyNodeDisplayMode? displayMode, double opacity, double? groupOpacity) {
        if (shape is not (TopologyNodeShape.DoubleCircle or TopologyNodeShape.Stadium))
            throw new ArgumentException("Captured surface elements require double-circle or stadium native geometry.", nameof(shape));
        if (displayMode.HasValue && displayMode != TopologyNodeDisplayMode.Card)
            throw new ArgumentException("Captured surface elements require a card presentation.", nameof(displayMode));
        TopologySurfacePaintRules.Opacity(opacity, nameof(opacity));
        TopologySurfacePaintRules.Opacity(groupOpacity, nameof(groupOpacity));
        if (shape == TopologyNodeShape.DoubleCircle ? groupOpacity != opacity : groupOpacity.HasValue)
            throw new ArgumentException("Captured surface isolation must agree with its closed element composition.", nameof(groupOpacity));
        Shape = shape; DisplayMode = displayMode; Opacity = opacity; GroupOpacity = groupOpacity;
    }
    internal TopologyNodeShape Shape { get; }
    internal TopologyNodeDisplayMode? DisplayMode { get; }
    internal double Opacity { get; }
    internal double? GroupOpacity { get; }
    internal bool FillContours => Shape == TopologyNodeShape.DoubleCircle;
    internal bool SeparateFillStroke => Shape == TopologyNodeShape.Stadium;

    internal bool Matches(TopologyNode node) => node.Shape == Shape && node.DisplayMode == DisplayMode
        && node.SurfaceOpacity == Opacity && node.TableContent == null;
    internal static TopologySurfaceElementAppearance? Resolve(TopologyNode node) =>
        node.SurfaceElementAppearance is {} value && value.Matches(node) ? value : null;
    internal static TopologySurfaceElementAppearance? Resolve(TopologyNode node, TopologyNodeDisplayMode mode) =>
        mode == TopologyNodeDisplayMode.Card ? Resolve(node) : null;
    internal TopologySurfaceElementAppearance RebindAfterCapture(TopologyNode node, TopologyNodeDisplayMode mode) {
        if (node.Shape != Shape || node.SurfaceOpacity != Opacity || node.TableContent != null || mode != TopologyNodeDisplayMode.Card)
            throw new InvalidOperationException("Captured surface elements cannot be rebound to different native paint geometry.");
        return node.DisplayMode == DisplayMode ? this : new TopologySurfaceElementAppearance(Shape, node.DisplayMode, Opacity, GroupOpacity);
    }
    internal void ValidateParts(IReadOnlyList<TopologyNodeShapeGeometry.ScenePart> parts) {
        if (parts.Count != (Shape == TopologyNodeShape.DoubleCircle ? 2 : 1))
            throw new NotSupportedException("Captured surface elements no longer match the native part composition.");
        foreach (var part in parts) if (!part.Ink || !part.ContourPaint || !part.Close || part.AccentFill)
            throw new NotSupportedException("Captured surface elements require the existing closed contour parts.");
    }
}
