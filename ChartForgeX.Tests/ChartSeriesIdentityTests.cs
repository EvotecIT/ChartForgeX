using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects retained handles, stable semantic identity, and the detached preparation boundary.</summary>
public sealed class ChartSeriesIdentityTests {
    [Fact]
    public void RetainedHandlesKeepIdentityWhenDisplayNamesAndPaintOrderChange() {
        var first = ChartSeries.CreateLine(new ChartSeriesId("actual"), "Reading", Points(2, 4));
        var second = ChartSeries.CreateLine(new ChartSeriesId("target"), "Reading", Points(3, 5));
        var chart = Chart.Create().AddSeries(first).AddSeries(second);
        first.StateRole = ChartSeriesState.Warning;
        chart.MoveSeries(first, 1);
        first.WithName("Localized reading");
        Assert.Same(first, chart.Series[1]);
        Assert.Equal("actual", first.Id.Value);
        Assert.Equal("target", second.Id.Value);
        Assert.Equal(ChartSeriesState.None, second.StateRole);
        var groups = XDocument.Parse(chart.Prepare(new VisualRenderContext()).ToSvg()).Descendants()
            .Where(node => (string?)node.Attribute("data-cfx-role") == "series").ToArray();
        Assert.Equal(new[] { "target", "actual" }, groups.Select(node => (string?)node.Attribute("data-cfx-series-key")));
        Assert.Equal("Localized reading", (string?)groups[1].Attribute("data-cfx-series-name"));
    }

    [Fact]
    public void AttachmentRejectsDuplicateIdsAndSharedMutableHandlesBeforeChangingEitherChart() {
        var handle = ChartSeries.CreateLine(new ChartSeriesId("source"), "Reading", Points(2, 4));
        var chart = Chart.Create().AddSeries(handle);
        var peer = Chart.Create();
        Assert.Throws<InvalidOperationException>(() => chart.AddSeries(handle));
        Assert.Throws<InvalidOperationException>(() => peer.AddSeries(handle));
        Assert.Throws<ArgumentException>(() => chart.AddSeries(ChartSeries.CreateLine(new ChartSeriesId("source"), "Other", Points(1))));
        Assert.Same(handle, Assert.Single(chart.Series));
        Assert.Empty(peer.Series);
        chart.RemoveSeries(handle);
        peer.AddSeries(handle);
        Assert.Empty(chart.Series);
        Assert.Same(handle, Assert.Single(peer.Series));
        Assert.Equal("source", handle.Id.Value);
    }

    [Fact]
    public void AutomaticIdsAreDeterministicLocalAndNeverRenumberedOrReused() {
        var first = Chart.Create().AddLine("Repeated", Points(1)).AddLine("Repeated", Points(2));
        var equivalent = Chart.Create().AddLine("Repeated", Points(1)).AddLine("Repeated", Points(2));
        Assert.Equal(first.Series.Select(series => series.Id), equivalent.Series.Select(series => series.Id));
        Assert.Equal(first.Prepare(new VisualRenderContext()).ToSvg(), equivalent.Prepare(new VisualRenderContext()).ToSvg());
        var removed = first.Series[0]; var retained = first.Series[1]; var id = retained.Id;
        Assert.True(removed.Id.IsAutomatic);
        first.RemoveSeries(removed).AddLine("Repeated", Points(3)).MoveSeries(retained, 1);
        Assert.Equal(id, retained.Id);
        Assert.NotEqual(removed.Id, first.Series[0].Id);
        first.AddSeries(removed);
        Assert.Equal(removed.Id, first.Series[2].Id);
        first.RemoveSeries(removed);
        Assert.Throws<InvalidOperationException>(() => Chart.Create().AddSeries(removed));
        Assert.Throws<ArgumentException>(() => new ChartSeriesId("cfx:auto:1"));
    }

