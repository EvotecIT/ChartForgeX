using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

public sealed partial class TopologyPngRenderer {
    private static void DrawPremiumEdgeRoute(RgbaCanvas canvas, IReadOnlyList<ChartPoint> points, ChartColor color, double width, bool dashed, double dash, double gap, TopologyEdge edge, TopologyRenderOptions options, bool selected) {
        var dashArray = dashed ? new[] { dash, gap } : null;
        foreach (var layer in ChartLineVisualLayers.Build(color, width, EdgeVisualStyle(edge, selected, options))) {
            if (!layer.IsVisible) continue;
            canvas.DrawPolyline(points, layer.ColorWithOpacity(), layer.StrokeWidth, RasterLineCap.Round, RasterLineJoin.Round, dashArray);
        }
    }

    private static void DrawTextWithReadableHalo(RgbaCanvas canvas, double x, double y, string text, ChartColor color, ChartColor haloColor, double fontSize, bool emphasized) {
        foreach (var layer in ChartTextHalo.CompactRasterLayers(fontSize)) {
            var halo = ChartColor.FromRgba(haloColor.R, haloColor.G, haloColor.B, (byte)System.Math.Round(haloColor.A * layer.Opacity));
            if (emphasized) canvas.DrawTextEmphasized(x + layer.Dx, y + layer.Dy, text, halo, fontSize);
            else canvas.DrawText(x + layer.Dx, y + layer.Dy, text, halo, fontSize);
        }

        if (emphasized) canvas.DrawTextEmphasized(x, y, text, color, fontSize);
        else canvas.DrawText(x, y, text, color, fontSize);
    }
}
