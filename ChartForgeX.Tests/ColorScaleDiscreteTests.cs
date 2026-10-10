using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Discrete bands retain their exact numeric intervals, solid paints and immutable caller-facing state.</summary>
public sealed class ColorScaleDiscreteTests {
    private static readonly ChartColor Low = ChartColor.FromHex("#DAE8F8");
    private static readonly ChartColor Middle = ChartColor.FromHex("#6F9ECE");
    private static readonly ChartColor High = ChartColor.FromHex("#1C5CAB");
    private static readonly ChartColor Missing = ChartColor.FromHex("#E2E4E7");

    [Fact]
    public void Discrete_CopiesBandsAndSelectsTheNextBandAtEveryExclusiveBoundary() {
        var bands = new List<ChartColorBand> { new(10, Low, " Low "), new(20, Middle, "Middle"), new(null, High, "High") };
        var scale = ChartColorScale.Discrete(bands).WithNoDataColor(Missing);
        bands.Clear();
        Assert.Equal(ChartColorScaleMode.Discrete, scale.Mode);
        Assert.Equal("Low", scale.Bands[0].Label);
        Assert.Equal(new[] { Low, Middle, High }, scale.Colors);
        Assert.Equal(Missing, scale.NoDataColor);
        Assert.Throws<NotSupportedException>(() => ((IList<ChartColorBand>)scale.Bands)[0] = new(null, ChartColor.Black));
        Assert.Equal(Low, scale.ColorFor(-double.MaxValue));
        Assert.Equal(Low, scale.ColorFor(9.999999));
        Assert.Equal(Middle, scale.ColorFor(10));
        Assert.Equal(High, scale.ColorFor(20));
        Assert.Equal(High, scale.ColorFor(double.MaxValue));
        Assert.Equal(Middle, scale.ColorFor(10, 100, 100));
    }

    [Fact]
    public void Discrete_RejectsInvalidIntervalsAndContinuousOnlyOperations() {
        Assert.Throws<ArgumentNullException>(() => ChartColorScale.Discrete(null!));
        Assert.Throws<ArgumentException>(() => ChartColorScale.Discrete(Array.Empty<ChartColorBand>()));
        Assert.Throws<ArgumentException>(() => ChartColorScale.Discrete(new ChartColorBand[] { null! }));
        Assert.Throws<ArgumentException>(() => ChartColorScale.Discrete(new[] { new ChartColorBand(10, Low) }));
        Assert.Throws<ArgumentException>(() => ChartColorScale.Discrete(new[] { new ChartColorBand(null, Low), new ChartColorBand(null, High) }));
        Assert.Throws<ArgumentException>(() => ChartColorScale.Discrete(new[] { new ChartColorBand(10, Low), new ChartColorBand(10, Middle), new ChartColorBand(null, High) }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartColorBand(double.PositiveInfinity, Low));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartColorBand(double.NaN, Low));
        var scale = ChartColorScale.Discrete(new[] { new ChartColorBand(null, Low) });
        Assert.Equal(Low, scale.ColorFor(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => scale.ColorFor(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => scale.ColorFor(0, 0, double.PositiveInfinity));
        Assert.Throws<InvalidOperationException>(() => scale.WithValueRange(0, 100));
        Assert.Throws<InvalidOperationException>(() => scale.WithLabels("Low", null, "High"));
        Assert.Throws<InvalidOperationException>(() => scale.WithMidpoint(50));
        Assert.Throws<InvalidOperationException>(() => scale.EffectiveMidpoint(0, 100));
    }

    [Theory]
    [InlineData("region-map", ChartMapScaleLegendPosition.Bottom, 760, 420)]
    [InlineData("region-map", ChartMapScaleLegendPosition.Right, 760, 420)]
    [InlineData("tile-map", ChartMapScaleLegendPosition.Bottom, 760, 420)]
    [InlineData("tile-map", ChartMapScaleLegendPosition.Right, 760, 420)]
    [InlineData("region-map", ChartMapScaleLegendPosition.Right, 460, 300)]
    [InlineData("tile-map", ChartMapScaleLegendPosition.Right, 460, 300)]
    public void MapBands_DrawEveryNamedIntervalAndMatchSvgAndNativePng(string family, ChartMapScaleLegendPosition placement, int width, int height) {
        var chart = Map(family, placement).WithSize(width, height);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var document = XDocument.Parse(prepared.ToSvg(new VisualSvgOptions()));
        Assert.Equal("discrete", (string?)Role(document, "map-scale").Single().Attribute("data-cfx-scale-mode"));
        Assert.Equal(new[] { Low.ToHex(), Middle.ToHex(), High.ToHex() }, Role(document, "map-scale-step").Select(element => (string?)element.Attribute("fill")));
        var bands = Role(document, "map-scale-step-source").ToArray();
        Assert.Equal(new[] { "Low", "Middle", "High" }, bands.Select(element => (string?)element.Attribute("data-cfx-band-label")));
        Assert.Equal(new[] { "", "10", "20" }, bands.Select(element => (string?)element.Attribute("data-cfx-lower-bound")));
        Assert.Equal(new[] { "10", "20", "" }, bands.Select(element => (string?)element.Attribute("data-cfx-upper-bound")));
        Assert.Contains("Low · < 10", Role(document, "map-scale-band-label").Select(element => element.Value));
        Assert.Contains("Middle · 10 ≤ value < 20", Role(document, "map-scale-band-label").Select(element => element.Value));
        Assert.Contains("High · ≥ 20", Role(document, "map-scale-band-label").Select(element => element.Value));
        foreach (var (region, index) in new[] { ("CA", 0), ("NY", 1), ("TX", 2) }) {
            var source = Role(document, family + "-region-source").Single(element => (string?)element.Attribute("data-cfx-region") == region);
            Assert.Equal(index.ToString(), (string?)source.Attribute("data-cfx-band"));
            Assert.Equal(new[] { Low, Middle, High }[index].ToHex(), (string?)source.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == family + "-region").Attribute("fill"));
        }
        var image = RasterImageDecoder.Decode(prepared.ToPng());
        var swatches = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "map-scale-step").ToArray();
        Assert.Equal(3, swatches.Length);
        for (var index = 0; index < swatches.Length; index++) Assert.Equal(new[] { Low, Middle, High }[index], Pixel(image, swatches[index].Bounds));
        var missing = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Single(node => node.Role == "map-scale-no-data");
        Assert.Equal(Missing, Pixel(image, missing.Bounds));
    }

