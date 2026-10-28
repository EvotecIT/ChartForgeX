using System;
using System.IO;
using System.Linq;
using ChartForgeX.Raster;
using ChartForgeX.SvgRaster;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Current-public-API source correctness cases; no future compositing enum or overload is used.</summary>
public sealed class SvgFiniteCompositeSourceBeforeTests {
    [Theory]
    [InlineData("over", 170, 0, 85, 192, 255, 128)]
    [InlineData("in", 255, 0, 0, 64, 0, 0)]
    [InlineData("out", 255, 0, 0, 64, 0, 0)]
    [InlineData("atop", 128, 0, 127, 128, 255, 128)]
    [InlineData("xor", 128, 0, 128, 127, 255, 128)]
    public void FiniteFloodCompositeUsesTheExplicitSourceBackdropAndAlphaOperator(string operation,
        int red, int green, int blue, int alpha, int outsideBlue, int outsideAlpha) {
        var source = "<svg xmlns='http://www.w3.org/2000/svg' width='32' height='32'><defs>"
            + "<filter id='composite' filterUnits='userSpaceOnUse' primitiveUnits='userSpaceOnUse' x='0' y='0' width='32' height='32' color-interpolation-filters='sRGB'>"
            + "<feFlood flood-color='#0000ff' flood-opacity='0.5019607843137255' result='backdrop'/>"
            + "<feComposite in='SourceGraphic' in2='backdrop' operator='" + operation + "'/></filter></defs>"
            + "<rect id='source' x='8' y='8' width='16' height='16' fill='#ff0000' fill-opacity='0.5019607843137255' filter='url(#composite)'/></svg>";
        var result = SvgRasterizer.Rasterize(source, filters: new SvgRasterFilterOptions());
        Capture(operation, source, result);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(new byte[] { (byte)red, (byte)green, (byte)blue, (byte)alpha }, Pixel(result.Image, 16, 16));
        Assert.Equal(new byte[] { 0, 0, (byte)outsideBlue, (byte)outsideAlpha }, Pixel(result.Image, 2, 2));
        var strict = SvgRasterizer.Rasterize(source, strict: true, filters: new SvgRasterFilterOptions());
        Assert.Equal(result.Image.Pixels, strict.Image.Pixels);
    }

    private static byte[] Pixel(RgbaImage image, int x, int y) =>
        image.Pixels.Skip((y * image.Width + x) * 4).Take(4).ToArray();

    private static void Capture(string operation, string source, SvgRasterizationResult result) {
        var output = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output);
        var stem = Path.Combine(output, "finite-composite-before-" + operation);
        File.WriteAllText(stem + ".svg", source);
        File.WriteAllBytes(stem + ".native.png", PngWriter.WriteRgba(result.Image));
        File.WriteAllText(stem + ".diagnostics.txt", string.Join("\n", result.Diagnostics.Select(item => item.Code + ":" + item.ElementId + ":" + item.Message)));
    }
}
