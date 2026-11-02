using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>Applies authored interiors after family layout, preserving geometry, stroke, opacity and identity.</summary>
internal static class VisualSceneChartFills {
    internal static VisualSceneNode ApplyFill(VisualSceneMark mark, ChartFill fill, ChartRect? bounds = null) {
        if (mark == null) throw new ArgumentNullException(nameof(mark));
        if (fill == null) throw new ArgumentNullException(nameof(fill));
        if (!mark.Fill.HasValue || mark is VisualSceneLine) return mark;
        var area = bounds ?? Bounds(mark);
        if (area.Width <= 0 || area.Height <= 0) return mark;
        return new VisualSceneGradient(mark, fill, area);
    }

    internal static ChartRect Bounds(VisualSceneMark mark) {
        if (mark is VisualSceneRectangle rectangle) return rectangle.Bounds;
        if (mark is VisualSceneEllipse ellipse) return new ChartRect(ellipse.Cx - ellipse.Rx, ellipse.Cy - ellipse.Ry, ellipse.Rx * 2, ellipse.Ry * 2);
        if (mark is VisualScenePath path) return new ChartPath(path.Commands).Bounds();
        if (mark is VisualSceneSlice slice) return ChartSlicePathGeometry.Bounds(slice.Cx, slice.Cy, slice.Outer, slice.Inner, slice.Start, slice.Sweep);
        throw new NotSupportedException("Unsupported chart fill shape.");
    }
}
