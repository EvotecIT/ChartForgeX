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
public sealed class NativeErrorComponentValueTests {
    [Theory] [InlineData("x", false)] [InlineData("x", true)] [InlineData("y", false)] [InlineData("y", true)] [InlineData("xy", false)] [InlineData("xy", true)]
    public void EstimateAndRealNullableBoundsEqualIndependentNativeWhiskerCapCaptionAndRegionFrames(string form, bool initial) {
        var renderer = new HtmlInteractiveChartRenderer(); var from = Section(renderer, form, initial); var to = Section(renderer, form, initial, true);
        var middle = NativeErrorComponentBeforeControls.Section(renderer, NativeErrorComponentBeforeControls.Model(form, initial, ChartMath.InterpolateRange(30, 50, .5), ChartMath.InterpolateRange(.5, .25, .5), ChartMath.InterpolateRange(1.5, 1.75, .5), ChartMath.InterpolateRange(20, 40, .5), ChartMath.InterpolateRange(40, 60, .5)));
        IReadOnlyList<PreparedVisual>? frames = null; string? payload = null; var error = Record.Exception(() => { frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null); payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(initial ? HtmlNativeTransitionEffect.InitialValues : HtmlNativeTransitionEffect.Values, 3)); });
        Capture(from, to, middle, frames, payload, error, "error-native-" + form + "-" + initial); Assert.Null(error); var observed = Assert.IsAssignableFrom<IReadOnlyList<PreparedVisual>>(frames);
        Assert.Equal(middle.PreparedVisual.ToSvg(), observed[1].ToSvg()); Assert.Equal(middle.PreparedVisual.ToSvg("error-fixed"), observed[1].ToSvg("error-fixed")); Assert.Equal(middle.PreparedVisual.ToPng(), observed[1].ToPng());
        Assert.Equal(middle.PreparedVisual.Regions.Select(row => (row.Id, row.Role, row.Bounds, row.Label)), observed[1].Regions.Select(row => (row.Id, row.Role, row.Bounds, row.Label))); Assert.Equal(middle.PreparedVisual.Accessibility.Description, observed[1].Accessibility.Description);
        Assert.Equal(to.PreparedVisual.ToSvg(), observed[^1].ToSvg()); Assert.Equal(to.PreparedVisual.ToPng(), observed[^1].ToPng());
        var source = NativeRawValueSourceMotionTests.Metadata(from).GetProperty("nativeValueSource"); Assert.Equal("error-components", source.GetProperty("kind").GetString()); Assert.False(source.TryGetProperty("mapping", out _));
        var members = new[] { "value" }.Concat(form.Contains('x') ? new[] { "x-lower", "x-upper" } : Array.Empty<string>()).Concat(form.Contains('y') ? new[] { "y-lower", "y-upper" } : Array.Empty<string>()).ToArray();
        using var json = JsonDocument.Parse(payload!); var readings = json.RootElement.GetProperty("readings").EnumerateArray().ToArray(); Assert.Equal(members.Length * 4, readings.Length); Assert.Equal(members, readings.Take(members.Length).Select(row => row.GetProperty("component").GetString()));
        if (form.Contains('x')) { Assert.Equal(4, readings[1].GetProperty("maximum").GetDouble()); Assert.Equal("x-lower", readings[1].GetProperty("component").GetString()); }
        Assert.Equal(100, readings[0].GetProperty("maximum").GetDouble()); Assert.Throws<NotSupportedException>(() => renderer.PrepareValueTransitionJson(from, to, 3));
    }
    [Theory] [InlineData("x")] [InlineData("y")] [InlineData("xy")]
    public void ChangedPointFlashKeepsUnchangedWhiskersAndAllLabelsExactAndDoesNotChangeAcceptedValues(string form) {
        var renderer = new HtmlInteractiveChartRenderer(); var from = Section(renderer, form, true); var to = Section(renderer, form, true, true);
        var frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, ChartColor.Black); var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.ValueFlash, 3, ChartColor.Black));
        Capture(from, to, to, frames, payload, null, "error-flash-" + form);
        string Point(PreparedVisual drawing, string key, string id) => XDocument.Parse(drawing.ToSvg("error-flash-fixed")).Descendants().Single(row => (string?)row.Attribute("data-cfx-role") == "point" && (string?)row.Attribute("data-cfx-series-key") == key && (string?)row.Attribute("data-cfx-observation-id") == id).ToString(SaveOptions.DisableFormatting);
        Assert.NotEqual(Point(to.PreparedVisual, "delivery", "current"), Point(frames[1], "delivery", "current")); Assert.Equal(Point(to.PreparedVisual, "delivery", "later"), Point(frames[1], "delivery", "later")); Assert.Equal(Point(to.PreparedVisual, "comparison", "current"), Point(frames[1], "comparison", "current"));
        string[] Labels(PreparedVisual drawing) => XDocument.Parse(drawing.ToSvg("error-flash-fixed")).Descendants().Where(row => row.Name.LocalName == "text").Select(row => row.ToString(SaveOptions.DisableFormatting)).ToArray();
        var acceptedLabels = Labels(to.PreparedVisual); Assert.NotEmpty(acceptedLabels); Assert.Equal(acceptedLabels, Labels(frames[1]));
        Assert.Equal(to.PreparedVisual.Regions.Select(row => (row.Id, row.Role, row.Bounds, row.Label)), frames[1].Regions.Select(row => (row.Id, row.Role, row.Bounds, row.Label))); Assert.NotEqual(to.PreparedVisual.ToPng(), frames[1].ToPng()); Assert.Equal(to.PreparedVisual.ToSvg(), frames[^1].ToSvg());
    }
    [Theory] [InlineData("shape")] [InlineData("x")] [InlineData("id")] [InlineData("auto-axis")] [InlineData("callback")]
    public void DifferentNullableShapeIdentityOrUncapturablePolicyRejectsBeforeNativePayload(string condition) {
        var renderer = new HtmlInteractiveChartRenderer(); var from = Section(renderer, "xy", true); var chart = NativeErrorComponentBeforeControls.Model("xy", true, 50, .25, 1.75, 40, 60);
        if (condition == "shape") chart.Series[0].UpdateErrorBar(0, new ChartErrorBar(1, 50, yBounds: new ChartErrorBounds(40, 60)));
        if (condition == "x") chart.Series[0].UpdateErrorBar(0, new ChartErrorBar(1.25, 50, new ChartErrorBounds(.25, 1.75), new ChartErrorBounds(40, 60)));
        if (condition == "id") chart.Series[0].WithObservationId(0, new ChartObservationId("foreign"));
        if (condition == "auto-axis") chart.ConfigureXAxis(axis => { axis.Minimum = null; axis.Maximum = null; });
        if (condition == "callback") chart.WithValueFormat(ChartValueFormat.Custom(_ => "callback"));
        HtmlChartSection? to = null; var error = Record.Exception(() => { to = NativeErrorComponentBeforeControls.Section(renderer, chart); _ = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(frameCount: 3)); });
        NativeRadialMotionBeforeControls.Capture(from, "error-reject-" + condition + "-from"); if (to != null) NativeRadialMotionBeforeControls.Capture(to, "error-reject-" + condition + "-to"); NativeRadialMotionBeforeControls.Save("error-reject-" + condition + ".facts.json", JsonSerializer.Serialize(new { error = error?.ToString() })); Assert.IsType<NotSupportedException>(error);
    }
    [Fact]
    public void CopiedNullableTuplesAndCapturedFacesSurviveCallerMutationAndOrdinaryOutputStaysExact() {
        var renderer = new HtmlInteractiveChartRenderer(); var chart = NativeErrorComponentBeforeControls.Model("xy", true); var ordinary = renderer.PrepareSection(chart); var from = NativeErrorComponentBeforeControls.Section(renderer, chart); var to = Section(renderer, "xy", true, true);
        var frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null); var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(frameCount: 3)); Capture(from, to, ordinary, frames, payload, null, "error-detached");
        Assert.Equal(ordinary.PreparedVisual.ToSvg(), from.PreparedVisual.ToSvg()); Assert.Equal(ordinary.PreparedVisual.ToPng(), from.PreparedVisual.ToPng()); chart.Series[0].ReplaceErrorBars(Array.Empty<ChartErrorBar>()); chart.WithFontFamily("mutated caller");
        try { FontRegistry.Clear(); var repeated = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null); Assert.Equal(payload, renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(frameCount: 3)));
            for (var index = 0; index < frames.Count; index++) { Assert.Equal(frames[index].ToSvg(), repeated[index].ToSvg()); Assert.Equal(frames[index].ToPng(), repeated[index].ToPng()); }
        } finally { _ = BrowserProofFonts.Family; }
    }
    [Theory] [InlineData("empty")] [InlineData("x-outside")] [InlineData("y-outside")]
    public void IncumbentTypedContainmentAndPresenceGuardsRejectInvalidErrorTuples(string condition) {
        var error = Record.Exception(() => { if (condition == "empty") _ = new ChartErrorBar(1, 30); else if (condition == "x-outside") _ = new ChartErrorBar(1, 30, new ChartErrorBounds(2, 3)); else _ = new ChartErrorBar(1, 30, yBounds: new ChartErrorBounds(40, 50)); });
        NativeRadialMotionBeforeControls.Save("error-invalid-" + condition + ".facts.json", JsonSerializer.Serialize(new { error = error?.ToString() })); if (condition == "empty") Assert.IsType<ArgumentException>(error); else Assert.IsType<ArgumentOutOfRangeException>(error);
    }
    private static HtmlChartSection Section(HtmlInteractiveChartRenderer renderer, string form, bool labels, bool finish = false) => NativeErrorComponentBeforeControls.Section(renderer, finish ? NativeErrorComponentBeforeControls.Model(form, labels, 50, .25, 1.75, 40, 60) : NativeErrorComponentBeforeControls.Model(form, labels));
    private static void Capture(HtmlChartSection from, HtmlChartSection to, HtmlChartSection middle, IReadOnlyList<PreparedVisual>? frames, string? payload, Exception? error, string name) {
        NativeRadialMotionBeforeControls.Capture(from, name + "-from"); NativeRadialMotionBeforeControls.Capture(to, name + "-to"); NativeRadialMotionBeforeControls.Capture(middle, name + "-independent"); NativeRadialMotionBeforeControls.Save(name + ".facts.json", JsonSerializer.Serialize(new { payload, error = error?.ToString() })); if (frames != null) NativeRadialValueMotionTests.Capture(from, to, frames, payload ?? string.Empty, name);
    }
}
