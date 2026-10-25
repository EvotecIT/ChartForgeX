using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeRawValueSourceMotionTests {
    [Theory] [InlineData(false,"zero",false)] [InlineData(true,"zero",true)] [InlineData(false,"empty",true)] [InlineData(true,"empty",false)]
    [InlineData(false,"other",false)] [InlineData(true,"other",true)] [InlineData(false,"radius",true)] [InlineData(true,"radius",false)]
    public void RawAuthoredReadingsReplayRealNativeZeroNoDataOtherAndUnpaintedRadiusGeometry(bool donut, string state, bool initial) {
        var renderer = new HtmlInteractiveChartRenderer(); var first = state is "zero" or "empty" ? 0d : 60d; var last = state == "other" ? 5d : 80d;
        var from = Section(renderer, Model(donut,state,first)); var to = Section(renderer, Model(donut,state,last));
        var middle = Section(renderer, Model(donut,state,ChartMath.InterpolateRange(first,last,.5)));
        IReadOnlyList<PreparedVisual>? observed = null; string? payload = null; var error = Record.Exception(() => { observed = VisualValueCapture.NativeFrames(from.PreparedVisual,to.PreparedVisual,3,null); payload = renderer.PrepareNativeTransitionJson(from,to,new HtmlNativeTransitionOptions(initial ? HtmlNativeTransitionEffect.InitialValues : HtmlNativeTransitionEffect.Values,3)); });
        var name = "raw-native-"+donut+"-"+state+"-"+initial; NativeRadialMotionBeforeControls.Capture(from,name+"-from"); NativeRadialMotionBeforeControls.Capture(to,name+"-to"); NativeRadialMotionBeforeControls.Capture(middle,name+"-independent");
        NativeRadialMotionBeforeControls.Save(name+".facts.json",JsonSerializer.Serialize(new { first,last,middle=ChartMath.InterpolateRange(first,last,.5),payload,error=error?.ToString() }));
        if(observed!=null)NativeRadialValueMotionTests.Capture(from,to,observed,payload??string.Empty,name);
        Assert.Null(error); var frames = Assert.IsAssignableFrom<IReadOnlyList<PreparedVisual>>(observed); NativeRadialValueMotionTests.Capture(from,to,frames,payload!,name);
        Assert.Equal(middle.PreparedVisual.ToSvg(),frames[1].ToSvg()); Assert.Equal(middle.PreparedVisual.ToSvg("raw-native-fixed"),frames[1].ToSvg("raw-native-fixed")); Assert.Equal(middle.PreparedVisual.ToPng(),frames[1].ToPng());
        Assert.Equal(to.PreparedVisual.ToSvg(),frames[^1].ToSvg()); Assert.Equal(to.PreparedVisual.ToPng(),frames[^1].ToPng());
        using var json = JsonDocument.Parse(payload!); var source = Metadata(from).GetProperty("nativeValueSource"); var destination = Metadata(to).GetProperty("nativeValueSource");
        Assert.Equal("radial-raw-value",json.RootElement.GetProperty("sourceKind").GetString()); Assert.Equal(from.PreparedVisual.Scene.ValueCapture!.Plan.Readings.Count,source.GetProperty("readings").GetArrayLength());
        Assert.Equal(source.GetProperty("structureKey").GetString(),destination.GetProperty("structureKey").GetString()); Assert.NotEqual(source.GetProperty("sourceKey").GetString(),destination.GetProperty("sourceKey").GetString());
        var facts = json.RootElement.GetProperty("drawings").GetProperty("wide").GetProperty("frames");
        Assert.Equal(source.GetProperty("sourceKey").GetString(),facts[0].GetProperty("nativeValueSource").GetProperty("sourceKey").GetString());
        Assert.Equal(destination.GetProperty("sourceKey").GetString(),facts[facts.GetArrayLength()-1].GetProperty("nativeValueSource").GetProperty("sourceKey").GetString());
        if(state is "zero" or "empty") Assert.DoesNotContain(from.PreparedVisual.Scene.Nodes.OfType<VisualSceneGroup>(),row=>row.Role=="radial-point"&&row.Metadata.TryGetValue("data-cfx-observation-id",out var id)&&id=="primary");
        if(state=="empty") Assert.Contains(from.PreparedVisual.Diagnostics,row=>row.Code=="radial.no-data");
        if(state=="other") Assert.Contains(destination.GetProperty("slices").EnumerateArray(),row=>row.GetProperty("pointIndex").GetInt32()==-1);
        Assert.Throws<NotSupportedException>(()=>renderer.PrepareValueTransitionJson(from,to,3));
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void OptInMetadataKeepsOrdinaryNativePaintAndHiddenRawValuePaintIdentityCannotMasquerade(bool donut) {
        var renderer = new HtmlInteractiveChartRenderer(); var chart = Model(donut,"radius",double.Epsilon).WithLegend(false).WithDataLabels(false).WithDonutCenterLabel(false);
        var ordinary = renderer.PrepareSection(chart); var from = Section(renderer,chart); var other = Model(donut,"radius",2*double.Epsilon).WithLegend(false).WithDataLabels(false).WithDonutCenterLabel(false); other.Series[0].WithPointColor(0,ChartColor.Black); var to = Section(renderer,other);
        NativeRadialMotionBeforeControls.Capture(ordinary,"raw-default-"+donut); NativeRadialMotionBeforeControls.Capture(from,"raw-hidden-from-"+donut); NativeRadialMotionBeforeControls.Capture(to,"raw-hidden-to-"+donut);
        Assert.False(Metadata(ordinary).TryGetProperty("nativeValueSource",out _)); Assert.Equal(ordinary.PreparedVisual.ToSvg(),from.PreparedVisual.ToSvg()); Assert.Equal(ordinary.PreparedVisual.ToPng(),from.PreparedVisual.ToPng());
        Assert.Equal(from.PreparedVisual.ToSvg("raw-unpainted"),to.PreparedVisual.ToSvg("raw-unpainted")); Assert.Equal(from.PreparedVisual.ToPng(),to.PreparedVisual.ToPng());
        Assert.NotEqual(Metadata(from).GetProperty("nativeValueSource").GetProperty("sourceKey").GetString(),Metadata(to).GetProperty("nativeValueSource").GetProperty("sourceKey").GetString());
        Assert.Throws<NotSupportedException>(()=>renderer.PrepareNativeTransitionJson(from,to,new HtmlNativeTransitionOptions()));
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void DetachedRawSourceAndCapturedFacesReplayAfterCallerAndRegistryMutation(bool donut) {
        var renderer = new HtmlInteractiveChartRenderer(); var chart = Model(donut,"other",60); var from = Section(renderer,chart); var to = Section(renderer,Model(donut,"other",5));
        var payload = renderer.PrepareNativeTransitionJson(from,to,new HtmlNativeTransitionOptions(frameCount:5)); var frames = VisualValueCapture.NativeFrames(from.PreparedVisual,to.PreparedVisual,5,null); NativeRadialValueMotionTests.Capture(from,to,frames,payload,"raw-detached-"+donut);
        chart.Series[0].Points.Clear(); chart.Options.MaximumPieSlices=2; chart.WithFontFamily("caller replacement");
        try { FontRegistry.Clear(); var repeated = VisualValueCapture.NativeFrames(from.PreparedVisual,to.PreparedVisual,5,null); Assert.Equal(payload,renderer.PrepareNativeTransitionJson(from,to,new HtmlNativeTransitionOptions(frameCount:5))); for(var index=0;index<frames.Count;index++){Assert.Equal(frames[index].ToSvg(),repeated[index].ToSvg());Assert.Equal(frames[index].ToPng(),repeated[index].ToPng());} }
        finally { _=BrowserProofFonts.Family; }
    }
    [Theory] [InlineData(false,false)] [InlineData(true,false)] [InlineData(false,true)] [InlineData(true,true)]
    public void SourceReflowBindsChangedFullRawValuesAndAbsoluteNativeOtherMembershipToOldNewFrames(bool donut,bool reverse) {
        var renderer = new HtmlInteractiveChartRenderer(); var from = Reflow(renderer,Model(donut,"other",reverse?5:60),reverse); var to = Reflow(renderer,Model(donut,"other",reverse?60:5),!reverse);
        var frames = VisualSeriesVisibilityCapture.ReflowFrames(from.PreparedVisual,to.PreparedVisual,5,sourceReflow:true); var payload = renderer.PrepareNativeTransitionJson(from,to,new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.SourceReflow,5));
        NativeReflowVisibilityTests.Capture(from,to,frames,payload,"raw-source-reflow-"+donut+"-"+reverse);
        Assert.Equal(from.PreparedVisual.ToSvg(),frames[0].Frame.ToSvg()); Assert.Equal(to.PreparedVisual.ToSvg(),frames[^1].Frame.ToSvg()); Assert.Equal(to.PreparedVisual.ToPng(),frames[^1].Frame.ToPng());
        Assert.True(frames[0].Source); Assert.False(frames[2].Source); Assert.Equal(0,frames[2].Opacity); Assert.Empty(frames[2].Frame.Regions);
        Assert.Throws<NotSupportedException>(()=>renderer.PrepareNativeTransitionJson(from,to,new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.LegendVisibility,5)));
        using var json = JsonDocument.Parse(payload); Assert.Equal("source-reflow",json.RootElement.GetProperty("effect").GetString()); var wire = json.RootElement.GetProperty("drawings").GetProperty("wide").GetProperty("frames");
        for(var index=0;index<frames.Count;index++){var expected=Metadata(frames[index].Source?from:to).GetProperty("nativeValueSource");Assert.Equal(expected.GetRawText(),wire[index].GetProperty("nativeValueSource").GetRawText());}
    }
    [Theory] [InlineData("negative")] [InlineData("unknown-id")] [InlineData("callback")] [InlineData("changed-policy")]
    public void RawOptInRetainsNativeDomainFullSourceAndImmutablePresentationAdmission(string condition) {
        var renderer = new HtmlInteractiveChartRenderer(); var chart=Model(false,"other",60); var from=Section(renderer,chart); Exception? error;
        if(condition=="negative"){chart.Series[0].Points[0]=new ChartPoint(1,-1);error=Record.Exception(()=>Reflow(renderer,chart,true));}
        else if(condition=="callback"){chart.WithValueFormat(ChartValueFormat.Custom(value=>"callback"));error=Record.Exception(()=>Section(renderer,chart));}
        else {chart.Series[0].Points[0]=new ChartPoint(1,5);if(condition=="unknown-id")chart.Series[0].WithObservationId(0,new ChartObservationId("different"));else chart.Options.PieSliceOpacity=.5; var to=Section(renderer,chart);error=Record.Exception(()=>renderer.PrepareNativeTransitionJson(from,to,new HtmlNativeTransitionOptions()));NativeRadialMotionBeforeControls.Capture(to,"raw-reject-"+condition+"-to");}
        NativeRadialMotionBeforeControls.Capture(from,"raw-reject-"+condition+"-from");NativeRadialMotionBeforeControls.Save("raw-reject-"+condition+".facts.json",JsonSerializer.Serialize(new{condition,error=error?.ToString()}));
        if(condition=="negative")Assert.IsType<InvalidOperationException>(error);else Assert.IsType<NotSupportedException>(error);
    }
    internal static Chart Model(bool donut,string state,double value,int width=760,bool dark=false) {
        var chart=NativeRadialMotionBeforeControls.Model(donut,state=="other","inside",value,width,dark);
        if(state=="empty")for(var index=1;index<chart.Series[0].Points.Count;index++)chart.Series[0].Points[index]=new ChartPoint(index+1,0);
        if(state=="radius")chart.Series[0].WithPointSliceRadiusRatio(0,0);return chart;
    }
    internal static HtmlChartSection Section(HtmlInteractiveChartRenderer renderer,Chart chart) => renderer.PrepareSection(chart,configure:options=>{options.CaptureNativeValueSource=true;options.Interaction.ChartId="native-raw";});
    internal static HtmlChartSection Reflow(HtmlInteractiveChartRenderer renderer,Chart chart,bool hidden) => renderer.PrepareVisibilitySection(chart,new ChartVisibilityView(hidden?new[]{ChartVisibilityTarget.RadialOther(new ChartSeriesId("parts"))}:Array.Empty<ChartVisibilityTarget>(),ChartVisibilityLayout.Reflow),configure:options=>{options.CaptureNativeValueSource=true;options.Interaction.ChartId="native-raw";});
    internal static JsonElement Metadata(HtmlChartSection section) {var match=Regex.Match(section.Html,"data-cfx-prepared-chart=\"([^\"]+)\"");using var document=JsonDocument.Parse(WebUtility.HtmlDecode(match.Groups[1].Value));return document.RootElement.Clone();}
}
