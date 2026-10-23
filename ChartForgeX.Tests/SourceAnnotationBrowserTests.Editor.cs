using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class SourceAnnotationBrowserTests {
    [BrowserTheory]
    [Trait("BrowserSuite", "Compatibility")]
    [InlineData(390, VisualThemeMode.Dark, false)]
    [InlineData(900, VisualThemeMode.Light, true)]
    public async Task SourceEditorDraftsAreTransientAndOnlyNativeAcceptedEditsEnterUndoHistory(int width, VisualThemeMode mode, bool secondary) {
        if (!Enabled) return;
        var host = new AnnotationHost(width, mode, secondary);
        await using var session = await BrowserProofFonts.OpenAsync(host.Initial, width + 24, 1000);
        var page = session.Page;
        await ConnectAnnotations(page, host);
        await page.EvaluateAsync("""
            () => { window.annotationCommits=[];
                drawings.root.addEventListener('cfxannotationcommit',event=>annotationCommits.push(event.detail));
                window.editor=drawings.editAnnotations({historyLimit:2});
                editor.begin({operation:'replace',id:'note'}); }
            """);
        var anchor = page.Locator("[data-cfx-editor-handle=start]");
        await anchor.FocusAsync(); await page.Keyboard.PressAsync("Shift+ArrowRight");
        var draft = await page.EvaluateAsync<JsonElement>("() => editor.getState().draft.drawing");
        Assert.True(draft.GetProperty("start").GetProperty("x").GetDouble() > 3);
        Assert.Equal("3", await page.Locator("[data-cfx-annotation-id=note]").GetAttributeAsync("data-cfx-source-x"));
        Assert.False(await page.EvaluateAsync<bool>("() => editor.getState().canUndo"));
        var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync());
        var path = await download.PathAsync() ?? throw new InvalidOperationException("The committed native SVG did not export.");
        var exported = XDocument.Load(path);
        Assert.DoesNotContain(exported.Descendants(), node => node.Attribute("data-cfx-annotation-editor") != null);
        Assert.Equal("3", Assert.Single(exported.Descendants(), node => (string?)node.Attribute("data-cfx-annotation-id") == "note").Attribute("data-cfx-source-x")?.Value);
        await Capture(page, width, mode, "editor-draft");
        await page.Locator("[data-cfx-annotation-control=commit]").ClickAsync(); await WaitForAnnotationChanges(page, 1);
        Assert.Equal(draft.GetProperty("start").GetProperty("x").GetDouble(), double.Parse((await page.Locator("[data-cfx-annotation-id=note]").GetAttributeAsync("data-cfx-source-x"))!, System.Globalization.CultureInfo.InvariantCulture));
        Assert.True(await page.EvaluateAsync<bool>("() => editor.getState().canUndo && editor.getState().draft===null"));
        await page.Locator("[data-cfx-annotation-control=undo]").ClickAsync(); await WaitForAnnotationChanges(page, 2);
        Assert.Equal("3", await page.Locator("[data-cfx-annotation-id=note]").GetAttributeAsync("data-cfx-source-x"));
        Assert.True(await page.EvaluateAsync<bool>("() => editor.getState().canRedo && !editor.getState().canUndo"));
        await page.Locator("[data-cfx-annotation-control=redo]").ClickAsync(); await WaitForAnnotationChanges(page, 3);
        Assert.True(await page.EvaluateAsync<bool>("() => annotationCommits.length===3 && annotationCommits.map(c=>c.history).join(',')==='edit,undo,redo'"));
        Assert.Empty(await page.EvaluateAsync<string[]>("() => annotationErrors.map(error=>error.message)"));
        Assert.True(await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth+1"));
        await Capture(page, width, mode, "editor-accepted"); AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    [Trait("BrowserSuite", "Compatibility")]
    public async Task SourceEditorPointerControlsCreateNativeDrawingsAndCancelRetainsTheCommittedScene() {
        if (!Enabled) return;
        var host = new AnnotationHost(900, VisualThemeMode.Light);
        await using var session = await BrowserProofFonts.OpenAsync(host.Initial, 924, 1000);
        var page = session.Page; await ConnectAnnotations(page, host);
        await page.EvaluateAsync("() => {window.nextDrawing=0;window.editor=drawings.editAnnotations({createId:kind=>'new-'+(++nextDrawing)});}");
        await page.Locator("[data-cfx-annotation-control=LineSegment]").ClickAsync();
        var handle = page.Locator("[data-cfx-editor-handle=end]");
        var bounds = await handle.BoundingBoxAsync() ?? throw new InvalidOperationException("The line editor did not expose its endpoint.");
        var before = await page.EvaluateAsync<JsonElement>("() => editor.getState().draft.drawing.end");
        await page.Mouse.MoveAsync(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
        await page.Mouse.DownAsync(); await page.Mouse.MoveAsync(bounds.X + bounds.Width / 2 - 18, bounds.Y + bounds.Height / 2 + 12); await page.Mouse.UpAsync();
        var after = await page.EvaluateAsync<JsonElement>("() => editor.getState().draft.drawing.end");
        Assert.NotEqual(before.GetRawText(), after.GetRawText());
        Assert.Equal(0, await page.Locator("[data-cfx-annotation-id=new-1]").CountAsync());
        await page.Locator("[data-cfx-annotation-control=commit]").ClickAsync(); await WaitForAnnotationChanges(page, 1);
        Assert.Equal("LineSegment", await page.Locator("[data-cfx-annotation-id=new-1]").GetAttributeAsync("data-cfx-kind"));
        await page.Locator("[data-cfx-annotation-control=ParallelChannel]").ClickAsync();
        await page.Locator("[data-cfx-editor-handle=offset]").FocusAsync(); await page.Keyboard.PressAsync("ArrowUp"); await page.Keyboard.PressAsync("Escape");
        Assert.Null((await page.EvaluateAsync<JsonElement>("() => editor.getState()")).GetProperty("draft").Deserialize<object>());
        Assert.Equal(0, await page.Locator("[data-cfx-annotation-id=new-2]").CountAsync());
        Assert.Single(await page.EvaluateAsync<JsonElement[]>("() => annotationChanges"));
        await page.EvaluateAsync("() => editor.disable()");
        Assert.True(await page.Locator(".cfx-annotation-editor").IsHiddenAsync());
        Assert.Equal(0, await page.Locator("[data-cfx-annotation-editor]").CountAsync()); AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    [Trait("BrowserSuite", "Compatibility")]
    public async Task SourceEditorRejectedCanceledAndExternalUpdatesCannotManufactureAcceptedHistory() {
        if (!Enabled) return;
        var host = new AnnotationHost(900, VisualThemeMode.Dark);
        await using var session = await BrowserProofFonts.OpenAsync(host.Initial, 924, 1000);
        var page = session.Page; await ConnectAnnotations(page, host);
        await page.EvaluateAsync("""
            () => {window.pendingEdits=[];
                drawings.connectNative((request,signal)=>new Promise((resolve,reject)=>pendingEdits.push({request,signal,resolve,reject})),{basis:'initial',axes:null});
                window.editor=drawings.editAnnotations({historyLimit:1});
                editor.begin({operation:'replace',id:'note',drawing:{kind:'Note',start:{x:4,y:40},label:'Draft'}}).commit();}
            """);
        await page.WaitForFunctionAsync("() => pendingEdits.length===1");
        Assert.True(await page.EvaluateAsync<bool>("() => editor.getState().pending && !editor.getState().canUndo"));
        await page.EvaluateAsync("() => pendingEdits[0].reject(new Error('Native edit declined'))");
        await page.WaitForFunctionAsync("() => annotationErrors.length===1");
        Assert.True(await page.EvaluateAsync<bool>("() => !editor.getState().pending && !editor.getState().canUndo && editor.getState().draft.drawing.label==='Draft'"));
        await page.EvaluateAsync("() => editor.commit()"); await page.WaitForFunctionAsync("() => pendingEdits.length===2");
        await page.EvaluateAsync("() => editor.cancel()");
        Assert.True(await page.EvaluateAsync<bool>("() => pendingEdits[1].signal.aborted && !editor.getState().canUndo"));
        await page.EvaluateAsync("""
            async()=>{const reply=await annotationHostPrepare(pendingEdits[1].request);
                pendingEdits[1].resolve({html:reply.html,basis:reply.token});}
            """);
        await page.EvaluateAsync("() => new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve)))");
        Assert.Empty(await page.EvaluateAsync<JsonElement[]>("() => annotationChanges"));
        Assert.Equal("Review", await page.Locator("[data-cfx-annotation-id=note]").GetAttributeAsync("data-cfx-label"));
        await page.EvaluateAsync("html=>drawings.update(html,{basis:'external'})", host.Fresh());
        Assert.True(await page.EvaluateAsync<bool>("() => !editor.getState().canUndo && !editor.getState().canRedo && editor.getState().draft===null"));
        await page.EvaluateAsync("() => drawings.dispose()");
        Assert.Equal(0, await page.Locator(".cfx-annotation-editor,[data-cfx-annotation-editor]").CountAsync()); AssertNoConsoleErrors(session);
    }
}
