using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.SvgRaster;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Observable layout contracts from original font fixtures, independent of installed font availability.</summary>
[Collection(nameof(FontRegistryCollection))]
public sealed class OpenTypeLayoutTests {
    private static byte[] Bytes(string name) {
        using var stream = typeof(OpenTypeLayoutTests).Assembly.GetManifestResourceStream("ChartForgeX.Tests.Fixtures.OpenType." + name + ".ttf")!;
        using var output = new MemoryStream(); stream.CopyTo(output); return output.ToArray();
    }
    private static TrueTypeFont Font(string name) => TrueTypeFont.TryLoad(Bytes(name))!;

    [Fact]
    public void DefaultFeaturesSubstituteBeforeLigatingAndDoNotEnableAlternates() {
        var face = Font("substitution");
        var glyph = Assert.Single(TextShaper.Shape(face, "Hx"));
        Assert.Equal(7, glyph.Glyph); Assert.Equal(0, glyph.SourceIndex);
        Assert.Equal(80, face.Measure("Hx", 100), 6);
        Assert.Equal(2, Assert.Single(TextShaper.Shape(face, "O")).Glyph);
    }
    [Fact]
    public void MultipleSubstitutionPreservesTheSourceClusterAndMarkAdvance() {
        var face = Font("substitution"); var glyphs = TextShaper.Shape(face, "é");
        Assert.Equal(new ushort[] { 4, 5 }, glyphs.Select(g => g.Glyph));
        Assert.All(glyphs, g => Assert.Equal(0, g.SourceIndex));
        Assert.Equal(50, face.Measure("é", 100), 6);
    }
    [Theory]
    [InlineData("HxO", new ushort[] { 1, 7, 2 })]
    [InlineData("HeO", new ushort[] { 1, 2, 3, 2 })]
    [InlineData("OxO", new ushort[] { 2, 3, 2 })]
    public void ChainedContextsApplyNestedLookupsOnlyInsideTheMatchedInput(string text, ushort[] expected) =>
        Assert.Equal(expected, TextShaper.Shape(Font("context"), text).Select(g => g.Glyph));

