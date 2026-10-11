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

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task MaximumSourceSizeRetainsFixedWardleyAndFishboneRoles(bool graphite, bool fishbone) {
        var body = fishbone ? "ishikawa-beta\nAPI\n  People\n    Data\n  Process\n    Review"
            : "wardley-beta\nanchor User [0.9, 0.15]\ncomponent API [0.55, 0.4]\nUser +'uses'> API\nnote \"Data\" [0.25, 0.6]\naccelerator Faster [0.45, 0.65]\nannotation 1, [0.5, 0.5] \"Review\"\npipeline API {\n  component Build [0.3]\n}";
        var parsed = MermaidRenderer.Render("%%{init: {'fontFamily':'Arial','themeVariables':{'fontSize':256}}}%%\n" + body);
        Assert.Empty(parsed.Diagnostics);
        var block = Assert.IsAssignableFrom<IVisualBlock>(parsed.Artifact!.Model);
        block.Options.Theme.UseGraphiteLayout = graphite;
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
        var name = (fishbone ? "fishbone" : "wardley") + "-font-ceiling-" + graphite;
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + "-browser.png") });
        File.WriteAllBytes(Path.Combine(captures, name + "-native.png"), png);
    }

    [Fact]
    public void SingleLineHeadingNormalizationDoesNotConsumeExtraBodySpace() {
        var theme = ChartForgeX.Themes.ChartTheme.Light(); theme.TitleFontSize = 42; theme.SubtitleFontSize = 27;
        var first = BlockLayoutBlock.Create().WithTitle("First Second").WithSubtitle("Third Fourth").WithTheme(theme).AddItem("a", "API");
        var second = BlockLayoutBlock.Create().WithTitle("First\nSecond").WithSubtitle("Third\r\nFourth").WithTheme(theme).AddItem("a", "API");
        static string Positions(string svg) => string.Join("|", XDocument.Parse(svg).Descendants()
            .Where(e => e.Name.LocalName == "text" || (string?)e.Attribute("data-cfx-role") == "block-layout-node")
            .Select(e => e.ToString()));
        Assert.Equal(Positions(first.ToSvg()), Positions(second.ToSvg()));
        Assert.Equal(first.ToPng(), second.ToPng());
    }
}
