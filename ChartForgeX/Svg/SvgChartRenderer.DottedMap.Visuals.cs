using System;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

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
            .Attribute("fill", ChartColorMath.Blend(t.PlotBackground, t.Grid, 0.12).ToCss())
            .Attribute("fill-opacity", "0.16")
            .Attribute("stroke", t.PlotBorder.ToCss())
            .Attribute("stroke-opacity", "0.16")
            .EndEmptyElement()
            .Line());
    }

    private static bool ShouldDrawDottedMapLandDot(ChartMapViewport viewport, ChartColor plotBackground, double longitude, double latitude) {
        var hasVectorGeography = DottedMapBoundaryLines(viewport).Length > 0 || DottedMapViewportOutlines(viewport).Length > 0;
        return !ChartDottedMapSurface.IsLightSurface(plotBackground) || !hasVectorGeography;
    }
}
