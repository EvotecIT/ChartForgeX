using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Authored region paint remains passive while the ordinary source observations keep keyboard and export behavior.</summary>
[Trait("BrowserSuite", "Compatibility")]
public sealed class CartesianPlotRegionDepthBrowserTests {
    [BrowserTheory]
    [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public async Task BorderedPivotRegionsRetainDeliberateCaptionsSourceNavigationAndNativeResponsiveReplacement(bool dark, bool narrow) {
        if (!Enabled) return;
        var chart = QuadrantRegionDepthRecipe.Create("pivot-quadrants", dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        var html = chart.ToInteractiveHtmlPage(options => {
            options.Interaction.Enable(ChartForgeX.Interactivity.ChartInteractionFeatures.Export);
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
            options.Adaptive.NarrowWidth = 360; options.Adaptive.NarrowHeight = 440;
        });
        await using var session = await OpenAsync(html, narrow ? 390 : 1100, 760);
        var page = session.Page; var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        await page.WaitForFunctionAsync("drawing=>document.querySelector('.cfx-interactive-chart').dataset.cfxAdaptiveDrawing===drawing", narrow ? "narrow" : "wide");
        Assert.Equal(4, await page.Locator(".cfx-stage [data-cfx-role=plot-region-border]").CountAsync());
        Assert.Equal(4, await page.Locator(".cfx-stage [data-cfx-role=plot-region-label]").CountAsync());
        Assert.Equal(0, await page.Locator(".cfx-stage [data-cfx-passive=true] [tabindex]").CountAsync());
        Assert.True(await page.EvaluateAsync<bool>("""
            () => {
              const stage=document.querySelector('.cfx-stage'), region=stage.querySelector('[data-cfx-source-id="plot-region-0"]');
              const fill=region.querySelector('[data-cfx-role="plot-region-fill"]').getBoundingClientRect();
              const caption=stage.querySelector('[data-cfx-source-id="plot-region-0-caption"] [data-cfx-role="plot-region-label-backplate"]').getBoundingClientRect();
              return caption.left>=fill.left && caption.top>=fill.top && caption.right<=fill.right && caption.bottom<=fill.bottom;
            }
            """));
        const string points = ".cfx-stage [data-cfx-role=point]";
        var first = page.Locator(points).First; await first.FocusAsync(); await page.Keyboard.PressAsync("End");
        Assert.Equal("5", await page.EvaluateAsync<string>("()=>document.activeElement.dataset.cfxSourcePoint"));
        await page.Keyboard.PressAsync("Enter");
        await page.SetViewportSizeAsync(narrow ? 1100 : 390, 760);
        await page.WaitForFunctionAsync("drawing=>document.querySelector('.cfx-interactive-chart').dataset.cfxAdaptiveDrawing===drawing", narrow ? "wide" : "narrow");
        Assert.Equal(4, await page.Locator(".cfx-stage [data-cfx-role=plot-region-border]").CountAsync());
        Assert.Equal(6, await page.Locator(points).CountAsync());
        Assert.Equal("true", await page.Locator(points + "[data-cfx-source-point='5']").GetAttributeAsync("aria-selected"));
        var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync());
        var path = await download.PathAsync() ?? throw new InvalidOperationException("No quadrant SVG download.");
        var document = XDocument.Load(path);
        Assert.Equal(4, document.Descendants().Count(node => (string?)node.Attribute("data-cfx-role") == "plot-region-border"));
        Assert.Equal(2, document.Descendants().Count(node => (string?)node.Attribute("data-cfx-role") == "axis-rule-crossing"));
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture); var name = "quadrant-region-depth-" + dark + "-initial-narrow-" + narrow;
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(capture, name + ".png"), FullPage = true });
            await download.SaveAsAsync(Path.Combine(capture, name + ".export.svg"));
        }
        Assert.Empty(errors); AssertNoConsoleErrors(session);
    }
}
