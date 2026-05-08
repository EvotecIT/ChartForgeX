using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Primitives;
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
        var leader = CalloutLeaderPoints(callout);
        element.Element("polyline", line => line
            .Class(prefix + "__geo-callout-leader-halo")
            .Attribute("data-cfx-role", "topology-geographic-callout-leader-halo")
            .Attribute("points", string.Join(" ", leader.Select(point => F(point.X) + "," + F(point.Y))))
            .Attribute("stroke", theme.Background)
            .Attribute("stroke-width", 4.8)
            .Attribute("stroke-linecap", "round")
            .Attribute("stroke-linejoin", "round")
            .Attribute("fill", "none")
            .Attribute("opacity", 0.76));
        element.Element("polyline", line => line
            .Class(prefix + "__geo-callout-leader")
            .Attribute("data-cfx-role", "topology-geographic-callout-leader")
            .Attribute("points", string.Join(" ", leader.Select(point => F(point.X) + "," + F(point.Y))))
            .Attribute("stroke", callout.AccentColor)
            .Attribute("stroke-width", 1.4)
            .Attribute("stroke-dasharray", "4 5")
            .Attribute("stroke-linecap", "round")
            .Attribute("stroke-linejoin", "round")
            .Attribute("fill", "none")
            .Attribute("opacity", 0.72));
        element.Element("circle", circle => circle
            .Class(prefix + "__geo-callout-anchor")
            .Attribute("cx", callout.AnchorX)
            .Attribute("cy", callout.AnchorY)
            .Attribute("r", 4.2)
            .Attribute("fill", callout.AccentColor)
            .Attribute("stroke", theme.Background)
            .Attribute("stroke-width", 2));
        element.Element("rect", rect => rect
            .Class(prefix + "__geo-callout-card")
            .Attribute("x", x)
            .Attribute("y", y)
            .Attribute("width", callout.Width)
            .Attribute("height", callout.Height)
            .Attribute("rx", 10)
            .Attribute("fill", theme.Card)
            .Attribute("stroke", callout.AccentColor)
            .Attribute("stroke-width", selected ? 2.4 : 1.2)
            .Attribute("stroke-opacity", selected ? 0.9 : 0.42)
            .Attribute("filter", "url(#" + SanitizeId(chartId ?? "topology") + "-shadow)"));
        element.Element("rect", rect => rect
            .Attribute("x", x)
            .Attribute("y", y)
            .Attribute("width", 5)
            .Attribute("height", callout.Height)
            .Attribute("rx", 2.5)
            .Attribute("fill", callout.AccentColor)
            .Attribute("opacity", 0.92));
        element.Element("text", text => text
            .Attribute("x", x + 18)
            .Attribute("y", y + 24)
            .Attribute("fill", theme.Foreground)
            .Attribute("font-size", 13)
            .Attribute("font-weight", "800")
            .Text(TrimTo(callout.Label, 18)));
        element.Element("text", text => text
            .Attribute("x", x + 18)
            .Attribute("y", y + 42)
            .Attribute("fill", theme.MutedForeground)
            .Attribute("font-size", 10.5)
            .Text(TrimTo(callout.Subtitle, 24)));
        AddCalloutMiniTopology(element, callout, x + callout.Width - 60, y + 19, theme);
        AddCalloutStatusChips(element, callout, x + 18, y + 58, theme);
    }

    private static void AddCalloutMiniTopology(SvgElement element, TopologyGeographicCallout callout, double x, double y, TopologyTheme theme) {
        var center = new ChartPoint(x + 24, y + 17);
        var points = new[] {
            new ChartPoint(x + 7, y + 9),
            new ChartPoint(x + 29, y + 5),
            new ChartPoint(x + 45, y + 12),
            new ChartPoint(x + 31, y + 30)
        };
        var statuses = CalloutPreviewStatuses(callout);
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
                    .Attribute("stroke-width", 1.1)
                    .Attribute("stroke-dasharray", "2.5 3")
                    .Attribute("opacity", 0.62));
            }

            group.Element("circle", circle => circle
                .Attribute("cx", center.X)
                .Attribute("cy", center.Y)
                .Attribute("r", 4)
                .Attribute("fill", callout.AccentColor)
                .Attribute("stroke", theme.Background)
                .Attribute("stroke-width", 1.6));
            for (var i = 0; i < points.Length; i++) {
                var color = theme.StatusColor(statuses[i]);
                group.Element("circle", circle => circle
                    .Attribute("cx", points[i].X)
                    .Attribute("cy", points[i].Y)
                    .Attribute("r", 3.8)
                    .Attribute("fill", color)
                    .Attribute("stroke", theme.Background)
                    .Attribute("stroke-width", 1.4));
            }
        });
    }

    private static TopologyHealthStatus[] CalloutPreviewStatuses(TopologyGeographicCallout callout) {
        var statuses = new List<TopologyHealthStatus>(4);
        AddStatuses(statuses, TopologyHealthStatus.Healthy, callout.HealthyCount);
        AddStatuses(statuses, TopologyHealthStatus.Warning, callout.WarningCount);
        AddStatuses(statuses, TopologyHealthStatus.Critical, callout.CriticalCount);
        AddStatuses(statuses, TopologyHealthStatus.Unknown, callout.UnknownCount + callout.DisabledCount);
        while (statuses.Count < 4) statuses.Add(callout.Group.Status);
        return statuses.Take(4).ToArray();
    }

    private static void AddStatuses(List<TopologyHealthStatus> statuses, TopologyHealthStatus status, int count) {
        for (var i = 0; i < count && statuses.Count < 4; i++) statuses.Add(status);
    }

    private static void AddCalloutStatusChips(SvgElement element, TopologyGeographicCallout callout, double x, double y, TopologyTheme theme) {
        var chips = new List<(TopologyHealthStatus Status, int Count)> {
            (TopologyHealthStatus.Healthy, callout.HealthyCount),
            (TopologyHealthStatus.Warning, callout.WarningCount),
            (TopologyHealthStatus.Critical, callout.CriticalCount),
            (TopologyHealthStatus.Unknown, callout.UnknownCount)
        };
        var offset = 0.0;
        foreach (var chip in chips) {
            if (chip.Count == 0) continue;
            var text = chip.Count.ToString(CultureInfo.InvariantCulture);
            var width = 30.0 + text.Length * 5.5;
            var color = theme.StatusColor(chip.Status);
            element.Element("g", group => {
                group.Attribute("data-cfx-role", "topology-geographic-callout-status").Attribute("data-cfx-status", chip.Status.ToString());
                group.Element("rect", rect => rect
                    .Attribute("x", x + offset)
                    .Attribute("y", y)
                    .Attribute("width", width)
                    .Attribute("height", 20)
                    .Attribute("rx", 10)
                    .Attribute("fill", StatusFill(color, theme.Background))
                    .Attribute("stroke", color)
                    .Attribute("stroke-opacity", 0.38));
                group.Element("circle", circle => circle
                    .Attribute("cx", x + offset + 10)
                    .Attribute("cy", y + 10)
                    .Attribute("r", 3.4)
                    .Attribute("fill", color));
                group.Element("text", textNode => textNode
                    .Attribute("x", x + offset + 19)
                    .Attribute("y", y + 13.5)
                    .Attribute("fill", color)
                    .Attribute("font-size", 9.5)
                    .Attribute("font-weight", "800")
                    .Text(text));
            });
            offset += width + 6;
        }
    }

    private static List<ChartPoint> CalloutLeaderPoints(TopologyGeographicCallout callout) {
        var connector = CalloutConnectorPoint(callout);
        if (callout.AnchorX < callout.X || callout.AnchorX > callout.X + callout.Width) {
            var side = callout.AnchorX < callout.X ? -1 : 1;
            var midX = (callout.AnchorX + connector.X) / 2;
            var guardX = connector.X + side * 24;
            if (side < 0) midX = Math.Min(midX, guardX);
            else midX = Math.Max(midX, guardX);
            return new List<ChartPoint> {
                new(callout.AnchorX, callout.AnchorY),
                new(midX, callout.AnchorY),
                new(midX, connector.Y),
                connector
            };
        }

        var sideY = callout.AnchorY < callout.Y ? -1 : 1;
        var midY = (callout.AnchorY + connector.Y) / 2;
        var guardY = connector.Y + sideY * 22;
        if (sideY < 0) midY = Math.Min(midY, guardY);
        else midY = Math.Max(midY, guardY);
        return new List<ChartPoint> {
            new(callout.AnchorX, callout.AnchorY),
            new(callout.AnchorX, midY),
            new(connector.X, midY),
            connector
        };
    }

    private static ChartPoint CalloutConnectorPoint(TopologyGeographicCallout callout) {
        var middleY = callout.Y + callout.Height / 2;
        if (callout.AnchorX < callout.X) return new ChartPoint(callout.X, middleY);
        if (callout.AnchorX > callout.X + callout.Width) return new ChartPoint(callout.X + callout.Width, middleY);
        return new ChartPoint(callout.X + callout.Width / 2, callout.AnchorY < callout.Y ? callout.Y : callout.Y + callout.Height);
    }
}
