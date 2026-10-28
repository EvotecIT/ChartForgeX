using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeComponentTimeValueTests {
    [Theory]
    [InlineData("candlestick", "elapsed", false)] [InlineData("candlestick", "sessions", false)] [InlineData("candlestick", "ordinal", false)] [InlineData("candlestick", "calendar", false)]
    [InlineData("ohlc", "elapsed", false)] [InlineData("ohlc", "sessions", false)] [InlineData("ohlc", "ordinal", false)] [InlineData("ohlc", "calendar", false)]
    [InlineData("bubble", "ordinal", false)] [InlineData("range", "calendar", false)] [InlineData("error", "elapsed", false)]
    [InlineData("candlestick", "elapsed", true)] [InlineData("candlestick", "sessions", true)] [InlineData("candlestick", "ordinal", true)] [InlineData("candlestick", "calendar", true)]
    public void FixedTemporalXFramesEqualIndependentNativeTimePoliciesAndExactNumericValues(string family, string policy, bool initial) {
        var renderer = new HtmlInteractiveChartRenderer(); var fromChart = NativeComponentTimeBeforeControls.Model(family, policy, 40); var from = NativeComponentTimeBeforeControls.Section(renderer, fromChart); var to = NativeComponentTimeBeforeControls.Section(renderer, NativeComponentTimeBeforeControls.Model(family, policy, 60)); var middle = NativeComponentTimeBeforeControls.Section(renderer, NativeComponentTimeBeforeControls.Model(family, policy, ChartMath.InterpolateRange(40, 60, .5)));
        IReadOnlyList<PreparedVisual>? frames = null; string? payload = null; var error = Record.Exception(() => { frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null); payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(initial ? HtmlNativeTransitionEffect.InitialValues : HtmlNativeTransitionEffect.Values, 3)); });
        Capture(from, to, middle, frames, payload, error, "time-native-" + family + "-" + policy + "-" + initial); Assert.Null(error); var actual = Assert.IsAssignableFrom<IReadOnlyList<PreparedVisual>>(frames);
        Assert.Equal(middle.PreparedVisual.ToSvg(), actual[1].ToSvg()); Assert.Equal(middle.PreparedVisual.ToSvg("time-native-fixed"), actual[1].ToSvg("time-native-fixed")); Assert.Equal(middle.PreparedVisual.ToPng(), actual[1].ToPng()); Assert.Equal(to.PreparedVisual.ToSvg(), actual[^1].ToSvg()); Assert.Equal(to.PreparedVisual.ToPng(), actual[^1].ToPng());
        Assert.Equal(middle.PreparedVisual.Regions.Select(row => (row.Id, row.Role, row.Bounds, row.Label)), actual[1].Regions.Select(row => (row.Id, row.Role, row.Bounds, row.Label))); Assert.Equal(middle.PreparedVisual.Accessibility.Description, actual[1].Accessibility.Description); Assert.Null(fromChart.Options.XAxis.PreparedTimeDomain);
        foreach (var frame in actual) { Assert.True(VisualSeriesVisibilityCapture.SameAxis(from.PreparedVisual.XProjection, frame.XProjection)); Assert.Equal(Coordinates(from.PreparedVisual), Coordinates(frame)); }
        using var json = JsonDocument.Parse(payload!); Assert.Equal("cartesian-component-value", json.RootElement.GetProperty("sourceKind").GetString()); Assert.Equal(initial ? "initial-values" : "values", json.RootElement.GetProperty("effect").GetString());
    }
    [Theory] [InlineData("time-y")] [InlineData("time-bound")] [InlineData("callback")] [InlineData("changed-policy")]
    public void TimeValuedMembersAndForeignCapturedPolicyStayExplicitlyUnsupported(string condition) {
        var renderer = new HtmlInteractiveChartRenderer(); var from = NativeComponentTimeBeforeControls.Section(renderer, NativeComponentTimeBeforeControls.Model(condition == "time-bound" ? "error" : "candlestick", "elapsed", 40)); var chart = NativeComponentTimeBeforeControls.Model(condition == "time-bound" ? "error" : "candlestick", "elapsed", 60);
        if (condition == "time-y") chart.ConfigureYAxis(axis => axis.Scale = ChartScaleKind.Time);
        if (condition == "time-bound") { var item = chart.Series[0].ErrorBars[0]; chart.Series[0].UpdateErrorBar(0, new ChartErrorBar(item.X, item.Y, new ChartErrorBounds(item.X - 1, item.X + 1), item.YBounds)); }
        if (condition == "callback") chart.ConfigureXAxis(axis => axis.TimeTickLabelFormatter = (_, _) => "custom time");
        if (condition == "changed-policy") chart.ConfigureXAxis(axis => axis.TimeZone = TimeZoneInfo.CreateCustomTimeZone("Different captured zone", TimeSpan.FromHours(2), "Different", "Different"));
        HtmlChartSection? to = null; var error = Record.Exception(() => { to = NativeComponentTimeBeforeControls.Section(renderer, chart); _ = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(frameCount: 3)); });
        Capture(from, to ?? from, from, null, null, error, "time-reject-" + condition); Assert.IsType<NotSupportedException>(error);
    }
    [Fact]
    public void OriginalTimePolicyAndReplayFontsRemainCapturedAcrossCallerAxisMutation() {
        var renderer = new HtmlInteractiveChartRenderer(); var chart = NativeComponentTimeBeforeControls.Model("candlestick", "calendar", 40); var from = NativeComponentTimeBeforeControls.Section(renderer, chart); var to = NativeComponentTimeBeforeControls.Section(renderer, NativeComponentTimeBeforeControls.Model("candlestick", "calendar", 60));
        var frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null); var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(frameCount: 3)); Capture(from, to, from, frames, payload, null, "time-detached");
        chart.ConfigureXAxis(axis => { axis.TimeSpacing = null; axis.TimeZone = TimeZoneInfo.CreateCustomTimeZone("Mutated", TimeSpan.FromHours(-3), "Mutated", "Mutated"); axis.Reversed = true; }); chart.Series[0].ReplaceFinancialValues(Array.Empty<ChartCandlestick>());
        try { FontRegistry.Clear(); var repeated = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null); Assert.Equal(payload, renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(frameCount: 3))); for (var index = 0; index < frames.Count; index++) { Assert.Equal(frames[index].ToSvg(), repeated[index].ToSvg()); Assert.Equal(frames[index].ToPng(), repeated[index].ToPng()); Assert.True(VisualSeriesVisibilityCapture.SameAxis(frames[index].XProjection, repeated[index].XProjection)); } } finally { _ = BrowserProofFonts.Family; }
    }
    internal static string[] Coordinates(PreparedVisual visual) => XDocument.Parse(visual.ToSvg()).Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "point").Select(node => string.Join("|", new[] { "data-cfx-series-key", "data-cfx-observation-id", "data-cfx-x", "data-cfx-x-scale", "data-cfx-x-instant" }.Select(name => (string?)node.Attribute(name)))).ToArray();
    private static void Capture(HtmlChartSection from, HtmlChartSection to, HtmlChartSection middle, IReadOnlyList<PreparedVisual>? frames, string? payload, Exception? error, string name) {
        NativeRadialMotionBeforeControls.Capture(from, name + "-from"); NativeRadialMotionBeforeControls.Capture(to, name + "-to"); NativeRadialMotionBeforeControls.Capture(middle, name + "-independent"); NativeRadialMotionBeforeControls.Save(name + ".facts.json", JsonSerializer.Serialize(new { payload, error = error?.ToString() })); if (frames != null) NativeRadialValueMotionTests.Capture(from, to, frames, payload ?? string.Empty, name);
    }
}
