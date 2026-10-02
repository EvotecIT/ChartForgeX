using ChartForgeX.Raster;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>
/// Arabic contextual joining: each letter's presentation form comes from the joining types of its
/// neighbours, marks are transparent, tatweel joins, ZWNJ breaks, and lam-alef becomes a ligature.
/// </summary>
public sealed class ArabicShapingTests {
    [Theory]
    [InlineData("\u0628\u0628", new[] { 0xFE91, 0xFE90 })] // beh beh: initial, final
    [InlineData("\u0628\u0628\u0628", new[] { 0xFE91, 0xFE92, 0xFE90 })] // initial, medial, final
    [InlineData("\u0628\u0627", new[] { 0xFE91, 0xFE8E })] // beh alef: initial, final
    [InlineData("\u0627\u0628", new[] { 0xFE8D, 0xFE8F })] // alef does not join forward: both isolated
    [InlineData("\u0645\u0631\u062D\u0628\u0627", new[] { 0xFEE3, 0xFEAE, 0xFEA3, 0xFE92, 0xFE8E })] // marhaba
    [InlineData("\u0640\u0628", new[] { 0x0640, 0xFE90 })] // tatweel joins the following letter
    [InlineData("\u0628\u200C\u0628", new[] { 0xFE8F, 0x200C, 0xFE8F })] // ZWNJ breaks joining
    [InlineData("\u0628\u064E\u0628", new[] { 0xFE91, 0x064E, 0xFE90 })] // a fatha is transparent
    [InlineData("\u067E\u06AF", new[] { 0xFB58, 0xFB93 })] // Persian peh and gaf (Forms-A)
    [InlineData("\u0628\u0649\u0628", new[] { 0xFE91, 0xFBE9, 0xFE90 })] // alef maksura medial (Forms-A)
    [InlineData("a\u0628", new[] { 'a', 0xFE8F })] // Latin letters do not join
    public void LettersTakeTheirContextualForms(string text, int[] expected) {
        var letters = text.Select(ch => (int)ch).ToArray();
        var forms = ArabicShaping.ResolveForms(letters);
        Assert.Equal(expected, letters.Select((cp, i) => ArabicShaping.PresentationForm(cp, forms[i])).ToArray());
    }

    [Fact]
    public void LamAlefUsesTheLigatureForms() {
        Assert.Equal(0xFEFB, ArabicShaping.LamAlef(0x0627, final: false));
        Assert.Equal(0xFEFC, ArabicShaping.LamAlef(0x0627, final: true));
        Assert.Equal(0xFEF5, ArabicShaping.LamAlef(0x0622, final: false));
        Assert.Equal(0xFEF7, ArabicShaping.LamAlef(0x0623, final: false));
        Assert.Equal(0xFEFA, ArabicShaping.LamAlef(0x0625, final: true));
        Assert.Equal(-1, ArabicShaping.LamAlef(0x0628, final: false));
    }

    [Fact]
    public void JoinerBetweenArabicLettersPreservesEachContextualGlyph() {
        var font = TrueTypeFont.TryLoadDefault();
        if (font == null || !font.HasGlyph(0xFE91) || !font.HasGlyph(0xFE90)) return;
        var glyphs = TextShaper.Shape(font, "\u0628\u200D\u0628");
        Assert.Equal(new[] { font.MapGlyph(0xFE90), font.MapGlyph(0xFE91) }, glyphs.Select(glyph => glyph.Glyph).ToArray());
    }

    [Fact]
    public void ShapedTextUsesTheFacesPresentationFormsInVisualOrder() {
        var font = TrueTypeFont.TryLoadDefault();
        if (font == null || !font.HasGlyph(0xFE91) || !font.HasGlyph(0xFEFC)) return;
        // "salam" (seen, lam-alef, meem): displayed right to left, so the final meem comes first.
        var glyphs = TextShaper.Shape(font, "\u0633\u0644\u0627\u0645");
        Assert.Equal(new[] { font.MapGlyph(0xFEE1), font.MapGlyph(0xFEFC), font.MapGlyph(0xFEB3) }, glyphs.Select(glyph => glyph.Glyph).ToArray());
        Assert.Equal(font.Measure("\uFEB3\uFEFC\uFEE1", 20), font.Measure("\u0633\u0644\u0627\u0645", 20), 6);
    }
}
