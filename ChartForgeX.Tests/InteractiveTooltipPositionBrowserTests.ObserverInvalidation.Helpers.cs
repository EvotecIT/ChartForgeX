using System.Text.Json;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveTooltipPositionBrowserTests {
    private static string PositionObserverHtml(HtmlChartTooltipAnchor anchor) =>
        "<html><head><style>svg [hidden]{display:none}.position-hidden-opacity{opacity:0}#position-observer-host .position-hidden-target-opacity{opacity:0!important}.position-hidden-visibility{visibility:hidden}</style></head><body id='position-observer-host' style='margin:20px'><div id='outside-content'></div>" +
        Observations(false).ToInteractiveHtmlFragment(options => {
            options.Tooltip.Position.Anchor = anchor;
            options.Tooltip.Mode = HtmlChartTooltipMode.Single;
        }) + "<aside id='unrelated-dashboard' style='position:fixed;left:8px;bottom:8px'><span>Background dashboard</span></aside></body></html>";

    private static async Task AcquireObservedPositionAsync(IPage page, bool pinned) {
        if (pinned) {
            await page.Locator(Reading).FocusAsync();
            await page.Keyboard.PressAsync("Space");
        } else await PointerAsync(page);
        await SettleObservedPositionAsync(page);
    }

    private static Task SettleObservedPositionAsync(IPage page) => page.EvaluateAsync("async()=>{for(let frame=0;frame<3;frame++)await new Promise(resolve=>requestAnimationFrame(resolve));}");

    private static Task InstallPositionWorkProbeAsync(IPage page) => page.EvaluateAsync("""
        ()=>{
            const tip=document.querySelector('.cfx-tooltip'),nativeStyle=window.getComputedStyle;
            window.positionWork={paintStyleReads:0,placementStyleReads:0,tipStyleWrites:0};
            window.getComputedStyle=(node,...args)=>{
                // Attribute the canonical paint work to the position owner; keyboard reconciliation has its own boundary.
                if(new Error().stack.includes('tooltipPositionAvailable'))window.positionWork.paintStyleReads++;
                if(node===tip)window.positionWork.placementStyleReads++;
                return nativeStyle.call(window,node,...args);
            };
            new MutationObserver(records=>window.positionWork.tipStyleWrites+=records.length).observe(tip,{attributes:true,attributeFilter:['style']});
        }
        """);

    private static void AssertPositionWatchersReleased(JsonElement baseline, JsonElement released) {
        foreach (var name in new[] { "resize", "scroll", "transitionend", "observed" })
            Assert.Equal(baseline.GetProperty(name).GetInt32(), released.GetProperty(name).GetInt32());
    }

    private static Task<JsonElement> PaintDashboardStateAsync(IPage page) => page.EvaluateAsync<JsonElement>("""
        ()=>({readouts:positionPaintRoots.map(root=>({hidden:root.querySelector('.cfx-tooltip').hidden,pinned:root.dataset.cfxTooltipPinned==='true'})),
            watchers:{resize:positionListeners.resize.size,scroll:positionListeners.scroll.size,transitionend:positionListeners.transitionend.size,
                observed:Array.from(positionObserved.values()).reduce((sum,nodes)=>sum+nodes.size,0)}})
        """);
}
