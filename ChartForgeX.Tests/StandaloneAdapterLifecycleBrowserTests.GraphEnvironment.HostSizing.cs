using System.Text.Json;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class StandaloneAdapterLifecycleBrowserTests {
    // Reuse the six environment inputs; three wide inputs also cover the same intrinsic-size contract in native host shapes.
    private static async Task AssertGraphEnvironmentHostSizingAsync(HtmlGraphRenderBackend backend) {
        foreach (var host in new[] { "page", "fragment", "fill-available" }) {
            var graph = GraphEnvironmentFixture(backend);
            void Configure(HtmlGraphExplorerOptions options) {
                options.RenderBackend = backend;
                options.Theme = HtmlGraphExplorerTheme.Light;
                options.PersistThemePreference = false;
                options.FillAvailableHeight = host == "fill-available";
            }
            var html = host == "fragment"
                ? "<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">"
                    + "<style>body{margin:0}main{box-sizing:border-box;width:calc(100% - 24px);height:calc(100vh - 24px);margin:12px;padding:8px;border:1px solid #888;overflow:auto}"
                    + "@media print{main{width:100%;height:auto;margin:0;padding:0;border:0;overflow:visible}}</style></head><body><main>"
                    + graph.ToGraphExplorerHtmlFragment(Configure) + "</main></body></html>"
                : graph.ToGraphExplorerHtmlPage(Configure);
            await using var session = await OpenAsync(Instrument(html), 980, 900);
            var page = session.Page;
            await page.EvaluateAsync("""
                ()=>{window.retainedGraph=document.querySelector('.cfx-graph-explorer');
                    window.hostSizingExports=[];retainedGraph.addEventListener('cfxgraphexport',event=>{event.preventDefault();hostSizingExports.push(event.detail.format);});
                    lifetimeProbe.recordGlobals=false;lifetimeProbe.recordObservers=false;}
                """);
            foreach (var size in new[] { (Width: 980, Height: 900), (Width: 390, Height: 900), (Width: 980, Height: 390) }) {
                await page.SetViewportSizeAsync(size.Width, size.Height);
                await page.EvaluateAsync("()=>new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve)))");
                var name = "graph-host-sizing-" + backend + "-" + host + "-" + size.Width + "x" + size.Height;
                await page.GetByRole(AriaRole.Button, new() { Name = "Fit graph to view", Exact = true }).ClickAsync();
                var facts = await page.EvaluateAsync<JsonElement>("""
                    ()=>{
                        const root=retainedGraph,stage=root.querySelector('.cfx-graph-stage'),parent=root.closest('main'),box=root.getBoundingClientRect(),
                            stageBox=stage.getBoundingClientRect(),hostBox=(parent||document.documentElement).getBoundingClientRect(),
                            controls=[...root.querySelectorAll('button,input,select,summary')].filter(node=>node.getClientRects().length),
                            outside=rect=>Math.max(box.left-rect.left,rect.right-box.right,hostBox.left-rect.left,rect.right-hostBox.right,0);
                        return {requested:root.dataset.cfxGraphRenderer,actual:root.dataset.cfxGraphRendererActive,viewport:{width:innerWidth,height:innerHeight},
                            root:box.toJSON(),stage:stageBox.toJSON(),physicalParent:hostBox.toJSON(),physicalParentOverflow:parent?parent.scrollWidth-parent.clientWidth:0,
                            pageOverflow:document.documentElement.scrollWidth-innerWidth,stageOverflow:outside(stageBox),
                            controlOverflow:Math.max(0,...controls.map(node=>outside(node.getBoundingClientRect()))),
                            fillVerticalOverflow:root.classList.contains('cfx-graph-fill-available')?Math.max(0,...controls.map(node=>{
                                const rect=node.getBoundingClientRect();return Math.max(-rect.top,rect.bottom-innerHeight);})):0,
                            controls:controls.map(node=>({name:node.getAttribute('aria-label')||node.textContent,box:node.getBoundingClientRect().toJSON()}))};
                    }
                    """);
                await WriteReceiptAsync(name, facts);
                await CaptureAsync(page, name);
                foreach (var property in new[] { "pageOverflow", "physicalParentOverflow", "stageOverflow", "controlOverflow", "fillVerticalOverflow" }) {
                    Assert.InRange(facts.GetProperty(property).GetDouble(), 0, 1);
                }
                await ClickGraphEnvironmentPointAsync(page, "a");
                Assert.Equal("a", await GraphEnvironmentPrimaryAsync(page));
                var export = page.GetByRole(AriaRole.Button, new() { Name = "Export graph", Exact = true });
                await ReachGraphEnvironmentControlAsync(page, export);
                await page.Keyboard.PressAsync("Enter");
                await WaitForGraphEnvironmentExportAsync(page, expanded: true);
                await AssertGraphEnvironmentMenuPaintAsync(page, name);
                await page.GetByRole(AriaRole.Button, new() { Name = "Export SVG document", Exact = true }).ClickAsync();
                Assert.Equal("svg", await page.EvaluateAsync<string>("()=>hostSizingExports.at(-1)"));
                await WaitForGraphEnvironmentExportAsync(page, expanded: false);
                if (size.Width == 980 && size.Height == 900) {
                    await AssertGraphEnvironmentPrintAsync(page, name, await GraphCommittedStateAsync(page));
                    await SetGraphEnvironmentPreferencesAsync(page, enabled: false);
                }
                await page.GetByRole(AriaRole.Button, new() { Name = "Clear selection", Exact = true }).ClickAsync();
            }
            Assert.True(await page.EvaluateAsync<bool>("()=>ChartForgeXGraphExplorer.dispose(retainedGraph)"));
            await AssertReleasedAsync(page, "graph-host-sizing-" + backend + "-" + host + "-released");
            AssertNoConsoleErrors(session);
        }
    }
}
