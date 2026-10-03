using ChartForgeX.Composition;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Svg;
using ChartForgeX.SvgRaster;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Language systems must affect glyph selection, measurement and paint without contaminating defaults.</summary>
[Collection(nameof(FontRegistryCollection))]
public sealed class TextLanguageTests {
    private static byte[] Bytes(string name = "language") {
        using var stream = typeof(TextLanguageTests).Assembly.GetManifestResourceStream("ChartForgeX.Tests.Fixtures.OpenType." + name + ".ttf")!;
        using var output = new MemoryStream(); stream.CopyTo(output); return output.ToArray();
    }
    private static TrueTypeFont Font(string name = "language") => TrueTypeFont.TryLoad(Bytes(name))!;

    [Theory]
    [InlineData(null, "HH", new ushort[] { 2, 2 }, 99)]
    [InlineData("TRK ", "HH", new ushort[] { 3, 3 }, 98)]
    [InlineData("SRB ", "бб", new ushort[] { 6, 6 }, 132)]
    [InlineData("BGR ", "бб", new ushort[] { 7, 7 }, 146)]
    [InlineData("URD ", "ب", new ushort[] { 9 }, 85)]
    [InlineData("ZZZ ", "HH", new ushort[] { 2, 2 }, 99)]
    public void SelectedLanguageUsesItsOwnSubstitutionAndPositioning(string? language, string text, ushort[] glyphs, double width) {
        var face = Font().WithLanguage(language);
        Assert.Equal(glyphs, TextShaper.Shape(face, text).Select(g => g.Glyph));
        Assert.Equal(width, face.Measure(text, 100), 6);
    }

    [Fact]
    public void LanguageWithoutDefaultSystemStillShapesAndUnknownLanguageDoesNotChooseAnArbitrarySystem() {
        var face = Font("language-no-default");
        Assert.Equal(98, face.WithLanguage("TRK ").Measure("HH", 100), 6);
        Assert.Equal(90, face.WithLanguage("ZZZ ").Measure("HH", 100), 6);
        Assert.Equal(90, face.Measure("HH", 100), 6);
    }

    [Fact]
    public void ConcurrentLanguagesKeepSeparatePlansAndShapedRunCaches() {
        var face = Font();
        Parallel.For(0, 100, i => {
            var selected = face.WithLanguage(i % 2 == 0 ? "SRB " : "BGR ");
            Assert.Equal(i % 2 == 0 ? 132 : 146, selected.Measure("бб", 100), 6);
        });
        Assert.Equal(99, face.Measure("HH", 100), 6);
        Assert.Equal(130, face.Measure("бб", 100), 6);
    }

    [Fact]
    public void WholeClusterFallbackRetainsTheRequestedLanguage() {
        var path = Path.Combine(Path.GetTempPath(), "cfx-language-" + Guid.NewGuid().ToString("N") + ".ttf");
        File.WriteAllBytes(path, Bytes());
        try {
            FontRegistry.Register("CFX Language Fallback", path);
            var primary = Font("substitution").WithFallbackFamilies(new[] { "CFX Language Fallback" }).WithLanguage("SRB ");
            var glyphs = TextShaper.Shape(primary, "бб");
            Assert.Equal(new ushort[] { 6, 6 }, glyphs.Select(g => g.Glyph));
            Assert.All(glyphs, glyph => Assert.Equal("SRB ", glyph.Face.LanguageTag));
            Assert.Equal(132, primary.Measure("бб", 100), 6);
        } finally { FontRegistry.Clear(); File.Delete(path); }
    }

    [Fact]
    public void PublicStyleMeasurementAndCompositionPaintTheSameLocalizedRun() {
        var path = Path.Combine(Path.GetTempPath(), "cfx-language-" + Guid.NewGuid().ToString("N") + ".ttf");
        File.WriteAllBytes(path, Bytes());
        try {
            var style = new TextStyle { Font = FontSpec.FromFile(path), FontSize = 100, OpenTypeLanguageTag = "SRB", Hinting = TextHinting.None };
            Assert.Equal(132, TextLayoutEngine.Measure("бб", style).Width, 6);
            var actual = ImageComposition.Create(200, 130, ChartColor.Transparent).DrawText(0, 0, 200, "бб", style, TextWrapMode.NoWrap).ToImage();
            var face = Font().WithLanguage("SRB ");
            var expected = new RgbaCanvas(200, 130, 1, face, 1, useDefaultOutlineFont: false);
            face.Draw(expected, 0, 0, "бб", ChartColor.Black, 100);
            Assert.Equal(expected.ToOutputPixels(), actual.Pixels);
        } finally { File.Delete(path); }
    }

    [Fact]
    public void SvgLanguageInheritanceAndNormalResetMatchDirectLocalizedPaint() {
        var path = Path.Combine(Path.GetTempPath(), "cfx-language-" + Guid.NewGuid().ToString("N") + ".ttf");
        File.WriteAllBytes(path, Bytes());
        try {
            FontRegistry.Register("CFX Language SVG", path);
            var svg = "<svg xmlns='http://www.w3.org/2000/svg' width='200' height='130'><g font-family='CFX Language SVG' font-size='100' style=\"font-language-override:'SRB'\"><text x='0' y='85'>б<tspan style='font-language-override:normal'>б</tspan></text></g></svg>";
            Assert.True(SvgRasterRenderer.TryRenderDocument(svg, null, 200, 130, out var actual));
            var expected = new RgbaCanvas(200, 130, 1, Font(), 1, useDefaultOutlineFont: false);
            Font().WithLanguage("SRB ").Draw(expected, 0, 0, "б", ChartColor.Black, 100);
            Font().Draw(expected, 70, 0, "б", ChartColor.Black, 100);
            Assert.Equal(expected.ToOutputPixels(), actual);
        } finally { FontRegistry.Clear(); File.Delete(path); }
    }

    [Fact]
    public void ChartAndPaginatedGridExportTheExplicitLanguage() {
        var chart = Chart.Create().WithTitle("бб").WithTitleStyle(s => s.WithOpenTypeLanguage("SRB"));
        var svg = new SvgChartRenderer().Render(chart);
        Assert.Contains("font-language-override:'SRB '", svg);
        var grid = new ChartGrid { Title = "бб" }; grid.TitleStyle.WithOpenTypeLanguage("BGR"); grid.Add(chart);
        var page = Assert.Single(grid.Paginate(1));
        Assert.Equal("BGR ", page.Grid.TitleStyle.OpenTypeLanguageTag);
        Assert.Contains("font-language-override:'BGR '", new SvgChartGridRenderer().Render(page.Grid));
    }

    [Fact]
    public void StyleClonesAndOverridesPreserveOrResetLanguageWithoutMutation() {
        var style = new TextStyle { OpenTypeLanguageTag = "SRB" };
        var copy = style.Clone(); copy.OpenTypeLanguageTag = "BGR";
        Assert.Equal("SRB ", style.OpenTypeLanguageTag);
        Assert.Equal("BGR ", copy.OpenTypeLanguageTag);
        Assert.Equal("SRB ", new TextStyleOverride().Resolve(style).OpenTypeLanguageTag);
        Assert.Null(new TextStyleOverride().WithOpenTypeLanguage("normal").Resolve(style).OpenTypeLanguageTag);
    }

    [Theory]
    [InlineData("sr-Latn")]
    [InlineData("SRB;")]
    [InlineData("S'B")]
    [InlineData("СРБ")]
    public void UnsupportedCultureNamesAndMarkupAreRejected(string language) =>
        Assert.Throws<ArgumentException>(() => new TextStyle { OpenTypeLanguageTag = language });
}
