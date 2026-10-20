using System.Collections.Concurrent;
using System.Text.Json;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Raster;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class StandaloneAdapterLifecycleBrowserTests {
    [BrowserTheory]
    [InlineData(HtmlGraphRenderBackend.Svg, false, 980)]
    [InlineData(HtmlGraphRenderBackend.Canvas, true, 390)]
    [InlineData(HtmlGraphRenderBackend.WebGl, false, 980)]
    public async Task GraphPrintMaterializesTheCommittedSceneAndReleasesItsSnapshotWithoutChangingTheCamera(
        HtmlGraphRenderBackend backend, bool dark, int width) {
        if (!Enabled) return;
        var graph = GraphScene.Create("graph-print", "Service map");
        graph.Subtitle = "Committed services and their relationships";
        for (var index = 0; index < 24; index++) {
            var position = index;
            graph.AddNode("n" + index, "Service " + index, node => {
                node.Fixed = true; node.X = position == 23 ? 1350 : 100 + position % 6 * 150;
                node.Y = position == 23 ? 650 : 100 + position / 6 * 110; node.Size = 16;
                if (position == 0) { node.Status = "HeAlThY"; node.BadgeText = "3"; node.SecondaryLabel = "Selected service"; }
                if (position == 2) { node.Status = "WaRnInG"; node.BadgeText = "7"; node.SecondaryLabel = "Highlighted service"; }
                if (position == 6) node.Status = "UNKNOWN";
                if (position == 1) node.Status = "CrItIcAl";
                if (position == 1) { node.Style.BackgroundColor = "#16a34a"; node.Style.BorderColor = "#0f766e"; }
            });
            if (index > 0) graph.AddEdge("e" + index, "n" + (index - 1), "n" + index, configure: edge => {
                edge.Directed = true;
                if (position is 1 or 2) edge.Weight = 2;
                if (position == 8) edge.Weight = 3;
                if (position == 7) {
                    edge.Label = "Authored relationship"; edge.SourceArrow = true; edge.Dashed = true;
                    edge.Style.Color = "#7c3aed"; edge.Style.Width = 3.25; edge.Style.DashPattern = "13 7";
                    edge.Style.LabelColor = "#ffffff";
                }
            });
        }
        graph.Options.Features = GraphSceneFeatures.Selection | GraphSceneFeatures.MultiSelection | GraphSceneFeatures.Viewport | GraphSceneFeatures.Export | GraphSceneFeatures.StateSnapshots | GraphSceneFeatures.IncrementalUpdates | GraphSceneFeatures.NeighborhoodFocus;
        if (backend != HtmlGraphRenderBackend.Svg) {
            graph.Options.Features |= GraphSceneFeatures.LevelOfDetail;
            graph.Options.LevelOfDetail.CanvasPreferredNodeThreshold = 1;
        }
        var consoleEntries = new ConcurrentQueue<object>();
        await using var session = await OpenAsync(Instrument(graph.ToGraphExplorerHtmlPage(options => {
            options.RenderBackend = backend; options.Theme = dark ? HtmlGraphExplorerTheme.Dark : HtmlGraphExplorerTheme.Light;
            options.PersistThemePreference = false; options.FillAvailableHeight = true;
        })).Replace("<head>", "<head><script>" + GraphPrintContextProbe + "</script>", StringComparison.Ordinal), width, 720,
            configurePage: page => page.Console += (_, message) => consoleEntries.Enqueue(new {
                receivedUnixMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                type = message.Type, text = message.Text, location = message.Location?.ToString()
            }));
        var page = session.Page;
        await MarkGraphPrintContextPhaseAsync(page, "committed-state");
        await page.EvaluateAsync("""
            () => {
                window.retainedGraph=document.querySelector('.cfx-graph-explorer');
                ChartForgeXGraphExplorer.applyState(retainedGraph,{version:1,viewport:{x:55,y:40,scale:1.8},selection:[{role:'graph-node',id:'n0'},{role:'graph-edge',id:'e1'}]});
                ChartForgeXGraphExplorer.highlight(retainedGraph,[{role:'graph-node',id:'n2'},{role:'graph-edge',id:'e2'}]);
                lifetimeProbe.recordGlobals=false;lifetimeProbe.recordObservers=false;
            }
            """);
        var actual = await page.Locator(".cfx-graph-explorer").GetAttributeAsync("data-cfx-graph-renderer-active");
        await WriteReceiptAsync("graph-print-" + backend + "-backend", JsonSerializer.SerializeToElement(new { requested = backend.ToString(), actual, engine = Engine.ToString() }));
        if (backend == HtmlGraphRenderBackend.WebGl) Assert.Contains(actual, new[] { "webgl", "canvas" });
        else Assert.Equal(backend.ToString().ToLowerInvariant(), actual);
        await MarkGraphPrintContextPhaseAsync(page, "initial-artifacts");
        await AssertGraphEdgeUpdateAsync(page, backend, dark);
        await AssertGraphArtifactPaintAsync(page, backend, dark);
        var before = await GraphCommittedStateAsync(page);
        await MarkGraphPrintContextPhaseAsync(page, "screen-before-print");
        await CaptureAsync(page, "graph-print-" + backend + "-screen");
        var screenBacking = await GraphWebGlBackingAsync(page);
        await MarkGraphPrintContextPhaseAsync(page, "print-initial");
        await page.EmulateMediaAsync(new() { Media = Media.Print });
        await WaitForGraphPrintSurfaceAsync(page);
        Assert.Equal(screenBacking, await GraphWebGlBackingAsync(page));
        var printed = await GraphPrintFactsAsync(page);
        await WriteReceiptAsync("graph-print-" + backend + "-print", printed);
        await CaptureAsync(page, "graph-print-" + backend + "-print");
        Assert.Equal(1, printed.GetProperty("surfaces").GetInt32());
        Assert.Equal(24, printed.GetProperty("nodes").GetInt32());
        Assert.Equal(23, printed.GetProperty("edges").GetInt32());
        Assert.Equal(0, printed.GetProperty("outsideNodes").GetInt32());
        Assert.Equal(0, printed.GetProperty("outsideDetails").GetInt32());
        Assert.Equal(0, printed.GetProperty("visibleControls").GetInt32());
        Assert.Equal(0, printed.GetProperty("duplicateIds").GetInt32());
        Assert.Equal(0, printed.GetProperty("missingReferences").GetInt32());
        Assert.Equal("", printed.GetProperty("camera").GetString());
        Assert.InRange(printed.GetProperty("overflow").GetDouble(), 0, 1);
        Assert.True(printed.GetProperty("printBlockHeight").GetDouble() <= printed.GetProperty("viewportHeight").GetDouble());
        Assert.InRange(printed.GetProperty("aspect").GetDouble(), 960d / 560 - .01, 960d / 560 + .01);
        AssertGraphPrintPaint(printed.GetProperty("paint"), dark);
        AssertGraphEdgePaint(printed.GetProperty("edgePaint"), dark, false);
        AssertGraphDetailPaint(printed.GetProperty("detailPaint"), dark);
        Assert.Equal(before, await GraphCommittedStateAsync(page));
        // Change the paint only after print has hidden the stage. Forced PNG export
        // must render these new pixels rather than returning the prior screen buffer.
        await MarkGraphPrintContextPhaseAsync(page, "print-hidden-artifacts");
        await page.EvaluateAsync("()=>ChartForgeXGraphExplorer.highlight(retainedGraph,[{role:'graph-node',id:'n2'},{role:'graph-edge',id:'e2'}],{dim:true})");
        await AssertGraphArtifactPaintAsync(page, backend, dark, true);
        Assert.Equal(screenBacking, await GraphWebGlBackingAsync(page));
        await page.EvaluateAsync("()=>ChartForgeXGraphExplorer.highlight(retainedGraph,[{role:'graph-node',id:'n2'},{role:'graph-edge',id:'e2'}])");
        await MarkGraphPrintContextPhaseAsync(page, "print-before-pdf");
        await CaptureGraphPrintPdfAsync(page, backend);

        await page.EvaluateAsync("""
            ()=>{
                window.graphPrintRestoreViewportEvents=[];
                const observe=event=>graphPrintRestoreViewportEvents.push(event.detail);
                retainedGraph.addEventListener('cfxgraphviewport',observe);
                window.releaseGraphPrintRestoreViewportProbe=()=>retainedGraph.removeEventListener('cfxgraphviewport',observe);
            }
            """);
        await MarkGraphPrintContextPhaseAsync(page, "screen-initial-request");
        await page.EmulateMediaAsync(new() { Media = Media.Screen });
        await page.WaitForFunctionAsync("()=>!document.querySelector('[data-cfx-role=graph-print-surface]')");
        Assert.Equal(before, await GraphCommittedStateAsync(page));
        await MarkGraphPrintContextPhaseAsync(page, "screen-restored");
        await AssertGraphPrintScreenPaintAsync(page, backend);
        Assert.Equal(before, await GraphCommittedStateAsync(page));
        Assert.Equal(0, await page.EvaluateAsync<int>("()=>{releaseGraphPrintRestoreViewportProbe();return graphPrintRestoreViewportEvents.length;}"));
        await MarkGraphPrintContextPhaseAsync(page, "screen-restored-capture-request");
        await CaptureAsync(page, "graph-print-" + backend + "-restored");
        await MarkGraphPrintContextPhaseAsync(page, "screen-restored-capture-complete");
        await MarkGraphPrintContextPhaseAsync(page, "screen-grow-request");
        await page.SetViewportSizeAsync(width + 80, 720);
        await MarkGraphPrintContextPhaseAsync(page, "screen-grow-applied");
        await WaitForGraphScreenBackingAsync(page);
        await MarkGraphPrintContextPhaseAsync(page, "screen-grow-backing-ready");
        Assert.Equal(before, await GraphCommittedStateAsync(page));
        await MarkGraphPrintContextPhaseAsync(page, "screen-grown-capture-request");
        await CaptureAsync(page, "graph-print-" + backend + "-visible-resize");
        await MarkGraphPrintContextPhaseAsync(page, "screen-grown-capture-complete");
        await MarkGraphPrintContextPhaseAsync(page, "screen-reset-request");
        await page.SetViewportSizeAsync(width, 720);
        await MarkGraphPrintContextPhaseAsync(page, "screen-reset-applied");
        await WaitForGraphScreenBackingAsync(page);
        await MarkGraphPrintContextPhaseAsync(page, "screen-reset-backing-ready");
        await MarkGraphPrintContextPhaseAsync(page, "dim-highlight-request");
        await page.EvaluateAsync("()=>ChartForgeXGraphExplorer.highlight(retainedGraph,[{role:'graph-node',id:'n2'},{role:'graph-edge',id:'e2'}],{dim:true})");
        await MarkGraphPrintContextPhaseAsync(page, "dim-highlight-complete");
        await MarkGraphPrintContextPhaseAsync(page, "dimmed-artifacts");
        await AssertGraphArtifactPaintAsync(page, backend, dark, true);
        await MarkGraphPrintContextPhaseAsync(page, "print-dimmed-request");
        await page.EmulateMediaAsync(new() { Media = Media.Print });
        await WaitForGraphPrintSurfaceAsync(page);
        await MarkGraphPrintContextPhaseAsync(page, "print-dimmed");
        var dimmed = await GraphPrintFactsAsync(page);
        await WriteReceiptAsync("graph-print-" + backend + "-dimmed", dimmed);
        AssertGraphEdgePaint(dimmed.GetProperty("edgePaint"), dark, true);
        AssertGraphDetailPaint(dimmed.GetProperty("detailPaint"), dark);
        await CaptureAsync(page, "graph-print-" + backend + "-dimmed");
        await MarkGraphPrintContextPhaseAsync(page, "screen-dimmed-request");
        await page.EmulateMediaAsync(new() { Media = Media.Screen });
        await page.WaitForFunctionAsync("()=>!document.querySelector('[data-cfx-role=graph-print-surface]')");
        await MarkGraphPrintContextPhaseAsync(page, "primary-print-and-artifacts");
        await AssertGraphPrimaryPaintAsync(page, backend, dark);
        await MarkGraphPrintContextPhaseAsync(page, "hidden-print");
        await page.EvaluateAsync("()=>retainedGraph.hidden=true");
        Assert.False(await page.Locator(".cfx-graph-explorer").IsVisibleAsync());
        await MarkGraphPrintContextPhaseAsync(page, "hidden-print-request");
        await page.EmulateMediaAsync(new() { Media = Media.Print });
        await page.EvaluateAsync("()=>new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve)))");
        Assert.Equal(0, await page.Locator("[data-cfx-role=graph-print-surface]").CountAsync());
        await MarkGraphPrintContextPhaseAsync(page, "visible-beforeprint");
        await page.EvaluateAsync("()=>{retainedGraph.hidden=false;window.dispatchEvent(new Event('beforeprint'));}");
        await page.WaitForFunctionAsync("()=>document.querySelectorAll('[data-cfx-role=graph-print-surface]').length===1");
        await MarkGraphPrintContextPhaseAsync(page, "dispose-in-print");
        await page.EvaluateAsync("()=>ChartForgeXGraphExplorer.dispose(retainedGraph)");
        Assert.Equal(0, await page.Locator("[data-cfx-role=graph-print-surface]").CountAsync());
        await AssertReleasedAsync(page, "graph-print-" + backend + "-released");
        await page.EvaluateAsync("()=>window.dispatchEvent(new Event('beforeprint'))");
        Assert.Equal(0, await page.Locator("[data-cfx-role=graph-print-surface]").CountAsync());
        await MarkGraphPrintContextPhaseAsync(page, "remount-in-print");
        await page.EvaluateAsync("()=>{lifetimeProbe.recordGlobals=true;lifetimeProbe.recordObservers=true;ChartForgeXGraphExplorer.mount(retainedGraph);lifetimeProbe.recordGlobals=false;lifetimeProbe.recordObservers=false;}");
        Assert.Equal(1, await page.Locator("[data-cfx-role=graph-print-surface]").CountAsync());
        await MarkGraphPrintContextPhaseAsync(page, "screen-remount-request");
        await page.EmulateMediaAsync(new() { Media = Media.Screen });
        await page.WaitForFunctionAsync("()=>!document.querySelector('[data-cfx-role=graph-print-surface]')");
        await MarkGraphPrintContextPhaseAsync(page, "dispose-after-remount");
        await page.EvaluateAsync("()=>ChartForgeXGraphExplorer.dispose(retainedGraph)");
        await MarkGraphPrintContextPhaseAsync(page, "disposed-after-remount");
        await AssertReleasedAsync(page, "graph-print-" + backend + "-remount-released");
        var contextReceipt = await page.EvaluateAsync<JsonElement>("""
            ()=>{const probe=window.graphPrintContextProbe;try{probe.mark('before-console-assert');return probe.read();}finally{probe.release();}}
            """);
        await WriteReceiptAsync("graph-print-" + backend + "-context-events",
            JsonSerializer.SerializeToElement(new {
                engine = Engine.ToString(), requested = backend.ToString(), observation = contextReceipt,
                consoleCoverage = "Playwright console observations registered before adapter scripts; UTC host receipt times are not browser emission times.",
                consoleEntries = consoleEntries.ToArray()
            }));
        AssertNoConsoleErrors(session);
    }

    // The screen painter has its own buffer: a forced export cannot prove it refreshed after print.
    private static async Task AssertGraphPrintScreenPaintAsync(IPage page, HtmlGraphRenderBackend backend) {
        var facts = await ReadGraphPrintScreenPaintAsync(page, "graph-print-" + backend + "-restored-live");
        Assert.False(facts.GetProperty("sourceDimmed").GetBoolean());
        Assert.All(facts.GetProperty("samples").EnumerateArray(), sample => Assert.True(sample.GetProperty("solidPixels").GetInt32() > 0,
            "The restored screen must paint the current full-opacity mark without resizing: " + facts.GetRawText()));
    }

    private static async Task<JsonElement> ReadGraphPrintScreenPaintAsync(IPage page, string name, JsonElement? retainedPaint = null) {
        GraphEnvironmentFrame frame;
        if (retainedPaint is { } retained) {
            // Disposed adapters have no public get/positions view. Retain the
            // previously observed geometry and inspect the actual screen bytes.
            await page.EvaluateAsync("()=>new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve)))");
            var png = await page.ScreenshotAsync(new() { Animations = ScreenshotAnimations.Disabled });
            frame = new(png, retained.GetProperty("samples")[1].GetProperty("witness"));
            if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
                Assert.True(Path.IsPathRooted(output));
                Directory.CreateDirectory(output);
                await File.WriteAllBytesAsync(Path.Combine(output, name + ".png"), png);
            }
        } else frame = await CaptureGraphEnvironmentFrameAsync(page, "n1", name);
        var image = PngReader.Decode(frame.Png);
        var samples = new List<object>();
        foreach (var expected in new[] { (Id: "n0", R: 37, G: 99, B: 235), (Id: "n1", R: 22, G: 163, B: 74) }) {
            var witness = retainedPaint is { } retainedFacts
                ? retainedFacts.GetProperty("samples").EnumerateArray().Single(sample => sample.GetProperty("id").GetString() == expected.Id).GetProperty("witness")
                : expected.Id == "n1" ? frame.Witness : await GraphEnvironmentWitnessAsync(page, expected.Id);
            var sx = image.Width / witness.GetProperty("viewport").GetProperty("width").GetDouble();
            var sy = image.Height / witness.GetProperty("viewport").GetProperty("height").GetDouble();
            var x = (int)Math.Round(witness.GetProperty("center").GetProperty("x").GetDouble() * sx);
            var y = (int)Math.Round(witness.GetProperty("center").GetProperty("y").GetDouble() * sy);
            var radius = Math.Max(1, Math.Min(5, (int)(witness.GetProperty("radius").GetDouble() * Math.Min(sx, sy) / 4)));
            var pixels = 0; var patch = new List<byte>();
            for (var row = Math.Max(0, y - radius); row <= Math.Min(image.Height - 1, y + radius); row++)
                for (var column = Math.Max(0, x - radius); column <= Math.Min(image.Width - 1, x + radius); column++) {
                    var offset = (row * image.Width + column) * 4;
                    for (var channel = 0; channel < 4; channel++) patch.Add(image.Pixels[offset + channel]);
                    if (image.Pixels[offset + 3] >= 250 && Math.Abs(image.Pixels[offset] - expected.R) <= 3
                        && Math.Abs(image.Pixels[offset + 1] - expected.G) <= 3 && Math.Abs(image.Pixels[offset + 2] - expected.B) <= 3) pixels++;
                }
            samples.Add(new { id = expected.Id, expected = new[] { expected.R, expected.G, expected.B }, solidPixels = pixels,
                pixels = Convert.ToBase64String(patch.ToArray()), witness });
        }
        var facts = JsonSerializer.SerializeToElement(new { samples, sourceDimmed = await page.EvaluateAsync<bool>(
            "()=>retainedGraph.__cfxGraphHighlight?.dim===true") });
        await WriteReceiptAsync(name + "-paint", facts);
        return facts;
    }

    private static Task MarkGraphPrintContextPhaseAsync(IPage page, string phase) =>
        page.EvaluateAsync("phase=>window.graphPrintContextProbe.mark(phase)", phase);

    private static Task<int[]> GraphWebGlBackingAsync(IPage page) => page.EvaluateAsync<int[]>("""
        ()=>{const canvas=retainedGraph.querySelector('[data-cfx-role=graph-webgl]');return canvas?[canvas.width,canvas.height]:[];}
        """);

    private static Task WaitForGraphScreenBackingAsync(IPage page) => page.WaitForFunctionAsync("""
        ()=>{
            const renderer=retainedGraph.dataset.cfxGraphRendererActive;
            if(!['canvas','webgl'].includes(renderer))return true;
            const canvas=retainedGraph.querySelector('[data-cfx-role=graph-'+renderer+']');
            const rect=canvas.getBoundingClientRect(),ratio=Math.max(1,devicePixelRatio||1);
            return rect.width>0&&rect.height>0&&canvas.width===Math.round(rect.width*ratio)&&canvas.height===Math.round(rect.height*ratio);
        }
        """);

    // Observe real events without creating a context, canceling loss, or requesting recovery.
    // Capture starts before adapter scripts; losses with no DOM event remain unobserved.
    // Listeners release before the final assertion; earlier failures close their owned page/session.
    private const string GraphPrintContextProbe = """
        (()=>{
            const events=[],checkpoints=[],printMedia=window.matchMedia('print');
            let phase='initial-runtime';
            const sample=canvas=>{
                const root=canvas?.closest('.cfx-graph-explorer')||document.querySelector('.cfx-graph-explorer');
                canvas=canvas||root?.querySelector('[data-cfx-role=graph-webgl]');
                const runtime=root?.__cfxGraphWebGl;
                return {phase,time:performance.now(),printMedia:printMedia.matches,renderer:root?.dataset.cfxGraphRendererActive||null,
                    requestedRenderer:root?.dataset.cfxGraphRenderer||null,fallback:root?.dataset.cfxGraphRendererFallback||null,
                    bound:root?.hasAttribute('data-cfx-graph-bound')||false,
                    lifetimeActive:root?(window[Symbol.for('ChartForgeX.adapterLifetimes')]?.get(root,'graph')?.active??false):false,
                    rootConnected:root?.isConnected||false,rootHidden:root?.hidden||false,
                    runtime:runtime===false?'unavailable':runtime?'allocated':'absent',contextLost:runtime?.gl?.isContextLost()??null,
                    viewport:root?{x:root.dataset.cfxViewportX||null,y:root.dataset.cfxViewportY||null,scale:root.dataset.cfxViewportScale||null}:null,
                    canvas:canvas?{connected:canvas.isConnected,width:canvas.width,height:canvas.height,
                        clientWidth:canvas.clientWidth,clientHeight:canvas.clientHeight}:null};
            };
            const observe=event=>{
                if(!event.target.matches?.('[data-cfx-role=graph-webgl]'))return;
                events.push({...sample(event.target),type:event.type,trusted:event.isTrusted,statusMessage:event.statusMessage||''});
            };
            const options={capture:true,passive:true};
            document.addEventListener('webglcontextlost',observe,options);
            document.addEventListener('webglcontextrestored',observe,options);
            window.graphPrintContextProbe={
                mark(value){phase=value;checkpoints.push(sample());},
                read(){return {coverage:'document capture before adapter scripts; attached-canvas DOM events only; event samples precede target handlers',timeOrigin:performance.timeOrigin,events,checkpoints};},
                release(){document.removeEventListener('webglcontextlost',observe,options);document.removeEventListener('webglcontextrestored',observe,options);}
            };
        })();
        """;

    private static Task<string> GraphCommittedStateAsync(IPage page) => page.EvaluateAsync<string>("""
        ()=>{const state=ChartForgeXGraphExplorer.captureState(retainedGraph);return JSON.stringify({viewport:state.viewport,selection:state.selection,positions:state.positions});}
        """);

    private static async Task WaitForGraphPrintSurfaceAsync(IPage page) {
        await page.WaitForFunctionAsync("()=>document.querySelector('[data-cfx-role=graph-print-surface] svg')&&document.querySelector('.cfx-graph-explorer[data-cfx-graph-print-ready=true]')");
        await page.EvaluateAsync("()=>new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve)))");
    }

    private static Task<JsonElement> GraphPrintFactsAsync(IPage page) => page.EvaluateAsync<JsonElement>("""
        ()=>{
            const surfaces=[...document.querySelectorAll('[data-cfx-role=graph-print-surface]')],svg=surfaces[0]?.querySelector('svg'),scene=svg||retainedGraph.querySelector('[data-cfx-role=graph-scene]');
            const box=scene.getBoundingClientRect(),ids=[...document.querySelectorAll('[id]')].map(node=>node.id),idSet=new Set(ids),printIds=new Set([scene,...scene.querySelectorAll('[id]')].map(node=>node.id).filter(Boolean));
            const paint=['n0','n1','n2','n6'].map(id=>{const node=scene.querySelector('[data-cfx-role=graph-node][data-node-id='+id+'] circle'),style=node&&getComputedStyle(node);
                return {id,fill:style?.fill,stroke:style?.stroke};});
            let missingReferences=0;
            for(const node of scene.querySelectorAll('*'))for(const attr of node.attributes){
                if(attr.name==='href'&&attr.value.startsWith('#')&&!printIds.has(attr.value.slice(1)))missingReferences++;
                for(const match of attr.value.matchAll(/url\(['"]?#([^)'"\s]+)['"]?\)/g))if(!printIds.has(match[1]))missingReferences++;
            }
            return {surfaces:surfaces.length,outsideNodes:[...scene.querySelectorAll('[data-cfx-role=graph-node]')].filter(node=>{const mark=node.getBoundingClientRect();return mark.left<box.left-1||mark.right>box.right+1||mark.top<box.top-1||mark.bottom>box.bottom+1;}).length,nodes:scene.querySelectorAll('[data-cfx-role=graph-node]').length,
                outsideDetails:[...scene.querySelectorAll('[data-cfx-role=graph-node-details],[data-cfx-role=graph-edge-label]')].filter(node=>node.getClientRects().length&&getComputedStyle(node).display!=='none').filter(node=>{const detail=node.getBoundingClientRect();return detail.left<box.left-1||detail.right>box.right+1||detail.top<box.top-1||detail.bottom>box.bottom+1;}).length,
                edges:scene.querySelectorAll('[data-cfx-role=graph-edge]').length,camera:scene.querySelector('[data-cfx-role=graph-viewport]')?.getAttribute('transform')||'',
                visibleControls:[...retainedGraph.querySelectorAll('button,input,select,summary')].filter(node=>node.getClientRects().length&&getComputedStyle(node).visibility!=='hidden').length,
                duplicateIds:ids.length-idSet.size,missingReferences,paint,edgePaint:readGraphEdgePaint(scene),detailPaint:readGraphDetailPaint(scene),aspect:box.width/box.height,overflow:Math.max(0,box.right-innerWidth,-box.left),
                printBlockHeight:retainedGraph.getBoundingClientRect().height+box.height,viewportHeight:innerHeight,
                renderer:retainedGraph.dataset.cfxGraphRendererActive,accelerated:retainedGraph.dataset.cfxGraphAcceleratedMarkup,
                title:retainedGraph.querySelector('h1')?.textContent,printMarkup:svg?.outerHTML||null};
        }
        """);
}
