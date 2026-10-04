using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

public sealed partial class TopologyPngRenderer {
    private static bool DrawInfrastructureGlyph(RgbaCanvas canvas, TopologyNode node, double cx, double cy, ChartColor color, TopologyRenderOptions options) {
        var shape = EffectiveIconShape(node, options);
        var marks = TopologyInfrastructureGlyphs.Build(shape, node.Kind, cx, cy);
        if (marks == null) return false;
        DrawGlyphMarks(canvas, marks, color);
        return true;
    }

    /// <summary>Strokes the same glyph geometry the SVG renderer writes, at the same widths, caps, and joins.</summary>
    private static void DrawGlyphMarks(RgbaCanvas canvas, IReadOnlyList<TopologyGlyphMark> marks, ChartColor color) {
        foreach (var mark in marks) {
            if (mark.PathData != null) canvas.StrokePathData(mark.PathData, color, mark.StrokeWidth, mark.RoundCap ? RasterLineCap.Round : RasterLineCap.Butt, mark.RoundJoin ? RasterLineJoin.Round : RasterLineJoin.Miter);
            else if (mark.Filled) canvas.DrawCircle(mark.Cx, mark.Cy, mark.Rx, color);
            else canvas.StrokeEllipse(mark.Cx, mark.Cy, mark.Rx, mark.Ry, color, mark.StrokeWidth);
        }
    }
}
