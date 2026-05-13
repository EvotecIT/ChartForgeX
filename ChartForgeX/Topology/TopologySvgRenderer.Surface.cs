using ChartForgeX.Svg;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

public sealed partial class TopologySvgRenderer {
    private static void AddCanvasSurface(SvgElement root, TopologyChart chart, string prefix, TopologyTheme theme, TopologyRenderOptions options) {
        if (!ShouldRenderCanvasSurface(chart, options)) return;
        var x = chart.Viewport.Padding;
        var y = options.IncludeTitle && (!string.IsNullOrWhiteSpace(chart.Title) || !string.IsNullOrWhiteSpace(chart.Subtitle)) ? chart.Viewport.Padding + 62 : chart.Viewport.Padding;
        var width = chart.Viewport.Width - chart.Viewport.Padding * 2;
        var height = chart.Viewport.Height - y - chart.Viewport.Padding - LegendReservedHeight(chart.Legend, chart.Viewport);
        if (width <= 0 || height <= 0) return;
        var layer = new SvgElement("g")
            .Class(prefix + "__canvas-surface")
            .Attribute("data-cfx-role", "topology-canvas-surface")
            .Attribute("data-canvas-surface-style", options.CanvasSurfaceStyle.ToString());
        layer.Element("rect", rect => rect
            .Attribute("x", x)
            .Attribute("y", y)
            .Attribute("width", width)
            .Attribute("height", height)
            .Attribute("rx", IsMonitoringDashboardStyle(options) ? 12 : 14)
            .Attribute("fill", theme.Card)
            .Attribute("stroke", theme.Border)
            .Attribute("filter", "url(#" + SanitizeId(chart.Id ?? "topology") + "-shadow)"));
        if (options.CanvasSurfaceStyle == TopologyCanvasSurfaceStyle.PanelGrid) AddCanvasGrid(layer, x, y, width, height, theme);
        root.AddElement(layer);
    }

    private static void AddCanvasGrid(SvgElement layer, double x, double y, double width, double height, TopologyTheme theme) {
        const double spacing = 56;
        var right = x + width;
        var bottom = y + height;
        for (var gx = x + spacing; gx < right - 2; gx += spacing) {
            layer.Element("line", line => line
                .Attribute("x1", gx)
                .Attribute("y1", y + 10)
                .Attribute("x2", gx)
                .Attribute("y2", bottom - 10)
                .Attribute("stroke", theme.Border)
                .Attribute("stroke-opacity", 0.28)
                .Attribute("stroke-width", 0.75));
        }

        for (var gy = y + spacing; gy < bottom - 2; gy += spacing) {
            layer.Element("line", line => line
                .Attribute("x1", x + 10)
                .Attribute("y1", gy)
                .Attribute("x2", right - 10)
                .Attribute("y2", gy)
                .Attribute("stroke", theme.Border)
                .Attribute("stroke-opacity", 0.22)
                .Attribute("stroke-width", 0.75));
        }
    }

    private static void AddArrowMarker(SvgElement defs, string id, string color, TopologyRenderOptions options) {
        defs.Element("marker", marker => {
            marker
                .Attribute("id", id)
                .Attribute("viewBox", "0 0 10 10")
                .Attribute("refX", options.ArrowMarkerStyle == TopologyArrowMarkerStyle.Circle ? 6 : 7.4)
                .Attribute("refY", 5)
                .Attribute("markerWidth", IsMonitoringDashboardStyle(options) ? 7.5 : 8)
                .Attribute("markerHeight", IsMonitoringDashboardStyle(options) ? 7.5 : 8)
                .Attribute("orient", "auto-start-reverse")
                .Attribute("overflow", "visible");
            switch (options.ArrowMarkerStyle) {
                case TopologyArrowMarkerStyle.Chevron:
                    marker.Element("path", path => path.Attribute("d", "M 2.2 1.6 L 7.4 5 L 2.2 8.4").Attribute("fill", "none").Attribute("stroke", color).Attribute("stroke-width", 1.85).Attribute("stroke-linecap", "round").Attribute("stroke-linejoin", "round"));
                    break;
                case TopologyArrowMarkerStyle.Diamond:
                    marker.Element("path", path => path.Attribute("d", "M 1 5 L 5 1 L 9 5 L 5 9 z").Attribute("fill", color));
                    break;
                case TopologyArrowMarkerStyle.Circle:
                    marker.Element("circle", circle => circle.Attribute("cx", 5).Attribute("cy", 5).Attribute("r", 3.4).Attribute("fill", color));
                    break;
                default:
                    marker.Element("path", path => path.Attribute("d", "M 0 0 L 10 5 L 0 10 z").Attribute("fill", color));
                    break;
            }
        });
    }

    private static string ArrowMarkerId(string svgId, string color) => svgId + "-arrow-" + ArrowMarkerToken(color);

    private static string ArrowMarkerToken(string color) {
        var value = string.IsNullOrWhiteSpace(color) ? "current" : color.Trim().ToLowerInvariant();
        var sb = new System.Text.StringBuilder(value.Length);
        foreach (var c in value) sb.Append(char.IsLetterOrDigit(c) ? c : '-');
        return sb.ToString().Trim('-');
    }
}
