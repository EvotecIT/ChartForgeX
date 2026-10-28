using System;
using System.Collections.Generic;
using ChartForgeX.Rendering;

namespace ChartForgeX.Topology;

internal static partial class TopologyNodeShapeGeometry {
    private static IReadOnlyList<ScenePart> DocumentCatalogParts(TopologyNode node, bool stacked, bool tagged) {
        var parts = new List<ScenePart>();
        if (stacked) {
            parts.Add(LinedDocumentParts(CatalogCell(node, .12, 0, 1, .84))[0]);
            parts.Add(LinedDocumentParts(CatalogCell(node, .06, .08, .94, .92))[0]);
        }
        var front = stacked ? CatalogCell(node, 0, .16, .88, 1) : node;
        parts.Add(LinedDocumentParts(front)[0]);
        if (tagged) parts.Add(CatalogTag(front));
        return parts;
    }
    private static ScenePart CatalogTag(TopologyNode node) {
        var tag = CatalogPolygon(node, .78, 1, 1, 1, 1, .7);
        return new ScenePart(tag.Path, true, true, true, false, true);
    }
    private static IReadOnlyList<ScenePart> StackRectangleParts(TopologyNode node) => new[] {
        CatalogRectangle(CatalogCell(node, .12, 0, 1, .84)),
        CatalogRectangle(CatalogCell(node, .06, .08, .94, .92)),
        CatalogRectangle(CatalogCell(node, 0, .16, .88, 1))
    };
    private static IReadOnlyList<ScenePart> LinedCylinderCatalogParts(TopologyNode node) {
        var cylinder = new TopologyNode { X = node.X, Y = node.Y, Width = node.Width, Height = node.Height, Shape = TopologyNodeShape.Cylinder };
        var original = SceneParts(cylinder);
        var parts = new List<ScenePart>();
        for (var index = 0; index < original.Count; index++) {
            var part = original[index];
            parts.Add(new ScenePart(part.Path, part.Close, part.Fill, true, index == 0));
        }
        var cap = Math.Min(14, node.Height * .2);
        parts.Add(new ScenePart(new ChartPath(new[] {
            ChartPathCommand.MoveTo(node.X, node.Y + cap * 1.65),
            ChartPathCommand.CubicTo(node.X, node.Y + cap * 2.95, node.X + node.Width, node.Y + cap * 2.95, node.X + node.Width, node.Y + cap * 1.65)
        }), false, false, true, false));
        return parts;
    }
    private static IReadOnlyList<ScenePart> PaperTapeParts(TopologyNode node) {
        var x = node.X; var y = node.Y; var w = node.Width; var h = node.Height;
        return new[] { new ScenePart(new ChartPath(new[] {
            ChartPathCommand.MoveTo(x, y + h * .12),
            ChartPathCommand.CubicTo(x + w * .2, y - h * .04, x + w * .3, y - h * .04, x + w * .5, y + h * .12),
            ChartPathCommand.CubicTo(x + w * .7, y + h * .28, x + w * .8, y + h * .28, x + w, y + h * .12),
            ChartPathCommand.LineTo(x + w, y + h * .88),
            ChartPathCommand.CubicTo(x + w * .8, y + h * 1.04, x + w * .7, y + h * 1.04, x + w * .5, y + h * .88),
            ChartPathCommand.CubicTo(x + w * .3, y + h * .72, x + w * .2, y + h * .72, x, y + h * .88)
        }), true, true) };
    }
}
