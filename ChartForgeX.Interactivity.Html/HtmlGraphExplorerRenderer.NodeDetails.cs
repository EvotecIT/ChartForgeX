using System;
using System.Text;
using ChartForgeX.Interactivity;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlGraphExplorerRenderer {
    private static void WriteNodeDetails(StringBuilder writer, GraphSceneNode node, double size, bool includeText = true) {
        var textShape = node.Shape == GraphNodeShape.Text;
        var cardNode = IsCardNode(node);
        var cardTextX = -BoxHalfWidth(size) + 52;
        var cardTextY = CardCaptionBaseline(node);
        if (includeText && !cardNode && !string.IsNullOrWhiteSpace(node.Style.LabelBackgroundColor)) {
            writer.Append("<rect class=\"cfx-graph-node-label-bg\" x=\"");
            writer.Append(Number(-Math.Max(24, node.Label.Length * 3.8)));
            writer.Append("\" y=\"");
            writer.Append(Number(textShape ? -9 : size + 7));
            writer.Append("\" width=\"");
            writer.Append(Number(Math.Max(48, node.Label.Length * 7.6)));
            writer.Append("\" height=\"18\" rx=\"5\"");
            Attribute(writer, "style", "fill:" + node.Style.LabelBackgroundColor + ";stroke:none;stroke-width:0;pointer-events:none");
            writer.Append("></rect>");
        }

        if (includeText) {
            writer.Append("<text class=\"cfx-graph-node-label");
            if (cardNode) writer.Append(" cfx-graph-node-card-label");
            writer.Append("\"");
            if (cardNode) Attribute(writer, "x", Number(cardTextX));
            if (cardNode) Attribute(writer, "data-cfx-full-label", node.Label);
            Attribute(writer, "y", Number(cardNode ? cardTextY : textShape ? 4 : size + 18));
            var labelStyle = NodeLabelStyle(node);
            if (!string.IsNullOrWhiteSpace(labelStyle)) Attribute(writer, "style", labelStyle);
            writer.Append('>');
            writer.Append(Text(cardNode ? CardText(node.Label, size) : node.Label));
            writer.Append("</text>");
        }

        if (includeText && !string.IsNullOrWhiteSpace(node.SecondaryLabel)) {
            writer.Append("<text class=\"cfx-graph-node-secondary");
            if (cardNode) writer.Append(" cfx-graph-node-card-secondary");
            writer.Append("\"");
            if (cardNode) Attribute(writer, "x", Number(cardTextX));
            if (cardNode) Attribute(writer, "data-cfx-full-label", node.SecondaryLabel);
            Attribute(writer, "y", Number(cardNode ? cardTextY + 19 : textShape ? 18 : size + 32));
            writer.Append('>');
            writer.Append(Text(cardNode ? CardText(node.SecondaryLabel!, size, true) : node.SecondaryLabel!));
            writer.Append("</text>");
        }

        if (includeText && !string.IsNullOrWhiteSpace(node.BadgeText)) {
            var bounds = NodeBadgeBounds(node, size);
            writer.Append("<g class=\"cfx-graph-node-badge\" transform=\"translate(");
            writer.Append(Number(bounds.Left + bounds.Width / 2));
            writer.Append(' ');
            writer.Append(Number(bounds.Top + 8));
            writer.Append(")\">");
            if (bounds.Width <= 16) writer.Append("<circle r=\"8\"");
            else writer.Append("<rect x=\"").Append(Number(-bounds.Width / 2)).Append("\" y=\"-8\" width=\"").Append(Number(bounds.Width)).Append("\" height=\"16\" rx=\"8\"");
            writer.Append(" style=\"fill:var(--cfx-color-text);stroke:var(--cfx-color-paper);stroke-width:2\"></").Append(bounds.Width <= 16 ? "circle" : "rect").Append('>');
            writer.Append("<text y=\"3.5\" style=\"fill:var(--cfx-color-paper);stroke:none\">");
            writer.Append(Text(node.BadgeText!));
            writer.Append("</text></g>");
        }

        if (!string.IsNullOrWhiteSpace(node.Status) && !string.Equals(node.Status, "unknown", StringComparison.OrdinalIgnoreCase)) {
            var statusRadius = Math.Min(4.5, Math.Max(1.35, size * 0.28));
            writer.Append("<circle class=\"cfx-graph-node-status\" cx=\"");
            writer.Append(Number(cardNode ? BoxHalfWidth(size) - 15 : -size * 0.8));
            writer.Append("\" cy=\"");
            writer.Append(Number(cardNode ? BoxHalfHeight(size) - 14 : -size * 0.8));
            writer.Append("\" r=\"");
            writer.Append(Number(statusRadius));
            writer.Append('"');
            Attribute(writer, "style", "fill:" + NodeStatusColor(node.Status) + ";stroke:var(--cfx-color-paper);stroke-width:2");
            writer.Append("></circle>");
        }
    }

    private const double BadgedCardCaptionClearance = 4;

    /// <summary>Reserves the badge's top strip while preserving unbadged card captions.</summary>
    private static double CardCaptionBaseline(GraphSceneNode node) =>
        -5 + (string.IsNullOrWhiteSpace(node.BadgeText) ? 0 : BadgedCardCaptionClearance);

    /// <summary>Measures the complete badge text and reserves padding without changing its corner anchor.</summary>
    private static ChartRect NodeBadgeBounds(GraphSceneNode node, double size) {
        var font = FontSpec.FromFamily("Inter, Segoe UI, Arial, sans-serif");
        font.Weight = 800;
        var width = Math.Max(16, TextLayoutEngine.Measure(node.BadgeText ?? string.Empty, new TextStyle { Font = font, FontSize = 7 }).Width + 8);
        var x = IsCardNode(node) ? BoxHalfWidth(size) - 18 : size * .82;
        var y = IsCardNode(node) ? -BoxHalfHeight(size) + 8 : -size * .82;
        return new ChartRect(x - width / 2, y - 8, width, 16);
    }

    private static string NodeStatusColor(string? status) {
        if (string.Equals(status, "healthy", StringComparison.OrdinalIgnoreCase)) return "#22c55e";
        if (string.Equals(status, "warning", StringComparison.OrdinalIgnoreCase)) return "#f59e0b";
        if (string.Equals(status, "critical", StringComparison.OrdinalIgnoreCase)) return "#ef4444";
        return "#94a3b8";
    }
}
