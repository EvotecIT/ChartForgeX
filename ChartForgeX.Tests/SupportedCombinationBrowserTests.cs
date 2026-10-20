using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Exercises composition, identity and browser state together instead of inferring composition support from isolated families.</summary>
public sealed class SupportedCombinationBrowserTests {
    private const string Root = ".cfx-interactive-chart";
    private const string Selected = "[data-cfx-series-key=reference][data-cfx-observation-id=north-central]:not([data-cfx-label-for])";
    private const string Legend = "[data-cfx-role=legend-item][data-cfx-series-key=observed]";

    [BrowserTheory]
    [InlineData("vertical-secondary", false, 980)]
    [InlineData("vertical-secondary", true, 390)]
    [InlineData("horizontal", false, 980)]
    [InlineData("horizontal", true, 390)]
    [InlineData("mixed-radial", false, 980)]
    [InlineData("mixed-radial", true, 390)]
    public async Task ReplacementRetainsSelectedSourceAndLegendStateAcrossSupportedCompositions(string layout, bool dark, int width) {
        if (!Enabled) return;
        var mode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light;
        var chart = SupportedCombinationRecipe.Create(layout, mode);
        var pageErrors = new List<string>();
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(Configure), width, 760,
            configurePage: page => page.PageError += (_, error) => pageErrors.Add(error));
        var page = session.Page;
        await page.EvaluateAsync("""
            ()=>{window.combinationController=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'));
                combinationController.select({seriesKey:'reference',observationIds:['north-central']});}
            """);
        var initial = Assert.Single((await Selection(page)).EnumerateArray());
        Assert.Equal("north-central", initial.GetProperty("observationId").GetString());
        await FocusWithKeyboardAsync(page, page.Locator(Selected));
        Assert.Contains("North", await TooltipTextAsync(page), StringComparison.Ordinal);
        await page.Locator(Legend).ClickAsync();
        Assert.Equal("true", await page.Locator(Legend).GetAttributeAsync("data-cfx-muted"));
        await FocusWithKeyboardAsync(page, page.Locator(Selected));
        await Capture(session, layout, width, "before", pageErrors);

        var updated = SupportedCombinationRecipe.Create("updated-" + layout, mode);
        await page.EvaluateAsync("html=>combinationController.update(html)", updated.ToInteractiveHtmlFragmentWithoutAssets(Configure));
        var retained = Assert.Single((await Selection(page)).EnumerateArray());
        Assert.Equal(initial.GetProperty("targetId").GetString(), retained.GetProperty("targetId").GetString());
        Assert.Equal("0", await page.Locator(Selected).GetAttributeAsync("data-cfx-series"));
        Assert.Equal(layout == "vertical-secondary" ? "1" : "0", await page.Locator(Selected).GetAttributeAsync("data-cfx-point"));
        Assert.Equal(layout == "vertical-secondary" ? .94 : 48, retained.GetProperty("numericValue").GetDouble());
        Assert.Equal("true", await page.Locator(Legend).GetAttributeAsync("data-cfx-muted"));
        Assert.True(await page.Locator(Selected).EvaluateAsync<bool>("node=>node===node.getRootNode().activeElement"));
        Assert.True(await page.Locator(".cfx-tooltip").EvaluateAsync<bool>("node=>node.hidden"));
        var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").EvaluateAsync("button=>button.click()"));
        var exported = XDocument.Parse(await File.ReadAllTextAsync(await download.PathAsync() ?? throw new InvalidOperationException("Missing composition export.")));
        var selected = Assert.Single(exported.Descendants(), node => (string?)node.Attribute("data-cfx-series-key") == "reference"
            && (string?)node.Attribute("data-cfx-observation-id") == "north-central" && node.Attribute("data-cfx-label-for") == null);
        Assert.Equal("true", (string?)selected.Attribute("aria-selected"));
        Assert.Equal(layout == "vertical-secondary" ? .94 : 48, double.Parse(((string?)selected.Attribute("data-cfx-y") ?? (string?)selected.Attribute("data-cfx-value"))!, System.Globalization.CultureInfo.InvariantCulture));
        await Capture(session, layout, width, "updated", pageErrors);
        await page.Locator("[data-cfx-reset]").ClickAsync();
        Assert.Empty((await Selection(page)).EnumerateArray());
        Assert.NotEqual("true", await page.Locator(Legend).GetAttributeAsync("data-cfx-muted"));
        Assert.True(await page.Locator("[data-cfx-reset]").IsHiddenAsync());
        await page.EvaluateAsync("()=>{combinationController.dispose();window.combinationController=ChartForgeXCharts.mount(combinationController.root);}");
        Assert.Empty((await Selection(page)).EnumerateArray());
        Assert.Empty(pageErrors); AssertNoConsoleErrors(session);
    }

    private static void Configure(HtmlChartInteractionOptions options) {
        options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
        options.Interaction.ChartId = "supported-combinations";
        options.Interaction.Enable(ChartInteractionFeatures.Export);
        options.Tooltip.PinOnSelect = false;
    }
    private static Task<JsonElement> Selection(Microsoft.Playwright.IPage page) => page.EvaluateAsync<JsonElement>("()=>combinationController.getSelection()");
    private static async Task Capture(HtmlTinkerX.HtmlBrowserSession session, string layout, int width, string state, IReadOnlyList<string> pageErrors) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        if (!Path.IsPathRooted(directory)) throw new InvalidOperationException("Captures require an absolute task path.");
        Directory.CreateDirectory(directory);
        var name = "supported-combination-" + layout + "-" + width + "-" + state;
        await session.Page.ScreenshotAsync(new() { Path = Path.Combine(directory, name + ".browser.png"), FullPage = true });
        await File.WriteAllTextAsync(Path.Combine(directory, name + ".json"), JsonSerializer.Serialize(new {
            selected = await Selection(session.Page), pageErrors,
            consoleErrors = session.ConsoleLog.Where(entry => entry.Type == HtmlTinkerX.HtmlConsoleMessageType.Error).Select(entry => entry.Text).ToArray()
        }));
    }
}
