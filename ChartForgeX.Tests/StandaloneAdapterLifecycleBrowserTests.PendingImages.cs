using System.Text.Json;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class StandaloneAdapterLifecycleBrowserTests {
    [BrowserFact]
    public async Task PendingPngImageCompletesAcrossDocumentMutationsWithoutItsTimeoutFallback() {
        if (!Enabled) return;
        var graph = GraphScene.Create("pending-image", "Pending image export")
            .AddNode("a", "API", node => { node.Fixed = true; node.X = 200; node.Y = 180; node.Size = 32; })
            .AddNode("b", "Database", node => { node.Fixed = true; node.X = 500; node.Y = 180; });
        graph.Options.Features = GraphSceneFeatures.Selection | GraphSceneFeatures.Viewport | GraphSceneFeatures.LevelOfDetail
            | GraphSceneFeatures.IncrementalUpdates | GraphSceneFeatures.Export;
        graph.Options.LevelOfDetail.CanvasPreferredNodeThreshold = 2;
        await using var session = await OpenAsync(graph.ToGraphExplorerHtmlPage(options => {
            options.RenderBackend = HtmlGraphRenderBackend.Canvas; options.PersistThemePreference = false;
        }), 900, 720);
        var page = session.Page;
        await page.EvaluateAsync("""
            () => {
                const BrowserImage=window.Image,schedule=window.setTimeout,cancel=window.clearTimeout;
                window.pendingImages=[];window.exportDone=false;window.preloadFallbackScheduled=false;window.preloadFallbackCleared=false;
                // Hold only the export fallback clock. Real HTMLImageElements must load and finish the export themselves.
                window.setTimeout=(callback,delay,...args)=>{
                    if(delay===1500){preloadFallbackScheduled=true;return -12345;}
                    return schedule(callback,delay,...args);
                };
                window.clearTimeout=timer=>{if(timer===-12345)preloadFallbackCleared=true;else cancel(timer);};
                window.Image=function(...args){const image=new BrowserImage(...args);pendingImages.push(image);return image;};
                const root=document.querySelector('.cfx-graph-explorer');
                root.addEventListener('cfxgraphexport',event=>{event.preventDefault();window.exportedPng=event.detail.content;});
                const image='data:image/svg+xml,'+encodeURIComponent('<svg xmlns="http://www.w3.org/2000/svg" width="32" height="32"><rect width="32" height="32" fill="#22c55e"/></svg>');
                ChartForgeXGraphExplorer.update(root,{upsertNodes:[{id:'a',label:'Loaded image',fixed:true,x:200,y:180,size:32,shape:'image',imageUrl:image}]});
                if(pendingImages.length!==1||pendingImages[0].complete)throw new Error('Image was not pending at the export boundary');
                ChartForgeXGraphExplorer.export(root,'png').then(()=>exportDone=true);
                const hostUpdate=document.createElement('div');hostUpdate.textContent='Unrelated host update';document.body.append(hostUpdate);
            }
            """);
        await page.WaitForFunctionAsync("()=>exportDone");
        var result = await page.EvaluateAsync<JsonElement>("""
            () => ({completed:exportDone,fallbackScheduled:preloadFallbackScheduled,fallbackCleared:preloadFallbackCleared,
                realImage:pendingImages[0] instanceof HTMLImageElement,loaded:pendingImages[0].complete&&pendingImages[0].naturalWidth===32,
                png:exportedPng?.startsWith('data:image/png;base64,')===true})
            """);
        Assert.True(result.GetProperty("completed").GetBoolean());
        Assert.True(result.GetProperty("fallbackScheduled").GetBoolean());
        Assert.True(result.GetProperty("fallbackCleared").GetBoolean());
        Assert.True(result.GetProperty("realImage").GetBoolean());
        Assert.True(result.GetProperty("loaded").GetBoolean());
        Assert.True(result.GetProperty("png").GetBoolean());
        await WriteReceiptAsync("graph-pending-image-mutation", result);
        await CaptureAsync(page, "graph-pending-image-mutation");
        AssertNoConsoleErrors(session);
    }
}
