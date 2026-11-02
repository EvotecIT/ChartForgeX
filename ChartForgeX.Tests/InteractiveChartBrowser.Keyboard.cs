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

    /// <summary>Checks the active target's two-pixel native or HTML focus outline and visible screenshot paint.</summary>
    internal static async Task AssertNativeKeyboardFocusAsync(IPage page, bool nativeCaption = false) {
        await page.WaitForFunctionAsync("()=>[...document.querySelectorAll('.cfx-interactive-chart')].every(root=>root.getAnimations({subtree:true}).every(animation=>animation.playState!=='running'||!Number.isFinite(animation.effect?.getComputedTiming().endTime)))");
        if (nativeCaption) await BrowserProofCaptions.InstallNativeAsync(page);
        await page.WaitForFunctionAsync("()=>document.querySelector('[data-cfx-focus-ink]')||document.activeElement?.matches('button:focus-visible')");
        var sample = await page.EvaluateAsync<JsonElement>("""
            nativeCaption=>{
                const activeElement=document.activeElement;
                if(activeElement.matches('button:focus-visible')){
                    const bounds=activeElement.getBoundingClientRect(),style=getComputedStyle(activeElement),outset=parseFloat(style.outlineOffset)+parseFloat(style.outlineWidth)/2,
                        left=bounds.left-outset,top=bounds.top-outset,width=bounds.width+2*outset,height=bounds.height+2*outset,length=2*(width+height);
                    const samples=Array.from({length:16},(_,index)=>{const p=length*index/16;return p<width?{x:left+p,y:top}:p<width+height?{x:left+width,y:top+p-width}:p<2*width+height?{x:left+2*width+height-p,y:top+height}:{x:left,y:top+length-p};});
                    return {x:left,y:top,samples,stroke:style.outlineStyle==='none'?'none':style.outlineColor,width:style.outlineWidth,length,
                        targetBounds:bounds.toJSON(),ringBounds:{left,top,right:left+width,bottom:top+height},
                        center:{x:bounds.left+bounds.width/2,y:bounds.top+bounds.height/2},ownsFocus:true};
                }
                const node=document.querySelector('[data-cfx-focus-ink]');
                const box=node.getBBox(),point=new DOMPoint(box.x+box.width/2,box.y).matrixTransform(node.getScreenCTM()),
                    active=node.getRootNode().activeElement,target=active.closest('[data-cfx-target-kind]')||active.querySelector('[data-cfx-target-kind]')||active,
                    region=target._cfxNativeRegion,bounds=target.getBoundingClientRect(),
                    caption=nativeCaption?window.cfxCaptionBox(target):null,
                    center=caption?{x:(caption.left+caption.right)/2,y:(caption.top+caption.bottom)/2}:region?new DOMPoint(region.x+region.width/2,region.y+region.height/2).matrixTransform(target.getScreenCTM())
                        :{x:(bounds.left+bounds.right)/2,y:(bounds.top+bounds.bottom)/2},ring=node.getBoundingClientRect(),style=getComputedStyle(node);
                const samples=Array.from({length:16},(_,index)=>{const p=node.getPointAtLength(node.getTotalLength()*index/16).matrixTransform(node.getScreenCTM());return{x:p.x,y:p.y};});
                const ancestors=[];for(let parent=node;parent;parent=parent.parentElement){const s=getComputedStyle(parent);ancestors.push({name:parent.localName,role:parent.dataset.cfxRole,opacity:s.opacity,transform:s.transform,filter:s.filter,classes:parent.getAttribute('class')});}
                return {x:point.x,y:point.y,samples,ancestors,stroke:style.stroke,width:style.strokeWidth,length:node.getTotalLength(),
                    targetBounds:bounds.toJSON(),ringBounds:ring.toJSON(),center,region,caption,
                    ownsFocus:active.matches(':focus-visible')&&center.x>=ring.left-1&&center.x<=ring.right+1&&center.y>=ring.top-1&&center.y<=ring.bottom+1};
            }
            """, nativeCaption);
        if (!sample.GetProperty("ownsFocus").GetBoolean()
            && Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } diagnostic) {
            Directory.CreateDirectory(diagnostic);
            var stem = Path.Combine(diagnostic, "focus-region-failure-" + Guid.NewGuid().ToString("N"));
            await File.WriteAllTextAsync(stem + ".json", sample.GetRawText());
            await File.WriteAllTextAsync(stem + ".svg", await page.Locator(".cfx-stage svg").EvaluateAsync<string>("node=>node.outerHTML"));
            await page.ScreenshotAsync(new() { Path = stem + ".png" });
        }
        Assert.True(sample.GetProperty("length").GetDouble() > 0, "The keyboard indicator has native SVG stroke geometry.");
        Assert.Equal("2px", sample.GetProperty("width").GetString());
        Assert.NotEqual("none", sample.GetProperty("stroke").GetString());
        Assert.True(sample.GetProperty("ownsFocus").GetBoolean(), "The native indicator surrounds the current keyboard target.");
        var screenshot = await page.ScreenshotAsync();
        var painted = await page.EvaluateAsync<bool>("""
            async ({png,samples,stroke})=>{
                const image=await new Promise((resolve,reject)=>{const image=new Image();image.onload=()=>resolve(image);image.onerror=reject;image.src='data:image/png;base64,'+png;});
                const canvas=document.createElement('canvas');canvas.width=image.naturalWidth;canvas.height=image.naturalHeight;
                const context=canvas.getContext('2d');context.drawImage(image,0,0);
                const swatch=document.createElement('canvas').getContext('2d');swatch.fillStyle=stroke;swatch.fillRect(0,0,1,1);
                const expected=swatch.getImageData(0,0,1,1).data,scale=image.naturalWidth/innerWidth;
                for(const {x,y} of samples)for(let dx=-1;dx<=1;dx++)for(let dy=-1;dy<=1;dy++){
                    const actual=context.getImageData(Math.round(x*scale)+dx,Math.round(y*scale)+dy,1,1).data;
                    // The tooltip's token shadow can cross the ring at the specified 12px gap.
                    // Preserve the accent hue and three-channel error bound while allowing
                    // its observed black-shadow attenuation; a dimmed or absent ring fails.
                    for(let shade=100;shade>=85;shade--)
                        if(actual.every((value,index)=>Math.abs(value-(index<3?expected[index]*shade/100:expected[index]))<=3))return true;
                }
                return false;
            }
            """, new { png = Convert.ToBase64String(screenshot), samples = sample.GetProperty("samples").EnumerateArray()
                .Select(point => new { x = point.GetProperty("x").GetDouble(), y = point.GetProperty("y").GetDouble() }).ToArray(),
                stroke = sample.GetProperty("stroke").GetString() });
        if (!painted && Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } captures) {
            if (!Path.IsPathRooted(captures)) throw new InvalidOperationException("Browser captures require an absolute task-owned directory.");
            Directory.CreateDirectory(captures);
            var name = "focus-stroke-failure-" + (page.ViewportSize?.Width ?? 0);
            await File.WriteAllBytesAsync(Path.Combine(captures, name + ".png"), screenshot);
            await File.WriteAllTextAsync(Path.Combine(captures, name + ".json"), sample.GetRawText());
            await File.WriteAllTextAsync(Path.Combine(captures, name + ".svg"), await page.Locator(".cfx-stage svg").EvaluateAsync<string>("svg=>svg.outerHTML"));
        }
        Assert.True(painted, "The browser screenshot contains the keyboard indicator's visible accent stroke.");
    }

}
