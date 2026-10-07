using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MapTextParityTests {
    [Fact]
    public void RouteLabelsDeclareTheChartFamilyAndFallbackUsesTheSameNativeFace() {
        const string family = "CFX Missing Family, Arial, sans-serif";
        var chart = Map(family);
        var route = XDocument.Parse(chart.ToSvg()).Descendants().Single(e => (string?)e.Attribute("data-cfx-role") == "dotted-map-connector-label");
        Assert.Equal(family, (string?)route.Attribute("font-family"));
        Assert.Equal("700", (string?)route.Attribute("font-weight"));
        Assert.Equal(Map("Arial, sans-serif").ToRgbaImage().Pixels, chart.ToRgbaImage().Pixels);
    }

    private static Chart Map(string family) => Chart.Create().WithSize(540, 340)
        .WithTheme(ChartTheme.ReportLight().WithFontFamily(family)).WithLegend(false).WithDataLabels()
        .WithMapViewport(ChartMapViewport.Europe())
        .AddDottedMap("Places", new[] { new ChartMapPoint("Madrid", -3.7, 40.4), new ChartMapPoint("Warsaw", 21, 52.2) })
        .AddMapRouteBetweenPoints("Madrid to Warsaw", "Madrid", "Warsaw");
}
