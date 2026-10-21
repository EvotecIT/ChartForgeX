using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using HtmlTinkerX;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Grouped axis captions describe existing observations without creating new interaction identities.</summary>
[Trait("BrowserSuite", "Compatibility")]
public sealed class GroupedCategoryAxisBrowserTests {
    private const string Root = ".cfx-interactive-chart";
    private const string Groups = "[data-cfx-role=axis-x-category-group],[data-cfx-role=axis-y-category-group]";
    private const string First = "[data-cfx-role=point][data-cfx-observation-id=first]";
    private const string Middle = "[data-cfx-role=point][data-cfx-observation-id=middle]";

    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GroupedCategoryContextStaysDetachedPassiveAndCurrentAcrossResizeAndReplacement(bool horizontal) {
        if (!Enabled) return;
        await using var session = await OpenAsync(Model(horizontal).ToInteractiveHtmlPage(Configure), horizontal ? 390 : 980, 780);
        var page = session.Page;
        await Bind(page);
        var targets = await page.EvaluateAsync<JsonElement>("()=>groupedController.resolveTargets()");
        AssertTargets(targets, grouped: true, "North");
        Assert.Equal(2, await page.EvaluateAsync<int>("()=>groupedController.resolveTargets({categories:['Central']}).length"));
        Assert.True(await page.EvaluateAsync<bool>("""
            ()=>{const target=groupedController.resolveTargets()[0];target.categoryPath[0]='host mutation';
                return groupedController.resolveTargets()[0].categoryPath[0]==='North';}
            """));
        await AssertPassiveGroups(page, present: true);

        await FocusWithKeyboardAsync(page, page.Locator(First));
        Assert.Contains("North / Operations / Central", await TooltipTextAsync(page), StringComparison.Ordinal);
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("first", Assert.Single((await Selection(page)).EnumerateArray()).GetProperty("observationId").GetString());
        await Capture(session, "before-resize-" + horizontal);
        await page.SetViewportSizeAsync(horizontal ? 980 : 390, 780);
        await page.WaitForFunctionAsync("drawing=>groupedRoot.dataset.cfxAdaptiveDrawing===drawing", horizontal ? "wide" : "narrow");
        AssertTargets(await page.EvaluateAsync<JsonElement>("()=>groupedController.resolveTargets()"), grouped: true, "North");
        Assert.Equal("first", Assert.Single((await Selection(page)).EnumerateArray()).GetProperty("observationId").GetString());
        await AssertPassiveGroups(page, present: true);
        await AssertSvgExport(page, horizontal);

        var replacement = Model(horizontal, north: "Changed North").ToInteractiveHtmlFragmentWithoutAssets(Configure);
        var rejected = await page.EvaluateAsync<int>("""
            fragment=>{let rejected=0;
                for(const groups of [null,'North',[3],[' ']]){
                    const template=document.createElement('template');template.innerHTML=fragment;
                    const candidate=template.content.firstElementChild,metadata=JSON.parse(candidate.dataset.cfxPreparedChart);
                    metadata.categoryLabels[0].groups=groups;candidate.dataset.cfxPreparedChart=JSON.stringify(metadata);
                    const before=groupedRoot.outerHTML;
                    try{groupedController.update(candidate);}catch(error){if(!(error instanceof TypeError))throw error;rejected++;}
                    if(groupedRoot.outerHTML!==before)throw Error('Invalid grouped metadata changed the committed chart.');
                }
                return rejected;}
            """, replacement);
        Assert.Equal(4, rejected);
        await page.EvaluateAsync("fragment=>groupedController.update(fragment)", replacement);
        AssertTargets(await page.EvaluateAsync<JsonElement>("()=>groupedController.resolveTargets()"), grouped: true, "Changed North");
        var retained = Assert.Single((await Selection(page)).EnumerateArray());
        Assert.Equal("first", retained.GetProperty("observationId").GetString());
        Assert.Equal("Changed North", retained.GetProperty("categoryPath")[0].GetString());

        await page.EvaluateAsync("fragment=>groupedController.update(fragment)", Model(horizontal, grouped: false).ToInteractiveHtmlFragmentWithoutAssets(Configure));
        AssertTargets(await page.EvaluateAsync<JsonElement>("()=>groupedController.resolveTargets()"), grouped: false, "North");
        Assert.False(Assert.Single((await Selection(page)).EnumerateArray()).TryGetProperty("categoryPath", out _));
        Assert.True(await page.EvaluateAsync<bool>("""
            ()=>JSON.parse(groupedRoot.dataset.cfxPreparedChart).categoryLabels.every(label=>!Object.hasOwn(label,'groups'))
            """));
        await AssertPassiveGroups(page, present: false);
        await Capture(session, "flat-replacement-" + horizontal);
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task NativeCategoryWindowRetainsFullSourceContextThroughAcceptedViewportReplacement() {
        if (!Enabled) return;
        var chart = Model(horizontal: false, line: true);
        var renderer = new HtmlInteractiveChartRenderer();
        var full = renderer.PrepareSection(chart, configure: ConfigureWindow);
        var window = renderer.PrepareSection(chart, new ChartCartesianViewport(x: new ChartXViewport(3.5, 5.5)), ConfigureWindow);
        var html = "<!doctype html><html><head><style>" + HtmlInteractiveChartRenderer.BuildFragmentStyle()
            + "</style></head><body style='margin:12px'>" + window.Html + "<script>"
            + HtmlInteractiveChartRenderer.BuildInteractionScript() + "</script></body></html>";
        await using var session = await OpenAsync(html, 980, 780);
        var page = session.Page;
        await Bind(page);
        var bindings = await page.EvaluateAsync<JsonElement>("()=>JSON.parse(groupedRoot.dataset.cfxPreparedChart).categoryLabels");
        Assert.Equal(new[] { 1.5, 4.25, 6.5 }, bindings.EnumerateArray().Select(binding => binding.GetProperty("value").GetDouble()));
        Assert.Equal(new[] { "North", "Operations" }, bindings[0].GetProperty("groups").EnumerateArray().Select(group => group.GetString()));
        await FocusWithKeyboardAsync(page, page.Locator(Middle));
        Assert.Contains("South / Operations / Central", await TooltipTextAsync(page), StringComparison.Ordinal);
        await page.Keyboard.PressAsync("Space");
        var selected = Assert.Single((await Selection(page)).EnumerateArray());
        Assert.Equal(new[] { "South", "Operations", "Central" }, selected.GetProperty("categoryPath").EnumerateArray().Select(value => value.GetString()));
        await AssertPassiveGroups(page, present: true);
        await Capture(session, "native-window");

        await page.EvaluateAsync("""
            ()=>{window.groupedRequests=[];window.groupedCommits=0;
                groupedRoot.addEventListener('cfxviewportchange',()=>groupedCommits++);
                groupedController.connectXViewport((request,signal)=>new Promise(resolve=>groupedRequests.push({request,signal,resolve})));
                groupedController.setXWindow(0,8);}
            """);
        await page.WaitForFunctionAsync("()=>groupedRequests.length===1");
        await page.EvaluateAsync("fragment=>groupedRequests[0].resolve(fragment)", full.Html);
        await page.WaitForFunctionAsync("()=>groupedCommits===1");
        Assert.Equal(selected.GetProperty("targetId").GetString(), Assert.Single((await Selection(page)).EnumerateArray()).GetProperty("targetId").GetString());
        Assert.Equal(selected.GetProperty("categoryPath").GetRawText(), Assert.Single((await Selection(page)).EnumerateArray()).GetProperty("categoryPath").GetRawText());
        Assert.Equal(8, await page.EvaluateAsync<double>("()=>groupedController.getXWindow().maximum"));
        AssertTargets(await page.EvaluateAsync<JsonElement>("()=>groupedController.resolveTargets()"), grouped: true, "North");
        await AssertPassiveGroups(page, present: true);
        await Capture(session, "native-full-domain");
        AssertNoConsoleErrors(session);
    }

    private static Chart Model(bool horizontal, bool grouped = true, string north = "North", bool line = false) {
        var points = new[] { new ChartPoint(1.5, 4), new ChartPoint(4.25, 7), new ChartPoint(6.5, 3) };
        var series = new ChartSeries(new ChartSeriesId("grouped-readings"), "Readings",
            line ? ChartSeriesKind.Line : horizontal ? ChartSeriesKind.HorizontalBar : ChartSeriesKind.Bar, points)
            .WithObservationIds(new[] { "first", "middle", "last" }.Select(id => new ChartObservationId(id)));
        if (line) series.ConfigureMarkers(markers => { markers.Enabled = true; markers.Radius = 4; });
        var chart = Chart.Create().WithSize(760, 460).WithHeader(false).WithLegend(false).AddSeries(series)
            .WithXAxisBounds(0, horizontal ? 10 : 8).WithYAxisBounds(0, horizontal ? 8 : 10);
        var labels = new[] {
            new ChartAxisLabel(1.5, "Central", grouped ? new[] { north, "Operations" } : Array.Empty<string>()),
            new ChartAxisLabel(2.75, "Planned", grouped ? new[] { north, "Operations" } : Array.Empty<string>()),
            new ChartAxisLabel(4.25, "Central", grouped ? new[] { "South", "Operations" } : Array.Empty<string>()),
            new ChartAxisLabel(6.5, "Remote", grouped ? new[] { "South", "Support" } : Array.Empty<string>())
        };
        if (horizontal) chart.ConfigureYAxis(axis => { foreach (var label in labels) axis.Labels.Add(label); });
        else chart.WithXLabels(labels);
        return chart;
    }

    private static void Configure(HtmlChartInteractionOptions options) {
        options.Interaction.Features = ChartInteractionFeatures.Tooltips | ChartInteractionFeatures.Selection
            | ChartInteractionFeatures.KeyboardNavigation | ChartInteractionFeatures.Export;
        options.Tooltip.Mode = HtmlChartTooltipMode.Single;
        options.Tooltip.PinOnSelect = false;
        options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
    }

    private static void ConfigureWindow(HtmlChartInteractionOptions options) {
        Configure(options);
        options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
        options.Tooltip.Mode = HtmlChartTooltipMode.SharedX;
        options.Interaction.Enable(ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan);
    }

    private static Task Bind(IPage page) => page.EvaluateAsync("""
        ()=>{window.groupedRoot=document.querySelector('.cfx-interactive-chart');window.groupedController=ChartForgeXCharts.get(groupedRoot);}
        """);

    private static Task<JsonElement> Selection(IPage page) => page.EvaluateAsync<JsonElement>("()=>groupedController.getSelection()");

    private static void AssertTargets(JsonElement targets, bool grouped, string north) {
        Assert.Equal(3, targets.GetArrayLength());
        Assert.Equal(new[] { 1.5, 4.25, 6.5 }, targets.EnumerateArray().Select(target => target.GetProperty("x").GetDouble()));
        Assert.Equal(new[] { "Central", "Central", "Remote" }, targets.EnumerateArray().Select(target => target.GetProperty("category").GetString()));
        Assert.Equal(new[] { "first", "middle", "last" }, targets.EnumerateArray().Select(target => target.GetProperty("observationId").GetString()));
        Assert.Equal(new[] { "0", "1", "2" }, targets.EnumerateArray().Select(target => target.GetProperty("sourcePoint").GetString()));
        if (!grouped) { Assert.All(targets.EnumerateArray(), target => Assert.False(target.TryGetProperty("categoryPath", out _))); return; }
        Assert.Equal(new[] { north, "Operations", "Central" }, targets[0].GetProperty("categoryPath").EnumerateArray().Select(value => value.GetString()));
        Assert.Equal(new[] { "South", "Operations", "Central" }, targets[1].GetProperty("categoryPath").EnumerateArray().Select(value => value.GetString()));
        Assert.Equal(new[] { "South", "Support", "Remote" }, targets[2].GetProperty("categoryPath").EnumerateArray().Select(value => value.GetString()));
    }

    private static async Task AssertPassiveGroups(IPage page, bool present) {
        var groups = page.Locator(Groups);
        Assert.Equal(present, await groups.CountAsync() > 0);
        Assert.True(await groups.EvaluateAllAsync<bool>("""
            groups=>groups.every(group=>[group,...group.querySelectorAll('*')].every(node=>
                !['data-cfx-series','data-cfx-point','data-cfx-target-kind','data-cfx-target-id','data-cfx-label','tabindex'].some(name=>node.hasAttribute(name))))
            """));
    }

    private static async Task AssertSvgExport(IPage page, bool horizontal) {
        var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync());
        var svg = await File.ReadAllTextAsync(await download.PathAsync() ?? throw new InvalidOperationException("Missing grouped SVG export."));
        var nodes = XDocument.Parse(svg).Descendants().ToArray();
        Assert.Contains(nodes, node => (string?)node.Attribute("data-cfx-role") is "axis-x-category-group" or "axis-y-category-group");
        var first = Assert.Single(nodes, node => (string?)node.Attribute("data-cfx-observation-id") == "first");
        Assert.Contains("North", (string?)first.Attribute("aria-label") ?? string.Empty, StringComparison.Ordinal);
        Assert.Contains("cfx-selected", (string?)first.Attribute("class") ?? string.Empty, StringComparison.Ordinal);
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) await File.WriteAllTextAsync(Path.Combine(directory, "grouped-category-export-" + horizontal + ".svg"), svg);
    }

    private static async Task Capture(HtmlBrowserSession session, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        if (!Path.IsPathRooted(directory)) throw new InvalidOperationException("Browser captures require an absolute output path.");
        Directory.CreateDirectory(directory);
        await session.Page.ScreenshotAsync(new() { Path = Path.Combine(directory, "grouped-category-" + name + ".png"), FullPage = true });
        var context = await session.Page.EvaluateAsync<JsonElement>("()=>({targets:groupedController.resolveTargets(),selected:groupedController.getSelection(),metadata:JSON.parse(groupedRoot.dataset.cfxPreparedChart)})");
        await File.WriteAllTextAsync(Path.Combine(directory, "grouped-category-" + name + ".json"), context.GetRawText());
    }
}
