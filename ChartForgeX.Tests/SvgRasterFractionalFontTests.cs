using System.Globalization;
using ChartForgeX.Raster;
using ChartForgeX.SvgRaster;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class SvgRasterFractionalFontTests {
    [Theory]
    [InlineData(.5)]
    [InlineData(6d / 7)]
    [InlineData(1)]
    public void FractionalFontAtHigherOutputDensityMatchesItsExplicitPixelSize(double size) {
        var logicalSize = size.ToString("R", CultureInfo.InvariantCulture);
        var pixelSize = (size * 16).ToString("R", CultureInfo.InvariantCulture);
        var actual = SvgRasterizer.ToImage("<svg xmlns='http://www.w3.org/2000/svg' width='32' height='12'><rect width='32' height='12' fill='white'/><text x='4' y='4' font-size='" + logicalSize + "'>HHH</text></svg>", 512, 192);
        var expected = SvgRasterizer.ToImage("<svg xmlns='http://www.w3.org/2000/svg' width='512' height='192'><rect width='512' height='192' fill='white'/><text x='64' y='64' font-size='" + pixelSize + "'>HHH</text></svg>");
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrEmpty(capture)) {
            Directory.CreateDirectory(capture);
            File.WriteAllBytes(Path.Combine(capture, "fractional-svg-" + logicalSize + ".png"), PngWriter.WriteRgba(actual.Width, actual.Height, actual.Pixels));
        }
        Assert.Equal(expected.Pixels, actual.Pixels);
    }

    [Fact]
    public void DownscaledTextKeepsItsRequestedAdvance() {
        var text = new string('H', 100);
        var image = SvgRasterizer.ToImage("<svg xmlns='http://www.w3.org/2000/svg' width='128' height='16'><text transform='scale(.5)' x='8' y='16' font-size='1' font-family='sans-serif'>" + text + "</text></svg>");
        var face = TypographyFontResolver.ResolveFace("sans-serif", 400, false).Font;
        var expectedWidth = RgbaCanvas.MeasureTextWidthWithFont(text, .5, face);
        var left = image.Width; var right = -1;
        for (var y = 0; y < image.Height; y++) for (var x = 0; x < image.Width; x++) {
            if (image.Pixels[(y * image.Width + x) * 4 + 3] == 0) continue;
            left = Math.Min(left, x); right = Math.Max(right, x);
        }
        Assert.True(right >= left, "Downscaled text should retain visible coverage.");
        Assert.InRange(right - left + 1, expectedWidth - 2, expectedWidth + 2);
    }
}
