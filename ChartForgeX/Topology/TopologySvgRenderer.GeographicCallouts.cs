using System.Globalization;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Svg;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

public sealed partial class TopologySvgRenderer {
    private static void AddGeographicCallouts(SvgElement root, TopologyChart chart, string prefix, TopologyTheme theme, TopologyRenderOptions options, TopologyHighlightState highlight) {
        var callouts = TopologyGeographicCallouts.Build(chart, options, theme);
        if (callouts.Count == 0) return;

        var layer = new SvgElement("g")
            .Class(prefix + "__geo-callouts")
            .Attribute("data-cfx-role", "topology-geographic-callouts");
        foreach (var callout in callouts) {
            var group = callout.Group;
            var selected = IsSelected(options.SelectedGroupIds, group.Id);
            var highlighted = highlight.IsGroupHighlighted(group);
            var parent = AddOptionalLink(layer, group.Href, prefix, options);
            var element = parent.Element("g", item => {
                item
                    .Attribute("id", SafeElementId(chart.Id, "geo-callout", group.Id))
                    .Class(prefix + "__geo-callout " + prefix + "__group " + prefix + "__group--" + CssToken(group.Status.ToString()) + (selected ? " " + prefix + "--selected" : string.Empty) + highlight.CssClass(prefix, highlighted) + CustomCssClasses(group.CssClass))
                    .Attribute("data-cfx-role", "topology-group")
                    .Attribute("data-cfx-visual-role", "topology-geographic-callout")
                    .Attribute("data-group-id", group.Id)
                    .Attribute("data-group-layout-policy", group.LayoutPolicy.ToString())
                    .Attribute("data-group-applied-layout-policy", group.AppliedLayoutPolicy.ToString())
                    .Attribute("data-cfx-status", group.Status.ToString())
                    .Attribute("data-cfx-selected", selected)
                    .Attribute("data-group-longitude", group.Longitude.HasValue ? F(group.Longitude.Value) : null)
                    .Attribute("data-group-latitude", group.Latitude.HasValue ? F(group.Latitude.Value) : null)
                    .Attribute("data-group-geo-visible", group.Metadata.TryGetValue("geoVisible", out var visible) ? visible : null)
                    .Attribute("data-group-symbol", !string.IsNullOrWhiteSpace(group.Symbol) ? TrimTo(group.Symbol!.Trim(), 12) : null)
                    .Attribute("data-group-color", callout.AccentColor)
                    .Attribute("data-callout-placement", callout.Placement)
                    .Attribute("data-callout-anchor-x", callout.AnchorX)
                    .Attribute("data-callout-anchor-y", callout.AnchorY)
                    .Attribute("data-callout-node-count", callout.NodeCount)
                    .Attribute("data-callout-healthy-count", callout.HealthyCount)
                    .Attribute("data-callout-warning-count", callout.WarningCount)
                    .Attribute("data-callout-critical-count", callout.CriticalCount)
                    .Attribute("data-callout-unknown-count", callout.UnknownCount)
                    .Attribute("data-callout-disabled-count", callout.DisabledCount);
                AddTopologyDataAttributes(item, "data-cfx-meta-", group.Metadata, options.IncludeDataAttributes);
                if (highlight.IsActive && !highlighted) item.Attribute("opacity", highlight.DimmedOpacity);
            });

            if (options.IncludeTooltips && !string.IsNullOrWhiteSpace(group.Tooltip)) element.Element("title", title => title.Text(group.Tooltip!));
            AddGeographicCalloutBody(element, callout, prefix, theme, selected, chart.Id);
        }

        root.AddElement(layer);
    }

