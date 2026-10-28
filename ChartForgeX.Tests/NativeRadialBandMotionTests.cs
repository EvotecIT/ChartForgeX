using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class NativeRadialBandMotionTests {
    [Theory] [InlineData("pie", true, "inside")] [InlineData("donut", false, "outside")] [InlineData("mixed", true, "outside")] [InlineData("mixed", false, "hidden")]
    public void EveryBandUsesItsOwnNativeMidpointOrderTypographyLegendAndSemanticFacts(string family, bool automatic, string labels) {
        var renderer = new HtmlInteractiveChartRenderer();
        HtmlChartSection Section(double inner, double outer) => renderer.PrepareSection(NativeRadialBandBeforeControls.Model(family, automatic, inner: inner, outer: outer, labels: labels));
        var from = Section(60, 45); var to = Section(80, 55); var middle = Section(70, 50);
        NativeRadialMotionBeforeControls.Capture(from,"bands-before-frame-from-"+family+"-"+automatic+"-"+labels); NativeRadialMotionBeforeControls.Capture(to,"bands-before-frame-to-"+family+"-"+automatic+"-"+labels); NativeRadialMotionBeforeControls.Capture(middle,"bands-before-frame-middle-"+family+"-"+automatic+"-"+labels);
        var frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null);
        var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.Values, 3));
        NativeRadialValueMotionTests.Capture(from, to, frames, payload, "bands-midpoint-" + family + "-" + automatic + "-" + labels); NativeRadialMotionBeforeControls.Capture(middle, "bands-independent-" + family + "-" + automatic + "-" + labels);
        Assert.Equal(middle.PreparedVisual.ToSvg(), frames[1].ToSvg()); Assert.Equal(middle.PreparedVisual.ToSvg("band-fixed"), frames[1].ToSvg("band-fixed")); Assert.Equal(middle.PreparedVisual.ToPng(), frames[1].ToPng());
        Assert.Equal(middle.PreparedVisual.Regions.Select(row => (row.Id, row.Role, row.Label)), frames[1].Regions.Select(row => (row.Id, row.Role, row.Label)));
        Assert.Equal(middle.PreparedVisual.Accessibility.Description, frames[1].Accessibility.Description);
        using var json = JsonDocument.Parse(payload); var readings = json.RootElement.GetProperty("readings").EnumerateArray().ToArray(); Assert.Equal(6, readings.Length);
        Assert.Equal(new[] { "parts", "parts", "parts", "outer", "outer", "outer" }, readings.Select(row => row.GetProperty("seriesKey").GetString()));
        Assert.Equal(2, readings.Count(row => row.GetProperty("observationId").GetString() == "primary")); Assert.Equal(to.PreparedVisual.ToPng(), frames[^1].ToPng());
    }
    [Fact]
    public void OneEditChoosesTheSecondBandWhileTheFirstDuplicateObservationRetainsItsValue() {
        var renderer = new HtmlInteractiveChartRenderer(); var from = renderer.PrepareSection(NativeRadialBandBeforeControls.Model()); var to = renderer.PrepareSection(NativeRadialBandBeforeControls.Model(outer: 55));
        var frames = VisualValueCapture.Frames(from.PreparedVisual, to.PreparedVisual, 3); var payload = renderer.PrepareValueTransitionJson(from, to, 3);
        NativeRadialValueMotionTests.Capture(from, to, frames, payload, "bands-legacy-second"); using var json = JsonDocument.Parse(payload);
        Assert.Equal("outer", json.RootElement.GetProperty("seriesKey").GetString()); Assert.Equal("primary", json.RootElement.GetProperty("observationId").GetString());
        var points = Points(frames[1]); Assert.Equal("60", points.Single(row => row.Metadata["data-cfx-series-key"] == "parts" && row.Metadata["data-cfx-observation-id"] == "primary").Metadata["data-cfx-value"]);
        Assert.Equal("50", points.Single(row => row.Metadata["data-cfx-series-key"] == "outer" && row.Metadata["data-cfx-observation-id"] == "primary").Metadata["data-cfx-value"]);
    }
    [Theory] [InlineData(true)] [InlineData(false)]
    public void ExplicitPositiveInitialBaselinesAndDetachedSourcesUseTheSamePlan(bool automatic) {
        var renderer = new HtmlInteractiveChartRenderer(); var chart = NativeRadialBandBeforeControls.Model(automatic: automatic); var from = renderer.PrepareSection(chart);
        var to = renderer.PrepareSection(NativeRadialBandBeforeControls.Model(automatic: automatic, inner: 80, outer: 55));
        var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.InitialValues, 3)); var frames = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null);
        NativeRadialValueMotionTests.Capture(from, to, frames, payload, "bands-detached-initial-" + automatic); var svg = frames[1].ToSvg(); var png = frames[1].ToPng();
        chart.Series[0].Points.Clear(); chart.Series[1].PointColors.Add(ChartColor.Black); chart.Options.MaximumPieSlices = 2; chart.WithValueFormat(ChartValueFormat.Custom(_ => "mutated"));
        try { FontRegistry.Clear(); var repeat = VisualValueCapture.NativeFrames(from.PreparedVisual, to.PreparedVisual, 3, null); Assert.Equal(svg, repeat[1].ToSvg()); Assert.Equal(png, repeat[1].ToPng());
            Assert.Equal(payload, renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.InitialValues, 3)));
        } finally { _ = BrowserProofFonts.Family; }
    }
    [Theory] [InlineData("pie", true)] [InlineData("donut", false)] [InlineData("mixed", true)]
    public void ReflowKeepsFullBandSlotsScopedOtherMembershipAndOriginalLegendFacts(string family, bool automatic) {
        var renderer = new HtmlInteractiveChartRenderer(); var chart = NativeRadialBandBeforeControls.Model(family, automatic, other: true);
        var from = renderer.PrepareVisibilitySection(chart, NativeRadialBandBeforeControls.View());
        var to = renderer.PrepareVisibilitySection(chart, NativeRadialBandBeforeControls.View(ChartVisibilityTarget.RadialOther(new ChartSeriesId("outer"))));
        var frames = VisualSeriesVisibilityCapture.ReflowFrames(from.PreparedVisual, to.PreparedVisual, 5); var payload = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.LegendVisibility, 5));
        NativeReflowVisibilityTests.Capture(from, to, frames, payload, "bands-other-" + family + "-" + automatic);
        var source = from.PreparedVisual.Scene.VisibilityCapture!.ReflowSource!; Assert.Equal(new[] { 2, 3, 4 }, source.RadialOtherMembers("parts")); Assert.Equal(new[] { 2, 3, 4 }, source.RadialOtherMembers("outer"));
        Assert.Equal(2, source.TargetCatalog().Count(row => row.Kind == ChartVisibilityTargetKind.RadialOther)); Assert.Equal(14, source.TargetCatalog().Count);
        var retained = Points(to.PreparedVisual); Assert.Equal(3, retained.Count(row => row.Metadata["data-cfx-series-key"] == "parts")); Assert.Equal(2, retained.Count(row => row.Metadata["data-cfx-series-key"] == "outer"));
        Assert.DoesNotContain(retained, row => row.Metadata["data-cfx-series-key"] == "outer" && row.Metadata["data-cfx-point"] == "-1");
        var fullLegend = from.PreparedVisual.Scene.Nodes.OfType<VisualSceneGroup>().Where(row => row.Role is "legend-entry" or "legend-entry-omitted").ToArray();
        var retainedLegend = to.PreparedVisual.Scene.Nodes.OfType<VisualSceneGroup>().Where(row => row.Role is "legend-entry" or "legend-entry-omitted").ToArray(); Assert.Equal(fullLegend.Length, retainedLegend.Length);
        var other = retainedLegend.Single(row => row.Metadata["data-cfx-series-key"] == "outer" && row.Metadata["data-cfx-point"] == "-1"); Assert.Equal("2,3,4", other.Metadata["data-cfx-source-points"]); Assert.Equal("true", other.Metadata["data-cfx-target-hidden"]);
        var ring = renderer.PrepareVisibilitySection(chart, NativeRadialBandBeforeControls.View(ChartVisibilityTarget.Series(new ChartSeriesId("outer")))); NativeRadialMotionBeforeControls.Capture(ring, "bands-empty-slot-" + family + "-" + automatic);
        var firstPoint = Points(from.PreparedVisual).First(row => row.Metadata["data-cfx-series-key"] == "parts"); var keptPoint = Points(ring.PreparedVisual).First();
        foreach (var key in new[] { "data-cfx-radial-inner-ratio", "data-cfx-radial-outer-ratio" }) Assert.Equal(firstPoint.Metadata[key], keptPoint.Metadata[key]);
        Assert.All(Points(ring.PreparedVisual), row => Assert.Equal("parts", row.Metadata["data-cfx-series-key"])); Assert.Empty(ring.PreparedVisual.Scene.VisibilityCapture!.RenderedSourceIndices(1)!);
        Assert.Equal(to.PreparedVisual.ToSvg(), frames[^1].Frame.ToSvg()); Assert.Equal(to.PreparedVisual.ToPng(), frames[^1].Frame.ToPng()); Assert.Empty(frames[2].Frame.Regions);
    }
    [Theory] [InlineData("order")] [InlineData("identity")] [InlineData("band")] [InlineData("zero")] [InlineData("aggregate")] [InlineData("callback")]
    public void IncompatibleNativeValueFactsRejectWhileOrdinaryNativeBandsRemainAvailable(string change) {
        var renderer = new HtmlInteractiveChartRenderer(); var chart = NativeRadialBandBeforeControls.Model(automatic: false, other: change == "aggregate"); var from = renderer.PrepareSection(chart);
        chart.Series[1].Points[0] = new ChartPoint(1, change == "zero" ? 0 : change == "order" ? 1 : 55);
        if (change == "identity") chart.Series[1].WithObservationId(0, new ChartObservationId("changed"));
        if (change == "band") chart.Series[1].WithRadialBand(new ChartRadialBand(.6, 1));
        if (change == "callback") chart.Options.PieSliceLabelFormatter = _ => "custom";
        var to = renderer.PrepareSection(chart); Exception? error = null; try { _ = renderer.PrepareNativeTransitionJson(from, to, new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.Values, 3)); } catch (Exception failure) { error = failure; }
        NativeRadialMotionBeforeControls.Capture(from, "bands-reject-" + change + "-from"); NativeRadialMotionBeforeControls.Capture(to, "bands-reject-" + change + "-to"); NativeRadialMotionBeforeControls.Save("bands-reject-" + change + ".facts.json", JsonSerializer.Serialize(new { error = error?.Message }));
        Assert.IsType<NotSupportedException>(error); Assert.NotEmpty(to.PreparedVisual.ToSvg());
    }
    [Theory] [InlineData("negative")] [InlineData("overlap")]
    public void HidingABandDoesNotLaunderAnInvalidFullNativeSource(string invalid) {
        var chart = NativeRadialBandBeforeControls.Model(automatic: false); if (invalid == "negative") chart.Series[1].Points[0] = new ChartPoint(1, -1); else chart.Series[1].WithRadialBand(new ChartRadialBand(.3, 1));
        var renderer = new HtmlInteractiveChartRenderer(); Exception? ordinary = null, hidden = null;
        try { _ = renderer.PrepareSection(chart); } catch (Exception failure) { ordinary = failure; }
        try { _ = renderer.PrepareVisibilitySection(chart, NativeRadialBandBeforeControls.View(ChartVisibilityTarget.Series(new ChartSeriesId("outer")))); } catch (Exception failure) { hidden = failure; }
        NativeRadialMotionBeforeControls.Save("bands-invalid-source-" + invalid + ".facts.json", JsonSerializer.Serialize(new { ordinary = ordinary?.GetType().Name, ordinaryMessage = ordinary?.Message, hidden = hidden?.GetType().Name, hiddenMessage = hidden?.Message }));
        Assert.NotNull(ordinary); Assert.NotNull(hidden);
    }
    [Fact]
    public void SameObservationIdentityInAnotherBandIsNotHiddenByScopedMembership() {
        var chart = NativeRadialBandBeforeControls.Model(); var renderer = new HtmlInteractiveChartRenderer();
        var from = renderer.PrepareVisibilitySection(chart, NativeRadialBandBeforeControls.View());
        var to = renderer.PrepareVisibilitySection(chart, NativeRadialBandBeforeControls.View(ChartVisibilityTarget.Observation(new ChartSeriesId("outer"),new ChartObservationId("primary"))));
        var payload = renderer.PrepareNativeTransitionJson(from,to,new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.LegendVisibility,3));
        NativeReflowVisibilityTests.Capture(from,to,VisualSeriesVisibilityCapture.ReflowFrames(from.PreparedVisual,to.PreparedVisual,3),payload,"bands-scoped-observation");
        Assert.Contains(Points(to.PreparedVisual),row=>row.Metadata["data-cfx-series-key"]=="parts" && row.Metadata["data-cfx-observation-id"]=="primary");
        Assert.DoesNotContain(Points(to.PreparedVisual),row=>row.Metadata["data-cfx-series-key"]=="outer" && row.Metadata["data-cfx-observation-id"]=="primary");
        Assert.Equal(new[] {1,2},to.PreparedVisual.Scene.VisibilityCapture!.RenderedSourceIndices(1)!);
        Assert.Equal(2,from.PreparedVisual.Scene.VisibilityCapture!.TargetCatalog.Count(row=>row.ObservationId?.Value=="primary"));
    }
    [Fact]
    public void UnpaintedZeroObservationPolicyStillBindsTheCompleteBandSourceIdentity() {
        Chart Model(bool changed) { var chart=NativeRadialBandBeforeControls.Model(labels:"hidden");chart.WithLegend(false);chart.Options.ShowDonutCenterLabel=false;
            chart.Series[1].Points[2]=new ChartPoint(3,0);if(changed)chart.Series[1].WithPointColor(2,ChartColor.Black);return chart; }
        var renderer=new HtmlInteractiveChartRenderer();var from=renderer.PrepareVisibilitySection(Model(false),NativeRadialBandBeforeControls.View());
        var other=renderer.PrepareVisibilitySection(Model(true),NativeRadialBandBeforeControls.View());
        var to=renderer.PrepareVisibilitySection(Model(true),NativeRadialBandBeforeControls.View(ChartVisibilityTarget.Series(new ChartSeriesId("outer"))));
        Exception? error=null;try{_=renderer.PrepareNativeTransitionJson(from,to,new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.LegendVisibility,3));}catch(Exception failure){error=failure;}
        NativeRadialMotionBeforeControls.Capture(from,"bands-hidden-key-from");NativeRadialMotionBeforeControls.Capture(other,"bands-hidden-key-other");
        NativeRadialMotionBeforeControls.Save("bands-hidden-key.facts.json",JsonSerializer.Serialize(new{first=from.PreparedVisual.Scene.VisibilityCapture!.SourceKey,second=other.PreparedVisual.Scene.VisibilityCapture!.SourceKey,error=error?.Message}));
        Assert.Equal(from.PreparedVisual.ToSvg("same-band-source"),other.PreparedVisual.ToSvg("same-band-source"));Assert.Equal(from.PreparedVisual.ToPng(),other.PreparedVisual.ToPng());
        Assert.NotEqual(from.PreparedVisual.Scene.VisibilityCapture!.SourceKey,other.PreparedVisual.Scene.VisibilityCapture!.SourceKey);Assert.IsType<NotSupportedException>(error);
    }
    [Fact]
    public void LaterBandLabelAndValueContextsUseActualCapturedFormatterAndCaseObservations() {
        Chart Model(double outer) {
            var chart=NativeRadialBandBeforeControls.Model(inner:600,outer:outer,labels:"outside").WithPieSliceLabelContent(ChartPieSliceLabelContent.LabelAndValue)
                .ConfigureDataLabelStyle(style=>style.TextCase=TextCaseTransform.ToggleCase);
            for(var index=0;index<chart.Series[1].Points.Count;index++){var point=chart.Series[1].Points[index];chart.Series[1].Points[index]=new ChartPoint(index+4,point.Y);}
            chart.ConfigureXAxis(axis=>{axis.Labels.Add(new ChartAxisLabel(4,"outer-delta"));axis.Labels.Add(new ChartAxisLabel(5,"outer-epsilon"));axis.Labels.Add(new ChartAxisLabel(6,"outer-zeta"));});return chart;
        }
        var renderer=new HtmlInteractiveChartRenderer();var from=renderer.PrepareSection(Model(800));var to=renderer.PrepareSection(Model(1200));var middle=renderer.PrepareSection(Model(1000));
        NativeRadialMotionBeforeControls.Capture(from,"bands-context-from");NativeRadialMotionBeforeControls.Capture(to,"bands-context-to");NativeRadialMotionBeforeControls.Capture(middle,"bands-context-independent");
        var frames=VisualValueCapture.NativeFrames(from.PreparedVisual,to.PreparedVisual,3,null);var payload=renderer.PrepareNativeTransitionJson(from,to,new HtmlNativeTransitionOptions(HtmlNativeTransitionEffect.Values,3));
        NativeRadialValueMotionTests.Capture(from,to,frames,payload,"bands-context");
        Assert.Equal(middle.PreparedVisual.ToSvg(),frames[1].ToSvg());Assert.Equal(middle.PreparedVisual.ToPng(),frames[1].ToPng());
        Assert.Contains(from.PreparedVisual.NativeTextCapture!.Requests.SelectMany(row=>row.Probes),row=>row.Owner=="native-radial-label-value-context" && row.Raw.StartsWith("outer-delta ",StringComparison.Ordinal));
    }
    private static VisualSceneGroup[] Points(PreparedVisual visual) => visual.Scene.Nodes.OfType<VisualSceneGroup>().Where(row => row.Role == "radial-point").ToArray();
}
