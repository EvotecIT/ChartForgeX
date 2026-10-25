using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeReflowVisibilityTests {
    [Theory]
    [InlineData("line", false)] [InlineData("line", true)] [InlineData("bar", false)] [InlineData("bar", true)]
    [InlineData("boxes", false)] [InlineData("boxes", true)] [InlineData("raw", false)] [InlineData("raw", true)]
    public void NativeMembershipDomainsSlotsLabelsAndLegendUseOneDetachedSource(string kind, bool groupLabels) {
        var chart = Model(kind, labels: true); var renderer = new HtmlInteractiveChartRenderer();
        var from = Section(renderer, chart, View(kind, false, groupLabels)); var to = Section(renderer, chart, View(kind, true, groupLabels));
        var frames = VisualSeriesVisibilityCapture.ReflowFrames(from.PreparedVisual, to.PreparedVisual, 5);
        var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.LegendVisibility, 5));
        Capture(from, to, frames, payload, kind + "-" + groupLabels);
        var reference = Reference(kind).WithDataLabels(true).WithLegend(true); reference.Options.ShowPointLegend = true;
        reference.UseVisibilitySourceLegend(from.PreparedVisual.Scene.VisibilityCapture!.ReflowSource!);
        var independentlyFiltered = renderer.PrepareSection(reference).PreparedVisual;
        NativeReflowVisibilityBeforeControls.Save("reflow-" + kind + "-" + groupLabels + ".reference.svg", independentlyFiltered.ToSvg());
        var last = to.PreparedVisual.Scene.VisibilityCapture!; var first = from.PreparedVisual.Scene.VisibilityCapture!;
        Assert.Equal(first.SourceKey, last.SourceKey); Assert.Equal(4, last.TargetCatalog.Count); Assert.Single(last.View.HiddenTargets);
        Assert.Equal(new[] { true, true, false, false, false }, frames.Select(row => row.Source)); Assert.Equal(new[] { 1d, .5, 0d, .5, 1d }, frames.Select(row => row.Opacity));
        Assert.Same(from.PreparedVisual, frames[0].Frame); Assert.Same(to.PreparedVisual, frames[4].Frame); Assert.Empty(frames[2].Frame.Regions);
        Assert.Equal(to.PreparedVisual.ToSvg(), frames[4].Frame.ToSvg()); Assert.Equal(to.PreparedVisual.ToPng(), frames[4].Frame.ToPng());
        var legend = to.PreparedVisual.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role is "legend-entry" or "legend-entry-omitted").ToArray();
        Assert.Equal(kind == "line" ? 1 : 3, legend.Length); Assert.Equal(kind == "line" ? 0 : 1, legend.Count(group => group.Metadata.ContainsKey("data-cfx-target-hidden")));
        var plot = PlotGroups(to.PreparedVisual); Assert.Equal(2, plot.Count);
        if (kind == "raw") { Assert.Equal(new[] { "earlier", "later" }, plot.Select(group => group.Metadata["data-cfx-group-id"]));
            Assert.Equal(new[] { "0", "2" }, plot.Select(group => group.Metadata["data-cfx-group-index"])); }
        else Assert.Equal(new[] { "earlier", "later" }, plot.Select(group => group.Metadata["data-cfx-observation-id"]));
        using var json = JsonDocument.Parse(payload); var drawing = json.RootElement.GetProperty("drawings").GetProperty("wide");
        Assert.Equal("source", drawing.GetProperty("frames")[1].GetProperty("basis").GetString()); Assert.Equal("destination", drawing.GetProperty("frames")[2].GetProperty("basis").GetString());
        Assert.Equal("native-visibility-reflow", json.RootElement.GetProperty("sourceKind").GetString());
        // Native domains and slot layout are independently prepared from actual visible values; full retained legend changes only frame allocation.
        Assert.Equal(independentlyFiltered.XProjection!.Minimum, to.PreparedVisual.XProjection!.Minimum); Assert.Equal(independentlyFiltered.YProjection!.Minimum, to.PreparedVisual.YProjection!.Minimum);
        Assert.Equal(independentlyFiltered.YProjection!.Maximum, to.PreparedVisual.YProjection!.Maximum);
    }
    [Theory] [InlineData("horizontal-line")] [InlineData("horizontal-boxes")] [InlineData("calendar")] [InlineData("secondary-slots")]
    public void ReflowReusesPhysicalAxisCalendarAndGroupedSlotOwners(string policy) {
        var kind = policy == "horizontal-boxes" ? "boxes" : policy == "secondary-slots" ? "bar" : "line";
        var chart = Model(kind).WithSize(390, 420); chart.Options.ShowPointLegend = false;
        if (policy == "horizontal-line") chart.Options.LineOrientation = ChartOrientation.Horizontal;
        if (policy == "horizontal-boxes") chart.ConfigureBoxPlot(boxes => boxes.Orientation = ChartOrientation.Horizontal);
        if (policy == "calendar") { var start = new DateTime(2026, 1, 1).ToOADate(); for (var index = 0; index < chart.Series[0].Points.Count; index++) chart.Series[0].Points[index] = new ChartPoint(start + index, chart.Series[0].Points[index].Y);
            chart.ConfigureXAxis(axis => { axis.Scale = ChartScaleKind.Time; axis.TimeSpacing = ChartTimeSpacing.Unit(new ChartTimeTickInterval(ChartTimeTickUnit.Day)); }); }
        ChartVisibilityView Last() => View(kind, true);
        if (policy == "secondary-slots") { var extra = ChartSeries.CreateBar(new ChartSeriesId("alternate"), "Alternate", new[] { new ChartPoint(1, 200), new ChartPoint(2, 400), new ChartPoint(3, 300) }).UseSecondaryValueAxis();
            extra.WithObservationIds(new[] { new ChartObservationId("earlier"), new ChartObservationId("current"), new ChartObservationId("later") }); chart.AddSeries(extra); chart.Options.MaximumBarWidth = 200; }
        var renderer = new HtmlInteractiveChartRenderer(); var from = Section(renderer, chart, View(kind, false));
        var last = policy == "secondary-slots" ? new ChartVisibilityView(new[] { new ChartSeriesId("alternate") }, ChartVisibilityLabelLayering.PreserveOverlay, ChartVisibilityLayout.Reflow) : Last();
        var to = Section(renderer, chart, last); Capture(from, to, VisualSeriesVisibilityCapture.ReflowFrames(from.PreparedVisual, to.PreparedVisual, 5), renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.LegendVisibility, 5)), policy);
        Assert.Equal(from.PreparedVisual.Scene.VisibilityCapture!.SourceKey, to.PreparedVisual.Scene.VisibilityCapture!.SourceKey);
        Assert.Equal(policy == "secondary-slots" ? 3 : 2, PlotGroups(to.PreparedVisual).Count);
        if (policy.StartsWith("horizontal", StringComparison.Ordinal)) Assert.Equal(ChartOrientation.Horizontal, to.PreparedVisual.CartesianOrientation);
        if (policy == "calendar") Assert.Equal(ChartScaleKind.Time, to.PreparedVisual.XProjection!.Scale);
        if (policy == "secondary-slots") { Assert.NotNull(from.PreparedVisual.SecondaryYProjection); Assert.Null(to.PreparedVisual.SecondaryYProjection);
            var firstBody = from.PreparedVisual.Scene.Nodes.OfType<VisualSceneRectangle>().First(row => row.Role == "bar"); var lastBody = to.PreparedVisual.Scene.Nodes.OfType<VisualSceneRectangle>().First(row => row.Role == "bar");
            Assert.True(lastBody.Bounds.Width > firstBody.Bounds.Width); }
    }
    [Fact]
    public void RawOutliersKeepOriginalSampleProvenanceAcrossHiddenGroupRecalculation() {
        var chart = Model("raw"); chart.Series[0].UpdateBoxPlotSampleGroup(2, new ChartBoxPlotSampleGroup(3, new double[] { 4, 5, 5, 5, 5, 6, 100 })).ConfigureBoxPlotStatistics(options => options.WhiskerPolicy = ChartBoxPlotWhiskerPolicy.Iqr);
        chart.Series[0].WithObservationIds(Enumerable.Range(0, chart.Series[0].BoxPlotSourceSamples.Count).Select(index => new ChartObservationId("sample-" + index)));
        var renderer = new HtmlInteractiveChartRenderer(); var from = Section(renderer, chart, View("raw", false)); var to = Section(renderer, chart, View("raw", true));
        Capture(from, to, VisualSeriesVisibilityCapture.ReflowFrames(from.PreparedVisual, to.PreparedVisual, 5), renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.LegendVisibility, 5)), "raw-outlier-source");
        var outliers = PlotGroups(to.PreparedVisual).Where(row => row.Metadata.TryGetValue("data-cfx-kind", out var kind) && kind == "BoxPlotOutlier")
            .OrderBy(row => int.Parse(row.Metadata["data-cfx-source-point"], System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        var originals = PlotGroups(from.PreparedVisual).Where(row => row.Metadata.TryGetValue("data-cfx-kind", out var kind) && kind == "BoxPlotOutlier")
            .OrderBy(row => int.Parse(row.Metadata["data-cfx-source-point"], System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        NativeReflowVisibilityBeforeControls.Save("reflow-raw-outlier-provenance.facts.json", JsonSerializer.Serialize(new { before = originals.Select(row => row.Metadata), after = outliers.Select(row => row.Metadata) }));
        Assert.Equal(2, outliers.Length); Assert.Equal(new[] { "14", "20" }, outliers.Select(row => row.Metadata["data-cfx-source-point"]));
        Assert.Equal(new[] { "sample-14", "sample-20" }, outliers.Select(row => row.Metadata["data-cfx-observation-id"]));
        Assert.Equal(new[] { "4", "100" }, outliers.Select(row => row.Metadata["data-cfx-y"])); Assert.All(outliers, row => Assert.Equal("later", row.Metadata["data-cfx-group-id"]));
        Assert.Equal(originals.Select(row => (row.Metadata["data-cfx-source-point"], row.Metadata["data-cfx-observation-id"], row.Metadata["data-cfx-group-id"], row.Metadata["data-cfx-y"])),
            outliers.Select(row => (row.Metadata["data-cfx-source-point"], row.Metadata["data-cfx-observation-id"], row.Metadata["data-cfx-group-id"], row.Metadata["data-cfx-y"])));
        var outlier = Assert.Single(outliers, row => row.Metadata["data-cfx-source-point"] == "20");
        Assert.Equal("20", outlier.Metadata["data-cfx-source-point"]); Assert.Equal("sample-20", outlier.Metadata["data-cfx-observation-id"]); Assert.Equal("later", outlier.Metadata["data-cfx-group-id"]);
    }
    [Theory] [InlineData("raw")] [InlineData("line")]
    public void FullSourceKeyRejectsChangedUnpaintedRawOrPointPaintDespiteIdenticalNativeSvg(string kind) {
        var renderer = new HtmlInteractiveChartRenderer(); var a = Model(kind).WithLegend(false); var b = Model(kind, changedUnpainted: true).WithLegend(false);
        a.Options.ShowPointLegend = false; b.Options.ShowPointLegend = false;
        var first = Section(renderer, a, View(kind, false)); var unchanged = Section(renderer, b, View(kind, false)); var last = Section(renderer, b, View(kind, true));
        NativeReflowVisibilityBeforeControls.Capture(first, "reflow-key-" + kind + "-a"); NativeReflowVisibilityBeforeControls.Capture(unchanged, "reflow-key-" + kind + "-b");
        var keys = new { before = first.PreparedVisual.Scene.VisibilityCapture!.SourceKey, after = unchanged.PreparedVisual.Scene.VisibilityCapture!.SourceKey };
        Exception? failure = null; try { _ = renderer.PrepareNativeTransitionJson(first, last, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.LegendVisibility, 5)); } catch (Exception exception) { failure = exception; }
        NativeReflowVisibilityBeforeControls.Save("reflow-key-" + kind + ".facts.json", JsonSerializer.Serialize(new { keys, error = failure?.Message }));
        Assert.Equal(first.PreparedVisual.ToSvg("same-painted-source"), unchanged.PreparedVisual.ToSvg("same-painted-source")); Assert.Equal(first.PreparedVisual.ToPng(), unchanged.PreparedVisual.ToPng());
        Assert.NotEqual(keys.before, keys.after); Assert.IsType<NotSupportedException>(failure);
    }
    [Theory] [InlineData("bar")] [InlineData("raw")]
    public void RetainedLegendKeepsFullSourceAuthoredLabelsAndReadoutsAfterFilteredOrdinalShift(string kind) {
        var chart = Model(kind); chart.Series[0].WithPointLabel(0, "Earlier authored").WithPointLabel(1, "Current authored").WithPointLabel(2, "Later authored");
        var renderer = new HtmlInteractiveChartRenderer(); var from = Section(renderer, chart, View(kind, false)); var to = Section(renderer, chart, View(kind, true));
        Capture(from, to, VisualSeriesVisibilityCapture.ReflowFrames(from.PreparedVisual, to.PreparedVisual, 5), renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.LegendVisibility, 5)), "legend-source-" + kind);
        var legend = to.PreparedVisual.Scene.Nodes.OfType<VisualSceneGroup>().Single(row => row.Role == "legend-entry"
            && row.Metadata.TryGetValue(kind == "raw" ? "data-cfx-group-id" : "data-cfx-observation-id", out var id) && id == "current");
        var original = from.PreparedVisual.Scene.Nodes.OfType<VisualSceneGroup>().Single(row => row.Role == "legend-entry"
            && row.Metadata.TryGetValue(kind == "raw" ? "data-cfx-group-id" : "data-cfx-observation-id", out var id) && id == "current");
        foreach (var row in new[] { (Id: "earlier", Label: "Earlier authored"), (Id: "current", Label: "Current authored"), (Id: "later", Label: "Later authored") }) {
            var retainedEntry = to.PreparedVisual.Scene.Nodes.OfType<VisualSceneGroup>().Single(group => group.Role == "legend-entry"
                && group.Metadata.TryGetValue(kind == "raw" ? "data-cfx-group-id" : "data-cfx-observation-id", out var id) && id == row.Id);
            var sourceEntry = from.PreparedVisual.Scene.Nodes.OfType<VisualSceneGroup>().Single(group => group.Role == "legend-entry"
                && group.Metadata.TryGetValue(kind == "raw" ? "data-cfx-group-id" : "data-cfx-observation-id", out var id) && id == row.Id);
            Assert.Equal(row.Label, retainedEntry.Metadata["data-cfx-authored-label"]); Assert.Equal(row.Label, sourceEntry.Metadata["data-cfx-authored-label"]);
            Assert.Equal(sourceEntry.Metadata["data-cfx-label"], retainedEntry.Metadata["data-cfx-label"]);
        }
        Assert.Equal("Current authored", legend.Metadata["data-cfx-authored-label"]); Assert.Equal(original.Metadata["data-cfx-label"], legend.Metadata["data-cfx-label"]);
        if (kind == "bar") Assert.Equal("80", legend.Metadata["data-cfx-y"]);
    }
    [Fact]
    public void CapturedReflowSourceSurvivesCallerCollectionStyleAxisAndFontRegistryMutation() {
        var renderer = new HtmlInteractiveChartRenderer(); var chart = Model("raw", labels: true); var from = Section(renderer, chart, View("raw", false)); var to = Section(renderer, chart, View("raw", true));
        var frames = VisualSeriesVisibilityCapture.ReflowFrames(from.PreparedVisual, to.PreparedVisual, 5); var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.LegendVisibility, 5));
        Capture(from, to, frames, payload, "detached"); var svg = frames.Select(row => row.Frame.ToSvg()).ToArray(); var png = frames.Select(row => row.Frame.ToPng()).ToArray();
        chart.Series[0].ReplaceBoxPlotSampleGroups(new[] { new ChartBoxPlotSampleGroup(99, new double[] { -9, 99 }) }); chart.Series[0].WithColor(ChartColor.Black);
        chart.ConfigureYAxis(axis => axis.Maximum = 999).WithFontFamily("Changed after capture").WithValueFormat(ChartValueFormat.Custom(_ => "changed"));
        try { FontRegistry.Clear(); var repeated = VisualSeriesVisibilityCapture.ReflowFrames(from.PreparedVisual, to.PreparedVisual, 5);
            Assert.Equal(svg, repeated.Select(row => row.Frame.ToSvg())); for (var index = 0; index < repeated.Count; index++) Assert.Equal(png[index], repeated[index].Frame.ToPng());
            Assert.Equal(payload, renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.LegendVisibility, 5)));
        } finally { _ = BrowserProofFonts.Family; }
    }
    [Fact]
    public void RawGroupAndSampleNamespacesRemainDistinctAndUnknownMembershipRejectsBeforeNativePaint() {
        var chart = Model("raw"); chart.Series[0].WithObservationId(0, new ChartObservationId("current")); var renderer = new HtmlInteractiveChartRenderer();
        var from = Section(renderer, chart, View("raw", false)); var to = Section(renderer, chart, View("raw", true));
        Capture(from, to, VisualSeriesVisibilityCapture.ReflowFrames(from.PreparedVisual, to.PreparedVisual, 3), renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.LegendVisibility, 3)), "namespace");
        Assert.DoesNotContain(from.PreparedVisual.Scene.VisibilityCapture!.TargetCatalog, row => row.Kind == ChartVisibilityTargetKind.Observation);
        Assert.Throws<ArgumentException>(() => Section(renderer, chart, new ChartVisibilityView(new[] { ChartVisibilityTarget.Observation(new ChartSeriesId("delivery"), new ChartObservationId("current")) }, ChartVisibilityLayout.Reflow)));
        Assert.Equal(2, PlotGroups(to.PreparedVisual).Count);
    }
    [Fact]
    public void WholeSeriesReflowCanBecomeNativeNoDataWithoutKeepingHiddenHitRegions() {
        var chart = Model("bar"); var renderer = new HtmlInteractiveChartRenderer(); var from = Section(renderer, chart, View("bar", false));
        var to = Section(renderer, chart, new ChartVisibilityView(new[] { new ChartSeriesId("delivery") }, ChartVisibilityLabelLayering.PreserveOverlay, ChartVisibilityLayout.Reflow));
        Capture(from, to, VisualSeriesVisibilityCapture.ReflowFrames(from.PreparedVisual, to.PreparedVisual, 3), renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.LegendVisibility, 3)), "all-hidden");
        Assert.Empty(PlotGroups(to.PreparedVisual)); Assert.DoesNotContain(to.PreparedVisual.Regions, row => row.Role == "point"); Assert.Single(to.PreparedVisual.Scene.VisibilityCapture!.SeriesKeys);
        Assert.Equal(3, to.PreparedVisual.Scene.Nodes.OfType<VisualSceneGroup>().Count(group => group.Role is "legend-entry" or "legend-entry-omitted"));
    }
    [Fact]
    public void UnsupportedSourcePoliciesAndIncompatibleViewsRejectWhileOrdinaryRenderingStaysCallable() {
        var renderer = new HtmlInteractiveChartRenderer(); var chart = Model("line"); var before = Section(renderer, chart, View("line", false));
        var changes = new Action<Chart>[] { value => value.WithValueFormat(ChartValueFormat.Custom(number => number.ToString())), value => value.ConfigureLabels(labels => labels.AccessibleTextFormatter = _ => "Captured accessible source") };
        var errors = new List<string>(); foreach (var change in changes) { var changed = Model("line"); change(changed); NativeReflowVisibilityBeforeControls.Capture(renderer.PrepareSection(changed), "reflow-unsupported-" + errors.Count);
            try { _ = Section(renderer, changed, View("line", false)); errors.Add("accepted"); } catch (NotSupportedException exception) { errors.Add(exception.Message); } }
        NativeReflowVisibilityBeforeControls.Save("reflow-unsupported.facts.json", JsonSerializer.Serialize(errors)); Assert.DoesNotContain("accepted", errors);
        Assert.Throws<NotSupportedException>(() => renderer.PrepareVisibilitySection(chart, new ChartVisibilityView(new[] { ChartVisibilityTarget.Observation(new ChartSeriesId("delivery"), new ChartObservationId("current")) }, ChartVisibilityLayout.Preserve)));
        var same = Section(renderer, chart, View("line", false)); Assert.Throws<NotSupportedException>(() => VisualSeriesVisibilityCapture.ReflowFrames(before.PreparedVisual, same.PreparedVisual, 5));
        var after = Section(renderer, chart, View("line", true)); Assert.Throws<ArgumentOutOfRangeException>(() => VisualSeriesVisibilityCapture.ReflowFrames(before.PreparedVisual, after.PreparedVisual, 2));
    }
    internal static Chart Model(string kind, bool changedUnpainted = false, bool labels = false) {
        var chart = NativeReflowVisibilityBeforeControls.Model(kind, changedUnpainted).WithLegend(true).WithDataLabels(labels);
        chart.Options.ShowPointLegend = true; return chart;
    }
    internal static ChartVisibilityView View(string kind, bool hidden, bool grouping = false) => new(hidden ? new[] {
        kind == "raw" ? ChartVisibilityTarget.BoxPlotGroup(new ChartSeriesId("delivery"), new ChartObservationId("current"))
            : ChartVisibilityTarget.Observation(new ChartSeriesId("delivery"), new ChartObservationId("current")) } : Array.Empty<ChartVisibilityTarget>(),
        ChartVisibilityLayout.Reflow, grouping ? ChartVisibilityLabelLayering.GroupWithSeries : ChartVisibilityLabelLayering.PreserveOverlay);
    internal static HtmlChartSection Section(HtmlInteractiveChartRenderer renderer, Chart chart, ChartVisibilityView view) => renderer.PrepareVisibilitySection(chart, view);
    private static List<VisualSceneGroup> PlotGroups(PreparedVisual value) => value.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "point").ToList();
    private static Chart Reference(string kind) {
        var original = NativeReflowVisibilityBeforeControls.Model(kind); ChartSeries series;
        if (kind == "raw") series = ChartSeries.CreateBoxPlot(new ChartSeriesId("delivery"), "Delivery", original.Series[0].BoxPlotSourceGroups.Where((_, index) => index != 1));
        else if (kind == "boxes") series = ChartSeries.CreateBoxPlot(new ChartSeriesId("delivery"), "Delivery", original.Series[0].BoxPlots.Where((_, index) => index != 1));
        else series = new ChartSeries(new ChartSeriesId("delivery"), "Delivery", original.Series[0].Kind, original.Series[0].Points.Where((_, index) => index != 1));
        return Chart.Create().WithSize(760, 420).WithLegend(false).WithLineMarkers(ChartLineMarkerMode.None).AddSeries(series);
    }
    internal static void Capture(HtmlChartSection from, HtmlChartSection to, IReadOnlyList<(PreparedVisual Frame, bool Source, double Opacity)> frames, string payload, string name) {
        NativeReflowVisibilityBeforeControls.Capture(from, "native-reflow-" + name + "-from"); NativeReflowVisibilityBeforeControls.Capture(to, "native-reflow-" + name + "-to");
        NativeReflowVisibilityBeforeControls.Save("native-reflow-" + name + ".transition.json", payload);
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        for (var index = 0; index < frames.Count; index++) { File.WriteAllText(Path.Combine(output, "native-reflow-" + name + "-" + index + ".svg"), frames[index].Frame.ToSvg()); File.WriteAllBytes(Path.Combine(output, "native-reflow-" + name + "-" + index + ".png"), frames[index].Frame.ToPng()); }
    }
}