    private static void AddGeographicCalloutBody(SvgElement element, TopologyGeographicCallout callout, string prefix, TopologyTheme theme, bool selected, string? chartId) {
        var x = callout.X;
        var y = callout.Y;
        var leader = TopologyGeographicCalloutPrimitives.LeaderPoints(callout);
        var leaderStyle = ChartRouteVisualStyles.TopologyGeographicCalloutLeader();
        element.Element("polyline", line => line
            .Class(prefix + "__geo-callout-leader-halo")
            .Attribute("data-cfx-role", "topology-geographic-callout-leader-halo")
            .Attribute("points", string.Join(" ", leader.Select(point => F(point.X) + "," + F(point.Y))))
            .Attribute("stroke", theme.Background)
            .Attribute("stroke-width", leaderStyle.HaloStrokeWidth)
            .Attribute("stroke-linecap", "round")
            .Attribute("stroke-linejoin", "round")
            .Attribute("fill", "none")
            .Attribute("opacity", leaderStyle.HaloOpacity));
        element.Element("polyline", line => line
            .Class(prefix + "__geo-callout-leader")
            .Attribute("data-cfx-role", "topology-geographic-callout-leader")
            .Attribute("points", string.Join(" ", leader.Select(point => F(point.X) + "," + F(point.Y))))
            .Attribute("stroke", callout.AccentColor)
            .Attribute("stroke-width", leaderStyle.StrokeWidth)
            .Attribute("stroke-dasharray", F(leaderStyle.Dash) + " " + F(leaderStyle.Gap))
            .Attribute("stroke-linecap", "round")
            .Attribute("stroke-linejoin", "round")
            .Attribute("fill", "none")
            .Attribute("opacity", leaderStyle.StrokeOpacity));
        element.Element("circle", circle => circle
            .Class(prefix + "__geo-callout-anchor-halo")
            .Attribute("data-cfx-role", "topology-geographic-callout-anchor-halo")
            .Attribute("cx", callout.AnchorX)
            .Attribute("cy", callout.AnchorY)
            .Attribute("r", TopologyGeographicCalloutPrimitives.AnchorHaloRadius)
            .Attribute("fill", theme.Background));
        element.Element("circle", circle => circle
            .Class(prefix + "__geo-callout-anchor")
            .Attribute("data-cfx-role", "topology-geographic-callout-anchor")
            .Attribute("cx", callout.AnchorX)
            .Attribute("cy", callout.AnchorY)
            .Attribute("r", TopologyGeographicCalloutPrimitives.AnchorRadius)
            .Attribute("fill", callout.AccentColor)
            .Attribute("stroke", "none"));
        element.Element("rect", rect => rect
            .Class(prefix + "__geo-callout-card")
            .Attribute("x", x)
            .Attribute("y", y)
            .Attribute("width", callout.Width)
            .Attribute("height", callout.Height)
            .Attribute("rx", TopologyGeographicCalloutPrimitives.CardRadius)
            .Attribute("fill", theme.Card)
            .Attribute("stroke", callout.AccentColor)
            .Attribute("stroke-width", selected ? TopologyGeographicCalloutPrimitives.CardSelectedStrokeWidth : TopologyGeographicCalloutPrimitives.CardStrokeWidth)
            .Attribute("stroke-opacity", selected ? TopologyGeographicCalloutPrimitives.CardSelectedStrokeOpacity : TopologyGeographicCalloutPrimitives.CardStrokeOpacity)
            .Attribute("filter", "url(#" + SanitizeId(chartId ?? "topology") + "-shadow)"));
        element.Element("rect", rect => rect
            .Attribute("x", x)
            .Attribute("y", y)
            .Attribute("width", TopologyGeographicCalloutPrimitives.AccentStripWidth)
            .Attribute("height", callout.Height)
            .Attribute("rx", TopologyGeographicCalloutPrimitives.AccentStripRadius)
            .Attribute("fill", callout.AccentColor)
            .Attribute("opacity", TopologyGeographicCalloutPrimitives.AccentStripOpacity));
        element.Element("text", text => text
            .Attribute("x", x + TopologyGeographicCalloutPrimitives.TitleXOffset)
            .Attribute("y", y + TopologyGeographicCalloutPrimitives.TitleYOffset)
            .Attribute("fill", theme.Foreground)
            .Attribute("font-size", TopologyGeographicCalloutPrimitives.TitleFontSize)
            .Attribute("font-weight", "800")
            .Text(TrimTo(callout.Label, TopologyGeographicCalloutPrimitives.TitleMaxLength)));
        element.Element("text", text => text
            .Attribute("x", x + TopologyGeographicCalloutPrimitives.TitleXOffset)
            .Attribute("y", y + TopologyGeographicCalloutPrimitives.SubtitleYOffset)
            .Attribute("fill", theme.MutedForeground)
            .Attribute("font-size", TopologyGeographicCalloutPrimitives.SubtitleFontSize)
            .Text(TrimTo(callout.Subtitle, TopologyGeographicCalloutPrimitives.SubtitleMaxLength)));
        AddCalloutMiniTopology(element, callout, x + callout.Width - TopologyGeographicCalloutPrimitives.MiniTopologyRightInset, y + TopologyGeographicCalloutPrimitives.MiniTopologyYOffset, theme);
        AddCalloutStatusChips(element, callout, x + TopologyGeographicCalloutPrimitives.StatusChipsXOffset, y + TopologyGeographicCalloutPrimitives.StatusChipsYOffset, theme);
    }

