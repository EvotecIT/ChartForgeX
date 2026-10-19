using System.Text.Json;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class StandaloneAdapterLifecycleBrowserTests {
    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnmodifiedGraphFragmentsShareOneRuntimeAcrossParsingAndLateInsertion(bool lateInsertion) {
        if (!Enabled) return;
        var fragments = RepeatedImageGraphFragments();
        const string witness = """
            <script>
            (()=>{
                const set=Map.prototype.set,seen=new WeakSet();window.fragmentImageMaps=[];window.fragmentReady=0;
                Map.prototype.set=function(key,value){
                    const result=set.call(this,key,value);
                    if(value instanceof HTMLImageElement&&!seen.has(this)){seen.add(this);fragmentImageMaps.push(new WeakRef(this));}
                    return result;
                };
                document.addEventListener('cfxgraphready',()=>fragmentReady++);
            })();
            </script>
            """;
        const string retainApi = "<script>window.firstFragmentApi=new WeakRef(window.ChartForgeXGraphExplorer);</script>";
        var html = "<!doctype html><html><head>" + witness + "</head><body>" + fragments[0] + retainApi
            + (lateInsertion ? "" : fragments[1]) + "</body></html>";
        await using var session = await OpenAsync(html, 900, 720);
        var page = session.Page;
        if (lateInsertion) await page.EvaluateAsync("""
            fragment=>{
                const range=document.createRange();range.selectNodeContents(document.body);
                document.body.append(range.createContextualFragment(fragment));
            }
            """, fragments[1]);
        var result = await page.EvaluateAsync<JsonElement>("""
            async()=>{
                const a=document.querySelector('[data-cfx-graph-id=fragment-image-a]'),b=document.querySelector('[data-cfx-graph-id=fragment-image-b]');
                const cacheCount=()=>fragmentImageMaps.reduce((count,reference)=>count+(reference.deref()?.size||0),0);
                const initial=cacheCount(),ready=fragmentReady,sameApi=firstFragmentApi.deref()===ChartForgeXGraphExplorer;
                const uri=color=>'data:image/svg+xml,'+encodeURIComponent(`<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16"><rect width="16" height="16" fill="${color}"/></svg>`);
                const patch=(root,color)=>ChartForgeXGraphExplorer.update(root,{upsertNodes:[{id:'a',label:'Current image',fixed:true,x:200,y:180,size:32,shape:'image',imageUrl:uri(color)}]});
                patch(a,'purple');const firstChanged=cacheCount();patch(b,'purple');const sharedChanged=cacheCount();
                ChartForgeXGraphExplorer.update(a,{removeNodeIds:['a']});const peerOnly=cacheCount();
                ChartForgeXGraphExplorer.update(b,{removeNodeIds:['a']});const dropped=cacheCount();
                ChartForgeXGraphExplorer.dispose(a);ChartForgeXGraphExplorer.dispose(b);const disposed=cacheCount();
                ChartForgeXGraphExplorer.mount(a);patch(a,'orange');
                let exports=0;a.addEventListener('cfxgraphexport',event=>{event.preventDefault();exports++;});
                const exporting=ChartForgeXGraphExplorer.export(a,'png');patch(a,'cyan');const leased=cacheCount();
                await exporting;const afterExport=cacheCount();
                ChartForgeXGraphExplorer.dispose(a);a.remove();b.remove();const released=cacheCount();
                return {initial,ready,sameApi,firstChanged,sharedChanged,peerOnly,dropped,disposed,leased,afterExport,released,exports,imageMaps:fragmentImageMaps.length};
            }
            """);
        Assert.True(result.GetProperty("sameApi").GetBoolean());
        Assert.Equal(2, result.GetProperty("ready").GetInt32());
        Assert.Equal(1, result.GetProperty("imageMaps").GetInt32());
        foreach (var property in new[] { "initial", "sharedChanged", "peerOnly", "afterExport", "exports" })
            Assert.Equal(1, result.GetProperty(property).GetInt32());
        foreach (var property in new[] { "firstChanged", "leased" }) Assert.Equal(2, result.GetProperty(property).GetInt32());
        foreach (var property in new[] { "dropped", "disposed", "released" }) Assert.Equal(0, result.GetProperty(property).GetInt32());
        await WriteReceiptAsync("graph-repeated-fragment-" + lateInsertion, result);
        AssertNoConsoleErrors(session);
    }

    private static string[] RepeatedImageGraphFragments() {
        var image = "data:image/svg+xml," + Uri.EscapeDataString("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"16\" height=\"16\"><rect width=\"16\" height=\"16\" fill=\"green\"/></svg>");
        var graph = GraphScene.Create("fragment-image-a", "Shared graph runtime")
            .AddNode("a", "Image", node => { node.Shape = GraphNodeShape.Image; node.ImageUrl = image; node.Fixed = true; node.X = 200; node.Y = 180; node.Size = 32; })
            .AddNode("b", "Peer", node => { node.Fixed = true; node.X = 500; node.Y = 180; });
        graph.Options.Features = GraphSceneFeatures.Selection | GraphSceneFeatures.Viewport | GraphSceneFeatures.LevelOfDetail
            | GraphSceneFeatures.IncrementalUpdates | GraphSceneFeatures.Export;
        graph.Options.LevelOfDetail.CanvasPreferredNodeThreshold = 2;
        Action<HtmlGraphExplorerOptions> configure = options => {
            options.RenderBackend = HtmlGraphRenderBackend.Canvas; options.PersistThemePreference = false;
        };
        var renderer = new HtmlGraphExplorerRenderer();
        var first = renderer.RenderFragment(graph, configure);
        graph.Id = "fragment-image-b";
        return new[] { first, renderer.RenderFragment(graph, configure) };
    }
}
