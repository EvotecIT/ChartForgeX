using ChartForgeX.Svg;

namespace ChartForgeX.Topology;

public sealed partial class TopologySvgRenderer {
    private static void AddLayoutDiagnosticOverlay(SvgElement root, TopologyChart chart, string prefix, TopologyRenderOptions options) {
        var report = TopologyLayoutDiagnostics.AnalyzePrepared(chart, options);
        var layer = new SvgElement("g").Class(prefix + "__layout-diagnostics").Attribute("data-cfx-role", "topology-layout-diagnostics").Attribute("pointer-events", "none");
        foreach (var group in report.Groups) AddDiagnosticRect(layer, group.Bounds, group.Id, "group", "#7C3AED");
        foreach (var node in report.Nodes) {
            AddDiagnosticRect(layer, node.Bounds, node.Id, "node", "#0284C7");
            foreach (var port in node.Ports) layer.Element("circle", circle => circle.Attribute("data-cfx-role", "topology-layout-port").Attribute("data-port-id", port.Id).Attribute("cx", port.Position.X).Attribute("cy", port.Position.Y).Attribute("r", 4).Attribute("fill", "#F59E0B").Attribute("stroke", "#FFFFFF").Attribute("stroke-width", 1));
        }
        foreach (var edge in report.Edges) foreach (var point in edge.Points) layer.Element("circle", circle => circle.Attribute("data-cfx-role", "topology-layout-route-point").Attribute("data-edge-id", edge.Id).Attribute("cx", point.X).Attribute("cy", point.Y).Attribute("r", 2.5).Attribute("fill", "#10B981"));
        foreach (var collision in report.Collisions) AddDiagnosticRect(layer, collision.Bounds, collision.FirstId + ":" + collision.SecondId, "collision", "#DC2626");
        root.AddElement(layer);
    }

    private static void AddDiagnosticRect(SvgElement layer, ChartForgeX.Primitives.ChartRect bounds, string id, string kind, string color) {
        layer.Element("rect", rect => rect.Attribute("data-cfx-role", "topology-layout-" + kind).Attribute("data-layout-id", id).Attribute("x", bounds.X).Attribute("y", bounds.Y).Attribute("width", bounds.Width).Attribute("height", bounds.Height).Attribute("fill", "none").Attribute("stroke", color).Attribute("stroke-width", kind == "collision" ? 2 : 1).Attribute("stroke-dasharray", kind == "collision" ? "none" : "5 4").Attribute("opacity", 0.85));
    }
}
