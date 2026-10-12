using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class RadarCurveMarkupHostTests {
    [Theory]
    [InlineData("circle", 0d)]
    [InlineData("polygon", .5)]
    public async Task FencesRetainTheCapturedHostCurvePolicy(string graticule, double tension) {
        var source = "radar-beta\naxis North, East, South, West\ncurve current{90,45,80,60}\nmax 100\ngraticule " + graticule;
        var options = new MermaidRadarRenderOptions { Width = 520, Height = 380, CurveTension = tension };
        var expected = new MermaidParser().ParseRadar(source).Document!.ToVisualArtifact(options);
        var parser = new MermaidVisualMarkupParser(new MermaidRenderOptions { Radar = options });
        options.CurveTension = 1;
        var fenced = parser.Parse("```mermaid\n" + source + "\n```");
        Assert.Empty(fenced.Diagnostics);
        var artifact = Assert.Single(fenced.Artifacts);
        Assert.Equal(expected.ToSvg(), artifact.ToSvg());
        Assert.Equal(expected.ToPng(), artifact.ToPng());
        var path = XDocument.Parse(artifact.ToSvg()).Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "radar-outline");
        Assert.Equal(tension > 0, path.Attribute("d")!.Value.Contains("C"));
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync(artifact.ToHtmlPage(), 520, 420);
        var observed = await session.Page.Locator("[data-cfx-role=radar-outline]").GetAttributeAsync("d");
        Assert.Equal(tension > 0, observed!.Contains("C"));
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(captures)) {
            Directory.CreateDirectory(captures);
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, "radar-host-" + graticule + "-browser.png"), FullPage = true });
            File.WriteAllBytes(Path.Combine(captures, "radar-host-" + graticule + "-native.png"), artifact.ToPng());
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
