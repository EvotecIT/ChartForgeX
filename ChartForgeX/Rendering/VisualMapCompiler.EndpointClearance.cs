using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static partial class VisualMapCompiler {
    private static IReadOnlyList<(ChartPoint Center, double Radius)> MapEndpointDisks(IReadOnlyList<MapDataset> peers,
        ChartPoint source, Func<ChartPoint, ChartPoint> project, double dot, double gap, double horizontalOffset = 0) {
        var disks = new List<(ChartPoint Center, double Radius)>();
        foreach (var data in peers) {
            if (data.Series.Kind != ChartSeriesKind.DottedMap) continue;
            for (var index = 0; index < data.Series.RenderPoints.Count; index++) {
                var point = data.Series.RenderPoints[index];
                if (!ChartMath.SameCoordinate(point.X, source.X) || !ChartMath.SameCoordinate(point.Y, source.Y)) continue;
                var cluster = data.ClusterFor(index);
                var center = cluster?.Center ?? project(point);
                if (horizontalOffset != 0) center = new ChartPoint(center.X + horizontalOffset, center.Y);
                var radius = cluster != null ? data.Series.MapClustering!.Radius : data.SizeScale!.Radius(data.Weights[index], dot);
                disks.Add((center, radius + Math.Max(1, dot * .55) / 2 + gap));
            }
        }
        // An endpoint without an authored point still honors its explicit logical gap; no source point is fabricated.
        if (disks.Count == 0) {
            var center = project(source);
            if (horizontalOffset != 0) center = new ChartPoint(center.X + horizontalOffset, center.Y);
            disks.Add((center, gap));
        }
        return disks;
    }
}
