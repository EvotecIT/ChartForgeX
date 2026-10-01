using ChartForgeX.Composition;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>
/// Light hinting of small raster text: at 12 output pixels and below the baseline, x-height, and cap
/// height land on whole pixels; nothing moves horizontally; larger text and
/// <see cref="TextHinting.None"/> draw the outlines exactly.
/// </summary>
public sealed class SmallTextHintingTests {
    [Theory]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    public void BaselineAndCapHeightLandOnWholePixels(double size) {
        // The generated CFF font's H is a flat-topped rectangle from the baseline to the cap height (700 units).
        var font = TrueTypeFont.TryLoad(OpenTypeTestFonts.NameKeyed())!;
        // Placed so the unhinted baseline falls half way through a pixel row.
        var y = 12.5 - 0.8 * size;
        var hinted = Rows(Draw(font, "HHH", size, y, TextHinting.Auto));
        var bottom = hinted.FindLastIndex(alpha => alpha > 0);
        var top = hinted.FindIndex(alpha => alpha > 0);
        // Whole-pixel edges: the first and last inked rows are fully covered where the stems are.
        Assert.True(hinted[bottom] >= 250, "The baseline row should be fully inked: " + hinted[bottom]);
        Assert.True(hinted[top] >= 250, "The cap-height row should be fully inked: " + hinted[top]);
        Assert.Equal(Math.Round(size * 0.7, MidpointRounding.AwayFromZero), bottom - top + 1);

        var exact = Rows(Draw(font, "HHH", size, y, TextHinting.None));
        Assert.True(exact[exact.FindLastIndex(alpha => alpha > 0)] < 250, "Unhinted text at a fractional position has a partly covered bottom row.");
    }

    [Fact]
    public void HintingNeverMovesTextHorizontally() {
        var font = TrueTypeFont.TryLoadDefault();
        if (font == null) return;
        foreach (var size in new[] { 9.0, 10.0, 11.0, 12.0 }) {
            var hinted = Columns(Draw(font, "Hamburgefonstiv|", size, 2.4, TextHinting.Auto));
            var exact = Columns(Draw(font, "Hamburgefonstiv|", size, 2.4, TextHinting.None));
            // The same columns carry ink, and the closing bar sits in the same column.
            Assert.Equal(exact.Select(value => value > 0), hinted.Select(value => value > 0));
            Assert.Equal(font.Measure("Hamburgefonstiv", size), font.Measure("Hamburgefonstiv", size), 12);
        }
    }

    [Fact]
    public void LargerTextAndHintingNoneDrawExactOutlines() {
        var font = TrueTypeFont.TryLoadDefault();
        if (font == null) return;
        Assert.Equal(Draw(font, "Hamburg", 13, 2.4, TextHinting.None), Draw(font, "Hamburg", 13, 2.4, TextHinting.Auto));
        Assert.NotEqual(Draw(font, "Hamburg", 11, 2.4, TextHinting.None), Draw(font, "Hamburg", 11, 2.4, TextHinting.Auto));
        Assert.Equal(Draw(font, "Hamburg", 11, 2.4, TextHinting.Auto), Draw(font, "Hamburg", 11, 2.4, TextHinting.Auto));
    }

    [Fact]
    public void TextStyleHintingReachesCompositionText() {
        var style = new TextStyle { FontSize = 11, Color = ChartColor.Black };
        var hinted = ImageComposition.CreateTransparent(200, 30).DrawText(4, 5.3, 190, "Hamburgefonstiv", style).ToImage().Pixels;
        style.Hinting = TextHinting.None;
        var exact = ImageComposition.CreateTransparent(200, 30).DrawText(4, 5.3, 190, "Hamburgefonstiv", style).ToImage().Pixels;
        if (TrueTypeFont.TryLoadDefault() != null) Assert.NotEqual(exact, hinted);
        Assert.Equal(TextHinting.None, style.Clone().Hinting);
        Assert.Throws<ArgumentOutOfRangeException>(() => style.Hinting = (TextHinting)7);
    }

    private static byte[] Draw(TrueTypeFont font, string text, double size, double y, TextHinting hinting) {
        var canvas = new RgbaCanvas(260, 30, 1, font, 1, useDefaultOutlineFont: false) { TextHinting = hinting };
        font.Draw(canvas, 2, y, text, ChartColor.Black, size);
        return canvas.ToOutputPixels();
    }

    // The strongest alpha of each row, and the summed alpha of each column.
    private static List<int> Rows(byte[] pixels) {
        var rows = new List<int>();
        for (var y = 0; y < 30; y++) {
            var strongest = 0;
            for (var x = 0; x < 260; x++) strongest = Math.Max(strongest, pixels[(y * 260 + x) * 4 + 3]);
            rows.Add(strongest);
        }

        return rows;
    }

    private static int[] Columns(byte[] pixels) {
        var columns = new int[260];
        for (var y = 0; y < 30; y++) for (var x = 0; x < 260; x++) columns[x] += pixels[(y * 260 + x) * 4 + 3];
        return columns;
    }
}
