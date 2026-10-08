using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>The flat bar style fills each bar with its colour only, in SVG and PNG.</summary>
public sealed class BarStyleTests {
    private static readonly ChartColor Blue = ChartColor.FromHex("#2A78D6");

    [Fact]
    public void Flat_VerticalBars_AreTheirColourWithoutGradientOrHighlight() {
        var chart = Chart.Create().WithSize(480, 300).WithBarStyle(ChartBarStyle.Flat).AddBar("Count", Values(3, 7, 5), Blue);
        Assert.Equal(ChartBarStyle.Flat, chart.Options.BarVisualStyle.Kind);
        var svg = XDocument.Parse(chart.ToSvg());
        var bars = ByRole(svg, "bar");
        Assert.Equal(3, bars.Length);
        Assert.All(bars, bar => {
            Assert.Equal(Blue.ToCss(), (string?)bar.Attribute("fill"));
            Assert.Null(bar.Attribute("opacity"));
        });
        Assert.Empty(ByRole(svg, "bar-highlight"));

        // The PNG bar is one colour from top to bottom.
        var image = PngReader.Decode(chart.ToPng());
        var scale = image.Width / 480.0;
        var tall = bars[1];
        var x = (Number(tall, "x") + Number(tall, "width") / 2) * scale;
        AssertPixel(image, x, (Number(tall, "y") + 4) * scale, Blue);
        AssertPixel(image, x, (Number(tall, "y") + Number(tall, "height") - 4) * scale, Blue);
    }

    [Fact]
    public void Flat_HorizontalAndHistogramBars_AreTheirColour() {
        var horizontal = XDocument.Parse(Chart.Create().WithSize(480, 300).WithBarStyle(ChartBarStyle.Flat).AddHorizontalBar("Count", Values(3, 7), Blue).ToSvg());
        Assert.Equal(2, ByRole(horizontal, "horizontal-bar").Length);
        Assert.All(ByRole(horizontal, "horizontal-bar"), bar => {
            Assert.Equal(Blue.ToCss(), (string?)bar.Attribute("fill"));
            Assert.Null(bar.Attribute("opacity"));
        });
        Assert.Empty(ByRole(horizontal, "bar-highlight"));

        var histogram = Chart.Create().WithSize(480, 300).WithBarStyle(ChartBarStyle.Flat).AddHistogram("Latency", new[] { 1d, 2d, 2d, 3d, 3d, 3d, 4d }, 4);
        Assert.NotEmpty(ByRole(XDocument.Parse(histogram.ToSvg()), "bar"));
        Assert.All(ByRole(XDocument.Parse(histogram.ToSvg()), "bar"), bar => Assert.DoesNotContain("url(", (string?)bar.Attribute("fill"), StringComparison.Ordinal));

        var solid = XDocument.Parse(Chart.Create().WithSize(480, 300).WithBarStyle(ChartBarStyle.Solid).AddBar("Count", Values(3, 7), Blue).ToSvg());
        Assert.All(ByRole(solid, "bar"), bar => Assert.StartsWith("url(", (string?)bar.Attribute("fill"), StringComparison.Ordinal));
        Assert.NotEmpty(ByRole(solid, "bar-highlight"));
    }

    private static void AssertPixel(RgbaImage image, double x, double y, ChartColor expected) {
        var offset = ((int)y * image.Width + (int)x) * 4;
        Assert.True(Math.Abs(image.Pixels[offset] - expected.R) <= 2 && Math.Abs(image.Pixels[offset + 1] - expected.G) <= 2 && Math.Abs(image.Pixels[offset + 2] - expected.B) <= 2,
            "Expected " + expected.ToHex() + " at " + x.ToString("0", CultureInfo.InvariantCulture) + "," + y.ToString("0", CultureInfo.InvariantCulture));
    }

    private static ChartPoint[] Values(params double[] values) => values.Select((value, index) => new ChartPoint(index + 1, value)).ToArray();

    private static XElement[] ByRole(XDocument svg, string role) => svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == role).ToArray();

    private static double Number(XElement element, string attribute) => double.Parse((string)element.Attribute(attribute)!, CultureInfo.InvariantCulture);
}
