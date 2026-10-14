using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyNodeShapeGeometry {
    /// <summary>Reserves an enclosed, centered text block inside the painted diagram surface.</summary>
    internal static ChartRect CaptionBounds(TopologyNode node, double measuredHeight) {
        var width = node.Width; var height = node.Height;
        var centerX = node.X + width / 2; var centerY = node.Y + height / 2;
        var availableHeight = Math.Max(0, height - 12);
        var blockHeight = Math.Min(availableHeight, Math.Max(0, measuredHeight));
        var inset = Math.Min(width * .18, height * .3);
        var availableWidth = Math.Max(0, width - 20);
        switch (node.Shape) {
            case TopologyNodeShape.Diamond:
                availableWidth = Math.Max(0, width * (1 - (blockHeight + 8) / height) - 8);
                break;
            case TopologyNodeShape.Ellipse:
            case TopologyNodeShape.Circle:
            case TopologyNodeShape.DoubleCircle:
                var rx = node.Shape == TopologyNodeShape.Ellipse ? width / 2 : Math.Min(width, height) / 2;
                var ry = node.Shape == TopologyNodeShape.Ellipse ? height / 2 : rx;
                var inner = node.Shape == TopologyNodeShape.DoubleCircle ? 5 : 0;
                rx = Math.Max(0, rx - inner); ry = Math.Max(0, ry - inner);
                blockHeight = Math.Min(blockHeight, Math.Max(0, 2 * ry - 12));
                var vertical = ry > 0 ? Math.Min(1, (blockHeight / 2 + 4) / ry) : 1;
                availableWidth = Math.Max(0, 2 * rx * Math.Sqrt(1 - vertical * vertical) - 8);
                break;
            case TopologyNodeShape.Cylinder:
                var cap = Math.Min(14, height * .2);
                var top = node.Y + cap * 2 + 2; var bottom = node.Y + height - cap - 2;
                blockHeight = Math.Min(blockHeight, Math.Max(0, bottom - top));
                centerY = (top + bottom) / 2;
                break;
            case TopologyNodeShape.Hexagon:
            case TopologyNodeShape.Parallelogram:
            case TopologyNodeShape.ParallelogramAlt:
            case TopologyNodeShape.Trapezoid:
            case TopologyNodeShape.TrapezoidAlt:
            case TopologyNodeShape.Asymmetric:
                availableWidth = Math.Max(0, width - 2 * inset - 12);
                break;
            case TopologyNodeShape.Subroutine:
                availableWidth = Math.Max(0, width - 30);
                break;
            case TopologyNodeShape.Cloud:
                availableWidth = width * .6;
                blockHeight = Math.Min(blockHeight, height * .4);
                break;
            case TopologyNodeShape.Stadium:
                availableWidth = Math.Max(0, width - Math.Min(width, height) / 2 - 12);
                break;
        }
        return new ChartRect(centerX - availableWidth / 2, centerY - blockHeight / 2, availableWidth, blockHeight);
    }
}
