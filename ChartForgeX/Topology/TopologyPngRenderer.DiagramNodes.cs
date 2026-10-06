using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Raster;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

public sealed partial class TopologyPngRenderer {
    private static void DrawDiagramNode(RgbaCanvas canvas, TopologyNode node, TopologyTheme theme, TopologyRenderOptions options, bool selected, double opacity) {
        var alpha = (byte)System.Math.Round(255 * opacity);
        var accent = WithAlpha(Color(NodeAccentColor(node, theme, options)), alpha);
        var fill = WithAlpha(Color(NodeFill(node, theme, NodeAccentColor(node, theme, options), options)), alpha);
        foreach (var path in ChartMapPathParser.ParseSubpaths(TopologyNodeShapeGeometry.Path(node), canvas.DeviceScale)) {
            if (path.IsClosed) canvas.FillPolygon(path.Points, fill);
            var stroke = path.Points.ToList();
            if (path.IsClosed && stroke.Count > 0) stroke.Add(stroke[0]);
            canvas.DrawPolyline(stroke, accent, selected ? 2.8 : 1.5, RasterLineCap.Butt, RasterLineJoin.Round, null);
        }
        if (!options.IncludeNodeLabels) return;
        var subtitleOffset = string.IsNullOrWhiteSpace(node.Subtitle) ? 0 : 20;
        var lines = NodeTextLines(node.Label, node.Width * .65, 12, true, options.MaxNodeLabelLines, options, node.MaximumLabelCharacters ?? NodeLabelMaxLength);
        DrawCenteredLines(canvas, CenterX(node), (node.Shape == TopologyNodeShape.Actor ? node.Y + 84 : (node.Details.Count > 0 || subtitleOffset > 0 ? node.Y + 24 : CenterY(node))) - (lines.Count - 1) * 7 - 7, lines, WithAlpha(Color(theme.Foreground), alpha), 12, true, 14);
        if (subtitleOffset > 0) DrawCenteredLines(canvas, CenterX(node), node.Y + 35, new[] { TrimToEstimatedWidth(node.Subtitle!, node.Width - 24, 10, false, options.TextMeasurement) }, WithAlpha(Color(theme.MutedForeground), alpha), 10, false, 12);
        for (var i = 0; i < node.Details.Count; i++) {
            if (i == 0) canvas.DrawLine(node.X + 12, node.Y + 34 + subtitleOffset, node.X + node.Width - 12, node.Y + 34 + subtitleOffset, WithAlpha(Color(theme.Border), alpha), 1);
            var detail = node.Details[i];
            var text = detail.Text ?? (detail.Label + " " + detail.Value);
            canvas.DrawText(node.X + 12, node.Y + 43 + subtitleOffset + i * 18, TrimToEstimatedWidth(text, node.Width - 24, 10, false, options.TextMeasurement), WithAlpha(Color(theme.Foreground), alpha), 10);
        }
    }
}
