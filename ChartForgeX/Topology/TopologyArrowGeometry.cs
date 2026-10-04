using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Topology;

/// <summary>Arrow geometry in the SVG marker's ten-unit view box, projected into canvas coordinates.</summary>
internal static class TopologyArrowGeometry {
    internal static double Extent(TopologyRenderOptions options) => TopologyRenderPrimitives.IsMonitoringDashboardStyle(options) ? 7.5 : 8;
    internal static double ReferenceX(TopologyArrowMarkerStyle style) => style == TopologyArrowMarkerStyle.Circle ? 6 : 7.4;
    internal static string Path(TopologyArrowMarkerStyle style) => style switch {
        TopologyArrowMarkerStyle.Chevron => "M 2.2 1.6 L 7.4 5 L 2.2 8.4",
        TopologyArrowMarkerStyle.Diamond => "M 1 5 L 5 1 L 9 5 L 5 9 z",
        _ => "M 0 0 L 10 5 L 0 10 z"
    };

    internal static ChartPoint Project(ChartPoint point, ChartPoint from, ChartPoint to, TopologyRenderOptions options) {
        var angle = Math.Atan2(to.Y - from.Y, to.X - from.X);
        var scale = Extent(options) / 10;
        var x = (point.X - ReferenceX(options.ArrowMarkerStyle)) * scale;
        var y = (point.Y - 5) * scale;
        return new ChartPoint(to.X + Math.Cos(angle) * x - Math.Sin(angle) * y, to.Y + Math.Sin(angle) * x + Math.Cos(angle) * y);
    }
}
