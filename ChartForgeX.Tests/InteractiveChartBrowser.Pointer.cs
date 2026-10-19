using Microsoft.Playwright;

namespace ChartForgeX.Tests;

internal static partial class InteractiveChartBrowser {
    /// <summary>Acquires the renderer's existing observation surface when it intentionally covers unpainted native interiors.</summary>
    internal static Task<double[]> PreparedHitAreaPointAsync(IPage page, string targetSelector) =>
        page.Locator(targetSelector).First.EvaluateAsync<double[]>("""
            (node,selector)=>{
                const hit=node.querySelector('[data-cfx-browser-hit-area]');
                if(!hit)throw Error('Missing prepared observation surface for '+selector);
                const box=hit.getBoundingClientRect(),x=Math.round(box.left+box.width/2),y=Math.round(box.top+box.height/2);
                if(document.elementFromPoint(x,y)!==hit||hit.closest(selector)!==node)
                    throw Error('Prepared observation surface does not own its integer hit for '+selector);
                return[x,y];
            }
            """, targetSelector);

    /// <summary>Finds an integer native hit with room for real pointer movement, including drivers that truncate coordinates.</summary>
    internal static Task<double[]> NativeInteriorPointAsync(IPage page, string targetSelector, string markSelector, int clearance = 4) =>
        page.Locator(targetSelector).First.EvaluateAsync<double[]>("""
            (node,args)=>{
                const mark=node.querySelector(args.markSelector);
                if(!mark)throw Error('Missing native mark '+args.markSelector);
                const box=mark.getBoundingClientRect();
                const hit=(x,y)=>{const ink=document.elementFromPoint(x,y);
                    return ink?.closest(args.targetSelector)===node
                        &&!ink.closest('[data-cfx-browser-hit-area],[data-cfx-highlight-outline],[data-cfx-state-outline]');};
                for(let y=1;y<40;y++)for(let x=1;x<40;x++){
                    const px=Math.round(box.left+box.width*x/40),py=Math.round(box.top+box.height*y/40);
                    if([-args.clearance,0,args.clearance].every(dx=>[-args.clearance,0,args.clearance].every(dy=>hit(px+dx,py+dy))))return[px,py];
                }
                throw Error('No interior native hit for '+args.targetSelector);
            }
            """, new { targetSelector, markSelector, clearance });
}
