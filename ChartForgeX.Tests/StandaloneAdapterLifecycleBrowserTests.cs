using System.Text.Json;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Topology;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Exercises detached standalone adapters and subsequent reuse through their public browser APIs.</summary>
[Trait("BrowserSuite", "Compatibility")]
public sealed partial class StandaloneAdapterLifecycleBrowserTests {
    [BrowserTheory]
    [InlineData("cfx-topology")]
    [InlineData("report-route")]
    public async Task TopologyRemovalAndExplicitDisposalStopPlaybackAndReleaseListenersBeforeRemount(string prefix) {
        if (!Enabled) return;
        var options = new TopologyRenderOptions {
            CssClassPrefix = prefix, ActiveScenarioId = "route", EnableHtmlViewportControls = true,
            EnableHtmlFullscreenControl = true
        };
        options.SelectedNodeIds.Add("b");
        await using var session = await OpenAsync(Instrument(new HtmlInteractiveTopologyRenderer().RenderPage(Topology(), options)), 900, 720);
        var page = session.Page;
        await page.EvaluateAsync("""
            () => {
                window.retainedTopology=document.querySelector('[data-chart-id=loop-lifetime]');
                window.topologySteps=[];
                retainedTopology.addEventListener('cfx-topology-scenario-step',event=>topologySteps.push({index:event.detail.index,connected:retainedTopology.isConnected}));
                lifetimeProbe.recordGlobals=false;lifetimeProbe.recordObservers=false;
            }
            """);
        var controls = page.Locator("[data-cfx-scenario-step-control=play]");
        var route = page.Locator("[data-cfx-topology-scenario=route]");
        await route.HoverAsync();
        Assert.True(await page.Locator("[data-cfx-role=topology-node]." + prefix + "-html-scenario-preview").CountAsync() > 0);
        await MoveAwayAsync(page);
        await route.FocusAsync();
        Assert.True(await page.Locator("[data-cfx-role=topology-node]." + prefix + "-html-scenario-preview").CountAsync() > 0);
        await controls.ClickAsync();
        await page.WaitForFunctionAsync("()=>topologySteps.length>=2");
        await page.Locator("[data-cfx-role=topology-node][data-node-id=a]").HoverAsync();
        Assert.Equal("a", await page.Locator("." + prefix + "-wrapper").GetAttributeAsync("data-cfx-hover-id"));
        await CaptureAsync(page, "topology-" + prefix + "-playing");
        await page.EvaluateAsync("()=>retainedTopology.remove()");
        await page.WaitForFunctionAsync("()=>!retainedTopology.hasAttribute('data-cfx-runtime-bound')");
        var stopped = await page.EvaluateAsync<int>("()=>topologySteps.length");
        await page.WaitForTimeoutAsync(650);
        Assert.Equal(stopped, await page.EvaluateAsync<int>("()=>topologySteps.length"));
        Assert.Equal(0, await page.EvaluateAsync<int>("()=>topologySteps.filter(step=>!step.connected).length"));
        await AssertReleasedAsync(page, "topology-global-removal");
        await MoveAwayAsync(page);

        for (var cycle = 0; cycle < 3; cycle++) {
            await page.EvaluateAsync("""
                () => {
                    document.body.append(retainedTopology); lifetimeProbe.recordGlobals=true;lifetimeProbe.recordObservers=true;
                    if(ChartForgeXTopology.mount(retainedTopology).length!==1||ChartForgeXTopology.mount(retainedTopology).length!==1)
                        throw new Error('Idempotent mount lost its topology');
                    lifetimeProbe.recordGlobals=false;lifetimeProbe.recordObservers=false;
                    if(lifetimeProbe.globals.filter(entry=>entry.active).length!==4)throw new Error('Mount duplicated global listeners');
                }
                """);
            Assert.Null(await page.Locator("." + prefix + "-wrapper").GetAttributeAsync("data-cfx-hover-id"));
            Assert.Equal(0, await page.Locator("." + prefix + "-html-hovered,." + prefix + "-html-hover-muted,." + prefix + "-html-hover-related").CountAsync());
            Assert.True(await page.Locator("[data-cfx-role=topology-node][data-node-id=b]").EvaluateAsync<bool>("node=>node.getAttribute('aria-selected')==='true'"));
            await controls.ClickAsync();
            await page.WaitForFunctionAsync("expected=>topologySteps.length>expected", stopped);
            await page.Locator("[data-cfx-role=topology-node][data-node-id=a]").HoverAsync();
            Assert.Equal(1, await page.EvaluateAsync<int>("()=>ChartForgeXTopology.dispose(retainedTopology).length"));
            Assert.Equal(0, await page.EvaluateAsync<int>("()=>ChartForgeXTopology.dispose(retainedTopology).length"));
            stopped = await page.EvaluateAsync<int>("()=>topologySteps.length");
            await page.WaitForTimeoutAsync(250);
            Assert.Equal(stopped, await page.EvaluateAsync<int>("()=>topologySteps.length"));
            await AssertReleasedAsync(page, "topology-global-dispose-" + cycle);
            await MoveAwayAsync(page);
        }
        await page.EvaluateAsync("()=>ChartForgeXTopology.mount(retainedTopology)");
        await page.Locator("[data-cfx-role=topology-node][data-node-id=a]").FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        var wrapper = page.Locator("." + prefix + "-wrapper");
        Assert.Equal("a", await wrapper.GetAttributeAsync("data-cfx-selection-id"));
        await page.EvaluateAsync("()=>{ChartForgeXTopology.dispose(retainedTopology);ChartForgeXTopology.mount(retainedTopology);}");
        Assert.Equal("a", await wrapper.GetAttributeAsync("data-cfx-selection-id"));
        Assert.Equal("true", await page.Locator("[data-cfx-role=topology-node][data-node-id=a]").GetAttributeAsync("aria-selected"));
        await CaptureAsync(page, "topology-" + prefix + "-committed-selection");
        await page.EvaluateAsync("""
            () => {
                retainedTopology.dispatchEvent(new CustomEvent('cfx-topology-set-selection',{detail:{kind:'node',ids:['a','b']}}));
                ChartForgeXTopology.dispose(retainedTopology);ChartForgeXTopology.mount(retainedTopology);
            }
            """);
        Assert.Equal(new[] { "a", "b" }, await page.Locator("[data-cfx-role=topology-node][aria-selected=true]").EvaluateAllAsync<string[]>("nodes=>nodes.map(node=>node.getAttribute('data-node-id')).sort()"));
        await page.Locator("[data-cfx-role=topology-node][data-node-id=a]").PressAsync("Escape");
        await page.EvaluateAsync("()=>{ChartForgeXTopology.dispose(retainedTopology);ChartForgeXTopology.mount(retainedTopology);}");
        Assert.Null(await wrapper.GetAttributeAsync("data-cfx-selection-id"));
        Assert.Equal(0, await page.Locator("[data-cfx-role=topology-node][aria-selected=true]").CountAsync());
        await page.Locator("[data-cfx-topology-reset]").ClickAsync();
        await page.Locator("[data-cfx-topology-zoom=in]").ClickAsync();
        Assert.Equal(1.2, double.Parse((await wrapper.GetAttributeAsync("data-cfx-topology-zoom"))!, System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(0, double.Parse((await wrapper.GetAttributeAsync("data-cfx-topology-pan-x"))!, System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(0, double.Parse((await wrapper.GetAttributeAsync("data-cfx-topology-pan-y"))!, System.Globalization.CultureInfo.InvariantCulture));
        await CaptureAsync(page, "topology-" + prefix + "-remounted");
        await page.EvaluateAsync("()=>ChartForgeXTopology.dispose(retainedTopology)");
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(HtmlGraphRenderBackend.Svg)]
    [InlineData(HtmlGraphRenderBackend.Canvas)]
    [InlineData(HtmlGraphRenderBackend.WebGl)]
    public async Task GraphRemovalReleasesPreferencesAndRemountRestoresOneSelectionHandler(HtmlGraphRenderBackend backend) {
        if (!Enabled) return;
        var graph = GraphScene.Create("graph-lifetime", "Graph lifetime")
            .AddNode("a", "API", node => { node.Fixed = true; node.X = 200; node.Y = 180; node.BadgeText = "+1234567890"; })
            .AddNode("b", "Database", node => { node.Fixed = true; node.X = 500; node.Y = 180; });
        graph.Options.Features = GraphSceneFeatures.Selection | GraphSceneFeatures.Viewport;
        var html = graph.ToGraphExplorerHtmlPage(options => { options.RenderBackend = backend; options.PersistThemePreference = false; });
        await using var session = await OpenAsync(Instrument(html), 900, 720);
        var page = session.Page;
        await page.EvaluateAsync("()=>lifetimeProbe.recordObservers=false");
        var actualRenderer = await page.Locator(".cfx-graph-explorer").GetAttributeAsync("data-cfx-graph-renderer-active");
        await WriteReceiptAsync("graph-" + backend + "-backend", JsonSerializer.SerializeToElement(new { requested = backend.ToString(), actual = actualRenderer, engine = Engine.ToString() }));
        await page.EvaluateAsync("""
            () => {
                window.retainedGraph=document.querySelector('.cfx-graph-explorer');
                window.graphSelections=0;retainedGraph.addEventListener('cfxgraphselection',()=>graphSelections++);
                lifetimeProbe.recordGlobals=false;lifetimeProbe.recordObservers=false;
                // Removing a shadow host must release an adapter mounted inside its own tree too.
                window.graphHost=document.createElement('div'); document.body.append(graphHost);
                graphHost.attachShadow({mode:'open'}).append(retainedGraph);
            }
            """);
        await page.EvaluateAsync("()=>graphHost.remove()");
        await page.WaitForFunctionAsync("()=>!retainedGraph.hasAttribute('data-cfx-graph-bound')");
        await AssertReleasedAsync(page, "graph-" + backend + "-removal");
        Assert.False(await page.EvaluateAsync<bool>("()=>ChartForgeXGraphExplorer.dispose(retainedGraph)"));
        Assert.Null(await page.EvaluateAsync<object?>("()=>ChartForgeXGraphExplorer.get(retainedGraph)"));
        for (var cycle = 0; cycle < 3; cycle++) {
            await page.EvaluateAsync("""
                () => {
                    document.body.append(retainedGraph); lifetimeProbe.recordGlobals=true;lifetimeProbe.recordObservers=true;
                    if(ChartForgeXGraphExplorer.mount(retainedGraph).length!==1||ChartForgeXGraphExplorer.mount('graph-lifetime').length!==1)
                        throw new Error('Idempotent mount lost its graph');
                    lifetimeProbe.recordGlobals=false;lifetimeProbe.recordObservers=false;
                    if(lifetimeProbe.media.reduce((count,entry)=>count+entry.listeners.filter(listener=>listener.active).length,0)!==4)
                        throw new Error('Mount duplicated media listeners');
                    ChartForgeXGraphExplorer.clearSelection(retainedGraph);
                    graphSelections=0;
                    ChartForgeXGraphExplorer.select(retainedGraph,[{role:'graph-node',id:'a'}],{emit:true});
                }
                """);
            Assert.Equal(1, await page.EvaluateAsync<int>("()=>graphSelections"));
            Assert.Equal(1, await page.EvaluateAsync<int>("()=>ChartForgeXGraphExplorer.get(retainedGraph).selection.count"));
            await page.EvaluateAsync("()=>ChartForgeXGraphExplorer.clearSelection(retainedGraph)");
            var renderer = await page.Locator(".cfx-graph-explorer").GetAttributeAsync("data-cfx-graph-renderer-active");
            if (renderer == "svg") {
                await page.Locator("[data-cfx-role=graph-node][data-node-id=a]").FocusAsync();
                await page.Keyboard.PressAsync("Space");
            } else {
                await page.Locator("[data-cfx-role=graph-" + renderer + "]").FocusAsync();
                await page.Keyboard.PressAsync("Home");
            }
            Assert.Equal("a", await page.EvaluateAsync<string>("()=>ChartForgeXGraphExplorer.get(retainedGraph).selection.primary"));
            await page.EvaluateAsync("()=>ChartForgeXGraphExplorer.theme(retainedGraph,'dark')");
            Assert.Equal("dark", await page.Locator(".cfx-graph-explorer").GetAttributeAsync("data-cfx-graph-theme-active"));
            await CaptureAsync(page, "graph-" + backend + "-remount-" + cycle);
            Assert.True(await page.EvaluateAsync<bool>("()=>ChartForgeXGraphExplorer.dispose(retainedGraph)"));
            await AssertReleasedAsync(page, "graph-" + backend + "-dispose-" + cycle);
        }
        AssertNoConsoleErrors(session);
    }

    private static TopologyChart Topology() => TopologyChart.Create().WithId("loop-lifetime").WithViewport(720, 400)
        .AddNode("a", "API", 100, 130).AddNode("b", "Database", 430, 130)
        .AddScenario("route", "Request route", route => route.WithPlayback(200, loop: true).AddNodeStep("a").AddNodeStep("b"));

    private static async Task AssertReleasedAsync(IPage page, string name) {
        var result = await page.EvaluateAsync<JsonElement>("""
            async () => {
                await Promise.resolve();
                const root=window.retainedGraph||window.retainedTopology;
                const before=JSON.stringify([...root.attributes].map(value=>[value.name,value.value]));
                for(const entry of lifetimeProbe.media)entry.media.dispatchEvent(new Event('change'));
                document.dispatchEvent(new Event('fullscreenchange'));
                await Promise.resolve();
                return {globals:lifetimeProbe.globals.filter(entry=>entry.active).length,
                    media:lifetimeProbe.media.reduce((count,entry)=>count+entry.listeners.filter(listener=>listener.active).length,0),
                    timers:lifetimeProbe.timers.size,frames:lifetimeProbe.frames.size,observers:lifetimeProbe.observers.size,
                    externalObservers:lifetimeProbe.externalObservers.size,
                    unchanged:before===JSON.stringify([...root.attributes].map(value=>[value.name,value.value]))};
            }
            """);
        await WriteReceiptAsync(name, result);
        Assert.Equal(0, result.GetProperty("globals").GetInt32());
        Assert.Equal(0, result.GetProperty("media").GetInt32());
        Assert.Equal(0, result.GetProperty("timers").GetInt32());
        Assert.Equal(0, result.GetProperty("frames").GetInt32());
        Assert.Equal(0, result.GetProperty("observers").GetInt32());
        Assert.True(result.GetProperty("unchanged").GetBoolean());
    }

    private static async Task CaptureAsync(IPage page, string name) {
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output);
        await page.ScreenshotAsync(new() { Path = Path.Combine(output, name + ".png"), FullPage = true });
    }

    private static async Task WriteReceiptAsync(string name, JsonElement value) {
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output);
        await File.WriteAllTextAsync(Path.Combine(output, name + ".json"), value.GetRawText());
    }
}
