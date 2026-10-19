using System.Text.Json;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class StandaloneAdapterLifecycleBrowserTests {
    private static void AssertGraphEdgePaint(JsonElement paint, bool dark, bool dimmed) {
        var selected = dark ? "rgb(251, 191, 36)" : "rgb(217, 119, 6)";
        foreach (var edge in paint.EnumerateArray()) {
            var id = edge.GetProperty("id").GetString();
            var emphasized = id is "e1" or "e2";
            var alpha = dimmed && id != "e2" ? .3 : 1;
            var color = emphasized ? selected : id == "e7" ? "rgb(124, 58, 237)" : dark ? "rgb(99, 115, 139)" : "rgb(135, 150, 170)";
            Assert.Equal(color, edge.GetProperty("stroke").GetString());
            Assert.Equal(id == "e7" ? 3.25 : emphasized ? 3.6 : 3, edge.GetProperty("width").GetDouble(), 6);
            Assert.Equal((emphasized ? .95 : .58) * alpha, edge.GetProperty("opacity").GetDouble(), 6);
            Assert.Equal(id == "e7" ? "13,7" : "none", edge.GetProperty("dash").GetString());
            Assert.Equal(id == "e7" ? 2 : 1, edge.GetProperty("arrows").GetArrayLength());
            foreach (var arrow in edge.GetProperty("arrows").EnumerateArray()) {
                Assert.Equal(color, arrow.GetProperty("fill").GetString());
                Assert.Equal(alpha, arrow.GetProperty("opacity").GetDouble(), 6);
            }
            if (id == "e7") {
                Assert.False(string.IsNullOrWhiteSpace(edge.GetProperty("label").GetString()));
                Assert.Equal(alpha, edge.GetProperty("labelOpacity").GetDouble(), 6);
                if (!dark) Assert.NotEqual("rgb(255, 255, 255)", edge.GetProperty("labelColor").GetString());
            }
        }
    }

    private static async Task AssertGraphEdgeUpdateAsync(IPage page, HtmlGraphRenderBackend backend, bool dark) {
        var facts = await page.EvaluateAsync<JsonElement>("""
            async () => {
                window.readGraphDetailPaint = scene => ['n0','n1','n2','n6'].map(id => {
                    const group=scene?.querySelector('[data-cfx-role=graph-node-details][data-node-details-for='+id+']');
                    const status=group?.querySelector('.cfx-graph-node-status'),badge=group?.querySelector('.cfx-graph-node-badge'),surface=badge?.firstElementChild,text=badge?.querySelector('text');
                    const statusStyle=status&&getComputedStyle(status),surfaceStyle=surface&&getComputedStyle(surface),textStyle=text&&getComputedStyle(text);
                    return {id,statuses:group?.querySelectorAll('.cfx-graph-node-status').length||0,statusFill:statusStyle?.fill||'',statusWidth:parseFloat(statusStyle?.strokeWidth||0),
                        badgeFill:surfaceStyle?.fill||'',badgeStroke:surfaceStyle?.stroke||'',badgeWidth:parseFloat(surfaceStyle?.strokeWidth||0),badgeText:textStyle?.fill||'',secondary:group?.querySelector('.cfx-graph-node-secondary')?.textContent||''};
                });
                window.readGraphEdgePaint = scene => ['e1','e2','e7','e8'].map(id => {
                    const edge=scene.querySelector('[data-cfx-role=graph-edge][data-edge-id='+id+']'),style=edge?.style;
                    const arrows=scene.querySelector('[data-cfx-role=graph-edge-arrows][data-edge-arrows-for='+id+']');
                    const label=scene.querySelector('[data-cfx-role=graph-edge-label][data-edge-label-for='+id+']');
                    const colour=value=>{const canvas=document.createElement('canvas'),context=canvas.getContext('2d');context.fillStyle=value||'#000';context.fillRect(0,0,1,1);const rgb=context.getImageData(0,0,1,1).data;return `rgb(${rgb[0]}, ${rgb[1]}, ${rgb[2]})`;};
                    return {id,stroke:colour(style?.stroke),width:parseFloat(style?.strokeWidth||0),opacity:parseFloat(style?.opacity||0),
                        dash:style?.strokeDasharray==='none'?'none':(style?.strokeDasharray||'').split(/[ ,]+/).map(parseFloat).join(','),
                        arrows:[...(arrows?.querySelectorAll('polygon')||[])].map(arrow=>({fill:colour(arrow.style.fill),opacity:parseFloat(arrows.style.opacity)})),
                        label:label?.textContent||'',labelColor:colour(label?.style.fill),labelOpacity:parseFloat(label?.style.opacity||0)};
                });
                const camera=ChartForgeXGraphExplorer.captureState(retainedGraph),arrowWitness=retainedGraph.querySelector('[data-cfx-role=graph-edge-arrows][data-edge-arrows-for=e1]');
                ChartForgeXGraphExplorer.applyState(retainedGraph,{...camera,viewport:{...camera.viewport,x:camera.viewport.x+12,y:camera.viewport.y+8}});
                ChartForgeXGraphExplorer.applyState(retainedGraph,camera);
                const cameraRetainsArrow=arrowWitness?arrowWitness===retainedGraph.querySelector('[data-cfx-role=graph-edge-arrows][data-edge-arrows-for=e1]'):null;
                const original=ChartForgeXGraphExplorer.get(retainedGraph).edges.find(edge=>edge.id==='e7');
                const capture=async()=>{let content='';const listener=event=>{if(event.detail.format==='svg'){content=event.detail.content;event.preventDefault();}};
                    retainedGraph.addEventListener('cfxgraphexport',listener);try{await ChartForgeXGraphExplorer.export(retainedGraph,'svg');}finally{retainedGraph.removeEventListener('cfxgraphexport',listener);}
                    return readGraphEdgePaint(new DOMParser().parseFromString(content,'image/svg+xml'));};
                ChartForgeXGraphExplorer.update(retainedGraph,{upsertEdges:[{...original,style:{...original.style,color:'#be123c',width:4.5},dashed:true,dashPattern:'17 9'}]});
                const changed=await capture();
                ChartForgeXGraphExplorer.update(retainedGraph,{upsertEdges:[{...original,dashed:true,dashPattern:'13 7'}]});
                return {changed,restored:await capture(),cameraRetainsArrow,selection:ChartForgeXGraphExplorer.captureState(retainedGraph).selection};
            }
            """);
        await WriteReceiptAsync("graph-edge-update-" + backend, facts);
        var changed = facts.GetProperty("changed").EnumerateArray().Single(edge => edge.GetProperty("id").GetString() == "e7");
        Assert.Equal("rgb(190, 18, 60)", changed.GetProperty("stroke").GetString());
        Assert.Equal(4.5, changed.GetProperty("width").GetDouble());
        Assert.Equal("17,9", changed.GetProperty("dash").GetString());
        Assert.Equal(2, changed.GetProperty("arrows").GetArrayLength());
        AssertGraphEdgePaint(facts.GetProperty("restored"), dark, false);
        Assert.Equal(2, facts.GetProperty("selection").GetArrayLength());
        if (backend == HtmlGraphRenderBackend.Svg) Assert.True(facts.GetProperty("cameraRetainsArrow").GetBoolean());
    }
}
