using ChartForgeX.SvgRaster;
using ChartForgeX.Raster;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class LetterSpacingBeforeControls : IDisposable {
    private const string Family = "CFX Tracking Before";
    public LetterSpacingBeforeControls() => FontRegistry.Register(Family, OpenTypeTestFonts.NameKeyed());

    [Fact]
    public void AuthoredRelativeTrackingChangesNativeInkThroughTheExistingSvgImporter() {
        var ordinary = SvgRasterizer.ToImage(Source("normal"));
        var tracked = SvgRasterizer.ToImage(Source("0.06em"));
        Capture("normal", ordinary); Capture("tracked", tracked);
        Assert.NotEqual(ordinary.Pixels, tracked.Pixels);
    }
    [Fact]
    public void NormalAndExplicitZeroPreserveNativeBytes() {
        Assert.Equal(SvgRasterizer.ToImage(Source("normal")).Pixels, SvgRasterizer.ToImage(Source("0px")).Pixels);
        Assert.Equal(SvgRasterizer.ToImage(Source("0px")).Pixels, SvgRasterizer.ToImage(Source("0em")).Pixels);
    }
    [Fact]
    public void RelativeTrackingUsesTheFinalSameElementFontSizeRegardlessOfDeclarationOrder() {
        var first = SvgRasterizer.ToImage(Source("0.06em", "letter-spacing:0.06em;font-size:20px"));
        var second = SvgRasterizer.ToImage(Source("1.2px", "font-size:20px;letter-spacing:1.2px"));
        Assert.Equal(first.Pixels, second.Pixels);
    }
    private static string Source(string spacing, string? style = null) => "<svg xmlns='http://www.w3.org/2000/svg' width='120' height='64' viewBox='0 0 120 64'>"
        + "<text x='4' y='40' font-family='" + Family + "' font-size='20' letter-spacing='" + spacing + "' style='" + style + "'>HOO</text></svg>";
    private static void Capture(string name, RgbaImage image) {
        var directory = Environment.GetEnvironmentVariable("CFX_LETTER_SPACING_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory); File.WriteAllBytes(Path.Combine(directory, name + ".rgba"), image.Pixels);
        File.WriteAllText(Path.Combine(directory, name + ".dimensions.txt"), image.Width + "x" + image.Height);
    }
    public void Dispose() => FontRegistry.Clear();
}
