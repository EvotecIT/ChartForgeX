using System.Text.Json;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Topology;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class StandaloneAdapterLifecycleBrowserTests {
    [BrowserTheory]
    [InlineData(HtmlGraphRenderBackend.Svg, 390)]
    [InlineData(HtmlGraphRenderBackend.Svg, 980)]
    [InlineData(HtmlGraphRenderBackend.Canvas, 390)]
    [InlineData(HtmlGraphRenderBackend.Canvas, 980)]
    [InlineData(HtmlGraphRenderBackend.WebGl, 390)]
    [InlineData(HtmlGraphRenderBackend.WebGl, 980)]
    public async Task GraphCardIconsAndBadgesKeepReadablePaintAcrossBackends(HtmlGraphRenderBackend backend, int width) {
        if (!Enabled) return;
        var graph = GraphIconFixture();
        if (backend != HtmlGraphRenderBackend.Svg) {
            graph.Options.Enable(GraphSceneFeatures.LevelOfDetail);
            graph.Options.LevelOfDetail.CanvasPreferredNodeThreshold = 1;
        }
        var html = Instrument(graph.ToGraphExplorerHtmlPage(options => {
            options.RenderBackend = backend; options.Theme = HtmlGraphExplorerTheme.Light;
            options.PersistThemePreference = false; options.PersistInteractionState = false;
        })).Replace("<head>", "<head><script>" + GraphIconPaintProbe + "</script>", StringComparison.Ordinal);
        await using var session = await OpenAsync(html, width, 900);
        var page = session.Page;
        await page.EvaluateAsync("()=>{window.retainedGraph=document.querySelector('.cfx-graph-explorer');lifetimeProbe.recordGlobals=false;lifetimeProbe.recordObservers=false;}");
        var expected = backend == HtmlGraphRenderBackend.WebGl ? "webgl" : backend.ToString().ToLowerInvariant();
        await page.WaitForFunctionAsync("expected=>retainedGraph.dataset.cfxGraphRendererActive===expected", expected);
        await page.Locator("[data-cfx-graph-action=fit]").ClickAsync();
        foreach (var dark in new[] { false, true }) {
            if (dark) {
                // Light -> Dark is a real control transition; preserve the existing camera and authored surfaces.
                await page.Locator("[data-cfx-graph-action=theme]").ClickAsync();
                await page.WaitForFunctionAsync("()=>retainedGraph.dataset.cfxGraphThemeActive==='dark'");
            }
            await page.EvaluateAsync("()=>document.fonts.ready.then(()=>new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve))))");
            var facts = await page.EvaluateAsync<JsonElement>(GraphIconPaintFacts);
            await WriteReceiptAsync($"graph-icons-{backend}-{width}-{dark}", facts);
            await CaptureAsync(page, $"graph-icons-{backend}-{width}-{dark}");
            Assert.Equal(expected, facts.GetProperty("actual").GetString());
            Assert.Equal(4, facts.GetProperty("icons").GetArrayLength());
            foreach (var icon in facts.GetProperty("icons").EnumerateArray()) {
                Assert.True(icon.GetProperty("contrast").GetDouble() >= 4.5, icon.GetProperty("id").GetString());
                Assert.True(icon.GetProperty("inkPixels").GetInt32() > 0, "Actual icon ink must differ from its mark surface.");
            }
            Assert.Equal(0, facts.GetProperty("badgeTitleIntersection").GetDouble());
            Assert.Equal(0, facts.GetProperty("badgeOverflow").GetDouble());
            Assert.Equal("+1234567890", facts.GetProperty("badge").GetString());
            Assert.Equal("+1234567890", facts.GetProperty("exportedBadge").GetString());
            Assert.Equal("KERBEROS.MICROSOFTONLINE.COM😀", facts.GetProperty("fullTitle").GetString());
        }
        var point = await GraphPointAsync(page, "d");
        await page.Mouse.ClickAsync((float)point.GetProperty("x").GetDouble(), (float)point.GetProperty("y").GetDouble());
        Assert.Contains("KERBEROS.MICROSOFTONLINE.COM😀", await page.Locator("[data-cfx-role=graph-announcer]").InnerTextAsync());
        var source = await page.EvaluateAsync<string>("()=>JSON.stringify(ChartForgeXGraphExplorer.get(retainedGraph))");
        Assert.Contains("+1234567890", source);
        await page.EvaluateAsync("()=>ChartForgeXGraphExplorer.dispose(retainedGraph)");
        await AssertReleasedAsync(page, $"graph-icons-{backend}-{width}");
        AssertNoConsoleErrors(session);
    }

    private static GraphScene GraphIconFixture() {
        // The existing relationship-overview fixture supplies prepared cards and retained long-label facts.
        var topology = TopologyChart.Create().WithId("relationship-overview").WithTitle("Relationship overview")
            .WithLayout(TopologyLayoutMode.RelationshipRadial)
            .AddAutoIconNode("a", "alpha.example", "microsoft-ad:domain", TopologyHealthStatus.Healthy, subtitle: "Domain", width: 180, height: 72)
            .AddAutoIconNode("b", "beta.example", "microsoft-ad:domain", TopologyHealthStatus.Warning, subtitle: "Domain", width: 180, height: 72)
            .AddAutoNode("c", "gamma.example", TopologyNodeKind.Namespace, TopologyHealthStatus.Critical, subtitle: "Domain", width: 180, height: 72, symbol: "AD")
            .AddAutoNode("d", "KERBEROS.MICROSOFTONLINE.COM😀", TopologyNodeKind.Namespace, TopologyHealthStatus.Unknown, subtitle: "Domain", width: 180, height: 72, symbol: "AD")
            .AddEdge("a-b", "a", "b", "Forest · Bidirectional", TopologyEdgeKind.Trust, TopologyHealthStatus.Warning, VisualLinkDirection.Bidirectional)
            .AddEdge("a-c", "a", "c", "External · Outbound", TopologyEdgeKind.Trust, TopologyHealthStatus.Critical, VisualLinkDirection.Forward);
        topology.WithNodeDisplay("a", TopologyNodeDisplayMode.Card).WithNodeDisplay("b", TopologyNodeDisplayMode.Card).WithNodeBadge("d", "+1234567890");
        var graph = topology.ToGraphScene();
        for (var index = 0; index < graph.Nodes.Count; index++) graph.Nodes[index].Style.BackgroundColor = index < 2 ? "#ffffff" : "#0f172a";
        return graph;
    }

    private const string GraphIconPaintProbe = """
        window.graphIconPaint=[];window.graphIconRects=[];
        const graphIconText=CanvasRenderingContext2D.prototype.fillText,graphIconRect=CanvasRenderingContext2D.prototype.roundRect;
        CanvasRenderingContext2D.prototype.roundRect=function(x,y,width,height,...args){
            if(this.canvas.getAttribute('data-cfx-role')==='graph-canvas')graphIconRects.push({x,y,width,height,t:this.getTransform(),canvas:this.canvas});
            return graphIconRect.call(this,x,y,width,height,...args);
        };
        CanvasRenderingContext2D.prototype.fillText=function(text,x,y,...args){
            if(this.canvas.getAttribute('data-cfx-role')==='graph-canvas')graphIconPaint.push({text:String(text),x,y,fill:this.fillStyle,font:this.font,
                metrics:this.measureText(text),t:this.getTransform(),canvas:this.canvas});
            return graphIconText.call(this,text,x,y,...args);
        };
        """;

    private const string GraphIconPaintFacts = """
        async()=>{
            const root=retainedGraph,actual=root.dataset.cfxGraphRendererActive,source=ChartForgeXGraphExplorer.get(root),icons=[];
            // Read CSS color channels directly; pixel readback is reserved for the rendered-ink check.
            const rgb=value=>{const c=document.createElement('canvas').getContext('2d');c.fillStyle=value;const color=c.fillStyle;
                if(/^#[0-9a-f]{6}$/i.test(color))return[1,3,5].map(index=>parseInt(color.slice(index,index+2),16));
                const channels=/^rgba?\(\s*([\d.]+)[, ]+([\d.]+)[, ]+([\d.]+)/i.exec(color);
                if(!channels)throw new Error('Unsupported fixture color: '+color);return channels.slice(1,4).map(Number);};
            const luminance=values=>values.map(v=>v/255).map(v=>v<=.03928?v/12.92:((v+.055)/1.055)**2.4).reduce((n,v,i)=>n+v*[.2126,.7152,.0722][i],0);
            const contrast=(a,b)=>{const x=luminance(rgb(a)),y=luminance(rgb(b));return(Math.max(x,y)+.05)/(Math.min(x,y)+.05);};
            const project=(box,m)=>{const a=new DOMPoint(box.x,box.y).matrixTransform(m),b=new DOMPoint(box.x+box.width,box.y+box.height).matrixTransform(m);return{left:a.x,top:a.y,right:b.x,bottom:b.y};};
            const screen=(record,box)=>{const r=record.canvas.getBoundingClientRect(),c=record.canvas;return{left:r.left+box.left*r.width/c.width,right:r.left+box.right*r.width/c.width,top:r.top+box.top*r.height/c.height,bottom:r.top+box.bottom*r.height/c.height};};
            const pixels=(context,box,background)=>{const color=rgb(background),image=context.getImageData(0,0,context.canvas.width,context.canvas.height);let ink=0;
                for(let y=Math.max(0,Math.floor(box.top));y<Math.min(image.height,Math.ceil(box.bottom));y++)for(let x=Math.max(0,Math.floor(box.left));x<Math.min(image.width,Math.ceil(box.right));x++){
                    const index=(y*image.width+x)*4;if(image.data[index+3]>0&&color.some((v,i)=>Math.abs(image.data[index+i]-v)>20))ink++;}return ink;};
            let svg='';const capture=e=>{if(e.detail.format==='svg'){svg=e.detail.content;e.preventDefault();}};
            root.addEventListener('cfxgraphexport',capture);try{await ChartForgeXGraphExplorer.export(root,'svg');}finally{root.removeEventListener('cfxgraphexport',capture);}
            const host=document.createElement('div');host.style.cssText='position:fixed;left:-10000px;top:0;width:960px;height:560px';document.body.append(host);
            const shadow=host.attachShadow({mode:'open'});shadow.innerHTML=svg;const exported=shadow.querySelector('svg'),frame=exported.getBoundingClientRect();
            const image=new Image();image.src='data:image/svg+xml;charset=utf-8,'+encodeURIComponent(svg);await image.decode();
            const surface=document.createElement('canvas');surface.width=image.naturalWidth;surface.height=image.naturalHeight;const context=surface.getContext('2d');context.drawImage(image,0,0);
            for(const node of source.nodes){const mark=exported.querySelector('[data-node-id="'+node.id+'"]'),icon=mark.querySelector('.cfx-graph-node-icon');let fill,inkPixels;
                if(actual==='svg'){fill=getComputedStyle(root.querySelector('[data-node-id="'+node.id+'"] .cfx-graph-node-icon')).fill;
                    const box=icon.getBoundingClientRect();inkPixels=pixels(context,{left:(box.left-frame.left)*surface.width/frame.width,top:(box.top-frame.top)*surface.height/frame.height,right:(box.right-frame.left)*surface.width/frame.width,bottom:(box.bottom-frame.top)*surface.height/frame.height},node.style.backgroundColor);
                }else{const painted=[...graphIconPaint].reverse().find(p=>p.text===node.icon&&Math.abs(p.x-(node.x-node.size*1.45+28))<.01&&Math.abs(p.y-node.y-1)<.01);
                    fill=painted.fill;const m=painted.metrics,box=project({x:painted.x-m.actualBoundingBoxLeft,y:painted.y-m.actualBoundingBoxAscent,width:m.actualBoundingBoxLeft+m.actualBoundingBoxRight,height:m.actualBoundingBoxAscent+m.actualBoundingBoxDescent},painted.t);
                    inkPixels=pixels(painted.canvas.getContext('2d'),box,node.style.backgroundColor);}
                icons.push({id:node.id,text:node.icon,fill,font:getComputedStyle(icon).font,contrast:contrast(fill,node.style.backgroundColor),inkPixels});}
            const node=source.nodes.find(n=>n.id==='d'),details=actual==='svg'?root.querySelector('[data-node-details-for=d]'):null;
            let title,badge;if(details){const label=details.querySelector('.cfx-graph-node-label'),mark=details.querySelector('.cfx-graph-node-badge').firstElementChild;
                title=project(label.getBBox(),label.getScreenCTM());badge=project(mark.getBBox(),mark.getScreenCTM());
            }else{const painted=[...graphIconPaint].reverse().find(p=>p.text.includes('KERBER')),count=[...graphIconPaint].reverse().find(p=>p.text==='+1234567890'),m=painted.metrics;
                title=screen(painted,project({x:painted.x-m.actualBoundingBoxLeft,y:painted.y-m.actualBoundingBoxAscent,width:m.actualBoundingBoxLeft+m.actualBoundingBoxRight,height:m.actualBoundingBoxAscent+m.actualBoundingBoxDescent},painted.t));
                const rect=[...graphIconRects].reverse().find(r=>Math.abs(r.x+r.width/2-count.x)<.01&&Math.abs(r.y+r.height/2-count.y+.5)<.01);badge=screen(rect,project(rect,rect.t));}
            const view=root.querySelector('[data-cfx-role=graph-scene]').getBoundingClientRect();
            const result={actual,icons,title,badgeBounds:badge,badgeTitleIntersection:Math.max(0,Math.min(title.right,badge.right)-Math.max(title.left,badge.left))*Math.max(0,Math.min(title.bottom,badge.bottom)-Math.max(title.top,badge.top)),
                badgeOverflow:Math.max(0,view.left-badge.left,badge.right-view.right,view.top-badge.top,badge.bottom-view.bottom),badge:node.badge,fullTitle:node.label,
                exportedBadge:exported.querySelector('[data-node-details-for=d] .cfx-graph-node-badge text')?.textContent,source,svg};host.remove();return result;
        }
        """;
}
