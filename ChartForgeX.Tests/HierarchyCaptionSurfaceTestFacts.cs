using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Tests;

/// <summary>Reads only the native frame marks below the fixture's chart data, independently from hierarchy label preparation.</summary>
internal static class HierarchyCaptionSurfaceTestFacts {
    internal static ChartColor FrameSurface(PreparedVisual prepared) {
        var color = ChartColor.White;
        foreach (var mark in prepared.Scene.Nodes.OfType<VisualSceneRectangle>())
            if (mark.Fill.HasValue && (mark.Role is "background" or "frame-card-shadow" or "frame-card" or "content-surface"))
                color = ChartColorMath.CompositeOverOpaque(mark.Fill.Value, color);
        return color;
    }
}
