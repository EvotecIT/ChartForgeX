using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using HtmlTinkerX;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class HeatmapGroupedAxisBrowserTests {
    private const string Groups = "[data-cfx-role=axis-x-category-group],[data-cfx-role=axis-y-category-group]";
    private const string First = "[data-cfx-role=heatmap-cell][data-cfx-series-key=west][data-cfx-observation-id=west-a]";

    [BrowserTheory]
    [InlineData(false, false, 390)] [InlineData(true, true, 1000)]
    public async Task NativeMatrixParentsStayPassiveWhilePointerKeyboardSelectionAndReplacementKeepRealCellIdentity(bool categorical, bool dark, int width) {
        if (!Enabled) return;
        var chart = HeatmapGroupedAxisTests.Model(categorical, dark, width);
        await using var session = await BrowserProofFonts.OpenAsync(chart.ToInteractiveHtmlPage(Configure), width, 720);
        var page = session.Page;
        await page.EvaluateAsync("()=>{window.matrixRoot=document.querySelector('.cfx-interactive-chart');window.matrixController=ChartForgeXCharts.get(matrixRoot);}");
        Assert.Equal(10, await page.Locator("[data-cfx-role=heatmap-cell]").CountAsync());
        await AssertParents(page);
        Assert.True(await page.EvaluateAsync<bool>("""
            () => {
                const regions=JSON.parse(matrixRoot.dataset.cfxPreparedChart).regions;
                const labels=[...matrixRoot.querySelectorAll('[data-cfx-role="axis-x-category-group-label"],[data-cfx-role="axis-y-category-group-label"]')];
                return labels.length>0&&labels.every(text=>{
                    const group=text.closest('[data-cfx-role$="category-group"]');
                    const a=regions.find(region=>region.id===group.dataset.cfxSourceId),b=text.getBBox();
                    return a&&b.x>=a.x-.2&&b.y>=a.y-.2&&b.x+b.width<=a.x+a.width+.2&&b.y+b.height<=a.y+a.height+.2;
                });
            }
            """));
        await MoveToAsync(page, First);
        Assert.Contains("North / Core / West", await TooltipTextAsync(page)); Assert.Contains("Morning / Work / A", await TooltipTextAsync(page));
        await MoveAwayAsync(page); await page.Locator(First).FocusAsync();
        Assert.Contains(categorical ? "Pass" : "0", await TooltipTextAsync(page));
        await page.Keyboard.PressAsync("Space");
        var selected = Assert.Single((await page.EvaluateAsync<JsonElement>("()=>matrixController.getSelection()")).EnumerateArray());
        Assert.Equal("west", selected.GetProperty("seriesKey").GetString()); Assert.Equal("west-a", selected.GetProperty("observationId").GetString());
        Assert.Equal(categorical ? 1 : 10, selected.GetProperty("x").GetDouble());
        await Capture(session, "before", chart, categorical, dark, width);
        var replacement = HeatmapGroupedAxisTests.Model(categorical, dark, width);
        replacement.Options.XAxis.Reversed = true; replacement.Options.YAxis.Reversed = true;
        var first = replacement.Options.YAxis.Labels[0]; replacement.Options.YAxis.Labels[0] = new ChartAxisLabel(first.Value, first.Text, "Changed North", "Core");
        await page.EvaluateAsync("fragment=>matrixController.update(fragment)", replacement.ToInteractiveHtmlFragmentWithoutAssets(Configure));
        var retained = Assert.Single((await page.EvaluateAsync<JsonElement>("()=>matrixController.getSelection()")).EnumerateArray());
        Assert.Equal("west", retained.GetProperty("seriesKey").GetString()); Assert.Equal("west-a", retained.GetProperty("observationId").GetString());
        await MoveAwayAsync(page); await page.Locator(First).FocusAsync(); Assert.Contains("Changed North / Core / West", await TooltipTextAsync(page));
        Assert.Equal(0, await page.Locator("[data-cfx-role=heatmap-cell][data-cfx-series-key=west][data-cfx-column='1']").CountAsync());
        await AssertParents(page); Assert.True(await page.EvaluateAsync<bool>("()=>document.documentElement.scrollWidth<=innerWidth+1"));
        AssertNoConsoleErrors(session); await Capture(session, "replaced-reversed", replacement, categorical, dark, width);
    }

    private static void Configure(HtmlChartInteractionOptions options) {
        options.Interaction.Features = ChartInteractionFeatures.Tooltips | ChartInteractionFeatures.Selection | ChartInteractionFeatures.KeyboardNavigation;
        options.Tooltip.Mode = HtmlChartTooltipMode.Single; options.Tooltip.PinOnSelect = false;
        options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
    }
    private static async Task AssertParents(IPage page) {
        Assert.True(await page.Locator(Groups).CountAsync() > 0);
        Assert.True(await page.Locator(Groups).EvaluateAllAsync<bool>("""
            groups=>groups.every(group=>[group,...group.querySelectorAll('*')].every(node=>
                !['data-cfx-series','data-cfx-point','data-cfx-target-kind','data-cfx-target-id','tabindex'].some(name=>node.hasAttribute(name))))
            """));
    }
    private static async Task Capture(HtmlBrowserSession session, string stage, Chart chart, bool categorical, bool dark, int width) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory); var name = "heatmap-groups-" + (categorical ? "states" : "numeric") + "-" + width + (dark ? "-dark-" : "-light-") + stage;
        File.WriteAllText(Path.Combine(directory, name + ".svg"), chart.ToSvg()); File.WriteAllBytes(Path.Combine(directory, name + ".native.png"), chart.ToPng());
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + ".browser.png"), FullPage = true });
        var state = await session.Page.EvaluateAsync<JsonElement>("()=>({selection:matrixController.getSelection(),targets:matrixController.resolveTargets(),readout:document.querySelector('.cfx-tooltip')?.innerText})");
        File.WriteAllText(Path.Combine(directory, name + ".runtime.json"), state.GetRawText());
    }
}
