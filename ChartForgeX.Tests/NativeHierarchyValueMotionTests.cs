using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeHierarchyValueMotionTests {
    [Theory]
    [InlineData(false, false, false)] [InlineData(false, false, true)]
    [InlineData(true, false, false)] [InlineData(true, false, true)]
    [InlineData(true, true, false)] [InlineData(true, true, true)]
    public void CompleteHierarchyFramesEqualIndependentNativeLayoutAndRetainAuthoredNodeReadings(bool sunburst, bool authored, bool subroot) {
        var renderer = new HtmlInteractiveChartRenderer(); var view = subroot ? new ChartHierarchyView("branch") : null;
        var from = renderer.PrepareHierarchySection(NativeHierarchyMotionBeforeControls.Model(sunburst, authored, 30), view);
        var to = renderer.PrepareHierarchySection(NativeHierarchyMotionBeforeControls.Model(sunburst, authored, 40), view);
        var middle = renderer.PrepareHierarchySection(NativeHierarchyMotionBeforeControls.Model(sunburst, authored, 35), view);
        var frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null);
        var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.Values, 3));
        var name = "hierarchy-complete-" + sunburst + "-" + authored + "-" + subroot;
        NativeRadialValueMotionTests.Capture(from, to, frames, payload, name); NativeRadialMotionBeforeControls.Capture(middle, name + "-independent");
        Assert.Equal(middle.PreparedVisual.ToSvg(), frames[1].ToSvg()); Assert.Equal(middle.PreparedVisual.ToSvg("hierarchy-independent"), frames[1].ToSvg("hierarchy-independent"));
        Assert.Equal(middle.PreparedVisual.ToPng(), frames[1].ToPng()); Assert.Equal(middle.PreparedVisual.Accessibility.Description, frames[1].Accessibility.Description);
        Assert.Equal(middle.PreparedVisual.Regions.Select(row => (row.Id, row.Role, row.Label)), frames[1].Regions.Select(row => (row.Id, row.Role, row.Label)));
        Assert.Equal(to.PreparedVisual.ToSvg(), frames[^1].ToSvg()); Assert.Equal(to.PreparedVisual.ToPng(), frames[^1].ToPng());
        using var json = JsonDocument.Parse(payload); Assert.Equal("hierarchy-value", json.RootElement.GetProperty("sourceKind").GetString());
        foreach (var reading in json.RootElement.GetProperty("readings").EnumerateArray()) { Assert.True(reading.TryGetProperty("nodeId", out _)); Assert.False(reading.TryGetProperty("observationId", out _)); }
        using var one = JsonDocument.Parse(renderer.PrepareValueTransitionJson(from, to, 3)); Assert.Equal("leaf-a", one.RootElement.GetProperty("nodeId").GetString()); Assert.False(one.RootElement.TryGetProperty("observationId", out _));
    }
    [Theory] [InlineData(false, false)] [InlineData(true, false)] [InlineData(true, true)]
    public void ExplicitZeroInitialBaselineCanRevealPreviouslyUnpaintedNativeCaptions(bool sunburst, bool authored) {
        var renderer = new HtmlInteractiveChartRenderer(); var from = renderer.PrepareHierarchySection(NativeHierarchyMotionBeforeControls.Model(sunburst, authored, 0));
        var to = renderer.PrepareHierarchySection(NativeHierarchyMotionBeforeControls.Model(sunburst, authored, 40)); var middle = renderer.PrepareHierarchySection(NativeHierarchyMotionBeforeControls.Model(sunburst, authored, 20));
        var frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null);
        var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.InitialValues, 3));
        NativeRadialValueMotionTests.Capture(from, to, frames, payload, "hierarchy-zero-" + sunburst + "-" + authored); NativeRadialMotionBeforeControls.Capture(middle, "hierarchy-zero-independent-" + sunburst + "-" + authored);
        Assert.Equal(middle.PreparedVisual.ToSvg(), frames[1].ToSvg()); Assert.Equal(middle.PreparedVisual.ToPng(), frames[1].ToPng());
        Assert.Equal("0", Node(from.PreparedVisual, "leaf-a").Metadata["data-cfx-authored-value"]); Assert.Equal("40", Node(to.PreparedVisual, "leaf-a").Metadata["data-cfx-authored-value"]);
    }
    [Fact]
    public void CapturedHierarchySourceAndActualFontRequestsSurviveCallerAndRegistryMutation() {
        var renderer = new HtmlInteractiveChartRenderer(); var chart = NativeHierarchyMotionBeforeControls.Model(true, true, 30);
        var from = renderer.PrepareHierarchySection(chart); var to = renderer.PrepareHierarchySection(NativeHierarchyMotionBeforeControls.Model(true, true, 40));
        var frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null); var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.Values, 3));
        NativeRadialValueMotionTests.Capture(from, to, frames, payload, "hierarchy-detached"); var svg = frames.Select(row => row.ToSvg()).ToArray(); var png = frames.Select(row => row.ToPng()).ToArray();
        chart.Series[0].ReplaceHierarchyItems(new[] { new ChartHierarchyItem("other", "Changed", value: 90) }); chart.Series[0].WithColor(ChartColor.Black);
        chart.ConfigureSunburst(options => options.CornerRadius = 9).WithFontFamily("Changed caller family").WithValueFormat(ChartValueFormat.Custom(_ => "Changed"));
        try { FontRegistry.Clear(); var repeated = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null);
            Assert.Equal(svg, repeated.Select(row => row.ToSvg())); for (var index = 0; index < repeated.Count; index++) Assert.Equal(png[index], repeated[index].ToPng());
            Assert.Equal(payload, renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.Values, 3)));
        } finally { _ = BrowserProofFonts.Family; }
    }
    [Theory] [InlineData("parent")] [InlineData("identity")] [InlineData("authored-shape")] [InlineData("callback")]
    public void IncompatibleStructureOrUncapturedPoliciesRejectWithoutChangingOrdinaryNativeRendering(string condition) {
        var renderer = new HtmlInteractiveChartRenderer(); var chart = NativeHierarchyMotionBeforeControls.Model(true, false, 30); var from = renderer.PrepareHierarchySection(chart);
        var items = chart.Series[0].HierarchyItems.Select(item => item.Id == "leaf-a" ? new ChartHierarchyItem(condition == "identity" ? "new-id" : item.Id, item.Label, condition == "parent" ? "root" : item.ParentId, 40, item.ColorValue)
            : condition == "authored-shape" && item.Id == "branch" ? new ChartHierarchyItem(item.Id, item.Label, item.ParentId, 70, item.ColorValue) : item).ToArray(); chart.Series[0].ReplaceHierarchyItems(items);
        if (condition == "callback") chart.ConfigureSunburst(options => options.SecondaryLabelFormatter = context => context.FormattedValue);
        var to = renderer.PrepareHierarchySection(chart); Exception? failure = null; try { _ = renderer.PrepareNativeTransitionJson(from, to,
            new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.Values, 3)); } catch (Exception exception) { failure = exception; }
        NativeRadialMotionBeforeControls.Capture(from, "hierarchy-reject-" + condition + "-from"); NativeRadialMotionBeforeControls.Capture(to, "hierarchy-reject-" + condition + "-to");
        NativeRadialMotionBeforeControls.Save("hierarchy-reject-" + condition + ".facts.json", JsonSerializer.Serialize(new { error = failure?.GetType().Name, message = failure?.Message }));
        Assert.IsType<NotSupportedException>(failure); Assert.NotEmpty(to.PreparedVisual.ToSvg());
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void CapturedHierarchyFlashKeepsAcceptedAuthoredAndDerivedNodeFactsAndExactFinalNativePaint(bool sunburst) {
        var renderer = new HtmlInteractiveChartRenderer();
        var from = renderer.PrepareHierarchySection(NativeHierarchyMotionBeforeControls.Model(sunburst, false, 30));
        var to = renderer.PrepareHierarchySection(NativeHierarchyMotionBeforeControls.Model(sunburst, false, 40));
        IReadOnlyList<PreparedVisual>? frames = null; string? payload = null;
        var failure = Record.Exception(() => { frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, ChartColor.Black);
            payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.ValueFlash, 3, ChartColor.Black)); });
        var name = "hierarchy-complete-flash-" + sunburst;
        NativeRadialMotionBeforeControls.Capture(from, name + "-from"); NativeRadialMotionBeforeControls.Capture(to, name + "-to");
        NativeRadialMotionBeforeControls.Save(name + ".facts.json", JsonSerializer.Serialize(new { payload, error = failure?.ToString() }));
        if (frames != null) NativeRadialValueMotionTests.Capture(from, to, frames, payload ?? string.Empty, name);
        Assert.Null(failure); var observed = Assert.IsAssignableFrom<IReadOnlyList<PreparedVisual>>(frames);
        Assert.Same(to.PreparedVisual, observed[0]); Assert.Same(to.PreparedVisual, observed[^1]);
        Assert.Equal(to.PreparedVisual.ToSvg(), observed[^1].ToSvg()); Assert.Equal(to.PreparedVisual.ToPng(), observed[^1].ToPng());
        Assert.NotEqual(to.PreparedVisual.ToPng(), observed[1].ToPng());
        string[] Facts(PreparedVisual drawing) => drawing.Scene.Nodes.OfType<VisualSceneGroup>().Select(group => string.Join("|", group.Metadata.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => pair.Key + "=" + pair.Value))).ToArray();
        Assert.Equal(Facts(to.PreparedVisual), Facts(observed[1])); Assert.Equal("40", Node(observed[1], "leaf-a").Metadata["data-cfx-authored-value"]);
        Assert.Equal(to.PreparedVisual.Regions.Select(row => (row.Id, row.Role, row.Label, row.Bounds)), observed[1].Regions.Select(row => (row.Id, row.Role, row.Label, row.Bounds)));
        Assert.Equal(to.PreparedVisual.Accessibility.Description, observed[1].Accessibility.Description);
        using var json = JsonDocument.Parse(payload!); Assert.Equal("value-flash", json.RootElement.GetProperty("effect").GetString()); Assert.Equal("hierarchy-value", json.RootElement.GetProperty("sourceKind").GetString());
        foreach (var reading in json.RootElement.GetProperty("readings").EnumerateArray()) { Assert.True(reading.TryGetProperty("nodeId", out _)); Assert.False(reading.TryGetProperty("observationId", out _)); }
    }
    [Fact]
    public void OrdinaryCustomSecondaryCallbackKeepsItsNativeInvocationCountAndIsNotRetained() {
        var calls = 0; var chart = NativeHierarchyMotionBeforeControls.Model(true, false, 30);
        chart.ConfigureSunburst(options => options.SecondaryLabelFormatter = context => { calls++; return context.FormattedValue; });
        var renderer = new HtmlInteractiveChartRenderer(); var prepared = renderer.PrepareHierarchySection(chart);
        NativeRadialMotionBeforeControls.Capture(prepared, "hierarchy-callback-control"); NativeRadialMotionBeforeControls.Save("hierarchy-callback-control.facts.json", JsonSerializer.Serialize(new { calls }));
        Assert.Equal(chart.Series[0].HierarchyItems.Count, calls); Assert.Null(prepared.PreparedVisual.Scene.ValueCapture);
        _ = renderer.PrepareHierarchySection(chart); Assert.Equal(chart.Series[0].HierarchyItems.Count * 2, calls);
    }
    internal static VisualSceneGroup Node(PreparedVisual source, string id) => source.Scene.Nodes.OfType<VisualSceneGroup>()
        .Single(row => row.Metadata.TryGetValue("data-cfx-node", out var node) && node == id && row.Role is not ("legend-entry" or "legend-entry-omitted"));
}
