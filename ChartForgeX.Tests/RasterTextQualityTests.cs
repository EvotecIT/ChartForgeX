using ChartForgeX.Composition;
using ChartForgeX.Raster;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>
/// Small text legibility, overlapping glyph contours, and family and weight resolution for text
/// drawn from a <see cref="FontSpec"/>. Cases that need a particular installed font return early
/// on hosts without it.
/// </summary>
public sealed class RasterTextQualityTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SynthesizedBoldKeepsTheRequestedOpacity(bool bitmap) {
        TrueTypeFont? font = bitmap ? null : TrueTypeFont.TryLoadDefault();
        if (!bitmap && font == null) return;
        var canvas = new RgbaCanvas(120, 80, 1, font, 1, useDefaultOutlineFont: false);
        canvas.DrawTextEmphasized(10, 10, "A", ChartColor.FromRgba(20, 40, 60, 128), 32, font);
        byte[] pixels = canvas.Pixels;
        Assert.Contains(Enumerable.Range(0, pixels.Length / 4).Select(i => pixels[i * 4 + 3]), alpha => alpha > 0);
        Assert.All(Enumerable.Range(0, pixels.Length / 4), i => Assert.InRange(pixels[i * 4 + 3], (byte)0, (byte)128));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public void ThinHorizontalFillRetainsAreaAtEveryPixelPhase(int scale) {
        foreach (double phase in new[] { .05, .17, .32, .48, .65, .82 }) {
            var canvas = new RgbaCanvas(20, 20, scale);
            canvas.FillPolygon(new[] { new ChartPoint(5, 5 + phase), new ChartPoint(15, 5 + phase), new ChartPoint(15, 5.1 + phase), new ChartPoint(5, 5.1 + phase) }, ChartColors.White);
            double total = Enumerable.Range(0, canvas.Pixels.Length / 4).Sum(i => (double)canvas.Pixels[i * 4 + 3]);
            Assert.InRange(total / (scale * scale), 250, 260);
        }
    }

    [Fact]
    public void SmallRegularTextKeepsThinHorizontalStems() {
        if (TrueTypeFont.TryLoadDefault() == null) return;
        // An unhinted crossbar is thinner than a pixel at these sizes. Wherever it falls between
        // pixel rows it must still leave ink across the bowl of the e.
        for (var size = 12.0; size <= 18; size += 0.5) {
            var least = long.MaxValue;
            var most = 0L;
            for (var offset = 0.0; offset < 1; offset += 0.125) {
                var image = ImageComposition.CreateTransparent(40, 40).DrawText(8, 8 + offset, 30, "e", size, ChartColors.White).ToImage();
                Assert.True(HasCrossbar(image), FormattableString.Invariant($"The crossbar of 'e' vanished at {size} px, vertical offset {offset}."));
                var ink = Ink(image.Pixels);
                least = Math.Min(least, ink);
                most = Math.Max(most, ink);
            }

            // Coverage is an area, so the glyph weighs the same wherever it sits between pixel rows.
            // Sampling one scanline per row instead makes thin horizontals come and go with the offset.
            Assert.True(most <= least * 1.04, FormattableString.Invariant($"An 'e' at {size} px should keep its ink at every vertical offset, but it ranged from {least} to {most}."));
        }
    }

    [Fact]
    public void UnscaledFillsWeighHorizontalEdgesByCoverage() {
        var canvas = new RgbaCanvas(8, 8, 1, null, 1, useDefaultOutlineFont: false);
        // A bar 0.25 px tall that misses every pixel center still covers a quarter of its row.
        canvas.FillContours(new[] { new List<ChartPoint> { new(1, 3.6), new(7, 3.6), new(7, 3.85), new(1, 3.85) } }, ChartColors.White, RasterFillRule.NonZero);
        Assert.InRange(canvas.Pixels[(3 * 8 + 4) * 4 + 3], 60, 68);
        Assert.Equal(0, canvas.Pixels[(2 * 8 + 4) * 4 + 3]);
        Assert.Equal(0, canvas.Pixels[(4 * 8 + 4) * 4 + 3]);
    }

    [Fact]
    public void OverlappingContoursFillAsOneShape() {
        // Variable fonts build a glyph from overlapping contours wound the same way.
        var canvas = new RgbaCanvas(12, 12, 1, null, 1, useDefaultOutlineFont: false);
        var stem = new List<ChartPoint> { new(5, 1), new(7, 1), new(7, 11), new(5, 11) };
        var bar = new List<ChartPoint> { new(2, 4), new(10, 4), new(10, 6), new(2, 6) };
        canvas.FillContours(new[] { stem, bar }, ChartColors.White, RasterFillRule.NonZero);
        Assert.Equal(255, canvas.Pixels[(5 * 12 + 6) * 4 + 3]);
    }