    [Fact]
    public void ReadOnlySeriesViewCannotBypassAttachmentRules() {
        var chart = Chart.Create().AddLine("Reading", Points(1));
        var view = Assert.IsAssignableFrom<IList<ChartSeries>>(chart.Series);
        Assert.Throws<NotSupportedException>(() => view.Clear());
        Assert.Throws<NotSupportedException>(() => view.Add(ChartSeries.CreateLine(new ChartSeriesId("other"), "Other", Points(2))));
        var foreign = ChartSeries.CreateLine(new ChartSeriesId("foreign"), "Foreign", Points(3));
        Assert.Throws<ArgumentException>(() => chart.RemoveSeries(foreign));
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.MoveSeries(chart.Series[0], 1));
        Assert.Single(chart.Series);
    }

    [Fact]
    public void PreparationCapturesSeriesAndObservationIdsBeforeLaterHandleEdits() {
        var series = ChartSeries.CreateScatter(new ChartSeriesId("readings"), "Reading", Points(5, 8))
            .WithObservationId(0, new ChartObservationId("north:primary"));
        var chart = Chart.Create().AddSeries(series);
        var prepared = chart.Prepare(new VisualRenderContext());
        var svg = prepared.ToSvg();
        var point = XDocument.Parse(svg).Descendants().Single(node => (string?)node.Attribute("data-cfx-source-id") == "series-0-point-0");
        Assert.Equal("readings", (string?)point.Attribute("data-cfx-series-key"));
        Assert.Equal("north:primary", (string?)point.Attribute("data-cfx-observation-id"));
        series.WithName("Changed").WithObservationId(0, new ChartObservationId("replacement"));
        chart.ClearSeries();
        Assert.Equal(svg, prepared.ToSvg());
        Assert.Empty(chart.Series);
        Assert.Equal("readings", series.Id.Value);
    }

    [Fact]
    public void ObservationIdsRejectAmbiguousOrMissingSourcesWithoutReplacingExistingIdentity() {
        var series = ChartSeries.CreateScatter(new ChartSeriesId("readings"), "Reading", Points(5, 8))
            .WithObservationId(0, new ChartObservationId("north"));
        Assert.Throws<ArgumentException>(() => series.WithObservationId(1, new ChartObservationId("north")));
        Assert.Throws<ArgumentOutOfRangeException>(() => series.WithObservationId(2, new ChartObservationId("east")));
        Assert.Throws<InvalidOperationException>(() => series.WithObservationId(1, default));
        Assert.Equal("north", Assert.Single(series.ObservationIds).Value.Value);
        Assert.Throws<NotSupportedException>(() => ((IDictionary<int, ChartObservationId>)series.ObservationIds).Clear());
    }

    [Fact]
    public void ExplicitFactoriesRetainHistogramSamplesAndDecimationProvenance() {
        var observations = new[] { new ChartPoint(.2, 5), new ChartPoint(.4, -2), new ChartPoint(1.5, 8) };
        var histogram = ChartSeries.CreateHistogram(new ChartSeriesId("histogram"), "Distribution", observations,
            ChartHistogramBinLayout.FromBoundaries(new[] { 0d, 1, 2 }), ChartHistogramAggregation.Sum);
        var samples = new[] { 30d, 0, 20, 10 };
        var box = ChartSeries.CreateBoxPlot(new ChartSeriesId("box"), "Distribution", 1, samples);
        var trend = ChartSeries.CreateTrendLine(new ChartSeriesId("trend"), "Trend", observations);
        var source = Enumerable.Range(0, 100).Select(index => new ChartPoint(index, Math.Sin(index / 4d))).ToArray();
        var line = ChartSeries.CreateDecimated(new ChartSeriesId("line"), "Trend", ChartSeriesKind.Line, source, 12);
        observations[0] = new ChartPoint(.2, 900); samples[0] = 900; source[0] = new ChartPoint(0, 900);
        Assert.Equal(3, histogram.SourcePointCount);
        Assert.Equal(new[] { 0, 1 }, histogram.HistogramBins[0].SourceIndices);
        Assert.Equal(3, histogram.HistogramBins[0].Value);
        Assert.Equal(5, histogram.HistogramSourcePoints[0].Y);
        Assert.Equal(new[] { 30d, 0, 20, 10 }, box.BoxPlotSourceSamples);
        Assert.Equal(15, Assert.Single(box.BoxPlots).Median);
        Assert.Equal(5, trend.TrendLineSourcePoints[0].Y);
        Assert.Equal(100, line.SourcePointCount);
        Assert.Equal(12, line.ObservationCount);
        Assert.Equal(0, line.SourcePointIndices[0]);
        Assert.Equal(0, line.Points[0].Y);
        Assert.Contains("data-cfx-series-key=\"line\"", Chart.Create().AddSeries(line).Prepare(new VisualRenderContext()).ToSvg());
    }

    [Fact]
    public void ArtifactEnvelopeCloneAndJsonCarryExplicitAndAutomaticSeriesIdentities() {
        var chart = Chart.Create().AddSeries(ChartSeries.CreateBar(new ChartSeriesId("actual"), "Reading", Points(2, 4)))
            .AddLine("Reading", Points(3, 5));
        var artifact = chart.ToVisualArtifact();
        var clone = artifact.Clone();
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(clone.ToInterchangeEnvelope().ToJson());
        Assert.Equal("actual", envelope.Extensions["chart.series.0.id"]);
        Assert.Equal("shared", envelope.Extensions["chart.series.0.scope"]);
        Assert.Equal("cfx:auto:1", envelope.Extensions["chart.series.1.id"]);
        Assert.Equal("local", envelope.Extensions["chart.series.1.scope"]);
    }

    private static ChartPoint[] Points(params double[] values) => values.Select((value, index) => new ChartPoint(index + 1, value)).ToArray();
}
