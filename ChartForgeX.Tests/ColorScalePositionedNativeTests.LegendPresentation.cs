using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class ColorScalePositionedNativeTests {
    [Theory]
    [InlineData("region-map")]
    [InlineData("tile-map")]
    [InlineData("heatmap")]
    [InlineData("hexbin")]
    [InlineData("calendar")]
    [InlineData("scatter")]
    [InlineData("bubble")]
    [InlineData("treemap")]
    [InlineData("sunburst")]
    public void GradientKeysRetainExactAnchorsMappedMarksAndDetachedOutput(string family) {
        var scale = Scale(); var chart = Create(family, scale);
        var dimension = Dimension(chart, family); dimension.LegendStyle = ChartColorLegendStyle.Gradient;
        var prepared = Prepare(chart); var svg = prepared.ToSvg(); var png = prepared.ToPng(); var document = XDocument.Parse(svg);
        var gradient = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGradient>(), node => node.Role == Prefix(family) + "-gradient");
        Assert.Equal(scale.Stops.Select(stop => (stop.Value + 20) / 60), gradient.Stops.Select(stop => stop.Offset));
        Assert.Equal(scale.Stops.Select(stop => stop.Color), gradient.Stops.Select(stop => stop.Color));
        AssertMetadata(prepared, document, family, scale); AssertMappedMarks(prepared, document, family, scale);
        AssertGradientPixels(prepared, gradient, scale);
        Assert.Equal("gradient", (string?)Role(document, Prefix(family)).Single().Attribute("data-cfx-legend-style"));
        Assert.DoesNotContain(document.Descendants().Attributes(), attribute => attribute.Name.LocalName == "data-cfx-formatted-color-value");
        dimension.ShowLegend = false; dimension.LegendStyle = ChartColorLegendStyle.Swatches;
        SetScale(chart, family, scale.WithValueRange(-30, 60));
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        ScatterBubbleColorRenderingTests.Capture(prepared, "gradient-" + family + "-800-light");
    }

    [Theory]
    [InlineData("tile-map", 360, false, true)]
    [InlineData("region-map", 800, true, true)]
    [InlineData("heatmap", 360, true, false)]
    [InlineData("scatter", 800, false, false)]
    [InlineData("treemap", 360, false, false)]
    [InlineData("sunburst", 800, true, false)]
    public void GradientKeysKeepNumericMidpointPositionsAndReadableCaptions(string family, int width, bool dark, bool right) {
        var scale = Scale(); var chart = Create(family, scale, width, dark);
        Dimension(chart, family).LegendStyle = ChartColorLegendStyle.Gradient;
        if (right) chart.WithMapScaleLegendPosition(ChartMapScaleLegendPosition.Right);
        var prepared = Prepare(chart); var gradient = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGradient>(), node => node.Role == Prefix(family) + "-gradient");
        AssertGradientPixels(prepared, gradient, scale);
        var box = ((VisualSceneRectangle)gradient.Shape).Bounds;
        var id = family is "tile-map" or "region-map" ? right ? "map-scale-label-1" : "map-scale-midpoint" : Prefix(family) + "-label-midpoint";
        var caption = prepared.Scene.Nodes.OfType<VisualSceneText>().Single(node => node.Id == id);
        if (right) Assert.Equal(box.Bottom - box.Height / 3, prepared.Regions.Single(region => region.Id == id).Bounds.Top + prepared.Regions.Single(region => region.Id == id).Bounds.Height / 2, 6);
        else Assert.Equal(box.Left + box.Width / 3, caption.X, 6);
        Assert.Contains("Zero", prepared.Regions.Single(region => region.Id == id).Label);
        Assert.True(caption.Text.Size >= 8);
        ScatterBubbleColorRenderingTests.Capture(prepared, "gradient-" + family + "-" + width + "-" + (dark ? "dark" : "light") + (right ? "-right" : ""));
    }

    [Theory]
    [InlineData("region-map")]
    [InlineData("tile-map")]
    [InlineData("heatmap")]
    [InlineData("hexbin")]
    [InlineData("calendar")]
    [InlineData("scatter")]
    [InlineData("bubble")]
    [InlineData("treemap")]
    [InlineData("sunburst")]
    public void NumericVisibilityIsIndependentOfCategoryEntriesAndRetainsDiscreteIntervals(string family) {
        var scale = ChartColorScale.Discrete(new[] { new ChartColorBand(5, Cold, "Low"), new ChartColorBand(null, Hot, "High") }).WithNoDataColor(Missing);
        var chart = Create(family, scale).WithLegend(); chart.Series[0].ShowInLegend = false;
        var dimension = Dimension(chart, family); dimension.LegendStyle = ChartColorLegendStyle.Swatches;
        var visible = XDocument.Parse(Prepare(chart).ToSvg()); var prefix = Prefix(family);
        Assert.NotEmpty(Role(visible, prefix)); Assert.Empty(Role(visible, "legend-entry"));
        var bandSources = visible.Descendants().Where(node => node.Attribute("data-cfx-upper-exclusive") != null).ToArray();
        Assert.Equal(2, bandSources.Length); Assert.Equal("5", (string?)bandSources[0].Attribute("data-cfx-upper-bound"));
        dimension.ShowLegend = false; chart.Series[0].ShowInLegend = true;
        var hidden = XDocument.Parse(Prepare(chart).ToSvg());
        Assert.Empty(Role(hidden, prefix)); AssertMappedMarks(Prepare(chart), hidden, family, scale);
        if (family is "scatter" or "bubble" or "region-map" or "tile-map") Assert.NotEmpty(Role(hidden, "legend-entry"));
        dimension.ShowLegend = true; chart.WithLegend(false);
        Assert.Empty(Role(XDocument.Parse(Prepare(chart).ToSvg()), prefix));
        dimension.LegendStyle = ChartColorLegendStyle.Gradient;
        Assert.Contains("continuous", Assert.Throws<InvalidOperationException>(() => Prepare(chart)).Message);
    }

    [Fact]
    public void GradientRetainsEveryRgbaAnchorBeyondTheSwatchBudget() {
        var stops = Enumerable.Range(0, 16).Select(index => new ChartColorStop(index * index - 20,
            (index % 2 == 0 ? Cold : Hot).WithAlpha((byte)(37 + index * 11)))).ToArray();
        var scale = ChartColorScale.Sequential(stops); var chart = Create("heatmap", scale);
        Dimension(chart, "heatmap").LegendStyle = ChartColorLegendStyle.Gradient;
        var prepared = Prepare(chart); var gradient = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGradient>());
        Assert.Equal(16, gradient.Stops.Count); AssertGradientPixels(prepared, gradient, scale);
        AssertMetadata(prepared, XDocument.Parse(prepared.ToSvg()), "heatmap", scale);
        ScatterBubbleColorRenderingTests.Capture(prepared, "gradient-rgba-anchors");
    }

    [Theory]
    [InlineData("tile-map", true)]
    [InlineData("heatmap", false)]
    [InlineData("treemap", false)]
    public void SwatchesSampleContinuousValuesWithoutChangingTheirMapping(string family, bool right) {
        var scale = Scale(); var chart = Create(family, scale, 360);
        Dimension(chart, family).LegendStyle = ChartColorLegendStyle.Swatches;
        if (right) chart.WithMapScaleLegendPosition(ChartMapScaleLegendPosition.Right);
        var prepared = Prepare(chart); var document = XDocument.Parse(prepared.ToSvg());
        Assert.Empty(prepared.Scene.Nodes.OfType<VisualSceneGradient>());
        AssertKeyPaint(prepared, document, family, scale, right); AssertMappedMarks(prepared, document, family, scale);
        Assert.Equal("swatches", (string?)Role(document, Prefix(family)).Single().Attribute("data-cfx-legend-style"));
        ScatterBubbleColorRenderingTests.Capture(prepared, "swatches-" + family + "-360-light" + (right ? "-right" : ""));
    }

    [Theory]
    [InlineData(-20)]
    [InlineData(40)]
    public void CollapsedDivergingArmsDrawOnlyReachableColors(double midpoint) {
        var scale = ChartColorScale.Diverging(Cold, ChartColor.White, Hot, midpoint).WithValueRange(-20, 40);
        var chart = Create("scatter", scale); Dimension(chart, "scatter").LegendStyle = ChartColorLegendStyle.Gradient;
        var prepared = Prepare(chart); var gradient = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGradient>());
        Assert.Equal(2, gradient.Stops.Count);
        Assert.Equal(scale.ColorFor(-20), gradient.Stops[0].Color); Assert.Equal(scale.ColorFor(40), gradient.Stops[1].Color);
        AssertGradientPixels(prepared, gradient, scale);
    }

    [Fact]
    public void ConstantDomainKeyRemainsOneMappedColorWithoutInventingAValueRange() {
        var chart = Chart.Create().WithSize(800, 480).AddScatter("Same", new[] { new ChartPoint(1, 10).WithColorValue(7), new ChartPoint(2, 20).WithColorValue(7) });
        chart.Series[0].WithColorScale(ChartColorScale.Sequential(Cold, Hot)).ConfigureColorDimension(options => options.LegendStyle = ChartColorLegendStyle.Gradient);
        var prepared = Prepare(chart); var document = XDocument.Parse(prepared.ToSvg());
        Assert.Empty(prepared.Scene.Nodes.OfType<VisualSceneGradient>());
        Assert.Equal(Cold, prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Single(node => node.Role == "color-scale-0-gradient").Fill);
        var key = Role(document, "color-scale-0").Single();
        Assert.Equal("7", (string?)key.Attribute("data-cfx-min-value")); Assert.Equal("7", (string?)key.Attribute("data-cfx-max-value"));
    }

    [Theory]
    [InlineData(ChartSeriesKind.TileMap, "compact-gradient-color-legend", true)]
    [InlineData(ChartSeriesKind.Heatmap, "swatch-color-legend", false)]
    [InlineData(ChartSeriesKind.Scatter, "compact-gradient-color-legend", false)]
    [InlineData(ChartSeriesKind.Heatmap, "gradient-color-legend", true)]
    public void GalleryModelsPrepareBothKeyPresentationsAtTheirDeclaredSize(ChartSeriesKind kind, string variant, bool dark) {
        var compact = variant.StartsWith("compact-", StringComparison.Ordinal);
        var chart = V2GalleryModels.Create(kind, variant, dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithSize(compact ? 360 : 800, compact ? 360 : 440).WithTitle("Measured temperature")
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight());
        var prepared = Prepare(chart); var gradient = variant.Contains("gradient", StringComparison.Ordinal);
        Assert.Equal(gradient ? 1 : 0, prepared.Scene.Nodes.OfType<VisualSceneGradient>().Count());
        Assert.Contains("Temperature", prepared.ToSvg());
        ScatterBubbleColorRenderingTests.Capture(prepared, "example-" + kind + "-" + variant + "-" + (dark ? "dark" : "light"));
    }

    private static ChartColorDimensionOptions Dimension(Chart chart, string family) => family switch {
        "scatter" or "bubble" => chart.Series[0].ColorDimension,
        "treemap" => chart.Options.Treemap.ColorDimension, "sunburst" => chart.Options.Sunburst.ColorDimension,
        "region-map" or "tile-map" => chart.Options.MapColorDimension, _ => chart.Options.HeatmapColorDimension
    };

    private static void AssertGradientPixels(PreparedVisual prepared, VisualSceneGradient gradient, ChartColorScale scale) {
        var image = RasterImageDecoder.Decode(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        var box = ((VisualSceneRectangle)gradient.Shape).Bounds; var vertical = gradient.Start.X == gradient.End.X;
        foreach (var amount in new[] { .09, .29, .49, .69, .89 }) {
            var x = Math.Floor(vertical ? box.Left + box.Width / 2 : box.Left + amount * box.Width) + .5;
            var y = Math.Floor(vertical ? box.Bottom - amount * box.Height : box.Top + box.Height / 2) + .5;
            var ratio = vertical ? (box.Bottom - y) / box.Height : (x - box.Left) / box.Width;
            var value = ChartMath.InterpolateRange(scale.EffectiveMinimum(0), scale.EffectiveMaximum(100), ratio);
            AssertPixel(image, x, y, scale.ColorFor(value, 0, 100));
        }
    }
}
