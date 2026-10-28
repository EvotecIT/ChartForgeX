using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeHierarchyVisibilityReflowTests {
    [Theory] [InlineData(false, false)] [InlineData(true, false)] [InlineData(true, true)]
    public void SubtreeMembershipUsesNativeResolvedValuesAndRetainsOriginalCatalog(bool sunburst, bool authored) {
        var chart = NativeHierarchyMotionBeforeControls.Model(sunburst, authored, 30).WithLegend(true); chart.Options.ShowPointLegend = !sunburst;
        var renderer = new HtmlInteractiveChartRenderer(); var from = renderer.PrepareVisibilitySection(chart, View());
        var to = renderer.PrepareVisibilitySection(chart, View("branch")); var frames = VisualSeriesVisibilityCapture.ReflowFrames(from.PreparedVisual, to.PreparedVisual, 5);
        var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.LegendVisibility, 5));
        NativeReflowVisibilityTests.Capture(from, to, frames, payload, "hierarchy-subtree-" + sunburst + "-" + authored);
        var capture = to.PreparedVisual.Scene.VisibilityCapture!; Assert.Equal(from.PreparedVisual.Scene.VisibilityCapture!.SourceKey, capture.SourceKey);
        Assert.Equal(6, capture.TargetCatalog.Count); Assert.Equal(5, capture.TargetCatalog.Count(row => row.Kind == ChartVisibilityTargetKind.HierarchyItem));
        Assert.All(capture.TargetCatalog.Where(row => row.Kind == ChartVisibilityTargetKind.HierarchyItem), row => { Assert.NotNull(row.NodeId); Assert.Null(row.ObservationId); });
        Assert.Equal(authored ? "100" : "20", NativeHierarchyValueMotionTests.Node(to.PreparedVisual, "root").Metadata["data-cfx-value"]);
        if (authored) Assert.Equal("80", NativeHierarchyValueMotionTests.Node(to.PreparedVisual, "root").Metadata["data-cfx-remainder-value"]);
        foreach (var id in new[] { "branch", "leaf-a", "leaf-b" }) { var node = NativeHierarchyValueMotionTests.Node(to.PreparedVisual, id); Assert.Equal("false", node.Metadata["data-cfx-visible"]); Assert.Equal("true", node.Metadata["data-cfx-passive"]); }
        Assert.Equal("visible", NativeHierarchyValueMotionTests.Node(to.PreparedVisual, "leaf-c").Metadata["data-cfx-geometry-status"]);
        Assert.Equal(from.PreparedVisual.Scene.VisibilityCapture.TargetCatalog.Select(row => row.NodeId), capture.TargetCatalog.Select(row => row.NodeId));
        Assert.Equal(to.PreparedVisual.ToSvg(), frames[^1].Frame.ToSvg()); Assert.Equal(to.PreparedVisual.ToPng(), frames[^1].Frame.ToPng());
        Assert.All(chart.Series[0].HierarchyItems.Where(item => item.Id.StartsWith("leaf-", StringComparison.Ordinal)), item => Assert.True(item.Value > 0));
        if (!sunburst) Assert.Equal(3, to.PreparedVisual.Scene.Nodes.OfType<VisualSceneGroup>().Count(row => row.Role == "legend-entry"));
    }
    [Theory] [InlineData(false, false)] [InlineData(true, false)] [InlineData(true, true)]
    public void HiddenRequestedRootUsesNativeNoDataAndNeverChoosesAnotherRoot(bool sunburst, bool authored) {
        var renderer = new HtmlInteractiveChartRenderer(); var chart = NativeHierarchyMotionBeforeControls.Model(sunburst, authored, 30);
        var from = renderer.PrepareVisibilitySection(chart, View(root: "branch")); var to = renderer.PrepareVisibilitySection(chart, View("branch", "branch"));
        var frames = VisualSeriesVisibilityCapture.ReflowFrames(from.PreparedVisual, to.PreparedVisual, 5); var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.LegendVisibility, 5));
        NativeReflowVisibilityTests.Capture(from, to, frames, payload, "hierarchy-hidden-root-" + sunburst + "-" + authored);
        Assert.Equal("branch", to.PreparedVisual.HierarchyView!.RootId); Assert.Contains(to.PreparedVisual.Diagnostics, row => row.Code == "hierarchy.no-data");
        Assert.All(to.PreparedVisual.Scene.Nodes.OfType<VisualSceneGroup>().Where(row => row.Metadata.ContainsKey("data-cfx-node")), row => Assert.Equal("false", row.Metadata["data-cfx-visible"]));
        Assert.Equal(5, to.PreparedVisual.Scene.Nodes.OfType<VisualSceneGroup>().Count(row => row.Metadata.ContainsKey("data-cfx-node")));
        using var json = JsonDocument.Parse(payload); Assert.Equal("branch", json.RootElement.GetProperty("visibility").GetProperty("toHierarchyRootId").GetString());
    }
    [Fact]
    public void RootOnlyChangeUsesTheSameSourceAndCapturedSourceDestinationFrameBasis() {
        var chart = NativeHierarchyMotionBeforeControls.Model(true, true, 30); var renderer = new HtmlInteractiveChartRenderer();
        var from = renderer.PrepareVisibilitySection(chart, View()); var to = renderer.PrepareVisibilitySection(chart, View(root: "branch"));
        var frames = VisualSeriesVisibilityCapture.ReflowFrames(from.PreparedVisual, to.PreparedVisual, 5); var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.LegendVisibility, 5));
        NativeReflowVisibilityTests.Capture(from, to, frames, payload, "hierarchy-root-only"); Assert.True(frames[1].Source); Assert.False(frames[2].Source);
        Assert.Equal(from.PreparedVisual.Scene.VisibilityCapture!.SourceKey, to.PreparedVisual.Scene.VisibilityCapture!.SourceKey);
        Assert.Null(from.PreparedVisual.HierarchyView); Assert.Equal("branch", to.PreparedVisual.HierarchyView!.RootId);
    }
    [Fact]
    public void HidingCannotLaunderInvalidOriginalAuthoredTotalsAndUnknownTargetsReject() {
        var renderer = new HtmlInteractiveChartRenderer(); var chart = NativeHierarchyMotionBeforeControls.Model(true, true, 30);
        var ordinary = renderer.PrepareHierarchySection(chart); chart.Series[0].ReplaceHierarchyItems(chart.Series[0].HierarchyItems.Select(item => item.Id == "branch" ? new ChartHierarchyItem(item.Id, item.Label, item.ParentId, 45) : item));
        Exception? failure = null; try { _ = renderer.PrepareVisibilitySection(chart, View("leaf-b")); } catch (Exception exception) { failure = exception; }
        NativeRadialMotionBeforeControls.Capture(ordinary, "hierarchy-invalid-total-control"); NativeRadialMotionBeforeControls.Save("hierarchy-invalid-total.facts.json", JsonSerializer.Serialize(new { authored = chart.Series[0].HierarchyItems.Select(item => new { item.Id, item.Value }), error = failure?.Message }));
        Assert.IsType<ArgumentException>(failure); var valid = NativeHierarchyMotionBeforeControls.Model(true, true, 30);
        Assert.Throws<ArgumentException>(() => renderer.PrepareVisibilitySection(valid, View("unknown"))); Assert.Throws<ArgumentException>(() => renderer.PrepareVisibilitySection(valid, View(root: "unknown")));
        Assert.Throws<ArgumentException>(() => renderer.PrepareVisibilitySection(valid, new ChartVisibilityView(new[] { ChartVisibilityTarget.Observation(new ChartSeriesId("areas"), new ChartObservationId("leaf-a")) }, ChartVisibilityLayout.Reflow)));
    }
    [Fact]
    public void InvisibleRawPaintChangesStillChangeCanonicalSourceKeyWhileNativeSvgCanRemainEqual() {
        var a = NativeHierarchyMotionBeforeControls.Model(false, false, 0); var b = NativeHierarchyMotionBeforeControls.Model(false, false, 0); b.Series[0].WithPointColor(2, ChartColor.Black);
        var renderer = new HtmlInteractiveChartRenderer(); var first = renderer.PrepareVisibilitySection(a, View()); var other = renderer.PrepareVisibilitySection(b, View());
        NativeRadialMotionBeforeControls.Capture(first, "hierarchy-hidden-paint-a"); NativeRadialMotionBeforeControls.Capture(other, "hierarchy-hidden-paint-b");
        var keys = new[] { first.PreparedVisual.Scene.VisibilityCapture!.SourceKey, other.PreparedVisual.Scene.VisibilityCapture!.SourceKey }; NativeRadialMotionBeforeControls.Save("hierarchy-hidden-paint.keys.json", JsonSerializer.Serialize(keys));
        Assert.Equal(first.PreparedVisual.ToSvg("same-hierarchy-ink"), other.PreparedVisual.ToSvg("same-hierarchy-ink")); Assert.NotEqual(keys[0], keys[1]);
        var destination = renderer.PrepareVisibilitySection(b, View("branch")); Assert.Throws<NotSupportedException>(() => VisualSeriesVisibilityCapture.ReflowFrames(first.PreparedVisual, destination.PreparedVisual, 5));
    }
    internal static ChartVisibilityView View(string? hidden = null, string? root = null) => new(hidden == null ? Array.Empty<ChartVisibilityTarget>() : new[] { ChartVisibilityTarget.HierarchyItem(new ChartSeriesId("areas"), hidden) },
        ChartVisibilityLayout.Reflow, ChartVisibilityLabelLayering.PreserveOverlay, root == null ? null : new ChartHierarchyView(root));
}
