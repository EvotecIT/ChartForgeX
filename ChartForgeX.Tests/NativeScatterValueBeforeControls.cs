using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;
[Collection(nameof(FontRegistryCollection))]
public sealed class NativeScatterValueBeforeControls {
    [Theory] [InlineData("default")] [InlineData("square")] [InlineData("customrole")] [InlineData("secondary")]
    public void ExistingNativeScatterValueApiCapturesAuthoredReadingsThroughItsMarkerOwner(string policy) {
        var renderer=new HtmlInteractiveChartRenderer();var from=renderer.PrepareSection(Model(policy,30));var to=renderer.PrepareSection(Model(policy,40));string? payload=null;var error=Record.Exception(()=>payload=renderer.PrepareValueTransitionJson(from,to,3));
        NativeRadialMotionBeforeControls.Capture(from,"scatter-before-"+policy+"-from");NativeRadialMotionBeforeControls.Capture(to,"scatter-before-"+policy+"-to");NativeRadialMotionBeforeControls.Save("scatter-before-"+policy+".facts.json",JsonSerializer.Serialize(new{payload,error=error?.ToString()}));Assert.Null(error);Assert.NotNull(payload);
    }
    [Theory] [InlineData("default")] [InlineData("square")] [InlineData("customrole")] [InlineData("secondary")]
    public void OrdinaryScatterNativePaintDefaultsRemainRepeatable(string policy) {
        var renderer=new HtmlInteractiveChartRenderer();var first=renderer.PrepareSection(Model(policy,30));var second=renderer.PrepareSection(Model(policy,30));NativeRadialMotionBeforeControls.Capture(first,"scatter-default-"+policy);Assert.Equal(first.Html,second.Html);Assert.Equal(first.PreparedVisual.ToSvg(),second.PreparedVisual.ToSvg());Assert.Equal(first.PreparedVisual.ToPng(),second.PreparedVisual.ToPng());
    }
    internal static Chart Model(string policy,double value,int width=760,bool dark=false) {
        var x=policy=="time"?new DateTime(2026,1,1).ToOADate():1d;var series=new ChartSeries(new ChartSeriesId("measurements"),"Measurements",ChartSeriesKind.Scatter,new[]{new ChartPoint(x,20),new ChartPoint(x+1,value),new ChartPoint(x+2,60)})
            .WithObservationIds(new[]{new ChartObservationId("earlier"),new ChartObservationId("current"),new ChartObservationId("later")});
        if(policy is "square" or "customrole"){series.FillPattern=ChartFillPattern.DiagonalForward;series.ConfigureMarkers(markers=>{markers.Shape=ChartMarkerShape.Square;markers.Radius=7;markers.Stroke=ChartColor.Black;markers.StrokeWidth=2;});}
        if(policy=="customrole")series.SemanticRole="inspection-mark";
        var chart=Chart.Create().WithSize(width,420).WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family),dark?VisualThemeMode.Dark:VisualThemeMode.Light).WithHeader(false).WithLegend(false).WithDataLabels(false).AddSeries(series);
        if(policy=="secondary"){series.ValueAxis=ChartAxisSide.Secondary;chart.AddSeries(ChartSeries.CreateLine(new ChartSeriesId("context"),"Context",new[]{new ChartPoint(x,10),new ChartPoint(x+1,15),new ChartPoint(x+2,20)}).WithObservationIds(new[]{new ChartObservationId("earlier"),new ChartObservationId("current"),new ChartObservationId("later")}));}
        chart.ConfigureXAxis(axis=>{axis.Minimum=x-.5;axis.Maximum=x+2.5;if(policy=="time")axis.Scale=ChartScaleKind.Time;});chart.ConfigureYAxis(axis=>{axis.Minimum=policy=="log"?1:0;axis.Maximum=100;if(policy=="log")axis.Scale=ChartScaleKind.Logarithmic;});chart.ConfigureSecondaryYAxis(axis=>{axis.Minimum=0;axis.Maximum=100;});return chart;
    }
}
