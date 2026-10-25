using System;

namespace ChartForgeX.Topology;

internal static partial class TopologyNodeShapeGeometry {
    /// <summary>Shares one bounded polygon across scene painting, SVG path export, attachment and hit contours.</summary>
    private static double[] BurstPoints(TopologyNode node) {
        const int vertices = 24;
        var points = new double[vertices * 2];
        for (var index = 0; index < vertices; index++) {
            var angle = -Math.PI / 2 + index * 2 * Math.PI / vertices;
            var radius = index % 2 == 0 ? .5 : .38;
            points[index * 2] = node.X + node.Width * (.5 + Math.Cos(angle) * radius);
            points[index * 2 + 1] = node.Y + node.Height * (.5 + Math.Sin(angle) * radius);
        }
        return points;
    }
}
