using ChartForgeX.Composition;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>
/// Per-character fallback: a character the chosen face lacks is drawn by the first face of its
/// fallback chain that covers it, and measuring uses the same glyphs as drawing. The cases that
/// depend on installed fonts return early on hosts without them.
/// </summary>
[Collection(nameof(FontRegistryCollection))]
public sealed class TextFallbackTests : IDisposable {
    private const int Tokyo = 0x6771;

    public void Dispose() => FontRegistry.Clear();

    [Fact]
    public void MissingCharactersComeFromAFaceThatHasThem() {
        var primary = TrueTypeFont.TryLoadDefault();
        if (primary == null || primary.HasGlyph(Tokyo)) return;
        var fallback = FontFallbackChain.For(primary).FaceFor(Tokyo);
        if (fallback == null) return;

        var glyphs = TextShaper.Shape(primary, "Server \u6771\u4EAC");
        Assert.Equal(9, glyphs.Count);
        Assert.All(glyphs.Take(7), glyph => Assert.Same(primary, glyph.Face));
        Assert.True(glyphs[7].Face.HasGlyph(Tokyo));
        Assert.Equal(glyphs[7].Face.MapGlyph(Tokyo), glyphs[7].Glyph);
        Assert.NotEqual(0, glyphs[8].Glyph);
    }

    [Fact]
    public void MeasurementMatchesTheDrawnAdvance() {
        var primary = TrueTypeFont.TryLoadDefault();
        if (primary == null || FontFallbackChain.For(primary).FaceFor(Tokyo) == null) return;
        foreach (var text in new[] { "\u6771\u4EAC \uC11C\uC6B8", "\u05E9\u05DC\u05D5\u05DD 123", "\u0645\u0631\u062D\u0628\u0627", "e\u0301 \u2705\uFE0F \uD83D\uDE80" }) {
            // Drawing the text and then a bar at its measured width matches drawing both as one run.
            var separate = new RgbaCanvas(420, 60, 1, primary, 1, useDefaultOutlineFont: false);
            primary.Draw(separate, 4, 4, text, ChartColor.Black, 24);
            primary.Draw(separate, 4 + primary.Measure(text, 24), 4, "|", ChartColor.Black, 24);
            var joined = new RgbaCanvas(420, 60, 1, primary, 1, useDefaultOutlineFont: false);
            primary.Draw(joined, 4, 4, text + "|", ChartColor.Black, 24);
            Assert.Equal(joined.ToOutputPixels(), separate.ToOutputPixels());
            Assert.Equal(primary.Measure(text + "|", 24), primary.Measure(text, 24) + primary.Measure("|", 24), 6);
        }
    }

    [Fact]
    public void StackFamiliesAreTriedBeforeThePlatformChain() {
        if (InstalledFontCatalog.Find("Segoe UI", 400, false) == null || InstalledFontCatalog.Find("Yu Gothic", 400, false) == null) return;
        var face = TypographyFontResolver.ResolveFace("Segoe UI, Yu Gothic, sans-serif", 400, false).Font!;
        Assert.Equal(new[] { "Yu Gothic" }, face.FallbackFamilies);
        var glyph = TextShaper.Shape(face, "\u6771").Single();
        Assert.Contains("Yu Gothic", glyph.Face.DisplayName, StringComparison.OrdinalIgnoreCase);

        // A stack with no other installed family keeps the shared face.
        Assert.Same(TypographyFontResolver.ResolveFace("Segoe UI", 400, false).Font, TypographyFontResolver.ResolveFace("Segoe UI, sans-serif", 400, false).Font);
    }

