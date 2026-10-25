using System.Text.Json;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;
namespace ChartForgeX.Tests;
[Collection(nameof(FontRegistryCollection))]
public sealed class NativeScatterValueTests {
    [Theory] [InlineData("default")] [InlineData("square")] [InlineData("customrole")] [InlineData("secondary")] [InlineData("time")] [InlineData("log")]
    public void NativeScatterValuesKeepActualTransformsMarkerPolicyIdentityAndExactIndependentFrames(string policy) {
        var renderer=new HtmlInteractiveChartRenderer();var model=NativeScatterValueBeforeControls.Model(policy,30);var from=renderer.PrepareSection(model);var to=renderer.PrepareSection(NativeScatterValueBeforeControls.Model(policy,40));var middle=renderer.PrepareSection(NativeScatterValueBeforeControls.Model(policy,35));
        IReadOnlyList<PreparedVisual>? frames=null;string? payload=null;var error=Record.Exception(()=>{frames=VisualValueCapture.NativeFrames(from.PreparedVisual,to.PreparedVisual,3,null);payload=renderer.PrepareNativeTransitionJson(from,to,new HtmlNativeTransitionOptions(frameCount:3));});
        NativeRadialMotionBeforeControls.Capture(from,"scatter-"+policy+"-from");NativeRadialMotionBeforeControls.Capture(to,"scatter-"+policy+"-to");NativeRadialMotionBeforeControls.Capture(middle,"scatter-"+policy+"-independent");NativeRadialMotionBeforeControls.Save("scatter-"+policy+".facts.json",JsonSerializer.Serialize(new{payload,error=error?.ToString()}));if(frames!=null)NativeRadialValueMotionTests.Capture(from,to,frames,payload??string.Empty,"scatter-"+policy);
        Assert.Null(error);var actual=Assert.IsAssignableFrom<IReadOnlyList<PreparedVisual>>(frames);Assert.Equal(middle.PreparedVisual.ToSvg(),actual[1].ToSvg());Assert.Equal(middle.PreparedVisual.ToSvg("scatter-middle"),actual[1].ToSvg("scatter-middle"));Assert.Equal(middle.PreparedVisual.ToPng(),actual[1].ToPng());Assert.Equal(to.PreparedVisual.ToSvg(),actual[^1].ToSvg());
        Assert.Equal(middle.PreparedVisual.Regions.Select(row=>(row.Id,row.Role,row.Bounds,row.Label)),actual[1].Regions.Select(row=>(row.Id,row.Role,row.Bounds,row.Label)));using var json=JsonDocument.Parse(payload!);Assert.Equal("cartesian-value",json.RootElement.GetProperty("sourceKind").GetString());
        model.Series[0].Points.Clear();model.WithFontFamily("changed caller");try{FontRegistry.Clear();var again=VisualValueCapture.NativeFrames(from.PreparedVisual,to.PreparedVisual,3,null);Assert.Equal(actual[1].ToSvg(),again[1].ToSvg());Assert.Equal(actual[1].ToPng(),again[1].ToPng());}finally{_=BrowserProofFonts.Family;}
        var peak=ChartColor.FromHex("#FFC000");var flash=VisualValueCapture.NativeFrames(from.PreparedVisual,to.PreparedVisual,3,peak);NativeRadialValueMotionTests.Capture(from,to,flash,renderer.PrepareNativeTransitionJson(from,to,new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.ValueFlash,3,peak)),"scatter-flash-"+policy);Assert.NotEqual(to.PreparedVisual.ToPng(),flash[1].ToPng());Assert.Equal(to.PreparedVisual.ToPng(),flash[^1].ToPng());
        if(policy=="customrole")Assert.Contains(flash[1].Scene.Nodes.OfType<VisualSceneMark>(),mark=>mark.Role=="inspection-mark"&&mark.Fill.HasValue&&mark.Fill.Value.Equals(peak));
    }
}
