using Microsoft.Playwright;

namespace ChartForgeX.Tests;

public sealed partial class ExplorerLocalizationBrowserTests {
    // Fixture-scoped observation: identical English WebKit remounts can differ by one
    // raster colour quantum. Compare exact submitted Canvas2D facts, not GPU quantization.
    // Full PNG snapshots remain evidence; this does not assert rendered glyph pixel equality.
    private static Task ObserveNativeCanvasPaintAsync(IPage page) => page.EvaluateAsync("""
        ()=>{if(window.__cfxLocalizationPaint)return;
            const prototype=CanvasRenderingContext2D.prototype,getTransform=prototype.getTransform,
                getLineDash=prototype.getLineDash,paints=new WeakMap();
            const transform=context=>{const m=getTransform.call(context);return [m.a,m.b,m.c,m.d,m.e,m.f]},
                value=item=>item===undefined?null:item===null||['string','number','boolean'].includes(typeof item)?item:
                    Array.isArray(item)?item.map(value):ArrayBuffer.isView(item)?Array.from(item):item instanceof HTMLImageElement?
                    {source:item.currentSrc||item.src,width:item.naturalWidth,height:item.naturalHeight}:
                    (()=>{throw new Error('Unsupported localization Canvas2D observation: '+Object.prototype.toString.call(item))})(),
                paint=(context,operation)=>{
                    const result={transform:transform(context)};
                    if(!['clearRect','fillRect','strokeRect','fill','stroke','fillText','strokeText','drawImage'].includes(operation))return result;
                    if(operation==='clearRect')return result;
                    for(const key of ['globalAlpha','globalCompositeOperation','filter','shadowColor','shadowBlur','shadowOffsetX','shadowOffsetY'])result[key]=value(context[key]);
                    if(operation.startsWith('fill'))result.fillStyle=value(context.fillStyle);
                    if(operation.startsWith('stroke')){
                        for(const key of ['strokeStyle','lineWidth','lineCap','lineJoin','miterLimit','lineDashOffset'])result[key]=value(context[key]);
                        result.lineDash=getLineDash.call(context);
                    }
                    if(operation.endsWith('Text')){
                        for(const key of ['font','fontKerning','fontStretch','fontVariantCaps','letterSpacing','wordSpacing','textAlign','textBaseline','textRendering'])result[key]=value(context[key]);
                        result.direction=context.direction==='inherit'?getComputedStyle(context.canvas).direction:context.direction;
                    }
                    if(operation==='drawImage')for(const key of ['imageSmoothingEnabled','imageSmoothingQuality'])result[key]=value(context[key]);
                    return result;
                };
            for(const operation of ['clearRect','fillRect','strokeRect','fill','stroke','fillText','strokeText','drawImage','putImageData',
                'beginPath','closePath','moveTo','lineTo','bezierCurveTo','quadraticCurveTo','arc','arcTo','ellipse','rect','roundRect','clip','save','restore']){
                const descriptor=Object.getOwnPropertyDescriptor(prototype,operation);if(typeof descriptor?.value!=='function')continue;
                const original=descriptor.value;
                Object.defineProperty(prototype,operation,{...descriptor,value:function(...args){
                    const canvas=this.canvas;
                    if(canvas.closest('.cfx-graph-explorer')||canvas.matches('[data-cfx-role=localization-canvas]')){
                        let commands=paints.get(canvas);
                        if(!commands||operation==='clearRect'){commands=[];paints.set(canvas,commands);}
                        commands.push({operation,args:args.map(value),paint:paint(this,operation)});
                    }
                    return Reflect.apply(original,this,args);
                }});
            }
            window.__cfxLocalizationPaint={get:canvas=>{
                const commands=paints.get(canvas);return commands?{width:canvas.width,height:canvas.height,commands}:null;
            }};
        }
        """);
}
