using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Region and tile measurements retain signed scalar facts independently of their fixed geometry.</summary>
public sealed class SignedMapValueTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SignedLifecycleKeepsObservedZeroMissingIdentityAndDetachedExports(bool tile) {
        var values = new[] {
            new ChartRegionMapItem("Alpha", -4), new ChartRegionMapItem("A", 4),
            new ChartRegionMapItem("B", -8), new ChartRegionMapItem("C", 12)
        };
        var chart = Create(tile, values).WithMapColorScale(ChartColorScale.Sequential(ChartColor.Black, ChartColor.White));
        chart.ConfigureMapColorDimension(options => options.ValueFormat = Units());
        var series = chart.Series[0].WithObservationId(1, new ChartObservationId("measured-b")).WithPointColor(1, "#123456");
        Assert.Equal(new[] { 0d, -8, 12 }, series.RegionValues.Select(value => value.Value));
        var prepared = Prepare(chart); var svg = prepared.ToSvg(); var png = prepared.ToPng();
        var sources = Sources(svg, tile);
        Assert.Equal("0", sources["A"].Attribute("data-cfx-value")!.Value);
        Assert.Equal("false", sources["A"].Attribute("data-cfx-empty")!.Value);
        Assert.Equal("true", sources["D"].Attribute("data-cfx-empty")!.Value);
        Assert.Null(sources["D"].Attribute("data-cfx-observation-id"));
        Assert.Equal("-8.0 °C", sources["B"].Attribute("data-cfx-formatted-value")!.Value);
        Assert.Equal("#123456", sources["B"].Descendants().Single(element =>
            (string?)element.Attribute("data-cfx-role") == (tile ? "tile-map-region" : "region-map-region")).Attribute("fill")!.Value);

        series.UpdateRegionValue(1, new ChartRegionMapItem("Beta", -3));
        var updated = Sources(chart.ToSvg(), tile)["B"];
        Assert.Equal("-3", updated.Attribute("data-cfx-value")!.Value);
        Assert.Equal("measured-b", updated.Attribute("data-cfx-observation-id")!.Value);
        Assert.Equal("-3.0 °C", updated.Attribute("data-cfx-formatted-value")!.Value);
        var beforeFailure = chart.ToSvg();
        Assert.Throws<ArgumentOutOfRangeException>(() => series.ReplaceRegionValues(new[] {
            new ChartRegionMapItem("B", -double.MaxValue), new ChartRegionMapItem("Beta", -double.MaxValue)
        }));
        Assert.Equal(beforeFailure, chart.ToSvg());
        Assert.Equal("measured-b", series.ObservationIds[1].Value);
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartRegionMapItem("A", double.NegativeInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartRegionMapItem("A", double.NaN));

