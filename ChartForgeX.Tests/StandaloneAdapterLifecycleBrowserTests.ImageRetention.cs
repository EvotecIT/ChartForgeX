using System.Text.Json;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class StandaloneAdapterLifecycleBrowserTests {
    [BrowserFact]
    public async Task GraphImagesFollowCurrentScenesAndShareUntilTheirLastConsumerLeaves() {
        if (!Enabled) return;
        await using var session = await OpenAsync(ImageRetentionPage(), 900, 720);
        var page = session.Page;
        var result = await page.EvaluateAsync<JsonElement>("""
            async () => {
                const root=document.querySelector('.cfx-graph-explorer'),canvas=document.createElement('canvas');
                canvas.width=128;canvas.height=128;const context=canvas.getContext('2d');
                const patch=(target,url)=>ChartForgeXGraphExplorer.update(target,{upsertNodes:[{id:'a',label:'Current image',fixed:true,x:200,y:180,size:32,shape:'image',imageUrl:url}]});
                const count=()=>imageCacheWitness[0]?.deref()?.size||0;
                let maxEntries=0,url='';
                for(let index=0;index<64;index++){
                    context.fillStyle=`rgb(${index*3%256},${index*11%256},${index*37%256})`;context.fillRect(0,0,128,128);
                    url=canvas.toDataURL('image/png');patch(root,url);
                    const image=imageCacheWitness[0].deref().get(url);
                    if(!image.complete)await new Promise((resolve,reject)=>{image.addEventListener('load',resolve,{once:true});image.addEventListener('error',reject,{once:true});});
                    if(image.naturalWidth!==128)throw new Error('Real image did not load');
                    maxEntries=Math.max(maxEntries,count());
                }
                const peer=document.querySelector('[data-cfx-graph-id=image-retention-peer]');
                ChartForgeXGraphExplorer.mount(peer);patch(peer,url);
                const shared=count();
                ChartForgeXGraphExplorer.update(root,{removeNodeIds:['a']});const oneConsumer=count();
                ChartForgeXGraphExplorer.dispose(root);root.remove();const peerOnly=count();
                ChartForgeXGraphExplorer.update(peer,{removeNodeIds:['a']});const removed=count();
                patch(peer,url);const reloaded=count();
                ChartForgeXGraphExplorer.dispose(peer);const disposed=count();peer.remove();
                document.body.append(peer);ChartForgeXGraphExplorer.mount(peer);const remounted=count();
                ChartForgeXGraphExplorer.dispose(peer);peer.remove();const released=count();
                return {maxEntries,shared,oneConsumer,peerOnly,removed,reloaded,disposed,remounted,released};
            }
            """);
        foreach (var property in new[] { "maxEntries", "shared", "oneConsumer", "peerOnly", "reloaded", "remounted" })
            Assert.Equal(1, result.GetProperty(property).GetInt32());
        foreach (var property in new[] { "removed", "disposed", "released" })
            Assert.Equal(0, result.GetProperty(property).GetInt32());
        await WriteReceiptAsync("graph-image-scene-retention", result);
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task GraphImageExportsLeaseReplacedImagesAndIgnoreAbandonedLoadCallbacks() {
        if (!Enabled) return;
        await using var session = await OpenAsync(ImageRetentionPage(), 900, 720);
        var page = session.Page;
        var result = await page.EvaluateAsync<JsonElement>("""
            async () => {
                const root=document.querySelector('.cfx-graph-explorer'),BrowserImage=window.Image;
                const source=Object.getOwnPropertyDescriptor(HTMLImageElement.prototype,'src');
                const complete=Object.getOwnPropertyDescriptor(HTMLImageElement.prototype,'complete').get;
                const pending=[],drawn=[],draw=CanvasRenderingContext2D.prototype.drawImage;
                CanvasRenderingContext2D.prototype.drawImage=function(image,...args){drawn.push(pending.indexOf(image));return draw.call(this,image,...args);};
                // Hold the network/image boundary while keeping genuine browser images and decoding.
                window.Image=function(){
                    const image=new BrowserImage();let held=true,url='';pending.push(image);
                    Object.defineProperty(image,'src',{get:()=>url,set:value=>{url=value;}});
                    Object.defineProperty(image,'complete',{get:()=>!held&&complete.call(image)});
                    image.release=()=>new Promise((resolve,reject)=>{
                        image.addEventListener('load',resolve,{once:true});image.addEventListener('error',reject,{once:true});
                        held=false;source.set.call(image,url);
                    });
                    return image;
                };
                const count=()=>imageCacheWitness[0]?.deref()?.size||0;
                const uri=color=>'data:image/svg+xml,'+encodeURIComponent(`<svg xmlns="http://www.w3.org/2000/svg" width="32" height="32"><rect width="32" height="32" fill="${color}"/></svg>`);
                const green=uri('#22c55e'),red=uri('#ef4444'),blue=uri('#3b82f6');
                const patch=url=>ChartForgeXGraphExplorer.update(root,{upsertNodes:[{id:'a',label:'Current image',fixed:true,x:200,y:180,size:32,shape:'image',imageUrl:url}]});
                let exports=0;root.addEventListener('cfxgraphexport',event=>{event.preventDefault();exports++;});
                patch(green);const original=pending.at(-1),exported=ChartForgeXGraphExplorer.export(root,'png');
                patch(red);const pendingExport=count();
                await original.release();await exported;const completedExport=count();
                const originalDrawn=drawn.includes(pending.indexOf(original));
                // Releasing and re-acquiring one URL creates a new load. The old
                // image's late event must not consume the replacement's redraw.
                patch(blue);const abandoned=pending.at(-1);patch(red);patch(blue);const replacement=pending.at(-1);
                await abandoned.release();drawn.length=0;await replacement.release();
                await new Promise(requestAnimationFrame);const replacementDrawn=drawn.includes(pending.indexOf(replacement));
                patch(green);const failed=pending.at(-1);patch(red);patch(green);const afterError=pending.at(-1);
                failed.dispatchEvent(new Event('error'));drawn.length=0;await afterError.release();
                await new Promise(requestAnimationFrame);const replacementAfterErrorDrawn=drawn.includes(pending.indexOf(afterError));
                patch(red);const canceled=ChartForgeXGraphExplorer.export(root,'png');
                ChartForgeXGraphExplorer.dispose(root);await canceled;const disposed=count();
                return {pendingExport,completedExport,originalDrawn,replacementDrawn,replacementAfterErrorDrawn,disposed,exports};
            }
            """);
        Assert.Equal(2, result.GetProperty("pendingExport").GetInt32());
        Assert.Equal(1, result.GetProperty("completedExport").GetInt32());
        Assert.True(result.GetProperty("originalDrawn").GetBoolean());
        Assert.True(result.GetProperty("replacementDrawn").GetBoolean());
        Assert.True(result.GetProperty("replacementAfterErrorDrawn").GetBoolean());
        Assert.Equal(0, result.GetProperty("disposed").GetInt32());
        Assert.Equal(1, result.GetProperty("exports").GetInt32());
        await WriteReceiptAsync("graph-image-export-retention", result);
        AssertNoConsoleErrors(session);
    }

    private static string ImageRetentionPage() {
        var graph = GraphScene.Create("image-retention", "Image ownership")
            .AddNode("a", "API", node => { node.Fixed = true; node.X = 200; node.Y = 180; node.Size = 32; })
            .AddNode("b", "Database", node => { node.Fixed = true; node.X = 500; node.Y = 180; });
        graph.Options.Features = GraphSceneFeatures.Selection | GraphSceneFeatures.Viewport | GraphSceneFeatures.LevelOfDetail
            | GraphSceneFeatures.IncrementalUpdates | GraphSceneFeatures.Export;
        graph.Options.LevelOfDetail.CanvasPreferredNodeThreshold = 2;
        Action<HtmlGraphExplorerOptions> configure = options => {
            options.RenderBackend = HtmlGraphRenderBackend.Canvas; options.PersistThemePreference = false;
        };
        var renderer = new HtmlGraphExplorerRenderer();
        var html = renderer.RenderPage(graph, configure);
        graph.Id = "image-retention-peer";
        var peer = System.Text.RegularExpressions.Regex.Replace(renderer.RenderFragment(graph, configure), "(?s)<script>.*?</script>", "");
        html = html.Replace("</body>", peer + "</body>");
        // Observe actual cache retention without a product hook or test-owned
        // strong references to the cache, its keys or its decoded images.
        return html.Replace("<head>", """
            <head><script>
            (()=>{
                const set=Map.prototype.set,seen=new WeakSet();window.imageCacheWitness=[];
                Map.prototype.set=function(key,value){
                    const result=set.call(this,key,value);
                    if(value instanceof HTMLImageElement&&!seen.has(this)){seen.add(this);imageCacheWitness.push(new WeakRef(this));}
                    return result;
                };
            })();
            </script>
            """);
    }
}
