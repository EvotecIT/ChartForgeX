using System.Text.Json;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class StandaloneAdapterLifecycleBrowserTests {
    private sealed record GraphEnvironmentFrame(byte[] Png, JsonElement Witness);

    private static async Task<GraphEnvironmentFrame> CaptureGraphEnvironmentFrameAsync(IPage page, string id, string name) {
        await page.Locator(".cfx-graph-stage").ScrollIntoViewIfNeededAsync();
        await page.EvaluateAsync("()=>new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve)))");
        var witness = await GraphEnvironmentWitnessAsync(page, id);
        Assert.True(witness.GetProperty("insideViewport").GetBoolean(), witness.GetRawText());
        var png = await page.ScreenshotAsync(new() { Animations = ScreenshotAnimations.Disabled });
        await WriteReceiptAsync(name, witness);
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Assert.True(Path.IsPathRooted(output), "Graph environment captures need an absolute task-owned folder.");
            Directory.CreateDirectory(output);
            await File.WriteAllBytesAsync(Path.Combine(output, name + ".png"), png);
        }
        return new GraphEnvironmentFrame(png, witness);
    }

    private static object GraphEnvironmentPixelSource(GraphEnvironmentFrame frame) => new {
        png = Convert.ToBase64String(frame.Png),
        x = frame.Witness.GetProperty("center").GetProperty("x").GetDouble(),
        y = frame.Witness.GetProperty("center").GetProperty("y").GetDouble(),
        radius = frame.Witness.GetProperty("radius").GetDouble(),
        width = frame.Witness.GetProperty("viewport").GetProperty("width").GetDouble(),
        height = frame.Witness.GetProperty("viewport").GetProperty("height").GetDouble()
    };

    // Compare real screen pixels: CSS variables and media flags cannot prove Canvas/WebGL selection ink.
    private static async Task AssertGraphEnvironmentMarkPaintAsync(IPage page, GraphEnvironmentFrame before, GraphEnvironmentFrame after, string name) {
        var facts = await page.EvaluateAsync<JsonElement>("""
            async({before,after})=>{
                const decode=async source=>{
                    const image=new Image();image.src='data:image/png;base64,'+source.png;await image.decode();
                    const canvas=document.createElement('canvas');canvas.width=image.naturalWidth;canvas.height=image.naturalHeight;
                    const context=canvas.getContext('2d');context.drawImage(image,0,0);
                    const data=context.getImageData(0,0,canvas.width,canvas.height).data,sx=canvas.width/source.width,sy=canvas.height/source.height;
                    return {pixel:(dx,dy)=>{
                        const x=Math.max(0,Math.min(canvas.width-1,Math.round((source.x+dx*source.radius)*sx))),
                            y=Math.max(0,Math.min(canvas.height-1,Math.round((source.y+dy*source.radius)*sy))),index=(y*canvas.width+x)*4;
                        return Array.from(data.slice(index,index+4));
                    }};
                };
                const first=await decode(before),last=await decode(after),background=last.pixel(-1.8,-1.6);
                const luminance=rgba=>rgba.slice(0,3).map(channel=>{const s=channel/255;return s<=.04045?s/12.92:Math.pow((s+.055)/1.055,2.4);})
                    .reduce((sum,value,index)=>sum+value*[.2126,.7152,.0722][index],0);
                const contrast=rgba=>{const a=luminance(rgba),b=luminance(background);return (Math.max(a,b)+.05)/(Math.min(a,b)+.05);};
                let changedPixels=0,contrastingPixels=0;
                // The upper semicircle avoids the fixture's horizontal connector and the caption below its mark.
                for(let radius=.8;radius<=1.3;radius+=.025)for(let angle=-Math.PI;angle<=0;angle+=Math.PI/80){
                    const x=Math.cos(angle)*radius,y=Math.sin(angle)*radius,a=first.pixel(x,y),b=last.pixel(x,y);
                    if(Math.max(...b.slice(0,3).map((value,index)=>Math.abs(value-a[index])))<24)continue;
                    changedPixels++;if(contrast(b)>=3)contrastingPixels++;
                }
                return {changedPixels,contrastingPixels,background,beforeRadius:before.radius,afterRadius:after.radius};
            }
            """, new { before = GraphEnvironmentPixelSource(before), after = GraphEnvironmentPixelSource(after) });
        await WriteReceiptAsync(name, facts);
        Assert.True(facts.GetProperty("changedPixels").GetInt32() > 0, "Selection must change the actually painted mark rim: " + facts.GetRawText());
        Assert.True(facts.GetProperty("contrastingPixels").GetInt32() > 0, "Selection ink must visibly contrast with the screenshot's adjacent surface: " + facts.GetRawText());
    }

    private static async Task AssertGraphEnvironmentFocusPaintAsync(IPage page, ILocator target, string name) {
        await target.ScrollIntoViewIfNeededAsync();
        var facts = await target.EvaluateAsync<JsonElement>("""
            node=>{
                const trace=window.graphEnvironmentFocusTrace;trace?.record('after-scroll');trace?.stop();
                delete window.graphEnvironmentFocusTrace;
                const style=getComputedStyle(node),box=node.getBoundingClientRect(),scale=box.width/node.offsetWidth;
                const backgrounds=[];for(let owner=node.parentElement;owner;owner=owner.parentElement)backgrounds.unshift(getComputedStyle(owner).backgroundColor);
                const pad=(parseFloat(style.outlineWidth)+parseFloat(style.outlineOffset))*scale,clips=[];
                for(let owner=node.parentElement;owner;owner=owner.parentElement){
                    const clip=getComputedStyle(owner),rect=owner.getBoundingClientRect(),outside=/auto|scroll|hidden|clip/;
                    clips.push({owner:owner.className,x:outside.test(clip.overflowX)?Math.max(0,rect.left-box.left+pad,box.right+pad-rect.right):0,
                        y:outside.test(clip.overflowY)?Math.max(0,rect.top-box.top+pad,box.bottom+pad-rect.bottom):0});
                }
                const describe=value=>({tag:value?.localName,id:value?.id,role:value?.getAttribute?.('data-cfx-role'),name:value?.getAttribute?.('aria-label')});
                return {ownsFocus:node===document.activeElement&&node.matches(':focus-visible'),
                    targetActive:node===document.activeElement,focusVisible:node.matches(':focus-visible'),documentFocused:document.hasFocus(),
                    targetConnected:node.isConnected,target:describe(node),activeElement:describe(document.activeElement),
                    traceTargetSame:trace?trace.node===node:null,traceTargetConnected:trace?trace.node.isConnected:null,
                    observedAt:performance.now(),chronology:trace?.events||[],box:box.toJSON(),
                    width:parseFloat(style.outlineWidth),offset:parseFloat(style.outlineOffset),scale,colour:style.outlineColor,
                    backgrounds,clips,transitionSeconds:Math.max(...style.transitionDuration.split(',').map(parseFloat))};
            }
            """);
        await WriteReceiptAsync(name + "-before-assert", JsonSerializer.SerializeToElement(new { focus = facts }));
        var screenshot = await page.ScreenshotAsync(new() { Animations = ScreenshotAnimations.Disabled });
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Assert.True(Path.IsPathRooted(output), "Graph environment captures need an absolute task-owned folder.");
            Directory.CreateDirectory(output);
            await File.WriteAllBytesAsync(Path.Combine(output, name + ".png"), screenshot);
        }
        Assert.True(facts.GetProperty("ownsFocus").GetBoolean(), "Focus facts: " + name + "-before-assert.json " + facts.GetRawText());
        Assert.True(facts.GetProperty("width").GetDouble() > 0);
        Assert.InRange(facts.GetProperty("transitionSeconds").GetDouble(), 0, .001);
        foreach (var clip in facts.GetProperty("clips").EnumerateArray()) {
            Assert.InRange(clip.GetProperty("x").GetDouble(), 0, 1);
            Assert.InRange(clip.GetProperty("y").GetDouble(), 0, 1);
        }
        var painted = await page.EvaluateAsync<bool>("""
            async({png,facts})=>{
                facts=JSON.parse(facts);
                const image=new Image();image.src='data:image/png;base64,'+png;await image.decode();
                const canvas=document.createElement('canvas');canvas.width=image.naturalWidth;canvas.height=image.naturalHeight;
                const context=canvas.getContext('2d');context.drawImage(image,0,0);
                const swatch=document.createElement('canvas').getContext('2d');swatch.fillStyle='#fff';swatch.fillRect(0,0,1,1);
                for(const colour of [...facts.backgrounds,facts.colour]){swatch.fillStyle=colour;swatch.fillRect(0,0,1,1);}
                const expected=swatch.getImageData(0,0,1,1).data,scale=image.naturalWidth/innerWidth,
                    box=facts.box,pad=(facts.offset+facts.width/2)*facts.scale;
                const points=[[box.left+box.width/2,box.top-pad],[box.left+box.width/2,box.bottom+pad],
                    [box.left-pad,box.top+box.height/2],[box.right+pad,box.top+box.height/2]];
                return points.every(point=>{
                    for(let dx=-3;dx<=3;dx++)for(let dy=-3;dy<=3;dy++){
                        const x=Math.round(point[0]*scale)+dx,y=Math.round(point[1]*scale)+dy;
                        if(x<0||x>=canvas.width||y<0||y>=canvas.height)continue;
                        const pixel=context.getImageData(x,y,1,1).data;
                        if(pixel.every((value,index)=>Math.abs(value-expected[index])<=12))return true;
                    }
                    return false;
                });
            }
            """, new { png = Convert.ToBase64String(screenshot), facts = facts.GetRawText() });
        await WriteReceiptAsync(name, JsonSerializer.SerializeToElement(new { focus = facts, painted }));
        Assert.True(painted, "The actual screenshot must contain the keyboard focus outline under host scaling.");
    }
}
