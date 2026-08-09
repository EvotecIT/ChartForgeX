using System;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

public sealed partial class TopologyPngRenderer {
    private static void DrawEndpointLabels(RgbaCanvas canvas, TopologyChart chart, TopologyTheme theme, TopologyRenderOptions options, TopologyHighlightState highlight) {
        var nodes = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        foreach (var edge in chart.Edges) {
            if (string.IsNullOrWhiteSpace(edge.SourceLabel) && string.IsNullOrWhiteSpace(edge.TargetLabel)) continue;
            var points = EdgePoints(chart, edge, nodes);
            if (points.Count < 2) continue;
            var edgeAlpha = (byte)Math.Round(255 * EdgeOpacity(edge, options), MidpointRounding.AwayFromZero);
            var alpha = highlight.IsEdgeHighlighted(edge) ? edgeAlpha : HighlightAlpha(edgeAlpha, false, highlight);
            var color = WithAlpha(Color(theme.MutedForeground), alpha);
            var halo = WithAlpha(Color(theme.Background), alpha);
            if (!string.IsNullOrWhiteSpace(edge.SourceLabel)) DrawEndpointLabel(canvas, edge.SourceLabel!, EdgeEndpointLabelPoint(points[0], points[1]), color, halo);
            if (!string.IsNullOrWhiteSpace(edge.TargetLabel)) DrawEndpointLabel(canvas, edge.TargetLabel!, EdgeEndpointLabelPoint(points[points.Count - 1], points[points.Count - 2]), color, halo);
        }
    }

    private static void DrawEndpointLabel(RgbaCanvas canvas, string text, ChartPoint point, ChartColor color, ChartColor halo) {
        var label = text.Trim();
        const double fontSize = 9.5;
        var width = RgbaCanvas.MeasureTextEmphasizedWidth(label, fontSize, null);
        DrawTextWithReadableHalo(canvas, point.X - width / 2, point.Y - fontSize / 2, label, color, halo, fontSize, true);
    }

}
