using System.Text.Json;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeRangeFlashPolicyTests {
    [Theory] [InlineData(false, "markers-off")] [InlineData(true, "markers-off")] [InlineData(false, "circle")]
    [InlineData(true, "secondary")] [InlineData(true, "log")] [InlineData(false, "clipped")]
    public void FlashRetainsTheRealNativeMarkerAxisAndClipPolicyWithoutCreatingInk(bool area, string policy) {
        var renderer = new HtmlInteractiveChartRenderer(); var from = NativeRangeFlashFixture.Section(renderer, area, false, "upper", policy);
        var to = NativeRangeFlashFixture.Section(renderer, area, true, "upper", policy); var peak = ChartColor.FromHex("#FFC000");
        var frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, peak);
        var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.ValueFlash, 3, peak));
        NativeRadialValueMotionTests.Capture(from, to, frames, payload, "range-flash-policy-" + area + "-" + policy);
        NativeRangeFlashTests.AssertOwnership(to.PreparedVisual, frames[1], "upper", peak);
        Assert.Equal(to.PreparedVisual.ToSvg(), frames[0].ToSvg()); Assert.Equal(to.PreparedVisual.ToPng(), frames[^1].ToPng());
        if (policy == "markers-off") Assert.DoesNotContain(frames[1].Scene.Nodes, node => node.Role == "range-marker" || node.Role == "range-marker-pattern");
        if (policy == "clipped") {
            Assert.Contains(to.PreparedVisual.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.Clip.HasValue);
            var readings = NativeRawValueSourceMotionTests.Metadata(to).GetProperty("nativeValueSource").GetProperty("readings");
            Assert.Equal(10, readings[0].GetProperty("value").GetDouble()); Assert.Equal(20, readings[0].GetProperty("minimum").GetDouble());
            Assert.Equal(60, readings[1].GetProperty("value").GetDouble()); Assert.Equal(50, readings[1].GetProperty("maximum").GetDouble());
        }
    }

    [Fact]
    public void HalfFlashKeepsTheExistingPremultipliedSrgbAndAlphaLawForTheRealTranslucentEnvelope() {
        var renderer = new HtmlInteractiveChartRenderer(); var from = NativeRangeFlashFixture.Section(renderer, false, false, "lower", "alpha");
        var to = NativeRangeFlashFixture.Section(renderer, false, true, "lower", "alpha"); var peak = ChartColor.FromRgba(224, 64, 16, 64);
        var frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 5, peak);
        VisualSceneMark Fill(PreparedVisual visual) => Assert.IsAssignableFrom<VisualSceneMark>(NativeRangeFlashFixture.Owned(visual).First(row => row.Series == "delivery" && row.Node.Role == "range-band").Node);
        // Authored RGBA(32,96,160,128) at the Band opacity .20 gives alpha26. At half flash:
        // alpha=(26+64)/2=45; RGB=(source*26+peak*64)/90, rounded to nearest even.
        Assert.Equal(ChartColor.FromRgba(32, 96, 160, 26), Fill(to.PreparedVisual).Fill!.Value);
        Assert.Equal(ChartColor.FromRgba(169, 73, 58, 45), Fill(frames[1]).Fill!.Value);
        Assert.Equal(Fill(frames[1]).Fill, Fill(frames[3]).Fill); Assert.Equal(peak, Fill(frames[2]).Fill!.Value);
        Assert.Equal(Fill(to.PreparedVisual).Opacity, Fill(frames[1]).Opacity);
        NativeRadialValueMotionTests.Capture(from, to, frames, renderer.PrepareNativeTransitionJson(from, to,
            new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.ValueFlash, 5, peak)), "range-flash-premultiplied-alpha");
    }

    [Theory] [InlineData(false)] [InlineData(true)]
    public void FlashReplayAndPngSvgExportsStayDetachedAfterCallerAndRegistryMutation(bool area) {
        var renderer = new HtmlInteractiveChartRenderer(); var before = NativeRangeFlashFixture.Model(area, false, "tuples"); var after = NativeRangeFlashFixture.Model(area, true, "tuples");
        var from = renderer.PrepareSection(before, configure: NativeRangeFlashFixture.Configure); var to = renderer.PrepareSection(after, configure: NativeRangeFlashFixture.Configure); var peak = ChartColor.FromHex("#FFC000");
        var frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 5, peak); var options = new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.ValueFlash, 5, peak);
        var payload = renderer.PrepareNativeTransitionJson(from, to, options); var savedSvg = frames.Select(row => row.ToSvg()).ToArray(); var savedPng = frames.Select(row => row.ToPng()).ToArray();
        before.Series[0].ReplaceRanges(Array.Empty<ChartForgeX.Core.ChartRangeBand>()); after.Series[1].ReplaceRanges(Array.Empty<ChartForgeX.Core.ChartRangeBand>());
        before.WithFontFamily("changed caller font"); after.Series[0].WithColor(ChartColor.Black).UseSolidFill();
        try {
            FontRegistry.Clear(); var repeat = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 5, peak);
            Assert.Equal(payload, renderer.PrepareNativeTransitionJson(from, to, options));
            for (var index = 0; index < repeat.Count; index++) { Assert.Equal(savedSvg[index], repeat[index].ToSvg()); Assert.Equal(savedPng[index], repeat[index].ToPng()); }
        } finally { _ = BrowserProofFonts.Family; }
    }

    [Fact]
    public void FlashStillRequiresChangedCapturedTuplesOnePolicyBoundedFramesAndCancellation() {
        var renderer = new HtmlInteractiveChartRenderer(); var from = NativeRangeFlashFixture.Section(renderer, false, false); var to = NativeRangeFlashFixture.Section(renderer, false, true);
        var options = new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.ValueFlash, 5, ChartColor.Black);
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => renderer.PrepareNativeTransitionJson(from, to, options, cancellation.Token));
        Assert.Throws<NotSupportedException>(() => renderer.PrepareNativeTransitionJson(from, from, options));
        Assert.Throws<NotSupportedException>(() => renderer.PrepareNativeTransitionJson(from, NativeRangeFlashFixture.Section(renderer, false, true, "lower", "markers-off"), options));
        Assert.Throws<ArgumentOutOfRangeException>(() => new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.ValueFlash, 2, ChartColor.Black));
        Assert.Throws<ArgumentOutOfRangeException>(() => new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.ValueFlash, 17, ChartColor.Black));
        NativeRadialMotionBeforeControls.Capture(from, "range-flash-guards-from"); NativeRadialMotionBeforeControls.Capture(to, "range-flash-guards-to");
        NativeRadialMotionBeforeControls.Save("range-flash-guards.facts.json", JsonSerializer.Serialize(new { cancelled = true, noOpRejected = true, changedMarkerPolicyRejected = true, minimumFrames = 3, maximumFrames = 16 }));
    }
}
