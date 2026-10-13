using ChartForgeX.Themes;
using System.Xml.Linq;
using System.Globalization;
using ChartForgeX.Mermaid;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidFixedBlockTypographyTests {
    [Fact]
    public void GitSourceSizeRemainsEffectiveAtBitmapScaleBoundaries() {
        static GitGraphBlock Create(double size) {
            var theme = VisualTheme.Report().WithFontFamily("Arial"); theme = theme.Configure(builder => { builder.Typography = new VisualTypography(family: builder.Typography.Family, titleSize: builder.Typography.TitleSize, subtitleSize: builder.Typography.SubtitleSize, axisSize: builder.Typography.AxisSize, legendSize: builder.Typography.LegendSize, dataLabelSize: size, axisTitleSize: builder.Typography.AxisTitleSize, scalarValueSize: builder.Typography.ScalarValueSize, centerValueSize: builder.Typography.CenterValueSize, monospaceFamily: builder.Typography.MonospaceFamily); });
            return GitGraphBlock.Create().WithSize(900, 500).WithTheme(theme).WithCard(false).AddBranch("main").AddCommit("base", "main", tag: "v1");
        }
        var small = Create(10.5); var large = Create(21);
        var first = XDocument.Parse(small.ToSvg()).Descendants().Single(e => e.Name.LocalName == "text" && e.Value == "main");
        var second = XDocument.Parse(large.ToSvg()).Descendants().Single(e => e.Name.LocalName == "text" && e.Value == "main");
        var original = double.Parse((string)first.Attribute("font-size")!, CultureInfo.InvariantCulture);
        var enlarged = double.Parse((string)second.Attribute("font-size")!, CultureInfo.InvariantCulture);
        CaptureBitmapCandidate(small, "git-10.5"); CaptureBitmapCandidate(large, "git-21");
        Assert.True(enlarged > original * 1.5, "An enlarged caption that fits its lane must retain the requested typography change.");
        Assert.InRange(enlarged, original, original * 2 + .001);
        Assert.False(small.ToPng().SequenceEqual(large.ToPng()));
    }

    [Fact]
    public void PacketBitSizeRemainsReadableAtBitmapScaleBoundaries() {
        static PacketLayoutBlock Create(double size) {
            var theme = VisualTheme.Report().WithFontFamily("Arial"); theme = theme.Configure(builder => { builder.Typography = new VisualTypography(family: builder.Typography.Family, titleSize: builder.Typography.TitleSize, subtitleSize: builder.Typography.SubtitleSize, axisSize: builder.Typography.AxisSize, legendSize: builder.Typography.LegendSize, dataLabelSize: size, axisTitleSize: builder.Typography.AxisTitleSize, scalarValueSize: builder.Typography.ScalarValueSize, centerValueSize: builder.Typography.CenterValueSize, monospaceFamily: builder.Typography.MonospaceFamily); });
            return PacketLayoutBlock.Create().WithSize(900, 500).WithTheme(theme).WithCard(false).WithBitsPerRow(32)
                .AddField(0, 0, "Flag").AddField(1, 31, "Payload");
        }
        var small = Create(10.5); var large = Create(21);
        static double BitSize(PacketLayoutBlock block) => double.Parse((string)XDocument.Parse(block.ToSvg()).Descendants()
            .First(e => (string?)e.Attribute("data-cfx-role") == "packet-bit-start").Attribute("font-size")!, CultureInfo.InvariantCulture);
        CaptureBitmapCandidate(small, "packet-10.5"); CaptureBitmapCandidate(large, "packet-21");
        Assert.InRange(BitSize(small), 9.499, 9.501);
        Assert.Equal(BitSize(small) * 2, BitSize(large), 3);
        Assert.False(small.ToPng().SequenceEqual(large.ToPng()));
    }

    [Fact]
    public void DensePacketKeepsFieldRangesWithoutOverlappingAtBitmapScaleBoundaries() {
        var theme = VisualTheme.Report().WithFontFamily("Arial");
        var block = PacketLayoutBlock.Create().WithSize(320, 240).WithTheme(theme).WithCard(false).WithBitsPerRow(512)
            .AddField(0, 0, "Flag").AddField(1, 511, "Payload");
        var svg = XDocument.Parse(block.ToSvg());
        var fields = svg.Descendants().Where(e => (string?)e.Attribute("data-cfx-role") == "packet-field").ToArray();
        Assert.Equal(new[] { "0", "1" }, fields.Select(e => (string?)e.Attribute("data-bit-start")));
        Assert.Equal(new[] { "0", "511" }, fields.Select(e => (string?)e.Attribute("data-bit-end")));
        var numbers = svg.Descendants().Where(e => ((string?)e.Attribute("data-cfx-role"))?.StartsWith("packet-bit-", StringComparison.Ordinal) == true).ToArray();
        if (TypographyFontResolver.ResolveFace(new FontSpec { Family = theme.Typography.Family }).Font == null) Assert.Empty(numbers);
        else if (numbers.Length > 0) {
            var number = numbers.First(e => e.Value == "0");
            var style = new TextStyle { Font = new FontSpec { Family = theme.Typography.Family, Weight = 700 },
                FontSize = double.Parse((string)number.Attribute("font-size")!, CultureInfo.InvariantCulture), LineHeight = 1 };
            var fieldWidth = double.Parse((string)fields[0].Attribute("width")!, CultureInfo.InvariantCulture);
            Assert.InRange(TextLayoutEngine.Measure(number.Value, style).Width, 0, fieldWidth);
        }
        CaptureBitmapCandidate(block, "packet-dense");
        Assert.NotEmpty(block.ToPng());
    }

    private static void CaptureBitmapCandidate(IVisualBlock block, string name) {
        if (Environment.GetEnvironmentVariable("CFX_TEST_REQUIRE_BITMAP_FONT") == "1")
            Assert.Null(TypographyFontResolver.ResolveFace(new FontSpec { Family = block.Options.Theme.Typography.Family }).Font);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures);
        File.WriteAllBytes(Path.Combine(captures, name + "-native.png"), block.ToPng());
    }

    [Theory]
    [InlineData(21)]
    [InlineData(256)]
    public void FittedFishboneCaptionRespectsTheNativeAdvanceAtBitmapScaleBoundaries(int size) {
        var rendered = MermaidRenderer.Render("%%{init: {'fontFamily':'Arial','themeVariables':{'fontSize':" + size + "}}}%%\nishikawa-beta\nAPI\n  Management\n    Data");
        Assert.Empty(rendered.Diagnostics);
        var caption = XDocument.Parse(rendered.Artifact!.ToSvg()).Descendants()
            .Single(e => e.Name.LocalName == "text" && e.Value == "Management");
        var style = new TextStyle {
            Font = new FontSpec { Family = (string)caption.Attribute("font-family")!, Weight = int.Parse((string)caption.Attribute("font-weight")!, CultureInfo.InvariantCulture) },
            FontSize = double.Parse((string)caption.Attribute("font-size")!, CultureInfo.InvariantCulture), LineHeight = 1
        };
        if (Environment.GetEnvironmentVariable("CFX_TEST_REQUIRE_BITMAP_FONT") == "1")
            Assert.Null(TypographyFontResolver.ResolveFace(style.Font).Font);
        var actual = TextLayoutEngine.Measure(caption.Value, style);
        Assert.InRange(actual.Width, 1, 108);
        Assert.InRange(actual.Height, 1, 24);
        var png = rendered.Artifact!.ToPng();
        Assert.NotEmpty(png);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures);
        File.WriteAllBytes(Path.Combine(captures, "fishbone-management-" + size + "-native.png"), png);
    }

    [Trait("Category", "Browser")]
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task MaximumSourceSizeRetainsFixedWardleyAndFishboneRoles(bool dark, bool fishbone) {
        var body = fishbone ? "ishikawa-beta\nAPI\n  People\n    Data\n  Process\n    Review"
            : "wardley-beta\nanchor User [0.9, 0.15]\ncomponent API [0.55, 0.4]\nUser +'uses'> API\nnote \"Data\" [0.25, 0.6]\naccelerator Faster [0.45, 0.65]\nannotation 1, [0.5, 0.5] \"Review\"\npipeline API {\n  component Build [0.3]\n}";
        var parsed = MermaidRenderer.Render("%%{init: {'fontFamily':'Arial','themeVariables':{'fontSize':256}}}%%\n" + body);
        Assert.Empty(parsed.Diagnostics);
        var block = Assert.IsAssignableFrom<IVisualBlock>(parsed.Artifact!.Model);
        block.Options.ThemeMode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light;
        var svg = parsed.Artifact.ToSvg(); var png = parsed.Artifact.ToPng();
        var labels = XDocument.Parse(svg).Descendants().Where(e => e.Name.LocalName == "text").Select(e => e.Value).ToArray();
        foreach (var value in fishbone ? new[] { "API", "People", "Data", "Process", "Review" }
            : new[] { "API", "User", "uses", "Data", "Faster", "Review", "Build", "Genesis", "Evolution", "Visibility" }) Assert.Contains(value, labels);
        Assert.NotEmpty(png);
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + svg + "</body></html>", block.Options.Size.Width, block.Options.Size.Height);
        Assert.True(await session.Page.EvaluateAsync<bool>("() => [...document.querySelectorAll('svg text')].every(t=>{const b=t.getBoundingClientRect();return b.width>0 && b.left>=-.1 && b.top>=-.1 && b.right<=innerWidth+.1 && b.bottom<=innerHeight+.1;})"));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures);
        var name = (fishbone ? "fishbone" : "wardley") + "-font-ceiling-" + dark;
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + "-browser.png") });
        File.WriteAllBytes(Path.Combine(captures, name + "-native.png"), png);
    }

    [Fact]
    public void SingleLineHeadingNormalizationDoesNotConsumeExtraBodySpace() {
        var theme = VisualTheme.Report(); theme = theme.Configure(builder => { builder.Typography = new VisualTypography(family: builder.Typography.Family, titleSize: 42, subtitleSize: builder.Typography.SubtitleSize, axisSize: builder.Typography.AxisSize, legendSize: builder.Typography.LegendSize, dataLabelSize: builder.Typography.DataLabelSize, axisTitleSize: builder.Typography.AxisTitleSize, scalarValueSize: builder.Typography.ScalarValueSize, centerValueSize: builder.Typography.CenterValueSize, monospaceFamily: builder.Typography.MonospaceFamily); }); theme = theme.Configure(builder => { builder.Typography = new VisualTypography(family: builder.Typography.Family, titleSize: builder.Typography.TitleSize, subtitleSize: 27, axisSize: builder.Typography.AxisSize, legendSize: builder.Typography.LegendSize, dataLabelSize: builder.Typography.DataLabelSize, axisTitleSize: builder.Typography.AxisTitleSize, scalarValueSize: builder.Typography.ScalarValueSize, centerValueSize: builder.Typography.CenterValueSize, monospaceFamily: builder.Typography.MonospaceFamily); });
        var first = BlockLayoutBlock.Create().WithTitle("First Second").WithSubtitle("Third Fourth").WithTheme(theme).AddItem("a", "API");
        var second = BlockLayoutBlock.Create().WithTitle("First\nSecond").WithSubtitle("Third\r\nFourth").WithTheme(theme).AddItem("a", "API");
        static string Positions(string svg) => string.Join("|", XDocument.Parse(svg).Descendants()
            .Where(e => e.Name.LocalName == "text" || (string?)e.Attribute("data-cfx-role") == "block-layout-node")
            .Select(e => e.ToString()));
        Assert.Equal(Positions(first.ToSvg()), Positions(second.ToSvg()));
        Assert.Equal(first.ToPng(), second.ToPng());
    }
}
