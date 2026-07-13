using System;
using System.Text;
using ChartForgeX.Interactivity;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlGraphExplorerRenderer {
    private static void WriteNodeDetails(StringBuilder writer, GraphSceneNode node, double size, bool includeText = true) {
        var textShape = node.Shape == GraphNodeShape.Text;
        if (includeText && !string.IsNullOrWhiteSpace(node.Style.LabelBackgroundColor)) {
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
            writer.Append("<text class=\"cfx-graph-node-label\" y=\"");
            writer.Append(Number(textShape ? 4 : size + 18));
            writer.Append('"');
            var labelStyle = NodeLabelStyle(node);
            if (!string.IsNullOrWhiteSpace(labelStyle)) Attribute(writer, "style", labelStyle);
            writer.Append('>');
            writer.Append(Text(node.Label));
            writer.Append("</text>");
        }

        if (includeText && !string.IsNullOrWhiteSpace(node.SecondaryLabel)) {
            writer.Append("<text class=\"cfx-graph-node-secondary\" y=\"");
            writer.Append(Number(textShape ? 18 : size + 32));
            writer.Append("\">");
            writer.Append(Text(node.SecondaryLabel!));
            writer.Append("</text>");
        }

        if (includeText && !string.IsNullOrWhiteSpace(node.BadgeText)) {
            var badge = node.BadgeText!.Length > 5 ? node.BadgeText.Substring(0, 5) : node.BadgeText;
            writer.Append("<g class=\"cfx-graph-node-badge\" transform=\"translate(");
            writer.Append(Number(size * 0.82));
            writer.Append(' ');
            writer.Append(Number(-size * 0.82));
            writer.Append(")\"><circle r=\"8\" style=\"fill:var(--cfx-color-text);stroke:var(--cfx-color-paper);stroke-width:2\"></circle><text y=\"3.5\" style=\"fill:var(--cfx-color-paper);stroke:none\">");
            writer.Append(Text(badge));
            writer.Append("</text></g>");
        }

        if (!string.IsNullOrWhiteSpace(node.Status) && !string.Equals(node.Status, "unknown", StringComparison.OrdinalIgnoreCase)) {
            var statusRadius = Math.Min(4.5, Math.Max(1.35, size * 0.28));
            writer.Append("<circle class=\"cfx-graph-node-status\" cx=\"");
            writer.Append(Number(-size * 0.8));
            writer.Append("\" cy=\"");
            writer.Append(Number(-size * 0.8));
            writer.Append("\" r=\"");
            writer.Append(Number(statusRadius));
            writer.Append('"');
            Attribute(writer, "style", "fill:" + NodeStatusColor(node.Status) + ";stroke:var(--cfx-color-paper);stroke-width:2");
            writer.Append("></circle>");
        }
    }

    private static string NodeStatusColor(string? status) {
        if (string.Equals(status, "healthy", StringComparison.OrdinalIgnoreCase)) return "#22c55e";
        if (string.Equals(status, "warning", StringComparison.OrdinalIgnoreCase)) return "#f59e0b";
        if (string.Equals(status, "critical", StringComparison.OrdinalIgnoreCase)) return "#ef4444";
        return "#94a3b8";
    }
}
