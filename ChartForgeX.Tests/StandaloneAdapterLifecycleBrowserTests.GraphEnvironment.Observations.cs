using System.Text.Json;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class StandaloneAdapterLifecycleBrowserTests {
    private static async Task SetGraphEnvironmentPreferencesAsync(IPage page, bool enabled) {
        await page.EmulateMediaAsync(new() {
            Media = Media.Screen,
            ForcedColors = enabled ? ForcedColors.Active : ForcedColors.None,
            ReducedMotion = enabled ? ReducedMotion.Reduce : ReducedMotion.NoPreference
        });
        await page.WaitForFunctionAsync("""
            enabled=>retainedGraph.dataset.cfxGraphForcedColors===String(enabled)
                &&retainedGraph.dataset.cfxGraphReducedMotion===String(enabled)
                &&matchMedia('(forced-colors:active)').matches===enabled
                &&matchMedia('(prefers-reduced-motion:reduce)').matches===enabled
            """, enabled);
    }

    // Traverse the real tab order: programmatic focus alone does not qualify navigation or keyboard modality.
    private static async Task ReachGraphEnvironmentControlAsync(IPage page, ILocator target) {
        for (var step = 0; step < 64; step++) {
            if (await target.EvaluateAsync<bool>("node=>node===document.activeElement&&node.matches(':focus-visible')")) return;
            await page.Keyboard.PressAsync("Tab");
        }
        Assert.Fail("The graph target was not reachable through the fixture's bounded native Tab order.");
    }

    private static Task<string> GraphEnvironmentPrimaryAsync(IPage page) =>
        page.EvaluateAsync<string>("()=>ChartForgeXGraphExplorer.get(retainedGraph).selection.primary||''");

    private static async Task ClickGraphEnvironmentPointAsync(IPage page, string id) {
        await page.Locator(".cfx-graph-stage").ScrollIntoViewIfNeededAsync();
        var witness = await GraphEnvironmentWitnessAsync(page, id);
        Assert.True(witness.GetProperty("insideViewport").GetBoolean(), witness.GetRawText());
        Assert.True(witness.GetProperty("nativeHit").GetBoolean(), "The trusted pointer must hit the rendered mark or its actual accelerated surface: " + witness.GetRawText());
        var center = witness.GetProperty("center");
        await page.Mouse.ClickAsync((float)center.GetProperty("x").GetDouble(), (float)center.GetProperty("y").GetDouble());
    }

    // Public source positions/camera and the observed surface rectangle locate the screenshot witness.
    private static Task<JsonElement> GraphEnvironmentWitnessAsync(IPage page, string id) => page.EvaluateAsync<JsonElement>("""
        id=>{
            const root=retainedGraph,actual=root.dataset.cfxGraphRendererActive,
                surface=root.querySelector('[data-cfx-role="graph-'+(actual==='svg'?'scene':actual)+'"]'),box=surface.getBoundingClientRect(),
                svg=root.querySelector('[data-cfx-role=graph-scene]'),domain=svg.viewBox.baseVal,
                point=ChartForgeXGraphExplorer.positions(root).find(point=>point.id===id),
                node=ChartForgeXGraphExplorer.get(root).nodes.find(node=>node.id===id),camera=ChartForgeXGraphExplorer.captureState(root).viewport,
                fit=Math.min(box.width/domain.width,box.height/domain.height),
                x=box.left+(box.width-domain.width*fit)/2+(point.x*camera.scale+camera.x)*fit,
                y=box.top+(box.height-domain.height*fit)/2+(point.y*camera.scale+camera.y)*fit,
                hit=document.elementFromPoint(x,y),owner=hit?.closest('[data-cfx-role=graph-node]');
            return {id,actual,center:{x,y},radius:node.size*camera.scale*fit,viewport:{width:innerWidth,height:innerHeight},
                insideViewport:x>0&&x<innerWidth&&y>0&&y<innerHeight,
                nativeHit:actual==='svg'?owner?.dataset.nodeId===id:hit===surface,
                hit:hit?.outerHTML.slice(0,600),surface:box.toJSON(),camera};
        }
        """, id);

    private static async Task AssertGraphEnvironmentTextContrastAsync(IPage page, ILocator target) {
        var paints = await target.EvaluateAsync<string[]>("""
            node=>{const backgrounds=[];for(let owner=node;owner;owner=owner.parentElement)backgrounds.unshift(getComputedStyle(owner).backgroundColor);
                return [getComputedStyle(node).color,...backgrounds];}
            """);
        var rgba = await ResolveCssColorsAsync(page, paints);
        ChartColor Color(int[] value) => ChartColor.FromRgba((byte)value[0], (byte)value[1], (byte)value[2], (byte)value[3]);
        var background = ChartColor.White;
        for (var index = 1; index < rgba.Length; index++) background = ChartColorMath.CompositeOverOpaque(Color(rgba[index]), background);
        var ink = ChartColorMath.CompositeOverOpaque(Color(rgba[0]), background);
        Assert.True(ChartColorMath.ContrastRatio(ink, background) >= 4.5,
            "The actual graph control/readout text must contrast with its composited host surface.");
    }

    private static async Task CaptureGraphEnvironmentAsync(IPage page, string name) {
        var facts = await page.EvaluateAsync<JsonElement>("""
            ()=>{
                const root=retainedGraph,active=document.activeElement,visible=node=>node.getClientRects().length&&getComputedStyle(node).visibility!=='hidden',
                    controls=[...root.querySelectorAll('button,input,select,summary')].filter(visible);
                return {requested:root.dataset.cfxGraphRenderer,actual:root.dataset.cfxGraphRendererActive,fallback:root.dataset.cfxGraphRendererFallback,
                    webGlContextAvailable:root.dataset.cfxGraphRenderer==='webgl'?!!root.querySelector('[data-cfx-role=graph-webgl]').getContext('webgl2'):null,
                    forced:matchMedia('(forced-colors:active)').matches,reduced:matchMedia('(prefers-reduced-motion:reduce)').matches,
                    print:matchMedia('print').matches,hostCssZoom:getComputedStyle(document.documentElement).zoom,
                    language:document.documentElement.lang,direction:getComputedStyle(root).direction,
                    selection:ChartForgeXGraphExplorer.captureState(root).selection,viewport:ChartForgeXGraphExplorer.captureState(root).viewport,
                    highlighted:[...root.querySelectorAll('[data-cfx-highlighted=true]')].map(node=>node.dataset.nodeId||node.dataset.edgeId),
                    readout:{hidden:root.querySelector('.cfx-graph-tooltip').hidden,text:root.querySelector('.cfx-graph-tooltip').textContent},
                    focus:{tag:active?.localName,name:root.contains(active)?active?.getAttribute('aria-label')||active?.textContent:'',visible:active?.matches(':focus-visible')},
                    pageOverflow:Math.max(0,document.documentElement.scrollWidth-innerWidth),
                    controls:controls.map(node=>({tag:node.localName,name:node.getAttribute('aria-label')||[...(node.labels||[])].map(label=>label.textContent).join(' ')||node.textContent,
                        box:node.getBoundingClientRect().toJSON(),disabled:node.disabled,expanded:node.getAttribute('aria-expanded'),transition:getComputedStyle(node).transitionDuration}))};
            }
            """);
        await WriteReceiptAsync(name, facts);
        await CaptureAsync(page, name);
        Assert.InRange(facts.GetProperty("pageOverflow").GetDouble(), 0, 1);
    }

    private static async Task AssertGraphEnvironmentPrintAsync(IPage page, string name, string committed) {
        await page.EmulateMediaAsync(new() { Media = Media.Print, ForcedColors = ForcedColors.Active, ReducedMotion = ReducedMotion.Reduce });
        await WaitForGraphPrintSurfaceAsync(page);
        var printed = await page.EvaluateAsync<JsonElement>("""
            ()=>{
                const surfaces=[...document.querySelectorAll('[data-cfx-role=graph-print-surface]')],svg=surfaces[0].querySelector('svg'),box=svg.getBoundingClientRect(),
                    visible=node=>node.getClientRects().length&&getComputedStyle(node).visibility!=='hidden',
                    details=[...svg.querySelectorAll('[data-cfx-role=graph-node],[data-cfx-role=graph-node-details],[data-cfx-role=graph-edge-label]')].filter(visible);
                return {surfaces:surfaces.length,nodes:[...svg.querySelectorAll('[data-cfx-role=graph-node]')].map(node=>node.dataset.nodeId).sort(),
                    label:svg.querySelector('[data-node-id=a]')?.getAttribute('data-node-label'),
                    controls:[...retainedGraph.querySelectorAll('button,input,select,summary,.cfx-graph-tooltip')].filter(visible).length,
                    overflow:Math.max(0,...details.map(node=>{const rect=node.getBoundingClientRect();return Math.max(box.left-rect.left,rect.right-box.right,box.top-rect.top,rect.bottom-box.bottom);})),
                    surface:box.toJSON(),viewBox:svg.getAttribute('viewBox')};
            }
            """);
        await WriteReceiptAsync(name + "-print", printed);
        await CaptureAsync(page, name + "-print");
        Assert.Equal(1, printed.GetProperty("surfaces").GetInt32());
        Assert.Equal(new[] { "a", "b", "c", "d" }, printed.GetProperty("nodes").EnumerateArray().Select(value => value.GetString()).ToArray());
        Assert.Equal(GraphEnvironmentLabel, printed.GetProperty("label").GetString());
        Assert.Equal(0, printed.GetProperty("controls").GetInt32());
        Assert.InRange(printed.GetProperty("overflow").GetDouble(), 0, 1);
        Assert.Equal(committed, await GraphCommittedStateAsync(page));
        await page.EmulateMediaAsync(new() { Media = Media.Screen, ForcedColors = ForcedColors.Active, ReducedMotion = ReducedMotion.Reduce });
        await page.WaitForFunctionAsync("()=>!document.querySelector('[data-cfx-role=graph-print-surface]')");
        Assert.Equal(committed, await GraphCommittedStateAsync(page));
        await CaptureGraphEnvironmentAsync(page, name + "-screen-restored");
    }
}
