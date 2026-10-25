using System.Xml.Linq;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class CartesianInteriorAxesBrowserTests {
    [BrowserTheory]
    [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public async Task CompleteInteriorAxesReprepareTogetherDuringAdaptiveReplacementAndDetachedExport(bool dark, bool narrow) {
        if (!Enabled) return;
        var chart = QuadrantRegionDepthRecipe.Create("interior-quadrants", dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        var html = chart.ToInteractiveHtmlPage(options => { options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
            options.Interaction.Enable(ChartForgeX.Interactivity.ChartInteractionFeatures.Export);
            options.Adaptive.NarrowWidth = 360; options.Adaptive.NarrowHeight = 440; });
        await using var session = await OpenAsync(html, narrow ? 390 : 1100, 760);
        var page = session.Page; var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        await page.WaitForFunctionAsync("drawing=>document.querySelector('.cfx-interactive-chart').dataset.cfxAdaptiveDrawing===drawing", narrow ? "narrow" : "wide");
        Assert.Equal(2, await page.Locator(".cfx-stage [data-cfx-role=axis-crossing]").CountAsync());
        Assert.True(await page.Locator(".cfx-stage [data-cfx-role=axis-x-label]").CountAsync() > 0);
        Assert.True(await page.Locator(".cfx-stage [data-cfx-role=axis-y-label]").CountAsync() > 0);
        await page.Locator(".cfx-stage [data-cfx-role=point]").First.FocusAsync(); await page.Keyboard.PressAsync("End"); await page.Keyboard.PressAsync("Enter");
        await page.SetViewportSizeAsync(narrow ? 1100 : 390, 760);
        await page.WaitForFunctionAsync("drawing=>document.querySelector('.cfx-interactive-chart').dataset.cfxAdaptiveDrawing===drawing", narrow ? "wide" : "narrow");
        Assert.Equal("true", await page.Locator(".cfx-stage [data-cfx-role=point][data-cfx-source-point='5']").GetAttributeAsync("aria-selected"));
        Assert.Equal(2, await page.Locator(".cfx-stage [data-cfx-role=axis-crossing]").CountAsync());
        var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync());
        var path = await download.PathAsync() ?? throw new InvalidOperationException("No interior-axis SVG export."); var document = XDocument.Load(path);
        Assert.Equal(2, document.Descendants().Count(node => (string?)node.Attribute("data-cfx-role") == "axis-crossing"));
        Assert.Contains(document.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "axis-x-label");
        Assert.Contains(document.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "axis-y-label");
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) { Directory.CreateDirectory(capture); var name = "quadrant-whole-axis-" + dark + "-initial-narrow-" + narrow;
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(capture, name + ".png"), FullPage = true });
            await download.SaveAsAsync(Path.Combine(capture, name + ".export.svg")); }
        Assert.Empty(errors); AssertNoConsoleErrors(session);
    }
}
