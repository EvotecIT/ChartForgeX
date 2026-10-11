using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

internal static partial class RasterStroker {
    internal static List<List<ChartPoint>> CreateOutline(IReadOnlyList<IReadOnlyList<ChartPoint>> paths,
        double width, RasterLineCap cap, RasterLineJoin join, double miter, IReadOnlyList<double>? pattern,
        double offset, double pixelsPerUnit, double left, double top, double right, double bottom, DashBudget? dashBudget = null) {
        var outline = new List<List<ChartPoint>>();
        if (!(width > 0) || double.IsInfinity(width)) return outline;
        if (!(miter >= 1) || double.IsInfinity(miter)) miter = 4;
        double padding = width * Math.Max(1, miter);
        if (pattern != null && pattern.Count > 0) dashBudget ??= new DashBudget(pixelsPerUnit);
        for (var pathIndex = 0; pathIndex < paths.Count; pathIndex++) {
            var path = paths[pathIndex];
            if (path == null || path.Count == 0) continue;
            if (pattern == null || pattern.Count == 0) AppendOutline(path, width, cap, join, miter, pixelsPerUnit, outline);
            else foreach (var dash in Dash(path, pattern, left-padding, top-padding, right+padding, bottom+padding, offset, dashBudget!, pathIndex))
                AppendOutline(dash, width, cap, join, miter, pixelsPerUnit, outline);
        }
        return outline;
    }
}
