using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeFinancialComponentValueTests {
    [Theory] [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public void ActualPricesDirectionChangesAndDojiEqualIndependentNativeGeometryPaintLabelsAndRegions(bool ohlc, bool initial) {
        var renderer = new HtmlInteractiveChartRenderer(); var from = Section(renderer, ohlc, initial); var to = Section(renderer, ohlc, initial, true);
        var middle = NativeFinancialComponentBeforeControls.Section(renderer, NativeFinancialComponentBeforeControls.Model(ohlc, initial, ChartMath.InterpolateRange(40, 60, .5), ChartMath.InterpolateRange(70, 80, .5), ChartMath.InterpolateRange(20, 10, .5), ChartMath.InterpolateRange(60, 40, .5)));
        IReadOnlyList<PreparedVisual>? frames = null; string? payload = null; var error = Record.Exception(() => { frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null); payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(initial ? HtmlNativeTransitionEffect.InitialValues : HtmlNativeTransitionEffect.Values, 3)); });
        Capture(from, to, middle, frames, payload, error, "financial-native-" + ohlc + "-" + initial); Assert.Null(error); var observed = Assert.IsAssignableFrom<IReadOnlyList<PreparedVisual>>(frames);
        Assert.Equal(middle.PreparedVisual.ToSvg(), observed[1].ToSvg()); Assert.Equal(middle.PreparedVisual.ToSvg("financial-fixed"), observed[1].ToSvg("financial-fixed")); Assert.Equal(middle.PreparedVisual.ToPng(), observed[1].ToPng());
        Assert.Equal(middle.PreparedVisual.Regions.Select(row => (row.Id, row.Role, row.Bounds, row.Label)), observed[1].Regions.Select(row => (row.Id, row.Role, row.Bounds, row.Label))); Assert.Equal(middle.PreparedVisual.Accessibility.Description, observed[1].Accessibility.Description);
        Assert.Equal(to.PreparedVisual.ToSvg(), observed[^1].ToSvg()); Assert.Equal(to.PreparedVisual.ToPng(), observed[^1].ToPng()); Assert.Equal(from.PreparedVisual.ToSvg(), observed[0].ToSvg());
        var metadata = NativeRawValueSourceMotionTests.Metadata(from).GetProperty("nativeValueSource"); Assert.Equal("financial-components", metadata.GetProperty("kind").GetString()); Assert.False(metadata.TryGetProperty("mapping", out _));
        using var json = JsonDocument.Parse(payload!); var readings = json.RootElement.GetProperty("readings").EnumerateArray().ToArray(); Assert.Equal(16, readings.Length); Assert.Equal(new[] { "open", "high", "low", "close" }, readings.Take(4).Select(row => row.GetProperty("component").GetString()));
        Assert.Equal("delivery", readings[0].GetProperty("seriesKey").GetString()); Assert.Equal("comparison", readings[8].GetProperty("seriesKey").GetString()); Assert.Equal("current", readings[0].GetProperty("observationId").GetString()); Assert.Equal(100, readings[0].GetProperty("maximum").GetDouble());
        Assert.Throws<NotSupportedException>(() => renderer.PrepareValueTransitionJson(from, to, 3));
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void ChangedFinancialPointFlashKeepsOtherBodiesWicksAndAllLabelsExact(bool ohlc) {
        var renderer = new HtmlInteractiveChartRenderer(); var from = Section(renderer, ohlc, true); var to = Section(renderer, ohlc, true, true); var frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, ChartColor.Black);
        var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.ValueFlash, 3, ChartColor.Black)); Capture(from, to, to, frames, payload, null, "financial-flash-" + ohlc);
        string Point(PreparedVisual drawing, string series, string id) => XDocument.Parse(drawing.ToSvg("financial-flash-fixed")).Descendants().Single(row => (string?)row.Attribute("data-cfx-role") == "point" && (string?)row.Attribute("data-cfx-series-key") == series && (string?)row.Attribute("data-cfx-observation-id") == id).ToString(SaveOptions.DisableFormatting);
        Assert.NotEqual(Point(to.PreparedVisual, "delivery", "current"), Point(frames[1], "delivery", "current")); Assert.Equal(Point(to.PreparedVisual, "delivery", "later"), Point(frames[1], "delivery", "later")); Assert.Equal(Point(to.PreparedVisual, "comparison", "current"), Point(frames[1], "comparison", "current"));
        string[] Text(PreparedVisual drawing) => XDocument.Parse(drawing.ToSvg("financial-flash-fixed")).Descendants().Where(row => row.Name.LocalName == "text").Select(row => row.ToString(SaveOptions.DisableFormatting)).ToArray();
        Assert.NotEmpty(Text(to.PreparedVisual)); Assert.Equal(Text(to.PreparedVisual), Text(frames[1])); Assert.Equal(to.PreparedVisual.Regions.Select(row => (row.Id, row.Role, row.Bounds, row.Label)), frames[1].Regions.Select(row => (row.Id, row.Role, row.Bounds, row.Label))); Assert.NotEqual(to.PreparedVisual.ToPng(), frames[1].ToPng()); Assert.Equal(to.PreparedVisual.ToSvg(), frames[^1].ToSvg());
    }
    [Theory] [InlineData("kind")] [InlineData("x")] [InlineData("id")] [InlineData("direction-style")] [InlineData("datum-style")] [InlineData("auto-axis")] [InlineData("callback")]
    public void ForeignTupleStructurePolicyOrUncapturedSourceRejectsBeforeNativePayload(string condition) {
        var renderer = new HtmlInteractiveChartRenderer(); var from = Section(renderer, false, true); var chart = NativeFinancialComponentBeforeControls.Model(condition == "kind", true, 60, 80, 10, 40);
        if (condition == "x") chart.Series[0].UpdateFinancialValue(0, new ChartCandlestick(1.25, 60, 80, 10, 40));
        if (condition == "id") chart.Series[0].WithObservationId(0, new ChartObservationId("foreign"));
        if (condition == "direction-style") chart.Series[0].ConfigureFinancial(options => options.Falling.StrokeWidth = 9);
        if (condition == "datum-style") chart.Series[0].ConfigureFinancialValue(0, options => options.StrokeWidth = 9);
        if (condition == "auto-axis") chart.ConfigureXAxis(axis => { axis.Minimum = null; axis.Maximum = null; });
        if (condition == "callback") chart.WithValueFormat(ChartValueFormat.Custom(_ => "callback"));
        HtmlChartSection? to = null; var error = Record.Exception(() => { to = NativeFinancialComponentBeforeControls.Section(renderer, chart); _ = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(frameCount: 3)); });
        Capture(from, to ?? from, from, null, null, error, "financial-reject-" + condition); Assert.IsType<NotSupportedException>(error);
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void CapturedBothDirectionAndDatumPoliciesAndFacesStayDetachedAfterCallerMutation(bool ohlc) {
        var renderer = new HtmlInteractiveChartRenderer(); var chart = NativeFinancialComponentBeforeControls.Model(ohlc, true); var ordinary = renderer.PrepareSection(chart); var from = NativeFinancialComponentBeforeControls.Section(renderer, chart); var to = Section(renderer, ohlc, true, true);
        var frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null); var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(frameCount: 3)); Capture(from, to, ordinary, frames, payload, null, "financial-detached-" + ohlc);
        Assert.Equal(ordinary.PreparedVisual.ToSvg(), from.PreparedVisual.ToSvg()); Assert.Equal(ordinary.PreparedVisual.ToPng(), from.PreparedVisual.ToPng()); chart.Series[0].ConfigureFinancial(options => options.Falling.StrokeWidth = 20); chart.Series[0].ConfigureFinancialValue(0, options => options.Stroke = ChartColor.Black); chart.Series[0].ReplaceFinancialValues(Array.Empty<ChartCandlestick>()); chart.WithFontFamily("mutated caller");
        try { FontRegistry.Clear(); var repeated = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null); Assert.Equal(payload, renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(frameCount: 3))); for (var index = 0; index < frames.Count; index++) { Assert.Equal(frames[index].ToSvg(), repeated[index].ToSvg()); Assert.Equal(frames[index].ToPng(), repeated[index].ToPng()); } } finally { _ = BrowserProofFonts.Family; }
    }
    [Theory] [InlineData("high")] [InlineData("low")] [InlineData("nan")]
    public void NativeFinancialTupleGuardsRejectInvalidPricesBeforeAnyObservation(string condition) {
        var error = Record.Exception(() => _ = condition == "high" ? new ChartCandlestick(1, 40, 30, 20, 60) : condition == "low" ? new ChartCandlestick(1, 40, 70, 50, 60) : new ChartCandlestick(1, double.NaN, 70, 20, 60));
        NativeRadialMotionBeforeControls.Save("financial-invalid-" + condition + ".facts.json", JsonSerializer.Serialize(new { error = error?.ToString() })); Assert.IsType<ArgumentOutOfRangeException>(error);
    }
    [Fact]
    public void UnusedFinancialDirectionPolicyBindsSourceIdentityEvenWhenNativeSvgIsUnchanged() {
        var renderer = new HtmlInteractiveChartRenderer(); var firstChart = NativeFinancialComponentBeforeControls.Model(false, false).WithLegend(false); var secondChart = NativeFinancialComponentBeforeControls.Model(false, false).WithLegend(false);
        foreach (var chart in new[] { firstChart, secondChart }) foreach (var series in chart.Series) series.UpdateFinancialValue(1, new ChartCandlestick(3, 40, 70, 20, 60));
        secondChart.Series[0].ConfigureFinancial(options => options.Falling.StrokeWidth = 9);
        var first = NativeFinancialComponentBeforeControls.Section(renderer, firstChart); var second = NativeFinancialComponentBeforeControls.Section(renderer, secondChart);
        Capture(first, second, first, null, null, null, "financial-unused-direction"); Assert.Equal(first.PreparedVisual.ToSvg(), second.PreparedVisual.ToSvg()); Assert.Equal(first.PreparedVisual.ToPng(), second.PreparedVisual.ToPng());
        var firstSource = NativeRawValueSourceMotionTests.Metadata(first).GetProperty("nativeValueSource"); var secondSource = NativeRawValueSourceMotionTests.Metadata(second).GetProperty("nativeValueSource");
        Assert.NotEqual(firstSource.GetProperty("sourceKey").GetString(), secondSource.GetProperty("sourceKey").GetString()); Assert.NotEqual(firstSource.GetProperty("structureKey").GetString(), secondSource.GetProperty("structureKey").GetString());
        Assert.Throws<NotSupportedException>(() => renderer.PrepareNativeTransitionJson(first, second, new HtmlNativeTransitionOptions(frameCount: 3)));
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void OptionalFinancialCaptureRetainsNativeValidClippedPricesAndExactCompleteFrames(bool ohlc) {
        var renderer = new HtmlInteractiveChartRenderer(); var chart = NativeFinancialComponentBeforeControls.Model(ohlc, true, 60, 120, 10, 40); var targetChart = NativeFinancialComponentBeforeControls.Model(ohlc, true, 60, 80, 10, 40); var ordinary = renderer.PrepareSection(chart); var ordinaryTarget = renderer.PrepareSection(targetChart); var middle = renderer.PrepareSection(NativeFinancialComponentBeforeControls.Model(ohlc, true, 60, ChartMath.InterpolateRange(120, 80, .5), 10, 40));
        HtmlChartSection? captured = null, target = null; IReadOnlyList<PreparedVisual>? frames = null;
        var error = Record.Exception(() => { captured = NativeFinancialComponentBeforeControls.Section(renderer, chart); target = NativeFinancialComponentBeforeControls.Section(renderer, targetChart); frames = VisualValueCapture.NativeFrames(captured.PreparedVisual, target.PreparedVisual, 3, null); }); Capture(ordinary, target ?? ordinaryTarget, middle, frames, null, error, "financial-clipped-" + ohlc); if (captured != null) NativeRadialMotionBeforeControls.Capture(captured, "financial-clipped-captured-" + ohlc);
        Assert.Null(error); Assert.NotNull(captured); var actual = Assert.IsAssignableFrom<IReadOnlyList<PreparedVisual>>(frames); Assert.Equal(ordinary.PreparedVisual.ToSvg(), captured!.PreparedVisual.ToSvg()); Assert.Equal(ordinary.PreparedVisual.ToPng(), captured.PreparedVisual.ToPng()); Assert.Equal(middle.PreparedVisual.ToSvg(), actual[1].ToSvg()); Assert.Equal(middle.PreparedVisual.ToPng(), actual[1].ToPng()); Assert.Equal(ordinaryTarget.PreparedVisual.ToSvg(), actual[^1].ToSvg()); Assert.False(NativeRawValueSourceMotionTests.Metadata(ordinary).TryGetProperty("nativeValueSource", out _));
    }
    private static HtmlChartSection Section(HtmlInteractiveChartRenderer renderer, bool ohlc, bool labels, bool finish = false) => NativeFinancialComponentBeforeControls.Section(renderer, finish ? NativeFinancialComponentBeforeControls.Model(ohlc, labels, 60, 80, 10, 40) : NativeFinancialComponentBeforeControls.Model(ohlc, labels));
    private static void Capture(HtmlChartSection from, HtmlChartSection to, HtmlChartSection middle, IReadOnlyList<PreparedVisual>? frames, string? payload, Exception? error, string name) {
        NativeRadialMotionBeforeControls.Capture(from, name + "-from"); NativeRadialMotionBeforeControls.Capture(to, name + "-to"); NativeRadialMotionBeforeControls.Capture(middle, name + "-independent"); NativeRadialMotionBeforeControls.Save(name + ".facts.json", JsonSerializer.Serialize(new { payload, error = error?.ToString() })); if (frames != null) NativeRadialValueMotionTests.Capture(from, to, frames, payload ?? string.Empty, name);
    }
}
