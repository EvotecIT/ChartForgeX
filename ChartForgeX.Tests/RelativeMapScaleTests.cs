using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Core;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class RelativeMapScaleTests {
    [Fact]
    public void DefaultRegionAndTileScales_AgreeWithRelativeCellColors() {
        var values = new[] { new ChartRegionMapItem("CA", 1), new ChartRegionMapItem("NY", 2) };
        var region = Chart.Create().AddRegionHeatmap("Regions", ChartMapCatalog.Get("us-states"), values);
        var tile = Chart.Create().AddTileHeatmap("Tiles", ChartTileMapCatalog.Get("us-states"), values);
        foreach (var chart in new[] { region, tile }) {
            chart.Options.HeatmapRelativeScale = true;
            var svg = XDocument.Parse(chart.ToSvg());
            var highest = svg.Descendants().Single(element =>
                (string?)element.Attribute("data-cfx-role") is "region-map-region" or "tile-map-region" &&
                (string?)element.Attribute("data-cfx-region") == "NY");
            Assert.Equal("positive", (string)highest.Attribute("data-cfx-status")!);
            var steps = svg.Descendants().Where(element =>
                (string?)element.Attribute("data-cfx-role") is "region-map-scale-step" or "tile-map-scale-step").ToArray();
            Assert.Equal("0", (string)steps.First().Attribute("data-cfx-value")!);
            Assert.Equal("2", (string)steps.Last().Attribute("data-cfx-value")!);
            Assert.Equal("positive", (string)steps.Last().Attribute("data-cfx-status")!);
            Assert.Equal((string)highest.Attribute("fill")!, (string)steps.Last().Attribute("fill")!);
            Assert.True(chart.ToPng().Length > 200);
        }
    }
}