    [Fact]
    public void VariableFontGlyphsKeepSolidStems() {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts", "CascadiaMono.ttf");
        if (!File.Exists(path)) return;
        var style = TextStyle.Create(64, ChartColors.White);
        style.Font = FontSpec.FromFile(path);
        var image = ImageComposition.CreateTransparent(80, 100).DrawText(10, 8, 60, "t", style, TextWrapMode.NoWrap).ToImage();

        // Walk down the column with the most ink (the stem): once it starts it must not break.
        var bestColumn = 0;
        var bestInk = 0;
        for (var x = 0; x < image.Width; x++) {
            var ink = 0;
            for (var y = 0; y < image.Height; y++) ink += Alpha(image, x, y);
            if (ink > bestInk) { bestInk = ink; bestColumn = x; }
        }

        var runs = 0;
        var inside = false;
        for (var y = 0; y < image.Height; y++) {
            var solid = Alpha(image, bestColumn, y) > 128;
            if (solid && !inside) runs++;
            inside = solid;
        }

        Assert.Equal(1, runs);
    }

    [Fact]
    public void InstalledFamilyAndWeightSelectDifferentFaces() {
        var regularFace = InstalledFontCatalog.Find("Segoe UI", 400, false);
        var boldFace = InstalledFontCatalog.Find("Segoe UI", 700, false);
        if (regularFace == null || boldFace == null || regularFace.Weight != 400 || boldFace.Weight != 700) return;
        Assert.NotEqual(regularFace.Path, boldFace.Path);

        var regular = Draw("Segoe UI", 400);
        var bold = Draw("Segoe UI", 700);
        var missing = Draw("No Such Family 4E1D", 400);
        var fallback = Draw("sans-serif", 400);
        Assert.NotEqual(regular, bold);
        Assert.NotEqual(regular, missing);
        Assert.Equal(fallback, missing);
        Assert.True(Ink(bold) > Ink(regular) * 1.15, "A bold face should lay down clearly more ink than the regular one.");
        // A stack falls through families that are not installed.
        Assert.Equal(regular, Draw("No Such Family 4E1D, 'Segoe UI', sans-serif", 400));

        var semibold = InstalledFontCatalog.Find("Segoe UI", 600, false);
        if (semibold != null && semibold.Weight == 600) Assert.NotEqual(Draw("Segoe UI", 600), bold);
    }

    [Fact]
    public void GenericFallbackUsesAnInstalledBoldSibling() {
        var regular = TypographyFontResolver.ResolveFace(FontSpec.SystemSans());
        var boldSpec = FontSpec.SystemSans();
        boldSpec.Weight = 700;
        var bold = TypographyFontResolver.ResolveFace(boldSpec);
        Assert.False(regular.SynthesizeBold);
        if (regular.Font == null) {
            // No fonts at all: the built-in bitmap font draws, with emphasis synthesized.
            Assert.Null(bold.Font);
            Assert.True(bold.SynthesizeBold);
            return;
        }

        Assert.NotNull(bold.Font);
        // Either a real bold face was found, or the regular one is emboldened; never neither.
        Assert.True(bold.SynthesizeBold || !ReferenceEquals(bold.Font, regular.Font));
        var measured = TextLayoutEngine.Measure("Hamburgefonstiv", new TextStyle { Font = boldSpec, FontSize = 24 }).Width;
        Assert.True(measured > TextLayoutEngine.Measure("Hamburgefonstiv", new TextStyle { FontSize = 24 }).Width, "Bold text should measure wider than regular text.");
    }

    [Fact]
    public void ExplicitFontFileStillWinsOverTheFamily() {
        var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var path = Path.Combine(windows, "Fonts", "georgia.ttf");
        if (!File.Exists(path)) return;
        var spec = FontSpec.FromFile(path);
        spec.Family = "Segoe UI";
        var face = TypographyFontResolver.ResolveFace(spec);
        Assert.Same(TrueTypeFont.TryLoadFromPath(path), face.Font);
        Assert.False(face.SynthesizeBold);
    }

