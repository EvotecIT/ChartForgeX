using System.Text.Json;
using Microsoft.Playwright;

namespace ChartForgeX.Tests;

public sealed partial class MermaidNotationReferenceRenderingTests {
    private static async Task<JsonElement> ObserveAsync(IPage page, string source, string key, bool dark, Dictionary<string, int> regions,string? preset=null,string? look=null,bool omitAppearance=false) {
        await BrowserProofCaptions.InstallAsync(page);
        var json = await page.EvaluateAsync<string>("""
            async ({source,key,dark,regions,fontFamily,preset,look,omitAppearance}) => {
                mermaid.initialize({startOnLoad:false,deterministicIds:true,securityLevel:'strict',fontFamily,
                    ...(omitAppearance?{}:{theme:preset??(dark?'dark':'default'),look:look??'classic'}),layout:'dagre',flowchart:{htmlLabels:true}});
                const result=await mermaid.render('notation-reference-diagram',source);
                document.querySelector('#reference').innerHTML=result.svg;
                const reference=document.querySelector('#reference svg');
                const native=document.querySelector('#native svg');
                for(const svg of [reference,native]) {
                    const view=svg.viewBox.baseVal;
                    svg.style.width=view.width+'px'; svg.style.height=view.height+'px'; svg.style.maxWidth='none';
                }
                await document.fonts.ready; await document.querySelector('#png').decode();
                const db=key?(await mermaid.mermaidAPI.getDiagramFromText(source)).db:null;
                const facts=key?window.cfxReferenceFacts[key](db):null;
                const normalize=text=>text.replace(/\\n/g,' ').replace(/\s+/g,' ').trim();
                const visibleText=window.cfxReferenceCaptionText;
                const clip=[];
                for(const [surface,svg] of [['reference',reference],['native',native]]) {
                    const frame=svg.getBoundingClientRect();
                    for(const label of svg.querySelectorAll('text,foreignObject,[data-cfx-caption]')) {
                        const box=label.getBoundingClientRect();
                        if(box.width===0||box.height===0||!window.cfxReferenceLabelText(label)) continue;
                        if(box.left<frame.left-1.5||box.top<frame.top-1.5||box.right>frame.right+1.5||box.bottom>frame.bottom+1.5)
                            clip.push(surface+': '+normalize(label.textContent));
                    }
                }
                const nativeRegions=Object.fromEntries(Object.keys(regions).map(role=>[role,native.querySelectorAll('[data-cfx-role="'+role+'"]').length]));
                return JSON.stringify({facts,referenceText:visibleText(reference),nativeText:visibleText(native),clip,nativeRegions,
                    referencePaintElements:reference.querySelectorAll('path,line,rect,circle,ellipse,polygon').length,
                    hosts:[...document.querySelectorAll('section')].map(host=>({width:host.clientWidth,scrollWidth:host.scrollWidth})),
                    horizontalOverflow:document.documentElement.scrollWidth>innerWidth,
                    nativeScripts:native.querySelectorAll('script,foreignObject').length,
                    referenceLayout:'dagre',referenceLook:omitAppearance?'omitted':look??'classic'});
            }
            """, new { source, key, dark, regions, fontFamily = BrowserProofFonts.Family,preset,look,omitAppearance });
        using var observed = JsonDocument.Parse(json);
        return observed.RootElement.Clone();
    }

    private static async Task CaptureAsync(IPage page, string name, string nativeSvg, byte[] nativePng, string semantics, JsonElement observed) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, name + ".json"), observed.GetRawText());
        File.WriteAllText(Path.Combine(output, name + ".native.json"), semantics);
        File.WriteAllText(Path.Combine(output, name + ".native.svg"), nativeSvg);
        File.WriteAllBytes(Path.Combine(output, name + ".native.png"), nativePng);
        File.WriteAllText(Path.Combine(output, name + ".reference.svg"), await page.Locator("#reference > svg").EvaluateAsync<string>("element=>element.outerHTML"));
        File.WriteAllText(Path.Combine(output, name + ".html"), await page.EvaluateAsync<string>("""
            () => { const copy=document.documentElement.cloneNode(true); for(const script of copy.querySelectorAll('script')) script.remove(); return '<!doctype html>'+copy.outerHTML; }
            """));
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, name + ".browser.png"), FullPage = true });
        await page.EvaluateAsync("() => { for(const host of document.querySelectorAll('section')) host.scrollLeft=host.scrollWidth; }");
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, name + ".scrolled.browser.png"), FullPage = true });
    }
}
