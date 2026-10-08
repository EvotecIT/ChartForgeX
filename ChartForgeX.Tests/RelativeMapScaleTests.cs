using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class RelativeMapScaleTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RegionAndTileScalesAgreeWithRelativeCellColorsAndDeclaredSemantics(bool semantic) {
        var values = new[] { new ChartRegionMapItem("CA", 1), new ChartRegionMapItem("NY", 2) };
        var region = Chart.Create().AddRegionHeatmap("Regions", ChartMapCatalog.Get("us-states"), values);
        var tile = Chart.Create().AddTileHeatmap("Tiles", ChartTileMapCatalog.Get("us-states"), values);
        foreach (var chart in new[] { region, tile }) {
            chart.Options.HeatmapRelativeScale = true;
            chart.Options.HeatmapScale = semantic ? ChartHeatmapScale.Semantic : ChartHeatmapScale.Sequential;
            var svg = XDocument.Parse(chart.Prepare(VisualExportRequest.ForChart(chart).Context).ToSvg(new VisualSvgOptions()));
            var highest = svg.Descendants().Single(element =>
                (string?)element.Attribute("data-cfx-role") is "region-map-region-source" or "tile-map-region-source" &&
                (string?)element.Attribute("data-cfx-region") == "NY");
            Assert.Equal("4", (string?)highest.Attribute("data-cfx-level"));
            if (semantic) Assert.Equal("positive", (string?)highest.Attribute("data-cfx-status"));
            else Assert.Null(highest.Attribute("data-cfx-status"));
            var steps = svg.Descendants().Where(element =>
                (string?)element.Attribute("data-cfx-role") == "map-scale-step").ToArray();
            var sources = svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "map-scale-step-source").ToArray();
            Assert.Equal("0", (string)sources.First().Attribute("data-cfx-value")!);
            Assert.Equal("2", (string)sources.Last().Attribute("data-cfx-value")!);
            var mark = highest.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") is "region-map-region" or "tile-map-region");
            Assert.Equal((string)mark.Attribute("fill")!, (string)steps.Last().Attribute("fill")!);
            Assert.True(chart.ToPng().Length > 200);
        }
    }
}