    [Fact]
    public void FontCatalogMatchesWeightsLikeCss() {
        var families = new Dictionary<string, List<InstalledFontFace>>(StringComparer.OrdinalIgnoreCase) {
            ["Example"] = new() {
                Face("light.ttf", 300), Face("regular.ttf", 400), Face("italic.ttf", 400, italic: true),
                Face("semibold.ttf", 600), Face("bold.ttf", 700), Face("condensed-bold.ttf", 700, width: 3)
            }
        };
        Assert.Equal("regular.ttf", InstalledFontCatalog.Find(families, "example", 400, false)!.Path);
        Assert.Equal("regular.ttf", InstalledFontCatalog.Find(families, "Example", 500, false)!.Path);
        Assert.Equal("semibold.ttf", InstalledFontCatalog.Find(families, "Example", 600, false)!.Path);
        Assert.Equal("bold.ttf", InstalledFontCatalog.Find(families, "Example", 700, false)!.Path);
        Assert.Equal("bold.ttf", InstalledFontCatalog.Find(families, "Example", 900, false)!.Path);
        Assert.Equal("light.ttf", InstalledFontCatalog.Find(families, "Example", 100, false)!.Path);
        Assert.Equal("italic.ttf", InstalledFontCatalog.Find(families, "Example", 400, true)!.Path);
        // Slant is matched before weight, as in CSS: with no bold italic the italic face is emboldened.
        Assert.Equal("italic.ttf", InstalledFontCatalog.Find(families, "Example", 700, true)!.Path);
        Assert.Null(InstalledFontCatalog.Find(families, "Other", 400, false));
        Assert.Null(InstalledFontCatalog.Find(families, " ", 400, false));
    }

    [Fact]
    public void HostWithoutFontsDrawsWithTheBuiltInFontInsteadOfThrowing() {
        // What a bare container looks like to the catalog: no font folders, or unreadable ones.
        var scratch = Path.Combine(Path.GetTempPath(), "cfx-no-fonts-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(scratch);
        try {
            File.WriteAllText(Path.Combine(scratch, "broken.ttf"), "not a font");
            File.WriteAllBytes(Path.Combine(scratch, "empty.ttc"), Array.Empty<byte>());
            var families = InstalledFontCatalog.Index(new[] { scratch, Path.Combine(scratch, "missing") });
            Assert.Empty(families);
            Assert.Null(InstalledFontCatalog.Find(families, "Arial", 400, false));
            Assert.Null(TrueTypeFont.TryLoadFromPath(Path.Combine(scratch, "broken.ttf")));
        } finally {
            Directory.Delete(scratch, true);
        }

        // With no outline face the canvas falls back to its built-in bitmap glyphs.
        var canvas = new RgbaCanvas(80, 24, 1, null, 1, useDefaultOutlineFont: false);
        canvas.DrawText(2, 2, "No fonts", ChartColors.White, 14, null, italic: false);
        canvas.DrawTextEmphasized(2, 2, "No fonts", ChartColors.White, 14, null, italic: true);
        Assert.Contains(canvas.Pixels.Where((_, index) => index % 4 == 3), alpha => alpha > 0);
        Assert.True(RgbaCanvas.MeasureTextWidthWithFont("No fonts", 14, null) > 0);
    }

    private static InstalledFontFace Face(string path, int weight, bool italic = false, int width = 5) =>
        new(path, null, "Example", null, weight, width, italic);

    private static byte[] Draw(string family, int weight) {
        var style = TextStyle.Create(28, ChartColors.White);
        style.Font = FontSpec.FromFamily(family);
        style.Font.Weight = weight;
        return ImageComposition.CreateTransparent(320, 48).DrawText(4, 4, 312, "Hamburgefonstiv 0123", style, TextWrapMode.NoWrap).ToImage().Pixels;
    }

    private static long Ink(byte[] pixels) {
        long ink = 0;
        for (var index = 3; index < pixels.Length; index += 4) ink += pixels[index];
        return ink;
    }

    /// <summary>True when some row in the middle of the glyph has ink across most of the glyph's width.</summary>
    private static bool HasCrossbar(RgbaImage image) {
        int left = image.Width, right = -1, top = image.Height, bottom = -1;
        for (var y = 0; y < image.Height; y++) for (var x = 0; x < image.Width; x++) {
            if (Alpha(image, x, y) < 48) continue;
            left = Math.Min(left, x);
            right = Math.Max(right, x);
            top = Math.Min(top, y);
            bottom = Math.Max(bottom, y);
        }

        if (right < left) return false;
        var height = bottom - top + 1;
        for (var y = top + height / 4; y <= bottom - height / 4; y++) {
            var inked = 0;
            for (var x = left + 1; x < right; x++) {
                // The bar may straddle two rows, each carrying part of its coverage.
                if (Alpha(image, x, y) + Alpha(image, x, y + 1) >= 96) inked++;
            }

            if (inked >= right - left - 1) return true;
        }

        return false;
    }

    private static int Alpha(RgbaImage image, int x, int y) =>
        x < 0 || y < 0 || x >= image.Width || y >= image.Height ? 0 : image.Pixels[(y * image.Width + x) * 4 + 3];
}
