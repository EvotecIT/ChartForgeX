using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;
[Collection(nameof(FontRegistryCollection))]
public sealed class NativeJointValuesVisibilityBeforeControls {
    [Theory][InlineData(false,false)][InlineData(false,true)][InlineData(true,false)][InlineData(true,true)]
    public void ExistingSeparateIntentsDeclineAccidentalMixedUpdatesWithoutChangingTheirNativeControls(bool bar,bool reverse) {
        var renderer=new HtmlInteractiveChartRenderer();var from=Section(renderer,Model(bar,30),reverse);var to=Section(renderer,Model(bar,40),!reverse);var middle=Model(bar,ChartMath.InterpolateRange(30,40,.5));var visible=Section(renderer,middle,false);var hidden=Section(renderer,middle,true);
        var errors=new List<Exception?>();foreach(var effect in new[]{HtmlNativeTransitionEffect.Values,HtmlNativeTransitionEffect.LegendVisibility})errors.Add(Record.Exception(()=>renderer.PrepareNativeTransitionJson(from,to,new HtmlNativeTransitionOptions(effect,3))));
        var name="joint-before-"+bar+"-"+reverse;NativeRadialMotionBeforeControls.Capture(from,name+"-from");NativeRadialMotionBeforeControls.Capture(to,name+"-to");NativeRadialMotionBeforeControls.Capture(visible,name+"-independent-visible");NativeRadialMotionBeforeControls.Capture(hidden,name+"-independent-hidden");NativeRadialMotionBeforeControls.Save(name+".facts.json",JsonSerializer.Serialize(new{errors=errors.Select(error=>error?.ToString())}));Assert.All(errors,error=>Assert.IsType<NotSupportedException>(error));
    }
    [Theory][InlineData(false,false)][InlineData(false,true)][InlineData(true,false)][InlineData(true,true)]
    public void ExistingSeparateValueAndVisibilityIntentsKeepExactOrdinarySourceOutput(bool bar,bool visibility) {
        var renderer=new HtmlInteractiveChartRenderer();var from=Section(renderer,Model(bar,30),false);var to=Section(renderer,Model(bar,visibility?30:40),visibility);var repeat=Section(renderer,Model(bar,30),false);
        var frames=visibility?VisualSeriesVisibilityCapture.Frames(from.PreparedVisual,to.PreparedVisual,3):VisualValueCapture.NativeFrames(from.PreparedVisual,to.PreparedVisual,3,null);var payload=renderer.PrepareNativeTransitionJson(from,to,new HtmlNativeTransitionOptions(visibility?HtmlNativeTransitionEffect.LegendVisibility:HtmlNativeTransitionEffect.Values,3));NativeRadialValueMotionTests.Capture(from,to,frames,payload,"joint-ordinary-"+bar+"-"+visibility);Assert.Equal(from.Html,repeat.Html);Assert.Equal(from.PreparedVisual.ToSvg(),repeat.PreparedVisual.ToSvg());Assert.Equal(from.PreparedVisual.ToPng(),repeat.PreparedVisual.ToPng());
    }
    internal static Chart Model(bool bar,double value,int width=760,bool dark=false) {
        var chart=NativeCartesianValueBeforeControls.Model(bar,"linear",value).WithHeader(false).WithLegend(true).WithSize(width,420).WithFontFamily(BrowserProofFonts.Family).WithThemeMode(dark?ChartForgeX.Themes.VisualThemeMode.Dark:ChartForgeX.Themes.VisualThemeMode.Light);
        if(!bar)chart.WithLineMarkers(ChartLineMarkerMode.All).WithLuminousLineStyle();
        var sibling=new ChartSeries(new ChartSeriesId("comparison"),"Comparison",bar?ChartSeriesKind.Bar:ChartSeriesKind.Line,new[]{new ChartForgeX.Primitives.ChartPoint(0,10),new ChartForgeX.Primitives.ChartPoint(1,15),new ChartForgeX.Primitives.ChartPoint(2,20)}).WithObservationIds(new[]{new ChartObservationId("earlier"),new ChartObservationId("current"),new ChartObservationId("later")});chart.AddSeries(sibling);return chart;
    }
    internal static ChartVisibilityView View(bool hidden,ChartVisibilityLabelLayering labels=ChartVisibilityLabelLayering.PreserveOverlay)=>new(hidden?new[]{new ChartSeriesId("delivery")}:Array.Empty<ChartSeriesId>(),labels);
    internal static HtmlChartSection Section(HtmlInteractiveChartRenderer renderer,Chart chart,bool hidden,ChartVisibilityLabelLayering labels=ChartVisibilityLabelLayering.PreserveOverlay)=>renderer.PrepareVisibilitySection(chart,View(hidden,labels),configure:options=>options.Interaction.ChartId="native-joint");
}
