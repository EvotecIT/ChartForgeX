using ChartForgeX.Mermaid;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ArtifactHtmlSizingTests {
    private const string Source = "%%{init: {'theme':'dark','fontFamily':'Georgia, serif'}}%%\nsequenceDiagram\nparticipant API\nparticipant Store\nAPI->>Store: Save request\nNote right of Store: Stored record\nStore-->>API: Complete";

    [Theory]
    [InlineData("source", 320)]
    [InlineData("native", 320)]
    [InlineData("prepared", 320)]
    [InlineData("decorated", 320)]
    [InlineData("trusted-svg", 320)]
    [InlineData("source", 1040)]
    public async Task SequenceLabelsKeepLogicalSizeAndCompactHostsCanScrollWithTheKeyboard(string route, int width) {
        if (!InteractiveChartBrowser.Enabled) return;
        var artifact = MermaidRenderer.Render(Source).Artifact!;
        if (route == "prepared") artifact = Assert.IsType<SequenceArtifact>(artifact.Model)
            .Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(960, 560)))).ToArtifact();
        if (route == "native") artifact = Assert.IsType<SequenceArtifact>(artifact.Model).ToVisualArtifact();
        if (route == "decorated") artifact = artifact.ToWatermarkedArtifact(VisualWatermark.FromText("REVIEW"));
        var html = route == "trusted-svg"
            ? artifact.ToWatermarkedHtmlPage(artifact.ToSvg(), VisualWatermark.FromText("REVIEW"))
            : artifact.ToHtmlPage();
        await using var session = await InteractiveChartBrowser.OpenAsync(html, width, 620);
        var host = session.Page.Locator(".chartforgex-visual-artifact");
        Assert.Equal("region", await host.GetAttributeAsync("role"));
        var text = session.Page.Locator("svg text").First;
        Assert.True(await text.EvaluateAsync<double>("element => parseFloat(getComputedStyle(element).fontSize) * element.getScreenCTM().a") >= 11.9);
        Assert.True(await session.Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
        Assert.Equal(0, await session.Page.Locator("script").CountAsync());
        if (width == 320) {
            await Capture(session.Page, "artifact-html-size-" + route + "-start-" + width);
            Assert.True(await host.EvaluateAsync<bool>("element => element.scrollWidth > element.clientWidth"));
            await host.FocusAsync();
            await session.Page.Keyboard.PressAsync("ArrowRight");
            await session.Page.WaitForFunctionAsync("() => document.querySelector('.chartforgex-visual-artifact').scrollLeft > 0");
            await session.Page.Locator("[data-cfx-role='sequence-participant']").Last.ScrollIntoViewIfNeededAsync();
            Assert.True(await session.Page.Locator("[data-cfx-role='sequence-participant']").Last.EvaluateAsync<bool>(
                "element => { const r=element.getBoundingClientRect(); const h=document.querySelector('.chartforgex-visual-artifact').getBoundingClientRect(); return r.right<=h.right+1 && r.left>=h.left-1; }"));
        } else Assert.False(await host.EvaluateAsync<bool>("element => element.scrollWidth > element.clientWidth"));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        await Capture(session.Page, "artifact-html-size-" + route + "-" + width);
        await session.Page.EmulateMediaAsync(new PageEmulateMediaOptions { Media = Media.Print });
        Assert.Equal("visible", await host.EvaluateAsync<string>("element => getComputedStyle(element).overflowX"));
        Assert.True(await session.Page.Locator("svg").EvaluateAsync<bool>("element => element.getBoundingClientRect().width <= innerWidth"));
    }

    [Theory]
    [InlineData("chart", false)]
    [InlineData("chart", true)]
    [InlineData("topology", false)]
    [InlineData("topology", true)]
    public async Task HostCanPreserveAnotherFamilyWithoutChangingItsAutomaticSizing(string family, bool trustedWatermarkedSvg) {
        if (!InteractiveChartBrowser.Enabled) return;
        var artifact = family == "topology"
            ? TopologyChart.Create().WithViewport(960, 560).WithTitle("Service path")
                .AddAutoNode("A", "Receive").AddAutoNode("B", "Store").AddEdge("link", "A", "B").ToVisualArtifact()
            : Chart.Create().WithSize(960, 560).WithTitle("Results")
                .AddBar("Counts", new[] { new ChartPoint(1, 2), new ChartPoint(2, 3) }).ToVisualArtifact();
        var defaultHtml = artifact.ToHtmlPage();
        var options = new VisualArtifactRenderOptions { HtmlSizing = VisualArtifactHtmlSizing.PreserveSize };
        var html = trustedWatermarkedSvg
            ? artifact.ToWatermarkedHtmlPage(options, artifact.ToSvg(), VisualWatermark.FromText("REVIEW"))
            : artifact.ToHtmlPage(options);
        await using var session = await InteractiveChartBrowser.OpenAsync(html, 320, 620);
        Assert.True(await session.Page.Locator(".chartforgex-visual-artifact").EvaluateAsync<bool>("element => element.scrollWidth > element.clientWidth"));
        Assert.True(await session.Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
        await Capture(session.Page, "artifact-html-preserve-" + family + "-" + trustedWatermarkedSvg + "-320");
        await session.Page.EmulateMediaAsync(new PageEmulateMediaOptions { Media = Media.Print });
        Assert.True(await session.Page.Locator("svg").EvaluateAsync<bool>("element => element.getBoundingClientRect().width <= innerWidth"));
        await session.Page.EmulateMediaAsync(new PageEmulateMediaOptions { Media = Media.Screen });
        await session.Page.SetContentAsync(defaultHtml);
        Assert.True(await session.Page.Locator("svg").EvaluateAsync<bool>("element => element.getBoundingClientRect().width <= innerWidth"));
        Assert.Equal(defaultHtml, artifact.ToHtmlPage());
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(VisualArtifactHtmlSizing.PreserveSize)]
    [InlineData(VisualArtifactHtmlSizing.FitToWidth)]
    public async Task HostSizingLeavesNestedSvgConstraintsIntact(VisualArtifactHtmlSizing sizing) {
        if (!InteractiveChartBrowser.Enabled) return;
        var artifact = Chart.Create().WithSize(960, 560).AddBar("Counts", new[] { new ChartPoint(1, 2) }).ToVisualArtifact();
        const string svg = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"960\" height=\"560\" viewBox=\"0 0 960 560\" style=\"max-width:100%\"><svg id=\"nested\" x=\"20\" y=\"20\" width=\"200\" height=\"100\" style=\"max-width:60px\"><rect width=\"200\" height=\"100\" fill=\"#2563eb\"/></svg></svg>";
        var html = artifact.ToWatermarkedHtmlPage(new VisualArtifactRenderOptions { HtmlSizing = sizing }, svg, VisualWatermark.FromText("REVIEW"));
        await using var session = await InteractiveChartBrowser.OpenAsync(html, 320, 620);
        foreach (var medium in new[] { Media.Screen, Media.Print }) {
            await session.Page.EmulateMediaAsync(new PageEmulateMediaOptions { Media = medium });
            Assert.Equal("60px", await session.Page.Locator("#nested").EvaluateAsync<string>("element => getComputedStyle(element).maxWidth"));
            var rootWidth = await session.Page.Locator(".chartforgex-visual-artifact > svg").EvaluateAsync<double>("element => element.getBoundingClientRect().width");
            Assert.Equal(sizing == VisualArtifactHtmlSizing.PreserveSize && medium == Media.Screen, rootWidth > 320);
            await Capture(session.Page, "artifact-html-nested-" + sizing + "-" + medium);
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExplicitFitToWidthOverridesSequenceDefaultsWithoutChangingItsScene(bool trustedWatermarkedSvg) {
        var artifact = MermaidRenderer.Render(Source).Artifact!;
        var svg = artifact.ToSvg();
        var png = artifact.ToPng();
        var options = new VisualArtifactRenderOptions { HtmlSizing = VisualArtifactHtmlSizing.FitToWidth };
        var mark = VisualWatermark.FromText("REVIEW");
        var html = trustedWatermarkedSvg ? artifact.ToWatermarkedHtmlPage(options, svg, mark) : artifact.ToHtmlPage(options);
        Assert.Equal(svg, artifact.ToSvg(options));
        Assert.Equal(png, artifact.ToPng(options));
        Assert.Contains(trustedWatermarkedSvg ? artifact.ToWatermarkedSvg(svg, mark) : svg, html);
        if (trustedWatermarkedSvg) Assert.Equal(artifact.ToWatermarkedHtmlPage(svg, mark), artifact.ToWatermarkedHtmlPage(null, svg, mark));
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync(html, 320, 620);
        Assert.True(await session.Page.Locator("svg text").First.EvaluateAsync<double>(
            "element => parseFloat(getComputedStyle(element).fontSize) * element.getScreenCTM().a") < 6);
        Assert.False(await session.Page.Locator(".chartforgex-visual-artifact").EvaluateAsync<bool>("element => element.scrollWidth > element.clientWidth"));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        await Capture(session.Page, "artifact-html-size-fit-" + trustedWatermarkedSvg + "-320");
    }

    [Fact]
    public void InvalidHostSizingIsRejectedAtThePublicBoundary() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new VisualArtifactRenderOptions { HtmlSizing = (VisualArtifactHtmlSizing)99 });

    private static async Task Capture(IPage page, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + ".png"), FullPage = true });
    }
}
