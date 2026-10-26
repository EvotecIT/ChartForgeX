using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class NativeTransitionBrowserTests {
    [BrowserTheory] [InlineData(390, false)] [InlineData(390, true)] [InlineData(760, false)] [InlineData(760, true)]
    [Trait("BrowserSuite", "Compatibility")]
    public async Task RealNativeTreemapEllipsisKeepsCapturedCaptionInkExportAndLifetime(int width, bool dark) {
        if (!Enabled) return; var renderer = new HtmlInteractiveChartRenderer();
        HtmlChartSection Section(double reading) => renderer.PrepareHierarchySection(NativeHierarchyEllipsisMotionTests.Model(reading, width, dark), configure: options => {
            options.Interaction.ChartId = "native-ellipsis"; options.Interaction.Enable(ChartInteractionFeatures.Export);
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive; options.Adaptive.NarrowWidth = 350; options.Adaptive.NarrowHeight = 420;
        });
        var from = Section(30); var to = Section(40); var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.Values, 5));
        await using var session = await BrowserProofFonts.OpenAsync(Page(from), width + 24, 680); var page = session.Page; await ReadyAndPauseAsync(page);
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.NoPreference });
        await page.EvaluateAsync("()=>{window.ellipsis=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'));window.ellipsisEvents=[];ellipsis.root.addEventListener('cfxupdated',event=>ellipsisEvents.push(event.detail));}");
        await page.EvaluateAsync("reply=>ellipsis.update(reply.html,{sceneRevision:ellipsis.sceneRevision,transition:reply.payload,motion:{duration:1000}})", new { html = to.Html, payload }); await page.Clock.RunForAsync(300);
        var observed = await page.EvaluateAsync<JsonElement>("""
            ()=>{const root=ellipsis.root,frame=root.querySelector('.cfx-native-motion-frame'),owner=frame.querySelector('[data-cfx-node=root]'),mark=owner.querySelector(':scope>[data-cfx-role=treemap-group-mark]');
            const labels=[...owner.querySelectorAll('[data-cfx-role=treemap-group-label] text')].filter(text=>text.closest('[data-cfx-node]')===owner);
            const faults=[];for(const text of labels){const box=text.getBBox(),matrix=mark.getCTM().inverse().multiply(text.getCTM());for(const x of [box.x,box.x+box.width])for(const y of [box.y,box.y+box.height])if(!mark.isPointInFill(new DOMPoint(x,y).matrixTransform(matrix)))faults.push('caption crosses native group');}
            return {texts:labels.map(text=>text.textContent),faults,events:ellipsisEvents,accepted:root.querySelector('.cfx-stage>svg [data-cfx-node=leaf-a]')?.dataset.cfxAuthoredValue,full:owner.dataset.cfxFullLabel,frame:frame.outerHTML};}
            """);
        var name = "ellipsis-browser-" + width + "-" + dark; NativeRadialMotionBeforeControls.Save(name + ".observed.json", observed.GetRawText()); NativeRadialValueMotionTests.Capture(from, to, VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 5, null), payload, name);
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, name + ".browser.png"), FullPage = true });
            var narrow = VisualValueCapture.NativeFrames(from.NarrowPreparedVisual!, to.NarrowPreparedVisual!, 5, null);
            for (var index = 0; index < narrow.Count; index++) { File.WriteAllText(Path.Combine(output, name + ".narrow-" + index + ".svg"), narrow[index].ToSvg()); File.WriteAllBytes(Path.Combine(output, name + ".narrow-" + index + ".png"), narrow[index].ToPng()); }
        }
        Assert.Equal(1, observed.GetProperty("events").GetArrayLength()); Assert.Equal("40", observed.GetProperty("accepted").GetString()); Assert.Equal(0, observed.GetProperty("faults").GetArrayLength());
        var caption = Assert.Single(observed.GetProperty("texts").EnumerateArray()).GetString() ?? throw new InvalidOperationException("Native caption observation missing."); Assert.EndsWith("…", caption); Assert.NotEqual("…", caption);
        Assert.StartsWith(caption[..^1], observed.GetProperty("full").GetString()!);
        await NativeValueFrameBrowserProof.AssertAsync(page, "[data-cfx-node=leaf-a] [data-cfx-role=treemap-tile-mark]", name + "-ink");
        var download = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync()); var path = await download.PathAsync() ?? throw new InvalidOperationException("Accepted Ellipsis export missing.");
        var exportedText = await File.ReadAllTextAsync(path); NativeRadialMotionBeforeControls.Save(name + ".exported.svg", exportedText); var exported = System.Xml.Linq.XDocument.Parse(exportedText);
        Assert.Equal("40", exported.Descendants().Single(row => (string?)row.Attribute("data-cfx-node") == "leaf-a").Attribute("data-cfx-authored-value")!.Value);
        await ReduceAndFlushMotionAsync(page); Assert.Equal(0, await page.Locator(".cfx-native-motion-frame").CountAsync());
        await page.EvaluateAsync("()=>ellipsis.dispose()"); await page.Clock.RunForAsync(2000); Assert.Equal(0, await page.Locator(".cfx-native-motion-frame,[data-cfx-update-motion]").CountAsync()); AssertNoConsoleErrors(session);
    }
}
