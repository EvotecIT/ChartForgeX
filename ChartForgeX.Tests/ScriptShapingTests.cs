using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.SvgRaster;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Portable, original-font regressions for syllable order and font-selected script forms.</summary>
[Collection(nameof(FontRegistryCollection))]
public sealed class ScriptShapingTests {
    private static byte[] Bytes(string name) {
        using var stream = typeof(ScriptShapingTests).Assembly.GetManifestResourceStream("ChartForgeX.Tests.Fixtures.OpenType." + name + ".ttf")!;
        using var output = new MemoryStream(); stream.CopyTo(output); return output.ToArray();
    }
    private static TrueTypeFont Font(string name) => TrueTypeFont.TryLoad(Bytes(name))!;
    private static ushort[] Glyphs(TrueTypeFont face, string text) => TextShaper.Shape(face, text).Select(g => g.Glyph).ToArray();

    [Theory]
    [InlineData("कि", new ushort[] { 5, 1 })]
    [InlineData("কি", new ushort[] { 5, 1 })]
    [InlineData("ਕਿ", new ushort[] { 5, 1 })]
    [InlineData("કિ", new ushort[] { 5, 1 })]
    [InlineData("କେ", new ushort[] { 7, 1 })]
    [InlineData("கெ", new ushort[] { 7, 1 })]
    [InlineData("కె", new ushort[] { 1, 7 })]
    [InlineData("ಕೆ", new ushort[] { 1, 7 })]
    [InlineData("കെ", new ushort[] { 7, 1 })]
    public void DependentVowelsUseEachSupportedScriptsPosition(string text, ushort[] expected) {
        var face = Font("indic-modern");
        Assert.Equal(expected, Glyphs(face, text)); Assert.Equal(100, face.Measure(text, 100), 6);
    }
    [Theory]
    [InlineData("indic-modern")]
    [InlineData("indic-legacy")]
    [InlineData("indic-mixed-tags")]
    public void FontGenerationSelectsItsHalantOrderAndCompletesTheRakaarBeforePresentation(string name) {
        var face = Font(name);
        Assert.Equal(new ushort[] { 15, 13 }, Glyphs(face, "क्रि"));
        Assert.Equal(105, face.Measure("क्रि", 100), 6);
        var glyphs = TextShaper.Shape(face, "र्कि");
        Assert.Equal(new ushort[] { 5, 1, 10 }, glyphs.Select(g => g.Glyph));
        Assert.Equal(-300, glyphs[2].OffsetX); Assert.Equal(700, glyphs[2].OffsetY);
    }
    [Theory]
    [InlineData("क्त", new ushort[] { 14 })]
    [InlineData("क्\u200dत", new ushort[] { 11, 2 })]
    [InlineData("क्\u200cत", new ushort[] { 1, 4, 2 })]
    [InlineData("र्\u200dक", new ushort[] { 26, 1 })]
    public void JoinersRequestHalfOrExplicitHalantWhileTheFontOwnsEyelashForms(string text, ushort[] expected) =>
        Assert.Equal(expected, Glyphs(Font("indic-modern"), text));
    [Fact]
    public void AnExplicitHalantKeepsThePreBaseVowelNextToItsMainConsonant() =>
        Assert.Equal(new ushort[] { 2, 4, 5, 1 }, Glyphs(Font("indic-modern"), "त्कि"));
    [Theory]
    [InlineData("indic-modern")]
    [InlineData("indic-legacy")]
    public void RephNeedsAnotherBaseRatherThanOnlyAnAccent(string name) =>
        Assert.Equal(new ushort[] { 3, 4, 20 }, Glyphs(Font(name), "र्॑"));
    [Theory]
    [InlineData("indic-modern")]
    [InlineData("indic-legacy")]
    public void TeluguDoesNotConvertAnUnrequestedInitialRaIntoABelowBaseForm(string name) =>
        Assert.Equal(new ushort[] { 3, 4, 1 }, Glyphs(Font(name), "ర్క"));
    [Fact]
    public void ModernBelowFormsCanPrecedeTheMainConsonant() {
        var face = Font("indic-modern");
        Assert.Equal(new ushort[] { 13, 4, 2 }, Glyphs(face, "क्र्त"));
        Assert.Equal(105, face.Measure("क्र्त", 100), 6);
    }
    [Theory]
    [InlineData("indic-reph-modern")]
    [InlineData("indic-reph-legacy")]
    public void RephUsesTheScriptsImplicitExplicitOrLogicalForm(string name) {
        var face = Font(name);
        Assert.Equal(new ushort[] { 1, 10 }, Glyphs(face, "र्क"));
        Assert.Equal(new ushort[] { 1, 10 }, Glyphs(face, "ర్\u200dక"));
        Assert.Equal(new ushort[] { 3, 4, 1 }, Glyphs(face, "ర్క"));
        Assert.Equal(new ushort[] { 3, 4, 1 }, Glyphs(face, "ర్\u200cక"));
        Assert.Equal(new ushort[] { 3, 4, 1 }, Glyphs(face, "ര്ക"));
        Assert.Equal(new ushort[] { 1, 10 }, Glyphs(face, "ൎക"));
        var attached = TextShaper.Shape(face, "ర్\u200dక")[1];
        Assert.Equal(-300, attached.OffsetX); Assert.Equal(700, attached.OffsetY);
    }
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void UnmatchedSurrogatesRetainMissingGlyphsThroughMeasurementAndDrawing(int input) {
        var text = input == 0 ? "कि" + (char)0xd800 : input == 1 ? (char)0xdc00 + "कि" : (char)0xd800 + "ि";
        var face = Font("indic-modern"); var glyphs = TextShaper.Shape(face, text);
        Assert.Contains(glyphs, glyph => glyph.Glyph == 0);
        Assert.Equal(glyphs.Sum(glyph => glyph.Advance ?? glyph.Face.AdvanceWidth(glyph.Glyph)) / 10, face.Measure(text, 100), 6);
        Assert.Contains(Paint(face, text).Where((value, index) => index % 4 == 3), alpha => alpha > 0);
        Assert.Same(glyphs, TextShaper.Shape(face, text));
    }
    [Theory]
    [InlineData(".ि", new ushort[] { 6, 5, 19 })]
    [InlineData(" ि", new ushort[] { 6, 5, 19 })]
    [InlineData("\u00a0ि", new ushort[] { 5, 6 })]
    [InlineData("\u200dि", new ushort[] { 5, 19 })]
    public void CommonBaseCharactersRetainTheScriptOfTheirAttachedVowel(string text, ushort[] expected) =>
        Assert.Equal(expected, Glyphs(Font("indic-modern"), text));
    [Theory]
    [InlineData("क")]
    [InlineData("ककक")]
    public void GlyphGrowthIsBoundedAcrossTheCompleteFeatureAndSyllableRun(string text) {
        var face = Font("indic-growth"); var glyphs = TextShaper.Shape(face, text);
        Assert.InRange(glyphs.Count, 1, text.Length * 8 + 64);
        Assert.Equal(glyphs.Count * 50, face.Measure(text, 100), 6);
    }
    [Fact]
    public void CanonicallyEquivalentNuktaAndSplitMatraSequencesMeasureAndDrawIdentically() {
        var face = Font("indic-modern");
        foreach (var pair in new[] { ("क़", "क़"), ("কো", "কো"), ("கொ", "கொ") }) {
            Assert.Equal(Glyphs(face, pair.Item1), Glyphs(face, pair.Item2));
            Assert.Equal(face.Measure(pair.Item1, 100), face.Measure(pair.Item2, 100));
            Assert.Equal(Paint(face, pair.Item1), Paint(face, pair.Item2));
        }
    }
    [Theory]
    [InlineData("ก่ำ", new ushort[] { 1, 21, 20, 6 })]
    [InlineData("ກ່ຳ", new ushort[] { 1, 21, 20, 6 })]
    public void AmVowelsDecomposeAndMoveTheRingBeforeTheToneForMarkToMarkAttachment(string text, ushort[] expected) {
        var face = Font("thai-script"); var glyphs = TextShaper.Shape(face, text);
        Assert.Equal(expected, glyphs.Select(g => g.Glyph));
        Assert.Equal(-250, glyphs[1].OffsetX); Assert.Equal(700, glyphs[1].OffsetY);
        Assert.Equal(-250, glyphs[2].OffsetX); Assert.Equal(900, glyphs[2].OffsetY);
        Assert.Equal(100, face.Measure(text, 100), 6);
    }
    [Theory]
    [InlineData("ក្រ", new ushort[] { 22, 1 })]
    [InlineData("កើ", new ushort[] { 7, 1, 24 })]
    [InlineData("កោ", new ushort[] { 7, 1, 25 })]
    [InlineData("ក្ត", new ushort[] { 1, 23 })]
    [InlineData("ក៊ី", new ushort[] { 1, 21, 24 })]
    [InlineData("ក៊\u200cី", new ushort[] { 1, 9, 24 })]
    public void KhmerCoengSplitVowelsAndEscapedShiftersUseTheirAssignedFeatureStages(string text, ushort[] expected) =>
        Assert.Equal(expected, Glyphs(Font("khmer-script"), text));