    [Fact]
    public void MapBands_BindRampRolesAndKeepLiteralPngAndThemeFallbackForMissingData() {
        var chart = Map("tile-map", ChartMapScaleLegendPosition.Bottom);
        var literalPng = chart.ToPng();
        chart.WithSvgColorVariables(new SvgColorVariables().Add("--series-same", Low, SvgColorRole.Series)
            .Add("--band-low", Low, SvgColorRole.Ramp).Add("--band-middle", Middle, SvgColorRole.Ramp)
            .Add("--band-high", High, SvgColorRole.Ramp).Add("--band-missing", Missing, SvgColorRole.Ramp));
        var themed = XDocument.Parse(chart.ToSvg());
        Assert.Equal(new[] { "var(--band-low, #DAE8F8)", "var(--band-middle, #6F9ECE)", "var(--band-high, #1C5CAB)" },
            Role(themed, "map-scale-step").Select(element => (string?)element.Attribute("fill")));
        Assert.Equal("var(--band-missing, #E2E4E7)", (string?)Role(themed, "map-scale-no-data").Single().Attribute("fill"));
        Assert.Equal(literalPng, chart.ToPng());
        chart.WithMapColorScale(ChartColorScale.Discrete(new[] { new ChartColorBand(null, Low) }));
        Assert.NotEqual(Low.ToCss(), (string?)Role(XDocument.Parse(chart.ToSvg()), "map-scale-no-data").Single().Attribute("fill"));
    }

    private static Chart Map(string family, ChartMapScaleLegendPosition placement) {
        var scale = ChartColorScale.Discrete(new[] { new ChartColorBand(10, Low, "Low"), new ChartColorBand(20, Middle, "Middle"), new ChartColorBand(null, High, "High") })
            .WithNoDataColor(Missing);
        var chart = Chart.Create().WithSize(760, 420).WithPngSupersampling(1).WithMapLabels(false).WithMapColorScale(scale).WithMapScaleLegendPosition(placement);
        var items = new[] { new ChartRegionMapItem("CA", 0), new ChartRegionMapItem("NY", 10), new ChartRegionMapItem("TX", 20) };
        return family == "region-map" ? chart.AddRegionMap("Dimension", ChartMapCatalog.Get("us-states"), items)
            : chart.AddTileMap("Dimension", ChartTileMapCatalog.Get("us-states"), items);
    }

    private static ChartColor Pixel(RgbaImage image, ChartRect bounds) {
        var offset = ((int)(bounds.Top + bounds.Height / 2) * image.Width + (int)(bounds.Left + bounds.Width / 2)) * 4;
        return ChartColor.FromRgba(image.Pixels[offset], image.Pixels[offset + 1], image.Pixels[offset + 2], image.Pixels[offset + 3]);
    }

    private static IEnumerable<XElement> Role(XDocument document, string role) =>
        document.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == role);
}
