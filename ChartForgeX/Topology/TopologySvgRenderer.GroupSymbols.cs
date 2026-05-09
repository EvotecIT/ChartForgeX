using System;
using ChartForgeX.Svg;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

public sealed partial class TopologySvgRenderer {
    private static void AddGroupSymbol(SvgElement parent, TopologyGroup group, double cx, double cy, string color, string prefix, TopologyRenderOptions options) {
        var symbol = string.IsNullOrWhiteSpace(group.Symbol) ? string.Empty : group.Symbol!.Trim();
        if (symbol.Equals("region", StringComparison.OrdinalIgnoreCase) || symbol.Equals("globe", StringComparison.OrdinalIgnoreCase)) {
            parent.Element("circle", circle => circle
                .Attribute("cx", cx)
                .Attribute("cy", cy)
                .Attribute("r", 5.8)
                .Attribute("fill", "none")
                .Attribute("stroke", color)
                .Attribute("stroke-width", 1.3));
            parent.Element("path", path => path
                .Attribute("d", "M " + F(cx - 5.2) + " " + F(cy) + " H " + F(cx + 5.2) + " M " + F(cx) + " " + F(cy - 5.8) + " C " + F(cx - 3.2) + " " + F(cy - 2.6) + " " + F(cx - 3.2) + " " + F(cy + 2.6) + " " + F(cx) + " " + F(cy + 5.8) + " M " + F(cx) + " " + F(cy - 5.8) + " C " + F(cx + 3.2) + " " + F(cy - 2.6) + " " + F(cx + 3.2) + " " + F(cy + 2.6) + " " + F(cx) + " " + F(cy + 5.8))
                .Attribute("fill", "none")
                .Attribute("stroke", color)
                .Attribute("stroke-width", 1.1)
                .Attribute("stroke-linecap", "round"));
            return;
        }

        var icon = ResolveGroupIcon(group, options);
        if (icon != null && AddGroupIconSymbol(parent, icon, cx, cy, color, prefix, options)) return;

        if (string.IsNullOrWhiteSpace(symbol)) {
            parent.Element("circle", circle => circle
                .Attribute("cx", cx)
                .Attribute("cy", cy)
                .Attribute("r", 4)
                .Attribute("fill", "none")
                .Attribute("stroke", color));
            return;
        }

        parent.Element("text", text => text
            .Attribute("x", cx)
            .Attribute("y", cy + 3.2)
            .Attribute("text-anchor", "middle")
            .Attribute("fill", color)
            .Attribute("font-size", 8)
            .Attribute("font-weight", "800")
            .Text(TrimTo(symbol, 3)));
    }

    private static bool AddGroupIconSymbol(SvgElement parent, TopologyIconDefinition icon, double cx, double cy, string color, string prefix, TopologyRenderOptions options) {
        if (TryDrawIconArtwork(parent, icon.Artwork, prefix, cx, cy, 20)) return true;
        if (icon.Shape == TopologyIconShape.Cloud) {
            parent.Element("circle", circle => circle
                .Attribute("cx", cx - 3)
                .Attribute("cy", cy + 1)
                .Attribute("r", 4.2)
                .Attribute("fill", "none")
                .Attribute("stroke", color)
                .Attribute("stroke-width", 1.3));
            parent.Element("circle", circle => circle
                .Attribute("cx", cx + 3)
                .Attribute("cy", cy - 1)
                .Attribute("r", 5)
                .Attribute("fill", "none")
                .Attribute("stroke", color)
                .Attribute("stroke-width", 1.3));
            return true;
        }

        var node = new TopologyNode { Id = "__group-icon", Label = icon.Label, IconId = icon.QualifiedId, Kind = icon.NodeKind, Symbol = icon.Symbol };
        return AddInfrastructureGlyph(parent, node, cx, cy, color, options);
    }
}
