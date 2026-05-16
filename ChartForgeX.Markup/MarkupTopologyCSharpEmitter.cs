using System;
using System.Globalization;
using System.Text;
using ChartForgeX.Topology;

namespace ChartForgeX.Markup;

/// <summary>
/// Emits C# builder code for topology markup documents.
/// </summary>
public static class MarkupTopologyCSharpEmitter {
    /// <summary>
    /// Emits fluent C# code that rebuilds a topology document.
    /// </summary>
    /// <param name="document">The topology document.</param>
    /// <returns>C# source code.</returns>
    public static string Emit(MarkupTopologyDocument document) {
        if (document == null) throw new ArgumentNullException(nameof(document));
        var sb = new StringBuilder();
        sb.AppendLine("using ChartForgeX.Topology;");
        sb.AppendLine();
        sb.AppendLine("var topology = TopologyChart.Create()");
        sb.Append("    .WithViewport(").Append(Number(document.Width)).Append(", ").Append(Number(document.Height)).Append(", ").Append(Number(document.Padding)).AppendLine(")");
        sb.Append("    .WithLayout(TopologyLayoutMode.").Append(document.LayoutMode).Append(", TopologyLayoutDirection.").Append(document.LayoutDirection).AppendLine(")");
        if (!string.IsNullOrWhiteSpace(document.Id)) sb.Append("    .WithId(").Append(Literal(document.Id!)).AppendLine(")");
        if (!string.IsNullOrWhiteSpace(document.Title)) sb.Append("    .WithTitle(").Append(Literal(document.Title!)).AppendLine(")");
        if (!string.IsNullOrWhiteSpace(document.Subtitle)) sb.Append("    .WithSubtitle(").Append(Literal(document.Subtitle!)).AppendLine(")");

        foreach (var group in document.Groups) {
            sb.Append("    .AddGroup(").Append(Literal(group.Id)).Append(", ").Append(Literal(group.Label)).Append(", 0, 0, ").Append(Number(group.Width)).Append(", ").Append(Number(group.Height)).Append(", TopologyHealthStatus.").Append(group.Status);
            AppendNamed(sb, "subtitle", group.Subtitle);
            AppendNamed(sb, "color", group.Color);
            AppendNamed(sb, "iconId", group.Icon);
            sb.AppendLine(")");
        }

        foreach (var node in document.Nodes) {
            sb.Append("    .AddAutoNode(").Append(Literal(node.Id)).Append(", ").Append(Literal(node.Label)).Append(", TopologyNodeKind.").Append(node.Kind).Append(", TopologyHealthStatus.").Append(node.Status);
            AppendNamed(sb, "groupId", node.Group);
            AppendNamed(sb, "subtitle", node.Subtitle);
            AppendNamed(sb, "width", node.Width, 120);
            AppendNamed(sb, "height", node.Height, 64);
            AppendNamed(sb, "symbol", node.Symbol);
            AppendNamed(sb, "color", node.Color);
            AppendNamed(sb, "iconId", node.Icon);
            sb.AppendLine(")");
            if (!string.IsNullOrWhiteSpace(node.Badge)) sb.Append("    .WithNodeBadge(").Append(Literal(node.Id)).Append(", ").Append(Literal(node.Badge!)).AppendLine(")");
            if (node.Display.HasValue) sb.Append("    .WithNodeDisplay(").Append(Literal(node.Id)).Append(", TopologyNodeDisplayMode.").Append(node.Display.Value).AppendLine(")");
        }

        foreach (var edge in document.Edges) {
            sb.Append("    .AddEdge(").Append(Literal(edge.Id)).Append(", ").Append(Literal(edge.Source)).Append(", ").Append(Literal(edge.Target)).Append(", ");
            sb.Append(edge.Label == null ? "null" : Literal(edge.Label)).Append(", TopologyEdgeKind.").Append(edge.Kind).Append(", TopologyHealthStatus.").Append(edge.Status);
            sb.Append(", TopologyDirection.").Append(edge.Direction).Append(", TopologyEdgeRouting.").Append(edge.Routing).AppendLine(")");
        }

        sb.Length -= Environment.NewLine.Length;
        sb.AppendLine(";");
        return sb.ToString();
    }

    private static void AppendNamed(StringBuilder sb, string name, string? value) {
        if (string.IsNullOrWhiteSpace(value)) return;
        sb.Append(", ").Append(name).Append(": ").Append(Literal(value!));
    }

    private static void AppendNamed(StringBuilder sb, string name, double value, double defaultValue) {
        if (Math.Abs(value - defaultValue) < 0.000001) return;
        sb.Append(", ").Append(name).Append(": ").Append(Number(value));
    }

    private static string Literal(string value) => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    private static string Number(double value) => value.ToString("0.########", CultureInfo.InvariantCulture);
}
