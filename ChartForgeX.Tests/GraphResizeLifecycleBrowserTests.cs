using System.Text.Json;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Protects graph camera ownership when mounting and observing host layout.</summary>
[Trait("BrowserSuite", "Compatibility")]
public sealed class GraphResizeLifecycleBrowserTests {
    [BrowserTheory]
    [InlineData(HtmlGraphRenderBackend.Svg)]
    [InlineData(HtmlGraphRenderBackend.Canvas)]
    [InlineData(HtmlGraphRenderBackend.WebGl)]
    public async Task ImmediateSearchAfterRemountDoesNotTurnInitialResizeNotificationIntoFit(HtmlGraphRenderBackend backend) {
        await using var session = await OpenAsync(Page(backend), 390, 850);
        var page = session.Page;
        var facts = await page.EvaluateAsync<JsonElement>("""
            async()=>{
                await document.fonts.ready;
                const frame=()=>new Promise(resolve=>requestAnimationFrame(resolve));
                await frame();await frame();
                const root=document.querySelector('.cfx-graph-explorer'),stage=root.querySelector('.cfx-graph-stage'),
                    search=root.querySelector('[data-cfx-graph-search]'),NativeResize=window.ResizeObserver,
                    camera=()=>{const view=ChartForgeXGraphExplorer.captureState(root).viewport;return [view.x,view.y,view.scale]},
                    positions=()=>ChartForgeXGraphExplorer.positions(root).map(node=>[node.id,node.x,node.y]);
                let notifications=0;
                // Observe the browser's real initial delivery without replacing its scheduling.
                window.ResizeObserver=class extends NativeResize {
                    constructor(callback){super((entries,observer)=>{
                        if(entries.some(entry=>entry.target===stage))notifications++;
                        callback(entries,observer);
                    });}
                };
                try {
                    ChartForgeXGraphExplorer.dispose(root);ChartForgeXGraphExplorer.mount(root);
                    const mounted=camera(),mountedPositions=positions();
                    // No Enter key or explicit Fit: changing the result set owns no camera move.
                    search.value='Singleton destination';search.dispatchEvent(new Event('input',{bubbles:true}));
                    const filteredMatches=+root.dataset.cfxGraphSearchMatches;
                    await frame();await frame();await frame();
                    const filtered=camera();
                    search.value='';search.dispatchEvent(new Event('input',{bubbles:true}));
                    await frame();
                    const restoredPositions=positions();
                    return {mounted,filtered,restored:camera(),positions:mountedPositions,restoredPositions,
                        positionsUnchanged:JSON.stringify(mountedPositions)===JSON.stringify(restoredPositions),notifications,filteredMatches,
                        restoredMatches:+root.dataset.cfxGraphSearchMatches,renderer:root.dataset.cfxGraphRendererActive};
                } finally {window.ResizeObserver=NativeResize;}
            }
            """);
        var point = await NodePointAsync(page, "a");
        await CaptureAsync(page, "remount-search-" + backend, new { facts, point });
        Assert.True(facts.GetProperty("notifications").GetInt32() > 0, facts.GetRawText());
        Assert.Equal(backend.ToString().ToLowerInvariant(), facts.GetProperty("renderer").GetString());
        Assert.Equal(1, facts.GetProperty("filteredMatches").GetInt32());
        Assert.Equal(2, facts.GetProperty("restoredMatches").GetInt32());
        Assert.Equal(Numbers(facts.GetProperty("mounted")), Numbers(facts.GetProperty("filtered")));
        Assert.Equal(Numbers(facts.GetProperty("mounted")), Numbers(facts.GetProperty("restored")));
        Assert.True(facts.GetProperty("positionsUnchanged").GetBoolean(), facts.GetRawText());
        Assert.True(point.GetProperty("hit").GetBoolean(), point.GetRawText());
        await page.Mouse.ClickAsync((float)point.GetProperty("x").GetDouble(), (float)point.GetProperty("y").GetDouble());
        Assert.Equal("a", await page.EvaluateAsync<string>("()=>ChartForgeXGraphExplorer.get(document.querySelector('.cfx-graph-explorer')).selection.primary"));
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(HtmlGraphRenderBackend.Svg)]
    [InlineData(HtmlGraphRenderBackend.Canvas)]
    [InlineData(HtmlGraphRenderBackend.WebGl)]
    public async Task HiddenRemountRefitsOnRevealAndActualResizePreservesTouchedCamera(HtmlGraphRenderBackend backend) {
        await using var session = await OpenAsync(Page(backend), 390, 850);
        var page = session.Page;
        var reveal = await page.EvaluateAsync<JsonElement>("""
            async()=>{
                await document.fonts.ready;
                const frame=()=>new Promise(resolve=>requestAnimationFrame(resolve));
                await frame();await frame();
                const root=document.querySelector('.cfx-graph-explorer'),stage=root.querySelector('.cfx-graph-stage'),
                    camera=()=>{const view=ChartForgeXGraphExplorer.captureState(root).viewport;return [view.x,view.y,view.scale]};
                ChartForgeXGraphExplorer.dispose(root);root.style.display='none';
                ChartForgeXGraphExplorer.mount(root);
                const hidden={width:stage.getBoundingClientRect().width,viewport:camera()};
                root.style.width='310px';root.style.display='';
                await frame();await frame();await frame();
                return {hidden,width:stage.getBoundingClientRect().width,
                    viewport:camera(),renderer:root.dataset.cfxGraphRendererActive};
            }
            """);
        var source = await NodePointAsync(page, "a");
        var target = await NodePointAsync(page, "b");
        await CaptureAsync(page, "hidden-remount-" + backend, new { reveal, source, target });
        Assert.Equal(backend.ToString().ToLowerInvariant(), reveal.GetProperty("renderer").GetString());
        Assert.Equal(0, reveal.GetProperty("hidden").GetProperty("width").GetDouble());
        Assert.True(reveal.GetProperty("width").GetDouble() > 0, reveal.GetRawText());
        Assert.False(Numbers(reveal.GetProperty("hidden").GetProperty("viewport")).SequenceEqual(Numbers(reveal.GetProperty("viewport"))), reveal.GetRawText());
        Assert.True(source.GetProperty("hit").GetBoolean(), source.GetRawText());
        Assert.True(target.GetProperty("hit").GetBoolean(), target.GetRawText());
        await page.GetByRole(AriaRole.Button, new() { Name = "Zoom in", Exact = true }).ClickAsync();
        var touched = await CameraAsync(page);
        await page.EvaluateAsync("""
            async()=>{document.querySelector('.cfx-graph-explorer').style.width='350px';
                await new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(()=>requestAnimationFrame(resolve))));}
            """);
        Assert.Equal(touched, await CameraAsync(page));
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(HtmlGraphRenderBackend.Svg)]
    [InlineData(HtmlGraphRenderBackend.Canvas)]
    [InlineData(HtmlGraphRenderBackend.WebGl)]
    public async Task PrintingAnUntouchedFitDoesNotRefitTheRestoredScreenFrame(HtmlGraphRenderBackend backend) {
        await using var session = await OpenAsync(Page(backend), 980, 900);
        var page = session.Page;
        await page.EmulateMediaAsync(new() { Media = Media.Screen, ForcedColors = ForcedColors.Active, ReducedMotion = ReducedMotion.Reduce });
        await page.EvaluateAsync("()=>document.fonts.ready");
        await page.GetByRole(AriaRole.Button, new() { Name = "Fit graph to view", Exact = true }).ClickAsync();
        await page.EvaluateAsync("""
            async()=>{
                await new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve)));
                const root=document.querySelector('.cfx-graph-explorer'),stage=root.querySelector('.cfx-graph-stage');
                const snapshot=()=>{
                    const camera=ChartForgeXGraphExplorer.captureState(root).viewport,box=stage.getBoundingClientRect();
                    return {camera:[camera.x,camera.y,camera.scale],width:box.width,height:box.height,
                        positions:ChartForgeXGraphExplorer.positions(root).map(node=>[node.id,node.x,node.y]),print:matchMedia('print').matches};
                };
                window.printResizeProof={before:snapshot(),events:[],sizes:[],snapshot};
                root.addEventListener('cfxgraphviewport',()=>printResizeProof.events.push(snapshot()));
                printResizeProof.observer=new ResizeObserver(()=>printResizeProof.sizes.push(snapshot()));
                printResizeProof.observer.observe(stage);
            }
            """);
        await page.EmulateMediaAsync(new() { Media = Media.Print, ForcedColors = ForcedColors.Active, ReducedMotion = ReducedMotion.Reduce });
        await page.WaitForFunctionAsync("()=>!!document.querySelector('[data-cfx-role=graph-print-surface]')");
        await page.EvaluateAsync("()=>new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve)))");
        await page.EmulateMediaAsync(new() { Media = Media.Screen, ForcedColors = ForcedColors.Active, ReducedMotion = ReducedMotion.Reduce });
        await page.WaitForFunctionAsync("()=>!document.querySelector('[data-cfx-role=graph-print-surface]')");
        var facts = await page.EvaluateAsync<JsonElement>("""
            async()=>{
                await new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(()=>requestAnimationFrame(resolve))));
                const before=printResizeProof.before,after=printResizeProof.snapshot();
                return {before,after,statePreserved:JSON.stringify(before)===JSON.stringify(after),events:printResizeProof.events,sizes:printResizeProof.sizes,
                    renderer:document.querySelector('.cfx-graph-explorer').dataset.cfxGraphRendererActive};
            }
            """);
        await CaptureAsync(page, "print-untouched-" + backend, facts);
        Assert.Equal(backend.ToString().ToLowerInvariant(), facts.GetProperty("renderer").GetString());
        Assert.True(facts.GetProperty("statePreserved").GetBoolean(), facts.GetRawText());
        Assert.Empty(facts.GetProperty("events").EnumerateArray());

        // A real host resize during print still belongs to the screen resize owner on return.
        await page.EmulateMediaAsync(new() { Media = Media.Print, ForcedColors = ForcedColors.Active, ReducedMotion = ReducedMotion.Reduce });
        await page.WaitForFunctionAsync("()=>!!document.querySelector('[data-cfx-role=graph-print-surface]')");
        await page.EvaluateAsync("()=>document.querySelector('.cfx-graph-explorer').style.width='310px'");
        await page.EmulateMediaAsync(new() { Media = Media.Screen, ForcedColors = ForcedColors.Active, ReducedMotion = ReducedMotion.Reduce });
        await page.WaitForFunctionAsync("()=>!document.querySelector('[data-cfx-role=graph-print-surface]')&&printResizeProof.events.length>0");
        var resizedCamera = await CameraAsync(page);
        Assert.False(Numbers(facts.GetProperty("before").GetProperty("camera")).SequenceEqual(resizedCamera));
        await page.EvaluateAsync("()=>printResizeProof.observer.disconnect()");
        AssertNoConsoleErrors(session);
    }

