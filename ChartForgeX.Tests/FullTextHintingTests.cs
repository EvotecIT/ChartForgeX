using ChartForgeX.Composition;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class FullTextHintingTests {
    private static TrueTypeFont Face(string name) {
        using var source = typeof(FullTextHintingTests).Assembly.GetManifestResourceStream("ChartForgeX.Tests.Fixtures.OpenType." + name)!;
        using var bytes = new MemoryStream(); source.CopyTo(bytes);
        return TrueTypeFont.TryLoad(bytes.ToArray())!;
    }

    [Theory]
    [InlineData("stem-true-type.ttf")]
    [InlineData("stem-compact.otf")]
    public void NarrowStemsBecomeOpaqueColumnsWithoutChangingLayout(string name) {
        var font = Face(name);
        var auto = Draw(font, "I I", 10, TextHinting.Auto);
        var full = Draw(font, "I I", 10, TextHinting.Full);
        // Original 0.8px stems start on fractional columns. The full fit produces a one-pixel stem.
        Assert.InRange(Strongest(auto), 1, 240);
        Assert.Equal(255, Strongest(full));
        Assert.True(Alpha(full) > Alpha(auto));
        var style = new TextStyle { FontSize = 10, Hinting = TextHinting.Auto };
        var before = TextLayoutEngine.Layout("Hamburg Hamburg", 48, style, TextWrapMode.Word);
        style.Hinting = TextHinting.Full;
        var after = TextLayoutEngine.Layout("Hamburg Hamburg", 48, style, TextWrapMode.Word);
        Assert.Equal(before.Lines.Select(line => (line.Text, line.Width)), after.Lines.Select(line => (line.Text, line.Width)));
    }

    [Theory]
    [InlineData("stem-true-type.ttf")]
    [InlineData("stem-compact.otf")]
    public void CountersRemainOpenAndOppositeWindingAgrees(string name) {
        var font = Face(name);
        var pixels = Draw(font, "O", 10, TextHinting.Full);
        Assert.Equal(0, pixels[(6 * 80 + 5) * 4 + 3]);
        Assert.Equal(255, pixels[(6 * 80 + 3) * 4 + 3]);
        Assert.Equal(255, pixels[(6 * 80 + 6) * 4 + 3]);
    }

    [Fact]
    public void FullModeRespectsOutputDensityAndLeavesLargeOrSlantedOutlinesAlone() {
        var font = Face("stem-true-type.ttf");
        Assert.Equal(Draw(font, "I", 13, TextHinting.None), Draw(font, "I", 13, TextHinting.Full));
        Assert.Equal(Draw(font, "I", 10, TextHinting.Auto, italic: true), Draw(font, "I", 10, TextHinting.Full, italic: true));
        Assert.Equal(Draw(font, "N", 10, TextHinting.Auto), Draw(font, "N", 10, TextHinting.Full));
        Assert.Equal(Draw(font, "I", 10, TextHinting.None, density: 2), Draw(font, "I", 10, TextHinting.Full, density: 2));
        Assert.Equal(255, Strongest(Draw(font, "I", 5, TextHinting.Full, density: 2)));
    }

    [Fact]
    public void AmbiguousNarrowCountersAndExcessiveOutlinesRetainVerticalFit() {
        var contours = new List<List<ChartPoint>> {
            Ring(2.1, 3.7, 2, 9), Ring(2.7, 3.1, 3, 8).AsEnumerable().Reverse().ToList()
        };
        var original = contours.SelectMany(c => c).Select(p => p.X).ToArray();
        var canvas = new RgbaCanvas(20, 20, 1) { TextHinting = TextHinting.Full };
        GlyphGridFit.Create(canvas, 10, 9)!.Apply(contours, 5, 7);
        Assert.Equal(original, contours.SelectMany(c => c).Select(p => p.X));
        var large = new List<List<ChartPoint>> { Enumerable.Range(0, 5000).Select(i => new ChartPoint(2.1 + i % 2 * 0.8, i / 500.0)).ToList() };
        var xs = large[0].Select(p => p.X).ToArray();
        GlyphGridFit.Create(canvas, 10, 9)!.Apply(large, 5, 7);
        Assert.Equal(xs, large[0].Select(p => p.X));
    }

    [Fact]
    public void CompositionPropagatesFullModeAndClonesIt() {
        var style = new TextStyle { FontSize = 10, Hinting = TextHinting.Full };
        Assert.Equal(TextHinting.Full, style.Clone().Hinting);
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".ttf");
        using var source = typeof(FullTextHintingTests).Assembly.GetManifestResourceStream("ChartForgeX.Tests.Fixtures.OpenType.stem-true-type.ttf")!;
        using (var output = File.Create(path)) source.CopyTo(output);
        try {
            style.Font = FontSpec.FromFile(path);
            var full = ImageComposition.CreateTransparent(80, 24).DrawText(2.24, 2.4, 70, "I I", style).ToImage().Pixels;
            style.Hinting = TextHinting.Auto;
            var auto = ImageComposition.CreateTransparent(80, 24).DrawText(2.24, 2.4, 70, "I I", style).ToImage().Pixels;
            Assert.Equal(255, Strongest(full)); Assert.True(Strongest(auto) < 255);
        } finally { File.Delete(path); }
    }

    [Theory]
    [InlineData("ital", 1)]
    [InlineData("slnt", -12)]
    public void SelectedSlantInstancesRetainVerticalFitting(string axis, double value) {
        var font = Face("stem-variable.ttf").WithVariations(FontVariationSettings.Default.WithAxis(axis, value));
        Assert.Equal(Draw(font, "I I", 10, TextHinting.Auto), Draw(font, "I I", 10, TextHinting.Full));
    }

    [Theory]
    [InlineData("L")]
    [InlineData("R")]
    public void FittedOverhangRetainsTheOuterFlourish(string glyph) {
        var font = Face("stem-true-type.ttf");
        var canvas = new RgbaCanvas(80, 80, 4, font, 1, false) { TextHinting = TextHinting.Full };
        canvas.DrawTextFitted(30, 20, glyph, ChartColor.Black, 10, 5);
        // Both the left bearing and right overhang extend past the advance rectangle.
        // The outer subpixel flourish remains visible after the stem moves toward it.
        var pixels = canvas.ToOutputPixels();
        Assert.True(pixels[(25 * 80 + (glyph == "L" ? 27 : 37)) * 4 + 3] > 0);
    }

    [Fact]
    public void RotatedFarBearingRetainsTheOuterFlourish() {
        var font = Face("stem-true-type.ttf");
        var canvas = new RgbaCanvas(80, 80, 4, font, 1, false) { TextHinting = TextHinting.Full };
        canvas.DrawTextRotated(45, 30, "C", ChartColor.Black, 10, 90, 0, 0);
        Assert.True(canvas.ToOutputPixels()[(21 * 80 + 39) * 4 + 3] > 0);
    }

    [Fact]
    public void FullBuffersUseTheFinalOutputPixelGrid() {
        var font = Face("stem-true-type.ttf");
        var fitted = new RgbaCanvas(80, 80, 4, font, 2, false) { TextHinting = TextHinting.Full };
        fitted.DrawTextFitted(30, 20, "I I", ChartColor.Black, 5, 7);
        Assert.True(Strongest(fitted.ToOutputPixels()) >= 150);
        var rotated = new RgbaCanvas(80, 80, 4, font, 2, false) { TextHinting = TextHinting.Full };
        rotated.DrawTextRotated(45, 30, "I I", ChartColor.Black, 5, 90, 0, 0);
        Assert.True(Alpha(rotated.ToOutputPixels()) >= 1300);
    }

    private static List<ChartPoint> Ring(double left, double right, double top, double bottom) => new() { new(left, top), new(right, top), new(right, bottom), new(left, bottom) };
    private static byte[] Draw(TrueTypeFont font, string text, double size, TextHinting hinting, bool italic = false, int density = 1) {
        var canvas = new RgbaCanvas(80, 24, 2, font, density, useDefaultOutlineFont: false) { TextHinting = hinting };
        font.Draw(canvas, 2.24, 2.4, text, ChartColor.Black, size, italic);
        return canvas.ToOutputPixels();
    }
    private static int Strongest(byte[] pixels) => Enumerable.Range(0, pixels.Length / 4).Max(i => (int)pixels[i * 4 + 3]);
    private static long Alpha(byte[] pixels) => Enumerable.Range(0, pixels.Length / 4).Sum(i => (long)pixels[i * 4 + 3]);
}
