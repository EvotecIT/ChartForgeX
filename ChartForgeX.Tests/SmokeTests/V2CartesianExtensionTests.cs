using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class V2CartesianExtensionTests {
    [Theory]
    [InlineData(ChartSeriesKind.Bubble, "bubble")]
    [InlineData(ChartSeriesKind.ErrorBar, "error-range")]
    [InlineData(ChartSeriesKind.Candlestick, "candlestick-body")]
    [InlineData(ChartSeriesKind.RangeBand, "range-band")]
    [InlineData(ChartSeriesKind.RangeArea, "range-area")]
    [InlineData(ChartSeriesKind.Lollipop, "lollipop-stem")]
    [InlineData(ChartSeriesKind.Dumbbell, "dumbbell-connector")]
    [InlineData(ChartSeriesKind.RangeBar, "range-bar")]
    [InlineData(ChartSeriesKind.BoxPlot, "boxplot-body")]
    [InlineData(ChartSeriesKind.HorizontalBar, "horizontal-bar")]
    [InlineData(ChartSeriesKind.Waterfall, "waterfall-bar")]
    [InlineData(ChartSeriesKind.Slope, "slope-line")]
    [InlineData(ChartSeriesKind.Ohlc, "ohlc-stem")]
    [InlineData(ChartSeriesKind.TrendLine, "trend-line")]
    public void PublicPreparation_AllCartesianExtensionsProduceNativeGeometryAndDetachedExports(ChartSeriesKind kind, string role) {
        var chart = Fixture(kind).WithAxes(false).WithDataLabels(false);
        chart.Options.ShowGrid = false;
        var context = new VisualRenderContext(frame: new VisualFrame(showLegend: false, showSurface: false, transparentBackground: true));
        var prepared = chart.Prepare(context);
        Assert.Contains(prepared.Scene.Nodes, node => node.Role == role);
        Assert.Contains(prepared.Regions, region => region.Role == "point");
        var svg = prepared.ToSvg();
        Assert.NotNull(XDocument.Parse(svg).Root);
        Assert.DoesNotContain("NaN", svg); Assert.DoesNotContain("Infinity", svg);
        var pixels = prepared.ToRgba(new VisualRenderOptions(supersampling: 1));
        Assert.Contains(Enumerable.Range(0, pixels.Width * pixels.Height), index => pixels.Pixels[index * 4 + 3] > 0);
        chart.Series[0].Points.Clear();
        Assert.Equal(svg, prepared.ToSvg());
    }

    [Fact]
    public void MixedFinancialRanges_UseSharedExtentsAndRetainCompleteObservationTuples() {
        var chart = Chart.Create().AddLine("Observed", new[] { new ChartPoint(1, 4), new ChartPoint(2, 8) })
            .AddErrorBar("Uncertainty", new[] { new ChartErrorBar(1, 4, -20, 80) })
            .AddCandlestick("Trade", new[] { new ChartCandlestick(2, 8, 110, -30, 12) })
            .AddRangeBar("Interval", new[] { new ChartInterval(1.5, -10, 95) });
        var range = ChartRange.FromChart(chart);
        Assert.True(range.MinY <= -30); Assert.True(range.MaxY >= 110);
        var prepared = chart.Prepare(new VisualRenderContext());
        var groups = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "point").ToArray();
        var candle = Assert.Single(groups, group => group.Metadata.ContainsKey("data-cfx-high"));
        Assert.Equal("110", candle.Metadata["data-cfx-high"]);
        Assert.Equal("4", candle.Metadata["data-cfx-source-count"]);
        Assert.Equal("3", candle.Metadata["data-cfx-source-3-index"]);
        Assert.Contains(prepared.Regions, region => region.Label?.Contains("low=-30") == true && region.Label.Contains("close=12"));
        var cap = prepared.Scene.Nodes.OfType<VisualSceneLine>().First(line => line.Role == "error-cap");
        Assert.True(cap.Start.X > 0); Assert.True(cap.End.X < prepared.Size.Width);
    }

    [Theory]
    [InlineData(ChartBarStyle.Flat)]
    [InlineData(ChartBarStyle.Solid)]
    [InlineData(ChartBarStyle.SegmentedCapsule)]
    public void HorizontalNegativeStacks_PreserveIndependentSignsTreatmentsLabelsAndCategoryMappings(ChartBarStyle style) {
        var chart = Chart.Create().AddHorizontalBar("A", new[] { new ChartPoint(1, 8), new ChartPoint(2, -6) })
            .AddHorizontalBar("B", new[] { new ChartPoint(1, 4), new ChartPoint(2, -3) }).WithStackedHorizontalBars();
        chart.Options.BarStyle = style; chart.Options.ShowStackTotals = true;
        chart.Options.XAxis.Labels.Add(new ChartAxisLabel(1, "Category one"));
        chart.Options.XAxis.Labels.Add(new ChartAxisLabel(2, "Category two"));
        chart.Series[0].FillPattern = ChartFillPattern.Crosshatch;
        var prepared = chart.Prepare(new VisualRenderContext());
        var groups = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "point").ToArray();
        Assert.Contains(groups, group => group.Metadata["data-cfx-base"] == "8");
        Assert.Contains(groups, group => group.Metadata["data-cfx-base"] == "-6");
        Assert.Contains(prepared.Regions, region => region.Role == "stack-total" && region.Label?.Contains("value=12") == true);
        Assert.Contains(prepared.Regions, region => region.Role == "stack-total" && region.Label?.Contains("value=-9") == true);
        Assert.Contains(prepared.Regions, region => region.Role == "axis-y-label" && region.Label?.Contains("Category one") == true);
        Assert.DoesNotContain(prepared.Regions, region => region.Role == "axis-x-label" && region.Label?.Contains("Category one") == true);
        Assert.Contains(prepared.Scene.Nodes, node => node.Role == "horizontal-bar-pattern");
        if (style == ChartBarStyle.Solid) Assert.Contains(prepared.Scene.Nodes.OfType<VisualSceneGradient>(), gradient => gradient.Role == "horizontal-bar");
        if (style == ChartBarStyle.SegmentedCapsule) Assert.Equal(4, prepared.Scene.Nodes.Count(node => node.Role == "horizontal-bar-cap"));
    }

    [Fact]
    public void Waterfall_CumulativeBoundsCoordinatesTotalAndRawDeltasAreConsistent() {
        var chart = Chart.Create().AddWaterfall("Movement", new[] { new ChartPoint(10, 100), new ChartPoint(20, -40), new ChartPoint(40, 80) });
        var range = ChartRange.FromChart(chart);
        Assert.True(range.MaxY >= 140); Assert.True(range.MaxX > 50);
        var prepared = chart.Prepare(new VisualRenderContext());
        var points = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "point").ToArray();
        Assert.Equal(new[] { "10", "20", "40" }, points.Select(point => point.Metadata["data-cfx-x"]));
        Assert.Equal("100", points[1].Metadata["data-cfx-start"]); Assert.Equal("60", points[1].Metadata["data-cfx-end"]);
        var total = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.Role == "waterfall-total");
        Assert.Equal("-1", total.Metadata["data-cfx-source-point"]); Assert.Equal("140", total.Metadata["data-cfx-end"]);
        Assert.Contains(prepared.Regions, region => region.Role == "axis-x-label" && region.Label?.StartsWith("Total", StringComparison.Ordinal) == true);
        Assert.Equal(3, chart.Series[0].Points.Count); Assert.Equal(-40, chart.Series[0].Points[1].Y);
    }

    [Fact]
    public void EncodedPointOptions_AreIndexedByObservationAndFullLabelsSurvivePlacement() {
        var chart = Fixture(ChartSeriesKind.Candlestick).WithDataLabels();
        var first = "Complete first trade description that cannot fit the tiny plotting viewport";
        var second = "Second trade";
        chart.Series[0].PointLabels.AddRange(new[] { first, second });
        var accent = ChartColor.FromHex("#B34C89"); chart.Series[0].WithPointColor(1, accent);
        chart.Options.ShowPointLegend = true;
        var context = new VisualRenderContext();
        var builder = new VisualSceneBuilder(new VisualSize(160, 100), context.Font);
        VisualCartesianCompiler.Build(chart, context, builder, new ChartRect(10, 10, 30, 20));
        var scene = builder.Build();
        var groups = scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "point").ToArray();
        Assert.Equal(first, groups[0].Metadata["data-cfx-label"]); Assert.Equal(second, groups[1].Metadata["data-cfx-label"]);
        Assert.Contains(scene.Regions, region => region.Label?.Contains(first) == true);
        Assert.Equal(2, VisualCartesianCompiler.LegendEntries(chart, context.Theme.Resolve(context.ThemeMode)).Count);
        var candles = scene.Nodes.OfType<VisualSceneRectangle>().Where(rect => rect.Role == "candlestick-body").ToArray();
        Assert.Equal(accent.R, candles[1].Fill!.Value.R); Assert.Equal(accent.G, candles[1].Fill!.Value.G);
    }

    [Fact]
    public void RegressionAndBoxSummary_PreserveCopiedOriginalInputsAndDetachedAlternatives() {
        var points = new[] { new ChartPoint(1, 3), new ChartPoint(2, 8), new ChartPoint(3, 7) };
        var samples = new[] { 8d, 1d, 3d, 9d, 6d, 2d };
        var trend = Chart.Create().AddTrendLine("Trend", points); var box = Chart.Create().AddBoxPlot("Distribution", 1, samples);
        points[1] = new ChartPoint(2, 999); samples[0] = 999;
        Assert.Equal(3, trend.Series[0].SourcePointCount); Assert.Equal(6, box.Series[0].SourcePointCount);
        var preparedTrend = trend.Prepare(new VisualRenderContext()); var preparedBox = box.Prepare(new VisualRenderContext());
        Assert.Contains(preparedTrend.Regions, region => region.Role == "source-observation" && region.Label == "x=2 y=8");
        Assert.Contains(preparedBox.Regions, region => region.Role == "source-sample" && region.Label == "value=8");
        Assert.DoesNotContain(preparedBox.Regions, region => region.Label?.Contains("999") == true);
        var svg = preparedBox.ToSvg(); box.Series[0].Points.Clear();
        Assert.Equal(svg, preparedBox.ToSvg());
    }

    [Fact]
    public void RangeArea_GapsSplitFillAndDashedMidlinesWhileFormattingEachIntervalOnce() {
        var chart = Fixture(ChartSeriesKind.RangeArea);
        chart.Series[0].Points.Add(new ChartPoint(3, 3, true)); chart.Series[0].Points.Add(new ChartPoint(3, 8));
        chart.Series[0].Points.Add(new ChartPoint(4, 4)); chart.Series[0].Points.Add(new ChartPoint(4, 7));
        chart.Series[0].FillPattern = ChartFillPattern.Crosshatch;
        var calls = 0;
        chart.Options.ValueFormatter = value => { calls++; return value.ToString(CultureInfo.InvariantCulture); };
        chart.WithAxes(false); chart.Options.ShowGrid = false;
        var prepared = chart.Prepare(new VisualRenderContext(frame: new VisualFrame(showLegend: false)));
        Assert.Equal(8, calls);
        Assert.Equal(2, prepared.Scene.Nodes.Count(node => node.Role == "range-area"));
        Assert.Equal(2, prepared.Scene.Nodes.OfType<VisualScenePath>().Count(path => path.Role == "range-midline" && path.Dash?.Count == 2));
        Assert.Contains(prepared.Scene.Nodes, node => node.Role == "range-area-pattern");
    }

    [Fact]
    public void SecondaryErrorAndFinancialMarks_UseTheirOwnExtentsAndLogarithmicMapper() {
        var chart = Chart.Create().AddLine("Primary", new[] { new ChartPoint(1, 2), new ChartPoint(2, 3) })
            .AddErrorBar("Secondary", new[] { new ChartErrorBar(1, 100, 10, 1000) })
            .AddOhlc("Price", new[] { new ChartCandlestick(2, 20, 2000, 5, 90) });
        chart.Series[1].YAxis = ChartAxisSide.Secondary; chart.Series[2].YAxis = ChartAxisSide.Secondary;
        chart.Options.SecondaryYAxis.Scale = ChartScaleKind.Logarithmic;
        var primary = ChartRange.FromChart(chart); var secondary = ChartRange.FromSecondaryYAxis(chart, primary);
        Assert.True(primary.MaxY < 10); Assert.True(secondary.MinY <= 5); Assert.True(secondary.MaxY >= 2000);
        var prepared = chart.Prepare(new VisualRenderContext());
        Assert.Contains(prepared.Scene.Nodes, node => node.Role == "ohlc-stem");
        Assert.Contains(prepared.Regions, region => region.Role == "axis-secondary-y-label");
    }

    [Fact]
    public void HorizontalTimeValues_PreserveCalendarTickAlignmentDisplayZoneAndCategoryLabels() {
        var date = new DateTime(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc).ToOADate();
        var chart = Chart.Create().AddHorizontalBar("Schedule", new[] { new ChartPoint(1, date), new ChartPoint(2, date + .25) });
        chart.Options.XAxis.WithTimeScale(TimeZoneInfo.CreateCustomTimeZone("TestPlusTwo", TimeSpan.FromHours(2), "TestPlusTwo", "TestPlusTwo"), true, "UTC+02");
        chart.Options.YAxis.Labels.Add(new ChartAxisLabel(1, "First event"));
        chart.Options.YAxis.Labels.Add(new ChartAxisLabel(2, "Second event"));
        var range = ChartRange.FromChart(chart);
        var ticks = ChartTicks.GenerateInside(chart.Options.XAxis, range.MinX, range.MaxX);
        var prepared = chart.Prepare(new VisualRenderContext());
        Assert.All(ticks, tick => Assert.Contains(prepared.Regions, region => region.Role == "axis-x-label"
            && region.Label == ChartTimeScale.Format(chart.Options.XAxis, tick) + " (" + tick.ToString("G17", CultureInfo.InvariantCulture) + ")"));
        Assert.Contains(prepared.Regions, region => region.Role == "axis-y-label" && region.Label?.StartsWith("First event", StringComparison.Ordinal) == true);
        Assert.Contains(prepared.Regions, region => region.Role == "axis-x-title" && region.Label == "UTC+02");
    }

    private static Chart Fixture(ChartSeriesKind kind) => kind switch {
        ChartSeriesKind.Bubble => Chart.Create().AddBubble("Bubbles", new[] { new ChartBubble(1, 3, 2), new ChartBubble(2, 6, 8) }),
        ChartSeriesKind.ErrorBar => Chart.Create().AddErrorBar("Errors", new[] { new ChartErrorBar(1, 3, 1, 5), new ChartErrorBar(2, 6, 4, 8) }),
        ChartSeriesKind.Candlestick => Chart.Create().AddCandlestick("Trades", new[] { new ChartCandlestick(1, 3, 6, 2, 5), new ChartCandlestick(2, 6, 8, 3, 4) }),
        ChartSeriesKind.Ohlc => Chart.Create().AddOhlc("Trades", new[] { new ChartCandlestick(1, 3, 6, 2, 5), new ChartCandlestick(2, 6, 8, 3, 4) }),
        ChartSeriesKind.RangeBand => Chart.Create().AddRangeBand("Band", new[] { new ChartRangeBand(1, 2, 5), new ChartRangeBand(2, 3, 7) }),
        ChartSeriesKind.RangeArea => Chart.Create().AddRangeArea("Envelope", new[] { new ChartRangeBand(1, 2, 5), new ChartRangeBand(2, 3, 7) }),
        ChartSeriesKind.Lollipop => Chart.Create().AddLollipop("Values", new[] { new ChartPoint(1, 3), new ChartPoint(2, -2) }),
        ChartSeriesKind.Dumbbell => Chart.Create().AddDumbbell("Change", new[] { new ChartDumbbell(1, 2, 5), new ChartDumbbell(2, 7, 3) }),
        ChartSeriesKind.RangeBar => Chart.Create().AddRangeBar("Intervals", new[] { new ChartInterval(1, 2, 5), new ChartInterval(2, 7, 3) }),
        ChartSeriesKind.BoxPlot => Chart.Create().AddBoxPlot("Distribution", new[] { new ChartBoxPlot(1, 1, 2, 3, 4, 5), new ChartBoxPlot(2, 2, 4, 5, 7, 8) }),
        ChartSeriesKind.HorizontalBar => Chart.Create().AddHorizontalBar("Values", new[] { new ChartPoint(1, 3), new ChartPoint(2, -2) }),
        ChartSeriesKind.Waterfall => Chart.Create().AddWaterfall("Movement", new[] { new ChartPoint(1, 3), new ChartPoint(2, -2) }),
        ChartSeriesKind.Slope => Chart.Create().AddSlope("Change", 3, 7),
        ChartSeriesKind.TrendLine => Chart.Create().AddTrendLine("Trend", new[] { new ChartPoint(1, 3), new ChartPoint(2, 5), new ChartPoint(3, 8) }),
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
}