    private static string Page(HtmlGraphRenderBackend backend) {
        var graph = GraphScene.Create("resize-lifecycle", "Services")
            .AddNode("a", "Source", node => { node.Fixed = true; node.X = 200; node.Y = 170; node.Size = 24; })
            .AddNode("b", "Singleton destination", node => { node.Fixed = true; node.X = 500; node.Y = 170; node.Size = 24; })
            .AddEdge("connection", "a", "b");
        graph.Options.Features = GraphSceneFeatures.Viewport | GraphSceneFeatures.Selection | GraphSceneFeatures.Search;
        return graph.ToGraphExplorerHtmlPage(options => {
            options.RenderBackend = backend; options.Theme = HtmlGraphExplorerTheme.Light; options.PersistThemePreference = false;
        });
    }

    private static Task<double[]> CameraAsync(IPage page) => page.EvaluateAsync<double[]>(
        "()=>{const view=ChartForgeXGraphExplorer.captureState(document.querySelector('.cfx-graph-explorer')).viewport;return [view.x,view.y,view.scale]}");

    private static double[] Numbers(JsonElement values) => values.EnumerateArray().Select(value => value.GetDouble()).ToArray();

    private static Task<JsonElement> NodePointAsync(IPage page, string id) => page.EvaluateAsync<JsonElement>("""
        id=>{const root=document.querySelector('.cfx-graph-explorer'),actual=root.dataset.cfxGraphRendererActive,
            surface=root.querySelector('[data-cfx-role="graph-'+(actual==='svg'?'scene':actual)+'"]'),box=surface.getBoundingClientRect(),
            view=root.querySelector('[data-cfx-role=graph-scene]').viewBox.baseVal,
            position=ChartForgeXGraphExplorer.positions(root).find(node=>node.id===id),camera=ChartForgeXGraphExplorer.captureState(root).viewport,
            scale=Math.min(box.width/view.width,box.height/view.height),
            x=box.left+(box.width-view.width*scale)/2+(position.x*camera.scale+camera.x)*scale,
            y=box.top+(box.height-view.height*scale)/2+(position.y*camera.scale+camera.y)*scale,hit=document.elementFromPoint(x,y);
            return {x,y,hit:actual==='svg'?hit?.closest('[data-node-id]')?.dataset.nodeId===id:hit===surface};}
        """, id);

    private static async Task CaptureAsync(IPage page, string name, object facts) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, "graph-resize-" + name + ".json"), JsonSerializer.Serialize(facts));
        await page.ScreenshotAsync(new() { Path = Path.Combine(directory, "graph-resize-" + name + ".png"), FullPage = true });
    }
}
