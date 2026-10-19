using System.Text.Json;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
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
        await using var session = await OpenAsync(Instrument(graph.ToGraphExplorerHtmlPage(options => {
            options.RenderBackend = backend; options.Theme = dark ? HtmlGraphExplorerTheme.Dark : HtmlGraphExplorerTheme.Light;
            options.PersistThemePreference = false; options.FillAvailableHeight = true;
        })).Replace("<head>", "<head><script>" + GraphPrintContextProbe + "</script>", StringComparison.Ordinal), width, 720);
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
        await MarkGraphPrintContextPhaseAsync(page, "print-initial");
        await page.EmulateMediaAsync(new() { Media = Media.Print });
        await WaitForGraphPrintSurfaceAsync(page);
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
        await MarkGraphPrintContextPhaseAsync(page, "print-before-pdf");
        await CaptureGraphPrintPdfAsync(page, backend);

        await MarkGraphPrintContextPhaseAsync(page, "screen-initial-request");
        await page.EmulateMediaAsync(new() { Media = Media.Screen });
        await page.WaitForFunctionAsync("()=>!document.querySelector('[data-cfx-role=graph-print-surface]')");
        Assert.Equal(before, await GraphCommittedStateAsync(page));
        await MarkGraphPrintContextPhaseAsync(page, "screen-restored");
        await CaptureAsync(page, "graph-print-" + backend + "-restored");
        await page.EvaluateAsync("()=>ChartForgeXGraphExplorer.highlight(retainedGraph,[{role:'graph-node',id:'n2'},{role:'graph-edge',id:'e2'}],{dim:true})");
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
            JsonSerializer.SerializeToElement(new { engine = Engine.ToString(), requested = backend.ToString(), observation = contextReceipt }));
        AssertNoConsoleErrors(session);
    }

    private static Task MarkGraphPrintContextPhaseAsync(IPage page, string phase) =>
        page.EvaluateAsync("phase=>window.graphPrintContextProbe.mark(phase)", phase);

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
                read(){return {coverage:'document capture before adapter scripts; attached-canvas DOM events only; event samples precede target handlers',events,checkpoints};},
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
