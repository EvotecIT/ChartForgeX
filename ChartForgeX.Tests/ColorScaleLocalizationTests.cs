using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ColorScaleLocalizationTests {
    [Theory]
    [InlineData("region-map", true)]
    [InlineData("tile-map", true)]
    [InlineData("treemap", true)]
    [InlineData("sunburst", true)]
    [InlineData("region-map", false)]
    [InlineData("tile-map", false)]
    [InlineData("treemap", false)]
    [InlineData("sunburst", false)]
    public void LocalizedIntervalsKeepNumericBoundsAndPreparedOutputsDetached(string family, bool oneBand) {
        var scale = ChartColorScale.Discrete(oneBand
            ? new[] { new ChartColorBand(null, ChartColor.FromHex("#6F9ECE"), "Zakres") }
            : new[] { new ChartColorBand(10, ChartColor.FromHex("#DAE8F8"), "Niski"),
                new ChartColorBand(20, ChartColor.FromHex("#6F9ECE"), "Średni"), new ChartColorBand(null, ChartColor.FromHex("#1C5CAB"), "Wysoki") });
        var chart = Chart.Create().WithSize(800, 460).WithPngSupersampling(1);
        if (family == "sunburst") chart.AddSunburst("Podział", new[] {
            new ChartHierarchyItem("root", "Wszystko"),
            new ChartHierarchyItem("low", "Niski", "root", 4, 0),
            new ChartHierarchyItem("middle", "Średni", "root", 2, 10),
            new ChartHierarchyItem("high", "Wysoki", "root", 1, 20)
        }).ConfigureSunburst(options => options.ColorScale = scale);
        else if (family == "treemap") chart.AddTreemap("Podział", new[] {
            new ChartHierarchyItem("low", "Niski", value: 4, colorValue: 0),
            new ChartHierarchyItem("middle", "Średni", value: 2, colorValue: 10),
            new ChartHierarchyItem("high", "Wysoki", value: 1, colorValue: 20)
        }).ConfigureTreemap(options => options.ColorScale = scale);
        else {
            chart.WithMapLabels(false).WithMapColorScale(scale).WithMapScaleLegendPosition(ChartMapScaleLegendPosition.Right);
            var items = new[] { new ChartRegionMapItem("CA", 0), new ChartRegionMapItem("NY", 10), new ChartRegionMapItem("TX", 20) };
            if (family == "region-map") chart.AddRegionMap("Pomiar", ChartMapCatalog.Get("us-states"), items);
            else chart.AddTileMap("Pomiar", ChartTileMapCatalog.Get("us-states"), items);
        }
        var suffix = family + (oneBand ? "-single" : "-interior");
        Capture(chart.Prepare(VisualExportRequest.ForChart(chart).Context), suffix + "-english");
        chart.WithLabels(labels => { labels.AllValues = "Wszystkie wartości"; labels.Value = "wartość"; });
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var svg = prepared.ToSvg(); var png = prepared.ToPng(new VisualRenderOptions(supersampling: 1));
        Capture(prepared, suffix + "-localized");
        var document = XDocument.Parse(svg);
        var sourceRole = family is "treemap" or "sunburst" ? family + "-color-scale-step-source" : "map-scale-step-source";
        var bands = document.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == sourceRole).ToArray();
        var captions = bands.Select(node => (string?)node.Attribute("data-cfx-label")).ToArray();
        Assert.Equal(oneBand ? new[] { "Zakres · Wszystkie wartości" }
            : new[] { "Niski · < 10", "Średni · 10 ≤ wartość < 20", "Wysoki · ≥ 20" }, captions);
        Assert.Equal(oneBand ? new[] { "" } : new[] { "", "10", "20" }, bands.Select(node => (string?)node.Attribute("data-cfx-lower-bound")));
        Assert.Equal(oneBand ? new[] { "" } : new[] { "10", "20", "" }, bands.Select(node => (string?)node.Attribute("data-cfx-upper-bound")));
        chart.WithLabels(labels => { labels.AllValues = "Changed"; labels.Value = "changed"; });
        Assert.Equal(svg, prepared.ToSvg());
        Assert.Equal(png, prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        Assert.NotEqual(svg, chart.Prepare(VisualExportRequest.ForChart(chart).Context).ToSvg());
    }

    [Fact]
    public void RendererOwnedIntervalLabelsUseExistingNonEmptyValidation() {
        var labels = new ChartLabels();
        Assert.Equal("All values", labels.AllValues); Assert.Equal("value", labels.Value);
        Assert.Throws<ArgumentException>(() => labels.AllValues = null!);
        Assert.Throws<ArgumentException>(() => labels.AllValues = " ");
        Assert.Throws<ArgumentException>(() => labels.Value = null!);
        Assert.Throws<ArgumentException>(() => labels.Value = " ");
    }

    private static void Capture(PreparedVisual prepared, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, name + ".svg"), prepared.ToSvg());
        File.WriteAllBytes(Path.Combine(directory, name + ".png"), prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
    }
}
