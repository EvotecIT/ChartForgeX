using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeBubbleComponentValueTests {
    [Theory]
    [InlineData(false, false, false)] [InlineData(false, true, true)] [InlineData(true, false, true)] [InlineData(true, true, false)]
    public void CompleteNativeMidpointKeepsTypedComponentsScopedIdsLabelsReadoutsAndEndpointExports(bool area, bool reversed, bool labels) {
        var renderer = new HtmlInteractiveChartRenderer(); HtmlChartSection Section(double y, double size) => NativeBubbleComponentBeforeControls.Section(renderer, NativeBubbleComponentBeforeControls.Model(y, size, area, reversed, labels: labels));
        var from = Section(30, 10); var to = Section(50, 70); var middle = Section(ChartMath.InterpolateRange(30, 50, .5), ChartMath.InterpolateRange(10, 70, .5));
        IReadOnlyList<PreparedVisual>? frames = null; string? payload = null;
        var error = Record.Exception(() => { frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null); payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(frameCount: 3)); });
        Capture(from, to, middle, frames, payload, error, "bubble-native-" + area + "-" + reversed + "-" + labels);
        Assert.Null(error); var observed = Assert.IsAssignableFrom<IReadOnlyList<PreparedVisual>>(frames);
        Assert.Equal(middle.PreparedVisual.ToSvg(), observed[1].ToSvg()); Assert.Equal(middle.PreparedVisual.ToSvg("bubble-fixed"), observed[1].ToSvg("bubble-fixed")); Assert.Equal(middle.PreparedVisual.ToPng(), observed[1].ToPng());
        Assert.Equal(to.PreparedVisual.ToSvg(), observed[^1].ToSvg()); Assert.Equal(to.PreparedVisual.ToPng(), observed[^1].ToPng());
        Assert.Equal(middle.PreparedVisual.Accessibility.Description, observed[1].Accessibility.Description);
        Assert.Equal(middle.PreparedVisual.Regions.Select(row => (row.Id, row.Role, row.Label, row.Bounds)), observed[1].Regions.Select(row => (row.Id, row.Role, row.Label, row.Bounds)));
        using var json = JsonDocument.Parse(payload!); var readings = json.RootElement.GetProperty("readings").EnumerateArray().ToArray();
        Assert.Equal(8, readings.Length); Assert.Equal(new[] { "y", "size", "y", "size", "y", "size", "y", "size" }, readings.Select(row => row.GetProperty("component").GetString()));
        Assert.Equal(new[] { "delivery", "delivery", "delivery", "delivery", "comparison", "comparison", "comparison", "comparison" }, readings.Select(row => row.GetProperty("seriesKey").GetString()));
        Assert.Equal(new[] { "current", "current", "later", "later", "current", "current", "later", "later" }, readings.Select(row => row.GetProperty("observationId").GetString()));
        Assert.Throws<NotSupportedException>(() => renderer.PrepareValueTransitionJson(from, to, 3));
    }
    [Theory] [InlineData(false, false)] [InlineData(true, true)]
    public void NativeRawSizeClampingAndExplicitInitialBaselineDoNotInventRadiusValues(bool area, bool reversed) {
        var renderer = new HtmlInteractiveChartRenderer(); HtmlChartSection Section(double size) => NativeBubbleComponentBeforeControls.Section(renderer, NativeBubbleComponentBeforeControls.Model(30, size, area, reversed));
        var from = Section(.25); var to = Section(180); var middle = Section(ChartMath.InterpolateRange(.25, 180, .5));
        var frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null); var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.InitialValues, 3));
        Capture(from, to, middle, frames, payload, null, "bubble-clamp-" + area + "-" + reversed);
        Assert.Equal(middle.PreparedVisual.ToSvg(), frames[1].ToSvg()); Assert.Equal(middle.PreparedVisual.ToPng(), frames[1].ToPng());
        var size = NativeRawValueSourceMotionTests.Metadata(to).GetProperty("nativeValueSource").GetProperty("readings")[1];
        Assert.Equal(180, size.GetProperty("value").GetDouble()); Assert.Equal(100, size.GetProperty("maximum").GetDouble());
        Assert.Equal("180", to.PreparedVisual.Scene.Nodes.OfType<VisualSceneGroup>().Single(row => row.Role == "point" && row.Metadata["data-cfx-series-key"] == "delivery" && row.Metadata["data-cfx-observation-id"] == "current").Metadata["data-cfx-size"]);
    }
    [Fact]
    public void OptInKeepsOrdinaryInkAndDetachedTuplesOptionsAndCapturedFontsSurviveMutation() {
        var renderer = new HtmlInteractiveChartRenderer(); var chart = NativeBubbleComponentBeforeControls.Model(30, 10, labels: true);
        var ordinary = renderer.PrepareSection(chart); var from = NativeBubbleComponentBeforeControls.Section(renderer, chart);
        var to = NativeBubbleComponentBeforeControls.Section(renderer, NativeBubbleComponentBeforeControls.Model(50, 70, labels: true));
        var frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null); var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(frameCount: 3));
        Capture(from, to, ordinary, frames, payload, null, "bubble-detached");
        Assert.Equal(ordinary.PreparedVisual.ToSvg(), from.PreparedVisual.ToSvg()); Assert.Equal(ordinary.PreparedVisual.ToPng(), from.PreparedVisual.ToPng());
        chart.Series[0].UpdateBubble(0, new ChartBubble(1, 90, 500).WithColorValue(99)); chart.ConfigureBubble(options => options.WithSizeDomain(0, 1000)); chart.WithFontFamily("mutated caller");
        try { FontRegistry.Clear(); var repeated = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null);
            Assert.Equal(payload, renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(frameCount: 3)));
            for (var index = 0; index < frames.Count; index++) { Assert.Equal(frames[index].ToSvg(), repeated[index].ToSvg()); Assert.Equal(frames[index].ToPng(), repeated[index].ToPng()); }
        } finally { _ = BrowserProofFonts.Family; }
    }
    [Theory] [InlineData("x")] [InlineData("id")] [InlineData("color")] [InlineData("size-domain")] [InlineData("size-map")] [InlineData("callback")] [InlineData("automatic-size")]
    public void DifferentTupleStructurePresentationOrUncapturablePolicyRejectsBeforePayload(string condition) {
        var renderer = new HtmlInteractiveChartRenderer(); var from = NativeBubbleComponentBeforeControls.Section(renderer, NativeBubbleComponentBeforeControls.Model(30, 10));
        var chart = NativeBubbleComponentBeforeControls.Model(50, 70);
        if (condition == "x") chart.Series[0].UpdateBubble(0, new ChartBubble(2, 50, 70).WithColorValue(7));
        if (condition == "id") chart.Series[0].WithObservationId(0, new ChartObservationId("foreign"));
        if (condition == "color") chart.Series[0].UpdateBubble(0, new ChartBubble(1, 50, 70).WithColorValue(8));
        if (condition == "size-domain") chart.ConfigureBubble(options => options.WithSizeDomain(0, 200));
        if (condition == "size-map") chart.ConfigureBubble(options => options.SizeEncoding = ChartBubbleSizeEncoding.Area);
        if (condition == "callback") chart.ConfigureBubble(options => options.SizeDimension.ValueFormat = ChartValueFormat.Custom(_ => "callback"));
        if (condition == "automatic-size") chart.ConfigureBubble(options => options.UseAutomaticSizeDomain());
        HtmlChartSection? to = null; var error = Record.Exception(() => { to = NativeBubbleComponentBeforeControls.Section(renderer, chart); _ = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(frameCount: 3)); });
        NativeRadialMotionBeforeControls.Capture(from, "bubble-reject-" + condition + "-from"); if (to != null) NativeRadialMotionBeforeControls.Capture(to, "bubble-reject-" + condition + "-to");
        NativeRadialMotionBeforeControls.Save("bubble-reject-" + condition + ".facts.json", JsonSerializer.Serialize(new { condition, error = error?.ToString() }));
        Assert.IsType<NotSupportedException>(error);
    }
    [Fact]
    public void FlashOwnsChangedBubbleInkOnceAndLeavesAcceptedReadingsAndUnchangedPointInkExact() {
        var renderer = new HtmlInteractiveChartRenderer(); var from = NativeBubbleComponentBeforeControls.Section(renderer, NativeBubbleComponentBeforeControls.Model(30, 10)); var to = NativeBubbleComponentBeforeControls.Section(renderer, NativeBubbleComponentBeforeControls.Model(50, 70));
        var frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, ChartColor.Black); var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.ValueFlash, 3, ChartColor.Black));
        Capture(from, to, to, frames, payload, null, "bubble-flash");
        Assert.NotEqual(to.PreparedVisual.ToPng(), frames[1].ToPng()); Assert.Equal(to.PreparedVisual.ToSvg(), frames[2].ToSvg());
        string Ink(PreparedVisual drawing, string series, string observation) => System.Xml.Linq.XDocument.Parse(drawing.ToSvg("bubble-flash-fixed")).Descendants()
            .Single(row => (string?)row.Attribute("data-cfx-role") == "point" && (string?)row.Attribute("data-cfx-series-key") == series && (string?)row.Attribute("data-cfx-observation-id") == observation)
            .ToString(System.Xml.Linq.SaveOptions.DisableFormatting);
        Assert.NotEqual(Ink(to.PreparedVisual, "delivery", "current"), Ink(frames[1], "delivery", "current"));
        Assert.Equal(Ink(to.PreparedVisual, "delivery", "later"), Ink(frames[1], "delivery", "later"));
        Assert.Equal(Ink(to.PreparedVisual, "comparison", "current"), Ink(frames[1], "comparison", "current"));
        Assert.Equal(to.PreparedVisual.Scene.Nodes.OfType<VisualSceneGroup>().Where(row => row.Role == "point").Select(row => row.Metadata), frames[1].Scene.Nodes.OfType<VisualSceneGroup>().Where(row => row.Role == "point").Select(row => row.Metadata));
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void SecondaryValueAxisRetainsItsOwnCapturedYDomain(bool area) {
        var renderer = new HtmlInteractiveChartRenderer();
        HtmlChartSection Section(double value) { var chart = NativeBubbleComponentBeforeControls.Model(value, 35, area);
            chart.Series[0].UseSecondaryValueAxis(); chart.ConfigureSecondaryYAxis(axis => { axis.Minimum = 0; axis.Maximum = 200; });
            return NativeBubbleComponentBeforeControls.Section(renderer, chart); }
        var from = Section(90); var to = Section(130); var middle = Section(ChartMath.InterpolateRange(90, 130, .5));
        var frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null); var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(frameCount: 3));
        Capture(from, to, middle, frames, payload, null, "bubble-secondary-" + area);
        Assert.Equal(middle.PreparedVisual.ToSvg(), frames[1].ToSvg()); Assert.Equal(middle.PreparedVisual.ToPng(), frames[1].ToPng());
        using var json = JsonDocument.Parse(payload); Assert.Equal(200, json.RootElement.GetProperty("readings")[0].GetProperty("maximum").GetDouble());
        Assert.Equal(100, json.RootElement.GetProperty("readings")[4].GetProperty("maximum").GetDouble());
    }
    [Fact]
    public void UnpaintedAuthoredPointPaintStillBindsSourceIdentity() {
        var renderer = new HtmlInteractiveChartRenderer();
        Chart Model(bool changed) { var chart = NativeBubbleComponentBeforeControls.Model(30, 10).WithLegend(false).WithDataLabels(false);
            foreach (var row in chart.Series) row.ConfigureMarkers(markers => markers.Enabled = false);
            if (changed) chart.Series[0].WithPointColor(0, ChartColor.Black); return chart; }
        var from = NativeBubbleComponentBeforeControls.Section(renderer, Model(false)); var to = NativeBubbleComponentBeforeControls.Section(renderer, Model(true));
        NativeRadialMotionBeforeControls.Capture(from, "bubble-unpainted-source-from"); NativeRadialMotionBeforeControls.Capture(to, "bubble-unpainted-source-to");
        var a = NativeRawValueSourceMotionTests.Metadata(from).GetProperty("nativeValueSource"); var b = NativeRawValueSourceMotionTests.Metadata(to).GetProperty("nativeValueSource");
        NativeRadialMotionBeforeControls.Save("bubble-unpainted-source.facts.json", JsonSerializer.Serialize(new { from = a, to = b }));
        Assert.Equal(from.PreparedVisual.ToSvg("bubble-unpainted"), to.PreparedVisual.ToSvg("bubble-unpainted")); Assert.Equal(from.PreparedVisual.ToPng(), to.PreparedVisual.ToPng());
        Assert.NotEqual(a.GetProperty("sourceKey").GetString(), b.GetProperty("sourceKey").GetString()); Assert.NotEqual(a.GetProperty("structureKey").GetString(), b.GetProperty("structureKey").GetString());
    }
    private static void Capture(HtmlChartSection from, HtmlChartSection to, HtmlChartSection middle, IReadOnlyList<PreparedVisual>? frames, string? payload, Exception? error, string name) {
        NativeRadialMotionBeforeControls.Capture(from, name + "-from"); NativeRadialMotionBeforeControls.Capture(to, name + "-to"); NativeRadialMotionBeforeControls.Capture(middle, name + "-independent");
        NativeRadialMotionBeforeControls.Save(name + ".facts.json", JsonSerializer.Serialize(new { error = error?.ToString(), payload }));
        if (frames != null) NativeRadialValueMotionTests.Capture(from, to, frames, payload ?? string.Empty, name);
    }
}
