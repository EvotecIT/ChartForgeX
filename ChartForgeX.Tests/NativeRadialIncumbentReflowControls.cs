using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Apply only after the frozen Cartesian/Reflow prerequisites and before this radial product. Uses their existing API only.</summary>
[Collection(nameof(FontRegistryCollection))]
public sealed class NativeRadialIncumbentReflowControls {
    [Theory] [InlineData("line")] [InlineData("bar")] [InlineData("boxes")] [InlineData("raw")]
    public void IncumbentFiniteReflowSourcePayloadAndFramesRemainExact(string kind) {
        var chart = NativeReflowVisibilityTests.Model(kind, labels: true).WithFontFamily(BrowserProofFonts.Family);
        var renderer = new HtmlInteractiveChartRenderer(); var from = renderer.PrepareVisibilitySection(chart, NativeReflowVisibilityTests.View(kind, false));
        var to = renderer.PrepareVisibilitySection(chart, NativeReflowVisibilityTests.View(kind, true)); var frames = VisualSeriesVisibilityCapture.ReflowFrames(from.PreparedVisual, to.PreparedVisual, 3);
        var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.LegendVisibility, 3));
        NativeReflowVisibilityTests.Capture(from, to, frames, payload, "incumbent-before-radial-" + kind);
        Assert.Equal(from.PreparedVisual.Scene.VisibilityCapture!.SourceKey, to.PreparedVisual.Scene.VisibilityCapture!.SourceKey);
        Assert.Equal(to.PreparedVisual.ToSvg(), frames[2].Frame.ToSvg()); Assert.Equal(to.PreparedVisual.ToPng(), frames[2].Frame.ToPng());
    }
}
