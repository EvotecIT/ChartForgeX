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
    [BrowserTheory] [InlineData(false,"zero",390,false,false)] [InlineData(true,"empty",760,true,true)]
    [InlineData(false,"other",760,true,false)] [InlineData(true,"zero",390,false,true)]
    public async Task RealRawNativeValuesKeepZeroOtherIdentityAndAcceptedExportsThroughTheExistingLifetime(bool donut,string state,int width,bool dark,bool initial) {
        if(!Enabled)return;var renderer=new HtmlInteractiveChartRenderer();var first=state is "zero" or "empty"?0d:60d;var last=state=="other"?5d:80d;
        HtmlChartSection Section(double value)=>renderer.PrepareSection(NativeRawValueSourceMotionTests.Model(donut,state,value,width,dark),configure:options=>{options.CaptureNativeValueSource=true;options.Interaction.ChartId="raw-browser";options.Interaction.Enable(ChartInteractionFeatures.Export);options.ResponsiveLayout=HtmlChartResponsiveLayout.Adaptive;options.Adaptive.NarrowWidth=350;options.Adaptive.NarrowHeight=420;});
        var from=Section(first);var to=Section(last);var frames=VisualValueCapture.NativeFrames(from.PreparedVisual,to.PreparedVisual,5,null);var payload=renderer.PrepareNativeTransitionJson(from,to,new HtmlNativeTransitionOptions(initial?HtmlNativeTransitionEffect.InitialValues:HtmlNativeTransitionEffect.Values,5));
        var name="raw-browser-"+donut+"-"+state+"-"+width;await using var session=await BrowserProofFonts.OpenAsync(Page(initial?to:from),width+24,680);var page=session.Page;await ReadyAndPauseAsync(page);await page.EmulateMediaAsync(new PageEmulateMediaOptions{ReducedMotion=ReducedMotion.NoPreference});
        await page.EvaluateAsync("()=>{window.rawChart=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'));window.rawEvents=[];window.rawRevision=rawChart.sceneRevision;rawChart.root.addEventListener('cfxupdated',event=>rawEvents.push(event.detail));}");
        if(initial)await page.EvaluateAsync("payload=>rawChart.presentInitial(payload,{sceneRevision:rawChart.sceneRevision,motion:{duration:1000}})",payload);else await page.EvaluateAsync("reply=>rawChart.update(reply.html,{sceneRevision:rawChart.sceneRevision,transition:reply.payload,motion:{duration:1000}})",new{html=to.Html,payload});await page.Clock.RunForAsync(300);
        var observed=await page.EvaluateAsync<JsonElement>("()=>({revision:rawChart.sceneRevision,before:rawRevision,events:rawEvents,metadata:JSON.parse(rawChart.root.dataset.cfxPreparedChart),drawing:rawChart.root.dataset.cfxAdaptiveDrawing,frame:rawChart.root.querySelector('.cfx-native-motion-frame')?.outerHTML,targets:rawChart.resolveTargets()})");
        NativeRadialValueMotionTests.Capture(from,to,frames,payload,name);NativeRadialMotionBeforeControls.Save(name+".observed.json",observed.GetRawText());
        if(Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is {Length:>0} output)await page.ScreenshotAsync(new PageScreenshotOptions{Path=Path.Combine(output,name+".browser.png"),FullPage=true});
        Assert.Equal(initial?0:1,observed.GetProperty("events").GetArrayLength());Assert.Equal(initial,observed.GetProperty("revision").GetString()==observed.GetProperty("before").GetString());Assert.Equal(last,observed.GetProperty("metadata").GetProperty("nativeValueSource").GetProperty("readings")[0].GetProperty("value").GetDouble());
        await NativeValueFrameBrowserProof.AssertAsync(page,"[data-cfx-role=radial-point][data-cfx-observation-id=primary] [data-cfx-role="+(donut?"donut-slice":"pie-slice")+"]",name+"-ink");
        var download=await page.RunAndWaitForDownloadAsync(()=>page.Locator("[data-cfx-export=svg]").ClickAsync());var file=await download.PathAsync()??throw new InvalidOperationException("Raw accepted SVG download missing.");var export=await File.ReadAllTextAsync(file);NativeRadialMotionBeforeControls.Save(name+".exported.svg",export);
        var endpoint=observed.GetProperty("drawing").GetString()=="narrow"?to.NarrowPreparedVisual!:to.PreparedVisual;
        var emission=observed.GetProperty("drawing").GetString()=="narrow"?to.NarrowEmission!:to.WideEmission!;
        var actual=System.Xml.Linq.XDocument.Parse(export);var expected=System.Xml.Linq.XDocument.Parse(emission.Svg);
        Assert.Equal(expected.Root!.Attribute("viewBox")!.Value,actual.Root!.Attribute("viewBox")!.Value);
        var expectedPaint=expected.Descendants().Where(row=>(string?)row.Attribute("data-cfx-role") is "pie-slice" or "donut-slice" or "radial-slice-outline").ToArray();
        var actualPaint=actual.Descendants().Where(row=>(string?)row.Attribute("data-cfx-role") is "pie-slice" or "donut-slice" or "radial-slice-outline").ToArray();
        Assert.Equal(expectedPaint.Length,actualPaint.Length);Assert.NotEmpty(actualPaint);
        for(var index=0;index<expectedPaint.Length;index++)Assert.Equal(expectedPaint[index].Attributes().Select(row=>(row.Name,row.Value)).OrderBy(row=>row.Name.ToString()),actualPaint[index].Attributes().Select(row=>(row.Name,row.Value)).OrderBy(row=>row.Name.ToString()));
        Assert.Equal(to.PreparedVisual.Scene.ValueCapture!.Plan.Readings.Select(row=>row.Value),observed.GetProperty("metadata").GetProperty("nativeValueSource").GetProperty("readings").EnumerateArray().Select(row=>row.GetProperty("value").GetDouble()));
        NativeRadialMotionBeforeControls.Save(name+".endpoint-native.svg",endpoint.ToSvg());
        await page.EmulateMediaAsync(new PageEmulateMediaOptions{ReducedMotion=ReducedMotion.Reduce});Assert.Equal(0,await page.Locator(".cfx-native-motion-frame").CountAsync());await page.EvaluateAsync("()=>rawChart.dispose()");await page.Clock.RunForAsync(2000);Assert.Equal(0,await page.Locator(".cfx-native-motion-frame,[data-cfx-update-motion]").CountAsync());AssertNoConsoleErrors(session);
    }
    [BrowserTheory] [InlineData(false,760,false)] [InlineData(true,390,true)]
    public async Task RealSourceReflowChangesRawValuesAndOtherMembershipOnTheExistingManagedLegendChannel(bool donut,int width,bool dark) {
        if(!Enabled)return;var renderer=new HtmlInteractiveChartRenderer();
        HtmlChartSection Section(double value,bool hidden)=>renderer.PrepareVisibilitySection(NativeRawValueSourceMotionTests.Model(donut,"other",value,width,dark),NativeRadialVisibilityReflowTests.View(hidden?new[]{ChartVisibilityTarget.RadialOther(new ChartSeriesId("parts"))}:Array.Empty<ChartVisibilityTarget>()),configure:options=>{options.CaptureNativeValueSource=true;options.Interaction.ChartId="source-reflow-browser";options.Interaction.Enable(ChartInteractionFeatures.Export|ChartInteractionFeatures.LegendToggles);});
        var from=Section(60,false);var to=Section(5,true);var frames=VisualSeriesVisibilityCapture.ReflowFrames(from.PreparedVisual,to.PreparedVisual,5,sourceReflow:true);var payload=renderer.PrepareNativeTransitionJson(from,to,new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.SourceReflow,5));
        var reset=Section(5,false);var resetPayload=renderer.PrepareNativeTransitionJson(to,reset,new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.LegendVisibility,5));var name="source-reflow-browser-"+donut+"-"+width;
        await using var session=await BrowserProofFonts.OpenAsync(Page(from),width+24,680);var page=session.Page;await ReadyAndPauseAsync(page);await page.EmulateMediaAsync(new PageEmulateMediaOptions{ReducedMotion=ReducedMotion.NoPreference});
        await page.EvaluateAsync("()=>{window.rawChart=ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart'));window.rawCalls=[];rawChart.connectNative((request,signal)=>new Promise(resolve=>rawCalls.push({request,signal,resolve})),{basis:'old-source',axes:null,legend:true,motion:{duration:1000}});}");
        await page.Locator("[data-cfx-role=legend-item][data-cfx-point='-1']").ClickAsync();await page.WaitForFunctionAsync("()=>rawCalls.length===1");var request=await page.EvaluateAsync<JsonElement>("()=>rawCalls[0].request");
        NativeReflowVisibilityTests.Capture(from,to,frames,payload,name);NativeRadialMotionBeforeControls.Save(name+".request.json",request.GetRawText());
        await page.EvaluateAsync("reply=>rawCalls[0].resolve(reply)",new{html=to.Html,basis="new-source",transition=payload});await page.Clock.RunForAsync(300);
        var observed=await page.EvaluateAsync<JsonElement>("()=>({metadata:JSON.parse(rawChart.root.dataset.cfxPreparedChart),drawing:rawChart.root.dataset.cfxAdaptiveDrawing,frame:rawChart.root.querySelector('.cfx-native-motion-frame')?.outerHTML,revision:rawChart.sceneRevision})");NativeRadialMotionBeforeControls.Save(name+".observed.json",observed.GetRawText());
        if(Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is {Length:>0} output)await page.ScreenshotAsync(new PageScreenshotOptions{Path=Path.Combine(output,name+".browser.png"),FullPage=true});
        Assert.Equal("legend",request.GetProperty("channel").GetString());Assert.Equal("radialOther",request.GetProperty("sourceVisibility").GetProperty("hiddenTargets")[0].GetProperty("kind").GetString());Assert.Equal(5,observed.GetProperty("metadata").GetProperty("nativeValueSource").GetProperty("readings")[0].GetProperty("value").GetDouble());Assert.Equal(1,observed.GetProperty("metadata").GetProperty("visibility").GetProperty("hiddenTargets").GetArrayLength());
        await NativeValueFrameBrowserProof.AssertAsync(page,"[data-cfx-role=radial-point][data-cfx-observation-id=primary] [data-cfx-role="+(donut?"donut-slice":"pie-slice")+"]",name+"-ink");
        await page.Locator("[data-cfx-reset]").ClickAsync();await page.WaitForFunctionAsync("()=>rawCalls.length===2");var resetRequest=await page.EvaluateAsync<JsonElement>("()=>rawCalls[1].request");NativeRadialMotionBeforeControls.Save(name+".reset.json",resetRequest.GetRawText());Assert.Equal("new-source",resetRequest.GetProperty("basis").GetString());Assert.Equal("reset",resetRequest.GetProperty("kind").GetString());
        await page.EvaluateAsync("reply=>rawCalls[1].resolve(reply)",new{html=reset.Html,basis="reset",transition=resetPayload});await page.Clock.RunForAsync(1000);await page.EvaluateAsync("()=>rawChart.dispose()");AssertNoConsoleErrors(session);
    }
}
