using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeCompleteCartesianJointAndFlashTests {
    [Theory] [InlineData(false, false)] [InlineData(true, false)] [InlineData(false, true)] [InlineData(true, true)]
    public void EnabledLabelsUseRealCompleteValuesAndRequireTheExplicitSeriesCompositeForJointMotion(bool bar, bool overlay) {
        var renderer = new HtmlInteractiveChartRenderer(); var variant = bar ? "bar-labels" : "line-labels";
        var layering = overlay ? ChartVisibilityLabelLayering.PreserveOverlay : ChartVisibilityLabelLayering.GroupWithSeries;
        HtmlChartSection Section(double value, bool hidden) => renderer.PrepareVisibilitySection(NativeCompleteCartesianValueTests.Model(variant, value),
            new ChartVisibilityView(hidden ? new[] { new ChartSeriesId("delivery") } : Array.Empty<ChartSeriesId>(), layering));
        var from = Section(30, false); var to = Section(40, true);
        var middle = ChartMath.InterpolateRange(30, 40, .5); var middleFrom = Section(middle, false); var middleTo = Section(middle, true);
        IReadOnlyList<PreparedVisual>? actual = null; string? payload = null;
        var error = Record.Exception(() => { actual = VisualSeriesVisibilityCapture.ValueVisibilityFrames(from.PreparedVisual, to.PreparedVisual, 3); payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.ValuesAndVisibility, 3)); });
        var name = "complete-joint-labels-" + bar + "-" + overlay;
        NativeRadialMotionBeforeControls.Capture(from, name + "-from"); NativeRadialMotionBeforeControls.Capture(to, name + "-to");
        NativeRadialMotionBeforeControls.Capture(middleFrom, name + "-independent-values");
        NativeRadialMotionBeforeControls.Save(name + ".facts.json", JsonSerializer.Serialize(new { error = error?.ToString(), payload }));
        if (actual != null) NativeRadialValueMotionTests.Capture(from, to, actual, payload ?? string.Empty, name);
        if (overlay) { Assert.IsType<NotSupportedException>(error); return; }
        Assert.Null(error);
        var expected = VisualSeriesVisibilityCapture.Frames(middleFrom.PreparedVisual, middleTo.PreparedVisual, 3)[1];
        NativeSeriesVisibilityDefaultControls.Save(name + "-independent-opacity", expected);
        Assert.Equal(expected.ToSvg(), actual![1].ToSvg()); Assert.Equal(expected.ToSvg("complete-joint-check"), actual[1].ToSvg("complete-joint-check")); Assert.Equal(expected.ToPng(), actual[1].ToPng());
        Assert.Contains(actual[1].Scene.Nodes.OfType<VisualSceneGroup>(), row => row.Role == "series-visibility" && row.Opacity == .5);
        Assert.Equal(to.PreparedVisual.Regions.Select(row => (row.Id, row.Role, row.Label)), actual[^1].Regions.Select(row => (row.Id, row.Role, row.Label)));
    }

    [Fact]
    public void NativeAreaFlashOwnsTheChangedSeriesFillAndLineWhileRetainingAcceptedLabelsAndGeometry() {
        var renderer = new HtmlInteractiveChartRenderer(); var from = renderer.PrepareSection(NativeCompleteCartesianValueTests.Model("area", 30)); var to = renderer.PrepareSection(NativeCompleteCartesianValueTests.Model("area", 40));
        IReadOnlyList<PreparedVisual>? frames = null; string? payload = null;
        var error = Record.Exception(() => { frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, ChartColor.Black); payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.ValueFlash, 3, ChartColor.Black)); });
        NativeRadialMotionBeforeControls.Capture(to, "complete-area-flash-accepted");
        NativeRadialMotionBeforeControls.Save("complete-area-flash.facts.json", JsonSerializer.Serialize(new { error = error?.ToString(), payload }));
        if (frames != null) NativeRadialValueMotionTests.Capture(from, to, frames, payload ?? string.Empty, "complete-area-flash");
        Assert.Null(error); Assert.Same(to.PreparedVisual, frames![0]); Assert.Same(to.PreparedVisual, frames[^1]);
        var accepted = to.PreparedVisual.Scene.Nodes.OfType<VisualScenePath>().Single(row => row.Role == "area");
        var flash = frames[1].Scene.Nodes.OfType<VisualScenePath>().Single(row => row.Role == "area");
        Assert.Equal(accepted.Commands, flash.Commands); Assert.True(flash.Fill.HasValue); Assert.Equal(ChartColor.Black, flash.Fill.Value);
        Assert.NotEqual(to.PreparedVisual.ToPng(), frames[1].ToPng());
        foreach (var label in to.PreparedVisual.Scene.Nodes.OfType<VisualSceneText>()) Assert.Contains(frames[1].Scene.Nodes, node => ReferenceEquals(node, label));
    }

    [Theory] [InlineData(false)] [InlineData(true)]
    public void CompleteStackValuesKeepSeparateIntentsStrictForAccidentalMixedUpdates(bool labels) {
        var renderer = new HtmlInteractiveChartRenderer();
        HtmlChartSection Section(double value, bool hidden) => renderer.PrepareVisibilitySection(
            NativeCompleteCartesianValueTests.Model("stacked-bar", value).WithDataLabels(labels),
            new ChartVisibilityView(hidden ? new[] { new ChartSeriesId("delivery") } : Array.Empty<ChartSeriesId>(), ChartVisibilityLabelLayering.GroupWithSeries));
        var from = Section(30, false); var to = Section(40, true);
        var errors = new[] { HtmlNativeTransitionEffect.Values, HtmlNativeTransitionEffect.LegendVisibility }.Select(effect =>
            Record.Exception(() => renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(effect, 3)))).ToArray();
        NativeRadialMotionBeforeControls.Capture(from, "complete-stack-separate-reject-" + labels + "-from"); NativeRadialMotionBeforeControls.Capture(to, "complete-stack-separate-reject-" + labels + "-to");
        NativeRadialMotionBeforeControls.Save("complete-stack-separate-reject-" + labels + ".facts.json", JsonSerializer.Serialize(new { errors = errors.Select(error => error?.ToString()) }));
        Assert.All(errors, error => Assert.IsType<NotSupportedException>(error));
    }

    [Theory] [InlineData("line")] [InlineData("area")]
    public void CustomScatterMarkRolesCannotAcquireSharedSeriesLineOrAreaOwnership(string role) {
        var renderer = new HtmlInteractiveChartRenderer();
        Chart Model(double value) { var chart = NativeScatterValueBeforeControls.Model("customrole", value); chart.Series[0].SemanticRole = role; return chart; }
        var from = renderer.PrepareSection(Model(30)); var to = renderer.PrepareSection(Model(40));
        var frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, ChartColor.Black);
        var accepted = to.PreparedVisual.Scene.Nodes.OfType<VisualSceneMark>().Where(row => row.Role == role).ToArray();
        var flashed = frames[1].Scene.Nodes.OfType<VisualSceneMark>().Where(row => row.Role == role).ToArray();
        NativeRadialValueMotionTests.Capture(from, to, frames, string.Empty, "complete-shared-role-" + role);
        Assert.Equal(3, accepted.Length); Assert.Equal(accepted.Length, flashed.Length);
        Assert.Same(accepted[0], flashed[0]); Assert.NotSame(accepted[1], flashed[1]); Assert.Same(accepted[2], flashed[2]);
    }
}
