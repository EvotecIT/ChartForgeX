using ChartForgeX.Raster;
using ChartForgeX.SvgRaster;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>
/// SVG <c>&lt;text&gt;</c> rasterization resolves font-family stacks, weights, and slant through the
/// same face matching as <see cref="FontSpec"/> text. Cases that need a particular installed
/// family return early on hosts without it.
/// </summary>
public sealed class SvgRasterTextFontTests {
    [Theory]
    [InlineData("lighter", 100)]
    [InlineData("bolder", 700)]
    public void RelativeWeightUsesParentDespiteOverriddenPresentationAttribute(string relative, int expected) {
        var svg = SvgRasterParser.ParseDocument("<svg xmlns=\"http://www.w3.org/2000/svg\"><text font-weight=\"900\" style=\"font-weight:" + relative + "\">text</text></svg>");
        var parent = SvgRasterStyle.Default;
        parent.FontWeight = 400;
        var style = SvgRasterStyle.Resolve(parent, svg.Children[0]);
        Assert.Equal(expected, style.FontWeight);
    }

    [Fact]
    public void CssWeightsResolveKeywordsNumbersAndRelativeValues() {
        Assert.Equal(400, TypographyFontResolver.ParseCssWeight("normal", 700));
        Assert.Equal(700, TypographyFontResolver.ParseCssWeight(" bold ", 400));
        Assert.Equal(600, TypographyFontResolver.ParseCssWeight("600", 400));
        Assert.Equal(650, TypographyFontResolver.ParseCssWeight("650", 400));
        Assert.Equal(700, TypographyFontResolver.ParseCssWeight("bolder", 400));
        Assert.Equal(900, TypographyFontResolver.ParseCssWeight("bolder", 700));
        Assert.Equal(400, TypographyFontResolver.ParseCssWeight("bolder", 300));
        Assert.Equal(100, TypographyFontResolver.ParseCssWeight("lighter", 400));
        Assert.Equal(400, TypographyFontResolver.ParseCssWeight("lighter", 700));
        Assert.Equal(500, TypographyFontResolver.ParseCssWeight("heavy", 500));
        Assert.Equal(500, TypographyFontResolver.ParseCssWeight("1200", 500));
    }

    [Fact]
    public void FontFamilyAndWeightSelectInstalledFaces() {
        if (!HasFace("Segoe UI", 400) || !HasFace("Segoe UI", 700) || !HasFace("Arial", 400)) return;
        var segoe = Text("font-family=\"Segoe UI\"");
        var arial = Text("font-family=\"Arial\"");
        var bold = Text("font-family=\"Segoe UI\" font-weight=\"700\"");
        Assert.NotEqual(arial, segoe);
        Assert.NotEqual(segoe, bold);
        Assert.True(Ink(bold) > Ink(segoe) * 1.15, "The bold face should lay down clearly more ink than the regular one.");
        Assert.False(TypographyFontResolver.ResolveFace("Segoe UI", 700, false).SynthesizeBold);

        // Keyword, numeric, style attribute, stylesheet-free inheritance, and tspan all reach the same face.
        Assert.Equal(bold, Text("font-family=\"Segoe UI\" font-weight=\"bold\""));
        Assert.Equal(bold, Text("style=\"font-family: 'Segoe UI', sans-serif; font-weight: bold\""));
        Assert.Equal(bold, Render("<g font-family=\"'No Such Family 4E1D', 'Segoe UI', sans-serif\" font-weight=\"700\"><text x=\"8\" y=\"40\" font-size=\"28\">Hamburgefonstiv</text></g>"));
        Assert.Equal(bold, Render("<text x=\"8\" y=\"40\" font-size=\"28\" font-family=\"Segoe UI\"><tspan font-weight=\"bold\">Hamburgefonstiv</tspan></text>"));
        Assert.Equal(bold, Render("<g font-weight=\"400\"><text x=\"8\" y=\"40\" font-size=\"28\" font-family=\"Segoe UI\" font-weight=\"bolder\">Hamburgefonstiv</text></g>"));

        if (HasFace("Segoe UI", 600)) Assert.NotEqual(bold, Text("font-family=\"Segoe UI\" font-weight=\"600\""));
    }

    [Fact]
    public void SerifFamiliesUseTheirRealBoldAndItalicFaces() {
        if (!HasFace("Georgia", 400) || !HasFace("Georgia", 700) || InstalledFontCatalog.Find("Georgia", 400, true)?.Italic != true) return;
        var regular = Text("font-family=\"Georgia\"");
        var bold = Text("font-family=\"Georgia\" font-weight=\"700\"");
        Assert.True(Ink(bold) > Ink(regular) * 1.15, "Georgia Bold should lay down clearly more ink than Georgia.");
        var italic = TypographyFontResolver.ResolveFace("Georgia", 400, true);
        Assert.False(italic.SynthesizeItalic);
        Assert.NotEqual(regular, Text("font-family=\"Georgia\" font-style=\"italic\""));
    }

    [Fact]
    public void TextSitsOnItsBaselineWhateverTheFaceAscent() {
        foreach (var family in new[] { "sans-serif", "Arial", "Segoe UI", "Georgia", "Calibri" }) {
            if (family != "sans-serif" && !HasFace(family, 400)) continue;
            var image = SvgRasterizer.ToImage("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"120\" height=\"100\"><text x=\"10\" y=\"60\" font-family=\"" + family + "\" font-size=\"40\">H</text></svg>");
            var bottom = -1;
            for (var y = 0; y < image.Height; y++) {
                for (var x = 0; x < image.Width; x++) if (image.Pixels[(y * image.Width + x) * 4 + 3] > 128) bottom = y;
            }

            if (bottom < 0) continue;
            // The H stands on the baseline: its last solid row is the one just above y = 60.
            Assert.InRange(bottom, 58, 60);
        }
    }

    private static bool HasFace(string family, int weight) => InstalledFontCatalog.Find(family, weight, false)?.Weight == weight;

    private static byte[] Text(string attributes) => Render("<text x=\"8\" y=\"40\" font-size=\"28\" " + attributes + ">Hamburgefonstiv</text>");

    private static byte[] Render(string body) =>
        SvgRasterizer.ToImage("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"320\" height=\"56\">" + body + "</svg>").Pixels;

    private static long Ink(byte[] pixels) {
        long ink = 0;
        for (var index = 3; index < pixels.Length; index += 4) ink += pixels[index];
        return ink;
    }
}
