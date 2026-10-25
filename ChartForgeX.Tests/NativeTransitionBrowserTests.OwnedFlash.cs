using System.Text.Json;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;
public sealed partial class NativeTransitionBrowserTests {
    [BrowserTheory] [InlineData("line",390,false)] [InlineData("bar-gradient",760,true)] [InlineData("raw-donut",390,true)] [InlineData("treemap",760,false)]
    public async Task ActualOwnedFlashPaintKeepsAcceptedValuesAndUsesTheExistingExportReducedDisposalLifetime(string kind,int width,bool dark) {
        if(!Enabled)return;var renderer=new HtmlInteractiveChartRenderer();var raw=kind.StartsWith("raw-",StringComparison.Ordinal);var peak=ChartColor.FromHex("#FFC000");
        HtmlChartSection Section(double value)=>renderer.PrepareSection(NativeOwnedValueFlashBeforeControls.Model(kind,value,width,dark),configure:options=>{options.CaptureNativeValueSource=raw;options.Interaction.ChartId="owned-flash-browser";options.Interaction.Enable(ChartInteractionFeatures.Export);});
        var from=Section(raw?60:30);var to=Section(raw?5:40);var frames=VisualValueCapture.NativeFrames(from.PreparedVisual,to.PreparedVisual,5,peak);var payload=renderer.PrepareNativeTransitionJson(from,to,new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.ValueFlash,5,peak));var name="owned-flash-browser-"+kind+"-"+width;
        await using var session=await BrowserProofFonts.OpenAsync(Page(from),width+24,680);var page=session.Page;await ReadyAndPauseAsync(page);await page.EmulateMediaAsync(new PageEmulateMediaOptions{ReducedMotion=ReducedMotion.NoPreference});
        await page.EvaluateAsync("()=>{window.ownedFlash=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'));window.flashEvents=[];ownedFlash.root.addEventListener('cfxupdated',event=>flashEvents.push(event.detail));}");
        await page.EvaluateAsync("reply=>ownedFlash.update(reply.html,{sceneRevision:ownedFlash.sceneRevision,transition:reply.payload,motion:{duration:1000}})",new{html=to.Html,payload});await page.Clock.RunForAsync(500);
        var observed=await page.EvaluateAsync<JsonElement>("()=>({metadata:JSON.parse(ownedFlash.root.dataset.cfxPreparedChart),frame:ownedFlash.root.querySelector('.cfx-native-motion-frame')?.outerHTML,events:flashEvents,revision:ownedFlash.sceneRevision})");NativeRadialValueMotionTests.Capture(from,to,frames,payload,name);NativeRadialMotionBeforeControls.Save(name+".observed.json",observed.GetRawText());
        if(Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is {Length:>0} output)await page.ScreenshotAsync(new PageScreenshotOptions{Path=Path.Combine(output,name+".browser.png"),FullPage=true});
        Assert.Equal(1,observed.GetProperty("events").GetArrayLength());Assert.Equal(1,await page.Locator(".cfx-native-motion-frame").CountAsync());
        var selector=kind=="line"?"[data-cfx-role=point][data-cfx-series-key=delivery][data-cfx-observation-id=current] [data-cfx-role=marker]":kind=="bar-gradient"?"[data-cfx-role=point][data-cfx-series-key=delivery][data-cfx-observation-id=current] [data-cfx-role=bar]":kind=="treemap"?"[data-cfx-node=leaf-a] [data-cfx-role=treemap-tile-mark]":"[data-cfx-role=radial-point][data-cfx-point='-1'] [data-cfx-role=donut-slice]";
        await NativeValueFrameBrowserProof.AssertAsync(page,selector,name+"-ink");
        var download=await page.RunAndWaitForDownloadAsync(()=>page.Locator("[data-cfx-export=svg]").ClickAsync());var file=await download.PathAsync()??throw new InvalidOperationException("Owned flash final export missing.");var exported=await File.ReadAllTextAsync(file);NativeRadialMotionBeforeControls.Save(name+".exported.svg",exported);
        Assert.DoesNotContain("native-motion-frame",exported,StringComparison.Ordinal);Assert.Equal(0,await page.Locator(".cfx-native-motion-frame").CountAsync());
        var actual=System.Xml.Linq.XDocument.Parse(exported);var expected=System.Xml.Linq.XDocument.Parse(to.WideEmission!.Svg);var roles=new[]{"line","bar","donut-slice","treemap-tile-mark"};
        var a=actual.Descendants().Where(row=>(string?)row.Attribute("data-cfx-role") is { } role&&roles.Contains(role)).ToArray();var b=expected.Descendants().Where(row=>(string?)row.Attribute("data-cfx-role") is { } role&&roles.Contains(role)).ToArray();Assert.Equal(b.Length,a.Length);Assert.NotEmpty(a);
        for(var index=0;index<a.Length;index++)Assert.Equal(b[index].Attributes().Select(row=>(row.Name,row.Value)).OrderBy(row=>row.Name.ToString()),a[index].Attributes().Select(row=>(row.Name,row.Value)).OrderBy(row=>row.Name.ToString()));
        await page.EmulateMediaAsync(new PageEmulateMediaOptions{ReducedMotion=ReducedMotion.Reduce});await page.EvaluateAsync("()=>ownedFlash.dispose()");await page.Clock.RunForAsync(1500);Assert.Equal(0,await page.Locator(".cfx-native-motion-frame,[data-cfx-update-motion]").CountAsync());AssertNoConsoleErrors(session);
    }
}
