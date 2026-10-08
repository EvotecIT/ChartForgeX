using System;
using System.Xml.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void StaticSvgRetainsRegionIdentityWithoutBrowserBehavior() {
        var heatmap = Chart.Create().WithXLabels("A").AddHeatmapRow("Row", Points(42));
        var calendar = Chart.Create().AddCalendarHeatmap("Days", new[] { new ChartCalendarHeatmapItem(new DateTime(2026, 1, 5), 1) });
        var map = Chart.Create().AddDottedMap("Visited", new[] {
            new ChartMapPoint("Spain", -3.7038, 40.4168), new ChartMapPoint("Poland", 19.1451, 51.9194)
        }).AddMapRouteBetweenPoints("Spain to Poland", "Spain", "Poland");
        var states = Chart.Create().AddTileMap("Revenue", ChartTileMapCatalog.Get("us-states"), new[] { new ChartRegionMapItem("CA", 95) });
        foreach (var (chart, role) in new[] {
            (heatmap, "heatmap-cell"), (calendar, "calendar-cell"), (map, "dotted-map-point-source"),
            (map, "dotted-map-connector-source"), (states, "tile-map-region-source")
        }) {
            var svg = chart.ToSvg();
            var document = XDocument.Parse(svg);
            var regions = document.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == role).ToArray();
            Assert(regions.Length > 0, "Static SVG must retain source regions for " + role + ".");
            foreach (var region in regions) {
                Assert(!string.IsNullOrWhiteSpace((string?)region.Attribute("data-cfx-source-id")), "Static regions must retain stable source identities.");
                Assert(!string.IsNullOrWhiteSpace((string?)region.Attribute("aria-label")), "Static regions must retain accessible source descriptions.");
            }
            var ids = document.Descendants().Select(element => (string?)element.Attribute("id")).Where(id => id != null).ToArray();
            Assert(ids.Distinct(StringComparer.Ordinal).Count() == ids.Length, "Every SVG node identity must be unique.");
            Assert(!document.Descendants().Any(element => element.Name.LocalName == "script" || element.Attribute("tabindex") != null), "Static marks are one image; the HTML adapter owns keyboard navigation and browser scripts.");
            Assert(svg == chart.ToSvg(), "Repeated static exports must preserve deterministic geometry and identities.");
        }
    }
}