    [Fact]
    public void RegisteredFontsAreFallbackFacesBeforeThePlatformChain() {
        var primary = TrueTypeFont.TryLoadDefault();
        if (primary == null || primary.HasGlyph(OpenTypeTestFonts.PrivateUseCharacter) || FontFallbackChain.For(primary).FaceFor(OpenTypeTestFonts.PrivateUseCharacter) != null) return;
        var path = Path.Combine(Path.GetTempPath(), "cfx-fallback-" + Guid.NewGuid().ToString("N") + ".otf");
        File.WriteAllBytes(path, OpenTypeTestFonts.NameKeyed());
        try {
            FontRegistry.Register("CFX Fallback Test", path);
            var text = "A" + char.ConvertFromUtf32(OpenTypeTestFonts.PrivateUseCharacter);
            var glyphs = TextShaper.Shape(primary, text);
            Assert.True(glyphs[1].Face.HasCompactOutlines);
            Assert.Equal(OpenTypeTestFonts.Box, glyphs[1].Glyph);
            Assert.Equal(primary.Measure("A", 50) + 25, primary.Measure(text, 50), 6);

            FontRegistry.Clear();
            Assert.Same(primary, TextShaper.Shape(primary, text)[1].Face);
        } finally {
            FontRegistry.Clear();
            File.Delete(path);
        }
    }

    [Fact]
    public void CombiningMarksComposeWhenTheFaceHasTheComposedLetter() {
        var primary = TrueTypeFont.TryLoadDefault();
        if (primary == null || !primary.HasGlyph(0x00E9)) return;
        Assert.Equal(primary.Measure("\u00E9", 20), primary.Measure("e\u0301", 20), 6);
        var glyph = TextShaper.Shape(primary, "e\u0301").Single();
        Assert.Equal(primary.MapGlyph(0x00E9), glyph.Glyph);
    }

    [Fact]
    public void IgnorableCharactersDrawNothingAndTakeNoRoom() {
        var primary = TrueTypeFont.TryLoadDefault();
        if (primary == null) return;
        Assert.Equal(primary.Measure("ab", 20), primary.Measure("a\u200Bb\u200D", 20), 6);
        Assert.Equal(primary.Measure("ab", 20), primary.Measure("a\u202Eb\u202C", 20), 6);
    }

    [Fact]
    public void WrappingAndTrimmingNeverSplitACharacter() {
        var style = new TextStyle { FontSize = 20 };
        var rockets = string.Concat(Enumerable.Repeat("\uD83D\uDE80", 6));
        var layout = TextLayoutEngine.Layout(rockets, 30, style, TextWrapMode.Character);
        Assert.True(layout.Lines.Count > 1);
        foreach (var line in layout.Lines) Assert.Equal(0, line.Text.Length % 2);
        Assert.Equal(rockets, string.Concat(layout.Lines.Select(line => line.Text)));

        var accented = string.Concat(Enumerable.Repeat("e\u0301", 12));
        var trimmed = TextLayoutEngine.Layout(accented, 40, style, TextWrapMode.Word, 1, TextTrimming.Ellipsis);
        Assert.True(trimmed.Trimmed);
        var kept = trimmed.Lines[0].Text.TrimEnd('\u2026');
        Assert.Equal(0, kept.Length % 2);
        // Room for "..." and one UTF-16 unit: half a rocket is dropped rather than drawn.
        Assert.Equal("...", ChartForgeX.Rendering.ChartTextFitting.TrimEnd("\uD83D\uDE80\uD83D\uDE80\uD83D\uDE80", 20, 45, (value, size) => value.Length * size * 0.5));
    }

    [Fact]
    public void CompositionTextDrawsFallbackCharacters() {
        var primary = TrueTypeFont.TryLoadDefault();
        if (primary == null || FontFallbackChain.For(primary).FaceFor(Tokyo) == null) return;
        var style = new TextStyle { FontSize = 32, Color = ChartColor.Black };
        var image = ImageComposition.Create(160, 60, ChartColor.White).DrawText(4, 4, 150, "\u6771\u4EAC", style, TextWrapMode.NoWrap).ToImage();
        var ink = 0;
        for (var i = 0; i < image.Pixels.Length; i += 4) if (image.Pixels[i] < 128) ink++;
        Assert.True(ink > 150, "CJK text should draw ink through its fallback face.");
    }
}
