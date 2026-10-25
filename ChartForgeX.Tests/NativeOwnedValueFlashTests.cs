using System.Text.Json;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeOwnedValueFlashTests {
    [Theory] [InlineData("line")] [InlineData("bar-flat")] [InlineData("bar-gradient")] [InlineData("bar-capsule")]
    [InlineData("pie")] [InlineData("donut")] [InlineData("raw-pie")] [InlineData("raw-donut")] [InlineData("treemap")] [InlineData("sunburst")]
    public void FlashUsesOnlyNativeValueOwnershipWithUnchangedGeometryMetadataContextsAndExactFinalExport(string kind) {
        var renderer=new HtmlInteractiveChartRenderer();var raw=kind.StartsWith("raw-",StringComparison.Ordinal);var from=NativeOwnedValueFlashBeforeControls.Section(renderer,kind,raw?60:30);var to=NativeOwnedValueFlashBeforeControls.Section(renderer,kind,raw?5:40);var peak=ChartColor.FromHex("#FFC000");
        IReadOnlyList<PreparedVisual>? observed=null;string? payload=null;var error=Record.Exception(()=>{observed=VisualValueCapture.NativeFrames(from.PreparedVisual,to.PreparedVisual,3,peak);payload=renderer.PrepareNativeTransitionJson(from,to,new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.ValueFlash,3,peak));});
        NativeRadialMotionBeforeControls.Capture(from,"owned-flash-"+kind+"-from");NativeRadialMotionBeforeControls.Capture(to,"owned-flash-"+kind+"-to");NativeRadialMotionBeforeControls.Save("owned-flash-"+kind+".facts.json",JsonSerializer.Serialize(new{payload,error=error?.ToString()}));if(observed!=null)NativeRadialValueMotionTests.Capture(from,to,observed,payload??string.Empty,"owned-flash-"+kind);
        Assert.Null(error);var frames=Assert.IsAssignableFrom<IReadOnlyList<PreparedVisual>>(observed);Assert.Same(to.PreparedVisual,frames[0]);Assert.Same(to.PreparedVisual,frames[^1]);Assert.Equal(to.PreparedVisual.ToSvg(),frames[^1].ToSvg());Assert.Equal(to.PreparedVisual.ToPng(),frames[^1].ToPng());Assert.NotEqual(to.PreparedVisual.ToPng(),frames[1].ToPng());
        var target=to.PreparedVisual.Scene.Nodes;var flash=frames[1].Scene.Nodes;Assert.Equal(target.Count,flash.Count);var changed=new List<(string? Role,string? Series,string? Observation,string? Node)>();var stack=new Stack<(string? Series,string? Observation,string? Node)>();var current=(Series:(string?)null,Observation:(string?)null,Node:(string?)null);
        for(var index=0;index<target.Count;index++) {
            if(target[index] is VisualSceneGroup group){stack.Push(current);if(group.Metadata.TryGetValue("data-cfx-series-key",out var series))current.Series=series;if(group.Metadata.TryGetValue("data-cfx-observation-id",out var point))current.Observation=point;if(group.Metadata.TryGetValue("data-cfx-node",out var node))current.Node=node;}
            else if(target[index] is VisualSceneEndGroup)current=stack.Pop();
            if(ReferenceEquals(target[index],flash[index]))continue;changed.Add((target[index].Role,current.Series,current.Observation,current.Node));
            if(target[index] is VisualSceneGradient gradient){var actual=Assert.IsType<VisualSceneGradient>(flash[index]);Assert.Equal(gradient.Start,actual.Start);Assert.Equal(gradient.End,actual.End);Assert.Equal(gradient.Stops.Select(row=>row.Offset),actual.Stops.Select(row=>row.Offset));Assert.All(actual.Stops,row=>Assert.True(row.Color.Equals(peak)));Geometry(gradient.Shape,actual.Shape);}
            else {var expected=Assert.IsAssignableFrom<VisualSceneMark>(target[index]);var actual=Assert.IsAssignableFrom<VisualSceneMark>(flash[index]);Geometry(expected,actual);Assert.True(actual.Fill.HasValue&&actual.Fill.Value.Equals(peak)||actual.Stroke.HasValue&&actual.Stroke.Value.Equals(peak));}
        }
        NativeRadialMotionBeforeControls.Save("owned-flash-"+kind+".owners.json",JsonSerializer.Serialize(changed));Assert.NotEmpty(changed);
        Assert.All(changed,row=>{Assert.NotEqual("comparison",row.Series);Assert.DoesNotContain(row.Role??string.Empty,new[]{"bar-track","grid-line","axis","title","data-label","radial-outer-outline"});});
        if(kind=="line"){Assert.Contains(changed,row=>row.Role=="line");Assert.Contains(changed,row=>row.Role=="marker"&&row.Observation=="current");Assert.All(changed.Where(row=>row.Role is "marker" or "marker-pattern"),row=>Assert.Equal("current",row.Observation));}
        if(kind.StartsWith("bar",StringComparison.Ordinal))Assert.All(changed,row=>Assert.Equal("current",row.Observation));
        if(kind is "treemap" or "sunburst"){Assert.Contains(changed,row=>row.Node=="leaf-a");Assert.DoesNotContain(changed,row=>row.Node is "leaf-b" or "leaf-c");}
        Assert.Equal(to.PreparedVisual.Regions.Select(row=>(row.Id,row.Role,row.Label,row.Bounds)),frames[1].Regions.Select(row=>(row.Id,row.Role,row.Label,row.Bounds)));Assert.Equal(to.PreparedVisual.Accessibility.Description,frames[1].Accessibility.Description);
    }
    private static void Geometry(VisualSceneMark expected,VisualSceneMark actual) {
        Assert.Equal(expected.GetType(),actual.GetType());Assert.Equal(expected.Role,actual.Role);Assert.Equal(expected.Id,actual.Id);Assert.Equal(expected.Opacity,actual.Opacity);Assert.Equal(expected.StrokeWidth,actual.StrokeWidth);
        switch(expected){case VisualSceneRectangle rectangle:Assert.Equal(rectangle.Bounds,((VisualSceneRectangle)actual).Bounds);Assert.Equal(rectangle.Radius,((VisualSceneRectangle)actual).Radius);break;
        case VisualSceneEllipse ellipse:var actualEllipse=(VisualSceneEllipse)actual;Assert.Equal((ellipse.Cx,ellipse.Cy,ellipse.Rx,ellipse.Ry),(actualEllipse.Cx,actualEllipse.Cy,actualEllipse.Rx,actualEllipse.Ry));break;
        case VisualSceneLine line:var actualLine=(VisualSceneLine)actual;Assert.Equal((line.Start,line.End,line.DashOffset),(actualLine.Start,actualLine.End,actualLine.DashOffset));Assert.Equal(line.Dash,actualLine.Dash);break;
        case VisualScenePath path:var actualPath=(VisualScenePath)actual;Assert.Equal(path.Commands,actualPath.Commands);Assert.Equal((path.Close,path.Cap,path.Join,path.DashOffset),(actualPath.Close,actualPath.Cap,actualPath.Join,actualPath.DashOffset));Assert.Equal(path.Dash,actualPath.Dash);break;
        case VisualSceneSlice slice:var actualSlice=(VisualSceneSlice)actual;Assert.Equal((slice.Cx,slice.Cy,slice.Outer,slice.Inner,slice.Start,slice.Sweep),(actualSlice.Cx,actualSlice.Cy,actualSlice.Outer,actualSlice.Inner,actualSlice.Start,actualSlice.Sweep));break;}
    }
}