    private static void AddCalloutMiniTopology(SvgElement element, TopologyGeographicCallout callout, double x, double y, TopologyTheme theme) {
        var center = TopologyGeographicCalloutPrimitives.MiniTopologyCenter(x, y);
        var leaderStyle = ChartRouteVisualStyles.TopologyGeographicMiniTopologyLeader();
        var points = TopologyGeographicCalloutPrimitives.MiniTopologyPoints(x, y);
        var statuses = TopologyGeographicCalloutPrimitives.PreviewStatuses(callout);
        element.Element("g", group => {
            group
                .Attribute("data-cfx-role", "topology-geographic-callout-mini-topology")
                .Attribute("data-group-id", callout.Group.Id);
            foreach (var point in points) {
                group.Element("line", line => line
                    .Attribute("x1", center.X)
                    .Attribute("y1", center.Y)
                    .Attribute("x2", point.X)
                    .Attribute("y2", point.Y)
                    .Attribute("stroke", callout.AccentColor)
                    .Attribute("stroke-width", leaderStyle.StrokeWidth)
                    .Attribute("stroke-dasharray", F(leaderStyle.Dash) + " " + F(leaderStyle.Gap))
                    .Attribute("opacity", leaderStyle.StrokeOpacity));
            }

            group.Element("circle", circle => circle
                .Attribute("cx", center.X)
                .Attribute("cy", center.Y)
                .Attribute("r", TopologyGeographicCalloutPrimitives.MiniTopologyCenterRadius)
                .Attribute("fill", callout.AccentColor)
                .Attribute("stroke", theme.Background)
                .Attribute("stroke-width", TopologyGeographicCalloutPrimitives.MiniTopologyCenterStrokeWidth));
            for (var i = 0; i < points.Length; i++) {
                var color = theme.StatusColor(statuses[i]);
                group.Element("circle", circle => circle
                    .Attribute("cx", points[i].X)
                    .Attribute("cy", points[i].Y)
                    .Attribute("r", TopologyGeographicCalloutPrimitives.MiniTopologyNodeRadius)
                    .Attribute("fill", color)
                    .Attribute("stroke", theme.Background)
                    .Attribute("stroke-width", TopologyGeographicCalloutPrimitives.MiniTopologyNodeStrokeWidth));
            }
        });
    }

    private static void AddCalloutStatusChips(SvgElement element, TopologyGeographicCallout callout, double x, double y, TopologyTheme theme) {
        var offset = 0.0;
        foreach (var chip in TopologyGeographicCalloutPrimitives.StatusChips(callout)) {
            var text = chip.Count.ToString(CultureInfo.InvariantCulture);
            var width = TopologyGeographicCalloutPrimitives.StatusChipBaseWidth + text.Length * TopologyGeographicCalloutPrimitives.StatusChipDigitWidth;
            var color = theme.StatusColor(chip.Status);
            element.Element("g", group => {
                group.Attribute("data-cfx-role", "topology-geographic-callout-status").Attribute("data-cfx-status", chip.Status.ToString());
                group.Element("rect", rect => rect
                    .Attribute("x", x + offset)
                    .Attribute("y", y)
                    .Attribute("width", width)
                    .Attribute("height", TopologyGeographicCalloutPrimitives.StatusChipHeight)
                    .Attribute("rx", TopologyGeographicCalloutPrimitives.StatusChipRadius)
                    .Attribute("fill", StatusFill(color, theme.Background))
                    .Attribute("stroke", color)
                    .Attribute("stroke-opacity", TopologyGeographicCalloutPrimitives.StatusChipStrokeOpacity));
                group.Element("circle", circle => circle
                    .Attribute("cx", x + offset + TopologyGeographicCalloutPrimitives.StatusChipDotX)
                    .Attribute("cy", y + TopologyGeographicCalloutPrimitives.StatusChipRadius)
                    .Attribute("r", TopologyGeographicCalloutPrimitives.StatusChipDotRadius)
                    .Attribute("fill", color));
                group.Element("text", textNode => textNode
                    .Attribute("x", x + offset + TopologyGeographicCalloutPrimitives.StatusChipTextX)
                    .Attribute("y", y + TopologyGeographicCalloutPrimitives.StatusChipTextSvgBaseline)
                    .Attribute("fill", color)
                    .Attribute("font-size", TopologyGeographicCalloutPrimitives.StatusChipTextFontSize)
                    .Attribute("font-weight", "800")
                    .Text(text));
            });
            offset += width + TopologyGeographicCalloutPrimitives.StatusChipGap;
        }
    }

}
