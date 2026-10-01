using ChartForgeX.Raster;
using ChartForgeX.SvgRaster;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>
/// Synthetic italic shears a regular face around the baseline and reserves the overhang only in the painted bounds, so
/// the next run starts where the regular advance ends. Every host's default sans has its own designed italic, which
/// slants differently, so the case registers the host's regular sans file alone under a name no host has: that family
/// has no italic face, and italic must be synthesized. The registry is process-wide, so this runs with its tests.
/// </summary>
[Collection(nameof(FontRegistryCollection))]
public sealed class SyntheticItalicTests : IDisposable {
    private const string Family = "CFX Regular Only Probe";

    public void Dispose() => FontRegistry.Clear();

    [Fact]
    public void SyntheticItalicDoesNotAdvanceTheNextSvgRun() {
        var regularPath = TypographyFontResolver.ResolveFace(null, 400, italic: false).Path;
        if (regularPath == null) return;
        FontRegistry.Register(Family, regularPath);
        Assert.True(TypographyFontResolver.ResolveFace(Family, 400, italic: true).SynthesizeItalic);

        var regular = Blue(Render(""));
        var italic = Blue(Render(" font-style='italic'"));
        Assert.True(regular.Found && italic.Found);
        Assert.InRange(italic.Left - regular.Left, -1, 1);
    }

    private static RgbaImage Render(string style) => RasterImageDecoder.Decode(SvgRasterizer.ToPng(
        "<svg xmlns='http://www.w3.org/2000/svg' width='140' height='50'><text x='8' y='38' font-size='32' font-family='" + Family + "'" + style +
        " fill='#ef4444'>AAAA<tspan fill='#2563eb'>B</tspan></text></svg>"));

    /// <summary>The leftmost column of the blue run's ink.</summary>
    private static (bool Found, int Left) Blue(RgbaImage image) {
        var left = int.MaxValue;
        for (var y = 0; y < image.Height; y++) {
            for (var x = 0; x < image.Width; x++) {
                var i = (y * image.Width + x) * 4;
                if (image.Pixels[i + 3] > 128 && image.Pixels[i + 2] > 180 && image.Pixels[i] < 90 && x < left) left = x;
            }
        }
        return (left != int.MaxValue, left);
    }
}
