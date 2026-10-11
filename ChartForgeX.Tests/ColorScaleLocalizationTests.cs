using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ColorScaleLocalizationTests {
    [Theory]
    [InlineData(ChartSeriesKind.RegionMap)]
    [InlineData(ChartSeriesKind.TileMap)]
    [InlineData(ChartSeriesKind.Heatmap)]
    [InlineData(ChartSeriesKind.HexbinHeatmap)]
    [InlineData(ChartSeriesKind.CalendarHeatmap)]
    [InlineData(ChartSeriesKind.Scatter)]
    [InlineData(ChartSeriesKind.Bubble)]
    [InlineData(ChartSeriesKind.Treemap)]
    [InlineData(ChartSeriesKind.Sunburst)]
    public void NamedDimensionsKeepLocalizedIntervalWordsAndCultureInCapturedExports(ChartSeriesKind kind) {
        var chart = kind == ChartSeriesKind.Sunburst
            ? Chart.Create().AddSunburst("Samples", new[] {
                new ChartHierarchyItem("root", "Root"),
                new ChartHierarchyItem("measured", "Measured", "root", 9, 7.125),
                new ChartHierarchyItem("zero", "Zero", "root", 4, 0),
                new ChartHierarchyItem("missing", "Missing", "root", 2)
            }) : ColorDimensionPresentationFixtures.Create(kind);
        chart.WithSize(800, 460).WithPngSupersampling(1)
            .ConfigureLabels(labels => { labels.Value = "wartość"; labels.Color = "Barwa"; });
        var scale = ChartColorScale.Discrete(new[] {
            new ChartColorBand(10.5, ChartColor.FromHex("#DAE8F8"), "Niski"),
            new ChartColorBand(20.5, ChartColor.FromHex("#6F9ECE"), "Średni"),
            new ChartColorBand(null, ChartColor.FromHex("#1C5CAB"), "Wysoki")
        });
        void Present(ChartColorDimensionOptions options) {
            options.Name = "Temperature";
            options.ValueFormat = ChartValueFormat.Number("0.0 '°C'", CultureInfo.GetCultureInfo("pl-PL"));
        }
        if (kind is ChartSeriesKind.RegionMap or ChartSeriesKind.TileMap)
            chart.WithMapColorScale(scale).ConfigureMapColorDimension(Present);
        else if (kind == ChartSeriesKind.Treemap)
            chart.ConfigureTreemap(options => { options.ColorScale = scale; Present(options.ColorDimension); });
        else if (kind == ChartSeriesKind.Sunburst)
            chart.ConfigureSunburst(options => { options.ColorScale = scale; Present(options.ColorDimension); });
        else if (kind is ChartSeriesKind.Scatter or ChartSeriesKind.Bubble)
            chart.Series[0].WithColorScale(scale).ConfigureColorDimension(Present);
        else chart.WithHeatmapColorScale(scale).ConfigureHeatmapColorDimension(Present);
        var prefix = kind switch {
            ChartSeriesKind.RegionMap or ChartSeriesKind.TileMap => "map-scale",
            ChartSeriesKind.Treemap => "treemap-color-scale",
            ChartSeriesKind.Sunburst => "sunburst-color-scale",
            ChartSeriesKind.CalendarHeatmap => "calendar-scale",
            ChartSeriesKind.Scatter or ChartSeriesKind.Bubble => "color-scale-0",
            _ => "heatmap-scale"
        };
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var svg = prepared.ToSvg(); var png = prepared.ToPng(new VisualRenderOptions(supersampling: 1));
        var document = XDocument.Parse(svg);
        var sourceRole = kind is ChartSeriesKind.RegionMap or ChartSeriesKind.TileMap or ChartSeriesKind.Treemap or ChartSeriesKind.Sunburst
            ? prefix + "-step-source" : prefix + "-step";
        var bands = document.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == sourceRole).ToArray();
        Assert.Equal(new[] { "Niski · < 10,5 °C", "Średni · 10,5 °C ≤ wartość < 20,5 °C", "Wysoki · ≥ 20,5 °C" },
            bands.Select(node => (string?)node.Attribute("data-cfx-label")));
        Assert.Equal(new[] { "", "10.5", "20.5" }, bands.Select(node => (string?)node.Attribute("data-cfx-lower-bound")));
        Assert.Equal(new[] { "10.5", "20.5", "" }, bands.Select(node => (string?)node.Attribute("data-cfx-upper-bound")));
        var titleRole = kind is ChartSeriesKind.Scatter or ChartSeriesKind.Bubble ? "color-scale-title" : prefix + "-title";
        Assert.Contains("Temperature", document.Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == titleRole).Value);
        Assert.Contains(document.Descendants(), node => (string?)node.Attribute("data-cfx-label-color") == "Temperature"
            || (string?)node.Attribute("data-cfx-label-value") == "Temperature");
        if (kind is ChartSeriesKind.Scatter or ChartSeriesKind.Bubble or ChartSeriesKind.Treemap or ChartSeriesKind.Sunburst)
            Assert.Contains(prepared.Regions, region => region.Label?.Contains("Temperature: 7,1 °C", StringComparison.Ordinal) == true);
        var options = kind == ChartSeriesKind.Sunburst ? chart.Options.Sunburst.ColorDimension : ColorDimensionPresentationFixtures.Options(chart, kind);
        chart.ConfigureLabels(labels => labels.Value = "pomiar");
        options.Name = "Changed"; options.ValueFormat = ChartValueFormat.InvariantCompact;
        Assert.Equal(svg, prepared.ToSvg());
        Assert.Equal(png, prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        var changed = chart.Prepare(VisualExportRequest.ForChart(chart).Context).ToSvg();
        Assert.Contains("10.5 ≤ pomiar &lt; 20.5", changed);
    }

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
        chart.ConfigureLabels(labels => { labels.AllValues = "Wszystkie wartości"; labels.Value = "wartość"; });
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
        chart.ConfigureLabels(labels => { labels.AllValues = "Changed"; labels.Value = "changed"; });
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
