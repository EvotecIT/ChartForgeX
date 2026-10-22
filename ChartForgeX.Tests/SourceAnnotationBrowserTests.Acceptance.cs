using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class SourceAnnotationBrowserTests {
    [BrowserTheory]
    [Trait("BrowserSuite", "Compatibility")]
    [InlineData(390, VisualThemeMode.Dark)]
    [InlineData(900, VisualThemeMode.Light)]
    public async Task NativeReferenceChangesCannotInventSourceDrawingUndoHistory(int width, VisualThemeMode mode) {
        if (!Enabled) return;
        var host = new AnnotationHost(width, mode, references: true);
        await using var session = await BrowserProofFonts.OpenAsync(host.Initial, width + 24, 1000);
        var page = session.Page; await ConnectAnnotations(page, host);
        await page.EvaluateAsync("""
            ()=>{window.editor=drawings.editAnnotations();window.annotationCommits=[];
                drawings.root.addEventListener('cfxannotationcommit',event=>annotationCommits.push(event.detail));
                editor.begin({operation:'replace',id:'note',drawing:{kind:'Note',start:{x:4,y:40},label:'Edited'}}).commit();}
            """);
        await WaitForAnnotationChanges(page, 1);
        await page.EvaluateAsync("()=>editor.undo()"); await WaitForAnnotationChanges(page, 2);
        Assert.True(await page.EvaluateAsync<bool>("()=>editor.getState().canRedo&&!editor.getState().canUndo"));
        await page.EvaluateAsync("""
            ()=>drawings.applyAnnotationChange({operation:'replace',id:'reference-line',
                drawing:{kind:'Note',start:{x:5,y:60},label:'Replacement'}})
            """);
        await WaitForAnnotationChanges(page, 3);
        Assert.Equal("Note", await page.Locator("[data-cfx-annotation-id=reference-line]").GetAttributeAsync("data-cfx-kind"));
        Assert.True(await page.EvaluateAsync<bool>("()=>!editor.getState().canUndo&&!editor.getState().canRedo&&annotationCommits.length===2"));
        await page.EvaluateAsync("""
            ()=>drawings.applyAnnotationChange({operation:'add',id:'supported',drawing:{kind:'Note',start:{x:7,y:70},label:'Supported'}})
            """);
        await WaitForAnnotationChanges(page, 4);
        Assert.True(await page.EvaluateAsync<bool>("()=>editor.getState().canUndo&&annotationCommits.at(-1).before===null"));
        await page.EvaluateAsync("()=>drawings.applyAnnotationChange({operation:'remove',id:'reference-band'})");
        await WaitForAnnotationChanges(page, 5);
        Assert.Equal(0, await page.Locator("[data-cfx-annotation-id=reference-band]").CountAsync());
        Assert.True(await page.EvaluateAsync<bool>("()=>!editor.getState().canUndo&&!editor.getState().canRedo&&annotationCommits.length===3"));
        await Capture(page, width, mode, "reference-history");
        Assert.Empty(await page.EvaluateAsync<JsonElement[]>("()=>annotationErrors")); AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [Trait("BrowserSuite", "Compatibility")]
    [InlineData(390, VisualThemeMode.Dark)]
    [InlineData(900, VisualThemeMode.Light)]
    public async Task AdaptiveAnnotationReplyRejectsStaleAlternateBeforeHistoryBasisOrExportChanges(int width, VisualThemeMode mode) {
        if (!Enabled) return;
        var host = new AnnotationHost(width, mode);
        await using var session = await BrowserProofFonts.OpenAsync(host.Initial, width + 24, 1000);
        var page = session.Page; await ConnectAnnotations(page, host);
        await page.EvaluateAsync("""
            ()=>{window.pendingEdits=[];
                drawings.connectNative((request,signal)=>new Promise(resolve=>pendingEdits.push({request,signal,resolve})),{basis:'initial',axes:null});
                window.editor=drawings.editAnnotations();window.beforeAnnotationRevision=drawings.sceneRevision;
                window.beforeAnnotationSvg=drawings.root.querySelector('.cfx-stage svg').outerHTML;
                window.annotationChange={operation:'replace',id:'note',drawing:{kind:'Note',start:{x:6,y:45},label:'Accepted'}};
                drawings.applyAnnotationChange(annotationChange);}
            """);
        await page.WaitForFunctionAsync("()=>pendingEdits.length===1");
        await page.EvaluateAsync("""
            async()=>{const reply=await annotationHostPrepare(pendingEdits[0].request),template=document.createElement('template');
                template.innerHTML=reply.html;const section=template.content.firstElementChild;
                section.querySelector('template[data-cfx-adaptive-narrow]').content.querySelector('[data-cfx-annotation-id=note]').dataset.cfxLabel='Stale';
                pendingEdits[0].resolve({html:section.outerHTML,basis:reply.token});}
            """);
        await page.WaitForFunctionAsync("()=>annotationErrors.length===1");
        Assert.True(await page.EvaluateAsync<bool>("""
            ()=>annotationChanges.length===0&&drawings.sceneRevision===beforeAnnotationRevision
                &&drawings.root.querySelector('.cfx-stage svg').outerHTML===beforeAnnotationSvg
                &&!editor.getState().canUndo&&!editor.getState().canRedo
            """));
        await page.EvaluateAsync("()=>drawings.applyAnnotationChange(annotationChange)");
        await page.WaitForFunctionAsync("()=>pendingEdits.length===2");
        Assert.Equal("initial", await page.EvaluateAsync<string>("()=>pendingEdits[1].request.basis"));
        await page.EvaluateAsync("async()=>{const reply=await annotationHostPrepare(pendingEdits[1].request);pendingEdits[1].resolve({html:reply.html,basis:reply.token});}");
        await WaitForAnnotationChanges(page, 1);
        Assert.Equal("Accepted", await page.Locator("[data-cfx-annotation-id=note]").GetAttributeAsync("data-cfx-label"));
        Assert.True(await page.EvaluateAsync<bool>("()=>editor.getState().canUndo"));
        var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync());
        var path = await download.PathAsync() ?? throw new InvalidOperationException("No accepted annotation SVG export.");
        Assert.Equal("Accepted", (string?)Assert.Single(XDocument.Load(path).Descendants(), node => (string?)node.Attribute("data-cfx-annotation-id") == "note").Attribute("data-cfx-label"));
        await Capture(page, width, mode, "adaptive-acceptance"); AssertNoConsoleErrors(session);
    }
}
