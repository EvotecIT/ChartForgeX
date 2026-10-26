using System.Text.Json;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeBubbleEndpointDomainTests {
    [Theory] [InlineData(false)] [InlineData(true)]
    public void NativeValidClippedBubbleYPublishesUsableCapturedSourceAndExactNativeFrames(bool area) {
        var renderer = new HtmlInteractiveChartRenderer(); var chart = NativeBubbleComponentBeforeControls.Model(120, 10, area); var targetChart = NativeBubbleComponentBeforeControls.Model(40, 10, area);
        var ordinary = renderer.PrepareSection(chart); var ordinaryTarget = renderer.PrepareSection(targetChart); var middle = renderer.PrepareSection(NativeBubbleComponentBeforeControls.Model(ChartMath.InterpolateRange(120, 40, .5), 10, area));
        NativeRadialMotionBeforeControls.Capture(ordinary, "bubble-outside-y-" + area + "-ordinary"); NativeRadialMotionBeforeControls.Capture(ordinaryTarget, "bubble-outside-y-" + area + "-target"); NativeRadialMotionBeforeControls.Capture(middle, "bubble-outside-y-" + area + "-independent");
        HtmlChartSection? captured = null, target = null; IReadOnlyList<PreparedVisual>? frames = null;
        var error = Record.Exception(() => { captured = NativeBubbleComponentBeforeControls.Section(renderer, chart); target = NativeBubbleComponentBeforeControls.Section(renderer, targetChart); frames = VisualValueCapture.NativeFrames(captured.PreparedVisual, target.PreparedVisual, 3, null); });
        NativeRadialMotionBeforeControls.Save("bubble-outside-y-" + area + ".facts.json", JsonSerializer.Serialize(new { rawY = chart.Series[0].Bubbles[0].Y, minimum = chart.Options.YAxis.Minimum, maximum = chart.Options.YAxis.Maximum, error = error?.ToString() }));
        if (captured != null) NativeRadialMotionBeforeControls.Capture(captured, "bubble-outside-y-" + area + "-captured"); if (frames != null) NativeRadialValueMotionTests.Capture(captured!, target!, frames, string.Empty, "bubble-outside-y-" + area);
        Assert.Null(error); Assert.NotNull(captured); var actual = Assert.IsAssignableFrom<IReadOnlyList<PreparedVisual>>(frames); Assert.Equal(ordinary.PreparedVisual.ToSvg(), captured!.PreparedVisual.ToSvg()); Assert.Equal(ordinary.PreparedVisual.ToPng(), captured.PreparedVisual.ToPng()); Assert.Equal(middle.PreparedVisual.ToSvg(), actual[1].ToSvg()); Assert.Equal(middle.PreparedVisual.ToPng(), actual[1].ToPng()); Assert.Equal(ordinaryTarget.PreparedVisual.ToSvg(), actual[^1].ToSvg());
        Assert.False(NativeRawValueSourceMotionTests.Metadata(ordinary).TryGetProperty("nativeValueSource", out _));
    }
}