    [Fact]
    public void EmojiJoinersRemainAvailableToLigaturesAndOtherwiseHaveNoInkOrAdvance() {
        var face = Font("substitution");
        Assert.Equal(21, Assert.Single(TextShaper.Shape(face, "\uE001\u200D\uE002")).Glyph);
        Assert.Equal(120, face.Measure("\uE001\u200D\uE002", 100), 6);
        Assert.Equal(face.Measure("O", 100), face.Measure("O\u200D", 100), 6);
    }
    [Fact]
    public void ArabicFontFormsAreSelectedBeforeVisualReordering() {
        var face = Font("arabic");
        Assert.Equal(new ushort[] { 13, 12, 11 }, TextShaper.Shape(face, "ببب").Select(g => g.Glyph));
        Assert.Equal(14, Assert.Single(TextShaper.Shape(face, "ب")).Glyph);
        Assert.Equal(17, Assert.Single(TextShaper.Shape(face, "لا")).Glyph);
    }
    [Theory]
    [InlineData("بب", new ushort[] { 13, 11 })]
    [InlineData("ب‍ب", new ushort[] { 13, 11 })]
    [InlineData("ب‌ب", new ushort[] { 14, 14 })]
    public void ArabicJoinControlsSelectFontFormsWithoutLeavingVisibleGlyphs(string text, ushort[] expected) =>
        Assert.Equal(expected, TextShaper.Shape(Font("arabic"), text).Select(g => g.Glyph));
    [Theory]
    [InlineData("لا", new ushort[] { 17 })]
    [InlineData("ل‍ا", new ushort[] { 17 })]
    [InlineData("ل‌ا", new ushort[] { 16, 15 })]
    public void RequiredArabicLigaturesRespectTheJoinControl(string text, ushort[] expected) =>
        Assert.Equal(expected, TextShaper.Shape(Font("arabic"), text).Select(g => g.Glyph));
    [Fact]
    public void SubstitutedDefaultIgnorablesRemainHiddenAndRequiredOnlyFeaturesRunOnce() {
        var face = Font("hidden-controls");
        Assert.Equal(1, Assert.Single(TextShaper.Shape(face, "H‍")).Glyph);
        Assert.Equal(60, face.Measure("H‍", 100), 6);
        Assert.Equal(2, Assert.Single(TextShaper.Shape(Font("required"), "H")).Glyph);
    }
    [Fact]
    public void PairAndClassKerningShareTheMeasuredAndPaintedAdvance() {
        var face = Font("positioning");
        Assert.Equal(102, face.Measure("HO", 100), 6); // 600 + 500 - 80
        Assert.Equal(106, face.Measure("Hx", 100), 6); // 600 + 500 - 40
        var actual = new RgbaCanvas(160, 110, 1, face, 1, useDefaultOutlineFont: false);
        face.Draw(actual, 0, 0, "HO", ChartColor.Black, 100);
        var expected = new RgbaCanvas(160, 110, 1, face, 1, useDefaultOutlineFont: false);
        face.Draw(expected, 0, 0, "H", ChartColor.Black, 100); face.Draw(expected, 52, 0, "O", ChartColor.Black, 100);
        Assert.Equal(expected.ToOutputPixels(), actual.ToOutputPixels());
    }
    [Fact]
    public void MarkAnchorsAndStackedMarksKeepZeroAdvance() {
        var face = Font("positioning"); var glyphs = TextShaper.Shape(face, "H\u0301\u0301");
        Assert.Equal(3, glyphs.Count);
        Assert.Equal(-300, glyphs[1].OffsetX); Assert.Equal(700, glyphs[1].OffsetY);
        Assert.Equal(-300, glyphs[2].OffsetX); Assert.Equal(900, glyphs[2].OffsetY);
        Assert.Equal(60, face.Measure("H\u0301\u0301", 100), 6);
    }
    [Fact]
    public void MarksSkippedDuringLigatureFormationKeepTheirComponentAnchor() {
        var glyphs = TextShaper.Shape(Font("mark-ligature"), "H\u0301O\u0301");
        Assert.Equal(new ushort[] { 7, 5, 5 }, glyphs.Select(g => g.Glyph));
        Assert.Equal(-650, glyphs[1].OffsetX); Assert.Equal(-200, glyphs[2].OffsetX);
        Assert.All(glyphs.Skip(1), glyph => Assert.Equal(700, glyph.OffsetY));
    }
    [Fact]
    public void CursiveAnchorsAdjustTheRunAdvanceAndBaselineTogether() {
        var face = Font("cursive"); var glyphs = TextShaper.Shape(face, "HO");
        Assert.Equal(500, glyphs[0].Advance); Assert.Equal(400, glyphs[1].Advance);
        Assert.Equal(-100, glyphs[1].OffsetX); Assert.Equal(100, glyphs[1].OffsetY);
        Assert.Equal(90, face.Measure("HO", 100), 6);
    }
    [Fact]
    public void RightToLeftCursiveUsesTheOppositeSideBearingAndFinalBaseline() {
        var face = Font("cursive"); var glyphs = TextShaper.Shape(face, "بل");
        Assert.Equal(new ushort[] { 15, 10 }, glyphs.Select(g => g.Glyph));
        Assert.All(glyphs, glyph => Assert.Equal(100, glyph.Advance));
        Assert.Equal(0, glyphs[0].OffsetX); Assert.Equal(0, glyphs[0].OffsetY);
        Assert.Equal(-400, glyphs[1].OffsetX); Assert.Equal(-100, glyphs[1].OffsetY);
        Assert.Equal(20, face.Measure("بل", 100), 6);
    }
    [Fact]
    public void LayoutDoesNotLigateAcrossDifferentPaintOwners() {
        var face = Font("substitution"); var glyphs = TextShaper.ShapeStyled("Hx", new[] { face, face }, new[] { 0, 1 });
        Assert.Equal(new ushort[] { 2, 3 }, glyphs.Select(g => g.Glyph));
        Assert.Equal(new[] { 0, 1 }, glyphs.Select(g => g.SourceIndex));
    }
    [Theory]
    [InlineData("extension", "H", new ushort[] { 2 })]
    [InlineData("alternate", "H", new ushort[] { 2 })]
    [InlineData("reverse", "HxO", new ushort[] { 1, 7, 2 })]
    [InlineData("recursive", "HxO", new ushort[] { 1, 3, 2 })]
    public void ExtendedReverseAlternateAndSelfReferencingLookupsAreBounded(string fixture, string text, ushort[] expected) =>
        Assert.Equal(expected, TextShaper.Shape(Font(fixture), text).Select(g => g.Glyph));

