using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeRadialValueMotionTests {
    [Theory]
    [InlineData(false, "hidden", false)] [InlineData(true, "hidden", true)]
    [InlineData(false, "inside", true)] [InlineData(true, "inside", false)]
    [InlineData(false, "outside", false)] [InlineData(true, "outside", true)]
    public void CompleteFramesEqualIndependentlyPreparedNativeLayoutIncludingCaptionsAndLegend(bool donut, string labels, bool dark) {
        var renderer = new HtmlInteractiveChartRenderer();
        var from = renderer.PrepareSection(NativeRadialMotionBeforeControls.Model(donut, false, labels, 60, dark: dark));
        var to = renderer.PrepareSection(NativeRadialMotionBeforeControls.Model(donut, false, labels, 80, dark: dark));
        var middle = renderer.PrepareSection(NativeRadialMotionBeforeControls.Model(donut, false, labels, 70, dark: dark));
        var frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null);
        var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.Values, 3));
        Capture(from, to, frames, payload, "value-" + donut + "-" + labels + "-" + dark); NativeRadialMotionBeforeControls.Capture(middle, "radial-value-independent-" + donut + "-" + labels);
        Assert.Equal("radial-value", from.PreparedVisual.Scene.ValueCapture!.Plan.Kind);
        Assert.Equal(middle.PreparedVisual.ToSvg(), frames[1].ToSvg()); Assert.Equal(middle.PreparedVisual.ToSvg("fixed-radial-frame"), frames[1].ToSvg("fixed-radial-frame"));
        Assert.Equal(middle.PreparedVisual.ToPng(), frames[1].ToPng()); Assert.Equal(middle.PreparedVisual.Accessibility.Description, frames[1].Accessibility.Description);
        Assert.Equal(middle.PreparedVisual.Regions.Select(row => (row.Id, row.Role, row.Label)), frames[1].Regions.Select(row => (row.Id, row.Role, row.Label)));
        using var json = JsonDocument.Parse(payload); Assert.Equal(3, json.RootElement.GetProperty("readings").GetArrayLength());
        Assert.Equal(to.PreparedVisual.ToSvg(), frames[2].ToSvg()); Assert.Equal(to.PreparedVisual.ToPng(), frames[2].ToPng());
        var legacy = renderer.PrepareValueTransitionJson(from, to, 3); using var single = JsonDocument.Parse(legacy); Assert.Equal("radial-value", single.RootElement.GetProperty("kind").GetString());
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void MultiReadingAndExplicitPositiveInitialBaselineUseTheSameCompletePlan(bool donut) {
        Chart Model(double first, double second) { var chart = NativeRadialMotionBeforeControls.Model(donut, false, "outside", first); chart.Series[0].Points[1] = new ChartPoint(2, second); return chart; }
        var renderer = new HtmlInteractiveChartRenderer(); var from = renderer.PrepareSection(Model(60, 25)); var to = renderer.PrepareSection(Model(80, 35));
        var middle = renderer.PrepareSection(Model(70, 30)); var frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null);
        var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.InitialValues, 3));
        Capture(from, to, frames, payload, "initial-" + donut); NativeRadialMotionBeforeControls.Capture(middle, "radial-initial-independent-" + donut);
        Assert.Equal(middle.PreparedVisual.ToSvg(), frames[1].ToSvg()); Assert.Equal(middle.PreparedVisual.ToPng(), frames[1].ToPng());
        Assert.Throws<NotSupportedException>(() => renderer.PrepareValueTransitionJson(from, to, 3));
    }
    [Fact]
    public void CompleteSourceAndMeasuredFacesSurviveCallerMutationAndRegistryReplacement() {
        var renderer = new HtmlInteractiveChartRenderer(); var chart = NativeRadialMotionBeforeControls.Model(true, false, "outside");
        var from = renderer.PrepareSection(chart); var to = renderer.PrepareSection(NativeRadialMotionBeforeControls.Model(true, false, "outside", 80));
        var frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 5, null); var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.Values, 5));
        Capture(from, to, frames, payload, "detached"); var svg = frames.Select(row => row.ToSvg()).ToArray(); var png = frames.Select(row => row.ToPng()).ToArray();
        chart.Series[0].Points.Clear(); chart.Series[0].WithColor(ChartColor.Black); chart.Options.MaximumPieSlices = 2; chart.Options.DonutCenterValue = "Changed";
        chart.WithFontFamily("Changed caller family").WithValueFormat(ChartValueFormat.Custom(_ => "Changed"));
        try { FontRegistry.Clear(); var repeated = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 5, null);
            Assert.Equal(svg, repeated.Select(row => row.ToSvg())); for (var index = 0; index < repeated.Count; index++) Assert.Equal(png[index], repeated[index].ToPng());
            Assert.Equal(payload, renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.Values, 5)));
        } finally { _ = BrowserProofFonts.Family; }
    }
    [Theory] [InlineData("zero")] [InlineData("negative")] [InlineData("aggregate")] [InlineData("reorder")] [InlineData("identity")] [InlineData("callback")] [InlineData("radius")] [InlineData("flash")]
    public void UnsupportedCompleteValueStructureRejectsWithoutDisablingOrdinaryRendering(string condition) {
        var renderer = new HtmlInteractiveChartRenderer(); var chart = NativeRadialMotionBeforeControls.Model(false, condition == "aggregate");
        var from = renderer.PrepareSection(chart); chart.Series[0].Points[0] = new ChartPoint(1, condition == "zero" ? 0 : condition == "negative" ? -1 : condition == "reorder" ? 20 : 80);
        if (condition == "identity") chart.Series[0].WithObservationId(0, new ChartObservationId("changed"));
        if (condition == "callback") chart.WithValueFormat(ChartValueFormat.Custom(value => "V" + value.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        if (condition == "radius") chart.Series[0].WithPointSliceRadiusRatio(0, 0);
        if (condition == "negative") {
            NativeRadialMotionBeforeControls.Capture(from, "radial-reject-negative-valid-source"); Exception? nativeInvalid = null;
            try { _ = renderer.PrepareSection(chart); } catch (Exception exception) { nativeInvalid = exception; }
            NativeRadialMotionBeforeControls.Save("radial-reject-negative.native-domain.json", JsonSerializer.Serialize(new { value = chart.Series[0].Points[0].Y, error = nativeInvalid?.GetType().Name, message = nativeInvalid?.Message }));
            Assert.IsType<InvalidOperationException>(nativeInvalid); Assert.Contains("non-negative", nativeInvalid!.Message, StringComparison.Ordinal);
            Assert.NotEmpty(from.PreparedVisual.ToSvg()); return;
        }
        var to = renderer.PrepareSection(chart); Exception? failure = null; try { _ = renderer.PrepareNativeTransitionJson(from, to,
            new HtmlNativeTransitionOptions(condition == "flash" ? HtmlNativeTransitionEffect.ValueFlash : HtmlNativeTransitionEffect.Values, 3, condition == "flash" ? ChartColor.Black : null)); } catch (Exception exception) { failure = exception; }
        NativeRadialMotionBeforeControls.Capture(from, "radial-reject-" + condition + "-from"); NativeRadialMotionBeforeControls.Capture(to, "radial-reject-" + condition + "-to");
        NativeRadialMotionBeforeControls.Save("radial-reject-" + condition + ".facts.json", JsonSerializer.Serialize(new { condition, error = failure?.GetType().Name, message = failure?.Message }));
        if (condition == "flash") { Assert.Null(failure); var flash = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, ChartColor.Black); Assert.NotEqual(to.PreparedVisual.ToPng(), flash[1].ToPng()); }
        else Assert.IsType<NotSupportedException>(failure);
        Assert.NotEmpty(to.PreparedVisual.ToSvg());
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void ExtremePositiveTotalsRetainNativeOverflowFactsAndEmptySourcesRemainOrdinary(bool donut) {
        Chart Model(double last) { var chart = NativeRadialMotionBeforeControls.Model(donut, false, "hidden");
            chart.Series[0].Points[0] = new ChartPoint(1, double.MaxValue); chart.Series[0].Points[1] = new ChartPoint(2, double.MaxValue / 2); chart.Series[0].Points[2] = new ChartPoint(3, last); return chart; }
        var renderer = new HtmlInteractiveChartRenderer(); var from = renderer.PrepareSection(Model(double.MaxValue / 4)); var to = renderer.PrepareSection(Model(double.MaxValue / 3));
        var middle = renderer.PrepareSection(Model(ChartMath.InterpolateRange(double.MaxValue / 4, double.MaxValue / 3, .5)));
        var frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null); var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.Values, 3));
        Capture(from, to, frames, payload, "extreme-" + donut); NativeRadialMotionBeforeControls.Capture(middle, "radial-extreme-independent-" + donut);
        var emptyChart = NativeRadialMotionBeforeControls.Model(donut, false, "hidden"); emptyChart.Series[0].Points.Clear(); var empty = renderer.PrepareSection(emptyChart);
        NativeRadialMotionBeforeControls.Capture(empty, "radial-empty-" + donut); Exception? failure = null;
        try { _ = renderer.PrepareNativeTransitionJson(empty, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.InitialValues, 3)); } catch (Exception exception) { failure = exception; }
        NativeRadialMotionBeforeControls.Save("radial-empty-" + donut + ".facts.json", JsonSerializer.Serialize(new { error = failure?.Message }));
        Assert.Equal(middle.PreparedVisual.ToSvg(), frames[1].ToSvg()); Assert.Equal(middle.PreparedVisual.ToPng(), frames[1].ToPng());
        Assert.Contains(from.PreparedVisual.Diagnostics, row => row.Code == "radial.total-overflow"); Assert.Contains(empty.PreparedVisual.Diagnostics, row => row.Code == "radial.no-data"); Assert.IsType<NotSupportedException>(failure);
    }
    internal static void Capture(HtmlChartSection from, HtmlChartSection to, IReadOnlyList<PreparedVisual> frames, string payload, string name) {
        NativeRadialMotionBeforeControls.Capture(from, "radial-" + name + "-from"); NativeRadialMotionBeforeControls.Capture(to, "radial-" + name + "-to");
        NativeRadialMotionBeforeControls.Save("radial-" + name + ".transition.json", payload);
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        for (var index = 0; index < frames.Count; index++) { File.WriteAllText(Path.Combine(output, "radial-" + name + "-frame-" + index + ".svg"), frames[index].ToSvg()); File.WriteAllBytes(Path.Combine(output, "radial-" + name + "-frame-" + index + ".png"), frames[index].ToPng()); }
    }
}
