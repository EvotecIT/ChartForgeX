using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;
using static ChartForgeX.Tests.InteractiveDrilldownTests;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class InteractiveDrilldownBrowserTests {
    private const string Parent = "section.cfx-interactive-chart:not([data-cfx-drill-level])";
    private const string Child = "section.cfx-interactive-chart[data-cfx-drill-level=\"1\"]";
    private static string PointIn(string section, int x) => section + " .cfx-stage g[data-cfx-role=\"point\"][data-cfx-x=\"" + x + "\"]";

    // The parent's reading state: selected categories, the standalone data window and the drawn zoom.
    private const string ViewScript = """
        selector=>{const root=document.querySelector(selector);return JSON.stringify({
            selected:[...root.querySelectorAll('.cfx-stage .cfx-selected[data-cfx-x]')].map(node=>node.dataset.cfxX),
            windows:root._cfxStandaloneViewport?.windows||null,zoom:root.dataset.cfxZoom||'1'});}
        """;

    private static Task<string> ViewAsync(IPage page) => page.EvaluateAsync<string>(ViewScript, Parent);
    private static Task WaitStatusAsync(IPage page, string text) =>
        page.WaitForFunctionAsync("text=>document.querySelector('[data-cfx-drill-status]')?.textContent===text", text);

    [BrowserTheory]
    [InlineData(HtmlChartRuntime.Full, ChartSeriesKind.Bar)]
    [InlineData(HtmlChartRuntime.Standalone, ChartSeriesKind.Line)]
    public async Task PointerAndKeyboardOpenEmbeddedChildrenAndBreadcrumbsRestoreSelectionWindowAndFocus(HtmlChartRuntime runtime, ChartSeriesKind kind) {
        if (!Enabled) return;
        var html = Quarters(kind).ToInteractiveHtmlPage(options => {
            options.Runtime = runtime;
            options.Interaction.ChartId = "sales";
            options.Interaction.Enable(ChartInteractionFeatures.Zoom);
            options.Drilldown.Add("Q1", Regions("Q1")).Add("Q2", Regions("Q2"));
        });
        await using var session = await OpenAsync(html, 760, 620);
        var page = session.Page;
        await page.EvaluateAsync("()=>{window.drills=[];document.addEventListener('cfxdrill',e=>drills.push({action:e.detail.action,level:e.detail.level,targetId:e.detail.targetId,path:e.detail.path.map(p=>p.label)}));}");
        var describedBy = await page.Locator(PointIn(Parent, 1)).GetAttributeAsync("aria-description");
        Assert.Equal("Opens Q1 by region", describedBy);
        Assert.Null(await page.Locator(PointIn(Parent, 3)).GetAttributeAsync("aria-description"));

        // A category without a target keeps selection; the zoomed window is part of the view to restore.
        await page.Locator(PointIn(Parent, 3)).ClickAsync();
        await page.Locator(Parent + " [data-cfx-zoom=in]").ClickAsync();
        await SettlePresentationAsync(page);
        var before = await ViewAsync(page);
        Assert.Contains("\"selected\":[\"3\"]", before);
        Assert.DoesNotContain("\"zoom\":\"1\",\"windows\":null", before);

        // Pointer drill: the child replaces the parent in place, with breadcrumbs and an announcement.
        await page.Locator(PointIn(Parent, 1)).ClickAsync();
        await page.Locator(Child).WaitForAsync();
        Assert.Equal("true", await page.Locator(Parent).GetAttributeAsync("data-cfx-drill-hidden"));
        Assert.True(await page.Locator(Child).IsVisibleAsync());
        Assert.False(await page.Locator(Parent).IsVisibleAsync());
        Assert.Equal("sales-q1", await page.Locator(Child).GetAttributeAsync("data-cfx-chart-id"));
        Assert.Equal("true", await page.Locator(Child).GetAttributeAsync("data-cfx-runtime-bound"));
        var crumbs = page.Locator(Child + " nav[data-cfx-drill-breadcrumbs] button");
        Assert.Equal(new[] { "Sales by quarter", "Q1 by region" }, await crumbs.AllTextContentsAsync());
        Assert.Equal("page", await crumbs.Nth(1).GetAttributeAsync("aria-current"));
        Assert.Equal("Drill-down path", await page.Locator(Child + " nav[data-cfx-drill-breadcrumbs]").GetAttributeAsync("aria-label"));
        Assert.True(await crumbs.Nth(1).EvaluateAsync<bool>("node=>document.activeElement===node"));
        await WaitStatusAsync(page, "Showing Q1 by region, level 2 of 2");
        Assert.Equal(0, await page.Locator(Child + " .cfx-selected").CountAsync());

        // Keyboard return: the parent breadcrumb is one Shift+Tab away and Enter activates it.
        await page.Keyboard.PressAsync("Shift+Tab");
        Assert.True(await crumbs.Nth(0).EvaluateAsync<bool>("node=>document.activeElement===node"));
        await page.Keyboard.PressAsync("Enter");
        await page.Locator(Child).WaitForAsync(new() { State = WaitForSelectorState.Detached });
        await SettlePresentationAsync(page);
        Assert.Null(await page.Locator(Parent).GetAttributeAsync("data-cfx-drill-hidden"));
        Assert.Equal(before, await ViewAsync(page));
        Assert.True(await page.Locator(PointIn(Parent, 1)).EvaluateAsync<bool>("node=>node.contains(document.activeElement)"));
        Assert.Equal(0, await page.Locator("nav[data-cfx-drill-breadcrumbs]").CountAsync());
        await WaitStatusAsync(page, "Back to Sales by quarter");

        // Keyboard drill: Enter opens the focused category, Space keeps selecting it.
        var second = page.Locator(PointIn(Parent, 2));
        await FocusWithKeyboardAsync(page, second);
        await page.Keyboard.PressAsync("Space");
        Assert.Equal(0, await page.Locator(Child).CountAsync());
        Assert.Contains("\"2\"", await ViewAsync(page));
        await page.Keyboard.PressAsync("Space");
        await page.Keyboard.PressAsync("Enter");
        await page.Locator(Child).WaitForAsync();
        Assert.Equal("sales-q2", await page.Locator(Child).GetAttributeAsync("data-cfx-chart-id"));
        await WaitStatusAsync(page, "Showing Q2 by region, level 2 of 2");
        // The child is an ordinary chart: its own points take keyboard focus and selection.
        var north = page.Locator(PointIn(Child, 1));
        await FocusWithKeyboardAsync(page, north);
        await page.Keyboard.PressAsync("Space");
        Assert.Equal(1, await page.Locator(Child + " .cfx-stage .cfx-selected").CountAsync());
        await page.Locator(Child + " nav[data-cfx-drill-breadcrumbs] button[data-cfx-drill-level=\"0\"]").ClickAsync();
        await page.Locator(Child).WaitForAsync(new() { State = WaitForSelectorState.Detached });
        await SettlePresentationAsync(page);
        Assert.Equal(before, await ViewAsync(page));
        Assert.True(await second.EvaluateAsync<bool>("node=>node.contains(document.activeElement)"));

        var drills = await page.EvaluateAsync<JsonElement>("()=>drills");
        Assert.Equal(new[] { "open:1:q1", "return:0:", "open:1:q2", "return:0:" },
            drills.EnumerateArray().Select(item => item.GetProperty("action").GetString() + ":" + item.GetProperty("level").GetInt32() + ":" + item.GetProperty("targetId").GetString()));
        Assert.Equal(new[] { "Sales by quarter", "Q1 by region" }, drills[0].GetProperty("path").EnumerateArray().Select(item => item.GetString()));
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task HostCallbackSuppliesOnDemandChildrenAndRejectionsKeepTheParent() {
        if (!Enabled) return;
        var renderer = new HtmlInteractiveChartRenderer();
        var html = Quarters().ToInteractiveHtmlPage(options => {
            options.Interaction.ChartId = "sales";
            options.Drilldown.AddOnDemand("Q3", "Q3 by region").AddOnDemand("Q4", "Q4 by region")
                .Add("Q1", Regions("Q1"), configure: child => child.Drilldown.AddOnDemand("North", "North sites"));
        });
        await using var session = await OpenAsync(html, 760, 620);
        var page = session.Page;
        await page.ExposeFunctionAsync("prepareChild", (string id) => renderer.PrepareSection(Regions(id), configure: options => {
            options.Interaction.ChartId = "sales-" + id; options.IdScope = "sales-" + id;
        }).Html);
        await page.EvaluateAsync("""
            ()=>{window.requests=[];window.errors=[];
                document.addEventListener('cfxdrillerror',e=>errors.push(e.detail.targetId+':'+e.detail.reason));
                document.addEventListener('cfxdrillrequest',async e=>{
                    const detail=e.detail;requests.push({targetId:detail.targetId,category:detail.category,level:detail.level,chartId:detail.chartId});
                    if(detail.targetId==='q4'){detail.reject('offline');return;}
                    detail.respond(await prepareChild(detail.category));
                });}
            """);

        await page.Locator(PointIn(Parent, 4)).ClickAsync();
        await WaitStatusAsync(page, "Q4 by region could not be loaded.");
        Assert.Equal(0, await page.Locator(Child).CountAsync());
        Assert.True(await page.Locator(Parent).IsVisibleAsync());
        Assert.Equal(new[] { "q4:offline" }, (await page.EvaluateAsync<string[]>("()=>errors")));

        await page.Locator(PointIn(Parent, 3)).ClickAsync();
        await page.Locator(Child).WaitForAsync();
        Assert.Equal("sales-Q3", await page.Locator(Child).GetAttributeAsync("data-cfx-chart-id"));
        await WaitStatusAsync(page, "Showing Q3 by region, level 2 of 2");
        await page.Locator(Child + " nav[data-cfx-drill-breadcrumbs] button[data-cfx-drill-level=\"0\"]").ClickAsync();
        await page.Locator(Child).WaitForAsync(new() { State = WaitForSelectorState.Detached });

        // An embedded child asks the host for its own on-demand level; the breadcrumbs cover all three.
        await page.Locator(PointIn(Parent, 1)).ClickAsync();
        await page.Locator(Child).WaitForAsync();
        await page.Locator(PointIn(Child, 1)).ClickAsync();
        const string grandchild = "section.cfx-interactive-chart[data-cfx-drill-level=\"2\"]";
        await page.Locator(grandchild).WaitForAsync();
        Assert.True(await page.Locator(Child).IsHiddenAsync());
        Assert.Equal(new[] { "Sales by quarter", "Q1 by region", "North sites" },
            await page.Locator(grandchild + " nav[data-cfx-drill-breadcrumbs] button").AllTextContentsAsync());
        await WaitStatusAsync(page, "Showing North sites, level 3 of 3");
        await page.Locator(grandchild + " nav[data-cfx-drill-breadcrumbs] button[data-cfx-drill-level=\"1\"]").ClickAsync();
        await page.Locator(grandchild).WaitForAsync(new() { State = WaitForSelectorState.Detached });
        Assert.True(await page.Locator(Child).IsVisibleAsync());
        await WaitStatusAsync(page, "Back to Q1 by region");

        var requests = await page.EvaluateAsync<JsonElement>("()=>requests");
        Assert.Equal(new[] { "sales:q4:0", "sales:q3:0", "sales:north:1" }, requests.EnumerateArray()
            .Select(item => item.GetProperty("chartId").GetString() + ":" + item.GetProperty("targetId").GetString() + ":" + item.GetProperty("level").GetInt32()));
        AssertNoConsoleErrors(session);
    }
}