    [Theory]
    [InlineData(10)]
    [InlineData(16)]
    public void OptionalLayoutCannotReadPastItsDeclaredTableLength(int length) {
        var data = Bytes("substitution");
        var record = TableRecord(data, "GSUB");
        Write32(data, record + 12, length);
        var face = TrueTypeFont.TryLoad(data)!;
        Assert.Equal(new ushort[] { 1, 3 }, TextShaper.Shape(face, "Hx").Select(g => g.Glyph));
        Assert.Equal(110, face.Measure("Hx", 100), 6);
    }
    [Fact]
    public void MalformedExtensionDoesNotInvalidateTheUsableOutlines() {
        var data = Bytes("extension"); var record = TableRecord(data, "GSUB");
        var start = Read32(data, record + 8); var lookups = start + Read16(data, start + 8);
        var lookup = lookups + Read16(data, lookups + 2); var subtable = lookup + Read16(data, lookup + 6);
        data[subtable + 2] = 0; data[subtable + 3] = 7; // An extension may not extend another extension.
        var face = TrueTypeFont.TryLoad(data)!;
        Assert.Equal(1, Assert.Single(TextShaper.Shape(face, "H")).Glyph);
        var canvas = new RgbaCanvas(100, 100, 1, face, 1, useDefaultOutlineFont: false);
        Assert.True(face.Draw(canvas, 0, 0, "H", ChartColor.Black, 100));
    }
    private static int TableRecord(byte[] data, string tag) {
        for (var i = 0; i < Read16(data, 4); i++) { var at = 12 + i * 16; if (System.Text.Encoding.ASCII.GetString(data, at, 4) == tag) return at; }
        throw new InvalidOperationException("Fixture table missing: " + tag);
    }
    private static int Read16(byte[] data, int at) => (data[at] << 8) | data[at + 1];
    private static int Read32(byte[] data, int at) => (Read16(data, at) << 16) | Read16(data, at + 2);
    private static void Write32(byte[] data, int at, int value) { data[at] = (byte)(value >> 24); data[at + 1] = (byte)(value >> 16); data[at + 2] = (byte)(value >> 8); data[at + 3] = (byte)value; }
    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    public void SvgRasterAndDirectDrawingUseTheSamePositionedGlyphsWithoutClippingStackedMarks(int count) {
        var path = Path.Combine(Path.GetTempPath(), "cfx-layout-" + Guid.NewGuid().ToString("N") + ".ttf");
        File.WriteAllBytes(path, Bytes("positioning"));
        try {
            FontRegistry.Register("CFX Layout Test", path);
            var font = TypographyFontResolver.ResolveFace("CFX Layout Test", 400, false).Font!;
            var text = "H" + new string('\u0301', count);
            var actual = SvgRasterizer.ToImage("<svg xmlns='http://www.w3.org/2000/svg' width='180' height='180'><text x='10' y='140' font-family='CFX Layout Test' font-size='100'>" + text + "</text></svg>").Pixels;
            var direct = new RgbaCanvas(180, 180, 4, font, 1, useDefaultOutlineFont: false);
            font.Draw(direct, 10, 60, text, ChartColor.Black, 100);
            Assert.All(direct.ToOutputPixels().Zip(actual), pair => Assert.InRange(Math.Abs(pair.First - pair.Second), 0, 1));
        } finally { FontRegistry.Clear(); File.Delete(path); }
    }
    [Fact]
    public void WidthFittedTextRetainsMarksAboveTheNominalLineBox() {
        var face = Font("positioning"); var text = "H" + new string('\u0301', 4);
        var actual = new RgbaCanvas(180, 180, 1, face, 1, useDefaultOutlineFont: false);
        actual.DrawTextFitted(10, 60, text, ChartColor.Black, 100, 30, face);
        // The fourth anchored mark occupies y=0..10 after the horizontal-only fit.
        var pixels = actual.ToOutputPixels(); var ink = 0;
        for (var y = 0; y < 10; y++) for (var x = 10; x < 40; x++) if (pixels[(y * 180 + x) * 4 + 3] != 0) ink++;
        Assert.True(ink > 0, "Fitting the advance must preserve ink above the font's ascent.");
    }
    [Theory]
    [InlineData("context-sequence", "HOxe", new ushort[] { 1, 7, 8 })]
    [InlineData("context-expansion", "HOe", new ushort[] { 1, 2, 7, 4 })]
    public void ContextRecordsAddressTheSequenceChangedByEarlierRecords(string fixture, string text, ushort[] expected) =>
        Assert.Equal(expected, TextShaper.Shape(Font(fixture), text).Select(glyph => glyph.Glyph));
    [Theory]
    [InlineData("mark-filter")]
    [InlineData("mark-attachment-filter")]
    public void MarkAttachmentSkipsMarksExcludedByTheLookupFilter(string fixture) {
        var glyphs = TextShaper.Shape(Font(fixture), "H\u0301\u0300\u0301");
        Assert.Equal(new double[] { 0, 700, -100, 900 }, glyphs.Select(glyph => glyph.OffsetY));
        Assert.Equal(60, TrueTypeFont.MeasureGlyphs(glyphs, 100), 6);
    }
    [Theory]
    [InlineData("script-routing", "ṪẶ")]
    [InlineData("script-routing", "ἀἁ")]
    [InlineData("script-routing", "Ꙁꙁ")]
    [InlineData("script-routing", "\U0001DF00\U0001DF01")]
    [InlineData("greek-only", "ἀἁ")]
    public void ExtendedScriptLettersUseTheirFontsLayoutInMeasurementAndDrawing(string fixture, string text) {
        var face = Font(fixture);
        Assert.Equal(102, face.Measure(text, 100), 6);
        var canvas = new RgbaCanvas(160, 110, 1, face, 1, useDefaultOutlineFont: false);
        face.Draw(canvas, 0, 0, text, ChartColor.Black, 100);
        var expected = new RgbaCanvas(160, 110, 1, face, 1, useDefaultOutlineFont: false);
        face.DrawGlyphs(expected, 0, 0, TextShaper.Shape(face, text), ChartColor.Black, 100, false);
        Assert.Equal(expected.ToOutputPixels(), canvas.ToOutputPixels());
    }
    [Fact]
    public void RotatedTextPreservesMarksBeyondItsOriginalFixedPadding() {
        var face = Font("positioning");
        var canvas = new RgbaCanvas(240, 240, 1, face, 1, useDefaultOutlineFont: false);
        canvas.DrawTextRotated(90, 90, "H" + new string('\u0301', 4), ChartColor.Black, 100, 90, 0, 0);
        var pixels = canvas.ToOutputPixels(); var ink = 0;
        for (var y = 115; y < 140; y++) for (var x = 140; x < 160; x++) if (pixels[(y * 240 + x) * 4 + 3] != 0) ink++;
        Assert.True(ink > 0, "Rotation must retain the fourth mark before transforming the glyph surface.");
    }
}
