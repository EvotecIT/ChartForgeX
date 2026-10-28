using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;

namespace ChartForgeX.Mermaid;

public static partial class MermaidCynefinRendering {
    // Pinned Cynefin styles use domainFontSize for labels/title(+2), itemFontSize for items,
    // and itemFontSize-1 for descriptions and transition labels. Only authored sizes override native defaults.
    private static void ApplyTitleTypography(TopologyChart chart, MermaidCynefinSettings? settings, MermaidTopologyRenderOptions options) {
        if (options.Presentation?.FontSize != null || settings?.AuthoredNumber("domainFontSize") is not double domain) return;
        var t = chart.Theme.Typography;
        chart.Theme = chart.Theme.WithTypography(new VisualTypography(t.Family, domain + 2, t.SubtitleSize, t.AxisSize, t.LegendSize,
            t.DataLabelSize, t.ScalarValueSize, t.CenterValueSize, t.AxisTitleSize, t.MonospaceFamily, t.TileLabelSize, t.TileValueSize, t.TileDetailSize));
    }

    private static void ApplyDomainTypography(TopologyChart chart, TopologyNode node, MermaidCynefinSettings? settings, MermaidTopologyRenderOptions options) {
        var hostSize = options.Presentation?.FontSize != null;
        var domain = hostSize ? null : settings?.AuthoredNumber("domainFontSize");
        var item = hostSize ? null : settings?.AuthoredNumber("itemFontSize");
        var color = settings?.AuthoredColor("textColor");
        var ordinary = chart.Theme.Typography.DataLabelSize;
        if (domain.HasValue) node.TextStyle = new TextStyleOverride { FontSize = domain };
        // A label-size override must not rescale an unauthored item or description role.
        if (item.HasValue || domain.HasValue) foreach (var detail in node.Details)
            detail.TextStyle = new TextStyleOverride { FontSize = item ?? ordinary * .8 };
        if (!string.IsNullOrWhiteSpace(node.Subtitle))
            node.SubtitleTextStyle = new TextStyleOverride { Italic = true, Color = color, FontSize = item.HasValue ? item - 1 : domain.HasValue ? ordinary * .85 : (double?)null };
        if (domain.HasValue || item.HasValue) node.FitLabelToContent = true;
    }

    private static void ApplyTransitionTypography(TopologyEdge edge, MermaidCynefinSettings? settings, MermaidTopologyRenderOptions options) {
        if (options.Presentation?.FontSize == null && settings?.AuthoredNumber("itemFontSize") is double item)
            edge.TextStyle = new TextStyleOverride { FontSize = item - 1 };
    }
}
