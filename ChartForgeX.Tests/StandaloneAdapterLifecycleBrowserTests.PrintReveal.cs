using System.Text.Json;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class StandaloneAdapterLifecycleBrowserTests {
    [BrowserTheory]
    [InlineData(HtmlGraphRenderBackend.Svg, false)]
    [InlineData(HtmlGraphRenderBackend.Canvas, false)]
    [InlineData(HtmlGraphRenderBackend.WebGl, false)]
    [InlineData(HtmlGraphRenderBackend.Canvas, true)]
    [InlineData(HtmlGraphRenderBackend.WebGl, true)]
    public async Task GraphPrintHiddenExitRepaintsOnSameSizeRevealUnlessDisposed(HtmlGraphRenderBackend backend, bool disposeBeforeReveal) {
        if (!Enabled) return;
        var graph = GraphPrintRevealFixture(backend);
        await using var session = await OpenAsync(Instrument(graph.ToGraphExplorerHtmlPage(options => {
            options.RenderBackend = backend; options.Theme = HtmlGraphExplorerTheme.Light;
            options.PersistThemePreference = false; options.FillAvailableHeight = true;
        })), 980, 720);
        var page = session.Page;
        var name = "graph-print-hidden-exit-" + backend + (disposeBeforeReveal ? "-disposed" : "-active");
        await page.EvaluateAsync("""
            ()=>{
                window.retainedGraph=document.querySelector('.cfx-graph-explorer');
                ChartForgeXGraphExplorer.applyState(retainedGraph,{version:1,viewport:{x:55,y:40,scale:1.8},selection:[{role:'graph-node',id:'n0'}]});
                ChartForgeXGraphExplorer.highlight(retainedGraph,[{role:'graph-node',id:'n2'}],{dim:true});
                window.graphPrintRevealViewportEvents=[];
                retainedGraph.addEventListener('cfxgraphviewport',event=>graphPrintRevealViewportEvents.push(event.detail));
                lifetimeProbe.recordGlobals=false;lifetimeProbe.recordObservers=false;
            }
            """);
        var actual = await page.Locator(".cfx-graph-explorer").GetAttributeAsync("data-cfx-graph-renderer-active");
        if (backend == HtmlGraphRenderBackend.WebGl) Assert.Contains(actual, new[] { "webgl", "canvas" });
        else Assert.Equal(backend.ToString().ToLowerInvariant(), actual);
        await WriteReceiptAsync(name + "-backend", JsonSerializer.SerializeToElement(new { requested = backend.ToString(), actual, engine = Engine.ToString() }));
        var original = await GraphCommittedStateAsync(page);
        var originalGeometry = await GraphPrintRetainedGeometryAsync(page);
        var dimmed = await ReadGraphPrintScreenPaintAsync(page, name + "-dimmed-before");
        Assert.True(dimmed.GetProperty("sourceDimmed").GetBoolean());
        Assert.All(dimmed.GetProperty("samples").EnumerateArray(), sample => Assert.Equal(0, sample.GetProperty("solidPixels").GetInt32()));
        var originalSize = await GraphPrintRevealStageSizeAsync(page);
        await page.EmulateMediaAsync(new() { Media = Media.Print });
        await WaitForGraphPrintSurfaceAsync(page);
        // The screen buffer is dimmed, while committed paint changes during print.
        // Hide the host before media exit so the print cleanup cannot paint yet.
        await page.EvaluateAsync("""
            ()=>{
                ChartForgeXGraphExplorer.highlight(retainedGraph,[{role:'graph-node',id:'n2'}]);
                retainedGraph.hidden=true;
            }
            """);
        await page.EmulateMediaAsync(new() { Media = Media.Screen });
        await page.WaitForFunctionAsync("()=>!document.querySelector('[data-cfx-role=graph-print-surface]')");
        Assert.False(await page.Locator(".cfx-graph-explorer").IsVisibleAsync());
        Assert.Equal(original, await GraphCommittedStateAsync(page));
        if (disposeBeforeReveal) await page.EvaluateAsync("()=>ChartForgeXGraphExplorer.dispose(retainedGraph)");
        await page.EvaluateAsync("()=>retainedGraph.hidden=false");
        var revealed = await ReadGraphPrintScreenPaintAsync(page, name + "-revealed", disposeBeforeReveal ? dimmed : null);
        Assert.Equal(originalSize, await GraphPrintRevealStageSizeAsync(page));
        Assert.Equal(originalGeometry, await GraphPrintRetainedGeometryAsync(page));
        if (!disposeBeforeReveal) Assert.Equal(original, await GraphCommittedStateAsync(page));
        Assert.False(revealed.GetProperty("sourceDimmed").GetBoolean());
        Assert.Equal(0, await page.EvaluateAsync<int>("()=>graphPrintRevealViewportEvents.length"));
        if (disposeBeforeReveal) {
            // Teardown must leave the stale live buffer alone. Compare actual
            // body pixels, rather than accepting a cleared or repainted canvas.
            if (backend != HtmlGraphRenderBackend.Svg)
                Assert.Equal(GraphPrintPixelPatches(dimmed), GraphPrintPixelPatches(revealed));
        } else {
            Assert.All(revealed.GetProperty("samples").EnumerateArray(), sample => Assert.True(sample.GetProperty("solidPixels").GetInt32() > 0,
                "A same-size reveal must paint the committed full-opacity marks after hidden print exit: " + revealed.GetRawText()));
            await page.EvaluateAsync("()=>ChartForgeXGraphExplorer.dispose(retainedGraph)");
        }
        await AssertReleasedAsync(page, name + "-released");
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(HtmlGraphRenderBackend.Svg, false)]
    [InlineData(HtmlGraphRenderBackend.Canvas, false)]
    [InlineData(HtmlGraphRenderBackend.WebGl, false)]
    [InlineData(HtmlGraphRenderBackend.Svg, true)]
    [InlineData(HtmlGraphRenderBackend.Canvas, true)]
    [InlineData(HtmlGraphRenderBackend.WebGl, true)]
    public async Task GraphPrintHiddenExitKeepsNormalChangedSizeCameraPolicy(HtmlGraphRenderBackend backend, bool touchedCamera) {
        if (!Enabled) return;
        var graph = GraphPrintRevealFixture(backend);
        await using var session = await OpenAsync(Instrument(graph.ToGraphExplorerHtmlPage(options => {
            options.RenderBackend = backend; options.Theme = HtmlGraphExplorerTheme.Light;
            options.PersistThemePreference = false; options.FillAvailableHeight = true;
        })), 980, 720);
        var page = session.Page;
        var name = "graph-print-changed-reveal-" + backend + (touchedCamera ? "-touched" : "-untouched");
        await page.EvaluateAsync("""
            touched=>{
                window.retainedGraph=document.querySelector('.cfx-graph-explorer');
                ChartForgeXGraphExplorer.select(retainedGraph,[{role:'graph-node',id:'n0'}]);
                if(touched)ChartForgeXGraphExplorer.applyState(retainedGraph,{version:1,viewport:{x:55,y:40,scale:1.8},selection:[{role:'graph-node',id:'n0'}]});
                ChartForgeXGraphExplorer.highlight(retainedGraph,[{role:'graph-node',id:'n2'}],{dim:true});
                window.graphPrintChangedViewportEvents=[];
                retainedGraph.addEventListener('cfxgraphviewport',event=>graphPrintChangedViewportEvents.push(event.detail));
                lifetimeProbe.recordGlobals=false;lifetimeProbe.recordObservers=false;
            }
            """, touchedCamera);
        await page.EvaluateAsync("()=>new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve)))");
        Assert.Equal(touchedCamera, await page.EvaluateAsync<bool>("()=>retainedGraph.__cfxGraphViewportTouched===true"));
        var before = JsonSerializer.Deserialize<JsonElement>(await GraphCommittedStateAsync(page));
        var beforeSize = JsonSerializer.Deserialize<JsonElement>(await GraphPrintRevealStageSizeAsync(page));
        await page.EmulateMediaAsync(new() { Media = Media.Print });
        await WaitForGraphPrintSurfaceAsync(page);
        await page.EvaluateAsync("""
            ()=>{ChartForgeXGraphExplorer.highlight(retainedGraph,[{role:'graph-node',id:'n2'}]);retainedGraph.hidden=true;}
            """);
        await page.EmulateMediaAsync(new() { Media = Media.Screen });
        await page.WaitForFunctionAsync("()=>!document.querySelector('[data-cfx-role=graph-print-surface]')");
        await page.EvaluateAsync("()=>{retainedGraph.style.width='780px';retainedGraph.hidden=false;}");
        var paint = await ReadGraphPrintScreenPaintAsync(page, name + "-revealed");
        var after = JsonSerializer.Deserialize<JsonElement>(await GraphCommittedStateAsync(page));
        var afterSize = JsonSerializer.Deserialize<JsonElement>(await GraphPrintRevealStageSizeAsync(page));
        var viewportEvents = await page.EvaluateAsync<JsonElement>("()=>graphPrintChangedViewportEvents");
        var expectedViewport = touchedCamera ? before.GetProperty("viewport") : await page.EvaluateAsync<JsonElement>("""
            ()=>{ChartForgeXGraphExplorer.fit(retainedGraph);return ChartForgeXGraphExplorer.captureState(retainedGraph).viewport;}
            """);
        await WriteReceiptAsync(name + "-camera", JsonSerializer.SerializeToElement(new {
            requested = backend.ToString(), actual = paint.GetProperty("samples")[0].GetProperty("witness").GetProperty("actual"),
            touchedCamera, beforeSize, afterSize, before = before.GetProperty("viewport"), after = after.GetProperty("viewport"), expectedViewport, viewportEvents
        }));
        Assert.True(beforeSize.GetProperty("width").GetDouble() - afterSize.GetProperty("width").GetDouble() > 100);
        Assert.Equal(before.GetProperty("positions").GetRawText(), after.GetProperty("positions").GetRawText());
        Assert.Equal(before.GetProperty("selection").GetRawText(), after.GetProperty("selection").GetRawText());
        foreach (var axis in new[] { "x", "y", "scale" })
            Assert.Equal(expectedViewport.GetProperty(axis).GetDouble(), after.GetProperty("viewport").GetProperty(axis).GetDouble());
        Assert.Equal(touchedCamera ? 0 : 1, viewportEvents.GetArrayLength());
        Assert.False(paint.GetProperty("sourceDimmed").GetBoolean());
        Assert.All(paint.GetProperty("samples").EnumerateArray(), sample => Assert.True(sample.GetProperty("solidPixels").GetInt32() > 0));
        await page.EvaluateAsync("()=>ChartForgeXGraphExplorer.dispose(retainedGraph)");
        await AssertReleasedAsync(page, name + "-released");
        AssertNoConsoleErrors(session);
    }

    private static GraphScene GraphPrintRevealFixture(HtmlGraphRenderBackend backend) {
        var graph = GraphScene.Create("graph-print-hidden-exit", "Services");
        for (var index = 0; index < 3; index++) {
            var position = index;
            graph.AddNode("n" + index, "Service " + index, node => {
                node.Fixed = true; node.X = 100 + position * 150; node.Y = 100; node.Size = 16;
                if (position == 1) node.Style.BackgroundColor = "#16a34a";
            });
        }
        graph.Options.Features = GraphSceneFeatures.Selection | GraphSceneFeatures.Viewport | GraphSceneFeatures.StateSnapshots;
        if (backend != HtmlGraphRenderBackend.Svg) {
            graph.Options.Features |= GraphSceneFeatures.LevelOfDetail;
            graph.Options.LevelOfDetail.CanvasPreferredNodeThreshold = 1;
        }
        return graph;
    }

    private static string[] GraphPrintPixelPatches(JsonElement facts) => facts.GetProperty("samples").EnumerateArray()
        .Select(sample => sample.GetProperty("pixels").GetString()!).ToArray();

    private static Task<string> GraphPrintRetainedGeometryAsync(IPage page) => page.EvaluateAsync<string>("""
        ()=>JSON.stringify({camera:['x','y','scale'].map(name=>retainedGraph.getAttribute('data-cfx-viewport-'+name)),
            positions:retainedGraph.__cfxGraphState.nodes.map(node=>({id:node.id,x:node.x,y:node.y}))})
        """);

    private static Task<string> GraphPrintRevealStageSizeAsync(IPage page) => page.EvaluateAsync<string>("""
        ()=>{const box=retainedGraph.querySelector('.cfx-graph-stage').getBoundingClientRect();return JSON.stringify({width:box.width,height:box.height});}
        """);
}
