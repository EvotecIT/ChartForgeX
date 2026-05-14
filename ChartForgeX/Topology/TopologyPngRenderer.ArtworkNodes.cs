using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

public sealed partial class TopologyPngRenderer {
    private static void DrawArtworkNodeFallback(RgbaCanvas canvas, TopologyNode node, TopologyTheme theme, ChartColor accent, bool isSelected, bool isHighlighted, TopologyHighlightState highlight, TopologyRenderOptions options) {
        if (node.Kind == TopologyNodeKind.Cloud || EffectiveIconShape(node, options) == TopologyIconShape.Cloud) {
            DrawCloudArtworkFallback(canvas, node, accent, isSelected);
        } else {
            canvas.FillRoundedRect(node.X + 2, node.Y + 5, node.Width, node.Height, 14, ChartColor.FromRgba(15, 23, 42, 18));
            canvas.FillRoundedRect(node.X, node.Y, node.Width, node.Height, 12, Color(NodeFill(node, theme, NodeAccentColor(node, theme, options), options)));
            canvas.StrokeRoundedRect(node.X, node.Y, node.Width, node.Height, 12, accent, isSelected ? 2.8 : 1.5);
            DrawNodeIcon(canvas, node, theme, accent, TopologyNodeDisplayMode.Card, options);
        }

        if (!isHighlighted && highlight.IsActive) canvas.FillRoundedRect(node.X, node.Y, node.Width, node.Height, 16, WithAlpha(Color(theme.Background), 185));
        DrawNodeBadge(canvas, node, theme, accent, TopologyNodeDisplayMode.Artwork);
    }

    private static void DrawCloudArtworkFallback(RgbaCanvas canvas, TopologyNode node, ChartColor accent, bool isSelected) {
        var fill = string.IsNullOrWhiteSpace(node.BackgroundColor) ? WithAlpha(accent, 34) : Color(node.BackgroundColor!);
        var stroke = WithAlpha(accent, isSelected ? (byte)210 : (byte)120);
        var y = node.Y + node.Height * 0.18;
        canvas.DrawCircle(node.X + node.Width * 0.28, y + node.Height * 0.42, node.Height * 0.24, fill);
        canvas.DrawCircle(node.X + node.Width * 0.46, y + node.Height * 0.28, node.Height * 0.30, fill);
        canvas.DrawCircle(node.X + node.Width * 0.66, y + node.Height * 0.40, node.Height * 0.26, fill);
        canvas.FillRoundedRect(node.X + node.Width * 0.18, y + node.Height * 0.36, node.Width * 0.60, node.Height * 0.34, node.Height * 0.17, fill);
        canvas.DrawCircleOutline(node.X + node.Width * 0.28, y + node.Height * 0.42, node.Height * 0.24, stroke, 1.2);
        canvas.DrawCircleOutline(node.X + node.Width * 0.46, y + node.Height * 0.28, node.Height * 0.30, stroke, 1.2);
        canvas.DrawCircleOutline(node.X + node.Width * 0.66, y + node.Height * 0.40, node.Height * 0.26, stroke, 1.2);
    }
}
