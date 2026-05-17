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
                .Attribute("r", GroupSymbolGlobeRadius)
                .Attribute("fill", "none")
                .Attribute("stroke", color)
                .Attribute("stroke-width", GroupSymbolGlobeOuterStrokeWidth));
            parent.Element("path", path => path
                .Attribute("d", "M " + F(cx - GroupSymbolGlobeHorizontalRadius) + " " + F(cy) + " H " + F(cx + GroupSymbolGlobeHorizontalRadius) + " M " + F(cx) + " " + F(cy - GroupSymbolGlobeRadius) + " C " + F(cx - GroupSymbolGlobeMeridianRadius) + " " + F(cy - 2.6) + " " + F(cx - GroupSymbolGlobeMeridianRadius) + " " + F(cy + 2.6) + " " + F(cx) + " " + F(cy + GroupSymbolGlobeRadius) + " M " + F(cx) + " " + F(cy - GroupSymbolGlobeRadius) + " C " + F(cx + GroupSymbolGlobeMeridianRadius) + " " + F(cy - 2.6) + " " + F(cx + GroupSymbolGlobeMeridianRadius) + " " + F(cy + 2.6) + " " + F(cx) + " " + F(cy + GroupSymbolGlobeRadius))
                .Attribute("fill", "none")
                .Attribute("stroke", color)
                .Attribute("stroke-width", GroupSymbolGlobeInnerStrokeWidth)
                .Attribute("stroke-linecap", "round"));
            return;
        }

        var icon = ResolveGroupIcon(group, options);
        if (icon != null && AddGroupIconSymbol(parent, icon, cx, cy, color, prefix, options)) return;

        if (string.IsNullOrWhiteSpace(symbol)) {
            parent.Element("circle", circle => circle
                .Attribute("cx", cx)
                .Attribute("cy", cy)
                .Attribute("r", GroupSymbolFallbackRadius)
                .Attribute("fill", "none")
                .Attribute("stroke", color)
                .Attribute("stroke-width", GroupSymbolFallbackStrokeWidth));
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
        if (TryDrawIconArtwork(parent, icon.Artwork, prefix, cx, cy, GroupSymbolArtworkSize)) return true;
        if (icon.Shape == TopologyIconShape.Cloud) {
            parent.Element("circle", circle => circle
                .Attribute("cx", cx + GroupSymbolCloudLeftOffsetX)
                .Attribute("cy", cy + GroupSymbolCloudLeftOffsetY)
                .Attribute("r", GroupSymbolCloudLeftRadius)
                .Attribute("fill", "none")
                .Attribute("stroke", color)
                .Attribute("stroke-width", GroupSymbolCloudStrokeWidth));
            parent.Element("circle", circle => circle
                .Attribute("cx", cx + GroupSymbolCloudRightOffsetX)
                .Attribute("cy", cy + GroupSymbolCloudRightOffsetY)
                .Attribute("r", GroupSymbolCloudRightRadius)
                .Attribute("fill", "none")
                .Attribute("stroke", color)
                .Attribute("stroke-width", GroupSymbolCloudStrokeWidth));
            return true;
        }

        var node = new TopologyNode { Id = "__group-icon", Label = icon.Label, IconId = icon.QualifiedId, Kind = icon.NodeKind, Symbol = icon.Symbol };
        return AddInfrastructureGlyph(parent, node, cx, cy, color, options);
    }
}
