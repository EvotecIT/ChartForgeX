using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Exercises host-owned mounting and replacement through generated native chart sections.</summary>
public sealed partial class InteractiveChartLifecycleBrowserTests {
    private const string Root = ".cfx-interactive-chart";
    private const string North = "g[data-cfx-role=point][data-cfx-observation-id=north]";

    [BrowserFact]
    public async Task DynamicallyInsertedChartMountsOnceAndRetainsItsHostRoot() {
        if (!Enabled) return;
        await using var session = await OpenAsync(RuntimePage());
        var page = session.Page;
        await InsertAsync(page, Readings().ToInteractiveHtmlFragmentWithoutAssets());
        Assert.True(await page.EvaluateAsync<bool>("""
            () => {
                const root=document.querySelector('.cfx-interactive-chart'); root.id='mounted-chart';
                window.chartController=ChartForgeXCharts.mount(root);
                window.lifecycleSelections=[];
                root.addEventListener('cfxselect',event=>window.lifecycleSelections.push(event.detail));
                return chartController===ChartForgeXCharts.mount('#mounted-chart') && chartController.root===root;
            }
            """));
        await SelectAsync(page, North);
        Assert.Equal(1, await page.EvaluateAsync<int>("() => lifecycleSelections.length"));
        Assert.Equal("north", await page.EvaluateAsync<string>("() => lifecycleSelections[0].target.observationId"));
        await page.EvaluateAsync("() => { chartController.resize(); ChartForgeXCharts.mount(chartController.root); }");
        await page.Locator(North).PressAsync("Space");
        Assert.Equal(2, await page.EvaluateAsync<int>("() => lifecycleSelections.length"));
        Assert.Equal("false", await page.Locator(North).GetAttributeAsync("aria-selected"));
        await CaptureAsync(page, "dynamic-mount");
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task InvalidReplacementLeavesTheCurrentChartAndBindingsWorking() {
        if (!Enabled) return;
        await using var session = await OpenAsync(Readings().ToInteractiveHtmlPage());
        var page = session.Page;
        await BindControllerAsync(page);
        var other = Chart.Create().WithSize(640, 360).AddSeries(ChartSeries.CreateLine(new ChartSeriesId("other-source"), "Other",
            new[] { new ChartPoint(1, 3), new ChartPoint(2, 4) })).ToInteractiveHtmlFragmentWithoutAssets();
        var result = await page.EvaluateAsync<JsonElement>("""
            other => {
                const root=chartController.root, original=root.innerHTML, errors=[];
                const template=document.createElement('template');template.innerHTML=other;
                for(const change of ['svg','metadata','invalid-json','invalid-shape','swapped-series']) {
                    const next=root.cloneNode(true); next.removeAttribute('data-cfx-runtime-bound');
                    if(change==='svg') next.querySelector('svg').remove();
                    else if(change==='metadata') next.removeAttribute('data-cfx-prepared-chart');
                    else if(change==='invalid-json') next.setAttribute('data-cfx-prepared-chart','{');
                    else if(change==='invalid-shape') next.setAttribute('data-cfx-prepared-chart',JSON.stringify({series:{},regions:[],xLabels:[]}));
                    else next.setAttribute('data-cfx-prepared-chart',template.content.firstElementChild.dataset.cfxPreparedChart);
                    try { chartController.update(next); errors.push(false); } catch { errors.push(true); }
                }
                return {errors, unchanged:root.innerHTML===original, sameRoot:chartController.root===root};
            }
            """, other);
        Assert.All(result.GetProperty("errors").EnumerateArray(), error => Assert.True(error.GetBoolean()));
        Assert.True(result.GetProperty("unchanged").GetBoolean());
        Assert.True(result.GetProperty("sameRoot").GetBoolean());
        await SelectAsync(page, North);
        Assert.Equal("true", await page.Locator(North).GetAttributeAsync("aria-selected"));
        Assert.Contains("5", await TooltipTextAsync(page));
        await CaptureAsync(page, "invalid-replacement", result);
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(false, 820)]
    [InlineData(true, 380)]
    public async Task ReplacementReconcilesExplicitIdentityAndLegendStateWithUpdatedTypedFacts(bool dark, int width) {
        if (!Enabled) return;
        await using var session = await OpenAsync(Readings(dark).ToInteractiveHtmlPage(Fit), width, 650);
        var page = session.Page;
        await BindControllerAsync(page);
        await SelectAsync(page, North);
        await CaptureAsync(page, "typed-before-" + width + "-" + (dark ? "dark" : "light"));
        await page.Locator("[data-cfx-role=legend-item][data-cfx-series-key=reference]").ClickAsync();
        await FocusWithKeyboardAsync(page, page.Locator(North));
        await UpdateAsync(page, Readings(dark, updated: true).ToInteractiveHtmlFragmentWithoutAssets(Fit));
        var north = page.Locator(North);
        Assert.Equal("1", await north.GetAttributeAsync("data-cfx-series"));
        Assert.Equal("1", await north.GetAttributeAsync("data-cfx-point"));
        Assert.Equal("17", await north.GetAttributeAsync("data-cfx-y"));
        Assert.Equal("64", await north.GetAttributeAsync("data-cfx-size"));
        Assert.Equal("true", await north.GetAttributeAsync("aria-selected"));
        Assert.Equal("true", await page.Locator("[data-cfx-role=legend-item][data-cfx-series-key=reference]").GetAttributeAsync("data-cfx-muted"));
        Assert.True(await north.EvaluateAsync<bool>("node=>document.activeElement===node"));
        Assert.NotEqual("true", await page.Locator(Root).GetAttributeAsync("data-cfx-tooltip-pinned"));
        await CaptureAsync(page, "typed-retained-state-" + width + "-" + (dark ? "dark" : "light"));
        await page.Locator("[data-cfx-role=legend-item][data-cfx-series-key=samples]").ClickAsync(new LocatorClickOptions { Modifiers = new[] { KeyboardModifier.Shift } });
        await UpdateAsync(page, Readings(dark, updated: true).ToInteractiveHtmlFragmentWithoutAssets(Fit), element: true);
        Assert.Equal("true", await page.Locator("[data-cfx-role=legend-item][data-cfx-series-key=samples]").GetAttributeAsync("data-cfx-isolated"));
        Assert.True(await page.Locator("[data-cfx-role=series][data-cfx-series-key=reference]").EvaluateAsync<bool>("node=>node.classList.contains('cfx-series-isolated-out')"));
        await FocusWithKeyboardAsync(page, page.Locator(North));
        // Native entry after a redraw beneath the stationary mouse must not replace the keyboard readout.
        await page.EvaluateAsync("()=>new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve)))");
        Assert.Contains("17", await TooltipTextAsync(page));
        Assert.Contains("64", await TooltipTextAsync(page));
        await MoveToAsync(page, "[data-cfx-role=legend-item][data-cfx-series-key=samples]");
        Assert.Equal("Updated samples", await TooltipTextAsync(page));
        await MoveAwayAsync(page);
        Assert.Contains("17", await TooltipTextAsync(page));
        await page.Locator(North).PressAsync("Space");
        Assert.Equal("17", await page.EvaluateAsync<string>("()=>lifecycleSelections.at(-1).target.value"));
        Assert.Equal("north", await page.EvaluateAsync<string>("()=>lifecycleSelections.at(-1).target.observationId"));
        Assert.Equal(2, await page.EvaluateAsync<int>("()=>lifecycleSelections.length"));
        await CaptureAsync(page, "typed-update-" + width + "-" + (dark ? "dark" : "light"));
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RemovedOrAnonymousObservationsDoNotInheritSelectionOrReadouts(bool explicitIdentity) {
        if (!Enabled) return;
        var initial = Readings(explicitIdentity: explicitIdentity);
        await using var session = await OpenAsync(initial.ToInteractiveHtmlPage());
        var page = session.Page;
        await BindControllerAsync(page);
        await SelectAsync(page, explicitIdentity ? North : Point(0, 0));
        var next = Readings(updated: true, explicitIdentity: explicitIdentity);
        if (explicitIdentity) next.Series[1].WithObservationId(1, new ChartObservationId("replacement"));
        await UpdateAsync(page, next.ToInteractiveHtmlFragmentWithoutAssets());
        Assert.Equal(0, await page.Locator("[aria-selected=true]").CountAsync());
        Assert.NotEqual("true", await page.Locator(Root).GetAttributeAsync("data-cfx-tooltip-pinned"));
        Assert.True(await page.Locator(".cfx-tooltip").EvaluateAsync<bool>("tip=>tip.hidden"));
        Assert.True(string.IsNullOrEmpty(await page.Locator(Root).GetAttributeAsync("data-cfx-hover-key")));
        await SelectAsync(page, Point(1, 1));
        Assert.Equal("17", await page.EvaluateAsync<string>("()=>lifecycleSelections.at(-1).target.value"));
        Assert.Equal(1, await page.Locator("g[data-cfx-role=point][aria-selected=true]").CountAsync());
        await CaptureAsync(page, "cleared-" + (explicitIdentity ? "removed" : "anonymous"));
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task RelationshipReplacementRebindsNodeFactsWithoutBorrowingRemovedNodeIdentity() {
        if (!Enabled) return;
        Chart Relationship(bool updated) => Chart.Create().WithSize(640, 360).WithTheme(VisualTheme.Graphite())
            .WithTitle("Requests by stage").AddSeries(ChartSeries.CreateSankey(new ChartSeriesId("traffic"), "Traffic",
                updated ? new[] { new ChartNode("output", "Resolved"), new ChartNode("input", "Received") }
                    : new[] { new ChartNode("input", "Received"), new ChartNode("output", "Processed") },
                new[] { new ChartFlowLink("received", "input", "output", updated ? 19 : 5) }));
        await using var session = await OpenAsync(Relationship(false).ToInteractiveHtmlPage());
        var page = session.Page;
        await BindControllerAsync(page);
        const string output = "[data-cfx-target-kind=node][data-cfx-target-id=output]";
        await SelectAsync(page, output);
        await CaptureAsync(page, "relationship-before");
        await UpdateAsync(page, Relationship(true).ToInteractiveHtmlFragmentWithoutAssets());
        Assert.Equal("true", await page.Locator(output).GetAttributeAsync("aria-selected"));
        Assert.Equal("Resolved", await page.Locator(output).GetAttributeAsync("data-cfx-label"));
        Assert.Equal("19", await page.Locator(output).GetAttributeAsync("data-cfx-value"));
        await page.Locator("[data-cfx-target-kind=node][data-cfx-target-id=input]").FocusAsync();
        await page.Locator(output).FocusAsync();
        Assert.Contains("Resolved", await TooltipTextAsync(page));
        Assert.Contains("19", await TooltipTextAsync(page));
        await CaptureAsync(page, "relationship-update");
        AssertNoConsoleErrors(session);
    }

    private static Chart Readings(bool dark = false, bool updated = false, bool explicitIdentity = true) {
        var observations = updated
            ? new[] { new ChartBubble(2, 9, 25), new ChartBubble(1, 17, 64) }
            : new[] { new ChartBubble(1, 5, 9), new ChartBubble(2, 8, 25) };
        var samples = ChartSeries.CreateBubble(new ChartSeriesId("samples"), updated ? "Updated samples" : "Samples", observations);
        if (explicitIdentity) samples.WithObservationId(updated ? 1 : 0, new ChartObservationId("north"))
            .WithObservationId(updated ? 0 : 1, new ChartObservationId("south"));
        var reference = ChartSeries.CreateLine(new ChartSeriesId("reference"), "Reference", new[] { new ChartPoint(0, 20), new ChartPoint(3, 20) });
        var chart = Chart.Create().WithSize(640, 360).WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithTitle(updated ? "Updated observations" : "Measured observations").WithLegend()
            .WithXAxisBounds(0, 3).WithYAxisBounds(0, 25).WithDataLabels(false);
        return updated ? chart.AddSeries(reference).AddSeries(samples) : chart.AddSeries(samples).AddSeries(reference);
    }

    private static void Fit(HtmlChartInteractionOptions options) => options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;

    private static string RuntimePage() => "<!doctype html><html><head><style>" + HtmlInteractiveChartRenderer.BuildFragmentStyle()
        + "</style></head><body style='margin:12px'><script>" + HtmlInteractiveChartRenderer.BuildInteractionScript() + "</script></body></html>";

    private static Task InsertAsync(IPage page, string html) => page.EvaluateAsync("html=>document.body.insertAdjacentHTML('beforeend',html)", html);

    private static Task BindControllerAsync(IPage page) => page.EvaluateAsync("""
        () => {
            const root=document.querySelector('.cfx-interactive-chart');
            window.chartController=ChartForgeXCharts.mount(root); window.lifecycleSelections=[];
            root.addEventListener('cfxselect',event=>lifecycleSelections.push(event.detail));
        }
        """);

    private static Task UpdateAsync(IPage page, string html, bool element = false) => page.EvaluateAsync("""
        ({html,element}) => {
            const root=chartController.root;
            if(element) { const template=document.createElement('template'); template.innerHTML=html; chartController.update(template.content.firstElementChild); }
            else chartController.update(html);
            if(chartController.root!==root || !root.isConnected) throw new Error('The host root was replaced.');
        }
        """, new { html, element });

    private static async Task SelectAsync(IPage page, string selector) {
        await page.Locator(selector).FocusAsync();
        await page.Keyboard.PressAsync("Space");
    }

    private static async Task CaptureAsync(IPage page, string name, object? evidence = null) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        if (!Path.IsPathRooted(directory)) throw new InvalidOperationException("Capture output must be an absolute path.");
        Directory.CreateDirectory(directory);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, "lifecycle-" + name + ".png"), FullPage = true });
        var state = await page.EvaluateAsync<JsonElement>("""
            () => [...document.querySelectorAll('.cfx-interactive-chart')].map(root=>({
                chartId:root.dataset.cfxChartId, selected:[...root.querySelectorAll('[aria-selected=true]')].map(node=>({...node.dataset})),
                hover:root.dataset.cfxHoverKey, pinned:root.dataset.cfxTooltipPinned,
                tooltip:root.querySelector('.cfx-tooltip')?.innerText
            }))
            """);
        await File.WriteAllTextAsync(Path.Combine(directory, "lifecycle-" + name + ".json"), JsonSerializer.Serialize(new { state, evidence }));
    }
}
