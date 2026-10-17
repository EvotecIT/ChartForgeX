using System.Text.Json;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class GraphSelectionPolicyBrowserTests {
    [BrowserTheory]
    [InlineData(HtmlGraphRenderBackend.Svg, false, 1440)]
    [InlineData(HtmlGraphRenderBackend.Canvas, true, 390)]
    public async Task SingleSelectionHostCommandsReachRenderedGraphWithoutConstrainingHighlights(HtmlGraphRenderBackend backend, bool dark, int width) {
        if (!Enabled) return;
        var graph = GraphScene.Create("selection-policy", "Service connections")
            .AddNode("shared", "API", node => { node.Fixed = true; node.X = 180; node.Y = 180; node.Size = 20; })
            .AddNode("peer", "Database", node => { node.Fixed = true; node.X = 480; node.Y = 180; node.Size = 20; })
            .AddNode("queue", "Queue", node => { node.Fixed = true; node.X = 680; node.Y = 340; node.Size = 20; })
            .AddEdge("shared", "shared", "peer", "Stores");
        graph.Options.Features = GraphSceneFeatures.Selection;
        await using var session = await OpenAsync(graph.ToGraphExplorerHtmlPage(options => {
            options.RenderBackend = backend;
            options.Theme = dark ? HtmlGraphExplorerTheme.Dark : HtmlGraphExplorerTheme.Light;
            options.PersistThemePreference = false;
        }), width, 680);
        var page = session.Page;
        var pageErrors = new List<string>();
        page.PageError += (_, error) => pageErrors.Add(error);
        var root = page.Locator(".cfx-graph-explorer");
        Assert.Equal(backend.ToString().ToLowerInvariant(), await root.GetAttributeAsync("data-cfx-graph-renderer-active"));
        await page.EvaluateAsync("()=>{window.selectionPolicyEvents=[];document.body.addEventListener('cfxgraphselection',event=>selectionPolicyEvents.push(event.detail));}");
        var before = await InspectScene(page);

        var replacement = await page.EvaluateAsync<JsonElement>("""
            ()=>ChartForgeXGraphExplorer.select('selection-policy',[{role:'graph-node',id:'shared'},{role:'graph-edge',id:'shared'}])
            """);
        Assert.Single(replacement.EnumerateArray());
        Assert.Equal("shared", replacement[0].GetProperty("id").GetString());
        var replaced = await InspectScene(page);
        Assert.Single(replaced.GetProperty("selection").EnumerateArray());
        Assert.Equal(replacement[0].GetProperty("role").GetString(), replaced.GetProperty("selection")[0].GetProperty("role").GetString());
        Assert.Equal("shared", replaced.GetProperty("selection")[0].GetProperty("id").GetString());
        Assert.Equal(1, replaced.GetProperty("pressed").GetInt32());

        await page.EvaluateAsync("()=>ChartForgeXGraphExplorer.select('selection-policy',[{role:'graph-node',id:'peer'}],{mode:'add',emit:true})");
        var added = await InspectScene(page);
        Assert.Single(added.GetProperty("selection").EnumerateArray());
        Assert.Equal("graph-node", added.GetProperty("selection")[0].GetProperty("role").GetString());
        Assert.Equal("peer", added.GetProperty("selection")[0].GetProperty("id").GetString());
        Assert.Equal(1, added.GetProperty("pressed").GetInt32());
        Assert.Equal("false", await root.Locator("[data-cfx-role=graph-node][data-node-id=shared]").GetAttributeAsync("aria-pressed"));
        Assert.Equal("false", await root.Locator("[data-cfx-role=graph-edge][data-edge-id=shared]").GetAttributeAsync("aria-pressed"));
        var emitted = await page.EvaluateAsync<JsonElement>("()=>selectionPolicyEvents");
        Assert.Single(emitted.EnumerateArray());
        Assert.Equal(1, emitted[0].GetProperty("count").GetInt32());
        Assert.Equal("peer", emitted[0].GetProperty("items")[0].GetProperty("id").GetString());

        if (backend == HtmlGraphRenderBackend.Svg) {
            await page.WaitForFunctionAsync("()=>getComputedStyle(document.querySelector('[data-cfx-role=graph-node][data-node-id=peer] circle')).strokeWidth==='5px'");
            added = await InspectScene(page);
            var selectedPaint = added.GetProperty("paint").EnumerateArray().Single(item => item.GetProperty("id").GetString() == "peer");
            Assert.Equal(added.GetProperty("selectedColor").GetString(), selectedPaint.GetProperty("stroke").GetString());
            Assert.Equal("5px", selectedPaint.GetProperty("width").GetString());
        } else {
            Assert.True(added.GetProperty("canvas").GetProperty("selectedPixels").GetInt32() > 0);
            Assert.NotEqual(before.GetProperty("canvas").GetProperty("hash").GetUInt32(), added.GetProperty("canvas").GetProperty("hash").GetUInt32());
            Assert.NotEqual(replaced.GetProperty("canvas").GetProperty("hash").GetUInt32(), added.GetProperty("canvas").GetProperty("hash").GetUInt32());
        }
        await Capture(page, $"selection-{backend}-{dark}-{width}", new { before, replaced, added });

        await page.EvaluateAsync("""
            ()=>{ChartForgeXGraphExplorer.highlight('selection-policy',[{role:'graph-node',id:'shared'},{role:'graph-edge',id:'shared'}],{dim:true});
              ChartForgeXGraphExplorer.clearSelection('selection-policy');}
            """);
        var cleared = await InspectScene(page);
        Assert.Empty(cleared.GetProperty("selection").EnumerateArray());
        Assert.Equal(0, cleared.GetProperty("pressed").GetInt32());
        Assert.Equal(2, cleared.GetProperty("highlighted").GetArrayLength());
        Assert.Contains(cleared.GetProperty("highlighted").EnumerateArray(), item => item.GetProperty("role").GetString() == "graph-node" && item.GetProperty("id").GetString() == "shared");
        Assert.Contains(cleared.GetProperty("highlighted").EnumerateArray(), item => item.GetProperty("role").GetString() == "graph-edge" && item.GetProperty("id").GetString() == "shared");
        Assert.True(await root.Locator("[data-cfx-role=graph-node][data-node-id=peer]").EvaluateAsync<bool>("node=>node.classList.contains('cfx-graph-link-dim')"));
        Assert.Equal(1, await page.EvaluateAsync<int>("()=>selectionPolicyEvents.length"));
        Assert.Empty(pageErrors);
        AssertNoConsoleErrors(session);
        await Capture(page, $"highlight-after-clear-{backend}-{dark}-{width}", new { cleared, pageErrors, consoleErrors = 0 });
    }

    private static Task<JsonElement> InspectScene(IPage page) => page.EvaluateAsync<JsonElement>("""
        ()=>{const root=document.querySelector('.cfx-graph-explorer'),canvas=root.querySelector('[data-cfx-role=graph-canvas]'),
          marks=[...root.querySelectorAll('[data-cfx-role=graph-node],[data-cfx-role=graph-edge],[data-cfx-role=graph-cluster]')],
          probe=document.createElement('span');probe.style.color='var(--cfx-color-selected)';root.append(probe);
          const selectedColor=getComputedStyle(probe).color;probe.remove();
          let pixels=null;if(root.dataset.cfxGraphRendererActive==='canvas'){
            const data=canvas.getContext('2d').getImageData(0,0,canvas.width,canvas.height).data;
            const selectedRgb=selectedColor.match(/[\d.]+/g).slice(0,3).map(Number);
            let hash=2166136261,selectedPixels=0;for(let i=0;i<data.length;i++)hash=Math.imul(hash^data[i],16777619);
            for(let i=0;i<data.length;i+=4)if(selectedRgb.every((value,index)=>Math.abs(data[i+index]-value)<8)&&data[i+3]>200)selectedPixels++;
            pixels={width:canvas.width,height:canvas.height,hash:hash>>>0,selectedPixels};}
          return{backend:root.dataset.cfxGraphRendererActive,selection:ChartForgeXGraphExplorer.captureState('selection-policy').selection,
            pressed:marks.filter(item=>item.getAttribute('aria-pressed')==='true').length,
            highlighted:marks.filter(item=>item.hasAttribute('data-cfx-highlighted')).map(item=>({role:item.dataset.cfxRole,id:item.dataset.nodeId||item.dataset.edgeId||item.dataset.clusterId})),
            selectedColor,paint:marks.filter(item=>item.dataset.cfxRole==='graph-node').map(item=>{
              const style=getComputedStyle(item.querySelector('circle,rect,ellipse,polygon,path'));return{id:item.dataset.nodeId,stroke:style.stroke,width:style.strokeWidth};}),canvas:pixels};}
        """);

    private static async Task Capture(IPage page, string name, object receipt) {
        var root = Environment.GetEnvironmentVariable("CFX_V2_CAPTURE_ROOT");
        if (string.IsNullOrWhiteSpace(root)) return;
        if (!Path.IsPathRooted(root)) throw new InvalidOperationException("CFX_V2_CAPTURE_ROOT must be absolute.");
        var folder = Path.Combine(root, "graph-selection-policy");
        Directory.CreateDirectory(folder);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(folder, name + ".png"), FullPage = true, Animations = ScreenshotAnimations.Disabled });
        await File.WriteAllTextAsync(Path.Combine(folder, name + ".json"), JsonSerializer.Serialize(receipt));
    }
}
