using System.Linq;
using ChartForgeX.Topology;

namespace ChartForgeX.Mermaid;

public static partial class MermaidC4Rendering {
    // This is an explicit native projection of retained source legend fields. The pinned
    // source stores those fields but does not paint a legend, so no reference pixel identity is claimed.
    private static void ApplyC4Legend(TopologyChart chart, MermaidC4Document document) {
        TopologyLegend? legend = null;
        foreach (var element in document.Elements) {
            if (string.IsNullOrWhiteSpace(element.LegendText)) continue;
            var node = chart.Nodes.First(item => item.Id == element.Alias);
            var sprite = MermaidC4Shapes.Resolve(null, element.LegendSprite, null);
            var kind = sprite switch {
                TopologyNodeShape.Actor => TopologyNodeKind.Person,
                TopologyNodeShape.Cylinder => TopologyNodeKind.Database,
                TopologyNodeShape.HorizontalCylinder => TopologyNodeKind.Queue,
                _ => node.Kind
            };
            (legend ??= TopologyLegend.Create("Legend")).AddNodeKind(element.LegendText!, kind, node.Color, node.Symbol, node.BackgroundColor,
                sprite.HasValue ? null : node.IconId);
        }
        foreach (var boundary in document.Boundaries) {
            if (string.IsNullOrWhiteSpace(boundary.LegendText)) continue;
            var group = chart.Groups.First(item => item.Id == boundary.Id);
            (legend ??= TopologyLegend.Create("Legend")).AddNodeKind(boundary.LegendText!, TopologyNodeKind.Generic, group.BorderColor ?? group.Color, group.Symbol, group.BackgroundColor);
        }
        if (legend != null) chart.Legend = legend;
    }
}
