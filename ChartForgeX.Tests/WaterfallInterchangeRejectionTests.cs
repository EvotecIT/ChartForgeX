using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class WaterfallInterchangeRejectionTests {
    [Fact]
    public void AuthoringModesCheckpointsAndOpeningRulesRejectContradictionsWithoutChangingEarlierImports() {
        var envelope = Native(); var imported = envelope.ToWaterfallChart(); var saved = imported.ToSvg();
        envelope.Waterfall!.DeltaPoints.Add(new ChartPoint(1, 1));
        Assert.ThrowsAny<ArgumentException>(() => envelope.ToJson()); Assert.Equal(saved, imported.ToSvg());
        envelope.Waterfall.DeltaPoints.Clear();
        envelope.Waterfall.Items.Add(ChartWaterfallItem.Delta(1, 3));
        Assert.ThrowsAny<ArgumentException>(() => envelope.ToWaterfallChart());
        envelope.Waterfall.Items.RemoveAt(envelope.Waterfall.Items.Count - 1);
        envelope.Waterfall.Items[1] = ChartWaterfallItem.OpeningBalance(2, 3);
        Assert.ThrowsAny<ArgumentException>(() => envelope.ToJson());
        envelope.Waterfall.Items[1] = null!;
        Assert.ThrowsAny<ArgumentException>(() => envelope.ToJson()); Assert.Equal(saved, imported.ToSvg());
    }

    [Fact]
    public void DerivedTotalIdsDuplicateMetadataAndForeignFamilyRootsNeverBecomeAuthoredObservations() {
        var chart = Chart.Create().AddWaterfall("Numeric", new[] { new ChartPoint(1, 5), new ChartPoint(2, 3) });
        var envelope = chart.ToVisualArtifact().ToInterchangeEnvelope();
        envelope.Waterfall!.PointPresentation.Add(new VisualArtifactInterchangeWaterfallPoint { Index = 2, ObservationId = "invented-total" });
        Assert.ThrowsAny<ArgumentException>(() => envelope.Validate()); envelope.Waterfall.PointPresentation.Clear();
        envelope.Waterfall.PointPresentation.Add(new VisualArtifactInterchangeWaterfallPoint { Index = 0, ObservationId = "same" });
        envelope.Waterfall.PointPresentation.Add(new VisualArtifactInterchangeWaterfallPoint { Index = 1, ObservationId = "same" });
        Assert.ThrowsAny<ArgumentException>(() => envelope.Validate()); envelope.Waterfall.PointPresentation[1].ObservationId = "other";
        envelope.Waterfall.PointPresentation[1].Index = 0;
        Assert.ThrowsAny<ArgumentException>(() => envelope.Validate()); envelope.Waterfall.PointPresentation[1].Index = 20;
        Assert.ThrowsAny<ArgumentException>(() => envelope.Validate()); envelope.Waterfall.PointPresentation.Clear();
        envelope.Nodes.Add(new VisualArtifactInterchangeNode { Id = "unrelated", Label = "Graph node" });
        Assert.ThrowsAny<ArgumentException>(() => envelope.ToJson()); envelope.Nodes.Clear();
        envelope.Sequence = new VisualArtifactInterchangeSequenceArtifact(); Assert.ThrowsAny<ArgumentException>(() => envelope.ToJson());
        envelope.Sequence = null; envelope.Family = VisualArtifactInterchangeFamily.None; Assert.ThrowsAny<ArgumentException>(() => envelope.ToJson());
    }

    [Fact]
    public void JsonRejectsCheckpointValuesInvalidEnumsDuplicateKeysAndUnsupportedVersions() {
        var envelope = Native(); var json = envelope.ToJson();
        Assert.ThrowsAny<ArgumentException>(() => VisualArtifactInterchangeEnvelope.FromJson(json.Replace("\"kind\":\"Total\"", "\"kind\":\"Total\",\"value\":7", StringComparison.Ordinal)));
        Assert.ThrowsAny<ArgumentException>(() => VisualArtifactInterchangeEnvelope.FromJson(json.Replace("\"kind\":\"Delta\"", "\"kind\":\"Unknown\"", StringComparison.Ordinal)));
        Assert.ThrowsAny<ArgumentException>(() => VisualArtifactInterchangeEnvelope.FromJson(json.Replace("\"typedItems\":true", "\"typedItems\":true,\"typedItems\":false", StringComparison.Ordinal)));
        var unsupported = json.Replace("\"version\":" + envelope.Version.ToString(CultureInfo.InvariantCulture),
            "\"version\":" + (VisualArtifactInterchangeEnvelope.CurrentVersion + 1).ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        Assert.NotEqual(json, unsupported);
        Assert.Throws<NotSupportedException>(() => VisualArtifactInterchangeEnvelope.FromJson(unsupported));
        Assert.ThrowsAny<ArgumentException>(() => VisualArtifactInterchangeEnvelope.FromJson(json.Replace("\"family\":\"Waterfall\"", "\"family\":\"FutureWaterfall\"", StringComparison.Ordinal)));
        Assert.Throws<InvalidOperationException>(() => VisualArtifactInterchangeEnvelope.FromJson(json.Replace("\"xAxis\":{", "\"xAxis\":{\"ruleCrossAt\":0,\"crossAt\":0,", StringComparison.Ordinal)));
    }

    [Fact]
    public void CallbackAndProcessStateRejectBeforeAnyCallbackRuns() {
        var calls = 0; var chart = WaterfallInterchangeTests.Model();
        chart.WithValueFormat(ChartValueFormat.Custom(value => { calls++; return value.ToString(); }));
        Assert.Throws<NotSupportedException>(() => chart.ToVisualArtifact().ToInterchangeJson()); Assert.Equal(0, calls);
        chart.WithValueFormat(ChartValueFormat.Number());
        chart.Options.XAxis.WithLabelFormatter(value => { calls++; return value.ToString(); });
        Assert.Throws<NotSupportedException>(() => chart.ToVisualArtifact().ToInterchangeJson()); Assert.Equal(0, calls);
        chart.Options.XAxis.WithLabelFormatter(null); chart.Options.Labels.AccessibleTextFormatter = _ => { calls++; return "Custom"; };
        Assert.Throws<NotSupportedException>(() => chart.ToVisualArtifact().ToInterchangeJson()); Assert.Equal(0, calls);
        chart.Options.Labels.AccessibleTextFormatter = null; chart.Options.PngFontPath = "process-local-font.ttf";
        Assert.Throws<NotSupportedException>(() => chart.ToVisualArtifact().ToInterchangeJson()); chart.Options.PngFontPath = null;
        var culture = (CultureInfo)CultureInfo.GetCultureInfo("pl-PL").Clone(); culture.NumberFormat.NegativeSign = "minus ";
        chart.WithValueFormat(ChartValueFormat.Number("0.##", culture));
        Assert.Throws<NotSupportedException>(() => chart.ToVisualArtifact().ToInterchangeJson());
        chart.WithValueFormat(ChartValueFormat.Compact(CultureInfo.GetCultureInfo("pl-PL")));
        Assert.Equal(chart.ToSvg(), chart.ToVisualArtifact().ToInterchangeEnvelope().ToWaterfallChart().ToSvg());
        chart.Options.XAxis.WithTimeSpacing(ChartTimeSpacing.Ordinal);
        Assert.Throws<NotSupportedException>(() => chart.ToVisualArtifact().ToInterchangeJson());
    }

    [Fact]
    public void WrongCompositionInvalidNumericGeometryAndCanonicalIdentityRulesKeepTheirExistingOwners() {
        var mixed = Chart.Create().AddWaterfall("Waterfall", new[] { new ChartPoint(1, 2) }).AddLine("Other", new[] { new ChartPoint(1, 3) });
        Assert.Throws<NotSupportedException>(() => mixed.ToVisualArtifact().ToInterchangeJson());
        var numeric = Chart.Create().AddWaterfall("Numeric", new[] { new ChartPoint(1, double.MaxValue), new ChartPoint(2, double.MaxValue) });
        Assert.Throws<InvalidOperationException>(() => numeric.ToVisualArtifact().ToInterchangeJson());
        numeric.Series[0].Points.Clear(); numeric.Series[0].Points.Add(new ChartPoint(double.MaxValue, 1));
        Assert.Throws<InvalidOperationException>(() => numeric.ToVisualArtifact().ToInterchangeJson());
        var envelope = Native(); envelope.Waterfall!.SeriesId = "cfx:auto:01"; envelope.Waterfall.AutomaticSeriesId = true;
        Assert.ThrowsAny<ArgumentException>(() => envelope.ToJson());
        envelope.Waterfall.SeriesId = "cfx:auto:1"; envelope.Waterfall.AutomaticSeriesId = false;
        Assert.ThrowsAny<ArgumentException>(() => envelope.ToJson());
        envelope.Waterfall.SeriesId = "explicit"; envelope.Waterfall.PointPresentation.Add(new VisualArtifactInterchangeWaterfallPoint { Index = 1, ObservationId = " trimmed " });
        Assert.ThrowsAny<ArgumentException>(() => envelope.ToJson());
        Assert.Throws<NotSupportedException>(() => Chart.Create().AddLine("Line", new[] { new ChartPoint(1, 2) }).ToVisualArtifact().ToInterchangeEnvelope().ToWaterfallChart());
        Assert.Equal(VisualArtifactInterchangeFamily.None, Chart.Create().AddLine("Line", new[] { new ChartPoint(1, 2) }).ToVisualArtifact().ToInterchangeEnvelope().Family);
    }

    [Fact]
    public void PortableCollectionsRetainTheirExistingBudgetsAndAtomicSparseIdentityRestore() {
        var envelope = Native(); envelope.Width = 600.5; Assert.ThrowsAny<ArgumentException>(() => envelope.ToJson());
        envelope.Width = 600; envelope.Height = null; Assert.ThrowsAny<ArgumentException>(() => envelope.ToJson());
        envelope.Height = 400; envelope.Waterfall!.PointPresentation.Clear();
        envelope.Waterfall.PointPresentation.Add(new VisualArtifactInterchangeWaterfallPoint { Index = 0, ObservationId = new string('x', 513) });
        Assert.ThrowsAny<ArgumentException>(() => envelope.ToJson());
        envelope.Waterfall.PointPresentation.Clear(); envelope.Waterfall.Items.Clear();
        envelope.Waterfall.Items.AddRange(Enumerable.Repeat(ChartWaterfallItem.Delta(1, 1), 50001));
        Assert.Throws<ArgumentOutOfRangeException>(() => envelope.Validate());
        var series = WaterfallInterchangeTests.Model().Series[0]; var saved = series.ObservationIds.ToArray();
        Assert.ThrowsAny<ArgumentException>(() => series.RestoreObservationIds(new[] {
            new KeyValuePair<int, ChartObservationId>(0, new ChartObservationId("new")),
            new KeyValuePair<int, ChartObservationId>(999, new ChartObservationId("bad")) }));
        Assert.Equal(saved, series.ObservationIds.ToArray());
    }

    [Fact]
    public void PortableContributorBudgetRejectsBeforeLaterAccumulationAndKeepsBoundaryTotalsUnchanged() {
        var series = ChartSeries.CreateWaterfall(new ChartSeriesId("budget"), "Budget", new[] {
            ChartWaterfallItem.Delta(1, 10), ChartWaterfallItem.Total(2), ChartWaterfallItem.Delta(3, -3),
            ChartWaterfallItem.Subtotal(4), ChartWaterfallItem.Total(5)
        });
        // Source references are 1,1,1,1,2 across these five display items.
        var bounded = ChartWaterfallSteps.Create(series, 6); var ordinary = ChartWaterfallSteps.Create(series);
        Assert.Equal(new[] { 10d, 10, -3, -3, 7 }, bounded.Select(step => step.Value));
        for (var index = 0; index < bounded.Count; index++) {
            Assert.Equal(ordinary[index].SourceIndices, bounded[index].SourceIndices);
            Assert.Equal(ordinary[index].Start, bounded[index].Start); Assert.Equal(ordinary[index].End, bounded[index].End);
        }
        Assert.Equal("maximumSourceReferences", Assert.Throws<ArgumentOutOfRangeException>(() => ChartWaterfallSteps.Create(series, 5)).ParamName);
        var laterOverflow = ChartSeries.CreateWaterfall(new ChartSeriesId("early"), "Early", new[] {
            ChartWaterfallItem.Delta(1, double.MaxValue), ChartWaterfallItem.Total(2), ChartWaterfallItem.Delta(3, double.MaxValue)
        });
        Assert.Equal("maximumSourceReferences", Assert.Throws<ArgumentOutOfRangeException>(() => ChartWaterfallSteps.Create(laterOverflow, 1)).ParamName);
        Assert.Throws<InvalidOperationException>(() => ChartWaterfallSteps.Create(laterOverflow));
        var envelope = Native(); envelope.Waterfall!.Items.Clear(); envelope.Waterfall.PointPresentation.Clear();
        for (var index = 0; index < 2000; index++) {
            envelope.Waterfall.Items.Add(ChartWaterfallItem.Delta(index * 2, 1));
            envelope.Waterfall.Items.Add(ChartWaterfallItem.Total(index * 2 + 1));
        }
        Assert.Equal("maximumSourceReferences", Assert.Throws<ArgumentOutOfRangeException>(() => envelope.Validate()).ParamName);
        Assert.Equal(4000, envelope.Waterfall.Items.Count);
    }

    private static VisualArtifactInterchangeEnvelope Native() => WaterfallInterchangeTests.Model().ToVisualArtifact("portable-waterfall").ToInterchangeEnvelope();
}
