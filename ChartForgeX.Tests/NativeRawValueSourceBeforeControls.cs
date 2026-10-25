using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeRawValueSourceBeforeControls {
    [Theory] [InlineData(false,"zero")] [InlineData(true,"zero")] [InlineData(false,"empty")] [InlineData(true,"empty")] [InlineData(false,"other")] [InlineData(true,"other")]
    public void OrdinaryNativeZeroAndOtherRetainActualSourceCellsAndTheirOwnPaintMembership(bool donut,string state) {
        var chart=NativeRadialMotionBeforeControls.Model(donut,state=="other","inside");
        if(state=="zero")chart.Series[0].Points[0]=new ChartPoint(1,0);
        if(state=="empty")for(var index=0;index<chart.Series[0].Points.Count;index++)chart.Series[0].Points[index]=new ChartPoint(index+1,0);
        var renderer=new HtmlInteractiveChartRenderer();var section=renderer.PrepareSection(chart);var repeat=renderer.PrepareSection(chart);var name="raw-before-"+donut+"-"+state;
        NativeRadialMotionBeforeControls.Capture(section,name);
        var marks=section.PreparedVisual.Scene.Nodes.OfType<VisualSceneGroup>().Where(row=>row.Role=="radial-point").ToArray();
        NativeRadialMotionBeforeControls.Save(name+".facts.json",JsonSerializer.Serialize(new{cells=chart.Series[0].Points.Select((point,index)=>new{index,id=chart.Series[0].ObservationIds[index].Value,value=point.Y}),paint=marks.Select(row=>row.Metadata)}));
        Assert.Equal(section.Html,repeat.Html);Assert.Equal(section.PreparedVisual.ToSvg(),repeat.PreparedVisual.ToSvg());Assert.Equal(section.PreparedVisual.ToPng(),repeat.PreparedVisual.ToPng());
        if(state=="zero"){Assert.Equal(0,chart.Series[0].Points[0].Y);Assert.DoesNotContain(marks,row=>row.Metadata.TryGetValue("data-cfx-observation-id",out var id)&&id=="primary");}
        if(state=="empty"){Assert.Empty(marks);Assert.Contains(section.PreparedVisual.Diagnostics,row=>row.Code=="radial.no-data");}
        if(state=="other"){var other=Assert.Single(marks,row=>row.Metadata["data-cfx-point"]=="-1");Assert.Equal("2,3,4",other.Metadata["data-cfx-source-points"]);Assert.Equal("15",other.Metadata["data-cfx-value"]);Assert.False(other.Metadata.ContainsKey("data-cfx-observation-id"));}
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void UnoptedExistingValueApiDeclinesMissingPaintReadingWithoutCreatingAnObservation(bool donut) {
        var renderer=new HtmlInteractiveChartRenderer();var start=NativeRadialMotionBeforeControls.Model(donut,false);start.Series[0].Points[0]=new ChartPoint(1,0);
        var from=renderer.PrepareSection(start);var to=renderer.PrepareSection(NativeRadialMotionBeforeControls.Model(donut,false));var name="raw-before-admission-"+donut;
        NativeRadialMotionBeforeControls.Capture(from,name+"-from");NativeRadialMotionBeforeControls.Capture(to,name+"-to");Exception? error=null;
        try{_=renderer.PrepareNativeTransitionJson(from,to,new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.Values,3));}catch(Exception failure){error=failure;}
        NativeRadialMotionBeforeControls.Save(name+".facts.json",JsonSerializer.Serialize(new{type=error?.GetType().Name,message=error?.Message}));Assert.IsType<NotSupportedException>(error);
    }
}
