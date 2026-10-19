using System.Text.Json;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

internal static partial class InteractiveChartBrowser {
    /// <summary>
    /// Establishes real keyboard input before a host moves focus to a specific native target.
    /// Navigation-order contracts use their own Tab/arrow traversal instead of this modality setup.
    /// </summary>
    internal static async Task FocusWithKeyboardAsync(IPage page, ILocator target) {
        await page.Keyboard.PressAsync("Tab");
        await target.FocusAsync();
        Assert.True(await target.EvaluateAsync<bool>("node => node.getRootNode().activeElement === node"), "The requested native target owns focus.");
        Assert.True(await target.EvaluateAsync<bool>("node => node.matches(':focus-visible')"), "The requested target has keyboard-visible focus.");
    }

    /// <summary>Checks the active target's native two-pixel focus outline and its visible screenshot paint.</summary>
    internal static async Task AssertNativeKeyboardFocusAsync(IPage page) {
        var sample = await page.Locator("[data-cfx-focus-ink]").EvaluateAsync<JsonElement>("""
            node=>{
                const box=node.getBBox(),point=new DOMPoint(box.x+box.width/2,box.y).matrixTransform(node.getScreenCTM()),
                    active=node.getRootNode().activeElement,target=active.closest('[data-cfx-target-kind]')||active.querySelector('[data-cfx-target-kind]')||active,
                    region=target._cfxNativeRegion,bounds=target.getBoundingClientRect(),
                    center=region?new DOMPoint(region.x+region.width/2,region.y+region.height/2).matrixTransform(target.getScreenCTM())
                        :{x:(bounds.left+bounds.right)/2,y:(bounds.top+bounds.bottom)/2},ring=node.getBoundingClientRect(),style=getComputedStyle(node);
                return {x:point.x,y:point.y,stroke:style.stroke,width:style.strokeWidth,length:node.getTotalLength(),
                    ownsFocus:active.matches(':focus-visible')&&center.x>=ring.left-1&&center.x<=ring.right+1&&center.y>=ring.top-1&&center.y<=ring.bottom+1};
            }
            """);
        Assert.True(sample.GetProperty("length").GetDouble() > 0, "The keyboard indicator has native SVG stroke geometry.");
        Assert.Equal("2px", sample.GetProperty("width").GetString());
        Assert.True(sample.GetProperty("ownsFocus").GetBoolean(), "The native indicator surrounds the current keyboard target.");
        var screenshot = await page.ScreenshotAsync();
        var painted = await page.EvaluateAsync<bool>("""
            async ({png,x,y,stroke})=>{
                const image=await new Promise((resolve,reject)=>{const image=new Image();image.onload=()=>resolve(image);image.onerror=reject;image.src='data:image/png;base64,'+png;});
                const canvas=document.createElement('canvas');canvas.width=image.naturalWidth;canvas.height=image.naturalHeight;
                const context=canvas.getContext('2d');context.drawImage(image,0,0);
                const swatch=document.createElement('canvas').getContext('2d');swatch.fillStyle=stroke;swatch.fillRect(0,0,1,1);
                const expected=swatch.getImageData(0,0,1,1).data,scale=image.naturalWidth/innerWidth;
                for(let dx=-1;dx<=1;dx++)for(let dy=-1;dy<=1;dy++){
                    const actual=context.getImageData(Math.round(x*scale)+dx,Math.round(y*scale)+dy,1,1).data;
                    if(actual.every((value,index)=>Math.abs(value-expected[index])<=3))return true;
                }
                return false;
            }
            """, new { png = Convert.ToBase64String(screenshot), x = sample.GetProperty("x").GetDouble(), y = sample.GetProperty("y").GetDouble(), stroke = sample.GetProperty("stroke").GetString() });
        if (!painted && Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } captures) {
            if (!Path.IsPathRooted(captures)) throw new InvalidOperationException("Browser captures require an absolute task-owned directory.");
            Directory.CreateDirectory(captures);
            var name = "focus-stroke-failure-" + (page.ViewportSize?.Width ?? 0);
            await File.WriteAllBytesAsync(Path.Combine(captures, name + ".png"), screenshot);
            await File.WriteAllTextAsync(Path.Combine(captures, name + ".json"), sample.GetRawText());
        }
        Assert.True(painted, "The browser screenshot contains the keyboard indicator's visible accent stroke.");
    }

}
