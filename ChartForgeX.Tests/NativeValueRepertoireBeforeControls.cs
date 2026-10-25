using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Run only after exact captured-fallback18 and before repertoire expansion. No new repertoire API is used.</summary>
[Collection(nameof(FontRegistryCollection))]
public sealed class NativeValueRepertoireBeforeControls : IDisposable {
    [Theory] [InlineData(false, false)] [InlineData(true, false)] [InlineData(false, true)] [InlineData(true, true)]
    public void RealInvariantNativeSuffixChangesCanUseTheSameCompleteValueApi(bool donut, bool automatic) => Observe(donut, automatic ? "automatic" : "compact", automatic ? 9999 : 900, automatic ? 10001 : 1100);
    [Theory] [InlineData(false, false)] [InlineData(true, false)] [InlineData(false, true)] [InlineData(true, true)]
    public void ExistingStableFallbackAndPrimaryOnlyNumericControlsRemainExact(bool donut, bool primaryOnly) => Observe(donut, primaryOnly ? "number" : "compact", primaryOnly ? 900 : 1100, primaryOnly ? 1100 : 1300);
    private static void Observe(bool donut, string policy, double start, double finish) {
        Register(); var renderer = new HtmlInteractiveChartRenderer(); var from = renderer.PrepareSection(Model(donut, policy, start)); var to = renderer.PrepareSection(Model(donut, policy, finish));
        var middle = renderer.PrepareSection(Model(donut, policy, ChartMath.InterpolateRange(start, finish, .5))); var name = "repertoire-before-" + donut + "-" + policy + "-" + start;
        NativeRadialMotionBeforeControls.Capture(from, name + "-from"); NativeRadialMotionBeforeControls.Capture(to, name + "-to"); NativeRadialMotionBeforeControls.Capture(middle, name + "-native-middle");
        string? payload = null; Exception? failure = null; try { payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.Values, 3)); } catch (Exception exception) { failure = exception; }
        NativeRadialMotionBeforeControls.Save(name + ".capture.json", JsonSerializer.Serialize(new { payload, error = failure?.GetType().Name, message = failure?.Message }));
        Assert.Null(failure); Assert.NotNull(payload); var frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null);
        Assert.Equal(middle.PreparedVisual.ToSvg(), frames[1].ToSvg()); Assert.Equal(middle.PreparedVisual.ToPng(), frames[1].ToPng());
    }
    internal static void Register() {
        FontRegistry.Register(NativeFallbackClusterBeforeControls.Primary, NativeFallbackClusterBeforeControls.PrimaryBytes());
        FontRegistry.Register(NativeFallbackClusterBeforeControls.Fallback, OpenTypeTestFonts.NameKeyed(extraGlyphs: ".,kMBE+- >%".Distinct().ToDictionary(character => (int)character, _ => OpenTypeTestFonts.Box)));
    }
    internal static Chart Model(bool donut, string policy, double value) {
        var chart = NativeFallbackClusterBeforeControls.Model(donut, false, value).WithPieSliceLabelContent(ChartPieSliceLabelContent.Value);
        if (policy == "compact") chart.WithValueFormat(ChartValueFormat.Compact()); else if (policy == "number") chart.WithValueFormat(ChartValueFormat.Number("0"));
        return chart;
    }
    public void Dispose() { FontRegistry.Clear(); _ = BrowserProofFonts.Family; }
}
