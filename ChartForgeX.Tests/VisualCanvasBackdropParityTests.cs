using System.Xml.Linq;
using ChartForgeX.Composition;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class VisualCanvasBackdropParityTests {
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void NativeTechnologyHorizonFollowsItsAuthoredCubicOutline(int outputScale) {
        var canvas = VisualCanvas.Create(800, 400).WithBackground(ChartColor.White)
            .WithBackdrop(VisualCanvasBackdropStyle.TechHorizon).WithPngOutputScale(outputScale)
            .ConfigureStyle(style => style.TechHorizonFill = ChartColor.Black);
        var svg = XDocument.Parse(canvas.ToSvg());
        var horizon = svg.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "visual-canvas-horizon");
        Assert.Equal("M 0 312 C 160 288 304 328 440 304 C 592 280 672 340 800 292 L 800 400 L 0 400 Z", ((string?)horizon.Attribute("d"))?.Replace(",", string.Empty));
        var image = canvas.ToRgbaImage();

        // Independently evaluated points on the two cubic segments, rather than their control-point hull.
        var probes = new[] { (160, 304.750), (320, 311.863), (592, 302.849), (672, 310.673) };
        foreach (var (x, expectedY) in probes) {
            var actualY = FirstDarkPixel(image, x * outputScale, 270 * outputScale);
            Assert.InRange(actualY, (int)Math.Floor(expectedY * outputScale) - 1, (int)Math.Ceiling(expectedY * outputScale) + 1);
        }
    }

    [Fact]
    public void ExplicitHeroHaloPaintReachesBothStaticOutputsAndCanBeDisabled() {
        var halo = ChartColor.FromHex("#D01BCC");
        var canvas = VisualCanvas.Create(200, 160).WithBackdrop(VisualCanvasBackdropStyle.Transparent)
            .AddHeroBadge(40, 30, 120, 90, string.Empty)
            .ConfigureStyle(style => style.HeroBadgeGlowColor = halo);
        var colored = canvas.ToSvg();
        Assert.Contains(XDocument.Parse(colored).Descendants(), element => (string?)element.Attribute("flood-color") == halo.ToCss());
        var image = canvas.ToRgbaImage();
        Assert.Equal(halo, Pixel(image, 37, 75));

        canvas.ConfigureStyle(style => style.HeroBadgeGlowColor = ChartColor.Transparent);
        Assert.NotEqual(colored, canvas.ToSvg());
        Assert.Equal(0, Pixel(canvas.ToRgbaImage(), 37, 75).A);
    }

    private static int FirstDarkPixel(RgbaImage image, int x, int startY) {
        for (var y = startY; y < image.Height; y++) if (Pixel(image, x, y).R < 128) return y;
        return -1;
    }

    private static ChartColor Pixel(RgbaImage image, int x, int y) {
        var offset = (y * image.Width + x) * 4;
        return ChartColor.FromRgba(image.Pixels[offset], image.Pixels[offset + 1], image.Pixels[offset + 2], image.Pixels[offset + 3]);
    }
}
