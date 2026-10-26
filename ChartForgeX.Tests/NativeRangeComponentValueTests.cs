using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeRangeComponentValueTests {
    [Theory] [InlineData(false, false, false)] [InlineData(false, true, true)] [InlineData(true, false, true)] [InlineData(true, true, false)]
    public void RealTypedIntervalFramesEqualIndependentNativeBoundaryCaptionRegionAndExportOwners(bool area, bool labels, bool initial) {
        var renderer = new HtmlInteractiveChartRenderer(); HtmlChartSection Section(double lower, double upper) => NativeRangeComponentBeforeControls.Section(renderer, NativeRangeComponentBeforeControls.Model(lower, upper, area, labels));
        var from = Section(10, 35); var to = Section(20, 60); var middle = Section(ChartMath.InterpolateRange(10, 20, .5), ChartMath.InterpolateRange(35, 60, .5));
        IReadOnlyList<PreparedVisual>? frames = null; string? payload = null; var error = Record.Exception(() => { frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null); payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(initial ? HtmlNativeTransitionEffect.InitialValues : HtmlNativeTransitionEffect.Values, 3)); });
        Capture(from, to, middle, frames, payload, error, "range-native-" + area + "-" + labels + "-" + initial); Assert.Null(error);
        var observed = Assert.IsAssignableFrom<IReadOnlyList<PreparedVisual>>(frames); Assert.Equal(middle.PreparedVisual.ToSvg(), observed[1].ToSvg()); Assert.Equal(middle.PreparedVisual.ToSvg("range-fixed"), observed[1].ToSvg("range-fixed")); Assert.Equal(middle.PreparedVisual.ToPng(), observed[1].ToPng());
        Assert.Equal(middle.PreparedVisual.Regions.Select(row => (row.Id, row.Role, row.Bounds, row.Label)), observed[1].Regions.Select(row => (row.Id, row.Role, row.Bounds, row.Label))); Assert.Equal(middle.PreparedVisual.Accessibility.Description, observed[1].Accessibility.Description);
        Assert.Equal(to.PreparedVisual.ToSvg(), observed[^1].ToSvg()); Assert.Equal(to.PreparedVisual.ToPng(), observed[^1].ToPng());
        var source = NativeRawValueSourceMotionTests.Metadata(from).GetProperty("nativeValueSource"); Assert.Equal("range-components", source.GetProperty("kind").GetString()); Assert.False(source.TryGetProperty("mapping", out _)); Assert.False(source.TryGetProperty("reversed", out _));
        using var json = JsonDocument.Parse(payload!); var readings = json.RootElement.GetProperty("readings"); Assert.Equal(12, readings.GetArrayLength()); Assert.Equal("lower", readings[0].GetProperty("component").GetString()); Assert.Equal("upper", readings[1].GetProperty("component").GetString());
        Assert.Equal("delivery", readings[0].GetProperty("seriesKey").GetString()); Assert.Equal("comparison", readings[6].GetProperty("seriesKey").GetString()); Assert.Equal("current", readings[6].GetProperty("observationId").GetString());
        Assert.Throws<NotSupportedException>(() => renderer.PrepareValueTransitionJson(from, to, 3));
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void SourceAndCapturedIntervalFontsRemainDetachedAndOptInKeepsOrdinaryInk(bool area) {
        var renderer = new HtmlInteractiveChartRenderer(); var chart = NativeRangeComponentBeforeControls.Model(10, 35, area, true); var ordinary = renderer.PrepareSection(chart); var from = NativeRangeComponentBeforeControls.Section(renderer, chart);
        var to = NativeRangeComponentBeforeControls.Section(renderer, NativeRangeComponentBeforeControls.Model(20, 60, area, true)); var frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null); var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(frameCount: 3));
        Capture(from, to, ordinary, frames, payload, null, "range-detached-" + area); Assert.Equal(ordinary.PreparedVisual.ToSvg(), from.PreparedVisual.ToSvg()); Assert.Equal(ordinary.PreparedVisual.ToPng(), from.PreparedVisual.ToPng());
        chart.Series[0].UpdateRange(0, new ChartRangeBand(.5, 1, 99)); chart.Series[0].ReplaceRanges(Array.Empty<ChartRangeBand>()); chart.WithFontFamily("mutated caller");
        try { FontRegistry.Clear(); var repeated = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null); Assert.Equal(payload, renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(frameCount: 3)));
            for (var index = 0; index < frames.Count; index++) { Assert.Equal(frames[index].ToSvg(), repeated[index].ToSvg()); Assert.Equal(frames[index].ToPng(), repeated[index].ToPng()); }
        } finally { _ = BrowserProofFonts.Family; }
    }
    [Theory] [InlineData("x")] [InlineData("break")] [InlineData("id")] [InlineData("auto-axis")] [InlineData("callback")]
    public void DifferentIntervalTopologyOrUnsupportedIntentRejectsBeforePayload(string condition) {
        var renderer = new HtmlInteractiveChartRenderer(); var from = NativeRangeComponentBeforeControls.Section(renderer, NativeRangeComponentBeforeControls.Model(10, 35, true, true)); var chart = NativeRangeComponentBeforeControls.Model(20, 60, true, true);
        if (condition == "x") chart.Series[0].UpdateRange(0, new ChartRangeBand(.75, 20, 60));
        if (condition == "break") chart.Series[0].UpdateRange(0, new ChartRangeBand(.5, 20, 60, true));
        if (condition == "id") chart.Series[0].WithObservationId(0, new ChartObservationId("foreign"));
        if (condition == "auto-axis") chart.ConfigureYAxis(axis => { axis.Minimum = null; axis.Maximum = null; });
        if (condition == "callback") chart.WithValueFormat(ChartValueFormat.Custom(_ => "callback"));
        HtmlChartSection? to = null; var error = Record.Exception(() => { to = NativeRangeComponentBeforeControls.Section(renderer, chart); _ = renderer.PrepareNativeTransitionJson(from, to,
            new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.Values, 3)); });
        NativeRadialMotionBeforeControls.Capture(from, "range-reject-" + condition + "-from"); if (to != null) NativeRadialMotionBeforeControls.Capture(to, "range-reject-" + condition + "-to"); NativeRadialMotionBeforeControls.Save("range-reject-" + condition + ".facts.json", JsonSerializer.Serialize(new { error = error?.ToString() }));
        Assert.IsType<NotSupportedException>(error);
    }
    [Fact]
    public void RealTypedLowerUpperConstraintRemainsAuthoritativeBeforeNativePreparation() {
        var error = Record.Exception(() => new ChartRangeBand(1, 60, 20)); NativeRadialMotionBeforeControls.Save("range-native-invalid.facts.json", JsonSerializer.Serialize(new { error = error?.ToString() })); Assert.IsType<ArgumentOutOfRangeException>(error);
    }
    private static void Capture(HtmlChartSection from, HtmlChartSection to, HtmlChartSection middle, IReadOnlyList<PreparedVisual>? frames, string? payload, Exception? error, string name) {
        NativeRadialMotionBeforeControls.Capture(from, name + "-from"); NativeRadialMotionBeforeControls.Capture(to, name + "-to"); NativeRadialMotionBeforeControls.Capture(middle, name + "-independent"); NativeRadialMotionBeforeControls.Save(name + ".facts.json", JsonSerializer.Serialize(new { payload, error = error?.ToString() }));
        if (frames != null) NativeRadialValueMotionTests.Capture(from, to, frames, payload ?? string.Empty, name);
    }
}
