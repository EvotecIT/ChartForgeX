using System.Text.Json;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class TopologyGraphLinkedBrowserTests {
    private const string TopologyRoot = ".cfx-topology-wrapper";

    [Fact]
    public void NarrowDrawingIsOptInValidatedAndCopiedWithoutChangingStaticSvg() {
        var options = new TopologyRenderOptions { EnableHtmlViewportControls = true }.WithHtmlNarrowMinimumHeight(288);
        Assert.Equal(288, options.Clone().HtmlNarrowMinimumHeight);
        Assert.Throws<ArgumentOutOfRangeException>(() => options.HtmlNarrowMinimumHeight = double.NaN);
        Assert.Throws<ArgumentOutOfRangeException>(() => options.HtmlNarrowMinimumHeight = -1);
        var chart = Topology(false);
        Assert.Equal(chart.ToSvg(), chart.ToSvg(options));
        Assert.Equal(chart.ToPng(), chart.ToPng(options));
        Assert.DoesNotContain("data-cfx-narrow-minimum-height=", chart.ToHtmlPage(options));
        Assert.Contains("data-cfx-narrow-minimum-height=\"288\"", chart.ToInteractiveHtmlPage(options));
        Assert.DoesNotContain("data-cfx-narrow-minimum-height=", chart.ToInteractiveHtmlPage(new TopologyRenderOptions()));
    }

    [BrowserTheory]
    [InlineData(false, 1440)]
    [InlineData(true, 1440)]
    [InlineData(false, 390)]
    [InlineData(true, 390)]
    public async Task TopologyCommandsSeparateSelectionAndHighlightWithoutEcho(bool dark, int width) {
        if (!Enabled) return;
        var chart = Topology(dark);
        var options = new TopologyRenderOptions { EnableHtmlSelectionPanel = false, EnableHtmlViewportControls = true, HtmlNarrowMinimumHeight = 288 };
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options), width, 680);
        var page = session.Page;
        await page.EvaluateAsync("""
            () => { window.topologyEvents=[]; for(const name of ['cfx-topology-select','cfx-topology-clear','cfx-topology-sync'])
              document.body.addEventListener(name,event=>topologyEvents.push({name,detail:event.detail})); }
            """);
        await TopologyCommand(page, "cfx-topology-set-selection", new { kind = "node", ids = new[] { "a", "c" } });
        Assert.Equal(2, await page.Locator("[data-cfx-role=topology-node][aria-selected=true]").CountAsync());
        Assert.Empty((await page.EvaluateAsync<JsonElement>("()=>topologyEvents")).EnumerateArray());
        await TopologyCommand(page, "cfx-topology-highlight", new { edgeIds = new[] { "ab" }, dim = true });
        Assert.Equal("true", await page.Locator("[data-cfx-role=topology-edge][data-edge-id=ab]").GetAttributeAsync("data-cfx-highlighted"));
        await TopologyCommand(page, "cfx-topology-clear-selection");
        Assert.Equal(0, await page.Locator("[aria-selected=true]").CountAsync());
        Assert.True(await page.Locator("[data-node-id=b][data-cfx-role=topology-node]").EvaluateAsync<bool>("item=>item.classList.contains('cfx-topology-html-muted')"));
        Assert.False(await page.Locator("[data-edge-id=ab][data-cfx-role=topology-edge]").EvaluateAsync<bool>("item=>item.classList.contains('cfx-topology-html-muted')"));
        await CaptureAsync(page, $"topology-linked-highlight-{width}-{dark}", new { dark, width });
        await TopologyCommand(page, "cfx-topology-clear-highlight");
        Assert.Equal(0, await page.Locator(".cfx-topology-html-muted,.cfx-topology-html-highlighted").CountAsync());
        await page.Locator("[data-node-id=a][data-cfx-role=topology-node]").ClickAsync();
        var clicked = await page.EvaluateAsync<JsonElement>("()=>topologyEvents.find(event=>event.name==='cfx-topology-select').detail");
        Assert.True(clicked.GetProperty("selected").GetBoolean());
        Assert.False(clicked.GetProperty("authoredSelected").GetBoolean());
        await page.EvaluateAsync("()=>topologyEvents=[]");
        await TopologyCommand(page, "cfx-topology-set-selection", new { kind = "edge", ids = new[] { "ab", "bc" }, emit = true });
        var selected = await page.EvaluateAsync<JsonElement>("()=>topologyEvents[0].detail");
        Assert.Equal(2, selected.GetProperty("ids").GetArrayLength());
        Assert.All(selected.GetProperty("selections").EnumerateArray(), item => Assert.True(item.GetProperty("selected").GetBoolean()));
        Assert.Equal(1, await page.EvaluateAsync<int>("()=>topologyEvents.length"));
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData("cfx-topology")]
    [InlineData("linked-topology")]
    public async Task LateInsertedTopologyMountsOnceAndNarrowFitCentersTheWholeDrawing(string prefix) {
        if (!Enabled) return;
        var chart = Topology(false);
        var options = new TopologyRenderOptions { EnableHtmlViewportControls = true, HtmlNarrowMinimumHeight = 288, CssClassPrefix = prefix };
        var rootSelector = "." + prefix + "-wrapper";
        var viewportSelector = "." + prefix + "-viewport";
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options), 390, 680);
        var page = session.Page;
        var late = Topology(false).WithId("late").ToInteractiveHtmlFragmentWithoutAssets(options);
        await page.EvaluateAsync("({html,root})=>{document.querySelector(root).remove();document.body.insertAdjacentHTML('beforeend',html);}", new { html = late, root = rootSelector });
        Assert.Null(await page.Locator(rootSelector).GetAttributeAsync("data-cfx-runtime-bound"));
        await page.Locator(rootSelector).EvaluateAsync("root=>{ChartForgeXTopology.mount(root);ChartForgeXTopology.mount(root);window.lateEvents=0;root.addEventListener('cfx-topology-select',()=>lateEvents++);}");
        Assert.True(await page.Locator(rootSelector + " " + viewportSelector + ">svg").EvaluateAsync<bool>("svg=>svg.getBoundingClientRect().height>=287"));
        await page.Locator("[data-cfx-topology-fit]").ClickAsync();
        await page.WaitForTimeoutAsync(150);
        var fit = await page.Locator(rootSelector).EvaluateAsync<JsonElement>("""
            (root,selector)=>{const viewport=root.querySelector(selector).getBoundingClientRect(),svg=root.querySelector(selector+'>svg').getBoundingClientRect();
              return {zoom:Number(root.dataset.cfxTopologyZoom),left:svg.left-viewport.left,right:svg.right-viewport.right};}
            """, viewportSelector);
        Assert.InRange(fit.GetProperty("zoom").GetDouble(), .001, .499);
        Assert.InRange(fit.GetProperty("left").GetDouble(), -.6, .6);
        Assert.InRange(fit.GetProperty("right").GetDouble(), -.6, .6);
        await page.Locator("[data-node-id=a][data-cfx-role=topology-node]").ClickAsync();
        Assert.Equal(1, await page.EvaluateAsync<int>("()=>lateEvents"));
        await CaptureAsync(page, "topology-linked-narrow-fit-" + prefix, fit);
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MixedTopologyPrefixesRetainPublicLateMountAcrossRegistrationOrderAndRepeatedAssets(bool reverse) {
        if (!Enabled) return;
        var first = reverse ? "linked-topology" : "cfx-topology";
        var last = reverse ? "cfx-topology" : "linked-topology";
        TopologyRenderOptions Options(string prefix) => new() { CssClassPrefix = prefix, EnableHtmlSelectionPanel = false };
        var html = "<!doctype html><html><body>"
            + Topology(false).WithId("initial-first").ToInteractiveHtmlFragment(Options(first))
            + "<script>window.retainedTopologyMount=ChartForgeXTopology.mount;</script>"
            + Topology(false).WithId("initial-last").ToInteractiveHtmlFragment(Options(last)) + "</body></html>";
        await using var session = await OpenAsync(html, 960, 680);
        var page = session.Page;
        Assert.Equal(2, await page.Locator("[data-cfx-interactive=true][data-cfx-runtime-bound=true]").CountAsync());
        for (var registration = 0; registration < 3; registration++)
            await page.AddScriptTagAsync(new PageAddScriptTagOptions { Content = HtmlInteractiveTopologyRenderer.BuildInteractionScript(Options(first)) });
        await page.EvaluateAsync("()=>window.mixedTopologyEvents=[]");
        foreach (var prefix in new[] { first, last }) {
            var id = "late-" + prefix;
            await page.EvaluateAsync("html=>document.body.insertAdjacentHTML('beforeend',html)",
                Topology(false).WithId(id).ToInteractiveHtmlFragmentWithoutAssets(Options(prefix)));
            var root = page.Locator("[data-chart-id='" + id + "'][data-cfx-interactive=true]");
            await root.EvaluateAsync("root=>ChartForgeXTopology.mount(root)");
            Assert.Equal("true", await root.GetAttributeAsync("data-cfx-runtime-bound"));
            await root.EvaluateAsync("root=>{retainedTopologyMount(root);ChartForgeXTopology.mount(root);root.addEventListener('cfx-topology-select',event=>mixedTopologyEvents.push(event.detail));}");
            await root.EvaluateAsync("root=>root.dispatchEvent(new CustomEvent('cfx-topology-set-selection',{detail:{kind:'node',ids:['a']}}))");
            Assert.Equal("true", await root.Locator("[data-cfx-role=topology-node][data-node-id=a]").GetAttributeAsync("aria-selected"));
            var second = root.Locator("[data-cfx-role=topology-node][data-node-id=b]");
            await second.FocusAsync();
            await second.PressAsync("Enter");
            Assert.Equal("true", await second.GetAttributeAsync("aria-selected"));
        }
        Assert.Equal(2, await page.EvaluateAsync<int>("()=>mixedTopologyEvents.length"));
        await CaptureAsync(page, "topology-linked-mixed-prefixes-" + reverse, new { first, last, repeatedRegistrations = 3 });
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(HtmlGraphRenderBackend.Svg, false)]
    [InlineData(HtmlGraphRenderBackend.Svg, true)]
    [InlineData(HtmlGraphRenderBackend.Canvas, false)]
    [InlineData(HtmlGraphRenderBackend.Canvas, true)]
    [InlineData(HtmlGraphRenderBackend.WebGl, false)]
    [InlineData(HtmlGraphRenderBackend.WebGl, true)]
    public async Task GraphPublicCommandsPreserveSeparateHighlightsAndEndpointIdentity(HtmlGraphRenderBackend backend, bool dark) {
        if (!Enabled) return;
        var graph = GraphScene.Create("linked-graph", "Service connections")
            .AddNode("a", "API", node => { node.Fixed = true; node.X = 180; node.Y = 180; })
            .AddNode("b", "Database", node => { node.Fixed = true; node.X = 480; node.Y = 180; })
            .AddNode("c", "Queue", node => { node.Fixed = true; node.X = 680; node.Y = 340; })
            .AddEdge("ab", "a", "b", "Stores");
        graph.Options.Features = GraphSceneFeatures.Selection | GraphSceneFeatures.MultiSelection | GraphSceneFeatures.Viewport;
        await using var session = await OpenAsync(graph.ToGraphExplorerHtmlPage(options => {
            options.RenderBackend = backend; options.Theme = dark ? HtmlGraphExplorerTheme.Dark : HtmlGraphExplorerTheme.Light;
            options.PersistThemePreference = false;
        }), 960, 680);
        var page = session.Page;
        await page.EvaluateAsync("""
            () => { window.graphEvents=[]; for(const name of ['cfxgraphselection','cfxgraphselect','cfxgraphhover','cfxgraphhoverclear'])
              document.body.addEventListener(name,event=>graphEvents.push({name,detail:event.detail}));
              ChartForgeXGraphExplorer.select('linked-graph',[{role:'graph-node',id:'a'},{role:'graph-edge',id:'ab'}]); }
            """);
        Assert.Equal(2, await page.EvaluateAsync<int>("()=>ChartForgeXGraphExplorer.captureState('linked-graph').selection.length"));
        Assert.Empty((await page.EvaluateAsync<JsonElement>("()=>graphEvents")).EnumerateArray());
        await page.EvaluateAsync("()=>ChartForgeXGraphExplorer.highlight('linked-graph',[{role:'graph-node',id:'b'}],{dim:true})");
        Assert.Equal(1, await page.Locator("[data-cfx-highlighted=true]").CountAsync());
        Assert.Equal(2, await page.EvaluateAsync<int>("()=>ChartForgeXGraphExplorer.captureState('linked-graph').selection.length"));
        Assert.True(await page.Locator("[data-cfx-role=graph-node][data-node-id=a]").EvaluateAsync<bool>("node=>node.classList.contains('cfx-graph-link-dim')"));
        if (backend == HtmlGraphRenderBackend.Svg)
            await page.WaitForFunctionAsync("()=>Number(getComputedStyle(document.querySelector('[data-cfx-role=graph-node][data-node-id=a]')).opacity)<=.301");
        await CaptureAsync(page, $"graph-linked-highlight-{backend}-{dark}", new { backend = backend.ToString(), actualBackend = await page.Locator(".cfx-graph-explorer").GetAttributeAsync("data-cfx-graph-renderer-active"), dark });
        await page.EvaluateAsync("()=>ChartForgeXGraphExplorer.clearSelection('linked-graph')");
        Assert.Equal(0, await page.EvaluateAsync<int>("()=>ChartForgeXGraphExplorer.captureState('linked-graph').selection.length"));
        Assert.Equal(1, await page.Locator("[data-cfx-highlighted=true]").CountAsync());
        Assert.Empty((await page.EvaluateAsync<JsonElement>("()=>graphEvents")).EnumerateArray());
        await page.EvaluateAsync("()=>ChartForgeXGraphExplorer.clearHighlight('linked-graph')");
        Assert.Equal(0, await page.Locator(".cfx-graph-link-dim,.cfx-graph-highlighted").CountAsync());
        await page.EvaluateAsync("()=>ChartForgeXGraphExplorer.select('linked-graph',[{role:'graph-edge',id:'ab'}],{emit:true})");
        var detail = await page.EvaluateAsync<JsonElement>("()=>graphEvents.find(event=>event.name==='cfxgraphselection').detail.items[0]");
        Assert.Equal("a", detail.GetProperty("sourceNodeId").GetString());
        Assert.Equal("b", detail.GetProperty("targetNodeId").GetString());
        if (backend == HtmlGraphRenderBackend.Svg) {
            await page.Locator("[data-cfx-role=graph-edge][data-edge-id=ab]").FocusAsync();
            await page.Locator("[data-cfx-role=graph-edge][data-edge-id=ab]").PressAsync("Enter");
            var clicked = await page.EvaluateAsync<JsonElement>("()=>graphEvents.find(event=>event.name==='cfxgraphselect').detail");
            Assert.Equal("a", clicked.GetProperty("sourceNodeId").GetString());
            await page.Locator("[data-cfx-role=graph-node][data-node-id=b]").FocusAsync();
            Assert.Contains((await page.EvaluateAsync<JsonElement>("()=>graphEvents")).EnumerateArray(), item => item.GetProperty("name").GetString() == "cfxgraphhover" && item.GetProperty("detail").GetProperty("id").GetString() == "b");
            await page.Locator("[data-cfx-role=graph-node][data-node-id=b]").EvaluateAsync("node=>node.blur()");
            Assert.Contains(await page.EvaluateAsync<string[]>("()=>graphEvents.map(item=>item.name)"), name => name == "cfxgraphhoverclear");
        }
        var hoverPoint = await page.Locator(".cfx-graph-explorer").EvaluateAsync<JsonElement>("""
            root=>{const node=ChartForgeXGraphExplorer.positions('linked-graph').find(node=>node.id==='b'),svg=root.querySelector('[data-cfx-role=graph-scene]'),box=svg.getBoundingClientRect(),size=svg.getAttribute('viewBox').split(/\s+/).map(Number),fit=Math.min(box.width/size[2],box.height/size[3]);
              return {x:box.left+(box.width-size[2]*fit)/2+(node.x*Number(root.dataset.cfxViewportScale||1)+Number(root.dataset.cfxViewportX||0))*fit,
                y:box.top+(box.height-size[3]*fit)/2+(node.y*Number(root.dataset.cfxViewportScale||1)+Number(root.dataset.cfxViewportY||0))*fit};}
            """);
        await page.Mouse.MoveAsync((float)hoverPoint.GetProperty("x").GetDouble(), (float)hoverPoint.GetProperty("y").GetDouble());
        Assert.Contains((await page.EvaluateAsync<JsonElement>("()=>graphEvents")).EnumerateArray(), item => item.GetProperty("name").GetString() == "cfxgraphhover" && item.GetProperty("detail").GetProperty("id").GetString() == "b");
        await page.Mouse.MoveAsync(1, 1);
        Assert.Contains((await page.EvaluateAsync<JsonElement>("()=>graphEvents")).EnumerateArray(), item => item.GetProperty("name").GetString() == "cfxgraphhoverclear" && item.GetProperty("detail").GetProperty("id").GetString() == "b");
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [Trait("BrowserSuite", "Compatibility")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GraphHighlightPaintStaysScopedToExplicitNodeShapes(bool dark) {
        if (!Enabled) return;
        var graph = GraphScene.Create("shape-highlights", "Service connections")
            .AddNode("a", "API", node => { node.Fixed = true; node.Shape = GraphNodeShape.Box; node.X = 180; node.Y = 180; })
            .AddNode("b", "Database", node => { node.Fixed = true; node.Shape = GraphNodeShape.Ellipse; node.X = 480; node.Y = 180; })
            .AddNode("c", "Queue", node => { node.Fixed = true; node.Shape = GraphNodeShape.Diamond; node.X = 680; node.Y = 340; })
            .AddEdge("ab", "a", "b").AddEdge("bc", "b", "c");
        graph.Options.Features = GraphSceneFeatures.Selection;
        await using var session = await OpenAsync(graph.ToGraphExplorerHtmlPage(options => {
            options.RenderBackend = HtmlGraphRenderBackend.Svg;
            options.Theme = dark ? HtmlGraphExplorerTheme.Dark : HtmlGraphExplorerTheme.Light;
            options.PersistThemePreference = false;
        }), 960, 680);
        var page = session.Page;
        const string paint = """
            ()=>[...document.querySelectorAll('[data-cfx-role=graph-node]')].map(node=>{
                const shape=node.querySelector('circle,rect,ellipse,polygon,path'),style=getComputedStyle(shape);
                return{id:node.dataset.nodeId,stroke:style.stroke,width:style.strokeWidth};})
            """;
        var before = await page.EvaluateAsync<JsonElement>(paint);
        await page.EvaluateAsync("()=>ChartForgeXGraphExplorer.highlight('shape-highlights',['a','b','c'].map(id=>({role:'graph-node',id})))");
        var highlighted = await page.EvaluateAsync<JsonElement>(paint);
        var selectedColor = await page.Locator(".cfx-graph-explorer").EvaluateAsync<string>("""
            root=>{const probe=document.createElement('span');probe.style.color='var(--cfx-color-selected)';
                root.append(probe);const color=getComputedStyle(probe).color;probe.remove();return color;}
            """);
        Assert.Equal(3, highlighted.GetArrayLength());
        Assert.All(highlighted.EnumerateArray(), item => {
            Assert.Equal(selectedColor, item.GetProperty("stroke").GetString());
            // Highlight and selection share the native five-pixel mark width across backends.
            Assert.Equal("5px", item.GetProperty("width").GetString());
        });
        Assert.Equal("none", await page.Locator(".cfx-graph-bg").EvaluateAsync<string>("node=>getComputedStyle(node).stroke"));
        await CaptureAsync(page, "graph-scoped-shape-highlights-" + dark, new { before, highlighted, selectedColor });
        await page.EvaluateAsync("()=>ChartForgeXGraphExplorer.clearHighlight('shape-highlights')");
        var cleared = await page.EvaluateAsync<JsonElement>(paint);
        Assert.Equal(before.GetRawText(), cleared.GetRawText());
        AssertNoConsoleErrors(session);
    }

    private static async Task CaptureAsync(IPage page, string name, object receipt) {
        var root = Environment.GetEnvironmentVariable("CFX_V2_CAPTURE_ROOT");
        if (string.IsNullOrWhiteSpace(root)) return;
        if (!Path.IsPathRooted(root)) throw new InvalidOperationException("CFX_V2_CAPTURE_ROOT must be absolute.");
        var folder = Path.Combine(root, "topology-linked");
        Directory.CreateDirectory(folder);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(folder, name + ".png"), FullPage = true, Animations = ScreenshotAnimations.Disabled });
        await File.WriteAllTextAsync(Path.Combine(folder, name + ".json"), JsonSerializer.Serialize(receipt));
    }

    private static Task TopologyCommand(IPage page, string name, object? detail = null) => page.Locator(TopologyRoot).EvaluateAsync(
        "(root,request)=>root.dispatchEvent(new CustomEvent(request.name,{detail:request.detail||{}}))", new { name, detail });

    private static TopologyChart Topology(bool dark) => TopologyChart.Create().WithId("linked-topology")
        .WithViewport(1060, 372, 0).WithLegend(null).WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
        .AddNode("a", "API", 80, 120, width: 140, height: 80)
        .AddNode("b", "Database", 460, 120, width: 140, height: 80)
        .AddNode("c", "Queue", 840, 120, width: 140, height: 80)
        .AddEdge("ab", "a", "b").AddEdge("bc", "b", "c");
}