    [Fact]
    public void FontFallbackCoversTheWholeViramaConnectedSyllable() {
        var path = Path.Combine(Path.GetTempPath(), "CFX-script-fallback-" + Guid.NewGuid().ToString("N") + ".ttf");
        try {
            File.WriteAllBytes(path, Bytes("indic-modern")); FontRegistry.Register("CFX Script Complete", path);
            var primary = Font("indic-primary");
            var glyph = Assert.Single(TextShaper.Shape(primary, "क्त"));
            Assert.Equal(14, glyph.Glyph); Assert.NotSame(primary, glyph.Face);
            Assert.Equal(60, primary.Measure("क्त", 100), 6);
        } finally { FontRegistry.Clear(); File.Delete(path); }
    }
    [Fact]
    public void PaintBoundariesDoNotFormAConjunctAcrossIndependentTextOwners() {
        var face = Font("indic-modern");
        var glyphs = TextShaper.ShapeStyled("क्त", new[] { face, face, face }, new[] { 0, 0, 1 });
        Assert.Equal(new ushort[] { 1, 4, 2 }, glyphs.Select(g => g.Glyph));
        Assert.Equal(new[] { 0, 0, 2 }, glyphs.Select(g => g.SourceIndex));
    }
    [Fact]
    public void ShapedSyllablesRetainLogicalSourceClustersAndRepeatedCacheResults() {
        var face = Font("indic-modern"); var shaped = TextShaper.Shape(face, "क्त कि");
        Assert.Equal(new[] { 0, 3, 4, 4 }, shaped.Select(g => g.SourceIndex));
        Assert.Same(shaped, TextShaper.Shape(face, "क्त कि"));
    }
    [Theory]
    [InlineData("indic-modern", "र्कि")]
    [InlineData("khmer-script", "កើ")]
    [InlineData("thai-script", "ก่ำ")]
    [InlineData("sinhala-script", "ර්\u200dකේ")]
    [InlineData("myanmar-script", "င်္ကြေ")]
    public void SvgRasterExportUsesTheSameSyllableAndAttachmentPositionsAsDirectDrawing(string name, string text) {
        var path = Path.Combine(Path.GetTempPath(), "CFX-script-svg-" + Guid.NewGuid().ToString("N") + ".ttf");
        try {
            File.WriteAllBytes(path, Bytes(name)); FontRegistry.Register("CFX Script Export", path);
            var font = TypographyFontResolver.ResolveFace("CFX Script Export", 400, false).Font!;
            var actual = SvgRasterizer.ToImage("<svg xmlns='http://www.w3.org/2000/svg' width='250' height='180'><text x='10' y='140' font-family='CFX Script Export' font-size='100'>" + text + "</text></svg>").Pixels;
            var direct = new RgbaCanvas(250, 180, 4, font, 1, useDefaultOutlineFont: false);
            font.Draw(direct, 10, 60, text, ChartColor.Black, 100);
            Assert.All(direct.ToOutputPixels().Zip(actual), pair => Assert.InRange(Math.Abs(pair.First - pair.Second), 0, 1));
        } finally { FontRegistry.Clear(); File.Delete(path); }
    }
    private static byte[] Paint(TrueTypeFont face, string text) {
        var canvas = new RgbaCanvas(250, 180, 2, face, 1, useDefaultOutlineFont: false);
        face.Draw(canvas, 10, 30, text, ChartColor.Black, 100);
        return canvas.ToOutputPixels();
    }
}
