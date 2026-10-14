using System.Text.Json;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Checks standalone page framing and the native chart's print presentation in Chromium.</summary>
public sealed class InteractiveHtmlPagePolishBrowserTests {
    [BrowserTheory]
    [InlineData(false, false, 380, HtmlChartResponsiveLayout.Fit)]
    [InlineData(false, false, 980, HtmlChartResponsiveLayout.Readable)]
    [InlineData(false, true, 380, HtmlChartResponsiveLayout.Readable)]
    [InlineData(false, true, 980, HtmlChartResponsiveLayout.Fit)]
    [InlineData(true, false, 380, HtmlChartResponsiveLayout.Fit)]
    [InlineData(true, false, 980, HtmlChartResponsiveLayout.Readable)]
    [InlineData(true, true, 380, HtmlChartResponsiveLayout.Readable)]
    [InlineData(true, true, 980, HtmlChartResponsiveLayout.Fit)]
    public async Task StandalonePagesPrintTheCompleteNativeChartAndRestoreTheScreenView(
        bool externalAssets, bool dark, int width, HtmlChartResponsiveLayout layout) {
        if (!Enabled) return;
        var chart = StatusLines(dark);
        var html = chart.ToInteractiveHtmlPage(options => {
            options.ResponsiveLayout = layout;
            options.Interaction.Enable(ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan);
            if (externalAssets) options.ExternalAssets = new HtmlAssetReferences("https://assets.example.test/");
        });
        await using var session = await OpenAsync("<!doctype html><html><body></body></html>", width, 620);
        var page = session.Page;
        var errors = new List<string>();
        page.PageError += (_, error) => errors.Add(error);
        if (externalAssets) await page.RouteAsync("https://assets.example.test/**", async route => {
            var asset = HtmlInteractiveAssetFiles.Charts().Single(file => route.Request.Url.EndsWith(file.FileName, StringComparison.Ordinal));
            await route.FulfillAsync(new RouteFulfillOptions { ContentType = asset.ContentType, Body = asset.Content });
        });
        await page.SetContentAsync(html);
        await page.Locator("[data-cfx-zoom=in]").ClickAsync();
        await page.Locator(Point(0, 0)).FocusAsync();
        await page.WaitForFunctionAsync("() => {const root=document.querySelector('.cfx-interactive-chart'),zoom=Number(root.dataset.cfxZoom);return zoom>1 && Math.abs(new DOMMatrix(getComputedStyle(root.querySelector('.cfx-stage svg')).transform).a-zoom)<.001;}");
        var name = (externalAssets ? "external" : "inline") + "-" + (dark ? "dark" : "light") + "-" + width + "-" + layout;
        var screen = await ReadStateAsync(page);
        await CaptureAsync(page, name + "-screen", screen);
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { Media = Media.Print });
        var printed = await ReadStateAsync(page);
        await CaptureAsync(page, name + "-print", printed);
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { Media = Media.Screen });
        await page.WaitForFunctionAsync("() => {const root=document.querySelector('.cfx-interactive-chart');return Math.abs(new DOMMatrix(getComputedStyle(root.querySelector('.cfx-stage svg')).transform).a-Number(root.dataset.cfxZoom))<.001;}");
        var restored = await ReadStateAsync(page);
        Assert.Empty(errors);
        AssertNoConsoleErrors(session);

        Assert.Equal("antialiased", screen.GetProperty("smoothing").GetString());
        Assert.Equal("geometricprecision", screen.GetProperty("textRendering").GetString()!.ToLowerInvariant());
        Assert.True(screen.GetProperty("toolbarVisible").GetBoolean());
        Assert.True(screen.GetProperty("tooltipVisible").GetBoolean());
        Assert.True(screen.GetProperty("zoom").GetDouble() > 1);
        Assert.InRange(screen.GetProperty("documentOverflow").GetDouble(), 0, 1);
        Assert.Equal("rgba(0, 0, 0, 0)", printed.GetProperty("bodyBackground").GetString());
        Assert.Equal("rgba(0, 0, 0, 0)", printed.GetProperty("shellBackground").GetString());
        Assert.False(printed.GetProperty("toolbarVisible").GetBoolean());
        Assert.False(printed.GetProperty("tooltipVisible").GetBoolean());
        Assert.False(printed.GetProperty("resetVisible").GetBoolean());
        Assert.Equal("none", printed.GetProperty("transform").GetString());
        Assert.InRange(printed.GetProperty("stageOverflow").GetDouble(), 0, 1);
        Assert.Equal(printed.GetProperty("stageWidth").GetDouble(), printed.GetProperty("renderedWidth").GetDouble(), 1);
        Assert.InRange(printed.GetProperty("documentOverflow").GetDouble(), 0, 1);
        Assert.Equal(screen.GetProperty("viewBox").GetString(), printed.GetProperty("viewBox").GetString());
        Assert.Equal(printed.GetProperty("nativeAspect").GetDouble(), printed.GetProperty("renderedAspect").GetDouble(), 3);
        Assert.Equal(screen.GetProperty("zoom").GetDouble(), restored.GetProperty("zoom").GetDouble());
        Assert.Equal(screen.GetProperty("zoom").GetDouble(), restored.GetProperty("scale").GetDouble(), 3);
        Assert.True(restored.GetProperty("toolbarVisible").GetBoolean());
    }

    private static Task<JsonElement> ReadStateAsync(IPage page) => page.EvaluateAsync<JsonElement>("""
        () => {
            const root=document.querySelector('.cfx-interactive-chart'), stage=root.querySelector('.cfx-stage'), svg=stage.querySelector('svg');
            const visible=selector=>{const node=document.querySelector(selector);return !!node && !node.hidden && getComputedStyle(node).display!=='none';};
            const bounds=svg.getBoundingClientRect(), box=svg.viewBox.baseVal;
            return {
                smoothing:getComputedStyle(document.body).webkitFontSmoothing,textRendering:getComputedStyle(document.body).textRendering,
                bodyBackground:getComputedStyle(document.body).backgroundColor,shellBackground:getComputedStyle(document.querySelector('.cfx-shell')).backgroundColor,
                toolbarVisible:visible('.cfx-toolbar'),tooltipVisible:visible('.cfx-tooltip'),resetVisible:visible('.cfx-reset'),
                zoom:Number(root.dataset.cfxZoom),transform:getComputedStyle(svg).transform,scale:new DOMMatrix(getComputedStyle(svg).transform).a,viewBox:svg.getAttribute('viewBox'),
                stageOverflow:Math.max(0,stage.scrollWidth-stage.clientWidth),documentOverflow:Math.max(0,document.documentElement.scrollWidth-innerWidth),
                nativeAspect:box.width/box.height,renderedAspect:bounds.width/bounds.height,renderedWidth:bounds.width,stageWidth:stage.getBoundingClientRect().width
            };
        }
        """);

    private static async Task CaptureAsync(IPage page, string name, JsonElement evidence) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        if (!Path.IsPathRooted(directory)) throw new InvalidOperationException("Capture output must be an absolute path.");
        Directory.CreateDirectory(directory);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, "page-polish-" + name + ".png"), FullPage = true });
        await File.WriteAllTextAsync(Path.Combine(directory, "page-polish-" + name + ".json"), evidence.GetRawText());
    }
}
