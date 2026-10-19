using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Retained linked intent dims marks only when it has a current foreground, without altering independent selection.</summary>
public sealed class HighlightFontContractsBrowserTests {
    [BrowserTheory]
    [InlineData(false, 1440, false)]
    [InlineData(true, 390, false)]
    [InlineData(false, 1440, true)]
    [InlineData(true, 390, true)]
    public async Task ChartHighlightUsesCurrentAvailableForegroundAndPageFontsStayInsideTheirDeclarations(bool dark, int width, bool descriptor) {
        if (!Enabled) return;
        var renderer = new HtmlInteractiveChartRenderer();
        void Options(HtmlChartInteractionOptions options) { options.IdScope = "highlight-availability"; options.Highlight.DimOthersWhileSelected = true; }
        var chart = Bars("present", dark).WithFontFamily("serif;--cfx-font-probe:injected;outline:7px solid red;{x}<y>");
        await using var session = await OpenAsync(renderer.RenderPage(chart, Options), width, 760);
        var page = session.Page;
        Assert.Equal("", await page.EvaluateAsync<string>("()=>document.body.style.getPropertyValue('--cfx-font-probe')"));
        Assert.Equal("", await page.Locator(".cfx-interactive-chart").EvaluateAsync<string>("root=>root.style.getPropertyValue('--cfx-font-probe')"));
        Assert.Equal("", await page.EvaluateAsync<string>("()=>document.body.style.getPropertyValue('outline')"));
        Assert.Equal("", await page.Locator(".cfx-interactive-chart").EvaluateAsync<string>("root=>root.style.getPropertyValue('outline')"));
        await page.EvaluateAsync("""
            descriptor=>{window.chartHighlightEvents=[];const root=document.querySelector('.cfx-interactive-chart');
              window.highlightController=ChartForgeXCharts.get(root);
              root.addEventListener('cfxselectionchange',e=>chartHighlightEvents.push(e.detail));
              highlightController.highlight(descriptor ? {targets:[{seriesKey:'primary',observationId:'missing'}]} : {observationIds:['missing']},{dim:true});}
            """, descriptor);
        Assert.Equal(0, await page.Locator(".cfx-linked-dim").CountAsync());
        await page.EvaluateAsync("()=>highlightController.select({observationIds:['present']})");
        Assert.True(await page.Locator(Point(0, 0)).EvaluateAsync<bool>("node=>node.classList.contains('cfx-selected')&&!node.classList.contains('cfx-linked-dim')"));
        Assert.Equal(1, await SelectionCount(page));
        await page.EvaluateAsync("html=>highlightController.update(html)", renderer.RenderFragmentWithoutAssets(Bars("missing", dark), Options));
        Assert.Equal(1, await page.Locator("[data-cfx-highlighted=true]").CountAsync());
        Assert.True(await page.Locator(".cfx-linked-dim").CountAsync() > 0);
        await Capture(page, "chart-later-match-" + width + "-" + dark + "-" + descriptor);
        await page.EvaluateAsync("html=>highlightController.update(html)", renderer.RenderFragmentWithoutAssets(Bars("present", dark), Options));
        Assert.Equal(0, await page.Locator("[data-cfx-highlighted=true],.cfx-linked-dim").CountAsync());
        await page.EvaluateAsync("()=>highlightController.highlight({observationIds:['present']},{dim:true})");
        var primary = page.Locator(Legend(0));
        await primary.ClickAsync();
        Assert.Equal(0, await page.Locator(".cfx-linked-dim").CountAsync());
        Assert.Equal(1, await page.Locator("[data-cfx-highlighted=true]").CountAsync());
        await primary.ClickAsync();
        Assert.True(await page.Locator(".cfx-linked-dim").CountAsync() > 0);
        var other = page.Locator(Legend(1));
        var shift = new LocatorClickOptions { Modifiers = new[] { KeyboardModifier.Shift } };
        await other.ClickAsync(shift);
        Assert.True(await page.Locator(Point(1, 0)).EvaluateAsync<bool>("node=>{let opacity=1;for(let p=node;p;p=p.parentElement)opacity*=Number(getComputedStyle(p).opacity);return opacity>.99}"));
        await other.ClickAsync(shift);
        Assert.Equal(1, await page.Locator("[data-cfx-highlighted=true]").CountAsync());
        await page.EvaluateAsync("()=>highlightController.clearHighlight()");
        Assert.Equal(0, await page.Locator("[data-cfx-highlighted=true],.cfx-linked-dim").CountAsync());
        Assert.Equal(0, await page.EvaluateAsync<int>("()=>chartHighlightEvents.length"));
        await Capture(page, "chart-cleared-" + width + "-" + dark + "-" + descriptor);
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TopologyUnknownIdsDoNotAddHighlightMutingOrClearSelection(bool dark) {
        if (!Enabled) return;
        var chart = TopologyChart.Create().WithViewport(800, 320, 12).WithLegend(null)
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .AddNode("a", "API", 80, 100).AddNode("b", "Worker", 480, 100).AddEdge("ab", "a", "b", "Dispatch");
        await using var session = await OpenAsync(new HtmlInteractiveTopologyRenderer().RenderPage(chart,
            new TopologyRenderOptions { EnableHtmlSelectionPanel = false }), dark ? 390 : 1440, 680);
        var page = session.Page;
        await TopologyCommand(page, "cfx-topology-highlight", new { nodeIds = new[] { "missing" }, edgeIds = new[] { "missing-edge" }, dim = true });
        Assert.Equal(0, await page.Locator("[data-cfx-highlight-muted],[data-cfx-highlighted=true]").CountAsync());
        await TopologyCommand(page, "cfx-topology-set-selection", new { kind = "node", ids = new[] { "a" } });
        await TopologyCommand(page, "cfx-topology-highlight", new { nodeIds = new[] { "missing" }, dim = true });
        Assert.Equal(1, await page.Locator("[aria-selected=true]").CountAsync());
        Assert.Equal(0, await page.Locator("[data-cfx-highlight-muted]").CountAsync());
        await TopologyCommand(page, "cfx-topology-clear-selection");
        Assert.Equal(0, await page.Locator(".cfx-topology-html-muted").CountAsync());
        await TopologyCommand(page, "cfx-topology-highlight", new { nodeIds = new[] { "b" }, dim = true });
        Assert.True(await page.Locator("[data-cfx-highlight-muted]").CountAsync() > 0);
        await Capture(page, "topology-matching-" + dark);
        await TopologyCommand(page, "cfx-topology-highlight", new { nodeIds = new[] { "missing" }, dim = true });
        Assert.Equal(0, await page.Locator(".cfx-topology-html-muted,[data-cfx-highlighted=true]").CountAsync());
        await Capture(page, "topology-unmatched-" + dark);
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(HtmlGraphRenderBackend.Svg)]
    [InlineData(HtmlGraphRenderBackend.Canvas)]
    [InlineData(HtmlGraphRenderBackend.WebGl)]
    public async Task GraphLogicalVisibilityRefreshesRetainedHighlightIntentAcrossHostUpdates(HtmlGraphRenderBackend backend) {
        if (!Enabled) return;
        var graph = GraphScene.Create("availability-graph", "Linked service graph")
            .AddNode("a", "API", node => { node.Fixed = true; node.X = 180; node.Y = 180; })
            .AddNode("b", "Worker", node => { node.Fixed = true; node.X = 480; node.Y = 180; node.Hidden = true; })
            .AddNode("c", "Queue", node => { node.Fixed = true; node.X = 680; node.Y = 340; }).AddEdge("ac", "a", "c", "Dispatch");
        graph.Options.Features = GraphSceneFeatures.Selection | GraphSceneFeatures.MultiSelection | GraphSceneFeatures.Viewport | GraphSceneFeatures.IncrementalUpdates;
        await using var session = await OpenAsync(new HtmlGraphExplorerRenderer().RenderPage(graph, options => {
            options.RenderBackend = backend; options.Theme = HtmlGraphExplorerTheme.Dark; options.PersistThemePreference = false;
        }), 960, 680);
        var page = session.Page;
        await page.EvaluateAsync("()=>ChartForgeXGraphExplorer.highlight('availability-graph',[{role:'graph-node',id:'missing'}],{dim:true})");
        Assert.Equal(0, await page.Locator(".cfx-graph-link-dim").CountAsync());
        await page.EvaluateAsync("()=>ChartForgeXGraphExplorer.select('availability-graph',[{role:'graph-node',id:'a'}])");
        await page.EvaluateAsync("()=>ChartForgeXGraphExplorer.highlight('availability-graph',[{role:'graph-node',id:'b'}],{dim:true})");
        Assert.Equal(0, await page.Locator(".cfx-graph-link-dim").CountAsync());
        Assert.Equal(1, await page.EvaluateAsync<int>("()=>ChartForgeXGraphExplorer.captureState('availability-graph').selection.length"));
        await SetGraphNode(page, "b", false);
        Assert.Equal("true", await page.Locator("[data-cfx-role=graph-node][data-node-id=b]").GetAttributeAsync("data-cfx-highlighted"));
        Assert.True(await page.Locator(".cfx-graph-link-dim").CountAsync() > 0);
        await Capture(page, "graph-visible-" + backend);
        await SetGraphNode(page, "b", true);
        Assert.Equal(0, await page.Locator(".cfx-graph-link-dim").CountAsync());
        await page.EvaluateAsync("()=>ChartForgeXGraphExplorer.highlight('availability-graph',[{role:'graph-node',id:'late'}],{dim:true})");
        Assert.Equal(0, await page.Locator(".cfx-graph-link-dim").CountAsync());
        await SetGraphNode(page, "late", false);
        Assert.Equal("true", await page.Locator("[data-cfx-role=graph-node][data-node-id=late]").GetAttributeAsync("data-cfx-highlighted"));
        await page.EvaluateAsync("()=>ChartForgeXGraphExplorer.update('availability-graph',{removeNodeIds:['late']})");
        Assert.Equal(0, await page.Locator(".cfx-graph-link-dim").CountAsync());
        await page.EvaluateAsync("()=>ChartForgeXGraphExplorer.clearHighlight('availability-graph')");
        await SetGraphNode(page, "late", false);
        Assert.Equal(0, await page.Locator(".cfx-graph-highlighted,.cfx-graph-link-dim").CountAsync());
        Assert.Equal(1, await page.EvaluateAsync<int>("()=>ChartForgeXGraphExplorer.captureState('availability-graph').selection.length"));
        await Capture(page, "graph-cleared-" + backend);
        AssertNoConsoleErrors(session);
    }

    private static Chart Bars(string id, bool dark) {
        var points = new[] { new ChartPoint(1, 10), new ChartPoint(2, 20) };
        return Chart.Create().WithTitle("Service observations").WithSize(800, 450).WithLegend().WithDataLabels(false)
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light).WithXLabels("Berlin", "Paris")
            .AddSeries(ChartSeries.CreateBar(new ChartSeriesId("primary"), "First", points).WithObservationId(0, new ChartObservationId(id)))
            .AddSeries(ChartSeries.CreateBar(new ChartSeriesId("reference"), "Second", points));
    }

