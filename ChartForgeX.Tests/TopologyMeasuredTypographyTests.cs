using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Topology;
using ChartForgeX.Raster;
using ChartForgeX.Terminal;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyMeasuredTypographyTests {
    [Fact]
    public void PortableLayoutDoesNotDependOnTheRequestedHostFont() {
        const string text = "Wide WWW and narrow iii";
        double sans = new TextMeasurementContext("Arial, sans-serif").Measure(text, 14, true);
        double mono = new TextMeasurementContext("monospace").Measure(text, 14, true);
        double missing = new TextMeasurementContext("not-an-installed-font").Measure(text, 14, true);
        Assert.Equal(sans, mono);
        Assert.Equal(sans, missing);
        var options = new TopologyRenderOptions();
        Assert.Equal(TextMeasurementMode.PortableEstimate, options.TextMeasurementMode);
        options.TextMeasurementMode = TextMeasurementMode.InstalledFonts;
        Assert.Equal(options.TextMeasurementMode, options.Clone().TextMeasurementMode);
    }

    [Fact]
    public void InvalidMeasurementModeFailsBeforeRendering() {
        var chart = TopologyChart.Create().AddAutoNode("a", "A");
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.ToSvg(new TopologyRenderOptions { TextMeasurementMode = (TextMeasurementMode)100 }));
    }

    [Fact]
    public void InstalledArialUsesRequestedFaceAndReservesNativeBoldAdvances() {
        var regularPath = new[] { "/System/Library/Fonts/Supplemental/Arial.ttf",
            System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts", "arial.ttf") }
            .FirstOrDefault(System.IO.File.Exists);
        if (regularPath == null) return; // The portable fallback remains covered on hosts without Arial.
        var theme = TopologyTheme.Light(); theme.FontFamily = "Arial, sans-serif";
        Assert.Equal("Arial", TopologyChart.Create().WithTheme(theme).GetPngFontInfo().ResolvedFaceName);
        var boldPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(regularPath)!,
            regularPath.StartsWith("/System/", StringComparison.Ordinal) ? "Arial Bold.ttf" : "arialbd.ttf");
        var bold = TrueTypeFont.TryLoadFromPath(boldPath);
        if (bold == null) return;
        const string text = "Measured labels across cards, compact cards, and tiles";
        var nativeWidth = TextLayoutEngine.MeasureWidth(text, new TextStyle { FontSize = 34 }, bold);
        Assert.True(new TextMeasurementContext(theme.FontFamily, TextMeasurementMode.InstalledFonts).Measure(text, 34, true) >= nativeWidth);
    }

    [Fact]
    public void ArialWithMonospaceFallbackKeepsTerminalTablesMonospaced() {
        var theme = TerminalTheme.Dark(); theme.FontFamily = "Arial, monospace";
        Assert.False(TrueTypeFont.IsMonospaceFamily(theme.FontFamily));
        var outline = TrueTypeFont.TryLoadForFamily(theme.FontFamily, out _);
        var table = PngTerminalStoryRenderer.ResolveTableFont(theme, outline);
        if (table == null) return;
        var style = new TextStyle { FontSize = 14 };
        Assert.Equal(TextLayoutEngine.MeasureWidth("iiii", style, table),
            TextLayoutEngine.MeasureWidth("WWWW", style, table), 5);
    }

    [Fact]
    public void FontDiagnosticsReflectCurrentTheme() {
        var theme = TopologyTheme.Light(); theme.FontFamily = "monospace";
        var chart = TopologyChart.Create().WithTheme(theme);
        Assert.Equal("monospace", chart.GetPngFontInfo().ThemeFontFamily);
        theme.FontFamily = "serif";
        Assert.Equal("serif", chart.GetPngFontInfo().ThemeFontFamily);
    }

    [Theory]
    [InlineData(TopologyNodeDisplayMode.Card)]
    [InlineData(TopologyNodeDisplayMode.CompactCard)]
    [InlineData(TopologyNodeDisplayMode.Tile)]
    public void SubtitleChipsContainTheirMeasuredText(TopologyNodeDisplayMode mode) {
        var theme = TopologyTheme.Light(); theme.FontFamily = "monospace";
        var chart = TopologyChart.Create().WithTheme(theme).AddAutoNode("a", "Service", subtitle: "WWWWWWWWWWWWWWWW");
        var options = new TopologyRenderOptions { TextMeasurementMode = TextMeasurementMode.InstalledFonts, NodeDisplayMode = mode, CardSubtitleMode = TopologyCardSubtitleMode.Chip, IncludeTileSubtitles = true };
        var svg = XDocument.Parse(chart.ToSvg(options));
        var chip = svg.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == (mode == TopologyNodeDisplayMode.Tile ? "topology-node-subtitle" : "topology-node-card-subtitle"));
        var text = chip.Elements().Single(element => element.Name.LocalName == "text").Value;
        double width = double.Parse(chip.Elements().Single(element => element.Name.LocalName == "rect").Attribute("width")!.Value, CultureInfo.InvariantCulture);
        Assert.True(new TextMeasurementContext(theme.FontFamily, TextMeasurementMode.InstalledFonts).Measure(text, 9.5, true) <= width - 17.9);
        Assert.NotEmpty(chart.ToPng(options));
    }

    [Theory]
    [InlineData("WWWWWWWWWWWWWWWWWWWWWWWW", 70)]
    [InlineData("Wide", 0)]
    [InlineData("Wide", 2)]
    [InlineData("iiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii", 70)]
    [InlineData("A\u0301A\u0301A\u0301A\u0301A\u0301A\u0301A\u0301A\u0301", 40)]
    [InlineData("😀😀😀😀😀😀😀😀😀😀", 40)]
    public void WidthTrimmingFitsAndRetainsTextElementBoundaries(string input, double width) {
        var style = new TextStyle { Font = new FontSpec { Family = "monospace", Weight = 700 }, FontSize = 12 };
        var context = new TextMeasurementContext(style.Font.Family, TextMeasurementMode.InstalledFonts);
        string trimmed = TopologyRenderPrimitives.TrimToEstimatedWidth(input, width, 12, true, context);
        Assert.True(TextLayoutEngine.Measure(trimmed, style).Width <= width);
        if (trimmed.EndsWith("...", StringComparison.Ordinal)) {
            string prefix = trimmed.Substring(0, trimmed.Length - 3);
            Assert.Contains(prefix.Length, StringInfo.ParseCombiningCharacters(input).Append(input.Length));
        }
    }

    [Fact]
    public void CharacterLimitDoesNotSplitCombiningSequences() {
        Assert.Equal("A\u0301B\u0301...", TopologyRenderPrimitives.TrimTo("A\u0301B\u0301C\u0301D\u0301E\u0301F\u0301", 5));
    }

    [Fact]
    public void EmittedEdgeLabelBoxFitsMeasuredTextInTheThemeFont() {
        var theme = TopologyTheme.Light();
        theme.FontFamily = "monospace";
        const string label = "WWWWWWWWWWWWWWWWWWWWWWWW";
        var chart = TopologyChart.Create().WithTheme(theme)
            .AddNode("a", "A", 80, 100).AddNode("b", "B", 500, 300).AddEdge("ab", "a", "b", label);
        var svg = XDocument.Parse(chart.ToSvg(new TopologyRenderOptions { TextMeasurementMode = TextMeasurementMode.InstalledFonts }));
        var box = svg.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "topology-edge-label");
        double width = double.Parse(box.Attribute("data-label-width")!.Value, CultureInfo.InvariantCulture);
        double glyphWidth = TextLayoutEngine.Measure(label, new TextStyle {
            Font = new FontSpec { Family = theme.FontFamily, Weight = 700 }, FontSize = 12
        }).Width;
        Assert.True(width >= glyphWidth + 17.9, "The rendered backplate must reserve the measured glyph width and its padding.");
    }
}
