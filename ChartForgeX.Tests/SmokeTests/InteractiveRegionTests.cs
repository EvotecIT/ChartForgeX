using System;
using ChartForgeX.Core;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void SvgInteractiveRegionsSupportStaticHoverAndFocus() {
        var heatmap = Chart.Create()
            .WithXLabels("A")
            .AddHeatmapRow("Row", Points(42))
            .ToSvg();
        Assert(heatmap.Contains(".cfx-interactive-region:hover", StringComparison.Ordinal) && heatmap.Contains(".cfx-interactive-region:focus", StringComparison.Ordinal), "SVG should include static hover and focus styling for interactive regions.");
        Assert(heatmap.Contains("stroke-width:var(--cfx-interactive-focus-stroke-width,2.2)", StringComparison.Ordinal), "Interactive SVG focus styling should use a role-specific stroke width variable.");
        Assert(heatmap.Contains(".cfx-interactive-region[data-cfx-role=\"dotted-map-connector\"]{pointer-events:stroke}", StringComparison.Ordinal), "Dotted map connectors should use stroke-only pointer targeting so routes remain easy to hover without blocking markers.");
        Assert(heatmap.Contains("class=\"cfx-interactive-region\" data-cfx-role=\"heatmap-cell\"", StringComparison.Ordinal), "Heatmap cells should be named interactive SVG regions without a tab stop of their own.");

        var calendar = Chart.Create()
            .AddCalendarHeatmap("Days", new[] { new ChartCalendarHeatmapItem(new DateTime(2026, 1, 5), 1) })
            .ToSvg();
        Assert(calendar.Contains("class=\"cfx-interactive-region\" data-cfx-role=\"calendar-heatmap-cell\"", StringComparison.Ordinal), "Calendar heatmap cells should be named interactive SVG regions without a tab stop of their own.");

        var map = Chart.Create()
            .AddDottedMap("Visited", new[] {
                new ChartMapPoint("Spain", -3.7038, 40.4168),
                new ChartMapPoint("Poland", 19.1451, 51.9194)
            })
            .AddMapRouteBetweenPoints("Spain to Poland", "Spain", "Poland")
            .ToSvg();
        Assert(map.Contains("class=\"cfx-interactive-region\" data-cfx-role=\"dotted-map-point\"", StringComparison.Ordinal), "Dotted map points should be named interactive SVG regions without a tab stop of their own.");
        Assert(map.Contains("class=\"cfx-interactive-region\" data-cfx-role=\"dotted-map-connector\"", StringComparison.Ordinal), "Dotted map connector routes should be named interactive SVG regions without a tab stop of their own.");
        Assert(map.Contains("style=\"--cfx-interactive-focus-stroke-width:", StringComparison.Ordinal), "Dotted map connector routes should keep a route-sized focus stroke instead of inheriting tiny cell defaults.");

        var states = Chart.Create()
            .AddTileMap("Revenue", ChartTileMapCatalog.Get("us-states"), new[] { new ChartRegionMapItem("CA", 95) })
            .ToSvg();
        // A static chart is one image for keyboard users; the interactive HTML adapter makes its marks focusable.
        foreach (var svg in new[] { heatmap, calendar, map, states }) Assert(!svg.Contains("tabindex=", StringComparison.Ordinal), "Static SVG marks should not be tab stops.");
        Assert(states.Contains("class=\"cfx-interactive-region\" data-cfx-role=\"tile-map-region\"", StringComparison.Ordinal), "Tile map regions should be named interactive SVG regions without a tab stop of their own.");
    }
}
