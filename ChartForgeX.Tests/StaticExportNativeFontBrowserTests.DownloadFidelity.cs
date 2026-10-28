using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using ChartForgeX.Raster;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class StaticExportNativeFontBrowserTests {
    /// <summary>Separates exact downloaded bitmap fidelity from independent SVG semantic replay.</summary>
    private static async Task<Exported> CaptureExports(IPage page, string svgButton, string pngButton, string name, bool? corruptOutput = null) {
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(output) || !Path.IsPathRooted(output)) throw new InvalidOperationException("Font proof requires an absolute capture path.");
        Directory.CreateDirectory(output); var stem = Path.Combine(output, name);
        await page.EvaluateAsync("""
            corrupt => {
                window.nativeFontPngInputs=[];window.nativeFontRasterDraws=[];window.nativeFontRasterReferences=[];
                window.nativeFontObjectUrlOwner=URL.createObjectURL;
                URL.createObjectURL=function(blob){if(blob.type.startsWith('image/svg+xml'))nativeFontPngInputs.push(blob.text());return nativeFontObjectUrlOwner.call(URL,blob);};
                window.nativeFontImageSrcOwner=Object.getOwnPropertyDescriptor(HTMLImageElement.prototype,'src');
                Object.defineProperty(HTMLImageElement.prototype,'src',{...nativeFontImageSrcOwner,set(value){
                    const prefix='data:image/svg+xml;charset=utf-8,';
                    if(String(value).startsWith(prefix))nativeFontPngInputs.push(Promise.resolve(decodeURIComponent(String(value).slice(prefix.length))));
                    return nativeFontImageSrcOwner.set.call(this,value);
                }});
                window.nativeFontDrawImageOwner=CanvasRenderingContext2D.prototype.drawImage;
                CanvasRenderingContext2D.prototype.drawImage=function(image,...args){
                    const result=nativeFontDrawImageOwner.call(this,image,...args),prefix='data:image/svg+xml;charset=utf-8,';
                    if(image instanceof HTMLImageElement&&image.src.startsWith(prefix)){
                        const matrix=this.getTransform();
                        nativeFontRasterDraws.push({canvas:this.canvas,source:decodeURIComponent(image.src.slice(prefix.length)),args,
                            width:this.canvas.width,height:this.canvas.height,transform:[matrix.a,matrix.b,matrix.c,matrix.d,matrix.e,matrix.f],
                            alpha:this.globalAlpha,composite:this.globalCompositeOperation,filter:this.filter||'none'});
                    }
                    return result;
                };
                window.nativeFontCanvasEncoderOwner=HTMLCanvasElement.prototype.toBlob;
                HTMLCanvasElement.prototype.toBlob=function(callback,type,...args){
                    if(type==='image/png'&&nativeFontRasterDraws.some(row=>row.canvas===this)){
                        // toBlob snapshots this bitmap before either the download encoder or
                        // the deliberate negative control can mutate it. No second SVG decode.
                        nativeFontRasterReferences.push(new Promise((resolve,reject)=>nativeFontCanvasEncoderOwner.call(this,blob=>{
                            if(!blob){reject(new Error('Missing pristine bitmap reference.'));return;}
                            const reader=new FileReader();reader.onerror=()=>reject(reader.error);
                            reader.onload=()=>resolve(String(reader.result).split(',')[1]);reader.readAsDataURL(blob);
                        },'image/png')));
                        if(corrupt===true)this.width+=1;
                        else if(corrupt===false){const context=this.getContext('2d');context.fillStyle='#ff00ff';context.fillRect(0,0,1,1);}
                    }
                    return nativeFontCanvasEncoderOwner.call(this,callback,type,...args);
                };
            }
            """, corruptOutput);
        string svg; byte[] png; JsonElement capture;
        try {
            var vector = await page.RunAndWaitForDownloadAsync(() => page.Locator(svgButton).ClickAsync());
            svg = await File.ReadAllTextAsync(await vector.PathAsync() ?? throw new InvalidOperationException("Missing SVG download."));
            await vector.SaveAsAsync(stem + ".svg");
            await page.EvaluateAsync("() => { nativeFontPngInputs.length=0;nativeFontRasterDraws.length=0;nativeFontRasterReferences.length=0; }");
            var raster = await page.RunAndWaitForDownloadAsync(() => page.Locator(pngButton).ClickAsync());
            png = await File.ReadAllBytesAsync(await raster.PathAsync() ?? throw new InvalidOperationException("Missing PNG download."));
            await raster.SaveAsAsync(stem + ".png");
            capture = await page.EvaluateAsync<JsonElement>("""
                async()=>{
                    const inputs=await Promise.all(nativeFontPngInputs),references=await Promise.all(nativeFontRasterReferences);
                    if(inputs.length!==1||references.length!==1||nativeFontRasterDraws.length!==1)throw new Error('Expected one completed SVG draw and pristine PNG reference.');
                    const {canvas,...draw}=nativeFontRasterDraws[0];return {composition:inputs[0],reference:references[0],draw};
                }
                """);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".browser.png", FullPage = true });
        } finally {
            await page.EvaluateAsync("""
                ()=>{
                    URL.createObjectURL=nativeFontObjectUrlOwner;Object.defineProperty(HTMLImageElement.prototype,'src',nativeFontImageSrcOwner);
                    CanvasRenderingContext2D.prototype.drawImage=nativeFontDrawImageOwner;HTMLCanvasElement.prototype.toBlob=nativeFontCanvasEncoderOwner;
                    delete window.nativeFontObjectUrlOwner;delete window.nativeFontImageSrcOwner;delete window.nativeFontDrawImageOwner;delete window.nativeFontCanvasEncoderOwner;
                }
                """);
        }
        var composition = capture.GetProperty("composition").GetString()!;
        var referencePng = Convert.FromBase64String(capture.GetProperty("reference").GetString()!);
        await File.WriteAllTextAsync(stem + ".png-composition.svg", composition);
        await File.WriteAllBytesAsync(stem + ".reference.png", referencePng);
        await File.WriteAllTextAsync(stem + ".draw.json", capture.GetProperty("draw").GetRawText());
        var raw = await page.EvaluateAsync<string>(ComparePixels, new { svg = composition, editableSvg = svg, png = Convert.ToBase64String(png) });
        var report = JsonNode.Parse(raw)!.AsObject();
        var width = report["width"]!.GetValue<int>(); var height = report["height"]!.GetValue<int>(); var scale = report["scale"]!.GetValue<int>();
        var expectedWidth = checked(width * scale); var expectedHeight = checked(height * scale);
        var draw = capture.GetProperty("draw");
        Assert.Equal(composition, draw.GetProperty("source").GetString());
        Assert.Equal(new[] { 0d, 0d, (double)width, height }, draw.GetProperty("args").EnumerateArray().Select(value => value.GetDouble()));
        Assert.Equal(new[] { (double)scale, 0d, 0d, scale, 0d, 0d }, draw.GetProperty("transform").EnumerateArray().Select(value => value.GetDouble()));
        Assert.Equal(1d, draw.GetProperty("alpha").GetDouble()); Assert.Equal("source-over", draw.GetProperty("composite").GetString()); Assert.Equal("none", draw.GetProperty("filter").GetString());
        Assert.Equal(expectedWidth, draw.GetProperty("width").GetInt32()); Assert.Equal(expectedHeight, draw.GetProperty("height").GetInt32());
        var downloaded = PngReader.Decode(png); var reference = PngReader.Decode(referencePng);
        await File.WriteAllTextAsync(stem + ".frames.json", JsonSerializer.Serialize(new { expectedWidth, expectedHeight,
            actualWidth = downloaded.Width, actualHeight = downloaded.Height, referenceWidth = reference.Width, referenceHeight = reference.Height }));
        Assert.Equal(expectedWidth, downloaded.Width); Assert.Equal(expectedHeight, downloaded.Height);
        Assert.Equal(expectedWidth, reference.Width); Assert.Equal(expectedHeight, reference.Height);
        var differences = new List<object>(); var changed = 0;
        for (var index = 0; index < downloaded.Pixels.Length; index += 4) {
            if (downloaded.Pixels.AsSpan(index, 4).SequenceEqual(reference.Pixels.AsSpan(index, 4))) continue;
            changed++;
            if (differences.Count < 32) differences.Add(new { x = index / 4 % expectedWidth, y = index / 4 / expectedWidth,
                actual = downloaded.Pixels.AsSpan(index, 4).ToArray().Select(value => (int)value), expected = reference.Pixels.AsSpan(index, 4).ToArray().Select(value => (int)value) });
        }
        report["pngVersusCapturedRasterDifferentPixels"] = changed;
        report["downloadPixelDifferences"] = JsonSerializer.SerializeToNode(differences);
        if (changed != 0) {
            await File.WriteAllBytesAsync(stem + ".comparison-actual.png", png);
            await File.WriteAllBytesAsync(stem + ".comparison-expected.png", referencePng);
        }
        await File.WriteAllTextAsync(stem + ".json", report.ToJsonString());
        using var final = JsonDocument.Parse(report.ToJsonString());
        return new Exported(svg, Convert.ToBase64String(png), composition, final.RootElement.Clone());
    }

    private const string ComparePixels = """
        async ({svg,editableSvg,png})=>{
            const documentSvg=new DOMParser().parseFromString(svg,'image/svg+xml'),root=documentSvg.documentElement;
            const width=Math.round(Number(root.getAttribute('width'))),height=Math.round(Number(root.getAttribute('height'))),scale=Math.max(1,Math.ceil(devicePixelRatio||1));
            // Both sides cross the PNG codec: Firefox can quantize translucent edge colors
            // during encoding, even when the source canvas and downloaded PNG are correct.
            const load=(source,encodePng=false)=>new Promise((resolve,reject)=>{const image=new Image();
                image.onload=()=>{try{
                    const canvas=document.createElement('canvas');canvas.width=width*scale;canvas.height=height*scale;
                    const context=canvas.getContext('2d');context.setTransform(scale,0,0,scale,0,0);context.drawImage(image,0,0,width,height);
                    if(!encodePng){resolve(context.getImageData(0,0,canvas.width,canvas.height).data);return;}
                    canvas.toBlob(blob=>{
                        if(!blob){reject(new Error('Expected PNG comparison encoding.'));return;}
                        const reader=new FileReader();reader.onerror=()=>reject(reader.error);
                        reader.onload=()=>load(String(reader.result)).then(resolve,reject);reader.readAsDataURL(blob);
                    },'image/png');
                }catch(error){reject(error);}};
                image.onerror=()=>reject(new Error('Font export image decode failed.'));
                image.src=source.startsWith('data:')?source:'data:image/svg+xml;charset=utf-8,'+encodeURIComponent(source);});
            const different=(a,b)=>{let count=0;for(let i=0;i<a.length;i+=4)if(a[i]!==b[i]||a[i+1]!==b[i+1]||a[i+2]!==b[i+2]||a[i+3]!==b[i+3])count++;return count;};
            const actual=await load('data:image/png;base64,'+png),same=await load(svg,true),editable=await load(editableSvg,true);
            const undecorated=source=>{const d=new DOMParser().parseFromString(source,'image/svg+xml');
                for(const n of [d.documentElement,...d.querySelectorAll('*')])n.style.setProperty('text-decoration','none','important');
                return new XMLSerializer().serializeToString(d);};
            const editableNone=await load(undecorated(editableSvg),true),pngNone=await load(undecorated(svg),true);
            const decorationInk=pixels=>{let count=0;for(let i=0;i<pixels.length;i+=4)if(pixels[i]===17&&pixels[i+1]===203&&pixels[i+2]===177&&pixels[i+3]===255)count++;return count;};
            for(const node of root.querySelectorAll('*'))node.style.setProperty('font-family','monospace','important');
            const fallback=await load(new XMLSerializer().serializeToString(documentSvg),true);let ink=0,stroke=0;
            for(let i=0;i<actual.length;i+=4){if(actual[i]===18&&actual[i+1]===52&&actual[i+2]===86&&actual[i+3]===255)ink++;
                if(actual[i]===211&&actual[i+1]===17&&actual[i+2]===19&&actual[i+3]===255)stroke++;}
            const nonTranslationGlyphs=Array.from(root.querySelectorAll('use')).filter(node=>{const matrix=node.transform.baseVal.consolidate().matrix;return matrix.a!==1||matrix.b!==0||matrix.c!==0||matrix.d!==1;}).length;
            return JSON.stringify({width,height,scale,resourcePaths:root.querySelectorAll('defs path[id]').length,uses:root.querySelectorAll('use').length,
                decorationInkPixels:decorationInk(actual),editableSvgDecorationInkPixels:decorationInk(editable),
                editableSvgDecorationDifferentPixels:different(editable,editableNone),compositionDecorationDifferentPixels:different(same,pngNone),
                independentSvgReplayDifferentPixels:different(actual,same),compositionVersusForcedMonospaceDifferentPixels:different(same,fallback),authoredTextInkPixels:ink,authoredStrokeInkPixels:stroke,nonTranslationGlyphs});
        }
        """;
}
