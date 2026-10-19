using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveSelectionDockBrowserTests {
    [BrowserTheory]
    [Trait("BrowserSuite", "Compatibility")]
    [InlineData(false, 340)]
    [InlineData(true, 980)]
    public async Task PagedComparisonInspectsEverySelectionAndPreservesExportsResizeAndReplacement(bool dark, int width) {
        if (!Enabled) return;
        void Configure(HtmlChartInteractionOptions options) {
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
            options.Interaction.Enable(ChartInteractionFeatures.Export);
            options.Localization.Direction = dark ? HtmlChartTextDirection.RightToLeft : HtmlChartTextDirection.LeftToRight;
            options.Localization[HtmlChartText.CompareNext] = "Następne wybrane obserwacje";
            options.Localization[HtmlChartText.ComparePrevious] = "Poprzednie wybrane obserwacje";
            options.Localization[HtmlChartText.CompareRange] = "{first}–{last} z {count}";
        }
        await using var session = await OpenAsync(PagedReadings(dark).ToInteractiveHtmlPage(Configure), width, 760);
        var page = session.Page;
        await page.EvaluateAsync("""
            ()=>{
                const root=document.querySelector('.cfx-interactive-chart');
                window.pagedController=ChartForgeXCharts.get(root);
                pagedController.select({observationIds:Array.from({length:15},(_,index)=>'reading-'+index)});
                window.pagingEvents=[];
                for(const name of ['cfxselectionchange','cfxcompare'])root.addEventListener(name,event=>pagingEvents.push({name,detail:event.detail}));
            }
            """);
        var original = await DownloadPagedSvgAsync(page);
        var indices = new List<int>();
        for (var group = 0; group < 3; group++) {
            Assert.Equal(group == 2 ? "13–15 z 15" : (group == 0 ? "1–6 z 15" : "7–12 z 15"),
                await page.Locator(".cfx-compare-tray__range").InnerTextAsync());
            indices.AddRange(await page.Locator(".cfx-compare-chip").EvaluateAllAsync<int[]>(
                "chips=>chips.map(chip=>Number(chip.dataset.cfxCompareIndex))"));
            Assert.InRange(await page.Locator(".cfx-compare-chip").CountAsync(), 1, 6);
            await page.Locator(".cfx-compare-chip").Last.ClickAsync();
            Assert.Contains("Observation " + (group == 2 ? "15" : group == 0 ? "06" : "12"), await TooltipTextAsync(page));
            await CaptureAsync(page, "selection-paging-" + dark + "-page-" + group, fullPage: false);
            if (group < 2) {
                var next = page.GetByRole(AriaRole.Button, new() { Name = "Następne wybrane obserwacje", Exact = true });
                await FocusWithKeyboardAsync(page, next);
                await page.Keyboard.PressAsync("Enter");
                Assert.Equal(((group + 1) * 6).ToString(), await page.EvaluateAsync<string>(
                    "()=>document.activeElement.dataset.cfxCompareIndex"));
            }
        }
        Assert.Equal(Enumerable.Range(0, 15), indices);
        Assert.True(await page.Locator("[data-cfx-compare-page=next]").IsDisabledAsync());
        var previous = page.GetByRole(AriaRole.Button, new() { Name = "Poprzednie wybrane obserwacje", Exact = true });
        await FocusWithKeyboardAsync(page, previous);
        await page.Keyboard.PressAsync("Enter");
        Assert.Equal("11", await page.EvaluateAsync<string>("()=>document.activeElement.dataset.cfxCompareIndex"));
        await page.Keyboard.PressAsync("Enter");
        Assert.Contains("Observation 12", await TooltipTextAsync(page));
        Assert.Equal(original, await DownloadPagedSvgAsync(page));
        Assert.Equal(0, await page.EvaluateAsync<int>("()=>pagingEvents.length"));
        Assert.Equal(15, await page.EvaluateAsync<int>("()=>pagedController.getSelection().length"));

        await page.SetViewportSizeAsync(width == 340 ? 980 : 340, 760);
        await page.EvaluateAsync("()=>pagedController.resize()");
        Assert.Equal("7–12 z 15", await page.Locator(".cfx-compare-tray__range").InnerTextAsync());
        await AssertDockBoundsAsync(page, ".cfx-interactive-chart");
        await page.EvaluateAsync("()=>window.detachedPageButton=document.querySelector('[data-cfx-compare-page=next]')");
        await page.EvaluateAsync("html=>pagedController.update(html)", PagedReadings(dark, updated: true).ToInteractiveHtmlFragmentWithoutAssets(Configure));
        Assert.Equal("7–12 z 15", await page.Locator(".cfx-compare-tray__range").InnerTextAsync());
        Assert.Equal(15, await page.EvaluateAsync<int>("()=>pagedController.getSelection().length"));
        await page.EvaluateAsync("()=>detachedPageButton.click()");
        Assert.Equal("7–12 z 15", await page.Locator(".cfx-compare-tray__range").InnerTextAsync());
        await page.EvaluateAsync("html=>pagedController.update(html)", PagedReadings(dark, count: 5).ToInteractiveHtmlFragmentWithoutAssets(Configure));
        Assert.Equal(5, await page.Locator(".cfx-compare-chip").CountAsync());
        Assert.Equal(0, await page.Locator(".cfx-compare-page").CountAsync());
        Assert.Equal(5, await page.EvaluateAsync<int>("()=>pagedController.getSelection().length"));
        await CaptureAsync(page, "selection-paging-" + dark + "-clamped", fullPage: false);
        await page.EvaluateAsync("()=>{pagedController.dispose();detachedPageButton.click()}");
        Assert.True(await page.Locator("[data-cfx-compare-tray]").IsHiddenAsync());
        Assert.Equal(0, await page.Locator(".cfx-compare-chip,.cfx-compare-page").CountAsync());
        Assert.True(await page.EvaluateAsync<bool>("()=>ChartForgeXCharts.get(pagedController.root)===null"));
        AssertNoConsoleErrors(session);
    }

    private static Chart PagedReadings(bool dark, int count = 15, bool updated = false) {
        var series = ChartSeries.CreateLine(new ChartSeriesId("paged-readings"), "Readings",
            Enumerable.Range(0, count).Select(index => new ChartPoint(index + 1, (index % 4 + 1) * (updated ? 3 : 2))).ToArray());
        for (var index = 0; index < count; index++) series.WithObservationId(index, new ChartObservationId("reading-" + index));
        return Chart.Create().WithSize(680, 380).WithTitle("Selected observations")
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithXLabels(Enumerable.Range(1, count).Select(index => "Observation " + index.ToString("00")).ToArray())
            .WithLegend(false).AddSeries(series);
    }

    private static async Task<string> DownloadPagedSvgAsync(IPage page) {
        var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").EvaluateAsync("button=>button.click()"));
        var path = await download.PathAsync() ?? throw new InvalidOperationException("No native SVG export.");
        var svg = await File.ReadAllTextAsync(path);
        Assert.DoesNotContain("cfx-compare", svg, StringComparison.Ordinal);
        Assert.DoesNotContain("data-cfx-keyboard-focus", svg, StringComparison.Ordinal);
        return svg;
    }
}
