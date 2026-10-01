namespace ChartForgeX.Topology;

public sealed partial class TopologySvgRenderer {
    /// <summary>
    /// Returns the rule that keeps status marks in their colours in forced-colours mode when the render opts in
    /// (<see cref="TopologyRenderOptions.PinStateColorsInForcedColors"/>): status leaves (group status dots, node detail
    /// status dots, legend swatches), edges, node status badges, callout status chips, and the arrow and endpoint markers
    /// of this render. Node and group cards are left out, so their labels keep following forced colours, and so are
    /// geographic region hulls, which lie under unpinned captions and labels. The root is matched as an attribute so
    /// sanitized ids that start with a digit still form a valid selector.
    /// </summary>
    private static string ForcedColorsRule(string id, TopologyRenderOptions options) {
        if (!options.PinStateColorsInForcedColors) return string.Empty;
        var root = "[id=\"" + id + "\"]";
        return root + " [data-cfx-status]:not(g):not([class$=\"__geo-region-hull\"])," +
            root + " [data-cfx-role=topology-edge]," +
            root + " [data-cfx-role=topology-node-status]," +
            root + " [data-cfx-role=topology-geographic-callout-status]," +
            "marker[id^=\"" + id + "-arrow-\"],marker[id^=\"" + id + "-circle-\"],marker[id^=\"" + id + "-diamond-\"]" +
            "{forced-color-adjust:none}";
    }
}
