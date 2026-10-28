using System;
using System.IO;
using System.Text.Json;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects the existing float-unassociation/linear-light caller before normalized helper extraction.</summary>
public sealed class ColrBlendExtractionBeforeControls {
    [Theory]
    [InlineData(24, 1044467770, 1046720295, 1033342287, 1063339949)]
    [InlineData(25, 1050790592, 1041328243, 1042401984, 1063339949)]
    [InlineData(26, 1044467770, 1046720295, 1033342287, 1063339949)]
    [InlineData(27, 1052284435, 1044315929, 1045389671, 1063339949)]
    public void ExistingLinearLightCallerKeepsExactFloatResultsAndOriginalFontPixels(int mode, int redBits,
        int greenBits, int blueBits, int alphaBits) {
        var source = new LinearRgba(.12, .24, .06, .6);
        var backdrop = new LinearRgba(.28, .07, .14, .7);
        var actual = LinearRgba.Composite(source, backdrop, mode);
        var bits = new[] { BitConverter.SingleToInt32Bits(actual.R), BitConverter.SingleToInt32Bits(actual.G),
            BitConverter.SingleToInt32Bits(actual.B), BitConverter.SingleToInt32Bits(actual.A) };
        using var stream = typeof(ColrBlendExtractionBeforeControls).Assembly
            .GetManifestResourceStream("ChartForgeX.Tests.Fixtures.OpenType.color-colr1.ttf")
            ?? throw new InvalidOperationException("The existing color-font control fixture is missing.");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        var font = TrueTypeFont.TryLoad(buffer.ToArray())
            ?? throw new InvalidOperationException("The existing color-font control fixture is invalid.");
        var canvas = new RgbaCanvas(220, 220, 1, font, 1, useDefaultOutlineFont: false);
        var drawn = font.Draw(canvas, 40, 40, char.ConvertFromUtf32(0xe000 + mode), ChartColor.Black, 100);
        var pixels = canvas.ToOutputPixels();
        var directory = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory);
            var stem = Path.Combine(directory, "colr-nonseparable-extraction-control-" + mode);
            File.WriteAllBytes(stem + ".native.png", PngWriter.WriteRgba(220, 220, pixels));
            File.WriteAllBytes(stem + ".rgba", pixels);
            File.WriteAllText(stem + ".float.json", JsonSerializer.Serialize(new { bits, mode, source = "color-colr1.ttf" }));
        }
        Assert.True(drawn);
        Assert.Equal(new[] { redBits, greenBits, blueBits, alphaBits }, bits);
        Assert.Contains(pixels, item => item != 0);
    }
}
