using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeCompleteCartesianValueTests {
    [Theory]
    [InlineData("line-labels")] [InlineData("bar-labels")] [InlineData("step-line")]
    [InlineData("area")] [InlineData("step-area")] [InlineData("stacked-bar")]
    [InlineData("normalized-bar")] [InlineData("stacked-area-gap")] [InlineData("stacked-area-zero")]
    public void CompleteNativeFramesMatchIndependentStackAreaAndCaptionPreparationExactly(string variant) {
        var renderer = new HtmlInteractiveChartRenderer(); var from = renderer.PrepareSection(Model(variant, 30)); var to = renderer.PrepareSection(Model(variant, 40));
        var middleValue = ChartMath.InterpolateRange(30, 40, .5); var expected = renderer.PrepareSection(Model(variant, middleValue));
        IReadOnlyList<PreparedVisual>? frames = null; string? payload = null;
        var error = Record.Exception(() => { frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null); payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(frameCount: 3)); });
        var name = "complete-cartesian-" + variant;
        NativeRadialMotionBeforeControls.Capture(from, name + "-from"); NativeRadialMotionBeforeControls.Capture(to, name + "-to"); NativeRadialMotionBeforeControls.Capture(expected, name + "-independent");
        NativeRadialMotionBeforeControls.Save(name + ".facts.json", JsonSerializer.Serialize(new { middleValue, payload, error = error?.ToString() }));
        if (frames != null) NativeRadialValueMotionTests.Capture(from, to, frames, payload ?? string.Empty, name);
        Assert.Null(error); Assert.NotNull(payload); Assert.Same(from.PreparedVisual, frames![0]); Assert.Same(to.PreparedVisual, frames[^1]);
        Assert.Equal(expected.PreparedVisual.ToSvg(), frames[1].ToSvg()); Assert.Equal(expected.PreparedVisual.ToSvg("complete-cartesian-check"), frames[1].ToSvg("complete-cartesian-check"));
        Assert.Equal(expected.PreparedVisual.ToPng(), frames[1].ToPng()); Assert.Equal(expected.PreparedVisual.Regions.Select(row => (row.Id, row.Role, row.Label, row.Bounds)), frames[1].Regions.Select(row => (row.Id, row.Role, row.Label, row.Bounds)));
        Assert.True(VisualSeriesVisibilityCapture.SameAxis(expected.PreparedVisual.YProjection, frames[1].YProjection));
        Assert.Equal(from.PreparedVisual.Scene.ValueCapture!.Plan.Readings.Count, to.PreparedVisual.Scene.ValueCapture!.Plan.Readings.Count);
        using var json = JsonDocument.Parse(payload); Assert.Equal("cartesian-value", json.RootElement.GetProperty("sourceKind").GetString());
        if (variant.StartsWith("stacked-area", StringComparison.Ordinal)) {
            Assert.Equal(5, frames[1].Scene.Nodes.OfType<VisualSceneGroup>().Count(row => row.Role == "point"));
            Assert.DoesNotContain(frames[1].Scene.Nodes.OfType<VisualSceneGroup>(), row => row.Metadata.TryGetValue("data-cfx-series-key", out var key) && key == "comparison" && row.Metadata.TryGetValue("data-cfx-observation-id", out var id) && id == "current");
        }
    }

    [Theory] [InlineData("line-labels")] [InlineData("stacked-area-zero")]
    public void CompleteFramesRemainDetachedAfterSourceAndGlobalFontRegistryMutation(string variant) {
        var renderer = new HtmlInteractiveChartRenderer(); var model = Model(variant, 30); var from = renderer.PrepareSection(model); var to = renderer.PrepareSection(Model(variant, 40));
        var frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null);
        NativeRadialValueMotionTests.Capture(from, to, frames, string.Empty, "complete-cartesian-detached-" + variant);
        model.Series[0].Points.Clear(); model.ConfigureYAxis(axis => axis.Maximum = 300); model.WithFontFamily("Later caller font");
        try { FontRegistry.Clear(); var repeated = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null); for (var index = 0; index < frames.Count; index++) { Assert.Equal(frames[index].ToSvg(), repeated[index].ToSvg()); Assert.Equal(frames[index].ToPng(), repeated[index].ToPng()); } }
        finally { _ = BrowserProofFonts.Family; }
    }

    [Theory] [InlineData("changed-missing-policy")] [InlineData("changed-id")] [InlineData("changed-domain")] [InlineData("custom-format")]
    public void CompleteAdmissionRejectsChangedAuthoredStructurePolicyOrUncapturedFormatting(string condition) {
        var renderer = new HtmlInteractiveChartRenderer(); var from = renderer.PrepareSection(Model("stacked-area-zero", 30)); var destination = Model("stacked-area-zero", 40);
        if (condition == "changed-missing-policy") foreach (var row in destination.Series) row.WithStackedAreaMissingData(ChartStackedAreaMissingDataPolicy.Gap);
        if (condition == "changed-id") destination.Series[0].WithObservationId(1, new ChartObservationId("changed"));
        if (condition == "changed-domain") destination.ConfigureYAxis(axis => axis.Maximum = 200);
        if (condition == "custom-format") destination.WithValueFormat(ChartValueFormat.Custom(value => "callback " + value));
        var to = renderer.PrepareSection(destination); var error = Record.Exception(() => renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(frameCount: 3)));
        NativeRadialMotionBeforeControls.Capture(from, "complete-cartesian-reject-" + condition + "-from"); NativeRadialMotionBeforeControls.Capture(to, "complete-cartesian-reject-" + condition + "-to");
        NativeRadialMotionBeforeControls.Save("complete-cartesian-reject-" + condition + ".facts.json", JsonSerializer.Serialize(new { error = error?.ToString() })); Assert.IsType<NotSupportedException>(error);
    }

    internal static Chart Model(string variant, double reading, int width = 760, bool dark = false) => NativeCompleteCartesianValueRecipe.Create(variant, reading, width, dark ? VisualThemeMode.Dark : VisualThemeMode.Light, BrowserProofFonts.Family);
}