    private static Task<int> SelectionCount(IPage page) => page.EvaluateAsync<int>("()=>highlightController.getSelection().length");
    private static Task TopologyCommand(IPage page, string name, object? detail = null) => page.Locator(".cfx-topology-wrapper")
        .EvaluateAsync("(root,request)=>root.dispatchEvent(new CustomEvent(request.name,{detail:request.detail||{}}))", new { name, detail });
    private static Task SetGraphNode(IPage page, string id, bool hidden) => page.EvaluateAsync("""
        ({id,hidden})=>ChartForgeXGraphExplorer.update('availability-graph',{
          upsertNodes:[{id,label:id,hidden,fixed:true,x:480,y:180,size:10}]})
        """, new { id, hidden });

    private static async Task Capture(IPage page, string name) {
        var root = Environment.GetEnvironmentVariable("CFX_V2_CAPTURE_ROOT");
        if (string.IsNullOrWhiteSpace(root)) return;
        var folder = Path.Combine(root, "paint-font"); Directory.CreateDirectory(folder);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(folder, name + ".png"), Animations = ScreenshotAnimations.Disabled });
        await File.WriteAllTextAsync(Path.Combine(folder, name + ".json"), JsonSerializer.Serialize(new {
            name, highlighted = await page.Locator("[data-cfx-highlighted=true]").CountAsync(),
            linkedDim = await page.Locator(".cfx-linked-dim,.cfx-graph-link-dim,[data-cfx-highlight-muted]").CountAsync()
        }));
    }
}
