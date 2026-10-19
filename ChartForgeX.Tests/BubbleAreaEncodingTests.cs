using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects truthful fill-area encoding through the actual shared marker shapes, native raster and size key.</summary>
public sealed class BubbleAreaEncodingTests {
    [Theory]
    [InlineData(ChartMarkerShape.Circle)]
    [InlineData(ChartMarkerShape.Square)]
    [InlineData(ChartMarkerShape.Diamond)]
    [InlineData(ChartMarkerShape.Triangle)]
    [InlineData(ChartMarkerShape.Plus)]
    [InlineData(ChartMarkerShape.Cross)]
    [InlineData(ChartMarkerShape.Heart)]
    [InlineData(ChartMarkerShape.Pin)]
    [InlineData(ChartMarkerShape.Star)]
    public void EqualValuesHaveEqualNativeFillAreaAcrossAllBuiltInShapes(ChartMarkerShape shape) {
        var prepared = Prepare(AreaChart(shape, 25));
        var smallArea = InkArea(prepared);
        var fullArea = InkArea(Prepare(AreaChart(shape, 100)));
        Assert.InRange(smallArea, Math.PI * 15 * 15 * .975, Math.PI * 15 * 15 * 1.025);
        Assert.InRange(fullArea / smallArea, 3.9, 4.1);
        Assert.Contains("data-cfx-size-mapping=\"linear-area\"", prepared.ToSvg());
        Assert.Equal("25", Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), node => node.Role == "point").Metadata["data-cfx-size"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AreaInterpolationUsesSquaredEndpointsAndTheKeySharesReferenceCircleArea(bool reversed) {
        var chart = AreaChart(ChartMarkerShape.Square, 25).WithSize(800, 500).WithLegend().ConfigureBubble(options => {
            options.MinimumRadius = 10; options.MaximumRadius = 30; options.Reversed = reversed;
            options.SizeDimension.ShowLegend = true;
        });
        var prepared = Prepare(chart);
        var source = Assert.Single(prepared.Regions, region => region.Role == "point");
        var ratio = reversed ? .75 : .25;
        var reference = Math.Sqrt(100 * (1 - ratio) + 900 * ratio);
        // A square's unit fill area is four; its radius bounds the same area as the reference circle.
        var expectedRadius = reference * Math.Sqrt(Math.PI / 4);
        Assert.Equal(expectedRadius * 2, source.Bounds.Width, 7);
        var key = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), node => node.Role == "bubble-size-key");
        Assert.Equal("linear-area", key.Metadata["data-cfx-size-mapping"]);
        var midpoint = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Single(node => node.Role == "bubble-size-key-sample"
            && node.Metadata["data-cfx-size-value"] == "50");
        Assert.Equal(Math.Sqrt(500), double.Parse(midpoint.Metadata["data-cfx-radius"], CultureInfo.InvariantCulture), 8);
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        chart.ConfigureBubble(options => options.SizeEncoding = ChartBubbleSizeEncoding.SquareRootRadius);
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        Assert.NotEqual(svg, Prepare(chart).ToSvg());
    }

    [Fact]
    public void EncodingConfigurationIsAtomicAndDefaultRadiusBehaviorRemainsExplicit() {
        var chart = AreaChart(ChartMarkerShape.Circle, 25);
        ChartBubbleOptions? captured = null;
        chart.ConfigureBubble(options => { captured = options; options.MinimumRadius = 10; options.SizeEncoding = ChartBubbleSizeEncoding.SquareRootRadius; });
        captured!.SizeEncoding = ChartBubbleSizeEncoding.Area;
        Assert.Equal(ChartBubbleSizeEncoding.SquareRootRadius, chart.Options.Bubble.SizeEncoding);
        Assert.Equal(20, Assert.Single(Prepare(chart).Scene.Nodes.OfType<VisualSceneEllipse>(), node => node.Role == "bubble").Rx);
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.ConfigureBubble(options => options.SizeEncoding = (ChartBubbleSizeEncoding)99));
        Assert.Equal(ChartBubbleSizeEncoding.SquareRootRadius, chart.Options.Bubble.SizeEncoding);
        chart.Series[0].UpdateBubble(0, new ChartBubble(5, 50, 100));
        Assert.Equal(30, Assert.Single(Prepare(chart).Scene.Nodes.OfType<VisualSceneEllipse>(), node => node.Role == "bubble").Rx);
    }

    private static Chart AreaChart(ChartMarkerShape shape, double size) {
        var chart = Chart.Create().WithSize(320, 240).WithHeader(false).WithLegend(false).WithDataLabels(false)
            .WithAxes(false).WithGrid(false).WithXAxisBounds(0, 10).WithYAxisBounds(0, 100)
            .AddBubble("Samples", new[] { new ChartBubble(5, 50, size) }).ConfigureBubble(options => {
                options.WithSizeDomain(0, 100); options.MinimumRadius = 0; options.MaximumRadius = 30;
                options.SizeEncoding = ChartBubbleSizeEncoding.Area;
            });
        chart.Series[0].ConfigureMarkers(options => { options.Shape = shape; options.StrokeWidth = 0; });
        chart.Options.ShowCard = false; chart.Options.ShowPlotBackground = false; chart.Options.TransparentBackground = true;
        return chart;
    }

    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    private static double InkArea(PreparedVisual prepared) {
        var image = RasterImageDecoder.Decode(prepared.ToPng(new VisualRenderOptions(supersampling: 2)));
        var sum = 0d; var maximum = 0;
        for (var offset = 3; offset < image.Pixels.Length; offset += 4) { sum += image.Pixels[offset]; maximum = Math.Max(maximum, image.Pixels[offset]); }
        Assert.True(maximum > 0);
        return sum / maximum;
    }
}
