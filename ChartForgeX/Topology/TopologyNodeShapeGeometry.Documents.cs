using System;
using System.Collections.Generic;
using ChartForgeX.Rendering;

namespace ChartForgeX.Topology;

internal static partial class TopologyNodeShapeGeometry {
    private static double LinedDocumentWave(TopologyNode node) => Math.Min(10, node.Height * .12);
    // One numeric contour owns native SVG/PNG ink and route attachment. The left
    // reference rail stays outside the content cell reserved by CaptionBounds.
    private static IReadOnlyList<ScenePart> LinedDocumentParts(TopologyNode node) {
        var x = node.X; var y = node.Y; var w = node.Width; var h = node.Height;
        var wave = LinedDocumentWave(node);
        var lower = y + h - wave;
        return new[] {
            new ScenePart(new ChartPath(new[] {
                ChartPathCommand.MoveTo(x, y), ChartPathCommand.LineTo(x + w, y),
                ChartPathCommand.LineTo(x + w, lower),
                ChartPathCommand.CubicTo(x + w * .84, lower - wave, x + w * .66, lower - wave, x + w * .5, lower),
                ChartPathCommand.CubicTo(x + w * .34, lower + wave, x + w * .16, lower + wave, x, lower)
            }), true, true),
            new ScenePart(new ChartPath(new[] {
                ChartPathCommand.MoveTo(x + Math.Min(12, w * .1), y),
                ChartPathCommand.LineTo(x + Math.Min(12, w * .1), y + h - 2 * wave)
            }), false, false)
        };
    }
}
