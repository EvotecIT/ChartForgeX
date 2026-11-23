using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class PieDonutAutomaticBandTests {
    [Theory]
    [InlineData(false, 390)]
    [InlineData(true, 390)]
    [InlineData(false, 900)]
    [InlineData(true, 900)]
    public void AutomaticBandsUseSourceOrderIndependentTotalsAndTheExistingNativeGeometry(bool dark, int width) {
        var chart = Fixture(dark, width); var prepared = PieDonutRadiusDepthTests.Prepare(chart);
        var slices = prepared.Scene.Nodes.OfType<VisualSceneSlice>().ToArray(); Assert.Equal(9, slices.Length);
        var full = slices.Single(mark => mark.Id == "series-2-point-2").Outer;
        var expected = new[] { (.2, .44), (.48, .72), (.76, 1d) };
        for (var index = 0; index < expected.Length; index++) {
            var sample = slices.Single(mark => mark.Id == "series-" + index + "-point-2");
            Assert.Equal(full * expected[index].Item1, sample.Inner, 8); Assert.Equal(full * expected[index].Item2, sample.Outer, 8);
            Assert.Null(chart.Series[index].RadialBand); Assert.Equal(8d * (index + 1), chart.Series[index].Points.Sum(point => point.Y));
            Assert.Equal(Math.PI, slices.Single(mark => mark.Id == "series-" + index + "-point-0").Sweep, 10);
        }
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        chart.RadialBandAllocation((ChartRadialBandAllocation?)null);
        for (var index = 0; index < expected.Length; index++) chart.Series[index].RadialBand(new ChartRadialBand(expected[index].Item1, expected[index].Item2));
        Assert.Equal(png, PieDonutRadiusDepthTests.Prepare(chart).ToPng());
        Assert.Equal(svg, prepared.ToSvg());
        var first = chart.Series[0]; chart.RadialBandAllocation(new ChartRadialBandAllocation(.2, .04));
        foreach (var series in chart.Series) series.RadialBand((ChartRadialBand?)null);
        chart.MoveSeries(first, 2);
        var envelope = chart.ToVisualArtifact().ToInterchangeEnvelope();
        Assert.Equal("period-1", envelope.Extensions["chart.series.0.id"]); Assert.Equal("period-0", envelope.Extensions["chart.series.2.id"]);
        Assert.Equal(.2, double.Parse(envelope.Extensions["chart.series.0.radial.innerRatio"], System.Globalization.CultureInfo.InvariantCulture), 9);
        Assert.Equal(.76, double.Parse(envelope.Extensions["chart.series.2.radial.innerRatio"], System.Globalization.CultureInfo.InvariantCulture), 9);
        NumericRadialEncodedGeometryTests.Capture(chart, "automatic-bands-" + width + (dark ? "-dark" : "-light"));
    }

    [Fact]
    public void AllocationBoundsWrongFamiliesAndExplicitMixturesRejectWithoutMutatingAuthoredBands() {
        foreach (var pair in new[] { (-.1, 0d), (1d, 0d), (0d, 1d), (double.NaN, 0d), (0d, double.PositiveInfinity) })
            Assert.Throws<ArgumentOutOfRangeException>(() => new ChartRadialBandAllocation(pair.Item1, pair.Item2));
        var chart = Fixture(false, 700); var allocation = chart.Options.RadialBandAllocation;
        chart.Series[0].RadialBand(new ChartRadialBand(.1, .2));
        Assert.Contains("mixed", Assert.Throws<InvalidOperationException>(() => PieDonutRadiusDepthTests.Prepare(chart)).Message);
        Assert.Same(allocation, chart.Options.RadialBandAllocation); Assert.Equal(.1, chart.Series[0].RadialBand!.InnerRatio);
        chart.Series[0].RadialBand((ChartRadialBand?)null); chart.RadialBandAllocation(new ChartRadialBandAllocation(.5, .3));
        Assert.Contains("positive-width", Assert.Throws<InvalidOperationException>(() => PieDonutRadiusDepthTests.Prepare(chart)).Message);
        Assert.All(chart.Series, series => Assert.Null(series.RadialBand));
        var wrong = Chart.Create().RadialBandAllocation(new ChartRadialBandAllocation()).AddLine("Line", new[] { new ChartPoint(1, 2) });
        Assert.Throws<InvalidOperationException>(() => PieDonutRadiusDepthTests.Prepare(wrong));
    }

    [Fact]
    public void EmptyInnerRingKeepsItsSlotAndDoesNotBorrowAnotherRingsCenter() {
        var chart = Fixture(false, 700).Pie(p => p.ShowCenterLabel()); chart.Series[0].Points.Clear();
        var prepared = PieDonutRadiusDepthTests.Prepare(chart);
        Assert.Equal(6, prepared.Scene.Nodes.OfType<VisualSceneSlice>().Count());
        Assert.DoesNotContain(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.Role == "donut-center");
        Assert.Contains(prepared.Scene.Diagnostics, diagnostic => diagnostic.Code == "radial.no-data");
        var facts = chart.ToVisualArtifact().ToInterchangeEnvelope();
        Assert.Equal("0.2", facts.Extensions["chart.series.0.radial.innerRatio"]);
        using var source = JsonDocument.Parse(facts.Extensions["chart.series.0.radial.sourceFacts"]); Assert.Equal(0, source.RootElement.GetArrayLength());
    }

    [Theory]
    [InlineData(ChartDataLabelPlacement.Outside)]
    [InlineData(ChartDataLabelPlacement.Left)]
    [InlineData(ChartDataLabelPlacement.Right)]
    [InlineData(ChartDataLabelPlacement.Above)]
    [InlineData(ChartDataLabelPlacement.Below)]
    public void AutomaticAllocationSupportsInnerExternalCaptionsAndActualMultiRingExplosion(ChartDataLabelPlacement placement) {
        var chart = Fixture(false, 700); chart.Series[0].ShowDataLabels(true); chart.Series[0].DataLabelPlacement = placement;
        MultiRingDepthTests.AssertCalloutAssociations(PieDonutRadiusDepthTests.Prepare(chart));
        chart.Series[0].ShowDataLabels(false); chart.Series[2].ShowDataLabels(true); chart.Series[2].DataLabelPlacement = placement;
        Assert.True(PieDonutRadiusDepthTests.Prepare(chart).ToPng().Length > 100);
        chart.Series[1].PointSliceOffset(0, .1);
        var displaced = PieDonutRadiusDepthTests.Prepare(chart);
        Assert.Equal(9, displaced.Scene.Nodes.OfType<VisualSceneSlice>().Count()); Assert.Equal(.1, chart.Series[1].PointSliceOffsets[0]);
    }

    [Fact]
    public void CanonicalPortableFactsRefreshEditsAndResetWithEscapedIdentitiesAndDetachedSnapshots() {
        var chart = Fixture(false, 700); chart.Series[0].ObservationId(0, new ChartObservationId("category,\"A\""));
        chart.Series[0].PointSliceRadiusRatio(0, .25);
        var artifact = chart.ToVisualArtifact("facts"); artifact.Metadata["consumer.note"] = "Retained";
        var saved = artifact.ToInterchangeEnvelope(); Assert.Equal(VisualArtifactInterchangeFamily.None, saved.Family);
        using var facts = JsonDocument.Parse(saved.Extensions["chart.series.0.radial.sourceFacts"]);
        Assert.Equal("category,\"A\"", facts.RootElement[0].GetProperty("observationId").GetString()); Assert.Equal(0, facts.RootElement[0].GetProperty("sourceIndex").GetInt32());
        Assert.Equal(4, facts.RootElement[0].GetProperty("value").GetDouble()); Assert.Equal(.25, facts.RootElement[0].GetProperty("radiusRatio").GetDouble());
        var json = saved.ToJson(); Assert.Equal(json, VisualArtifactInterchangeEnvelope.FromUtf8Json(saved.ToUtf8Json()).ToJson());
        var captured = PieDonutRadiusDepthTests.Prepare(chart).ToArtifact("facts", VisualArtifactKind.Chart, saved); var capturedJson = captured.ToInterchangeJson();
        chart.Series[0].PointSliceRadiusRatio(0, .6); chart.Series[0].Points[0] = new ChartPoint(9, 11); chart.Options.RadialBandAllocation = new ChartRadialBandAllocation(.1, .02);
        using var edited = JsonDocument.Parse(artifact.ToInterchangeEnvelope().Extensions["chart.series.0.radial.sourceFacts"]);
        Assert.Equal(.6, edited.RootElement[0].GetProperty("radiusRatio").GetDouble());
        Assert.Equal(9, edited.RootElement[0].GetProperty("x").GetDouble()); Assert.Equal(11, edited.RootElement[0].GetProperty("value").GetDouble());
        Assert.Equal("0.1", artifact.ToInterchangeEnvelope().Extensions["chart.radial.auto.innerRatio"]);
        Assert.Equal("Retained", artifact.ToInterchangeEnvelope().Extensions["consumer.note"]);
        Assert.Equal(json, saved.ToJson()); Assert.Equal(capturedJson, captured.ToInterchangeJson()); Assert.True(captured.ToPng().Length > 100);
        while (chart.Series.Count > 1) chart.RemoveSeries(chart.Series[chart.Series.Count - 1]);
        chart.Series[0].ResetPointSliceRadius(0); chart.RadialBandAllocation((ChartRadialBandAllocation?)null);
        var reset = artifact.ToInterchangeEnvelope(); Assert.DoesNotContain("chart.radial.auto.innerRatio", reset.Extensions.Keys);
        Assert.DoesNotContain("chart.series.0.radial.sourceFacts", reset.Extensions.Keys);
        Assert.DoesNotContain("chart.series.0.radial.innerRatio", reset.Extensions.Keys);
    }

    [Fact]
    public void PortableSourceFactsFailExistingTextAndEntryBudgetsWithoutPartialSourceChanges() {
        var source = new ChartSeries(new ChartSeriesId("dense"), "Dense", ChartSeriesKind.Pie,
            Enumerable.Range(0, 2000).Select(index => new ChartPoint(index, 1))).PointSliceRadiusRatio(0, .5);
        var chart = Chart.Create().AddSeries(source);
        Assert.Contains("text budget", Assert.Throws<ArgumentException>(() => chart.ToVisualArtifact()).Message);
        Assert.Equal(2000, source.ObservationCount); Assert.Equal(.5, source.PointSliceRadiusRatios[0]);
        var many = Chart.Create().RadialBandAllocation(new ChartRadialBandAllocation(.2));
        for (var index = 0; index < 110; index++) many.AddSeries(new ChartSeries(new ChartSeriesId("s" + index), "S", ChartSeriesKind.Pie, new[] { new ChartPoint(1, 1) }));
        Assert.Contains("entry budget", Assert.Throws<ArgumentException>(() => many.ToVisualArtifact()).Message);
        Assert.Equal(110, many.Series.Count); Assert.All(many.Series, series => Assert.Null(series.RadialBand));
    }

    [Fact]
    public void PortableSourceIdentityLimitsRejectBeforeEncodingWithoutChangingAuthoredFacts() {
        var chart = Fixture(false, 700); var series = chart.Series[0];
        var oversized = new string('x', 513); series.ObservationId(0, new ChartObservationId(oversized));
        Assert.Contains("identity budget", Assert.Throws<ArgumentException>(() => chart.ToVisualArtifact()).Message);
        Assert.Equal(oversized, series.ObservationIds[0].Value);
        var oversizedSeries = PieDonutRadiusDepthTests.Source(oversized, ChartSeriesKind.Pie).PointSliceRadiusRatio(0, .5);
        Assert.Contains("identity budget", Assert.Throws<ArgumentException>(() => Chart.Create().AddSeries(oversizedSeries).ToVisualArtifact()).Message);
        series.ObservationId(0, new ChartObservationId("broken-\ud800"));
        Assert.Contains("UTF-16", Assert.Throws<ArgumentException>(() => chart.ToVisualArtifact()).Message);
        series.ObservationId(0, new ChartObservationId("fixed"));
        var saved = chart.ToVisualArtifact().ToInterchangeEnvelope();
        using var facts = JsonDocument.Parse(saved.Extensions["chart.series.0.radial.sourceFacts"]);
        Assert.Equal("fixed", facts.RootElement[0].GetProperty("observationId").GetString());
    }

    [Fact]
    public void PortableSourceAggregateBudgetRejectsBeforeBuildingAnOversizedFactArray() {
        var count = VisualArtifactInterchangeEnvelope.MaximumJsonValues / 6 + 1;
        var series = new ChartSeries(new ChartSeriesId("aggregate-budget"), "Budget", ChartSeriesKind.Pie,
            Enumerable.Range(0, count).Select(index => new ChartPoint(index, 1))).PointSliceRadiusRatio(0, .5);
        Assert.Contains("aggregate value budget", Assert.Throws<ArgumentException>(() => Chart.Create().AddSeries(series).ToVisualArtifact()).Message);
        Assert.Equal(count, series.ObservationCount); Assert.Equal(.5, series.PointSliceRadiusRatios[0]);
    }

    internal static Chart Fixture(bool dark, int width) {
        var chart = Chart.Create().Size(width, width < 500 ? 500 : 560).ShowHeader(false).Legend(false)
            .Pie(p => p.ShowCenterLabel(false)).Theme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family)).ThemeMode(dark ? ThemeMode.Dark : ThemeMode.Light)
            .RadialBandAllocation(new ChartRadialBandAllocation(.2, .04)).XLabels("Alpha", "Beta", "Gamma");
        for (var index = 0; index < 3; index++) {
            var series = PieDonutRadiusDepthTests.Source("period-" + index, ChartSeriesKind.Donut).ShowDataLabels(false);
            for (var point = 0; point < series.Points.Count; point++)
                series.Points[point] = new ChartPoint(series.Points[point].X, series.Points[point].Y * (index + 1));
            if (index == 2) series.PointSliceRadiusRatio(0, .35).PointSliceRadiusRatio(1, .7).PointSliceRadiusRatio(2, 1);
            chart.AddSeries(series);
        }
        return chart;
    }
}
