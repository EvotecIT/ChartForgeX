using System.Text.Json;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class StandaloneAdapterLifecycleBrowserTests {
    private static void AssertGraphDetailPaint(JsonElement paint, bool dark) {
        foreach (var detail in paint.EnumerateArray()) {
            var id = detail.GetProperty("id").GetString();
            if (id == "n6") { Assert.Equal(0, detail.GetProperty("statuses").GetInt32()); continue; }
            Assert.Equal(1, detail.GetProperty("statuses").GetInt32());
            Assert.Equal(id == "n0" ? "rgb(34, 197, 94)" : id == "n1" ? "rgb(239, 68, 68)" : "rgb(245, 158, 11)", detail.GetProperty("statusFill").GetString());
            Assert.Equal(2, detail.GetProperty("statusWidth").GetDouble());
            if (id == "n1") continue;
            Assert.Equal(dark ? "rgb(229, 237, 248)" : "rgb(51, 65, 85)", detail.GetProperty("badgeFill").GetString());
            Assert.Equal(dark ? "rgb(11, 18, 32)" : "rgb(255, 255, 255)", detail.GetProperty("badgeStroke").GetString());
            Assert.Equal(2, detail.GetProperty("badgeWidth").GetDouble());
            Assert.Equal(dark ? "rgb(11, 18, 32)" : "rgb(255, 255, 255)", detail.GetProperty("badgeText").GetString());
            Assert.False(string.IsNullOrWhiteSpace(detail.GetProperty("secondary").GetString()));
        }
    }

    private static async Task AssertGraphPrimaryPaintAsync(IPage page, HtmlGraphRenderBackend backend, bool dark) {
        var before = await GraphCommittedStateAsync(page);
        await page.EvaluateAsync("()=>{ChartForgeXGraphExplorer.clearHighlight(retainedGraph);ChartForgeXGraphExplorer.focus(retainedGraph,'n0',{maximumNodes:3,maximumEdges:2});}");
        await page.EmulateMediaAsync(new() { Media = Media.Print });
        await WaitForGraphPrintSurfaceAsync(page);
        var facts = await page.EvaluateAsync<JsonElement>("""
            ()=>{
                const scene=document.querySelector('[data-cfx-role=graph-print-surface] svg');
                const ring=scene?.querySelector('[data-cfx-role=graph-node-details][data-node-details-for=n0] [data-cfx-role=graph-node-primary-ring]');
                const mark=scene?.querySelector('[data-cfx-role=graph-node][data-node-id=n0] circle');
                return {ring:ring?{r:parseFloat(ring.getAttribute('r')),stroke:getComputedStyle(ring).stroke,width:parseFloat(getComputedStyle(ring).strokeWidth)}:null,
                    primary:scene?.querySelectorAll('[data-cfx-role=graph-node].cfx-graph-neighborhood-primary').length,
                    mark:mark?{stroke:getComputedStyle(mark).stroke,width:parseFloat(getComputedStyle(mark).strokeWidth)}:null,details:readGraphDetailPaint(scene)};
            }
            """);
        await WriteReceiptAsync("graph-primary-" + backend, facts);
        Assert.Equal(1, facts.GetProperty("primary").GetInt32());
        if (backend == HtmlGraphRenderBackend.Svg) {
            Assert.Equal("rgb(20, 184, 166)", facts.GetProperty("mark").GetProperty("stroke").GetString());
            Assert.Equal(6, facts.GetProperty("mark").GetProperty("width").GetDouble());
        } else {
            Assert.Equal(25, facts.GetProperty("ring").GetProperty("r").GetDouble());
            Assert.Equal("rgb(15, 118, 110)", facts.GetProperty("ring").GetProperty("stroke").GetString());
            Assert.Equal(3, facts.GetProperty("ring").GetProperty("width").GetDouble());
        }
        AssertGraphDetailPaint(facts.GetProperty("details"), dark);
        var artifacts = await page.EvaluateAsync<JsonElement>("""
            async ordinary => {
                const contents={},capture=event=>{contents[event.detail.format]=event.detail.content;event.preventDefault();};
                retainedGraph.addEventListener('cfxgraphexport',capture);
                try{await ChartForgeXGraphExplorer.export(retainedGraph,'svg');await ChartForgeXGraphExplorer.export(retainedGraph,'png');}
                finally{retainedGraph.removeEventListener('cfxgraphexport',capture);}
                const view=ChartForgeXGraphExplorer.captureState(retainedGraph).viewport,rows=[];
                for(const format of ['svg','png']){
                    const image=new Image();image.src=format==='svg'?'data:image/svg+xml;charset=utf-8,'+encodeURIComponent(contents.svg):contents.png;await image.decode();
                    const canvas=document.createElement('canvas');canvas.width=image.naturalWidth;canvas.height=image.naturalHeight;
                    const context=canvas.getContext('2d');context.drawImage(image,0,0);const bytes=context.getImageData(0,0,canvas.width,canvas.height).data;
                    const scale=Math.min(canvas.width/960,canvas.height/560),x=(canvas.width-960*scale)/2+(100*view.scale+view.x)*scale,y=(canvas.height-560*scale)/2+(100*view.scale+view.y)*scale;
                    const native=ordinary&&format==='svg',radius=(native?16:25)*view.scale*scale,colour=native?[20,184,166]:[15,118,110];
                    let pixels=0;
                    for(let row=Math.max(0,Math.floor(y-radius*1.12));row<Math.min(canvas.height,y+radius*1.12);row++)
                        for(let column=Math.max(0,Math.floor(x-radius*1.12));column<Math.min(canvas.width,x+radius*1.12);column++){
                            const distance=Math.hypot(column+.5-x,row+.5-y)/radius,index=(row*canvas.width+column)*4;
                            if(distance>.88&&distance<1.12&&bytes[index+3]>=250&&colour.every((value,position)=>Math.abs(bytes[index+position]-value)<=3))pixels++;
                        }
                    rows.push({format,pixels});
                }
                return rows;
            }
            """, backend == HtmlGraphRenderBackend.Svg);
        await WriteReceiptAsync("graph-primary-artifacts-" + backend, artifacts);
        foreach (var artifact in artifacts.EnumerateArray()) Assert.True(artifact.GetProperty("pixels").GetInt32() > 0, artifact.GetProperty("format").GetString() + " must retain the primary-neighborhood affordance.");
        await CaptureAsync(page, "graph-primary-" + backend);
        await page.EmulateMediaAsync(new() { Media = Media.Screen });
        await page.WaitForFunctionAsync("()=>!document.querySelector('[data-cfx-role=graph-print-surface]')");
        await page.EvaluateAsync("()=>ChartForgeXGraphExplorer.focus(retainedGraph,null)");
        Assert.Equal(before, await GraphCommittedStateAsync(page));
    }
}
