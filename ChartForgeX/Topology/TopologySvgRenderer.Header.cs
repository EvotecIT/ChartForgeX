using System;
using ChartForgeX.Svg;

namespace ChartForgeX.Topology;

public sealed partial class TopologySvgRenderer {
    private static void AddHeader(SvgElement root, TopologyChart chart, string prefix, TopologyTheme theme, TopologyRenderOptions options) {
        if (string.IsNullOrWhiteSpace(chart.Title) && string.IsNullOrWhiteSpace(chart.Subtitle)) return;
        if (options.HeaderStyle == TopologyHeaderStyle.CenterBanner && !string.IsNullOrWhiteSpace(chart.Title)) {
            AddCenterBannerHeader(root, chart, prefix, options);
            return;
        }

        var x = chart.Viewport.Padding;
        var y = chart.Viewport.Padding + 8;
        var header = new SvgElement("g")
            .Class(prefix + "__header")
            .Attribute("data-cfx-role", "topology-header")
            .Attribute("data-header-style", options.HeaderStyle.ToString());
        if (!string.IsNullOrWhiteSpace(chart.Title)) {
            header.Element("text", text => text
                .Attribute("x", x)
                .Attribute("y", y + 18)
                .Attribute("fill", theme.Foreground)
                .Attribute("font-size", 22)
                .Attribute("font-weight", "700")
                .Text(chart.Title!));
        }

        if (!string.IsNullOrWhiteSpace(chart.Subtitle)) {
            header.Element("text", text => text
                .Attribute("x", x)
                .Attribute("y", y + 42)
                .Attribute("fill", theme.MutedForeground)
                .Attribute("font-size", 13)
                .Text(chart.Subtitle!));
        }

        root.AddElement(header);
    }

    private static void AddCenterBannerHeader(SvgElement root, TopologyChart chart, string prefix, TopologyRenderOptions options) {
        var fontSize = 34.0;
        var availableWidth = Math.Max(0, chart.Viewport.Width - chart.Viewport.Padding * 2);
        var bannerWidth = Math.Min(availableWidth, Math.Max(360, chart.Title!.Length * 18.5 + 72));
        var bannerHeight = 58.0;
        var bannerX = (chart.Viewport.Width - bannerWidth) / 2;
        var bannerY = chart.Viewport.Padding + 2;
        var bannerHeader = new SvgElement("g")
            .Class(prefix + "__header " + prefix + "__header--center-banner")
            .Attribute("data-cfx-role", "topology-header")
            .Attribute("data-header-style", options.HeaderStyle.ToString());
        bannerHeader.Element("rect", rect => rect
            .Attribute("x", bannerX)
            .Attribute("y", bannerY)
            .Attribute("width", bannerWidth)
            .Attribute("height", bannerHeight)
            .Attribute("rx", 5)
            .Attribute("fill", "#050505"));
        bannerHeader.Element("text", text => text
            .Attribute("x", chart.Viewport.Width / 2)
            .Attribute("y", bannerY + 39)
            .Attribute("text-anchor", "middle")
            .Attribute("fill", "#FFFFFF")
            .Attribute("font-size", fontSize)
            .Attribute("font-weight", "700")
            .Text(chart.Title!));
        root.AddElement(bannerHeader);
    }
}
