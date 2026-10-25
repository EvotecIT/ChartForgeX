using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

internal static partial class TopologyNodeShapeGeometry {
    /// <summary>Reserves an enclosed, centered text block inside the painted diagram surface.</summary>
    internal static ChartRect CaptionBounds(TopologyNode node, double measuredHeight, double spacingScale = 1) {
        if (TopologyPreparedNodeBasis.Resolve(node) is {} basis && !IsActor(node))
            return basis.Project(node, CaptionBounds(basis.OriginalNode(node), measuredHeight / basis.Factor, spacingScale));
        if (IsCatalogSurface(node)) return CatalogCaptionBounds(node, measuredHeight, spacingScale);
        var width = node.Width; var height = node.Height;
        var centerX = node.X + width / 2; var centerY = node.Y + height / 2;
        var availableHeight = Math.Max(0, height - 12 * spacingScale);
        var blockHeight = Math.Min(availableHeight, Math.Max(0, measuredHeight));
        var inset = Math.Min(width * .18, height * .3);
        var availableWidth = Math.Max(0, width - 20 * spacingScale);
        switch (node.Shape) {
            case TopologyNodeShape.LinedDocument:
                var wave = LinedDocumentWave(node);
                availableWidth = Math.Max(0, width - 36);
                var documentTop = node.Y + 8; var documentBottom = node.Y + height - 2 * wave - 4;
                blockHeight = Math.Min(blockHeight, Math.Max(0, documentBottom - documentTop));
                centerY = (documentTop + documentBottom) / 2;
                centerX += 5;
                break;
            case TopologyNodeShape.Diamond:
                availableWidth = Math.Max(0, width * (1 - (blockHeight + 8) / height) - 8);
                break;
            case TopologyNodeShape.BusinessEllipse:
            case TopologyNodeShape.Ellipse:
            case TopologyNodeShape.Circle:
            case TopologyNodeShape.DoubleCircle:
                var rx = node.Shape is TopologyNodeShape.Ellipse or TopologyNodeShape.BusinessEllipse ? width / 2 : Math.Min(width, height) / 2;
                var ry = node.Shape is TopologyNodeShape.Ellipse or TopologyNodeShape.BusinessEllipse ? height / 2 : rx;
                var inner = node.Shape == TopologyNodeShape.DoubleCircle ? 5 : 0;
                rx = Math.Max(0, rx - inner); ry = Math.Max(0, ry - inner);
                blockHeight = Math.Min(blockHeight, Math.Max(0, 2 * ry - 12));
                var vertical = ry > 0 ? Math.Min(1, (blockHeight / 2 + 4) / ry) : 1;
                availableWidth = Math.Max(0, 2 * rx * Math.Sqrt(1 - vertical * vertical) - 8);
                // The business marker starts at .65 of the horizontal radius; keep native caption ink to its left.
                if (node.Shape == TopologyNodeShape.BusinessEllipse) availableWidth = Math.Min(availableWidth, width * .6);
                break;
            case TopologyNodeShape.HorizontalCylinder:
                var horizontalCap = Math.Min(14, width * .2);
                availableWidth = Math.Max(0, width - 3 * horizontalCap - 4);
                centerX += horizontalCap / 2;
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
            case TopologyNodeShape.Burst:
                // The central rectangle fits inside the inner ellipse at every tooth.
                availableWidth = width * .5;
                blockHeight = Math.Min(blockHeight, height * .5);
                break;
            case TopologyNodeShape.Cloud:
                availableWidth = width * .6;
                blockHeight = Math.Min(blockHeight, height * .4);
                break;
            case TopologyNodeShape.Stadium:
                availableWidth = Math.Max(0, width - Math.Min(width, height) / 2 - 12);
                break;
        }
        if (node.CornerRadius.HasValue) {
            // The quadratic contour has y=r*t^2 and inset=r*(1-t)^2.
            // Measure at the full text block extremum plus its ordinary vertical padding.
            var radius = SurfaceCornerRadius(node);
            var y = Math.Max(0, height / 2 - blockHeight / 2 - 4 * spacingScale);
            var cornerInset = radius > 0 && y < radius ? radius * Math.Pow(1 - Math.Sqrt(y / radius), 2) : 0;
            availableWidth = Math.Min(availableWidth, Math.Max(0, width - 2 * cornerInset - 8 * spacingScale));
        }
        return new ChartRect(centerX - availableWidth / 2, centerY - blockHeight / 2, availableWidth, blockHeight);
    }
}
