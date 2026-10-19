using Microsoft.Playwright;

namespace ChartForgeX.Tests;

internal static partial class InteractiveChartBrowser {
    /// <summary>Finds an integer native hit with room for real pointer movement, including drivers that truncate coordinates.</summary>
    internal static Task<double[]> NativeInteriorPointAsync(IPage page, string targetSelector, string markSelector, int clearance = 4) =>
        page.Locator(targetSelector).First.EvaluateAsync<double[]>("""
            (node,args)=>{
                const mark=node.querySelector(args.markSelector),box=mark.getBoundingClientRect();
                const hit=(x,y)=>{const ink=document.elementFromPoint(x,y);
                    return ink?.closest('[data-cfx-target-kind=node]')===node
                        &&!ink.closest('[data-cfx-highlight-outline],[data-cfx-state-outline]');};
                for(let y=1;y<40;y++)for(let x=1;x<40;x++){
                    const px=Math.round(box.left+box.width*x/40),py=Math.round(box.top+box.height*y/40);
                    if([-args.clearance,0,args.clearance].every(dx=>[-args.clearance,0,args.clearance].every(dy=>hit(px+dx,py+dy))))return[px,py];
                }
                throw Error('No interior native hit for '+node.dataset.cfxNode);
            }
            """, new { markSelector, clearance });
}
