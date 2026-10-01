using System.Text;
using ChartForgeX.Raster;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>
/// UAX #9 reference orderings: each case gives the paragraph level and the expected display order
/// from left to right, with brackets mirrored at odd levels and formatting characters removed.
/// Hebrew letters stand for R, Arabic letters for AL, Latin letters for L.
/// </summary>
public sealed class UnicodeBidiTests {
    [Theory]
    [InlineData("abc \u05D0\u05D1\u05D2 def", 0, "abc \u05D2\u05D1\u05D0 def")] // R run inside L text
    [InlineData("\u05D0\u05D1\u05D2 abc \u05D3\u05D4\u05D5", 1, "\u05D5\u05D4\u05D3 abc \u05D2\u05D1\u05D0")] // L run inside R text
    [InlineData("\u05D0\u05D1\u05D2 123", 0, "123 \u05D2\u05D1\u05D0")] // European digits after R stay left-to-right (W7, I1)
    [InlineData("abc 123 \u05D0\u05D1\u05D2", 0, "abc 123 \u05D2\u05D1\u05D0")] // digits after L are L (W7)
    [InlineData("\u05D0\u05D1 12 cd", 0, "12 \u05D1\u05D0 cd")] // neutrals between R and numbers are R (N1)
    [InlineData("\u0628 12 abc", 0, "12 \u0628 abc")] // digits after AL are Arabic numbers (W2)
    [InlineData("\u0628\u062A 12", 0, "12 \u062A\u0628")]
    [InlineData("\u05D0 1,2", 1, "1,2 \u05D0")] // a common separator between digits joins them (W4)
    [InlineData("\u05D0 $12", 1, "$12 \u05D0")] // a terminator before digits joins them (W5)
    [InlineData("\u05D0 (abc) \u05D1", 1, "\u05D1 (abc) \u05D0")] // brackets take the embedding direction (N0)
    [InlineData("a (\u05D0\u05D1) c", 0, "a (\u05D1\u05D0) c")] // opposite content, embedding context (N0)
    [InlineData("\u05D0\u05D1 (\u05D2\u05D3) \u05D4", 0, "\u05D4 (\u05D3\u05D2) \u05D1\u05D0")] // opposite content and context (N0), mirrored
    [InlineData("\u05D0 (1) \u05D1", 0, "\u05D1 (1) \u05D0")] // numbers inside brackets count as R (N0)
    [InlineData("a\u202Bbc\u202Cd", 0, "abcd")] // an RLE embedding of L text keeps it left-to-right
    [InlineData("a\u202Ebc\u202Cd", 0, "acbd")] // an RLO override reverses L text
    [InlineData("a \u2068\u05D0\u05D1 c\u2069 d", 0, "a c \u05D1\u05D0 d")] // FSI takes its direction from its first strong character
    [InlineData("\u05D0 \u2068abc\u2069", 1, "abc \u05D0")] // an LTR isolate inside RTL text
    [InlineData("\u05D0\u05D1\tabc\t\u05D2\u05D3", 0, "\u05D1\u05D0\tabc\t\u05D3\u05D2")] // segment separators reset (L1)
    [InlineData("\u05D0\u05B5\u05D1", 0, "\u05D1\u05B5\u05D0")] // a mark takes the direction of its base (W1)
    public void ReordersLikeTheReferenceAlgorithm(string text, int paragraphLevel, string expected) {
        Assert.Equal(expected, Visual(text, paragraphLevel));
    }

    [Theory]
    [InlineData("123 \u05D0\u05D1", 1)]
    [InlineData("abc \u05D0", 0)]
    [InlineData("\u2067abc\u2069 \u05D0\u05D1", 1)] // isolated text is skipped when looking for the first strong character
    [InlineData("123 ...", 0)]
    public void ParagraphLevelComesFromTheFirstStrongCharacter(string text, int expected) {
        Assert.Equal(expected, UnicodeBidi.ParagraphLevel(CodePoints(text)));
    }

    [Fact]
    public void ResolvedLevelsFollowTheImplicitRules() {
        // L at the RTL paragraph level goes up one, numbers two at an even level.
        Assert.Equal(new byte[] { 1, 1, 2, 2 }, UnicodeBidi.ResolveLevels(CodePoints("\u05D0 ab"), 1));
        Assert.Equal(new byte[] { 1, 1, 2, 2 }, UnicodeBidi.ResolveLevels(CodePoints("\u05D0 12"), 0));
        Assert.False(UnicodeBidi.NeedsResolution(CodePoints("Plain text 123 (ok)")));
        Assert.True(UnicodeBidi.NeedsResolution(CodePoints("text \u05D0")));
    }

    [Fact]
    public void ShapedRunsDisplayRightToLeftTextInVisualOrder() {
        var font = TrueTypeFont.TryLoadDefault();
        if (font == null || !font.HasGlyph(0x05D0) || !font.HasGlyph(0x05D1)) return;
        var glyphs = TextShaper.Shape(font, "a \u05D0\u05D1 (x)");
        var expected = new[] { 'a', ' ', '\u05D1', '\u05D0', ' ', '(', 'x', ')' };
        Assert.Equal(expected.Select(ch => font.MapGlyph(ch)), glyphs.Select(glyph => glyph.Glyph));
    }

    private static string Visual(string text, int paragraphLevel) {
        var codePoints = CodePoints(text);
        var levels = UnicodeBidi.ResolveLevels(codePoints, paragraphLevel);
        var builder = new StringBuilder();
        foreach (var index in UnicodeBidi.VisualOrder(levels)) {
            var cp = codePoints[index];
            if (TextShaper.IsIgnorable(cp)) continue;
            builder.Append(char.ConvertFromUtf32((levels[index] & 1) == 1 ? BidiCharacterData.Mirror(cp) : cp));
        }

        return builder.ToString();
    }

    private static List<int> CodePoints(string text) {
        var codePoints = new List<int>();
        for (var index = 0; index < text.Length;) codePoints.Add(TrueTypeFont.ReadCodePoint(text, ref index));
        return codePoints;
    }
}
