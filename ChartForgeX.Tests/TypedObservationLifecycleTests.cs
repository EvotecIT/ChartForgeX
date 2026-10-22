using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TypedObservationLifecycleTests {
    public static IEnumerable<object[]> Families() => new[] {
        ChartSeriesKind.Bubble, ChartSeriesKind.Candlestick, ChartSeriesKind.Ohlc, ChartSeriesKind.RangeBand,
        ChartSeriesKind.RangeArea, ChartSeriesKind.RangeBar, ChartSeriesKind.ErrorBar, ChartSeriesKind.BoxPlot,
        ChartSeriesKind.Dumbbell, ChartSeriesKind.DifferenceArea
    }.Select(kind => new object[] { kind });

    [Theory]
    [MemberData(nameof(Families))]
    public void EmptyConvenienceFactoriesCanPrepareThenRefillTheRetainedTypedSeries(ChartSeriesKind kind) {
        var chart = CreateEmpty(kind); var series = Assert.Single(chart.Series); var id = series.Id;
        Assert.Equal(0, series.ObservationCount); Assert.Equal(0, series.SourcePointCount);
        var empty = chart.Prepare(Context()); var svg = empty.ToSvg(); var png = empty.ToPng(new VisualRenderOptions(supersampling: 1));
        Assert.DoesNotContain(empty.Scene.Nodes, node => node.Role == "point");
        Assert.Contains(empty.Diagnostics, item => item.Code == "cartesian.no-data");
        Refill(series);
        series.WithObservationId(0, new ChartObservationId("arrived"));
        var filled = chart.Prepare(Context());
        var point = Assert.Single(filled.Scene.Nodes.OfType<VisualSceneGroup>(), node => node.Role == "point");
        Assert.Equal("arrived", point.Metadata["data-cfx-observation-id"]);
        Assert.Equal("1", point.Metadata["data-cfx-source-count"]);
        Assert.Same(series, Assert.Single(chart.Series)); Assert.Equal(id, series.Id);
        Assert.Equal(1, series.ObservationCount); Assert.Equal(1, series.SourcePointCount);
        Assert.NotEqual(svg, filled.ToSvg()); Assert.NotEmpty(filled.ToPng(new VisualRenderOptions(supersampling: 1)));
        Assert.Equal(svg, empty.ToSvg()); Assert.Equal(png, empty.ToPng(new VisualRenderOptions(supersampling: 1)));
    }

    [Fact]
    public void AnEmptyCollectionOfRawBoxGroupsCanBeFilledWithoutInventingSamples() {
        var chart = Chart.Create().AddBoxPlot("Raw", Array.Empty<ChartBoxPlotSampleGroup>());
        var series = Assert.Single(chart.Series); var empty = chart.Prepare(Context());
        Assert.Empty(series.BoxPlotSourceGroups); Assert.Equal(0, series.SourcePointCount);
        Assert.DoesNotContain(empty.Scene.Nodes, node => node.Role == "point");
        series.ReplaceBoxPlotSampleGroups(new[] { new ChartBoxPlotSampleGroup(1, new[] { 2d, 4, 6 }) });
        var filled = chart.Prepare(Context());
        Assert.Equal(3, series.SourcePointCount); Assert.Equal(1, series.ObservationCount);
        Assert.Equal("3", Assert.Single(filled.Scene.Nodes.OfType<VisualSceneGroup>(), node => node.Role == "point").Metadata["data-cfx-source-count"]);
        Assert.NotEmpty(filled.ToPng(new VisualRenderOptions(supersampling: 1)));
        Assert.Throws<ArgumentException>(() => new ChartBoxPlotSampleGroup(1, Array.Empty<double>()));
    }

    [Theory]
    [MemberData(nameof(Families))]
    public void TypedFamiliesHaveOneObservationAndNoMutableNumericEncoding(ChartSeriesKind kind) {
        var series = Create(kind);
        Assert.Equal(1, series.ObservationCount); Assert.Equal(1, series.SourcePointCount);
        Assert.Empty(series.Points); Assert.True(series.Points.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => series.Points.Add(new ChartPoint(1, 2)));
        Assert.Throws<ArgumentException>(() => new ChartSeries(new ChartSeriesId("invalid"), "Invalid", kind, new[] { new ChartPoint(1, 2) }));
        var chart = Chart.Create().AddSeries(series);
        var prepared = chart.Prepare(Context());
        var point = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), node => node.Role == "point");
        Assert.Equal("1", point.Metadata["data-cfx-source-count"]);
        Assert.Equal("0", point.Metadata["data-cfx-source-point"]);
        Assert.Contains(prepared.Regions, region => region.Role == "point");
        Assert.DoesNotContain(point.Metadata.Keys, key => key.StartsWith("data-cfx-source-0-", StringComparison.Ordinal));
        Assert.NotEmpty(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
    }

    [Theory]
    [MemberData(nameof(Families))]
    public void SingleUpdatesRetainIdentityAndPresentationWhileReplacementClearsObservationOverrides(ChartSeriesKind kind) {
        var series = Create(kind).WithObservationId(0, new ChartObservationId("row-a"))
            .WithPointLabel(0, "Original label").WithPointColor(0, ChartColor.FromHex("#B34C89"));
        series.WithPointFillPattern(0, ChartFillPattern.Crosshatch);
        var chart = Chart.Create().AddSeries(series);
        var prepared = chart.Prepare(Context()); var svg = prepared.ToSvg(); var png = prepared.ToPng(new VisualRenderOptions(supersampling: 1));
        var id = series.Id;
        Update(series, 20);
        Assert.Equal(id, series.Id); Assert.Equal("row-a", series.ObservationIds[0].Value);
        Assert.Equal("Original label", series.PointLabels[0]); Assert.NotNull(series.PointColors[0]);
        Assert.Equal(ChartFillPattern.Crosshatch, series.PointFillPatterns[0]);
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        Assert.NotEqual(svg, chart.Prepare(Context()).ToSvg());
        ReplaceEmpty(series);
        Assert.Equal(id, series.Id); Assert.Equal(0, series.ObservationCount); Assert.Equal(0, series.SourcePointCount);
        Assert.Empty(series.ObservationIds); Assert.Empty(series.PointLabels); Assert.Empty(series.PointColors); Assert.Empty(series.PointFillPatterns);
        Assert.NotEmpty(chart.Prepare(Context()).ToSvg());
    }

    [Fact]
    public void TypedInputIsSnapshottedAndFailedReplacementDoesNotDiscardValuesOrKeys() {
        var input = new[] { new ChartBubble(1, 2, 3).WithColorValue(4) };
        var series = ChartSeries.CreateBubble(new ChartSeriesId("bubble"), "Bubble", input).WithObservationId(0, new ChartObservationId("a"));
        input[0] = new ChartBubble(9, 8, 7);
        Assert.Equal(2, series.Bubbles[0].Y); Assert.Equal(4, series.Bubbles[0].ColorValue);
        Assert.Throws<NotSupportedException>(() => ((IList<ChartBubble>)series.Bubbles)[0] = input[0]);
        Assert.Throws<ArgumentOutOfRangeException>(() => series.ReplaceBubbles(new[] { default(ChartBubble) }));
        Assert.Equal(2, series.Bubbles[0].Y); Assert.Equal("a", series.ObservationIds[0].Value);
        Assert.Throws<InvalidOperationException>(() => series.ReplaceRanges(new[] { new ChartRangeBand(1, 2, 3) }));
        Assert.Throws<ArgumentOutOfRangeException>(() => series.UpdateBubble(1, input[0]));
    }

    [Fact]
    public void RawBoxSamplesRemainIndependentUntilAnAuthoredSummaryReplacesTheirDerivation() {
        var samples = new[] { 8d, 2d, 6d, 4d };
        var chart = Chart.Create().AddBoxPlot("Raw", 1, samples); var series = chart.Series[0];
        Assert.Equal(1, series.ObservationCount); Assert.Equal(4, series.SourcePointCount);
        var prepared = chart.Prepare(Context());
        var facts = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), node => node.Role == "point").Metadata;
        Assert.Equal("five-number-summary", facts["data-cfx-derived"]); Assert.Equal("4", facts["data-cfx-source-count"]);
        Assert.False(facts.ContainsKey("data-cfx-source-point"));
        samples[0] = 99; Assert.Equal(8, series.BoxPlotSourceSamples[0]);
        series.UpdateBoxPlot(0, new ChartBoxPlot(1, 1, 2, 3, 4, 5));
        Assert.Empty(series.BoxPlotSourceSamples); Assert.Equal(1, series.SourcePointCount);
        Assert.Equal("4", facts["data-cfx-source-count"]);
    }

    [Fact]
    public void TypedRowSelectorsRunOnceAndRangeBreaksReachPreparedMetadata() {
        var rows = new[] { new { X = 1d, Low = 2d, High = 5d }, new { X = 2d, Low = 3d, High = 6d } };
        var calls = 0;
        var chart = Chart.Create().AddRangeArea("Range", rows, row => { calls++; return new ChartRangeBand(row.X, row.Low, row.High, true); }, smooth: false);
        Assert.Equal(2, calls); Assert.Equal(2, chart.Series[0].Ranges.Count);
        var prepared = chart.Prepare(Context());
        Assert.Equal(2, calls);
        Assert.All(prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(node => node.Role == "point"), point => Assert.Equal("true", point.Metadata["data-cfx-break-before"]));
    }

    private static VisualRenderContext Context() => new(new VisualLayoutOptions(new VisualSize(320, 240)), VisualTheme.Graphite(), frame: new VisualFrame(showLegend: false));
    private static Chart CreateEmpty(ChartSeriesKind kind) {
        var chart = Chart.Create();
        return kind switch {
            ChartSeriesKind.Bubble => chart.AddBubble("Typed", Array.Empty<ChartBubble>()),
            ChartSeriesKind.Candlestick => chart.AddCandlestick("Typed", Array.Empty<ChartCandlestick>()),
            ChartSeriesKind.Ohlc => chart.AddOhlc("Typed", Array.Empty<ChartCandlestick>()),
            ChartSeriesKind.RangeBand => chart.AddRangeBand("Typed", Array.Empty<ChartRangeBand>()),
            ChartSeriesKind.RangeArea => chart.AddRangeArea("Typed", Array.Empty<ChartRangeBand>()),
            ChartSeriesKind.RangeBar => chart.AddRangeBar("Typed", Array.Empty<ChartInterval>()),
            ChartSeriesKind.ErrorBar => chart.AddErrorBar("Typed", Array.Empty<ChartErrorBar>()),
            ChartSeriesKind.BoxPlot => chart.AddBoxPlot("Typed", Array.Empty<ChartBoxPlot>()),
            ChartSeriesKind.Dumbbell => chart.AddDumbbell("Typed", Array.Empty<ChartDumbbell>()),
            _ => chart.AddDifferenceArea("Typed", Array.Empty<ChartDifferencePoint>())
        };
    }
    private static void Refill(ChartSeries series) {
        var input = Create(series.Kind);
        switch (series.Kind) {
            case ChartSeriesKind.Bubble: series.ReplaceBubbles(input.Bubbles); break;
            case ChartSeriesKind.Candlestick: case ChartSeriesKind.Ohlc: series.ReplaceFinancialValues(input.FinancialValues); break;
            case ChartSeriesKind.RangeBand: case ChartSeriesKind.RangeArea: series.ReplaceRanges(input.Ranges); break;
            case ChartSeriesKind.RangeBar: series.ReplaceIntervals(input.Intervals); break;
            case ChartSeriesKind.ErrorBar: series.ReplaceErrorBars(input.ErrorBars); break;
            case ChartSeriesKind.BoxPlot: series.ReplaceBoxPlots(input.BoxPlots); break;
            case ChartSeriesKind.Dumbbell: series.ReplaceDumbbells(input.Dumbbells); break;
            default: series.ReplaceDifferencePoints(input.DifferencePoints); break;
        }
    }
    private static ChartSeries Create(ChartSeriesKind kind) {
        var id = new ChartSeriesId("typed");
        return kind switch {
            ChartSeriesKind.Bubble => ChartSeries.CreateBubble(id, "Typed", new[] { new ChartBubble(1, 5, 3).WithColorValue(7) }),
            ChartSeriesKind.Candlestick => ChartSeries.CreateCandlestick(id, "Typed", new[] { new ChartCandlestick(1, 3, 8, 1, 5) }),
            ChartSeriesKind.Ohlc => ChartSeries.CreateOhlc(id, "Typed", new[] { new ChartCandlestick(1, 3, 8, 1, 5) }),
            ChartSeriesKind.RangeBand => ChartSeries.CreateRangeBand(id, "Typed", new[] { new ChartRangeBand(1, 2, 6) }),
            ChartSeriesKind.RangeArea => ChartSeries.CreateRangeArea(id, "Typed", new[] { new ChartRangeBand(1, 2, 6) }, smooth: false),
            ChartSeriesKind.RangeBar => ChartSeries.CreateRangeBar(id, "Typed", new[] { new ChartInterval(1, 6, 2) }),
            ChartSeriesKind.ErrorBar => ChartSeries.CreateErrorBar(id, "Typed", new[] { new ChartErrorBar(1, 4, yBounds: new ChartErrorBounds(2, 6)) }),
            ChartSeriesKind.BoxPlot => ChartSeries.CreateBoxPlot(id, "Typed", new[] { new ChartBoxPlot(1, 1, 2, 3, 4, 5) }),
            ChartSeriesKind.Dumbbell => ChartSeries.CreateDumbbell(id, "Typed", new[] { new ChartDumbbell(1, 6, 2) }),
            _ => ChartSeries.CreateDifferenceArea(id, "Typed", new[] { new ChartDifferencePoint(1, 6, 2) })
        };
    }
    private static void Update(ChartSeries series, double value) {
        switch (series.Kind) {
            case ChartSeriesKind.Bubble: series.UpdateBubble(0, new ChartBubble(1, value, 4)); break;
            case ChartSeriesKind.Candlestick: case ChartSeriesKind.Ohlc: series.UpdateFinancialValue(0, new ChartCandlestick(1, value, value + 5, value - 5, value + 2)); break;
            case ChartSeriesKind.RangeBand: case ChartSeriesKind.RangeArea: series.UpdateRange(0, new ChartRangeBand(1, value, value + 5)); break;
            case ChartSeriesKind.RangeBar: series.UpdateInterval(0, new ChartInterval(1, value, value + 5)); break;
            case ChartSeriesKind.ErrorBar: series.UpdateErrorBar(0, new ChartErrorBar(1, value, yBounds: new ChartErrorBounds(value - 2, value + 2))); break;
            case ChartSeriesKind.BoxPlot: series.UpdateBoxPlot(0, new ChartBoxPlot(1, value, value + 1, value + 2, value + 3, value + 4)); break;
            case ChartSeriesKind.Dumbbell: series.UpdateDumbbell(0, new ChartDumbbell(1, value, value + 5)); break;
            default: series.UpdateDifferencePoint(0, new ChartDifferencePoint(1, value, value + 5)); break;
        }
    }
    private static void ReplaceEmpty(ChartSeries series) {
        switch (series.Kind) {
            case ChartSeriesKind.Bubble: series.ReplaceBubbles(Array.Empty<ChartBubble>()); break;
            case ChartSeriesKind.Candlestick: case ChartSeriesKind.Ohlc: series.ReplaceFinancialValues(Array.Empty<ChartCandlestick>()); break;
            case ChartSeriesKind.RangeBand: case ChartSeriesKind.RangeArea: series.ReplaceRanges(Array.Empty<ChartRangeBand>()); break;
            case ChartSeriesKind.RangeBar: series.ReplaceIntervals(Array.Empty<ChartInterval>()); break;
            case ChartSeriesKind.ErrorBar: series.ReplaceErrorBars(Array.Empty<ChartErrorBar>()); break;
            case ChartSeriesKind.BoxPlot: series.ReplaceBoxPlots(Array.Empty<ChartBoxPlot>()); break;
            case ChartSeriesKind.Dumbbell: series.ReplaceDumbbells(Array.Empty<ChartDumbbell>()); break;
            default: series.ReplaceDifferencePoints(Array.Empty<ChartDifferencePoint>()); break;
        }
    }
}
