using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Existing native responsive drawings and keyboard navigation retain authored run boundaries and point selection.</summary>
[Trait("BrowserSuite", "Compatibility")]
public sealed class PolarFormsResponsiveBrowserTests {
    [BrowserTheory]
    [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public async Task SourceNavigationCrossesBreaksWithoutJoiningGeometryAndSurvivesNativeResponsiveReplacement(bool dark, bool narrow) {
        if (!Enabled) return;
        var chart = PolarDisconnectedCurveTests.Create(800, dark).WithAxes().WithHeader(true).WithTitle("Directional observations");
        chart.Options.XAxis.LabelOrientation = ChartAxisLabelOrientation.Parallel;
        var html = chart.ToInteractiveHtmlPage(options => {
            options.Interaction.Enable(ChartInteractionFeatures.Export);
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
            options.Adaptive.NarrowWidth = 360; options.Adaptive.NarrowHeight = 380;
        });
        await using var session = await OpenAsync(html, narrow ? 390 : 1100, 760);
        var page = session.Page; var pageErrors = new List<string>(); page.PageError += (_, error) => pageErrors.Add(error);
        await page.WaitForFunctionAsync("drawing=>document.querySelector('.cfx-interactive-chart').dataset.cfxAdaptiveDrawing===drawing", narrow ? "narrow" : "wide");
        const string points = "[data-cfx-role=polar-point-source]";
        Assert.Equal(6, await page.Locator(points).CountAsync());
        Assert.Equal(2, await page.Locator("[data-cfx-role=polar-area]").CountAsync());
        await page.EvaluateAsync("()=>{window.polarSelections=[];document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect',event=>polarSelections.push(event.detail.target));}");
        await page.Locator(points + "[data-cfx-source-id='series-0-point-0']").FocusAsync();
        for (var index = 0; index < 3; index++) await page.Keyboard.PressAsync("ArrowRight");
        Assert.Equal("3", await page.EvaluateAsync<string>("()=>document.activeElement.dataset.cfxSourcePoint"));
        await page.Keyboard.PressAsync("Enter");
        var target = page.Locator(points + "[data-cfx-source-id='series-0-point-3']");
        Assert.Equal("true", await target.GetAttributeAsync("data-cfx-break-before"));
        Assert.Equal("true", await target.GetAttributeAsync("aria-selected"));
        Assert.Equal("3", await page.EvaluateAsync<string>("()=>polarSelections.at(-1).sourcePoint"));
        await page.Keyboard.PressAsync("End"); Assert.Equal("5", await page.EvaluateAsync<string>("()=>document.activeElement.dataset.cfxSourcePoint"));
        await page.Keyboard.PressAsync("Home"); Assert.Equal("0", await page.EvaluateAsync<string>("()=>document.activeElement.dataset.cfxSourcePoint"));
        await page.SetViewportSizeAsync(narrow ? 1100 : 390, 760);
        await page.WaitForFunctionAsync("drawing=>document.querySelector('.cfx-interactive-chart').dataset.cfxAdaptiveDrawing===drawing", narrow ? "wide" : "narrow");
        Assert.Equal("true", await page.Locator(points + "[data-cfx-source-id='series-0-point-3']").GetAttributeAsync("aria-selected"));
        Assert.Equal(6, await page.Locator(points).CountAsync()); Assert.Equal(2, await page.Locator("[data-cfx-role=polar-area]").CountAsync());
        Assert.All(await page.Locator("[data-cfx-role=polar-line]").EvaluateAllAsync<string[]>("nodes=>nodes.map(node=>node.getAttribute('d'))"), drawing => { Assert.Contains("C", drawing); Assert.EndsWith("Z", drawing); });
        var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync());
        var downloadPath = await download.PathAsync() ?? throw new InvalidOperationException("No polar SVG download."); var svg = XDocument.Load(downloadPath);
        Assert.Equal(6, svg.Descendants().Count(node => (string?)node.Attribute("data-cfx-role") == "polar-point-source"));
        Assert.Equal(2, svg.Descendants().Count(node => (string?)node.Attribute("data-cfx-role") == "polar-area"));
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture); var name = "polar-forms-responsive-" + (dark ? "dark" : "light") + "-" + (narrow ? "initial-narrow" : "initial-wide");
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(capture, name + ".png"), FullPage = true });
            await download.SaveAsAsync(Path.Combine(capture, name + ".export.svg"));
        }
        Assert.Empty(pageErrors); AssertNoConsoleErrors(session);
    }
}
