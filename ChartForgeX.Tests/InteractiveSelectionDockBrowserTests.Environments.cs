using System.Text.Json;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveSelectionDockBrowserTests {
    [BrowserTheory]
    [Trait("BrowserSuite", "Compatibility")]
    [InlineData(false, 980)]
    [InlineData(true, 390)]
    public async Task ForcedPaletteReducedMotionAndHostZoomKeepControlsReadoutsFocusAndPrintUsable(bool dark, int width) {
        if (!Enabled) return;
        await using var session = await OpenAsync(PagedReadings(dark).ToInteractiveHtmlPage(options => {
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
            options.Interaction.Enable(ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan);
        }), width, 900);
        var page = session.Page;
        await page.EmulateMediaAsync(new() { ForcedColors = ForcedColors.Active, ReducedMotion = ReducedMotion.Reduce });
        await page.EvaluateAsync("()=>document.documentElement.style.zoom='1.5'");
        Assert.True(await page.EvaluateAsync<bool>("()=>matchMedia('(forced-colors:active)').matches&&matchMedia('(prefers-reduced-motion:reduce)').matches"));
        await page.Locator("[data-cfx-zoom=in]").ClickAsync();
        Assert.Equal("0s", await page.Locator(".cfx-stage svg").EvaluateAsync<string>("node=>getComputedStyle(node).transitionDuration"));
        await page.Locator("[data-cfx-reset]").ClickAsync();
        var target = page.Locator("g[data-cfx-observation-id=reading-7]");
        await FocusWithKeyboardAsync(page, target);
        await CaptureEnvironmentAsync(page, "focus-" + dark);
        await AssertEnvironmentFocusAsync(page, target);
        Assert.Contains("Observation 08", await TooltipTextAsync(page));
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await target.GetAttributeAsync("aria-selected"));
        var paint = await page.Locator(".cfx-tooltip").EvaluateAsync<string[]>(
            "tip=>{const style=getComputedStyle(tip);return[style.color,style.backgroundColor]}" );
        var channels = await ResolveCssColorsAsync(page, paint);
        ChartColor Color(int[] value) => ChartColor.FromRgba((byte)value[0], (byte)value[1], (byte)value[2], (byte)value[3]);
        Assert.True(ChartColorMath.ContrastRatio(Color(channels[0]), Color(channels[1])) >= 4.5,
            "Forced-palette readouts retain readable text against their actual surface.");
        await CaptureEnvironmentAsync(page, "screen-" + dark);
        await page.EvaluateAsync("""
            ()=>{
                const root=document.querySelector('.cfx-interactive-chart');
                ChartForgeXCharts.get(root).unpinTooltip();
                const host=document.createElement('button');host.textContent='Continue report';document.body.appendChild(host);host.focus();
                window.environmentPointer=[];
                root.addEventListener('pointermove',event=>environmentPointer.push({x:event.clientX,y:event.clientY,
                    target:event.target.closest('[data-cfx-target-id]')?.dataset.cfxTargetId,hit:document.elementFromPoint(event.clientX,event.clientY)?.outerHTML.slice(0,800)}));
            }
            """);
        await CaptureEnvironmentAsync(page, "host-focus-" + dark);
        await target.ScrollIntoViewIfNeededAsync();
        var position = await target.EvaluateAsync<float[]>("node=>{const box=node.getBoundingClientRect();return[(box.left+box.right)/2,(box.top+box.bottom)/2]}" );
        await page.Mouse.MoveAsync(position[0], position[1], new() { Steps = 4 });
        await CaptureEnvironmentAsync(page, "pointer-" + dark);
        Assert.Contains("Observation 08", await TooltipTextAsync(page));
        await page.EmulateMediaAsync(new() { Media = Media.Print, ForcedColors = ForcedColors.Active, ReducedMotion = ReducedMotion.Reduce });
        var printed = await page.EvaluateAsync<JsonElement>("""
            ()=>{
                const root=document.querySelector('.cfx-interactive-chart'),svg=root.querySelector('.cfx-stage svg');
                return {transform:getComputedStyle(svg).transform,
                    controls:[...root.querySelectorAll('.cfx-toolbar,.cfx-tooltip,.cfx-compare-tray,[data-cfx-keyboard-focus-outline]')].map(node=>getComputedStyle(node).display),
                    viewBox:svg.getAttribute('viewBox'),selection:ChartForgeXCharts.get(root).getSelection().length};
            }
            """);
        Assert.Equal("none", printed.GetProperty("transform").GetString());
        Assert.All(printed.GetProperty("controls").EnumerateArray(), item => Assert.Equal("none", item.GetString()));
        Assert.Equal(1, printed.GetProperty("selection").GetInt32());
        await CaptureEnvironmentAsync(page, "print-" + dark);
        await page.EmulateMediaAsync(new() { Media = Media.Screen, ForcedColors = ForcedColors.Active, ReducedMotion = ReducedMotion.Reduce });
        Assert.True(await page.Locator(".cfx-toolbar").IsVisibleAsync());
        Assert.True(await page.Locator("[data-cfx-compare-tray]").IsVisibleAsync());
        Assert.Equal("true", await target.GetAttributeAsync("aria-selected"));
        Assert.True(await page.EvaluateAsync<bool>("()=>document.documentElement.scrollWidth<=innerWidth+1"));
        AssertNoConsoleErrors(session);
    }

    private static async Task AssertEnvironmentFocusAsync(IPage page, ILocator target) {
        var actual = await target.EvaluateAsync<JsonElement>("""
            target=>{
                const ink=target.closest('.cfx-interactive-chart').querySelector('[data-cfx-focus-ink]'),
                    ring=ink.getBoundingClientRect(),mark=target.getBoundingClientRect();
                return {ring:ring.toJSON(),mark:mark.toJSON(),stroke:getComputedStyle(ink).stroke,width:getComputedStyle(ink).strokeWidth,
                    contains:(mark.left+mark.right)/2>=ring.left-1&&(mark.left+mark.right)/2<=ring.right+1
                        &&(mark.top+mark.bottom)/2>=ring.top-1&&(mark.top+mark.bottom)/2<=ring.bottom+1};
            }
            """);
        Assert.True(actual.GetProperty("contains").GetBoolean(), "The rendered ring surrounds the actual host-scaled mark: " + actual.GetRawText());
        Assert.Equal("2px", actual.GetProperty("width").GetString());
        var screenshot = await page.ScreenshotAsync();
        var painted = await page.EvaluateAsync<bool>("""
            async({png,ring,stroke})=>{
                const image=await new Promise(resolve=>{const image=new Image();image.onload=()=>resolve(image);image.src='data:image/png;base64,'+png});
                const canvas=document.createElement('canvas');canvas.width=image.naturalWidth;canvas.height=image.naturalHeight;
                const context=canvas.getContext('2d');context.drawImage(image,0,0);
                const swatch=document.createElement('canvas').getContext('2d');swatch.fillStyle=stroke;swatch.fillRect(0,0,1,1);
                const expected=swatch.getImageData(0,0,1,1).data,scale=image.naturalWidth/innerWidth;
                // At 150% host zoom a stroke can straddle physical pixels. Sample the rendered perimeter,
                // retaining a small channel allowance for native antialiasing instead of assuming an opaque centre.
                for(const point of [[(ring.left+ring.right)/2,ring.top],[ring.left,(ring.top+ring.bottom)/2],[ring.right,(ring.top+ring.bottom)/2]])
                    for(let dx=-2;dx<=2;dx++)for(let dy=-2;dy<=2;dy++){
                        const rgba=context.getImageData(Math.round(point[0]*scale)+dx,Math.round(point[1]*scale)+dy,1,1).data;
                        if(rgba.every((value,index)=>Math.abs(value-expected[index])<=10))return true;
                    }
                return false;
            }
            """, new { png = Convert.ToBase64String(screenshot), ring = new {
                left = actual.GetProperty("ring").GetProperty("left").GetDouble(), right = actual.GetProperty("ring").GetProperty("right").GetDouble(),
                top = actual.GetProperty("ring").GetProperty("top").GetDouble(), bottom = actual.GetProperty("ring").GetProperty("bottom").GetDouble()
            }, stroke = actual.GetProperty("stroke").GetString() });
        Assert.True(painted, "The screenshot contains the focused mark's visible stroke under host scaling.");
    }

    private static async Task CaptureEnvironmentAsync(IPage page, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        var receipt = await page.EvaluateAsync<object>("""
            ()=>{
                const root=document.querySelector('.cfx-interactive-chart'),style=node=>{const s=getComputedStyle(node);return {color:s.color,background:s.backgroundColor,border:s.borderColor,outline:s.outlineColor,transition:s.transitionDuration}},
                    active=root.getRootNode().activeElement,source=root.querySelector('g[data-cfx-observation-id=reading-7]');
                return {forced:matchMedia('(forced-colors:active)').matches,reduced:matchMedia('(prefers-reduced-motion:reduce)').matches,
                    media:matchMedia('print').matches?'print':'screen',hostCssZoom:getComputedStyle(document.documentElement).zoom,
                    pointer:window.environmentPointer,readout:{hidden:root.querySelector('.cfx-tooltip').hidden,text:root.querySelector('.cfx-tooltip').textContent},
                    root:{...root.dataset},source:{box:source.getBoundingClientRect().toJSON(),region:source._cfxNativeRegion,
                        matrix:['a','b','c','d','e','f'].map(key=>source.getScreenCTM()[key]),
                        hit:(()=>{const box=source.getBoundingClientRect();return document.elementFromPoint((box.left+box.right)/2,(box.top+box.bottom)/2)?.outerHTML.slice(0,1000)})()},
                    focus:{tag:active?.localName,data:{...active?.dataset},box:active?.getBoundingClientRect().toJSON(),
                        matrix:(()=>{const matrix=active?.getScreenCTM?.();return matrix&&['a','b','c','d','e','f'].map(key=>matrix[key])})(),region:active?._cfxNativeRegion,
                        ink:(()=>{const ink=root.querySelector('[data-cfx-focus-ink]');return ink&&{box:ink.getBoundingClientRect().toJSON(),bbox:ink.getBBox(),matrix:['a','b','c','d','e','f'].map(key=>ink.getScreenCTM()[key]),stroke:getComputedStyle(ink).stroke}})()},
                    controls:[...root.querySelectorAll('.cfx-tool,.cfx-compare-chip,.cfx-compare-page,.cfx-tooltip')].map(node=>({text:node.textContent,box:node.getBoundingClientRect().toJSON(),style:style(node)}))};
            }
            """);
        await File.WriteAllTextAsync(Path.Combine(directory, "environment-" + name + ".json"), JsonSerializer.Serialize(receipt));
        await page.ScreenshotAsync(new() { Path = Path.Combine(directory, "environment-" + name + ".png"), FullPage = false });
    }
}