        series.ReplaceRegionValues(Array.Empty<ChartRegionMapItem>());
        Assert.All(Sources(chart.ToSvg(), tile).Values, source => Assert.Equal("true", source.Attribute("data-cfx-empty")!.Value));
        series.ReplaceRegionValues(new[] { new ChartRegionMapItem("Alpha", -7) });
        Assert.Equal(-7, Assert.Single(series.RegionValues).Value);
        Assert.Empty(series.ObservationIds);
        var refilled = Sources(chart.ToSvg(), tile);
        Assert.Equal("false", refilled["A"].Attribute("data-cfx-empty")!.Value);
        Assert.Equal("-7", refilled["A"].Attribute("data-cfx-value")!.Value);
        Assert.Equal(svg, prepared.ToSvg());
        Assert.Equal(png, prepared.ToPng());
    }

    [Theory]
    [InlineData(false, "automatic")]
    [InlineData(true, "automatic")]
    [InlineData(false, "diverging")]
    [InlineData(true, "diverging")]
    [InlineData(false, "partial")]
    [InlineData(true, "partial")]
    [InlineData(false, "positioned")]
    [InlineData(true, "positioned")]
    [InlineData(false, "discrete")]
    [InlineData(true, "discrete")]
    public void SignedScalesKeepMappedMarksRawValuesAndNativePixels(bool tile, string mode) {
        var red = ChartColor.FromHex("#FF0000");
        var scale = mode switch {
            "automatic" => ChartColorScale.Sequential(ChartColor.Black, ChartColor.White),
            "diverging" => ChartColorScale.Diverging(ChartColor.Black, red, ChartColor.White, 0).WithValueRange(-10, 10),
            "partial" => ChartColorScale.Sequential(ChartColor.Black, ChartColor.White).WithValueRange(-5, null),
            "positioned" => ChartColorScale.Sequential(new[] {
                new ChartColorStop(-10, ChartColor.Black), new ChartColorStop(0, red), new ChartColorStop(10, ChartColor.White)
            }),
            _ => ChartColorScale.Discrete(new[] {
                new ChartColorBand(0, ChartColor.Black), new ChartColorBand(10, red), new ChartColorBand(null, ChartColor.White)
            })
        };
        var chart = Create(tile, new[] {
            new ChartRegionMapItem("A", -10), new ChartRegionMapItem("B", 0), new ChartRegionMapItem("C", 10)
        }).WithMapColorScale(scale.WithNoDataColor(ChartColor.FromHex("#123456")));
        var prepared = Prepare(chart); var svg = prepared.ToSvg(); var sources = Sources(svg, tile);
        var expectedMiddle = mode == "automatic" ? "#808080" : mode == "partial" ? "#555555" : "#FF0000";
        var expected = new[] { "#000000", expectedMiddle, "#FFFFFF", "#123456" };
        var image = RasterImageDecoder.Decode(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        foreach (var (code, index) in new[] { ("A", 0), ("B", 1), ("C", 2), ("D", 3) }) {
            Assert.Equal(expected[index], sources[code].Descendants().Single(element =>
                (string?)element.Attribute("data-cfx-role") == (tile ? "tile-map-region" : "region-map-region")).Attribute("fill")!.Value);
            var id = index < 3 ? "series-0-point-" + index : (tile ? "tile-map-empty-" : "region-map-empty-") + code;
            var bounds = prepared.Regions.Single(region => region.Id == id).Bounds;
            AssertPixel(image, bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2, ChartColor.FromHex(expected[index]));
        }
        Assert.Equal("-10", sources["A"].Attribute("data-cfx-value")!.Value);
        if (mode == "discrete") {
            Assert.Equal("0", sources["A"].Attribute("data-cfx-band")!.Value);
            Assert.Equal("1", sources["B"].Attribute("data-cfx-band")!.Value);
            Assert.Equal("2", sources["C"].Attribute("data-cfx-band")!.Value);
        } else {
            var key = XDocument.Parse(svg).Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "map-scale");
            Assert.Equal(mode == "partial" ? "-5" : "-10", key.Attribute("data-cfx-min-value")!.Value);
            Assert.Equal("10", key.Attribute("data-cfx-max-value")!.Value);
        }
    }

    [Theory]
    [InlineData(-1e-9, 1e-9)]
    [InlineData(-4e-9, -2e-9)]
    [InlineData(-10, 10)]
    [InlineData(-double.MaxValue, double.MaxValue)]
    public void LegacySignedRangeKeepsEndpointsAndMidpointAtEveryMagnitude(double min, double max) {
        var midpoint = min / 2 + max / 2;
        Assert.Equal(0, ChartHeatmapSurface.Ratio(min, min, max));
        Assert.Equal(.5, ChartHeatmapSurface.Ratio(midpoint, min, max), 12);
        Assert.Equal(1, ChartHeatmapSurface.Ratio(max, min, max));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void LegacySignedMapKeyAgreesWithItsMarksAndConstantDomain(bool tile, bool relative) {
        foreach (var range in new[] { (-1e-9, 1e-9), (-double.MaxValue, double.MaxValue), (-7d, -7d) }) {
            var values = new[] { range.Item1, range.Item1 / 2 + range.Item2 / 2, range.Item2 };
            var chart = Create(tile, values.Select((value, index) => new ChartRegionMapItem(((char)('A' + index)).ToString(), value)));
            chart.Options.HeatmapScale = ChartHeatmapScale.Sequential;
            chart.Options.HeatmapRelativeScale = relative;
            var document = XDocument.Parse(chart.ToSvg()); var sources = Sources(document.ToString(), tile);
            var steps = document.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "map-scale-step").ToArray();
            var expectedSteps = new[] { 0, 2, 4 };
            for (var index = 0; index < 3; index++) {
                var mark = sources[((char)('A' + index)).ToString()].Descendants().Single(element =>
                    (string?)element.Attribute("data-cfx-role") == (tile ? "tile-map-region" : "region-map-region"));
                Assert.Equal(steps[expectedSteps[index]].Attribute("fill")!.Value, mark.Attribute("fill")!.Value);
            }
            var key = document.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "map-scale");
            Assert.Equal(range.Item1.ToString("R", CultureInfo.InvariantCulture), key.Attribute("data-cfx-min-value")!.Value);
            Assert.Equal(range.Item2.ToString("R", CultureInfo.InvariantCulture), key.Attribute("data-cfx-max-value")!.Value);
        }
        Assert.Equal(.5, ChartHeatmapSurface.Ratio(50, 40, 60));
        Assert.Equal(0, ChartHeatmapSurface.Ratio(-7, -7, -7));
    }

    private static Chart Create(bool tile, IEnumerable<ChartRegionMapItem> values) {
        var chart = Chart.Create().WithSize(760, 420).WithMapLabels(false);
        return tile ? chart.AddTileHeatmap("Temperature", new ChartTileMapDefinition("tiles", "Tiles", new[] {
            new ChartTileMapRegion("A", "Alpha", 0, 0), new ChartTileMapRegion("B", "Beta", 1, 0),
            new ChartTileMapRegion("C", "Gamma", 0, 1), new ChartTileMapRegion("D", "Delta", 1, 1)
        }), values) : chart.AddRegionHeatmap("Temperature", new ChartMapDefinition("regions", "Regions", 100, 100, new[] {
            new ChartMapRegion("A", "Alpha", "M0 0H50V50H0Z"), new ChartMapRegion("B", "Beta", "M50 0H100V50H50Z"),
            new ChartMapRegion("C", "Gamma", "M0 50H50V100H0Z"), new ChartMapRegion("D", "Delta", "M50 50H100V100H50Z")
        }), values);
    }

    private static ChartValueFormat Units() => ChartValueFormat.Number("0.0 '°C'", CultureInfo.InvariantCulture);
    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    private static Dictionary<string, XElement> Sources(string svg, bool tile) => XDocument.Parse(svg).Descendants()
        .Where(element => (string?)element.Attribute("data-cfx-role") == (tile ? "tile-map-region-source" : "region-map-region-source"))
        .ToDictionary(element => element.Attribute("data-cfx-region")!.Value);
    private static void AssertPixel(RgbaImage image, double x, double y, ChartColor expected) {
        var offset = ((int)y * image.Width + (int)x) * 4;
        Assert.Equal(expected.R, image.Pixels[offset]); Assert.Equal(expected.G, image.Pixels[offset + 1]);
        Assert.Equal(expected.B, image.Pixels[offset + 2]); Assert.Equal(expected.A, image.Pixels[offset + 3]);
    }
}
