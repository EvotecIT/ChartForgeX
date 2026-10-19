using System.Text.Json;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class StandaloneAdapterLifecycleBrowserTests {
    [BrowserTheory]
    [InlineData(HtmlGraphRenderBackend.Canvas)]
    [InlineData(HtmlGraphRenderBackend.WebGl)]
    public async Task AcceleratedGraphRemountPreservesDraggedPositionsSelectionAndExportsAndClearsHostHover(HtmlGraphRenderBackend backend) {
        if (!Enabled) return;
        var graph = GraphScene.Create("dragged-lifetime", "Dragged positions")
            .AddNode("a", "API", node => { node.Fixed = true; node.X = 200; node.Y = 180; node.Size = 24; })
            .AddNode("b", "Database", node => { node.Fixed = true; node.X = 500; node.Y = 180; node.Size = 24; });
        graph.Options.Features = GraphSceneFeatures.Selection | GraphSceneFeatures.Viewport | GraphSceneFeatures.DragNodes
            | GraphSceneFeatures.LevelOfDetail | GraphSceneFeatures.IncrementalUpdates | GraphSceneFeatures.Export;
        graph.Options.LevelOfDetail.CanvasPreferredNodeThreshold = 2;
        await using var session = await OpenAsync(graph.ToGraphExplorerHtmlPage(options => {
            options.RenderBackend = backend; options.PersistInteractionState = false; options.PersistThemePreference = false;
        }), 900, 720);
        var page = session.Page;
        Assert.Equal("true", await page.Locator(".cfx-graph-explorer").GetAttributeAsync("data-cfx-graph-accelerated-markup"));
        await page.EvaluateAsync("""
            () => {
                ChartForgeXGraphExplorer.update('dragged-lifetime',{upsertNodes:[{id:'a',label:'Updated API',fixed:true,x:200,y:180,size:24}]});
                window.retainedGraph=document.querySelector('.cfx-graph-explorer');
                window.hoverNotifications=[];window.linkedPeer=document.createElement('output');linkedPeer.id='linked-lifetime-peer';document.body.append(linkedPeer);
                retainedGraph.addEventListener('cfxgraphhover',event=>{hoverNotifications.push(event.type);linkedPeer.textContent=event.detail.id;});
                retainedGraph.addEventListener('cfxgraphhoverclear',event=>{hoverNotifications.push(event.type);linkedPeer.textContent='';});
                retainedGraph.addEventListener('cfxgraphexport',event=>{event.preventDefault();window.lastSvg=event.detail.content;});
            }
            """);
        var point = await GraphPointAsync(page, "a");
        await page.Mouse.MoveAsync((float)point.GetProperty("x").GetDouble(), (float)point.GetProperty("y").GetDouble());
        Assert.Equal("a", await page.Locator("#linked-lifetime-peer").TextContentAsync());
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync((float)point.GetProperty("x").GetDouble() + 70, (float)point.GetProperty("y").GetDouble() + 50);
        await page.Mouse.UpAsync();
        var moved = await page.EvaluateAsync<JsonElement>("()=>ChartForgeXGraphExplorer.get(retainedGraph).nodes.find(node=>node.id==='a')");
        Assert.True(moved.GetProperty("x").GetDouble() > 220);
        Assert.True(moved.GetProperty("y").GetDouble() > 200);
        Assert.True(await page.EvaluateAsync<bool>("()=>ChartForgeXGraphExplorer.dispose(retainedGraph)"));
        Assert.Equal("", await page.Locator("#linked-lifetime-peer").TextContentAsync());
        Assert.Equal("cfxgraphhoverclear", await page.EvaluateAsync<string>("()=>hoverNotifications.at(-1)"));
        await MoveAwayAsync(page);
        await page.EvaluateAsync("()=>ChartForgeXGraphExplorer.mount(retainedGraph)");
        var restored = await page.EvaluateAsync<JsonElement>("()=>ChartForgeXGraphExplorer.get(retainedGraph)");
        var node = restored.GetProperty("nodes").EnumerateArray().Single(item => item.GetProperty("id").GetString() == "a");
        Assert.Equal(moved.GetProperty("x").GetDouble(), node.GetProperty("x").GetDouble());
        Assert.Equal(moved.GetProperty("y").GetDouble(), node.GetProperty("y").GetDouble());
        Assert.Equal("Updated API", node.GetProperty("label").GetString());
        Assert.Equal("a", restored.GetProperty("selection").GetProperty("primary").GetString());
        await page.EvaluateAsync("()=>ChartForgeXGraphExplorer.export(retainedGraph,'svg')");
        var exported = await page.EvaluateAsync<JsonElement>("""
            () => {
                const svg=new DOMParser().parseFromString(lastSvg,'image/svg+xml');
                const node=svg.querySelector('[data-cfx-role=graph-node][data-node-id=a]');
                const point=node.getAttribute('transform').slice(10,-1).trim().split(/[,\s]+/).map(Number);
                return {x:point[0],y:point[1],selected:node.classList.contains('cfx-graph-selected')};
            }
            """);
        Assert.Equal(moved.GetProperty("x").GetDouble(), exported.GetProperty("x").GetDouble());
        Assert.Equal(moved.GetProperty("y").GetDouble(), exported.GetProperty("y").GetDouble());
        Assert.True(exported.GetProperty("selected").GetBoolean());
        await WriteReceiptAsync("graph-accelerated-drag-" + backend, JsonSerializer.SerializeToElement(new { moved, restored, exported }));
        await CaptureAsync(page, "graph-accelerated-drag-" + backend);
        AssertNoConsoleErrors(session);
    }

    private static Task<JsonElement> GraphPointAsync(Microsoft.Playwright.IPage page, string id) => page.EvaluateAsync<JsonElement>("""
        id => {
            const root=document.querySelector('.cfx-graph-explorer'),node=ChartForgeXGraphExplorer.positions(root).find(node=>node.id===id);
            const svg=root.querySelector('[data-cfx-role=graph-scene]'),box=svg.getBoundingClientRect(),size=svg.getAttribute('viewBox').split(/\s+/).map(Number);
            const fit=Math.min(box.width/size[2],box.height/size[3]);
            return {x:box.left+(box.width-size[2]*fit)/2+(node.x*Number(root.dataset.cfxViewportScale||1)+Number(root.dataset.cfxViewportX||0))*fit,
                y:box.top+(box.height-size[3]*fit)/2+(node.y*Number(root.dataset.cfxViewportScale||1)+Number(root.dataset.cfxViewportY||0))*fit};
        }
        """, id);

    [BrowserFact]
    public async Task GraphBadgeMeasurementsReusePaintWorkAndRefreshWithFontLoadingAndRemount() {
        if (!Enabled) return;
        var graph = GraphScene.Create("measured-badges", "Measured badges")
            .AddNode("a", "API", node => { node.Fixed = true; node.X = 200; node.Y = 180; node.BadgeText = "+1234567890"; })
            .AddNode("b", "Database", node => { node.Fixed = true; node.X = 500; node.Y = 180; node.BadgeText = "+1234567890"; });
        graph.Options.Features = GraphSceneFeatures.Selection | GraphSceneFeatures.Viewport;
        var html = graph.ToGraphExplorerHtmlPage(options => { options.RenderBackend = HtmlGraphRenderBackend.Canvas; options.PersistThemePreference = false; });
        const string probe = """
            window.badgeMeasurements=0;
            const measureBadgeText=CanvasRenderingContext2D.prototype.measureText;
            CanvasRenderingContext2D.prototype.measureText=function(text){
                if(text==='+1234567890'&&this.font.includes('7px'))badgeMeasurements++;
                return measureBadgeText.call(this,text);
            };
            """;
        await using var session = await OpenAsync(html.Replace("<head>", "<head><script>" + probe + "</script>", StringComparison.Ordinal), 900, 720);
        var page = session.Page;
        Assert.Equal(1, await page.EvaluateAsync<int>("()=>badgeMeasurements"));
        await page.EvaluateAsync("""
            () => {
                for(let index=0;index<12;index++)ChartForgeXGraphExplorer.theme('measured-badges',index%2?'dark':'light');
            }
            """);
        Assert.Equal(1, await page.EvaluateAsync<int>("()=>badgeMeasurements"));
        await page.EvaluateAsync("()=>document.fonts.dispatchEvent(new Event('loadingdone'))");
        Assert.Equal(2, await page.EvaluateAsync<int>("()=>badgeMeasurements"));
        await page.EvaluateAsync("()=>ChartForgeXGraphExplorer.dispose('measured-badges')");
        await page.EvaluateAsync("()=>document.fonts.dispatchEvent(new Event('loadingdone'))");
        Assert.Equal(2, await page.EvaluateAsync<int>("()=>badgeMeasurements"));
        await page.EvaluateAsync("()=>ChartForgeXGraphExplorer.mount('measured-badges')");
        Assert.Equal(3, await page.EvaluateAsync<int>("()=>badgeMeasurements"));
        await CaptureAsync(page, "graph-badges-after-font-reload");
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task GraphHierarchyDoubleClickAndBreadcrumbsRemainOperableAfterRemount() {
        if (!Enabled) return;
        var graph = GraphScene.Create("reused-hierarchy", "Service hierarchy")
            .AddNode("a", "Platform", node => { node.Fixed = true; node.X = 200; node.Y = 150; })
            .AddNode("b", "API", node => { node.Fixed = true; node.X = 400; node.Y = 250; node.ParentId = "a"; })
            .AddNode("c", "Database", node => { node.Fixed = true; node.X = 600; node.Y = 350; node.ParentId = "b"; });
        graph.Options.Features = GraphSceneFeatures.Selection | GraphSceneFeatures.HierarchyNavigation;
        var html = graph.ToGraphExplorerHtmlPage().Replace("<head>", "<head><script>" + BreadcrumbProbe + "</script>", StringComparison.Ordinal);
        await using var session = await OpenAsync(html, 900, 720);
        var page = session.Page;
        await page.EvaluateAsync("()=>{ChartForgeXGraphExplorer.dispose('reused-hierarchy');ChartForgeXGraphExplorer.mount('reused-hierarchy');}");
        var replaced = await page.EvaluateAsync<JsonElement>("""
            async () => {
                for(let cycle=0;cycle<12;cycle++){
                    ChartForgeXGraphExplorer.navigate('reused-hierarchy','b');await Promise.resolve();
                    ChartForgeXGraphExplorer.navigate('reused-hierarchy','');await Promise.resolve();
                }
                return {registered:breadcrumbListeners.length,active:breadcrumbListeners.filter(entry=>entry.active).length,
                    detachedActive:breadcrumbListeners.filter(entry=>entry.active&&!entry.target.isConnected).length};
            }
            """);
        Assert.Equal(24, replaced.GetProperty("registered").GetInt32());
        Assert.Equal(0, replaced.GetProperty("active").GetInt32());
        Assert.Equal(0, replaced.GetProperty("detachedActive").GetInt32());
        await WriteReceiptAsync("graph-breadcrumb-replacements", replaced);
        await page.Locator("[data-cfx-role=graph-node][data-node-id=b]").DblClickAsync();
        Assert.Equal("b", await page.Locator(".cfx-graph-explorer").GetAttributeAsync("data-cfx-graph-hierarchy-root"));
        await page.Locator("[data-cfx-hierarchy-node=a]").ClickAsync();
        Assert.Equal("a", await page.Locator(".cfx-graph-explorer").GetAttributeAsync("data-cfx-graph-hierarchy-root"));
        await page.Locator("[data-cfx-graph-action=hierarchy-up]").ClickAsync();
        Assert.Equal("", await page.Locator(".cfx-graph-explorer").GetAttributeAsync("data-cfx-graph-hierarchy-root"));
        Assert.Equal(0, await page.EvaluateAsync<int>("()=>breadcrumbListeners.filter(entry=>entry.active).length"));
        await CaptureAsync(page, "graph-hierarchy-remounted");
        AssertNoConsoleErrors(session);
    }
}
