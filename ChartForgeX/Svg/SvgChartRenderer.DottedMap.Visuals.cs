using System;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawDottedMapSurface(StringBuilder sb, Chart chart, ChartRect map, double dot) {
        var t = chart.Options.Theme;
        var pad = Math.Max(10, dot * 3.8);
        AppendSvg(sb, 384, writer => writer.StartElement("rect")
            .Attribute("data-cfx-role", "dotted-map-surface")
            .Attribute("x", map.Left - pad)
            .Attribute("y", map.Top - pad)
            .Attribute("width", map.Width + pad * 2)
            .Attribute("height", map.Height + pad * 2)
            .Attribute("rx", Math.Min(18, Math.Max(7, dot * 3.2)))
            .Attribute("fill", Blend(t.PlotBackground, t.Grid, 0.12).ToCss())
            .Attribute("fill-opacity", "0.16")
            .Attribute("stroke", t.PlotBorder.ToCss())
            .Attribute("stroke-opacity", "0.16")
            .EndEmptyElement()
            .Line());
    }

    private static bool ShouldDrawDottedMapLandDot(ChartMapViewport viewport, double longitude, double latitude) {
        var boundaries = DottedMapBoundaryLines(viewport);
        var hasClosedBoundary = false;
        foreach (var boundary in boundaries) {
            if (!CanFillDottedMapBoundary(boundary)) continue;
            hasClosedBoundary = true;
        }

        return !hasClosedBoundary;
    }
}
