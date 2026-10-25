using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeOwnedValueFlashBeforeControls {
    [Theory] [InlineData("line")] [InlineData("bar-gradient")] [InlineData("pie")] [InlineData("donut")] [InlineData("raw-pie")] [InlineData("raw-donut")] [InlineData("treemap")] [InlineData("sunburst")]
    public void ExistingPublicCaptureCanFlashItsActualNativeOwnedValueInk(string kind) {
        var renderer=new HtmlInteractiveChartRenderer();var from=Section(renderer,kind,kind.StartsWith("raw-",StringComparison.Ordinal)?60:30);var to=Section(renderer,kind,kind.StartsWith("raw-",StringComparison.Ordinal)?5:40);
        string? payload=null;var error=Record.Exception(()=>payload=renderer.PrepareNativeTransitionJson(from,to,new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.ValueFlash,3,ChartColor.FromHex("#FFC000"))));
        NativeRadialMotionBeforeControls.Capture(from,"owned-flash-before-"+kind+"-from");NativeRadialMotionBeforeControls.Capture(to,"owned-flash-before-"+kind+"-to");NativeRadialMotionBeforeControls.Save("owned-flash-before-"+kind+".facts.json",JsonSerializer.Serialize(new{payload,error=error?.ToString()}));
        Assert.Null(error);Assert.NotNull(payload);
    }
    [Theory] [InlineData("line")] [InlineData("bar-gradient")] [InlineData("pie")] [InlineData("donut")] [InlineData("raw-pie")] [InlineData("raw-donut")] [InlineData("treemap")] [InlineData("sunburst")]
    public void OrdinaryNativeDefaultsAndExistingValueFramesKeepTheirExactSourceControls(string kind) {
        var renderer=new HtmlInteractiveChartRenderer();var first=kind.StartsWith("raw-",StringComparison.Ordinal)?60d:30d;var last=kind.StartsWith("raw-",StringComparison.Ordinal)?5d:40d;
        var from=Section(renderer,kind,first);var to=Section(renderer,kind,last);var middle=Section(renderer,kind,ChartMath.InterpolateRange(first,last,.5));var repeat=Section(renderer,kind,first);
        var frames=VisualValueCapture.NativeFrames(from.PreparedVisual,to.PreparedVisual,3,null);var payload=renderer.PrepareNativeTransitionJson(from,to,new HtmlNativeTransitionOptions(frameCount:3));
        NativeRadialValueMotionTests.Capture(from,to,frames,payload,"owned-flash-ordinary-"+kind);NativeRadialMotionBeforeControls.Capture(middle,"owned-flash-ordinary-"+kind+"-independent");
        Assert.Equal(from.Html,repeat.Html);Assert.Equal(from.PreparedVisual.ToSvg(),repeat.PreparedVisual.ToSvg());Assert.Equal(from.PreparedVisual.ToPng(),repeat.PreparedVisual.ToPng());Assert.Equal(middle.PreparedVisual.ToSvg(),frames[1].ToSvg());Assert.Equal(middle.PreparedVisual.ToPng(),frames[1].ToPng());
    }
    internal static Chart Model(string kind,double reading,int width=760,bool dark=false) {
        Chart chart;
        if(kind is "treemap" or "sunburst")chart=NativeHierarchyMotionBeforeControls.Model(kind=="sunburst",false,reading);
        else if(kind.Contains("pie",StringComparison.Ordinal)||kind.Contains("donut",StringComparison.Ordinal))chart=NativeRadialMotionBeforeControls.Model(kind.Contains("donut",StringComparison.Ordinal),kind.StartsWith("raw-",StringComparison.Ordinal),"inside",reading);
        else {
            chart=NativeCartesianValueTransitionTests.Styled(kind=="line"?"line-pattern-markers":kind,reading);
            if(kind=="line")chart.WithLuminousLineStyle();
            var comparison=new ChartSeries(new ChartSeriesId("comparison"),"Comparison",kind=="line"?ChartSeriesKind.Line:ChartSeriesKind.Bar,new[]{new ChartPoint(0,10),new ChartPoint(1,15),new ChartPoint(2,20)})
                .WithObservationIds(new[]{new ChartObservationId("earlier"),new ChartObservationId("current"),new ChartObservationId("later")});chart.AddSeries(comparison);
        }
        return chart.WithSize(width,420).WithThemeMode(dark?ChartForgeX.Themes.VisualThemeMode.Dark:ChartForgeX.Themes.VisualThemeMode.Light);
    }
    internal static HtmlChartSection Section(HtmlInteractiveChartRenderer renderer,string kind,double reading,int width=760,bool dark=false)=>renderer.PrepareSection(Model(kind,reading,width,dark),configure:options=>{options.CaptureNativeValueSource=kind.StartsWith("raw-",StringComparison.Ordinal);options.Interaction.ChartId="owned-flash";});
}
