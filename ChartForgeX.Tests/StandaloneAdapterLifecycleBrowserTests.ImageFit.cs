using System.Text.Json;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Raster;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Image-backed marks preserve the complete source across browser and static exports.</summary>
public sealed partial class StandaloneAdapterLifecycleBrowserTests {
    [BrowserTheory]
    [InlineData(HtmlGraphRenderBackend.Svg)]
    [InlineData(HtmlGraphRenderBackend.Canvas)]
    [InlineData(HtmlGraphRenderBackend.WebGl)]
    public async Task NonSquareImagesStretchConsistentlyBeforeAndAfterSelectedAuthoredUpdates(HtmlGraphRenderBackend backend) {
        if (!Enabled) return;
        var graph = ImageFitGraph();
        if (backend != HtmlGraphRenderBackend.Svg) {
            graph.Options.Features |= GraphSceneFeatures.LevelOfDetail;
            graph.Options.LevelOfDetail.CanvasPreferredNodeThreshold = 2;
        }
        await using var session = await OpenAsync(graph.ToGraphExplorerHtmlPage(options => {
            options.RenderBackend = backend; options.PersistThemePreference = false;
        }), 960, 760);
        var page = session.Page;
        for (var phase = 0; phase < 2; phase++) {
            var artifacts = await page.EvaluateAsync<JsonElement>("""
                async phase=>{
                    const root=document.querySelector('.cfx-graph-explorer');
                    if(phase){
                        const nodes=ChartForgeXGraphExplorer.get(root).nodes;
                        ChartForgeXGraphExplorer.update(root,{upsertNodes:nodes.map(node=>({...node,style:{backgroundColor:'#f97316',borderColor:'#0f766e'}}))});
                        ChartForgeXGraphExplorer.select(root,[{role:'graph-node',id:'circle'},{role:'graph-node',id:'rect'}],{mode:'replace'});
                    }
                    ChartForgeXGraphExplorer.applyState(root,{version:1,viewport:{x:0,y:0,scale:1}});
                    const artifacts={},capture=event=>{artifacts[event.detail.format]=event.detail.content;event.preventDefault();};
                    root.addEventListener('cfxgraphexport',capture);
                    try{await ChartForgeXGraphExplorer.export(root,'svg');await ChartForgeXGraphExplorer.export(root,'png');}
                    finally{root.removeEventListener('cfxgraphexport',capture);}
                    return artifacts;
                }
                """, phase);
            var facts = await InspectImageFitAsync(page, artifacts);
            AssertImageFit(facts);
            await WriteReceiptAsync("graph-image-fit-" + backend + "-" + phase, facts);
        }
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task NativeGraphSvgAndPngPreserveTheSameNonSquareImageContentAndCircularClipping() {
        if (!Enabled) return;
        var graph = ImageFitGraph();
        var artifacts = JsonSerializer.SerializeToElement(new {
            svg = graph.ToGraphSvg(), png = "data:image/png;base64," + Convert.ToBase64String(graph.ToGraphPng())
        });
        await using var session = await OpenAsync("<!doctype html><html><body></body></html>", 960, 760);
        var facts = await InspectImageFitAsync(session.Page, artifacts);
        AssertImageFit(facts);
        await WriteReceiptAsync("graph-image-fit-native", facts);
        AssertNoConsoleErrors(session);
    }

    private static Task<JsonElement> InspectImageFitAsync(IPage page, JsonElement artifacts) => page.EvaluateAsync<JsonElement>("""
        async serialized=>{
            const artifacts=JSON.parse(serialized);
            const parsed=new DOMParser().parseFromString(artifacts.svg,'image/svg+xml');
            const transform=parsed.querySelector('[data-cfx-role=graph-viewport]').getAttribute('transform');
            const values=transform.match(/[+-]?(?:\d*\.)?\d+(?:e[+-]?\d+)?/gi).map(Number),[tx,ty,zoom]=values;
            const inspect=async format=>{
                const image=new Image();image.src=format==='svg'?'data:image/svg+xml,'+encodeURIComponent(artifacts.svg):artifacts.png;await image.decode();
                const canvas=document.createElement('canvas');canvas.width=image.naturalWidth;canvas.height=image.naturalHeight;
                const context=canvas.getContext('2d');context.drawImage(image,0,0);
                const scale=Math.min(canvas.width/960,canvas.height/560),offsetX=(canvas.width-960*scale)/2,offsetY=(canvas.height-560*scale)/2;
                const sample=(x,y)=>[...context.getImageData(Math.round(offsetX+(tx+x*zoom)*scale),Math.round(offsetY+(ty+y*zoom)*scale),1,1).data];
                return {format,samples:{circleLeft:sample(176,180),circleCenter:sample(200,180),circleTop:sample(200,156),circleRight:sample(224,180),circleCorner:sample(172,152),
                    rectLeft:sample(476,180),rectCenter:sample(500,180),rectTop:sample(500,157),rectRight:sample(524,180)}};
            };
            return [await inspect('svg'),await inspect('png')];
        }
        """, artifacts.GetRawText());

    private static void AssertImageFit(JsonElement facts) {
        foreach (var artifact in facts.EnumerateArray()) {
            var samples = artifact.GetProperty("samples");
            foreach (var name in new[] { "circleLeft", "rectLeft" }) AssertPixel(samples, name, 239, 68, 68);
            foreach (var name in new[] { "circleCenter", "circleTop", "rectCenter", "rectTop" }) AssertPixel(samples, name, 34, 197, 94);
            foreach (var name in new[] { "circleRight", "rectRight" }) AssertPixel(samples, name, 59, 130, 246);
            AssertPixel(samples, "circleCorner", 255, 255, 255);
        }
    }

    private static void AssertPixel(JsonElement samples, string name, int red, int green, int blue) {
        var pixel = samples.GetProperty(name).EnumerateArray().Select(channel => channel.GetInt32()).ToArray();
        Assert.InRange(Math.Abs(pixel[0] - red), 0, 3);
        Assert.InRange(Math.Abs(pixel[1] - green), 0, 3);
        Assert.InRange(Math.Abs(pixel[2] - blue), 0, 3);
        Assert.Equal(255, pixel[3]);
    }

    private static GraphScene ImageFitGraph() {
        var pixels = new byte[64 * 32 * 4];
        for (var y = 0; y < 32; y++) for (var x = 0; x < 64; x++) {
            var color = x < 16 ? (239, 68, 68) : x < 48 ? (34, 197, 94) : (59, 130, 246);
            var offset = (y * 64 + x) * 4;
            pixels[offset] = (byte)color.Item1; pixels[offset + 1] = (byte)color.Item2; pixels[offset + 2] = (byte)color.Item3; pixels[offset + 3] = 255;
        }
        var uri = "data:image/png;base64," + Convert.ToBase64String(PngWriter.WriteRgba(64, 32, pixels));
        var graph = GraphScene.Create("image-fit", "Image content")
            .AddNode("circle", "Circular image", node => { node.Shape = GraphNodeShape.Image; node.ImageUrl = uri; node.Fixed = true; node.X = 200; node.Y = 180; node.Size = 32; })
            .AddNode("rect", "Rectangular image", node => { node.Shape = GraphNodeShape.RectangularImage; node.ImageUrl = uri; node.Fixed = true; node.X = 500; node.Y = 180; node.Size = 32; });
        graph.Options.Features = GraphSceneFeatures.Selection | GraphSceneFeatures.Viewport | GraphSceneFeatures.Export
            | GraphSceneFeatures.StateSnapshots | GraphSceneFeatures.IncrementalUpdates;
        return graph;
    }
}
