using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using ChartForgeX.Interactivity;
using ChartForgeX.Topology;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class TopologyAccentBrowserTests {
    [BrowserTheory]
    [InlineData(HtmlGraphRenderBackend.Svg, TopologyAccentSource.Auto)]
    [InlineData(HtmlGraphRenderBackend.Svg, TopologyAccentSource.Status)]
    [InlineData(HtmlGraphRenderBackend.Canvas, TopologyAccentSource.Auto)]
    [InlineData(HtmlGraphRenderBackend.Canvas, TopologyAccentSource.Status)]
    public async Task ImplicitSurfacesFollowThemeWhileExplicitPaintAndIconFallbackSurvivePatches(HtmlGraphRenderBackend backend, TopologyAccentSource source) {
        if (!Enabled) return;
        var render = TopologyAccentSourceTests.Options(source);
        var chart = TopologyChart.Create().WithTheme(VisualTheme.Graphite()).WithLayout(TopologyLayoutMode.Manual);
        var displays = new[] { TopologyNodeDisplayMode.Dot, TopologyNodeDisplayMode.Icon, TopologyNodeDisplayMode.Tile, TopologyNodeDisplayMode.Card };
        for (var index = 0; index < displays.Length; index++) {
            chart.AddNode("implicit-" + index, displays[index].ToString(), 80 + index * 160, 100, status: TopologyHealthStatus.Critical,
                iconId: "accent-proof:certificate").WithNodeDisplay("implicit-" + index, displays[index]);
            chart.Nodes[index].Color = index % 2 == 0 ? "" : "   ";
        }
        chart.AddNode("explicit", "Authored white", 80, 300, color: "#713CA6", backgroundColor: "#ffffff")
            .WithNodeDisplay("explicit", TopologyNodeDisplayMode.Dot);
        var graph = chart.ToGraphScene(o => { o.AccentSource = source; o.IconCatalog = render.IconCatalog; });
        graph.Options.Enable(GraphSceneFeatures.LevelOfDetail);
        if (backend == HtmlGraphRenderBackend.Canvas) graph.Options.LevelOfDetail.CanvasPreferredNodeThreshold = 1;
        string Html(HtmlGraphExplorerTheme theme) => graph.ToGraphExplorerHtmlPage(o => {
            o.RenderBackend = backend; o.AllowCanvasFallback = true; o.Theme = theme; o.PersistThemePreference = false;
        });
        await using var session = await OpenAsync(Html(HtmlGraphExplorerTheme.Light), 1100, 720);
        var page = session.Page;
        var root = page.Locator(".cfx-graph-explorer");
        async Task Capture(IPage capturePage, string phase, string mode) {
            var folder = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
            if (string.IsNullOrWhiteSpace(folder)) return;
            Directory.CreateDirectory(folder);
            var name = "topology-surfaces-" + backend + "-" + source + "-" + phase + "-" + mode;
            var htmlPath = Path.Combine(folder, name + ".html");
            await File.WriteAllTextAsync(htmlPath, Html(mode == "dark" ? HtmlGraphExplorerTheme.Dark : HtmlGraphExplorerTheme.Light));
            var originalViewport = capturePage.ViewportSize!;
            try {
                foreach (var width in new[] { 1440, 390 }) {
                    await capturePage.SetViewportSizeAsync(width, 720);
                    await capturePage.EvaluateAsync("()=>new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve)))");
                    var facts = await capturePage.EvaluateAsync<System.Text.Json.JsonElement>("""
                        () => {
                            const root = document.querySelector('.cfx-graph-explorer');
                            return { viewportWidth: innerWidth, documentWidth: document.documentElement.clientWidth,
                                scrollWidth: document.documentElement.scrollWidth, theme: root.dataset.cfxGraphThemeActive,
                                renderer: root.dataset.cfxGraphRendererActive,
                                nodes: root.__cfxGraphState.nodes.map(n => ({ id: n.id, fill: n.backgroundColor, border: n.borderColor,
                                    authoredFill: n.el.getAttribute('data-node-background-color') })) };
                        }
                        """);
                    Assert.True(facts.GetProperty("scrollWidth").GetInt32() <= facts.GetProperty("documentWidth").GetInt32(), facts.GetRawText());
                    Assert.Equal(mode, facts.GetProperty("theme").GetString());
                    var capturePath = Path.Combine(folder, name + "-" + width + ".png");
                    await capturePage.ScreenshotAsync(new() { Path = capturePath, FullPage = true, Animations = ScreenshotAnimations.Disabled });
                    var receipt = new { phase = phase == "before" ? "reproduced previous authored fill projection" : "fixed theme-derived fill projection",
                        htmlPath, capturePath, facts };
                    await File.WriteAllTextAsync(Path.ChangeExtension(capturePath, ".json"), System.Text.Json.JsonSerializer.Serialize(receipt));
                }
            } finally {
                await capturePage.SetViewportSizeAsync(originalViewport.Width, originalViewport.Height);
            }
        }
        await page.WaitForFunctionAsync("()=>!!document.querySelector('.cfx-graph-explorer').__cfxGraphState");
        if (backend == HtmlGraphRenderBackend.Canvas)
            Assert.True(await root.EvaluateAsync<bool>("root=>root.__cfxGraphState.nodes[0].el.__cfxVirtual===true"));
        async Task AssertPaint(VisualThemeMode mode) {
            var card = await root.EvaluateAsync<string>("root=>getComputedStyle(root).getPropertyValue('--cfx-color-surface-solid').trim()");
            for (var index = 0; index < displays.Length; index++) {
                var paint = await root.EvaluateAsync<string[]>("(root,id)=>{const n=root.__cfxGraphState.byId.get(id);return [n.backgroundColor,n.borderColor]}", "implicit-" + index);
                Assert.Equal(card, paint[0]);
                Assert.Equal(source == TopologyAccentSource.Status ? chart.Theme.Resolve(mode).Status.Critical.Fill.ToCss() : "#2468AC", paint[1]);
                if (backend == HtmlGraphRenderBackend.Svg) {
                    var actual = await root.Locator("[data-node-id='implicit-" + index + "'] > :is(rect,circle,path,polygon,ellipse)").First
                        .EvaluateAsync<string>("node=>getComputedStyle(node).fill");
                    var expected = await root.EvaluateAsync<string>("(root,color)=>{const probe=document.createElement('span');probe.style.color=color;root.append(probe);const value=getComputedStyle(probe).color;probe.remove();return value}", card);
                    Assert.Equal(expected, actual);
                }
            }
            Assert.Equal("#ffffff", await root.EvaluateAsync<string>("root=>root.__cfxGraphState.byId.get('explicit').backgroundColor"));
            if (backend == HtmlGraphRenderBackend.Svg)
                Assert.Equal("rgb(255, 255, 255)", await root.Locator("[data-node-id=explicit] > :is(rect,circle,path,polygon,ellipse)").First.EvaluateAsync<string>("node=>getComputedStyle(node).fill"));
        }
        await AssertPaint(VisualThemeMode.Light);
        await Capture(page, "after", "light");
        await root.Locator("[data-cfx-graph-action=theme]").ClickAsync();
        await page.WaitForFunctionAsync("()=>document.querySelector('.cfx-graph-explorer').dataset.cfxGraphThemeActive==='dark'");
        await AssertPaint(VisualThemeMode.Dark);
        await Capture(page, "after", "dark");
        await page.EvaluateAsync("()=>{const root=document.querySelector('.cfx-graph-explorer'),nodes=ChartForgeXGraphExplorer.get(root).nodes;nodes.forEach(n=>n.label+=' updated');ChartForgeXGraphExplorer.update(root,{upsertNodes:nodes});}");
        await AssertPaint(VisualThemeMode.Dark);
        await page.EvaluateAsync("()=>ChartForgeXGraphExplorer.theme(document.querySelector('.cfx-graph-explorer'),'light')");
        await AssertPaint(VisualThemeMode.Light);
        AssertNoConsoleErrors(session);
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY"))) {
            // Reproduce only the former C# projection: non-card defaults authored the source theme's light Card fill.
            var previousFill = chart.Theme.Resolve(VisualThemeMode.Light).Surface.ToCss();
            var previousNodes = graph.Nodes.Where(node => node.Id.StartsWith("implicit-", StringComparison.Ordinal) && !node.Metadata.ContainsKey("topology.card")).ToArray();
            try {
                foreach (var node in previousNodes) node.Style.BackgroundColor = previousFill;
                await using var beforeSession = await OpenAsync(Html(HtmlGraphExplorerTheme.Light), 1100, 720);
                var beforePage = beforeSession.Page;
                await beforePage.WaitForFunctionAsync("()=>!!document.querySelector('.cfx-graph-explorer').__cfxGraphState");
                await Capture(beforePage, "before", "light");
                await beforePage.Locator("[data-cfx-graph-action=theme]").ClickAsync();
                await beforePage.WaitForFunctionAsync("()=>document.querySelector('.cfx-graph-explorer').dataset.cfxGraphThemeActive==='dark'");
                foreach (var node in previousNodes)
                    Assert.Equal(previousFill, await beforePage.Locator(".cfx-graph-explorer").EvaluateAsync<string>("(root,id)=>root.__cfxGraphState.byId.get(id).backgroundColor", node.Id));
                await Capture(beforePage, "before", "dark");
                AssertNoConsoleErrors(beforeSession);
            } finally {
                foreach (var node in previousNodes) node.Style.BackgroundColor = null;
            }
        }
    }

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
