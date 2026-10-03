using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Portable script contracts use original fonts; positioned tuples are reference-verified.</summary>
[Collection(nameof(FontRegistryCollection))]
public sealed class SinhalaMyanmarShapingTests {
    private static byte[] Bytes(string name) {
        using var stream = typeof(SinhalaMyanmarShapingTests).Assembly.GetManifestResourceStream("ChartForgeX.Tests.Fixtures.OpenType." + name + ".ttf")!;
        using var output = new MemoryStream(); stream.CopyTo(output); return output.ToArray();
    }
    private static TrueTypeFont Font(string name) => TrueTypeFont.TryLoad(Bytes(name))!;

    [Theory]
    [InlineData("කෙ", new ushort[] { 7, 1 }, 100)]
    [InlineData("කේ", new ushort[] { 7, 21 }, 100)]
    [InlineData("කෝ", new ushort[] { 7, 1, 8, 5 }, 150)]
    [InlineData("කෞ", new ushort[] { 7, 1, 23 }, 150)]
    [InlineData("ර්\u200dක", new ushort[] { 1, 14 }, 50)]
    [InlineData("ර්\u200dකේ", new ushort[] { 7, 1, 14, 5 }, 100)]
    [InlineData("ක්\u200dරේ", new ushort[] { 7, 1, 15, 5 }, 100)]
    [InlineData("ක\u200d්කේ", new ushort[] { 1, 5, 7, 21 }, 150)]
    [InlineData("ක්\u200cර", new ushort[] { 21, 3 }, 100)]
    [InlineData("ෙ", new ushort[] { 7, 11 }, 100)]
    public void SinhalaVowelsAndExplicitJoiningKeepTheFontsFormsAndAdvances(string text, ushort[] expected, double width) {
        var face = Font("sinhala-script"); var glyphs = TextShaper.Shape(face, text);
        Assert.Equal(expected, glyphs.Select(g => g.Glyph));
        Assert.Equal(width, face.Measure(text, 100), 6);
        Assert.Equal(width, glyphs.Sum(g => g.Advance!.Value) / 10, 6);
    }

    [Theory]
    [InlineData("င်္က", new ushort[] { 1, 14 }, 50)]
    [InlineData("ရ်္က", new ushort[] { 1, 14 }, 50)]
    [InlineData("\u105a်္က", new ushort[] { 1, 14 }, 50)]
    [InlineData("က္က", new ushort[] { 1, 16 }, 50)]
    [InlineData("ကြေ", new ushort[] { 7, 20, 1 }, 124)]
    [InlineData("ကုံ", new ushort[] { 1, 19, 18 }, 50)]
    [InlineData("င်္ကေ", new ushort[] { 7, 1, 14 }, 100)]
    [InlineData("ေ", new ushort[] { 7, 11 }, 100)]
    [InlineData("ကကေ", new ushort[] { 1, 7, 1 }, 150)]
    public void MyanmarKinziStacksAndVowelsShareOnePositionedRun(string text, ushort[] expected, double width) {
        var face = Font("myanmar-script"); var glyphs = TextShaper.Shape(face, text);
        Assert.Equal(expected, glyphs.Select(g => g.Glyph));
        Assert.Equal(width, face.Measure(text, 100), 6);
        Assert.Equal(width, glyphs.Sum(g => g.Advance!.Value) / 10, 6);
    }

    [Fact]
    public void ScriptMarksZeroTheirFontWidthBeforeDistanceAndAnchorPositioning() {
        var myanmar = TextShaper.Shape(Font("myanmar-script"), "ကြေ");
        Assert.Equal(240, myanmar[1].Advance); // The required dist feature restores this width once.
        var sinhala = TextShaper.Shape(Font("sinhala-script"), "ක්\u200dර");
        Assert.Equal(0, sinhala[1].Advance); // The font's 240-unit mark width cannot widen the syllable.
        Assert.Equal(-250, sinhala[1].OffsetX); Assert.Equal(-100, sinhala[1].OffsetY);
        var kinzi = TextShaper.Shape(Font("myanmar-script"), "င်္က");
        Assert.Equal(0, kinzi[1].Advance); Assert.Equal(-250, kinzi[1].OffsetX); Assert.Equal(700, kinzi[1].OffsetY);
    }

    [Theory]
    [InlineData("කේ")]
    [InlineData("කෝ")]
    [InlineData("ක්\u200dරේ")]
    [InlineData("ර්\u200dකෝ")]
    public void SinhalaCanonicalVowelsRetainJoinerLocationAndIdenticalInk(string text) {
        var face = Font("sinhala-script"); var decomposed = text.Normalize(System.Text.NormalizationForm.FormD);
        var a = TextShaper.Shape(face, text); var b = TextShaper.Shape(face, decomposed);
        Assert.Equal(a.Select(g => (g.Glyph, g.Advance, g.OffsetX, g.OffsetY)), b.Select(g => (g.Glyph, g.Advance, g.OffsetX, g.OffsetY)));
        Assert.Equal(Paint(face, text), Paint(face, decomposed));
    }

