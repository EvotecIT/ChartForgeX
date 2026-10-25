using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeRadialVisibilityReflowTests {
    [Theory] [InlineData(false, false)] [InlineData(true, false)] [InlineData(false, true)] [InlineData(true, true)]
    public void FullSourceOtherTargetKeepsItsContributorsWhenVisibleNativeOtherChanges(bool donut, bool sourceOrder) {
        var chart = NativeRadialMotionBeforeControls.Model(donut, true, "outside"); chart.Options.PiePreserveSourceOrder = sourceOrder;
        var renderer = new HtmlInteractiveChartRenderer(); var from = renderer.PrepareVisibilitySection(chart, View(Primary()));
        var to = renderer.PrepareVisibilitySection(chart, View(Primary(), ChartVisibilityTarget.RadialOther(new ChartSeriesId("parts"))));
        var frames = VisualSeriesVisibilityCapture.ReflowFrames(from.PreparedVisual, to.PreparedVisual, 5);
        var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.LegendVisibility, 5));
        NativeReflowVisibilityTests.Capture(from, to, frames, payload, "radial-other-" + donut + "-" + sourceOrder);
        var first = from.PreparedVisual.Scene.VisibilityCapture!; var last = to.PreparedVisual.Scene.VisibilityCapture!;
        Assert.Equal(first.SourceKey, last.SourceKey); Assert.Equal(new[] { 2, 3, 4 }, first.ReflowSource!.RadialOtherMembers("parts"));
        var nativeOther = Points(from.PreparedVisual).Single(row => row.Metadata["data-cfx-point"] == "-1");
        Assert.Equal("3,4", nativeOther.Metadata["data-cfx-source-points"]); Assert.Equal("5", nativeOther.Metadata["data-cfx-value"]);
        var only = Assert.Single(Points(to.PreparedVisual)); Assert.Equal("part-1", only.Metadata["data-cfx-observation-id"]); Assert.Equal("1", only.Metadata["data-cfx-source-points"]);
        Assert.DoesNotContain(to.PreparedVisual.Regions, row => (row.Role is "pie-slice" or "donut-slice") && row.Label != null && row.Label.StartsWith("Part 3", StringComparison.Ordinal));
        var legend = Legend(to.PreparedVisual); Assert.Equal(3, legend.Length); var retained = legend.Single(row => row.Metadata["data-cfx-point"] == "-1");
        Assert.Equal("2,3,4", retained.Metadata["data-cfx-source-points"]); Assert.Equal("15", retained.Metadata["data-cfx-value"]); Assert.Equal("true", retained.Metadata["data-cfx-target-hidden"]);
        Assert.Single(last.TargetCatalog, row => row.Kind == ChartVisibilityTargetKind.RadialOther); Assert.Equal(7, last.TargetCatalog.Count);
        Assert.Equal(to.PreparedVisual.ToSvg(), frames[^1].Frame.ToSvg()); Assert.Equal(to.PreparedVisual.ToPng(), frames[^1].Frame.ToPng());
        Assert.Empty(frames[2].Frame.Regions); Assert.Equal(new[] { true, true, false, false, false }, frames.Select(row => row.Source));
        using var json = JsonDocument.Parse(payload); Assert.Contains(json.RootElement.GetProperty("visibility").GetProperty("toHiddenTargets").EnumerateArray(), row => row.GetProperty("kind").GetString() == "radialOther");
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void ObservationReflowKeepsOriginalPaletteSparsePolicyIdsAndFullLegendReadouts(bool donut) {
        var chart = NativeRadialMotionBeforeControls.Model(donut, false, "inside"); chart.Series[0].WithPointColor(2, ChartColor.FromRgb(31, 117, 180));
        var renderer = new HtmlInteractiveChartRenderer(); var from = renderer.PrepareVisibilitySection(chart, View()); var to = renderer.PrepareVisibilitySection(chart, View(Primary()));
        NativeReflowVisibilityTests.Capture(from, to, VisualSeriesVisibilityCapture.ReflowFrames(from.PreparedVisual, to.PreparedVisual, 3),
            renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.LegendVisibility, 3)), "radial-observation-" + donut);
        var retained = Points(to.PreparedVisual); Assert.Equal(new[] { "part-1", "part-2" }, retained.Select(row => row.Metadata["data-cfx-observation-id"]));
        Assert.Equal(new[] { "1", "2" }, retained.Select(row => row.Metadata["data-cfx-source-points"]));
        var beforeSlices = SliceMarks(from.PreparedVisual); var afterSlices = SliceMarks(to.PreparedVisual);
        Assert.Equal(beforeSlices[1].Fill, afterSlices[0].Fill); Assert.Equal(beforeSlices[2].Fill, afterSlices[1].Fill);
        var original = Legend(from.PreparedVisual).Single(row => row.Metadata["data-cfx-observation-id"] == "primary");
        var hidden = Legend(to.PreparedVisual).Single(row => row.Metadata["data-cfx-observation-id"] == "primary");
        foreach (var fact in new[] { "data-cfx-value", "data-cfx-percent", "data-cfx-source-points", "data-cfx-label" }) Assert.Equal(original.Metadata[fact], hidden.Metadata[fact]);
        Assert.Equal("true", hidden.Metadata["data-cfx-target-hidden"]);
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public void HiddenRawValueAndPaintCannotMasqueradeAsSameCapturedSource(bool donut) {
        Chart Model(bool changed) { var chart = NativeRadialMotionBeforeControls.Model(donut, true, "hidden"); chart.Options.MaximumPieSlices = 10; chart.WithLegend(false); chart.Options.ShowDonutCenterLabel = false;
            chart.Series[0].Points[4] = new ChartPoint(5, changed ? 2 * double.Epsilon : double.Epsilon);
            chart.Series[0].WithPointSliceRadiusRatio(4, 0); if (changed) chart.Series[0].WithPointColor(4, ChartColor.Black); return chart; }
        var renderer = new HtmlInteractiveChartRenderer(); var firstSource = Model(false); var lastSource = Model(true);
        var a = renderer.PrepareVisibilitySection(firstSource, View()); var b = renderer.PrepareVisibilitySection(lastSource, View());
        var last = renderer.PrepareVisibilitySection(lastSource, View(Primary())); Exception? failure = null;
        try { _ = renderer.PrepareNativeTransitionJson(a, last, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.LegendVisibility, 3)); } catch (Exception exception) { failure = exception; }
        NativeRadialMotionBeforeControls.Capture(a, "radial-hidden-key-" + donut + "-a"); NativeRadialMotionBeforeControls.Capture(b, "radial-hidden-key-" + donut + "-b");
        NativeRadialMotionBeforeControls.Save("radial-hidden-key-" + donut + ".facts.json", JsonSerializer.Serialize(new { first = a.PreparedVisual.Scene.VisibilityCapture!.SourceKey, second = b.PreparedVisual.Scene.VisibilityCapture!.SourceKey, error = failure?.Message }));
        var invalid = Model(false); invalid.Series[0].Points[4] = new ChartPoint(5, -1);
        Exception? ordinaryInvalid = null, hiddenInvalid = null;
        try { _ = renderer.PrepareSection(invalid); } catch (Exception exception) { ordinaryInvalid = exception; }
        try { _ = renderer.PrepareVisibilitySection(invalid, View(Primary())); } catch (Exception exception) { hiddenInvalid = exception; }
        NativeRadialMotionBeforeControls.Save("radial-hidden-key-" + donut + ".source-domain.json", JsonSerializer.Serialize(new {
            first = firstSource.Series[0].Points[4].Y, last = lastSource.Series[0].Points[4].Y, radius = firstSource.Series[0].SliceRadiusRatio(4),
            firstPaint = firstSource.Series[0].PointColors.Select(color => color?.ToHexRgba()), lastPaint = lastSource.Series[0].PointColors.Select(color => color?.ToHexRgba()),
            ordinaryInvalid = ordinaryInvalid?.Message, hiddenInvalid = hiddenInvalid?.Message }));
        Assert.NotEqual(firstSource.Series[0].Points[4].Y, lastSource.Series[0].Points[4].Y); Assert.True(firstSource.Series[0].Points[4].Y > 0);
        Assert.IsType<InvalidOperationException>(ordinaryInvalid); Assert.IsType<InvalidOperationException>(hiddenInvalid);
        Assert.Contains("non-negative", ordinaryInvalid!.Message, StringComparison.Ordinal); Assert.Contains("non-negative", hiddenInvalid!.Message, StringComparison.Ordinal);
        Assert.Equal(a.PreparedVisual.ToSvg("same-native-radial-ink"), b.PreparedVisual.ToSvg("same-native-radial-ink")); Assert.Equal(a.PreparedVisual.ToPng(), b.PreparedVisual.ToPng());
        Assert.NotEqual(a.PreparedVisual.Scene.VisibilityCapture!.SourceKey, b.PreparedVisual.Scene.VisibilityCapture!.SourceKey); Assert.IsType<NotSupportedException>(failure);
    }
    [Fact]
    public void WholeSeriesNoDataAndOverlappingAbsoluteTargetsKeepOneRetainedSourceCatalog() {
        var chart = NativeRadialMotionBeforeControls.Model(false, true); var renderer = new HtmlInteractiveChartRenderer(); var from = renderer.PrepareVisibilitySection(chart, View());
        var to = renderer.PrepareVisibilitySection(chart, View(ChartVisibilityTarget.Series(new ChartSeriesId("parts"))));
        NativeReflowVisibilityTests.Capture(from, to, VisualSeriesVisibilityCapture.ReflowFrames(from.PreparedVisual, to.PreparedVisual, 3), renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.LegendVisibility, 3)), "radial-all-hidden");
        Assert.Empty(Points(to.PreparedVisual)); Assert.Equal(3, Legend(to.PreparedVisual).Length); Assert.Contains(to.PreparedVisual.Diagnostics, row => row.Code == "radial.no-data");
        var other = ChartVisibilityTarget.RadialOther(new ChartSeriesId("parts")); var child = ChartVisibilityTarget.Observation(new ChartSeriesId("parts"), new ChartObservationId("part-2"));
        var both = renderer.PrepareVisibilitySection(chart, View(other, child)); var onlyChild = renderer.PrepareVisibilitySection(chart, View(child));
        NativeRadialMotionBeforeControls.Capture(both, "radial-overlap-both"); NativeRadialMotionBeforeControls.Capture(onlyChild, "radial-overlap-child");
        Assert.Equal(new[] { 0, 1 }, both.PreparedVisual.Scene.VisibilityCapture!.RenderedSourceIndices(0)); Assert.Equal(new[] { 0, 1, 3, 4 }, onlyChild.PreparedVisual.Scene.VisibilityCapture!.RenderedSourceIndices(0));
        Assert.Throws<ArgumentException>(() => renderer.PrepareVisibilitySection(NativeRadialMotionBeforeControls.Model(false, false), View(other)));
    }
    [Fact]
    public void AuthoredObservationNamedOtherKeepsASeparateIdentityFromCapturedOtherMembership() {
        var chart = NativeRadialMotionBeforeControls.Model(false, true); chart.Series[0].WithObservationId(0, new ChartObservationId("Other"));
        var renderer = new HtmlInteractiveChartRenderer(); var from = renderer.PrepareVisibilitySection(chart, View());
        var observation = renderer.PrepareVisibilitySection(chart, View(ChartVisibilityTarget.Observation(new ChartSeriesId("parts"), new ChartObservationId("Other"))));
        var aggregate = renderer.PrepareVisibilitySection(chart, View(ChartVisibilityTarget.RadialOther(new ChartSeriesId("parts"))));
        var options = new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.LegendVisibility, 3);
        NativeReflowVisibilityTests.Capture(from, observation, VisualSeriesVisibilityCapture.ReflowFrames(from.PreparedVisual, observation.PreparedVisual, 3), renderer.PrepareNativeTransitionJson(from, observation, options), "radial-namespace-observation");
        NativeReflowVisibilityTests.Capture(from, aggregate, VisualSeriesVisibilityCapture.ReflowFrames(from.PreparedVisual, aggregate.PreparedVisual, 3), renderer.PrepareNativeTransitionJson(from, aggregate, options), "radial-namespace-aggregate");
        var catalog = from.PreparedVisual.Scene.VisibilityCapture!.TargetCatalog;
        Assert.Single(catalog, row => row.Kind == ChartVisibilityTargetKind.RadialOther && row.ObservationId == null);
        Assert.Single(catalog, row => row.Kind == ChartVisibilityTargetKind.Observation && row.ObservationId?.Value == "Other");
        Assert.Equal(new[] { 1, 2, 3, 4 }, observation.PreparedVisual.Scene.VisibilityCapture!.RenderedSourceIndices(0));
        Assert.Equal(new[] { 0, 1 }, aggregate.PreparedVisual.Scene.VisibilityCapture!.RenderedSourceIndices(0));
    }
    [Fact]
    public void OrdinarySliceCallbackRemainsCallableAndOptInCapturedPolicyRejectsClearly() {
        var chart = NativeRadialMotionBeforeControls.Model(false, false); var calls = 0; chart.Options.PieSliceLabelFormatter = _ => { calls++; return "Custom"; };
        var renderer = new HtmlInteractiveChartRenderer(); var ordinary = renderer.PrepareSection(chart); Exception? failure = null;
        try { _ = renderer.PrepareVisibilitySection(chart, View()); } catch (Exception exception) { failure = exception; }
        NativeRadialMotionBeforeControls.Capture(ordinary, "radial-ordinary-callback"); NativeRadialMotionBeforeControls.Save("radial-ordinary-callback.facts.json", JsonSerializer.Serialize(new { calls, error = failure?.Message }));
        Assert.True(calls > 0); Assert.IsType<NotSupportedException>(failure); Assert.Contains("custom callbacks", failure!.Message, StringComparison.OrdinalIgnoreCase);
    }
    [Fact]
    public void UnprovenRadialLegendIdentitiesRejectWithoutReclassifyingEveryEntryAsAWholeSeries() {
        var series = new ChartSeries(new ChartSeriesId("parts"), "Parts", ChartSeriesKind.Pie, new[] { new ChartPoint(1, 60), new ChartPoint(2, 25), new ChartPoint(3, 10) })
            .WithObservationId(0, new ChartObservationId("primary"));
        var chart = Chart.Create().WithSize(760, 420).WithFontFamily(BrowserProofFonts.Family).WithLegend(true).AddSeries(series);
        var renderer = new HtmlInteractiveChartRenderer(); var ordinary = renderer.PrepareSection(chart); Exception? failure = null;
        try { _ = renderer.PrepareVisibilitySection(chart, View()); } catch (Exception exception) { failure = exception; }
        NativeRadialMotionBeforeControls.Capture(ordinary, "radial-unproven-identities"); NativeRadialMotionBeforeControls.Save("radial-unproven-identities.facts.json", JsonSerializer.Serialize(new { error = failure?.Message }));
        Assert.IsType<NotSupportedException>(failure); Assert.Contains("authored observation identity", failure!.Message, StringComparison.Ordinal);
    }
    internal static ChartVisibilityTarget Primary() => ChartVisibilityTarget.Observation(new ChartSeriesId("parts"), new ChartObservationId("primary"));
    internal static ChartVisibilityView View(params ChartVisibilityTarget[] hidden) => new(hidden, ChartVisibilityLayout.Reflow);
    internal static VisualSceneGroup[] Points(PreparedVisual prepared) => prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(row => row.Role == "radial-point").ToArray();
    private static VisualSceneGroup[] Legend(PreparedVisual prepared) => prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(row => row.Role is "legend-entry" or "legend-entry-omitted").ToArray();
    private static VisualSceneMark[] SliceMarks(PreparedVisual prepared) => prepared.Scene.Nodes.OfType<VisualSceneMark>().Where(row => row.Role is "pie-slice" or "donut-slice").ToArray();
}
