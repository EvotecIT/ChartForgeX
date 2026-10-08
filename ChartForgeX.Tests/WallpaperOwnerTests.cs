using System.Globalization;
using System.Xml.Linq;
using ChartForgeX;
using ChartForgeX.Composition;
using ChartForgeX.Core;
using ChartForgeX.Raster;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class WallpaperOwnerTests {
    [Theory]
    [InlineData("CenterRight", 2560, 1080)]
    [InlineData("ContrastBox", 2560, 1080)]
    [InlineData("RaisedSections", 2560, 1080)]
    [InlineData("CenterRight", 3840, 2160)]
    [InlineData("ContrastBox", 3840, 2160)]
    [InlineData("RaisedSections", 3840, 2160)]
    public void WallpaperExamplesKeepTheRequestedArtboardAndInsetTopology(string layout, int width, int height) {
        var canvas = WallpaperOwnerExamples.Create(layout, width, height);
        var layer = Assert.IsType<VisualCanvasImageLayer>(canvas.Layers.Last());
        Assert.Equal(width - 560 - 34, layer.X);
        Assert.Equal(height - 310 - 34, layer.Y);
        Assert.Equal(560, layer.Width); Assert.Equal(310, layer.Height);
        Assert.Equal(560, layer.SourceWidth); Assert.Equal(310, layer.SourceHeight);
        Assert.False(canvas.AnalyzeLayout().HasErrors);
        var svg = XDocument.Parse(canvas.ToSvg());
        Assert.Equal(width, (int)svg.Root!.Attribute("width")!);
        Assert.Equal(height, (int)svg.Root.Attribute("height")!);
        var image = RasterImageDecoder.Decode(canvas.ToPng());
        Assert.Equal(width, image.Width); Assert.Equal(height, image.Height);
    }

    [Theory]
    [InlineData(VisualCanvasInfoTileMiniChartKind.Sparkline)]
    [InlineData(VisualCanvasInfoTileMiniChartKind.Area)]
    [InlineData(VisualCanvasInfoTileMiniChartKind.Bars)]
    public void TileMiniChartsHandleZeroSamplesAndRespectAnExplicitMaximum(VisualCanvasInfoTileMiniChartKind kind) {
        foreach (var samples in new[] { new[] { 0d, 0d, 0d }, new[] { 0d, 10d, 20d } }) {
            var canvas = VisualCanvas.Create(480, 180).WithBackdrop(VisualCanvasBackdropStyle.Transparent)
                .AddInfoTile(20, 20, 440, 140, "CPU", "PROCESSOR", "20%", miniChartKind: kind,
                    miniChartValues: samples, miniChartMaximum: 100);
            var svg = XDocument.Parse(canvas.ToSvg());
            var mini = Assert.Single(svg.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "visual-canvas-info-tile-mini-chart");
            var frame = mini.Elements().First();
            var middle = Number(frame, "y") + Number(frame, "height") / 2;
            if (kind == VisualCanvasInfoTileMiniChartKind.Bars) {
                Assert.All(mini.Elements().Where(element => element.Name.LocalName == "rect").Skip(1), bar => {
                    Assert.True(Number(bar, "height") > 0);
                    Assert.True(Number(bar, "y") >= middle, "Small observations should stay in the lower half of an explicit 0..100 scale.");
                });
            } else {
                var trace = mini.Elements().Last();
                var points = ChartMapPathParser.ParseSubpaths((string)trace.Attribute("d")!, 1).SelectMany(path => path.Points).ToArray();
                Assert.NotEmpty(points);
                Assert.All(points, point => Assert.True(double.IsFinite(point.X) && double.IsFinite(point.Y) && point.Y >= middle));
                if (samples.All(value => value == 0)) Assert.Single(points.Select(point => point.Y).Distinct());
            }
            var image = RasterImageDecoder.Decode(canvas.ToPng());
            Assert.Equal(480, image.Width); Assert.Equal(180, image.Height);
            Assert.Equal(0, image.Pixels[3]);
        }
    }

    [Fact]
    public void TransparentTopologyOverlayPreservesAlphaAndJpegFlattensAgainstTheChosenBackground() {
        var topology = WallpaperOwnerExamples.CreateTopology();
        var image = topology.ToRgbaImage(new TopologyRenderOptions { IncludeTitle = false, IncludeLegend = false, FitContentToViewport = true, PngSupersamplingScale = 1 });
        Assert.Equal(560, image.Width); Assert.Equal(310, image.Height);
        Assert.Equal(0, image.Pixels[3]);
        Assert.Contains(image.Pixels.Where((_, index) => index % 4 == 3), alpha => alpha > 0);
        var background = ChartColor.FromRgb(23, 41, 67);
        var flattened = RasterImageDecoder.Decode(image.ToJpeg(new RasterImageOptions { Background = background, JpegQuality = 95 }));
        Assert.Equal(560, flattened.Width); Assert.Equal(310, flattened.Height);
        Assert.InRange((int)flattened.Pixels[0], background.R - 4, background.R + 4);
        Assert.InRange((int)flattened.Pixels[1], background.G - 4, background.G + 4);
        Assert.InRange((int)flattened.Pixels[2], background.B - 4, background.B + 4);
        Assert.Equal(255, flattened.Pixels[3]);
    }

    private static double Number(XElement element, string attribute) => double.Parse(element.Attribute(attribute)!.Value, CultureInfo.InvariantCulture);
}