    [Fact]
    public void FallbackOwnsTheCompleteKinziConnectedSyllable() {
        var path = Path.Combine(Path.GetTempPath(), "CFX-Myanmar-" + Guid.NewGuid().ToString("N") + ".ttf");
        try {
            File.WriteAllBytes(path, Bytes("myanmar-script")); FontRegistry.Register("CFX Myanmar Complete", path);
            var primary = Font("myanmar-primary"); var glyphs = TextShaper.Shape(primary, "င်္က");
            Assert.Equal(new ushort[] { 1, 14 }, glyphs.Select(g => g.Glyph));
            Assert.All(glyphs, g => Assert.NotSame(primary, g.Face));
            Assert.Same(glyphs[0].Face, glyphs[1].Face);
            Assert.Equal(50, primary.Measure("င်္က", 100), 6);
        } finally { FontRegistry.Clear(); File.Delete(path); }
    }

    [Fact]
    public void PaintOwnersDoNotCombineKinziAcrossIndependentRuns() {
        var face = Font("myanmar-script");
        var glyphs = TextShaper.ShapeStyled("င်္က", new[] { face, face, face, face }, new[] { 0, 0, 0, 1 });
        Assert.Equal(new ushort[] { 11, 14, 1 }, glyphs.Select(g => g.Glyph));
        Assert.Equal(new[] { 0, 0, 3 }, glyphs.Select(g => g.SourceIndex));
    }

    [Theory]
    [InlineData("myanmar-no-circle", "ေ", new ushort[] { 7 })]
    [InlineData("myanmar-preprocessed", "င်္ကေ", new ushort[] { 7, 1, 14 })]
    public void OptionalPlaceholdersAndPreprocessingDoNotDuplicateOrLoseGlyphs(string name, string text, ushort[] expected) {
        var glyphs = TextShaper.Shape(Font(name), text);
        Assert.Equal(expected, glyphs.Select(g => g.Glyph));
    }

    [Theory]
    [InlineData("ಕೋ", new ushort[] { 23 })]
    [InlineData("ಕ\u200dೋ", new ushort[] { 1, 7, 8, 10 })]
    [InlineData("ಕೆ\u200cೂೕ", new ushort[] { 1, 7, 8, 10 })]
    [InlineData("కై", new ushort[] { 22 })]
    [InlineData("క\u200dై", new ushort[] { 2, 18, 19 })]
    [InlineData("కె\u200cౖ", new ushort[] { 2, 18, 19 })]
    public void CanonicalIndicVowelsDoNotMoveAnExplicitJoinerToTheClusterEnd(string text, ushort[] expected) =>
        Assert.Equal(expected, TextShaper.Shape(Font("indic-joiner-vowels"), text).Select(g => g.Glyph));

    [Fact]
    public void RepeatedSinhalaSyllablesRetainEachUnjoinedBase() {
        var glyphs = TextShaper.Shape(Font("sinhala-script"), string.Concat(Enumerable.Repeat("ක්", 8192)));
        Assert.Equal(8192, glyphs.Count); Assert.All(glyphs, glyph => Assert.Equal(21, glyph.Glyph));
    }

    [Fact]
    public void RepeatedMyanmarAnusvaraRetainsStableOrderAcrossTheWholeBelowVowelBlock() {
        var glyphs = TextShaper.Shape(Font("myanmar-script"), "က" + new string('\u102f', 8192) + new string('\u1036', 8192));
        Assert.Equal(16385, glyphs.Count); Assert.Equal(1, glyphs[0].Glyph);
        Assert.All(glyphs.Skip(1).Take(8192), glyph => Assert.Equal(19, glyph.Glyph));
        Assert.All(glyphs.Skip(8193), glyph => Assert.Equal(18, glyph.Glyph));
        Assert.All(glyphs.Skip(1), glyph => Assert.Equal(-250, glyph.OffsetX));
        Assert.All(glyphs.Skip(1).Take(8192), glyph => Assert.Equal(700, glyph.OffsetY));
        Assert.All(glyphs.Skip(8193), glyph => Assert.Equal(-100, glyph.OffsetY));
    }

    [Theory]
    [InlineData("sinhala-script", "ක\u200d්ක", '\u0dd9', new ushort[] { 1, 5 }, 7, 1)]
    [InlineData("indic-modern", "त्क", '\u093f', new ushort[] { 2, 4 }, 5, 1)]
    public void RepeatedPreVowelsStayAfterTheSurvivingHalant(string name, string prefix, char mark, ushort[] before, ushort vowel, ushort after) {
        var glyphs = TextShaper.Shape(Font(name), prefix + new string(mark, 8192));
        Assert.Equal(before.Concat(Enumerable.Repeat(vowel, 8192)).Append(after), glyphs.Select(g => g.Glyph));
    }

    private static byte[] Paint(TrueTypeFont face, string text) {
        var canvas = new RgbaCanvas(250, 180, 2, face, 1, useDefaultOutlineFont: false);
        face.Draw(canvas, 10, 30, text, ChartColor.Black, 100);
        return canvas.ToOutputPixels();
    }
}
