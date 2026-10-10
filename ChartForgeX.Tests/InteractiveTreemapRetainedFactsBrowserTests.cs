using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class InteractiveTreemapRetainedFactsBrowserTests {
    private const string DataStop = "[data-cfx-keyboard-component=data][tabindex='0']";
    private static string Target(string id) => "[data-cfx-target-kind=node][data-cfx-target-id='" + id + "']";

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task AuthoredZeroAndCollapsedLeavesRoveAndSelectWithoutFilledGeometry(bool allZero, bool compactDark) {
        if (!Enabled) return;
        var items = new List<ChartHierarchyItem> { new("group", "Allocated work"), new("reserve", "Reserve", "group", 0) };
        if (allZero) items.Add(new("standalone", "Standalone", value: 0));
        else { items.Add(new("visible", "Main allocation", "group", 1)); items.Add(new("tiny", "Small allocation", "group", 1e-20)); }
        var chart = Chart.Create().WithSize(compactDark ? 340 : 680, compactDark ? 340 : 400).WithLegend(false)
            .WithTheme(compactDark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
            .WithTitle(allZero ? "Unallocated work" : "Allocation by team").AddTreemap("Allocation", items);
        chart.Series[0].WithInteractionKey("allocation-source");
        var html = chart.ToInteractiveHtmlPage(options => { options.IncludeResetButton = false; options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit; })
            .Replace("<main ", "<button id='before-chart'>Before chart</button><main ", StringComparison.Ordinal);
        await using var session = await OpenAsync(html, compactDark ? 390 : 800, compactDark ? 620 : 550);
        var page = session.Page; var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        var root = page.Locator(".cfx-interactive-chart");
        await root.EvaluateAsync("root => { window.retainedSelections=[]; root.addEventListener('cfxselect', event => window.retainedSelections.push(event.detail.target)); }");
        var secondId = allZero ? "standalone" : "tiny";
        var expected = allZero ? new[] { "reserve", "standalone" } : new[] { "visible", "reserve", "tiny" };
        Assert.Equal(expected, await page.Locator("[data-cfx-keyboard-component=data]").EvaluateAllAsync<string[]>("nodes => nodes.map(node => node.dataset.cfxTargetId)"));
        foreach (var id in new[] { "reserve", secondId }) {
            var target = page.Locator(Target(id));
            Assert.Equal(id == "tiny" ? "precision-collapse" : "zero", await target.GetAttributeAsync("data-cfx-geometry-status"));
            Assert.True(await target.EvaluateAsync<bool>("node => { const box=node.getBoundingClientRect(); return node.childElementCount===0 && box.width===0 && box.height===0; }"));
        }
        await page.Locator("#before-chart").FocusAsync(); await page.Keyboard.PressAsync("Tab");
        Assert.Equal(expected[0], await ActiveId(page));
        if (!allZero) await page.Keyboard.PressAsync("ArrowRight");
        Assert.Equal("reserve", await ActiveId(page)); await page.Keyboard.PressAsync("Space");
        await page.Keyboard.PressAsync("ArrowRight"); Assert.Equal(secondId, await ActiveId(page)); await page.Keyboard.PressAsync("Enter");
        Assert.Equal(new[] { "reserve", secondId }, await page.EvaluateAsync<string[]>("() => window.retainedSelections.map(target => target.targetId)"));
        Assert.Equal(new[] { "0", allZero ? "0" : "1E-20" }, await page.EvaluateAsync<string[]>("() => window.retainedSelections.map(target => target.value)"));
        Assert.True(await page.EvaluateAsync<bool>("() => window.retainedSelections.every(target => target.targetKind==='node' && target.seriesKey==='allocation-source' && target.point===undefined && target.sourcePoint===undefined)"));
        Assert.Equal(1, await page.Locator(DataStop).CountAsync());
        foreach (var id in new[] { "reserve", secondId }) Assert.Equal("true", await page.Locator(Target(id)).GetAttributeAsync("aria-selected"));
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture);
            var name = "treemap-retained-" + (allZero ? "zero" : "mixed") + "-" + (compactDark ? "compact-dark" : "wide-light");
            File.WriteAllText(Path.Combine(capture, name + ".html"), html);
            File.WriteAllText(Path.Combine(capture, name + ".json"), await page.EvaluateAsync<string>("() => JSON.stringify(window.retainedSelections)"));
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(capture, name + ".png") });
        }
        // Retained facts obey the same actual host visibility and muted-data filters as painted leaves.
        await page.Locator(Target(secondId)).EvaluateAsync("node => node.classList.add('cfx-series-muted')");
        await page.Keyboard.PressAsync("Home"); Assert.Equal(expected[0], await ActiveId(page));
        Assert.Equal("-1", await page.Locator(Target(secondId)).GetAttributeAsync("tabindex"));
        await root.EvaluateAsync("root => { root.style.display='none'; window.dispatchEvent(new Event('resize')); }");
        await page.WaitForFunctionAsync("() => !document.querySelector('[data-cfx-keyboard-component=data][tabindex=\"0\"]')");
        await root.EvaluateAsync("root => { root.style.display=''; window.dispatchEvent(new Event('resize')); }");
        await page.WaitForFunctionAsync("() => document.querySelectorAll('[data-cfx-keyboard-component=data][tabindex=\"0\"]').length===1");
        await page.Locator(Target(secondId)).EvaluateAsync("node => node.classList.remove('cfx-series-muted')");
        await page.Locator("#before-chart").FocusAsync(); await page.Keyboard.PressAsync("Tab"); await page.Keyboard.PressAsync("End");
        Assert.Equal(secondId, await ActiveId(page));
        Assert.Empty(errors); AssertNoConsoleErrors(session);
    }

    private static Task<string> ActiveId(IPage page) => page.EvaluateAsync<string>("() => document.activeElement.dataset.cfxTargetId || ''");
}
