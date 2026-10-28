using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeComponentClipValueTests {
    [Theory] [InlineData("bubble", false)] [InlineData("range", false)] [InlineData("error", false)] [InlineData("candlestick", false)] [InlineData("ohlc", false)] [InlineData("bubble", true)] [InlineData("candlestick", true)]
    public void RawValuesEnteringAndLeavingNativeClipEqualIndependentCompleteFramesAndRealRegions(string family, bool leaving) {
        var renderer = new HtmlInteractiveChartRenderer(); var start = leaving ? 40d : 120d; var finish = leaving ? 120d : 40d; var from = NativeComponentClipBeforeControls.Section(renderer, NativeComponentClipBeforeControls.Model(family, start)); var to = NativeComponentClipBeforeControls.Section(renderer, NativeComponentClipBeforeControls.Model(family, finish)); var middle = NativeComponentClipBeforeControls.Section(renderer, NativeComponentClipBeforeControls.Model(family, ChartMath.InterpolateRange(start, finish, .5)));
        IReadOnlyList<PreparedVisual>? frames = null; string? payload = null; var error = Record.Exception(() => { frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null); payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(leaving ? HtmlNativeTransitionEffect.InitialValues : HtmlNativeTransitionEffect.Values, 3)); }); Capture(from, to, middle, frames, payload, error, "clip-native-" + family + "-" + leaving); Assert.Null(error); var actual = Assert.IsAssignableFrom<IReadOnlyList<PreparedVisual>>(frames);
        Assert.Equal(middle.PreparedVisual.ToSvg(), actual[1].ToSvg()); Assert.Equal(middle.PreparedVisual.ToSvg("clip-fixed"), actual[1].ToSvg("clip-fixed")); Assert.Equal(middle.PreparedVisual.ToPng(), actual[1].ToPng()); Assert.Equal(to.PreparedVisual.ToSvg(), actual[^1].ToSvg()); Assert.Equal(to.PreparedVisual.ToPng(), actual[^1].ToPng());
        foreach (var frame in actual) { Assert.True(VisualSeriesVisibilityCapture.SameAxis(from.PreparedVisual.XProjection, frame.XProjection)); Assert.True(VisualSeriesVisibilityCapture.SameAxis(from.PreparedVisual.YProjection, frame.YProjection)); }
        Assert.Equal(middle.PreparedVisual.Regions.Select(row => (row.Id, row.Role, row.Bounds, row.Label)), actual[1].Regions.Select(row => (row.Id, row.Role, row.Bounds, row.Label))); Assert.Equal(middle.PreparedVisual.Accessibility.Description, actual[1].Accessibility.Description); Assert.NotEqual(from.PreparedVisual.ToPng(), to.PreparedVisual.ToPng());
    }
    [Theory] [InlineData("bubble")] [InlineData("range")] [InlineData("error")] [InlineData("candlestick")] [InlineData("ohlc")]
    public void EnabledNativeCaptionsAndReadoutsFollowClippedRawMembersWithoutStaleText(string family) {
        var renderer = new HtmlInteractiveChartRenderer(); var from = NativeComponentClipBeforeControls.Section(renderer, NativeComponentClipBeforeControls.Model(family, 120).WithDataLabels(true)); var to = NativeComponentClipBeforeControls.Section(renderer, NativeComponentClipBeforeControls.Model(family, 40).WithDataLabels(true)); var middle = NativeComponentClipBeforeControls.Section(renderer, NativeComponentClipBeforeControls.Model(family, ChartMath.InterpolateRange(120, 40, .5)).WithDataLabels(true));
        var frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null); var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(frameCount: 3)); Capture(from, to, middle, frames, payload, null, "clip-captions-" + family); Assert.Equal(middle.PreparedVisual.ToSvg(), frames[1].ToSvg()); Assert.Equal(middle.PreparedVisual.ToPng(), frames[1].ToPng()); Assert.Equal(middle.PreparedVisual.Accessibility.Description, frames[1].Accessibility.Description); Assert.Equal(to.PreparedVisual.ToSvg(), frames[^1].ToSvg());
    }
    [Fact]
    public void NativeClippedXBoundsKeepTrueEstimateAndOnlyActualRawErrorMembers() {
        var renderer = new HtmlInteractiveChartRenderer(); var from = NativeComponentClipBeforeControls.Section(renderer, NativeComponentClipBeforeControls.ErrorX(-1, 2)); var to = NativeComponentClipBeforeControls.Section(renderer, NativeComponentClipBeforeControls.ErrorX(.5, 1.5)); var middle = NativeComponentClipBeforeControls.Section(renderer, NativeComponentClipBeforeControls.ErrorX(ChartMath.InterpolateRange(-1, .5, .5), ChartMath.InterpolateRange(2, 1.5, .5)));
        var frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null); var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(frameCount: 3)); Capture(from, to, middle, frames, payload, null, "clip-error-x"); Assert.Equal(middle.PreparedVisual.ToSvg(), frames[1].ToSvg()); Assert.Equal(middle.PreparedVisual.ToPng(), frames[1].ToPng()); Assert.Equal(to.PreparedVisual.ToSvg(), frames[^1].ToSvg());
        var source = NativeRawValueSourceMotionTests.Metadata(from).GetProperty("nativeValueSource").GetProperty("readings"); Assert.Equal(-1, source[1].GetProperty("value").GetDouble()); Assert.Equal(0, source[1].GetProperty("minimum").GetDouble());
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void CorrectLogSignBeyondDisplayDomainUsesRealNativeProjectionAndExactEndpoint(bool negative) {
        var renderer = new HtmlInteractiveChartRenderer(); var start = negative ? -150d : 150d; var finish = negative ? -50d : 50d; var from = NativeComponentClipBeforeControls.Section(renderer, Log(negative, start)); var to = NativeComponentClipBeforeControls.Section(renderer, Log(negative, finish)); var middle = NativeComponentClipBeforeControls.Section(renderer, Log(negative, ChartMath.InterpolateRange(start, finish, .5)));
        var frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null); var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(frameCount: 3)); Capture(from, to, middle, frames, payload, null, "clip-log-" + negative); Assert.Equal(middle.PreparedVisual.ToSvg(), frames[1].ToSvg()); Assert.Equal(middle.PreparedVisual.ToPng(), frames[1].ToPng()); Assert.Equal(to.PreparedVisual.ToSvg(), frames[^1].ToSvg());
    }
    [Theory] [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public void LogIntrinsicZeroAndWrongSignRejectSeparatelyFromDisplayDomainClipping(bool negative, bool wrongSign) {
        var renderer = new HtmlInteractiveChartRenderer(); var section = NativeComponentClipBeforeControls.Section(renderer, Log(negative, negative ? -150 : 150)); var capture = section.PreparedVisual.Scene.ValueCapture!; var values = capture.Plan.Readings.Select(row => row.Value).ToArray(); values[0] = wrongSign ? negative ? 1 : -1 : 0;
        var error = Record.Exception(() => capture.Frame(section.PreparedVisual, values)); NativeRadialMotionBeforeControls.Capture(section, "clip-log-invalid-" + negative + "-" + wrongSign); NativeRadialMotionBeforeControls.Save("clip-log-invalid-" + negative + "-" + wrongSign + ".facts.json", JsonSerializer.Serialize(new { values, error = error?.ToString() })); Assert.IsType<NotSupportedException>(error);
    }
    [Fact]
    public void HiddenClippedRawValuesCannotMasqueradeAsAnEqualFullSourceDespiteIdenticalPng() {
        var renderer = new HtmlInteractiveChartRenderer(); var first = NativeComponentClipBeforeControls.Section(renderer, NativeComponentClipBeforeControls.Model("bubble", 120)); var second = NativeComponentClipBeforeControls.Section(renderer, NativeComponentClipBeforeControls.Model("bubble", 140)); Capture(first, second, first, null, null, null, "clip-hidden-raw-source"); Assert.Equal(first.PreparedVisual.ToPng(), second.PreparedVisual.ToPng());
        var a = NativeRawValueSourceMotionTests.Metadata(first).GetProperty("nativeValueSource"); var b = NativeRawValueSourceMotionTests.Metadata(second).GetProperty("nativeValueSource"); Assert.Equal(a.GetProperty("structureKey").GetString(), b.GetProperty("structureKey").GetString()); Assert.NotEqual(a.GetProperty("sourceKey").GetString(), b.GetProperty("sourceKey").GetString()); Assert.Equal(120, a.GetProperty("readings")[0].GetProperty("value").GetDouble()); Assert.Equal(140, b.GetProperty("readings")[0].GetProperty("value").GetDouble());
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void RawNativeClipAndFixedTemporalCategoryComposeThroughTheSameCompleteFrameOwner(bool calendar) {
        Chart Model(double value) {
            var chart = NativeComponentClipBeforeControls.Model("candlestick", value); var dates = new[] { new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc), new DateTime(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc) };
            for (var itemIndex = 0; itemIndex < dates.Length; itemIndex++) { var item = chart.Series[0].FinancialValues[itemIndex]; chart.Series[0].UpdateFinancialValue(itemIndex, new ChartCandlestick(dates[itemIndex], item.Open, item.High, item.Low, item.Close)); }
            chart.ConfigureXAxis(axis => { axis.Minimum = null; axis.Maximum = null; axis.WithTimeScale(TimeZoneInfo.Utc); axis.WithBounds(new DateTime(2026, 1, 1).ToOADate(), new DateTime(2026, 4, 1).ToOADate()); if (calendar) axis.WithTimeSpacing(ChartTimeSpacing.Unit(new ChartTimeTickInterval(ChartTimeTickUnit.Month))); }); return chart;
        }
        var renderer = new HtmlInteractiveChartRenderer(); var from = NativeComponentClipBeforeControls.Section(renderer, Model(120)); var to = NativeComponentClipBeforeControls.Section(renderer, Model(40)); var middle = NativeComponentClipBeforeControls.Section(renderer, Model(ChartMath.InterpolateRange(120, 40, .5))); var frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null); var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(frameCount: 3)); Capture(from, to, middle, frames, payload, null, "clip-time-composed-" + calendar); Assert.Equal(middle.PreparedVisual.ToSvg(), frames[1].ToSvg()); Assert.Equal(middle.PreparedVisual.ToPng(), frames[1].ToPng()); Assert.True(VisualSeriesVisibilityCapture.SameAxis(from.PreparedVisual.XProjection, frames[1].XProjection)); Assert.Equal(NativeComponentTimeValueTests.Coordinates(from.PreparedVisual), NativeComponentTimeValueTests.Coordinates(frames[1]));
    }
    [Theory] [InlineData("none")] [InlineData("configured-null")] [InlineData("override")]
    public void NativeFinancialSparsePolicyShapeRemainsCanonicalAcrossCompleteFrames(string policy) {
        Chart Model(double value) { var chart = NativeComponentClipBeforeControls.Model("candlestick", value); if (policy != "none") chart.Series[0].ConfigureFinancialValue(0, options => options.StrokeWidth = policy == "override" ? 4 : null); return chart; }
        var renderer = new HtmlInteractiveChartRenderer(); var from = NativeComponentClipBeforeControls.Section(renderer, Model(40)); var to = NativeComponentClipBeforeControls.Section(renderer, Model(60)); var middle = NativeComponentClipBeforeControls.Section(renderer, Model(ChartMath.InterpolateRange(40, 60, .5))); var frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null); var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(frameCount: 3)); Capture(from, to, middle, frames, payload, null, "financial-sparse-shape-" + policy);
        using var json = JsonDocument.Parse(payload); var expected = NativeRawValueSourceMotionTests.Metadata(from).GetProperty("nativeValueSource").GetProperty("structureKey").GetString(); foreach (var frame in json.RootElement.GetProperty("drawings").GetProperty("wide").GetProperty("frames").EnumerateArray()) Assert.Equal(expected, frame.GetProperty("nativeValueSource").GetProperty("structureKey").GetString()); Assert.Equal(middle.PreparedVisual.ToSvg(), frames[1].ToSvg()); Assert.Equal(middle.PreparedVisual.ToPng(), frames[1].ToPng()); Assert.Equal(to.PreparedVisual.ToSvg(), frames[^1].ToSvg());
    }
    internal static Chart Log(bool negative, double value) {
        var chart = NativeComponentClipBeforeControls.Model("bubble", value); chart.Series[0].UpdateBubble(1, new ChartBubble(3, negative ? -45 : 45, 30));
        chart.ConfigureYAxis(axis => { axis.Minimum = null; axis.Maximum = null; axis.LogarithmicDomain = negative ? ChartLogarithmicDomain.Negative : ChartLogarithmicDomain.Positive; axis.Scale = ChartScaleKind.Logarithmic; axis.WithBounds(negative ? -100 : 1, negative ? -1 : 100); }); return chart;
    }
    private static void Capture(HtmlChartSection from, HtmlChartSection to, HtmlChartSection middle, IReadOnlyList<PreparedVisual>? frames, string? payload, Exception? error, string name) {
        NativeRadialMotionBeforeControls.Capture(from, name + "-from"); NativeRadialMotionBeforeControls.Capture(to, name + "-to"); NativeRadialMotionBeforeControls.Capture(middle, name + "-independent"); NativeRadialMotionBeforeControls.Save(name + ".facts.json", JsonSerializer.Serialize(new { payload, error = error?.ToString() })); if (frames != null) NativeRadialValueMotionTests.Capture(from, to, frames, payload ?? string.Empty, name);
    }
}
