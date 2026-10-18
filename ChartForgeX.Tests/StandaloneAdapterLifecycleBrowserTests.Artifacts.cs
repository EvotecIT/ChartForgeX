using System.Text.Json;
using ChartForgeX.Interactivity.Html;
using HtmlTinkerX;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class StandaloneAdapterLifecycleBrowserTests {
    private static void AssertGraphPrintPaint(JsonElement paint, bool dark) {
        var selected = dark ? "rgb(251, 191, 36)" : "rgb(217, 119, 6)";
        foreach (var node in paint.EnumerateArray()) {
            var id = node.GetProperty("id").GetString(); var authored = id == "n1";
            Assert.Equal(authored ? "rgb(22, 163, 74)" : "rgb(37, 99, 235)", node.GetProperty("fill").GetString());
            Assert.Equal(authored ? "rgb(15, 118, 110)" : id == "n6" ? "rgb(239, 246, 255)" : selected, node.GetProperty("stroke").GetString());
        }
    }

    private static async Task AssertGraphArtifactPaintAsync(IPage page, HtmlGraphRenderBackend backend, bool dark, bool dimmed = false) {
        var facts = await page.EvaluateAsync<JsonElement>("""
            async ({dark,dimmed})=>{
                window.graphPaintArtifacts={};
                const capture=event=>{graphPaintArtifacts[event.detail.format]=event.detail.content;event.preventDefault();};
                retainedGraph.addEventListener('cfxgraphexport',capture);
                try {await ChartForgeXGraphExplorer.export(retainedGraph,'svg');await ChartForgeXGraphExplorer.export(retainedGraph,'png');}
                finally {retainedGraph.removeEventListener('cfxgraphexport',capture);}
                const selected=dark?[251,191,36]:[217,119,6],targets=[
                    {id:'n0',x:100,y:100,fill:[37,99,235],stroke:selected},
                    {id:'n1',x:250,y:100,fill:[22,163,74],stroke:[15,118,110]},
                    {id:'n2',x:400,y:100,fill:[37,99,235],stroke:selected},
                    {id:'n6',x:100,y:210,fill:[37,99,235],stroke:[239,246,255]}];
                const edgeTargets=[{id:'e1',x:175,y:100,tip:227,color:selected,alpha:.95},
                    {id:'e2',x:325,y:100,tip:377,color:selected,alpha:.95},
                    {id:'e7',x:175,y:210,tip:227,color:[124,58,237],alpha:.58},
                    {id:'e8',x:325,y:210,tip:377,color:dark?[99,115,139]:[135,150,170],alpha:.58}];
                const paper=dark?[11,18,32]:[255,255,255],blend=(colour,alpha)=>colour.map((channel,index)=>Math.round(channel*alpha+paper[index]*(1-alpha)));
                const inspect=async(format,content)=>{
                    const image=new Image();image.src=format==='svg'?'data:image/svg+xml;charset=utf-8,'+encodeURIComponent(content):content;await image.decode();
                    const canvas=document.createElement('canvas');canvas.width=image.naturalWidth;canvas.height=image.naturalHeight;
                    const context=canvas.getContext('2d');context.drawImage(image,0,0);
                    const pixels=context.getImageData(0,0,canvas.width,canvas.height).data;
                    const scale=Math.min(canvas.width/960,canvas.height/560),offsetX=(canvas.width-960*scale)/2,offsetY=(canvas.height-560*scale)/2;
                    const matches=(index,colour)=>pixels[index+3]>=250&&colour.every((channel,position)=>Math.abs(pixels[index+position]-channel)<=3);
                    const inkMatches=(index,colour)=>{
                        const delta=colour.map((value,position)=>value-paper[position]),actual=colour.map((value,position)=>pixels[index+position]-paper[position]);
                        const coverage=actual.reduce((sum,value,position)=>sum+value*delta[position],0)/delta.reduce((sum,value)=>sum+value*value,0);
                        return pixels[index+3]>=250&&coverage>=.25&&coverage<=1.05&&actual.every((value,position)=>Math.abs(value-delta[position]*coverage)<=4);
                    };
                    const edgeSamples=edgeTargets.map(target=>{
                        const alpha=dimmed&&target.id!=='e2'?.3:1,stroke=blend(target.color,target.alpha*alpha),arrow=blend(target.color,alpha);
                        const arrowOverStroke=blend(target.color,alpha+target.alpha*alpha*(1-alpha));
                        const x=offsetX+(target.x*1.8+55)*scale,y=offsetY+(target.y*1.8+40)*scale;
                        const arrowX=offsetX+((target.tip-4.6)*1.8+55)*scale;
                        let strokePixels=0,arrowPixels=0;
                        for(let row=Math.max(0,Math.floor(y-5*scale));row<Math.min(canvas.height,y+5*scale);row++)
                            for(let column=Math.max(0,Math.floor(x-24*scale));column<Math.min(canvas.width,x+24*scale);column++)
                                if(matches((row*canvas.width+column)*4,stroke))strokePixels++;
                        for(let row=Math.max(0,Math.floor(y-4*scale));row<Math.min(canvas.height,y+4*scale);row++)
                            for(let column=Math.max(0,Math.floor(arrowX-4*scale));column<Math.min(canvas.width,arrowX+4*scale);column++)
                                if(matches((row*canvas.width+column)*4,arrow)||matches((row*canvas.width+column)*4,arrowOverStroke))arrowPixels++;
                        return {id:target.id,strokePixels,arrowPixels};
                    });
                    const detailSamples=dimmed?[]:[{id:'n0',x:87.2,y:87.2,color:[34,197,94]},
                        {id:'n1',x:237.2,y:87.2,color:[239,68,68]},{id:'n2',x:387.2,y:87.2,color:[245,158,11]}].map(target=>{
                        const x=offsetX+(target.x*1.8+55)*scale,y=offsetY+(target.y*1.8+40)*scale;
                        let pixels=0;
                        for(let row=Math.max(0,Math.floor(y-3*scale));row<Math.min(canvas.height,y+3*scale);row++)
                            for(let column=Math.max(0,Math.floor(x-3*scale));column<Math.min(canvas.width,x+3*scale);column++)
                                if(matches((row*canvas.width+column)*4,target.color))pixels++;
                        return {id:target.id,pixels};
                    });
                    if(!dimmed){
                        let pixels=0;
                        for(let row=Math.max(0,Math.floor(offsetY+(140*1.8+40)*scale));row<Math.min(canvas.height,offsetY+(154*1.8+40)*scale);row++)
                            for(let column=Math.max(0,Math.floor(offsetX+(55*1.8+55)*scale));column<Math.min(canvas.width,offsetX+(145*1.8+55)*scale);column++)
                                if(inkMatches((row*canvas.width+column)*4,dark?[169,183,202]:[100,116,139]))pixels++;
                        detailSamples.push({id:'n0-secondary',pixels});
                    }
                    return {format,width:canvas.width,height:canvas.height,edgeSamples,detailSamples,samples:targets.map(target=>{
                        const alpha=dimmed&&target.id!=='n2'?.3:1;
                        const x=offsetX+(target.x*1.8+55)*scale,y=offsetY+(target.y*1.8+40)*scale,radius=16*1.8*scale;
                        let fillPixels=0,strokePixels=0;
                        for(let row=Math.max(0,Math.floor(y-radius*1.3));row<Math.min(canvas.height,y+radius*1.3);row++)
                            for(let column=Math.max(0,Math.floor(x-radius*1.3));column<Math.min(canvas.width,x+radius*1.3);column++){
                                const distance=Math.hypot(column+.5-x,row+.5-y)/radius,index=(row*canvas.width+column)*4;
                                if(distance<.7&&matches(index,blend(target.fill,alpha)))fillPixels++;
                                if(distance>.8&&distance<1.3&&matches(index,blend(target.stroke,alpha)))strokePixels++;
                            }
                        return {id:target.id,fillPixels,strokePixels};
                    })};
                };
                return [await inspect('svg',graphPaintArtifacts.svg),await inspect('png',graphPaintArtifacts.png)];
            }
            """, new { dark, dimmed });
        var suffix = dimmed ? "-dimmed" : "";
        await WriteReceiptAsync("graph-paint-" + backend + suffix, facts);
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) {
            await File.WriteAllTextAsync(Path.Combine(output, "graph-paint-" + backend + suffix + ".svg"), await page.EvaluateAsync<string>("()=>graphPaintArtifacts.svg"));
            var png = await page.EvaluateAsync<string>("()=>graphPaintArtifacts.png");
            await File.WriteAllBytesAsync(Path.Combine(output, "graph-paint-" + backend + suffix + ".png"), Convert.FromBase64String(png[(png.IndexOf(',') + 1)..]));
        }
        foreach (var artifact in facts.EnumerateArray()) foreach (var sample in artifact.GetProperty("samples").EnumerateArray()) {
            Assert.True(sample.GetProperty("fillPixels").GetInt32() > 0, artifact.GetProperty("format").GetString() + " must retain " + sample.GetProperty("id").GetString() + " fill.");
            Assert.True(sample.GetProperty("strokePixels").GetInt32() > 0, artifact.GetProperty("format").GetString() + " must retain " + sample.GetProperty("id").GetString() + " selection/highlight/authored stroke.");
        }
        foreach (var artifact in facts.EnumerateArray()) foreach (var sample in artifact.GetProperty("edgeSamples").EnumerateArray()) {
            Assert.True(sample.GetProperty("strokePixels").GetInt32() > 0, artifact.GetProperty("format").GetString() + " must retain " + sample.GetProperty("id").GetString() + " edge paint.");
            Assert.True(sample.GetProperty("arrowPixels").GetInt32() > 0, artifact.GetProperty("format").GetString() + " must retain " + sample.GetProperty("id").GetString() + " arrow paint.");
        }
        foreach (var artifact in facts.EnumerateArray()) foreach (var sample in artifact.GetProperty("detailSamples").EnumerateArray())
            Assert.True(sample.GetProperty("pixels").GetInt32() > 0, artifact.GetProperty("format").GetString() + " must retain " + sample.GetProperty("id").GetString() + " detail paint.");
    }

    private static async Task CaptureGraphPrintPdfAsync(IPage page, HtmlGraphRenderBackend backend) {
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (Engine != HtmlBrowserEngine.Chromium || string.IsNullOrWhiteSpace(output)) return;
        await page.PdfAsync(new() {
            Path = Path.Combine(output, "graph-print-" + backend + ".pdf"), Format = "A4", Landscape = true, PrintBackground = true,
            Margin = new() { Top = "12mm", Bottom = "12mm", Left = "12mm", Right = "12mm" }
        });
    }
}
