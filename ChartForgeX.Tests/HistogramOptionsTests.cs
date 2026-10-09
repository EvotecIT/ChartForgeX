using System;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Data;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class HistogramOptionsTests {
    [Fact]
    public void AuthoredIntervalsAssignBoundaryObservationsOnceAndDetachFromInput() {
        var boundaries = new[] { 0d, 1d, 4d, 10d };
        var layout = ChartHistogramBinLayout.FromBoundaries(boundaries);
        boundaries[1] = 9;
        var belowOne = BitConverter.Int64BitsToDouble(BitConverter.DoubleToInt64Bits(1d) - 1);
        var chart = Chart.Create().AddHistogram("Observations", new[] { 0, belowOne, 1, 4, 10 }, layout);
        var bins = chart.Series[0].HistogramBins;
        Assert.Equal(new[] { 2, 1, 2 }, bins.Select(bin => bin.Count));
        Assert.Equal(new[] { 1d, 3d, 6d }, Enumerable.Range(0, layout.Count).Select(layout.GetWidth));
        Assert.Equal(new[] { 0, 1 }, bins[0].SourceIndices);
        Assert.Equal(new[] { 2 }, bins[1].SourceIndices);
        Assert.Equal(new[] { 3, 4 }, bins[2].SourceIndices);
        Assert.Equal(5, chart.Series[0].SourcePointCount);
        Assert.Throws<NotSupportedException>(() => ((System.Collections.Generic.IList<int>)bins[0].SourceIndices)[0] = 99);
    }

    [Fact]
    public void GapsOutsideValuesAndInvalidIntervalsFailWithoutChangingChart() {
        var layout = ChartHistogramBinLayout.FromIntervals(new[] { (0d, 1d), (3d, 5d) });
        var chart = Chart.Create().AddLine("Existing", new[] { new ChartPoint(0, 1) });
        foreach (var outside in new[] { -1d, 1d, 2d, 5.1 }) {
            Assert.Throws<ArgumentOutOfRangeException>(() => chart.AddHistogram("Rejected", new[] { 0.5, outside }, layout));
            Assert.Single(chart.Series);
        }
        var accepted = Chart.Create().AddHistogram("Endpoints", new[] { 0d, 3d, 5d }, layout);
        Assert.Equal(new[] { 1d, 2d }, accepted.Series[0].Points.Select(point => point.Y));
        foreach (var intervals in new[] { new[] { (0d, 0d) }, new[] { (2d, 3d), (0d, 1d) }, new[] { (0d, 3d), (2d, 4d) }, new[] { (0d, double.PositiveInfinity) } })
            Assert.ThrowsAny<ArgumentException>(() => ChartHistogramBinLayout.FromIntervals(intervals));
        Assert.Throws<ArgumentException>(() => ChartHistogramBinLayout.FromBoundaries(new[] { 1d }));
        Assert.Throws<ArgumentException>(() => ChartHistogramBinLayout.FromBoundaries(new[] { 0d, 1d, 1d }));
    }

    [Theory]
    [InlineData(ChartHistogramAggregation.Count, 2, 1)]
    [InlineData(ChartHistogramAggregation.Sum, 0, -9)]
    [InlineData(ChartHistogramAggregation.Mean, 0, -9)]
    public void AggregatesPreserveCountsSourceQuantitiesAndUndefinedEmptyMeans(ChartHistogramAggregation aggregation, double first, double second) {
        var observations = new[] { new ChartPoint(.25, 6), new ChartPoint(.75, -6), new ChartPoint(2, -9) };
        var layout = ChartHistogramBinLayout.FromBoundaries(new[] { 0d, 1d, 3d, 5d });
        var chart = Chart.Create().WithAxes(false).WithDataLabels().AddHistogram("Quantity", observations, layout, aggregation);
        var bins = chart.Series[0].HistogramBins;
        Assert.Equal(new[] { 2, 1, 0 }, bins.Select(bin => bin.Count));
        Assert.Equal(first, bins[0].Value); Assert.Equal(second, bins[1].Value);
        Assert.Equal(aggregation == ChartHistogramAggregation.Mean ? (double?)null : 0, bins[2].Value);
        var prepared = Prepare(chart);
        Assert.Equal("true", Point(prepared, 0, 0).Metadata["data-cfx-bin-has-value"]);
        Assert.Equal("0,1", Point(prepared, 0, 0).Metadata["data-cfx-source-points"]);
        Assert.False(Point(prepared, 0, 0).Metadata.ContainsKey("data-cfx-source-point"));
        var empty = Point(prepared, 0, 2);
        Assert.Equal(aggregation == ChartHistogramAggregation.Mean ? "false" : "true", empty.Metadata["data-cfx-bin-has-value"]);
        Assert.Equal(aggregation != ChartHistogramAggregation.Mean, empty.Metadata.ContainsKey("data-cfx-y"));
        Assert.Equal(aggregation != ChartHistogramAggregation.Mean, empty.Metadata.ContainsKey("data-cfx-bin-value"));
        Assert.Contains(prepared.Regions, region => region.Role == "source-observation" && region.Label == "x=2 y=-9");
        var svg = prepared.ToSvg();
        observations[0] = new ChartPoint(99, 99);
        chart.Series[0].Points.Clear();
        Assert.Equal(svg, prepared.ToSvg());
    }

    [Fact]
    public void ExplicitEmptyInputRetainsBinsAndAutomaticLayoutsStillRequireMeasurements() {
        var layout = ChartHistogramBinLayout.FromBoundaries(new[] { 0d, 1d, 4d });
        var emptyCount = Chart.Create().AddHistogram("No rows", Array.Empty<double>(), layout);
        Assert.Equal(2, emptyCount.Series[0].Points.Count);
        Assert.Equal(0, emptyCount.Series[0].SourcePointCount);
        Assert.All(emptyCount.Series[0].HistogramBins, bin => { Assert.Equal(0, bin.Count); Assert.Equal(0, bin.Value); });
        var emptyMean = Chart.Create().AddHistogram("No means", Array.Empty<ChartPoint>(), layout, ChartHistogramAggregation.Mean);
        Assert.All(emptyMean.Series[0].HistogramBins, bin => Assert.Null(bin.Value));
        Assert.Equal(2, Prepare(emptyMean).Scene.Nodes.Count(node => node.Role == "bar"));
        Assert.Throws<ArgumentException>(() => Chart.Create().AddHistogram("Cannot infer", Array.Empty<double>()));
    }

    [Fact]
    public void DensityRectanglesUseActualWidthsAndEqualAreasAcrossSvgAndNativeRaster() {
        var layout = ChartHistogramBinLayout.FromBoundaries(new[] { 0d, 1d, 4d });
        var red = ChartColor.FromHex("#C52E3C");
        var chart = Chart.Create().WithAxes(false).WithBarStyle(ChartBarStyle.SegmentedCapsule)
            .AddHistogram("Equal quantity", new[] { .25, .75, 2d, 3d }, layout, ChartHistogramEncoding.Density, red);
        chart.Options.ShowGrid = false;
        chart.Options.XAxis.WithBounds(0, 4); chart.Options.YAxis.WithBounds(0, 2);
        var prepared = Prepare(chart);
        var narrow = Bounds(prepared, 0, 0); var wide = Bounds(prepared, 0, 1);
        Assert.Equal(narrow.Right, wide.Left, 8);
        Assert.Equal(3, wide.Width / narrow.Width, 8);
        Assert.Equal(3, narrow.Height / wide.Height, 8);
        Assert.Equal(narrow.Width * narrow.Height, wide.Width * wide.Height, 7);
        Assert.Equal("2", Point(prepared, 0, 1).Metadata["data-cfx-y"]);
        Assert.Equal("0.66666666666666663", Point(prepared, 0, 1).Metadata["data-cfx-rendered-y"]);
        Assert.Contains("density=", Point(prepared, 0, 1).Metadata["aria-label"]);
        var rectangles = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "bar").ToArray();
        Assert.All(rectangles, rectangle => Assert.Equal(0, rectangle.Radius));
        Assert.DoesNotContain(prepared.Scene.Nodes, node => node.Role != null && node.Role.StartsWith("bar-cap", StringComparison.Ordinal));
        var svg = XDocument.Parse(prepared.ToSvg());
        Assert.Equal(2, svg.Descendants().Count(element => (string?)element.Attribute("data-cfx-role") == "bar"));
        var raster = prepared.ToRgba(new VisualRenderOptions(supersampling: 1));
        foreach (var bounds in new[] { narrow, wide }) {
            // A point near the interval edge would be unpainted by the ordinary inset histogram style.
            var pixel = ((int)(bounds.Top + bounds.Height / 2) * raster.Width + (int)(bounds.Left + bounds.Width * .03)) * 4;
            Assert.Equal(red.R, raster.Pixels[pixel]); Assert.Equal(red.G, raster.Pixels[pixel + 1]); Assert.Equal(red.B, raster.Pixels[pixel + 2]);
        }
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(-1, false)]
    [InlineData(-1, true)]
    public void DensityRetainsItsZeroOriginWhenAuthoredBoundsCropTheRepresentedArea(int sign, bool clipping) {
        var layout = ChartHistogramBinLayout.FromBoundaries(new[] { 0d, 2d });
        var chart = Chart.Create().WithAxes(false).WithGrid(false)
            .AddHistogram("Quantity", new[] { new ChartPoint(1, 6 * sign) }, layout,
                ChartHistogramAggregation.Sum, ChartHistogramEncoding.Density);
        chart.Options.YAxis.WithBounds(sign > 0 ? 1 : -4, sign > 0 ? 4 : -1);
        chart.Options.ClipMarksToPlot = clipping;
        var prepared = Prepare(chart);
        var mark = Bounds(prepared, 0, 0);
        chart.Options.YAxis.WithBounds(sign > 0 ? 0 : -3, sign > 0 ? 3 : 0);
        var reference = Bounds(Prepare(chart), 0, 0);
        // Density represents area from zero, even when authored bounds crop that area.
        Assert.Equal(reference.Height, mark.Height, 8);
        Assert.True(sign > 0 ? mark.Bottom > reference.Bottom : mark.Top < reference.Top);
        Assert.Equal((6 * sign).ToString(System.Globalization.CultureInfo.InvariantCulture), Point(prepared, 0, 0).Metadata["data-cfx-y"]);
        Assert.Equal((3 * sign).ToString(System.Globalization.CultureInfo.InvariantCulture), Point(prepared, 0, 0).Metadata["data-cfx-rendered-y"]);
        Assert.True(prepared.ToPng().Length > 64);
    }

    [Fact]
    public void DensityStacksAndRangesUseEncodedHeightWhileTotalsRetainSignedRawAggregates() {
        var layout = ChartHistogramBinLayout.FromBoundaries(new[] { 0d, 2d, 5d });
        var chart = Chart.Create().WithStackedBars().WithStackTotals()
            .AddHistogram("A", new[] { new ChartPoint(1, 6), new ChartPoint(3, -9) }, layout, ChartHistogramAggregation.Sum, ChartHistogramEncoding.Density)
            .AddHistogram("B", new[] { new ChartPoint(1, 2), new ChartPoint(3, -3) }, layout, ChartHistogramAggregation.Sum, ChartHistogramEncoding.Density);
        var prepared = Prepare(chart);
        Assert.Equal("3", Point(prepared, 1, 0).Metadata["data-cfx-base"]);
        Assert.Equal("4", Point(prepared, 1, 0).Metadata["data-cfx-stack-end"]);
        Assert.Equal("8", Point(prepared, 1, 0).Metadata["data-cfx-stack-source-total"]);
        Assert.Equal("-4", Point(prepared, 1, 1).Metadata["data-cfx-stack-end"]);
        Assert.Equal("-12", Point(prepared, 1, 1).Metadata["data-cfx-stack-source-total"]);
        var totals = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(node => node.Role == "stack-total").ToArray();
        Assert.Contains(totals, total => total.Metadata["data-cfx-y"] == "4" && total.Metadata["data-cfx-source-total"] == "8");
        Assert.Contains(totals, total => total.Metadata["data-cfx-y"] == "-4" && total.Metadata["data-cfx-source-total"] == "-12");
        var range = ChartRange.FromChart(chart);
        Assert.Equal(-4, range.MinY); Assert.InRange(range.MaxY, 4, 5);
        Assert.Equal(Bounds(prepared, 0, 0).Top, Bounds(prepared, 1, 0).Bottom, 8);
    }

    [Fact]
    public void DensityRejectsNonlinearScalesNormalizationSubdivisionAndIncompatibleStacks() {
        Chart Density() => Chart.Create().AddHistogram("Density", new[] { .5 }, ChartHistogramBinLayout.FromBoundaries(new[] { 0d, 1d }), ChartHistogramEncoding.Density);
        foreach (var scale in new[] { ChartScaleKind.Logarithmic, ChartScaleKind.SymmetricLogarithmic }) {
            var chart = Density(); chart.Options.XAxis.Scale = scale;
            Assert.Throws<InvalidOperationException>(() => Prepare(chart));
            chart = Density(); chart.Options.YAxis.Scale = scale;
            Assert.Throws<InvalidOperationException>(() => Prepare(chart));
        }
        var normalized = Density().WithStackedBars(); normalized.Series[0].WithNormalization(100);
        Assert.Throws<InvalidOperationException>(() => Prepare(normalized));
        var grouped = Density().AddHistogram("Other", new[] { .5 }, ChartHistogramBinLayout.FromBoundaries(new[] { 0d, 1d }), ChartHistogramEncoding.Density);
        Assert.Throws<InvalidOperationException>(() => Prepare(grouped));
        var mixed = Density().WithStackedBars().AddBar("Count", new[] { new ChartPoint(.5, 2) });
        Assert.Throws<InvalidOperationException>(() => Prepare(mixed));
        var overlapping = Density().WithStackedBars().AddHistogram("Other bounds", new[] { .5 }, ChartHistogramBinLayout.FromBoundaries(new[] { 0d, 2d }), ChartHistogramEncoding.Density);
        Assert.Throws<InvalidOperationException>(() => Prepare(overlapping));
        var degenerate = ChartHistogramBinLayout.FromCount(1, 1, 1, roundBounds: false);
        Assert.Throws<ArgumentException>(() => Chart.Create().AddHistogram("No area", new[] { 1d }, degenerate, ChartHistogramEncoding.Density));
    }

    [Fact]
    public void DifferentIntervalsWithIdenticalCentersNeverCollapseIntoOneStackCoordinate() {
        var left = ChartHistogramBinLayout.FromBoundaries(new[] { 0d, 1d, 3d, 7d, 8d, 10d });
        var right = ChartHistogramBinLayout.FromBoundaries(new[] { 0d, 1d, 4d, 6d, 8d, 10d });
        var chart = Chart.Create().WithStackedBars().AddHistogram("Wide", new[] { 5d }, left).AddHistogram("Narrow", new[] { 5d }, right);
        var prepared = Prepare(chart);
        Assert.Equal("0", Point(prepared, 1, 2).Metadata["data-cfx-base"]);
        Assert.Equal("1", Point(prepared, 1, 2).Metadata["data-cfx-stack-end"]);
        Assert.Equal(2, Bounds(prepared, 0, 2).Width / Bounds(prepared, 1, 2).Width, 8);
        var equivalent = Chart.Create().WithStackedBars().AddHistogram("First", new[] { 5d }, left)
            .AddHistogram("Same bins", new[] { 5d }, ChartHistogramBinLayout.FromBoundaries(new[] { 0d, 1d, 3d, 7d, 8d, 10d }));
        Assert.Equal("1", Point(Prepare(equivalent), 1, 2).Metadata["data-cfx-base"]);
    }

    [Fact]
    public void TypedBinningUsesCanonicalIntervalsSourceOrderAndTrueHistogramGeometry() {
        var rows = ChartDataset<double>.From(new[] { 3d, 0d, 1d, 4d });
        var layout = ChartHistogramBinLayout.FromBoundaries(new[] { 0d, 1d, 4d });
        var bins = rows.Bin(value => value, layout);
        Assert.Equal(new[] { 1 }, bins[0].SourceIndices);
        Assert.Equal(new[] { 0, 2, 3 }, bins[1].SourceIndices);
        var chart = Chart.Create().AddHistogram("Typed", bins);
        Assert.Same(layout, chart.Series[0].HistogramBinLayout);
        Assert.Equal(new[] { 1d, 3d }, chart.Series[0].HistogramBins.Select(bin => bin.Value!.Value));
        var prepared = Prepare(chart);
        Assert.Equal(3, Bounds(prepared, 0, 1).Width / Bounds(prepared, 0, 0).Width, 8);
        Assert.Contains(prepared.Regions, region => region.Id == "series-0-source-0" && region.Label == "x=3 y=1");
        var empty = ChartDataset<double>.Empty.Bin(value => value, layout);
        Assert.Equal(layout.Count, empty.Count);
        Assert.Equal(layout.Count, Chart.Create().AddHistogram("Empty typed", empty).Series[0].Points.Count);
        Assert.Throws<ArgumentException>(() => Chart.Create().AddHistogram("Partial", bins.Filter(bin => bin.Index == 0)));
        var differentSource = ChartDataset<double>.From(new[] { .5, 2d }).Bin(value => value, layout);
        Assert.Throws<ArgumentException>(() => Chart.Create().AddHistogram("Mixed provenance", ChartDataset<ChartDataBin<double>>.From(new[] { bins[0], differentSource[1] })));
        var raw = Chart.Create().AddHistogram("Raw", rows, 2);
        var automatic = Chart.Create().AddHistogram("Typed default", rows.Bin(value => value, 2));
        Assert.Equal(raw.Series[0].Points.Select(point => point.X), automatic.Series[0].Points.Select(point => point.X));
        Assert.Equal(raw.Series[0].Points.Select(point => point.Y), automatic.Series[0].Points.Select(point => point.Y));
    }

    [Fact]
    public void HistogramConstructionPreservesAuthoredAxisLabelsAndRejectsStaleAggregateEdits() {
        var chart = Chart.Create(); var label = new ChartAxisLabel(1, "Authored measurement label");
        chart.Options.XAxisLabels.Add(label);
        chart.AddHistogram("First", new[] { 1d, 2d }, ChartHistogramBinLayout.FromBoundaries(new[] { 0d, 1d, 4d }));
        chart.AddHistogram("Second", new[] { 1d }, ChartHistogramBinLayout.FromBoundaries(new[] { 0d, 2d, 4d }));
        Assert.Equal(label, Assert.Single(chart.Options.XAxisLabels));
        chart.Series[0].Points[0] = new ChartPoint(.5, 99);
        Assert.Throws<InvalidOperationException>(() => Prepare(chart));
    }

    [Fact]
    public void FiniteMeansDoNotOverflowAndNonfiniteSumsOrDensitiesAreRejected() {
        var layout = ChartHistogramBinLayout.FromBoundaries(new[] { 0d, 1d });
        var large = new[] { new ChartPoint(.5, 1e308), new ChartPoint(.5, 1e308) };
        Assert.Equal(1e308, Chart.Create().AddHistogram("Mean", large, layout, ChartHistogramAggregation.Mean).Series[0].HistogramBins[0].Value);
        Assert.Throws<ArgumentOutOfRangeException>(() => Chart.Create().AddHistogram("Sum", large, layout, ChartHistogramAggregation.Sum));
        var cancelled = new[] { new ChartPoint(.5, 1e308), new ChartPoint(.5, 1), new ChartPoint(.5, -1e308) };
        Assert.Equal(1, Chart.Create().AddHistogram("Cancellation", cancelled, layout, ChartHistogramAggregation.Sum).Series[0].HistogramBins[0].Value!.Value, 8);
        Assert.Throws<ArgumentOutOfRangeException>(() => Chart.Create().AddHistogram("Overflow density", new[] { new ChartPoint(double.Epsilon, 1) },
            ChartHistogramBinLayout.FromBoundaries(new[] { 0d, double.Epsilon }), ChartHistogramAggregation.Sum, ChartHistogramEncoding.Density));
    }

    [Fact]
    public void DensitySupportsLinearTimeMeasurementsAndIndependentDisjointLayouts() {
        var first = ChartHistogramBinLayout.FromBoundaries(new[] { 45000d, 45001d });
        var second = ChartHistogramBinLayout.FromBoundaries(new[] { 45002d, 45004d });
        var chart = Chart.Create().AddHistogram("First day", new[] { 45000.5 }, first, ChartHistogramEncoding.Density)
            .AddHistogram("Later days", new[] { 45003d }, second, ChartHistogramEncoding.Density);
        chart.Options.XAxis.Scale = ChartScaleKind.Time;
        var prepared = Prepare(chart);
        Assert.Equal(2, Bounds(prepared, 1, 0).Width / Bounds(prepared, 0, 0).Width, 8);
        Assert.Equal(2, Bounds(prepared, 0, 0).Height / Bounds(prepared, 1, 0).Height, 8);
        Assert.True(Bounds(prepared, 0, 0).Right < Bounds(prepared, 1, 0).Left);
    }

    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(new VisualRenderContext(
        new VisualLayoutOptions(new VisualSize(640, 400)), frame: new VisualFrame(showLegend: false)));
    private static ChartRect Bounds(PreparedVisual prepared, int series, int point) =>
        prepared.Regions.Single(region => region.Id == "series-" + series + "-point-" + point && region.Role == "point").Bounds;
    private static VisualSceneGroup Point(PreparedVisual prepared, int series, int point) =>
        prepared.Scene.Nodes.OfType<VisualSceneGroup>().Single(node => node.Id == "series-" + series + "-point-" + point && node.Role == "point");
}
