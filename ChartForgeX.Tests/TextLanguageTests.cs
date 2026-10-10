using ChartForgeX.Composition;
using ChartForgeX.Core;
using ChartForgeX.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Svg;
using ChartForgeX.SvgRaster;
using ChartForgeX.Typography;
using System.Xml.Linq;
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
        var chart = Chart.Create().WithTitle("бб").ConfigureTitleStyle(s => s.WithOpenTypeLanguage("SRB"));
        var svg = chart.ToSvg();
        Assert.Contains("font-language-override:'SRB '", svg);
        var grid = new ChartGrid { Title = "бб" }; grid.TitleStyle.WithOpenTypeLanguage("BGR"); grid.Add(chart);
        var page = Assert.Single(grid.Paginate(1));
        Assert.Equal("BGR ", page.Grid.TitleStyle.OpenTypeLanguageTag);
        Assert.Contains("font-language-override:'BGR '", page.Grid.ToSvg());
    }

    [Fact]
    public void SvgAxisFittingReservesTheLocalizedAdvanceWidth() {
        var path = Path.Combine(Path.GetTempPath(), "cfx-language-" + Guid.NewGuid().ToString("N") + ".ttf");
        File.WriteAllBytes(path, Bytes());
        try {
            FontRegistry.Register("CFX Language Fitting", path);
            const string sourceLabel = "бббббб";
            var chart = Chart.Create().WithSize(360, 600).WithXLabels(sourceLabel).WithXAxisLabelAngle(0)
                .ConfigureTickLabelStyle(s => s.WithFontFamily("CFX Language Fitting").WithFontSize(100).WithWeight("400"))
                .AddBar("Value", new[] { new ChartPoint(1, 1) });
            chart.Options.YAxis.Visible = false;
            chart.Options.TickLabelStyle.WithOpenTypeLanguage("BGR");
            var prepared = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(360, 600))));
            var document = XDocument.Parse(prepared.ToSvg());
            var selected = document.Descendants().Single(e => (string?)e.Attribute("data-cfx-role") == "axis-x-label")
                .Elements().Single(e => e.Name.LocalName == "text");
            var axis = document.Descendants().Single(e => (string?)e.Attribute("data-cfx-role") == "axis-x");
            double Number(XElement element, string attribute) => double.Parse(element.Attribute(attribute)!.Value, System.Globalization.CultureInfo.InvariantCulture);
            var left = Number(axis, "x1");
            var right = Number(axis, "x2");
            var selectedSize = Number(selected, "font-size");
            var localizedFont = Font().WithLanguage("BGR ");
            Assert.True(localizedFont.Measure(sourceLabel, selectedSize) > right - left, "The fixture must require fitting.");
            var displayedWidth = localizedFont.Measure(selected.Value, selectedSize);
            Assert.True(Number(selected, "x") >= left - 1 && Number(selected, "x") + displayedWidth <= right + 1,
                "The visible label must fit its axis strip using localized glyph advances.");
            Assert.Contains("font-language-override:'BGR '", selected.Attribute("style")!.Value);
            Assert.Equal(100d, chart.Options.TickLabelStyle.FontSize);
            Assert.Contains(prepared.Regions, region => region.Role == "axis-x-label" && region.Label == sourceLabel + " (1)");
        } finally { FontRegistry.Clear(); File.Delete(path); }
    }

    [Fact]
    public void HtmlGridHeadersPreserveLanguageAndExplicitDefaultReset() {
        var grid = new ChartGrid { Title = "бб", Subtitle = "HH" }; grid.Add(Chart.Create());
        grid.TitleStyle.WithOpenTypeLanguage("SRB"); grid.SubtitleStyle.WithOpenTypeLanguage("normal");
        var renderer = new HtmlChartGridRenderer();
        foreach (var html in new[] { renderer.RenderFragment(grid), renderer.RenderPage(grid) }) {
            var decoded = System.Net.WebUtility.HtmlDecode(html);
            Assert.Contains("font-language-override:'SRB '", decoded);
            Assert.Contains("font-language-override:normal", decoded);
        }
    }

    [Fact]
    public void SvgGridTrimmingKeepsLocalizedHeaderInsideItsAvailableWidth() {
        var path = Path.Combine(Path.GetTempPath(), "cfx-language-" + Guid.NewGuid().ToString("N") + ".ttf");
        File.WriteAllBytes(path, Bytes());
        try {
            FontRegistry.Register("CFX Language Grid", path);
            var grid = new ChartGrid { Title = "бббббб" }; grid.Add(Chart.Create().WithSize(400, 300));
            grid.TitleStyle.WithFontFamily("CFX Language Grid").WithFontSize(100).WithWeight("400").WithOpenTypeLanguage("BGR");
            var doc = XDocument.Parse(grid.ToSvg());
            var header = doc.Descendants().First(e => (string?)e.Attribute("data-cfx-role") == "frame-heading");
            var width = double.Parse(doc.Root!.Attribute("width")!.Value, System.Globalization.CultureInfo.InvariantCulture) - grid.Padding * 2;
            Assert.True(Font().WithLanguage("BGR ").Measure(header.Value, 100) <= width, "Localized header must be trimmed using shaped widths.");
        } finally { FontRegistry.Clear(); File.Delete(path); }
    }

    [Fact]
    public void RadialCenterClearanceIncludesTheSelectedLanguageForms() {
        var path = Path.Combine(Path.GetTempPath(), "cfx-language-" + Guid.NewGuid().ToString("N") + ".ttf");
        File.WriteAllBytes(path, Bytes());
        try {
            FontRegistry.Register("CFX Language Radial", path);
            var chart = Chart.Create().AddProgressRing("бб", new[] { new ChartPoint(0, 40) });
            chart.Options.ShowProgressRingCenterLabel = true;
            var style = new TextStyleOverride().WithFontFamily("CFX Language Radial").WithWeight("400").WithOpenTypeLanguage("BGR");
            var radius = ProgressRingLayout.RequestedCenterRadius(chart, chart.Series[0], 300, "бб", 100, 1, style);
            Assert.True(radius * 2 >= Font().WithLanguage("BGR ").Measure("бб", 100) + 20, "The ring must reserve localized text and horizontal padding.");
        } finally { FontRegistry.Clear(); File.Delete(path); }
    }

    [Theory]
    [InlineData("650", "SRB")]
    [InlineData("850", "SRB")]
    [InlineData("1000", "SRB")]
    [InlineData("650", "normal")]
    public void NumericRoleWeightsRemainSupportedWhenLanguageIsExplicit(string weight, string language) {
        var grid = new ChartGrid { Title = "Language", Subtitle = "Forms" }; grid.Add(Chart.Create());
        grid.TitleStyle.WithWeight(weight).WithOpenTypeLanguage(language);
        grid.SubtitleStyle.WithWeight(weight).WithOpenTypeLanguage(language);
        Assert.Contains("font-weight=\"" + weight + "\"", grid.ToSvg());
        var chart = Chart.Create().WithSize(360, 260).WithDataLabels()
            .ConfigureDataLabelStyle(s => s.WithWeight(weight).WithOpenTypeLanguage(language))
            .AddProgressRing("Language", new[] { new ChartPoint(0, 40) });
        Assert.Contains("font-weight=\"" + weight + "\"", chart.ToSvg());
        Assert.NotEmpty(chart.ToPng());
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
