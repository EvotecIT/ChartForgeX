using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeTransitionCaptureTests {
    [Theory, MemberData(nameof(NativeTransitionDefaultControls.Cases), MemberType = typeof(NativeTransitionDefaultControls))]
    public void SixNativePlansRetainExactEndpointsForValuesExplicitInitialBaselineAndAcceptedValueFlash(string kind, int width, bool dark) {
        var from = NativeTransitionDefaultControls.Section(kind, width, dark, .2); var to = NativeTransitionDefaultControls.Section(kind, width, dark, .8);
        var renderer = new HtmlInteractiveChartRenderer(); var peak = ChartColor.FromHex("#FFD900");
        foreach (var effect in new[] { HtmlNativeTransitionEffect.Values, HtmlNativeTransitionEffect.InitialValues, HtmlNativeTransitionEffect.ValueFlash }) {
            var options = new HtmlNativeTransitionOptions(effect, 3, effect == HtmlNativeTransitionEffect.ValueFlash ? peak : null);
            var payload = renderer.PrepareNativeTransitionJson(from, to, options); using var json = JsonDocument.Parse(payload);
            Capture(kind + "-" + width + "-" + dark + "-" + effect, payload);
            Assert.Equal("native-transition", json.RootElement.GetProperty("kind").GetString()); Assert.Equal(to.Html, json.RootElement.GetProperty("destination").GetString());
            foreach (var drawingName in new[] { "wide", "narrow" }) {
                var drawing = json.RootElement.GetProperty("drawings").GetProperty(drawingName); var frames = drawing.GetProperty("frames");
                Assert.Equal(3, frames.GetArrayLength()); Assert.Equal(dark, drawing.GetProperty("capturePngText").GetBoolean());
                Assert.Contains("data-cfx-series-key", frames[0].GetProperty("svg").GetString(), StringComparison.Ordinal);
                var sourcePrefix = drawing.GetProperty("sourcePrefix").GetString()!; var destinationPrefix = drawing.GetProperty("destinationPrefix").GetString()!;
                Assert.All(frames.EnumerateArray(), frame => { Assert.NotEqual(sourcePrefix, frame.GetProperty("prefix").GetString()); Assert.NotEqual(destinationPrefix, frame.GetProperty("prefix").GetString()); });
            }
            var native = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, options.FlashColor);
            Assert.Same(effect == HtmlNativeTransitionEffect.ValueFlash ? to.PreparedVisual : from.PreparedVisual, native[0]); Assert.Same(to.PreparedVisual, native[2]);
            if (effect != HtmlNativeTransitionEffect.ValueFlash) {
                var expected = NativeTransitionDefaultControls.Section(kind, width, dark, .5).PreparedVisual;
                Assert.Equal(expected.ToSvg("generic-native-middle"), native[1].ToSvg("generic-native-middle")); Assert.Equal(expected.ToPng(), native[1].ToPng());
                NativeValueIdentityProof.AssertEqual(expected, native[1], "generic-" + kind + "-" + effect);
            } else {
                var targetValues = to.PreparedVisual.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Metadata.ContainsKey("data-cfx-value")).Select(group => group.Metadata["data-cfx-value"]).ToArray();
                Assert.Equal(targetValues, native[1].Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Metadata.ContainsKey("data-cfx-value")).Select(group => group.Metadata["data-cfx-value"]));
                Assert.Contains(native[1].Scene.Nodes.OfType<VisualSceneMark>(), mark => (mark.Fill.HasValue && mark.Fill.Value.Equals(peak)) || (mark.Stroke.HasValue && mark.Stroke.Value.Equals(peak)));
                Assert.NotEqual(to.PreparedVisual.ToPng(), native[1].ToPng());
            }
            SaveNative(kind + "-" + width + "-" + dark + "-" + effect, native[1]);
        }
    }
    [Fact]
    public void MultipleProgressReadingsUseRawIndependentDomainsAndOldOneEditApiStillRejectsThem() {
        var renderer = new HtmlInteractiveChartRenderer();
        HtmlChartSection Section(double first, double second) {
            var chart = NativeTransitionDefaultControls.Model("progress", 760, false, first); var row = chart.Series[0].ProgressItems[1];
            chart.Series[0].UpdateProgressItem(1, new ChartProgressItem(row.Label, second, row.Minimum, row.Maximum!.Value, row.Color));
            return renderer.PrepareSection(chart, configure: options => NativeTransitionDefaultControls.Configure(options, false));
        }
        var from = Section(.2, 1000); var to = Section(.8, 2000); var expected = Section(.5, 1500);
        var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(frameCount: 3)); Capture("multi-progress", payload);
        var middle = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null)[1]; SaveNative("multi-progress", middle);
        Assert.Equal(expected.PreparedVisual.ToSvg("multi-progress"), middle.ToSvg("multi-progress")); Assert.Equal(expected.PreparedVisual.ToPng(), middle.ToPng());
        using var json = JsonDocument.Parse(payload); var readings = json.RootElement.GetProperty("readings");
        Assert.Equal(2, readings.EnumerateArray().Count(row => row.GetProperty("fromValue").GetDouble() != row.GetProperty("toValue").GetDouble()));
        Assert.Contains(readings.EnumerateArray(), row => row.GetProperty("maximum").GetDouble() > 1);
        Assert.Throws<NotSupportedException>(() => renderer.PrepareValueTransitionJson(from, to, 3));
    }
    [Fact]
    public void LegendFramesUseCapturedWholeSeriesCompositeAndExactCapturedHtmlExportOptions() {
        var chart = NativeSeriesVisibilityDefaultControls.Model("mixed-labels"); var renderer = new HtmlInteractiveChartRenderer();
        HtmlChartSection Section(bool hidden) => renderer.PrepareVisibilitySection(chart,
            new ChartVisibilityView(hidden ? new[] { new ChartSeriesId("observed") } : null, ChartVisibilityLabelLayering.GroupWithSeries),
            configure: options => NativeTransitionDefaultControls.Configure(options, true));
        var from = Section(false); var to = Section(true); var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.LegendVisibility, 3));
        Capture("legend-native-grouping", payload); using var json = JsonDocument.Parse(payload);
        Assert.Equal("legend-visibility", json.RootElement.GetProperty("effect").GetString()); Assert.Equal("observed", json.RootElement.GetProperty("visibility").GetProperty("toHidden")[0].GetString());
        var middle = VisualSeriesVisibilityCapture.Frames(from.PreparedVisual, to.PreparedVisual, 3)[1]; SaveNative("legend-native-grouping", middle);
        Assert.Contains(middle.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.Role == "series-visibility" && group.Opacity == .5);
        var actual = to.WideEmission!; Assert.Equal(actual.Svg, to.PreparedVisual.ForInteractivePngExport().ToSvg(actual.Options));
        Assert.Equal(actual.Options.IdPrefix, actual.Prefix);
    }
    [Theory, InlineData("gauge-needle"), InlineData("gauge-combined"), InlineData("gauge-vertical"), InlineData("bullet-shared-vertical")]
    public void FlashRetainsFiniteNativeNeedleMarkerAndAlternateBulletAnatomy(string kind) {
        var from = NativeTransitionDefaultControls.Section(kind, 390, true, .2); var to = NativeTransitionDefaultControls.Section(kind, 390, true, .8);
        var peak = ChartColor.FromHex("#FFD900"); var renderer = new HtmlInteractiveChartRenderer();
        var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.ValueFlash, 3, peak));
        var middle = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, peak)[1];
        Capture("alternate-flash-" + kind, payload); SaveNative("alternate-flash-" + kind, middle);
        Assert.Equal(to.PreparedVisual.Scene.Nodes.Count, middle.Scene.Nodes.Count);
        Assert.Contains(middle.Scene.Nodes.OfType<VisualSceneMark>(), mark => (mark.Fill.HasValue && mark.Fill.Value.Equals(peak)) || (mark.Stroke.HasValue && mark.Stroke.Value.Equals(peak)));
        if (kind is "gauge-needle" or "gauge-combined") Assert.Contains(middle.Scene.Nodes.OfType<VisualSceneLine>(), line => line.Role == "gauge-needle" && line.Stroke.HasValue && line.Stroke.Value.Equals(peak));
        Assert.Equal(to.PreparedVisual.Regions.Select(region => (region.Id, region.Role, region.Bounds, region.Label)), middle.Regions.Select(region => (region.Id, region.Role, region.Bounds, region.Label)));
    }
    [Fact]
    public void MultipleBulletReadingsRetainSameObservationIdUnderDistinctOrderedSeriesIdentities() {
        var renderer = new HtmlInteractiveChartRenderer();
        HtmlChartSection Section(double first, double second) {
            var chart = NativeTransitionDefaultControls.Model("bullet-horizontal", 760, false, second / 100); var row = chart.Series[0].BulletValues[0];
            chart.Series[0].UpdateBulletValue(0, new ChartBulletValue(first, row.Target, row.Minimum, row.Maximum, row.RangeEnds));
            return renderer.PrepareSection(chart, configure: options => NativeTransitionDefaultControls.Configure(options, false));
        }
        var from = Section(25, 20); var to = Section(65, 80); var expected = Section(45, 50);
        var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(frameCount: 3)); Capture("multi-series-bullet", payload);
        using var json = JsonDocument.Parse(payload); var readings = json.RootElement.GetProperty("readings").EnumerateArray().ToArray();
        Assert.Equal(new[] { "reviewed", "verified" }, readings.Select(row => row.GetProperty("seriesKey").GetString()));
        Assert.All(readings, row => Assert.Equal("current", row.GetProperty("observationId").GetString()));
        var middle = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null)[1]; SaveNative("multi-series-bullet", middle);
        Assert.Equal(expected.PreparedVisual.ToSvg("multi-series-bullet"), middle.ToSvg("multi-series-bullet")); Assert.Equal(expected.PreparedVisual.ToPng(), middle.ToPng());
        Assert.Throws<NotSupportedException>(() => renderer.PrepareValueTransitionJson(from, to, 3));
    }
    [Fact]
    public void UnsupportedNativeBaselinesPoliciesAndCancelledCaptureRejectWithoutChangingAcceptedFacts() {
        var renderer = new HtmlInteractiveChartRenderer(); var from = NativeTransitionDefaultControls.Section("progress", 760, false, .2); var to = NativeTransitionDefaultControls.Section("progress", 760, false, .8);
        var original = from.Html; using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        Assert.Throws<OperationCanceledException>(() => renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(), cancelled.Token));
        Assert.Throws<NotSupportedException>(() => renderer.PrepareNativeTransitionJson(from, from, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.InitialValues)));
        Assert.Throws<ArgumentException>(() => new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.ValueFlash));
        Assert.Throws<ArgumentOutOfRangeException>(() => new HtmlNativeTransitionOptions(frameCount: 17));
        var line = renderer.PrepareSection(Chart.Create().AddSeries(ChartSeries.CreateLine(new ChartSeriesId("line"), "Line", ChartPoints.FromValues(1, 2, 3))));
        Assert.Throws<NotSupportedException>(() => renderer.PrepareNativeTransitionJson(line, line, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.InitialValues)));
        Assert.Equal(original, from.Html);
    }
    private static void Capture(string name, string payload) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, "native-transition-" + name + ".json"), payload);
    }
    private static void SaveNative(string name, PreparedVisual visual) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, "native-transition-" + name + ".svg"), visual.ToSvg()); File.WriteAllBytes(Path.Combine(output, "native-transition-" + name + ".png"), visual.ToPng());
    }
}
