using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using ChartForgeX.Interactivity;
using ChartForgeX.Topology;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class TopologyAccentBrowserTests {
    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AcceleratedVirtualNodesResolveStatusAndOrdinaryAccentsWithoutDomStyle(bool statusSource) {
        if (!Enabled) return;
        var render = TopologyAccentSourceTests.Options(TopologyAccentSource.Status);
        var chart = TopologyAccentSourceTests.Chart();
        var graph = statusSource ? chart.ToGraphScene(o => { o.AccentSource = render.AccentSource; o.IconCatalog = render.IconCatalog; })
            : GraphScene.Create("ordinary", "Ordinary graph").AddNode("node", "Service", node => node.Style.BorderColor = "#2468AC");
        graph.Options.Enable(GraphSceneFeatures.LevelOfDetail);
        graph.Options.LevelOfDetail.CanvasPreferredNodeThreshold = 1;
        await using var session = await OpenAsync(graph.ToGraphExplorerHtmlPage(o => {
            o.RenderBackend = HtmlGraphRenderBackend.Canvas; o.AllowCanvasFallback = true;
            o.Theme = HtmlGraphExplorerTheme.Light; o.PersistThemePreference = false;
        }), 390, 700);
        await session.Page.WaitForFunctionAsync("()=>!!document.querySelector('.cfx-graph-explorer').__cfxGraphState");
        var root = session.Page.Locator(".cfx-graph-explorer");
        Assert.True(await root.EvaluateAsync<bool>("root=>root.__cfxGraphState.nodes[0].el.__cfxVirtual===true"));
        Assert.Equal(statusSource ? chart.Theme.Resolve(VisualThemeMode.Light).Status.Critical.Fill.ToCss() : "#2468AC",
            await root.EvaluateAsync<string>("root=>root.__cfxGraphState.nodes[0].borderColor"));
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(1440, HtmlGraphRenderBackend.Svg)]
    [InlineData(390, HtmlGraphRenderBackend.Svg)]
    [InlineData(1440, HtmlGraphRenderBackend.Canvas)]
    [InlineData(390, HtmlGraphRenderBackend.Canvas)]
    [InlineData(1440, HtmlGraphRenderBackend.WebGl)]
    [InlineData(390, HtmlGraphRenderBackend.WebGl)]
    public async Task StatusAccentsFollowTheExplorerThemeToggleWithoutFreezingIconColor(int width, HtmlGraphRenderBackend backend) {
        if (!Enabled) return;
        var chart = TopologyAccentSourceTests.Chart().WithTheme(VisualTheme.Graphite());
        var render = TopologyAccentSourceTests.Options(ChartForgeX.Topology.TopologyAccentSource.Status);
        var graph = chart.ToGraphScene(options => { options.AccentSource = render.AccentSource; options.IconCatalog = render.IconCatalog; });
        Assert.Contains("var(--cfx-topology-status-accent,", graph.Nodes[0].Style.BorderColor);
        await using var session = await OpenAsync(graph.ToGraphExplorerHtmlPage(options => {
            options.RenderBackend = backend; options.PersistThemePreference = false; options.Theme = HtmlGraphExplorerTheme.Light;
        }), width, 700);
        var page = session.Page;
        var root = page.Locator(".cfx-graph-explorer");
        var mark = root.Locator("[data-cfx-role=graph-node] > :is(rect,circle,path,polygon,ellipse)").First;
        async Task<string> Color() => backend == HtmlGraphRenderBackend.Svg ? await mark.EvaluateAsync<string>("node=>getComputedStyle(node).stroke")
            : await root.EvaluateAsync<string>("root=>root.__cfxGraphState.nodes[0].borderColor");
        await page.WaitForFunctionAsync("()=>!!document.querySelector('.cfx-graph-explorer').__cfxGraphState");
        Assert.DoesNotContain("var(", await root.EvaluateAsync<string>("root=>root.__cfxGraphState.nodes[0].borderColor"));
        var light = await Color();
        await root.Locator("[data-cfx-graph-action=theme]").ClickAsync();
        await page.WaitForFunctionAsync("()=>document.querySelector('.cfx-graph-explorer').dataset.cfxGraphThemeActive==='dark'");
        var dark = await Color();
        await page.EvaluateAsync("()=>{const root=document.querySelector('.cfx-graph-explorer'),node=ChartForgeXGraphExplorer.get(root).nodes[0];node.label='Updated service';ChartForgeXGraphExplorer.update(root,{upsertNodes:[node]});}");
        Assert.Equal(dark, await Color());
        Assert.NotEqual(light, dark);
        Assert.Equal(chart.Theme.Resolve(VisualThemeMode.Dark).Status.Critical.Fill.ToCss(),
            await root.Locator("[data-cfx-role=graph-node]").First.EvaluateAsync<string>("node=>getComputedStyle(node).getPropertyValue('--cfx-topology-status-accent').trim()"));
        await page.EvaluateAsync("()=>ChartForgeXGraphExplorer.theme(document.querySelector('.cfx-graph-explorer'),'light')");
        Assert.Equal(light, await Color());
        await page.EvaluateAsync("()=>{const root=document.querySelector('.cfx-graph-explorer'),node=ChartForgeXGraphExplorer.get(root).nodes[0];node.status='warning';ChartForgeXGraphExplorer.update(root,{upsertNodes:[node]});}");
        Assert.Equal(chart.Theme.Resolve(VisualThemeMode.Light).Status.Medium.Fill.ToCss(),
            await root.EvaluateAsync<string>("root=>root.__cfxGraphState.nodes[0].borderColor"));
        AssertNoConsoleErrors(session);
    }
}
