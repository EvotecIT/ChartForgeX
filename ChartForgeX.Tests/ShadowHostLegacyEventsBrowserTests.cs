using System.Text.Json;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Topology;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class ShadowHostLegacyEventsBrowserTests {
    [BrowserTheory]
    [InlineData("open")]
    [InlineData("closed")]
    public async Task TopologyPublicNotificationsCrossShadowHostWhilePeerSyncStaysLocal(string shadowMode) {
        if (!Enabled) return;
        var options = new TopologyRenderOptions {
            EnableHtmlSelectionPanel = false,
            EnableHtmlViewportControls = true,
            EnableHtmlSynchronizedState = true,
            HtmlSyncGroupName = "shadow-legacy-topology"
        };
        var html = Topology("shadow-topology-source").ToInteractiveHtmlPage(options);
        var peer = Topology("shadow-topology-peer").ToInteractiveHtmlFragmentWithoutAssets(options);
        await using var session = await OpenAsync(html.Replace("</body>", peer + "</body>", StringComparison.Ordinal), 1000, 1000);
        var page = session.Page;
        var pageErrors = new List<string>();
        page.PageError += (_, error) => pageErrors.Add(error);
        await page.EvaluateAsync("() => ChartForgeXTopology.mount(document)");
        Assert.True(await page.EvaluateAsync<bool>("() => [...document.querySelectorAll('.cfx-topology-wrapper')].length === 2 && [...document.querySelectorAll('.cfx-topology-wrapper')].every(root => root.dataset.cfxRuntimeBound === 'true')"));
        await page.EvaluateAsync("""
            () => {
                window.legacyTopologyPeer=document.querySelector('[data-chart-id="shadow-topology-peer"]');
                window.legacyPeerCommands=[];window.legacyLeakedCommands=0;
                legacyTopologyPeer.addEventListener('cfx-topology-apply-sync',event=>legacyPeerCommands.push({
                    bubbles:event.bubbles,composed:event.composed,detail:JSON.parse(JSON.stringify(event.detail))}));
                document.addEventListener('cfx-topology-apply-sync',()=>legacyLeakedCommands++);
            }
            """);
        await MoveBoundRootToShadowAsync(page, "[data-chart-id=shadow-topology-source]", shadowMode,
            new[] { "cfx-topology-select", "cfx-topology-viewport" });

        await ClickRetainedRootAsync(page, "[data-cfx-role=topology-node][data-node-id=a]");
        await ClickRetainedRootAsync(page, "[data-cfx-topology-zoom=in]");
        var result = await page.EvaluateAsync<JsonElement>("""
            () => JSON.parse(JSON.stringify({local:legacyLocalEvents,outer:legacyOuterEvents,commands:legacyPeerCommands,
                leakedCommands:legacyLeakedCommands,sourceZoom:legacyShadowRoot.dataset.cfxTopologyZoom,
                peerZoom:legacyTopologyPeer.dataset.cfxTopologyZoom,peerSelection:legacyTopologyPeer.dataset.cfxSelectionId}))
            """);
        var local = result.GetProperty("local").EnumerateArray().ToArray();
        Assert.Equal(new[] { "cfx-topology-select", "cfx-topology-viewport" }, local.Select(item => item.GetProperty("name").GetString()));
        var outer = result.GetProperty("outer").EnumerateArray().ToArray();
        Assert.Equal(new[] { "cfx-topology-select", "cfx-topology-viewport" }, outer.Select(item => item.GetProperty("name").GetString()));
        Assert.All(outer, item => AssertOutsideShadow(item, shadowMode));
        var selection = outer[0].GetProperty("detail");
        Assert.Equal("a", selection.GetProperty("id").GetString());
        Assert.Equal("node", selection.GetProperty("kind").GetString());
        Assert.Equal("API", selection.GetProperty("label").GetString());
        Assert.True(selection.GetProperty("selected").GetBoolean());
        var viewport = outer[1].GetProperty("detail");
        Assert.Equal("shadow-topology-source", viewport.GetProperty("chartId").GetString());
        Assert.Equal(1.2, viewport.GetProperty("state").GetProperty("zoom").GetDouble());
        Assert.Equal("1.200", result.GetProperty("sourceZoom").GetString());
        Assert.Equal(result.GetProperty("sourceZoom").GetString(), result.GetProperty("peerZoom").GetString());
        Assert.Equal("a", result.GetProperty("peerSelection").GetString());
        var commands = result.GetProperty("commands").EnumerateArray().ToArray();
        Assert.Equal(new[] { "selection", "viewport" }, commands.Select(item => item.GetProperty("detail").GetProperty("action").GetString()));
        Assert.All(commands, item => {
            Assert.False(item.GetProperty("bubbles").GetBoolean());
            Assert.False(item.GetProperty("composed").GetBoolean());
            Assert.Equal("shadow-topology-source", item.GetProperty("detail").GetProperty("chartId").GetString());
            Assert.Equal("shadow-legacy-topology", item.GetProperty("detail").GetProperty("group").GetString());
        });
        Assert.Equal(0, result.GetProperty("leakedCommands").GetInt32());
        await CaptureAsync(page, "topology-" + shadowMode, result, pageErrors);
        Assert.Empty(pageErrors);
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData("open")]
    [InlineData("closed")]
    public async Task IconBrowserSelectionCrossesShadowHostWithItsStableIconIdentity(string shadowMode) {
        if (!Enabled) return;
        var catalog = new TopologyIconCatalog().AddPack(new TopologyIconPack("shadow-icons", "Shadow Icons", vendor: "Example")
            .AddIcon(new TopologyIconDefinition("shadow-icons", "worker", "Worker", TopologyNodeKind.Application, TopologyIconShape.Application) {
                Symbol = "W", Category = "Services"
            }));
        var html = catalog.ToStencilBrowserHtmlPage(new TopologyIconStencilBrowserOptions {
            Id = "shadow-icon-browser", IncludeBuiltInPacks = false, PacksPerRow = 1, ColumnsPerPack = 1
        });
        await using var session = await OpenAsync(html, 1200, 900);
        var page = session.Page;
        var pageErrors = new List<string>();
        page.PageError += (_, error) => pageErrors.Add(error);
        await MoveBoundRootToShadowAsync(page, "[data-cfx-icon-browser=true]", shadowMode, new[] { "cfx-icon-browser-select" });
        await ClickRetainedRootAsync(page, "[data-cfx-role=topology-node][data-node-icon-id='shadow-icons:worker']");
        var result = await page.EvaluateAsync<JsonElement>("""
            () => JSON.parse(JSON.stringify({local:legacyLocalEvents,outer:legacyOuterEvents,
                selectedIcon:legacyShadowRoot.querySelector('.cfx-icon-browser-selected')?.getAttribute('data-node-icon-id'),
                inspectorId:legacyShadowRoot.querySelector('[data-cfx-icon-detail="id"]').textContent}))
            """);
        Assert.Single(result.GetProperty("local").EnumerateArray());
        var notification = Assert.Single(result.GetProperty("outer").EnumerateArray());
        Assert.Equal("cfx-icon-browser-select", notification.GetProperty("name").GetString());
        AssertOutsideShadow(notification, shadowMode);
        var detail = notification.GetProperty("detail");
        Assert.Equal("shadow-icons:worker", detail.GetProperty("iconId").GetString());
        Assert.Equal("shadow-icons", detail.GetProperty("iconPack").GetString());
        Assert.Equal("Worker", detail.GetProperty("iconLabel").GetString());
        Assert.Equal("Example", detail.GetProperty("vendor").GetString());
        Assert.Equal("Services", detail.GetProperty("category").GetString());
        Assert.Equal("shadow-icons:worker", result.GetProperty("selectedIcon").GetString());
        Assert.Equal("shadow-icons:worker", result.GetProperty("inspectorId").GetString());
        await CaptureAsync(page, "icon-browser-" + shadowMode, result, pageErrors);
        Assert.Empty(pageErrors);
        AssertNoConsoleErrors(session);
    }

    private static TopologyChart Topology(string id) => TopologyChart.Create().WithId(id)
        .WithViewport(640, 300, 20).WithLegend(null)
        .AddNode("a", "API", 80, 120, width: 120, height: 70)
        .AddNode("b", "Worker", 400, 120, width: 120, height: 70)
        .AddEdge("ab", "a", "b");

    private static Task MoveBoundRootToShadowAsync(IPage page, string selector, string mode, string[] names) => page.EvaluateAsync("""
        ({selector,mode,names}) => {
            const root=document.querySelector(selector),host=document.createElement('div');
            host.id='legacy-shadow-host';root.before(host);
            window.legacyShadowRoot=root;window.legacyShadowHost=host;
            window.legacyLocalEvents=[];window.legacyOuterEvents=[];
            const styles=[...document.querySelectorAll('style')].map(style=>style.cloneNode(true));
            host.attachShadow({mode}).append(...styles,root);
            for(const name of names) {
                root.addEventListener(name,event=>legacyLocalEvents.push({name,bubbles:event.bubbles,composed:event.composed}));
                document.addEventListener(name,event=>{
                    if(event.target!==host)return;
                    legacyOuterEvents.push({name,bubbles:event.bubbles,composed:event.composed,retargeted:event.target===host,
                        pathIncludesRoot:event.composedPath().includes(root),detail:JSON.parse(JSON.stringify(event.detail))});
                });
            }
        }
        """, new { selector, mode, names });

    private static async Task ClickRetainedRootAsync(IPage page, string selector) {
        var position = await page.EvaluateAsync<double[]>("""
            selector=>{const node=legacyShadowRoot.querySelector(selector);node.scrollIntoView({block:'center',inline:'center'});
                const box=node.getBoundingClientRect();return [box.x+box.width/2,box.y+box.height/2]}
            """, selector);
        await page.Mouse.ClickAsync((float)position[0], (float)position[1]);
    }

    private static void AssertOutsideShadow(JsonElement notification, string shadowMode) {
        Assert.True(notification.GetProperty("bubbles").GetBoolean());
        Assert.True(notification.GetProperty("composed").GetBoolean());
        Assert.True(notification.GetProperty("retargeted").GetBoolean());
        Assert.Equal(shadowMode == "open", notification.GetProperty("pathIncludesRoot").GetBoolean());
    }

    private static async Task CaptureAsync(IPage page, string name, JsonElement result, IReadOnlyCollection<string> pageErrors) {
        var captures = Environment.GetEnvironmentVariable("CFX_V2_CAPTURE_ROOT");
        if (string.IsNullOrWhiteSpace(captures)) return;
        var directory = Path.Combine(captures, "shadow-legacy-events");
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, name + ".json"), JsonSerializer.Serialize(new {
            runtime = result, pageErrorCount = pageErrors.Count, pageErrors
        }));
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + ".png"), Animations = ScreenshotAnimations.Disabled });
    }
}
