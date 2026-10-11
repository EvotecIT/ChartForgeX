using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.VisualBlocks;

internal static partial class VisualBlockRendering {
    public static double BlockLayoutCircleRadius(double width, double height) => Math.Max(8, Math.Min(width, height) / 2);

    public static BlockLayoutEdgeRoute BlockLayoutRoute(ChartRect source, BlockLayoutShape sourceShape, ChartRect target, BlockLayoutShape targetShape) {
        var sx = source.X + source.Width / 2;
        var sy = source.Y + source.Height / 2;
        var tx = target.X + target.Width / 2;
        var ty = target.Y + target.Height / 2;
        BlockLayoutEdgeRoute route;
        if (Math.Abs(ty - sy) < Math.Max(source.Height, target.Height) * 0.65) {
            route = tx >= sx
                ? new BlockLayoutEdgeRoute(source.Right, sy, target.Left, ty)
                : new BlockLayoutEdgeRoute(source.Left, sy, target.Right, ty);
        } else {
            route = ty >= sy
                ? new BlockLayoutEdgeRoute(sx, source.Bottom, tx, target.Top)
                : new BlockLayoutEdgeRoute(sx, source.Top, tx, target.Bottom);
        }

        var sourceCircle = sourceShape == BlockLayoutShape.Circle;
        var targetCircle = targetShape == BlockLayoutShape.Circle;
        if ((!sourceCircle && !targetCircle) || (sx == tx && sy == ty)) return route;
        // Keep rectangular ports stable. Circles attach along the ray to the other port,
        // or along the centre line when both nodes are circles.
        var start = sourceCircle
            ? CircleBoundary(sx, sy, targetCircle ? tx : route.X2, targetCircle ? ty : route.Y2, source)
            : new ChartPoint(route.X1, route.Y1);
        var end = targetCircle
            ? CircleBoundary(tx, ty, sourceCircle ? sx : route.X1, sourceCircle ? sy : route.Y1, target)
            : new ChartPoint(route.X2, route.Y2);
        return new BlockLayoutEdgeRoute(start.X, start.Y, end.X, end.Y);
    }

    private static ChartPoint CircleBoundary(double cx, double cy, double towardX, double towardY, ChartRect bounds) {
        var dx = towardX - cx;
        var dy = towardY - cy;
        var length = Math.Sqrt(dx * dx + dy * dy);
        if (length == 0) return new ChartPoint(cx, cy);
        var scale = BlockLayoutCircleRadius(bounds.Width, bounds.Height) / length;
        return new ChartPoint(cx + dx * scale, cy + dy * scale);
    }

    public readonly struct BlockLayoutEdgeRoute {
        public BlockLayoutEdgeRoute(double x1, double y1, double x2, double y2) {
            X1 = x1;
            Y1 = y1;
            X2 = x2;
            Y2 = y2;
        }

        public double X1 { get; }
        public double Y1 { get; }
        public double X2 { get; }
        public double Y2 { get; }
    }
}
