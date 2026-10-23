using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class HorizontalWaterfallTests {
    [Fact]
    public void OrientationDefaultsToVerticalRejectsUnknownValuesAndPreparedOutputDetaches() {
        var chart = Sample();
        Assert.Equal(ChartOrientation.Vertical, chart.Options.Waterfall.Orientation);
        var vertical = chart.Prepare(VisualExportRequest.ForChart(chart).Context).ToSvg();
        Assert.Same(chart, chart.ConfigureWaterfall(options => options.Orientation = ChartOrientation.Vertical));
        Assert.Equal(vertical, chart.Prepare(VisualExportRequest.ForChart(chart).Context).ToSvg());
        Assert.Throws<ArgumentNullException>(() => chart.ConfigureWaterfall(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.Options.Waterfall.Orientation = (ChartOrientation)99);
        chart.ConfigureWaterfall(options => options.Orientation = ChartOrientation.Horizontal);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        Assert.NotEqual(vertical, svg);
        chart.Options.Waterfall.Orientation = ChartOrientation.Vertical;
        chart.Options.YAxis.Labels.Add(new ChartAxisLabel(4, "Changed"));
        chart.Options.YAxis.WithLabelFormatter(_ => throw new InvalidOperationException("Changed category callback"));
        chart.ClearSeries();
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void PhysicalAxesMapIntervalsConnectorsAndUnsortedCategoriesWithoutChangingSourceFacts(bool reverseValue, bool reverseCategory) {
        var chart = Horizontal().WithAxes(false).WithLegend(false).WithBarStyle(ChartBarStyle.Flat);
        chart.Options.XAxis.WithBounds(-50, 150).WithReversal(reverseValue);
        chart.Options.YAxis.WithBounds(0, 9).WithReversal(reverseCategory);
        chart.AddVerticalLine(25).AddHorizontalLine(4.5);
        var scene = Scene(chart);
        var regions = scene.Regions.Where(region => region.Role is "point" or "waterfall-subtotal" or "waterfall-total").ToArray();
        var coordinates = new[] { 4d, 1, 3, 2, 5, 6, 7, 8 };
        var starts = new[] { 0d, 0, 100, 100, -40, -40, 0, -15 };
        var ends = new[] { 100d, 100, -40, -40, -40, -15, -15, -5 };
        double X(double value) => (reverseValue ? (150 - value) : (value + 50)) * 4;
        double Y(double value) => (reverseCategory ? value : 9 - value) / 9 * 400;
        Assert.Equal(coordinates.Length, regions.Length);
        for (var index = 0; index < regions.Length; index++) {
            var bounds = regions[index].Bounds;
            Assert.Equal(Math.Min(X(starts[index]), X(ends[index])), bounds.Left, 8);
            Assert.Equal(Math.Abs(X(ends[index]) - X(starts[index])), bounds.Width, 8);
            Assert.Equal(Y(coordinates[index]), bounds.Top + bounds.Height / 2, 8);
            Assert.InRange(bounds.Height, 1, 40);
        }
        var connectors = scene.Nodes.OfType<VisualSceneLine>().Where(line => line.Role == "waterfall-connector").ToArray();
        Assert.Equal(6, connectors.Length);
        Assert.All(connectors, connector => Assert.Equal(connector.Start.X, connector.End.X));
        Assert.Equal(X(100), connectors[0].Start.X, 8);
        Assert.Equal(X(100), connectors[1].Start.X, 8);
        Assert.Equal(X(-40), connectors[2].Start.X, 8);
        Assert.Equal(X(25), scene.Regions.Single(region => region.Id == "annotation-0").Bounds.Left, 8);
        Assert.Equal(Y(4.5), scene.Regions.Single(region => region.Id == "annotation-1").Bounds.Top, 8);
        var groups = Marks(scene);
        Assert.Equal("1", groups[3].Metadata["data-cfx-source-points"]);
        Assert.Equal("subtotal:2:sources:1", groups[3].Metadata["data-cfx-derived-identity"]);
        Assert.Equal("3", groups[7].Metadata["data-cfx-source-point"]);
        Assert.Equal("10", groups[7].Metadata["data-cfx-delta"]);
        Assert.Equal("0", groups[4].Metadata["data-cfx-value"]);
        Assert.Equal(0, regions[4].Bounds.Width);
        var range = ChartRange.FromChart(Horizontal());
        Assert.Equal(-40, range.MinX); Assert.Equal(100, range.MaxX);
        Assert.Equal(.5, range.MinY); Assert.Equal(8.5, range.MaxY);
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture);
            chart.WithAxes().WithDataLabels().WithBarStyle(ChartBarStyle.SegmentedCapsule).WithTitle("Unsorted categories and consecutive checkpoints");
            var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
            var name = "horizontal-waterfall-edges-x" + reverseValue + "-y" + reverseCategory;
            File.WriteAllText(Path.Combine(capture, name + ".native.svg"), prepared.ToSvg());
            File.WriteAllBytes(Path.Combine(capture, name + ".native.png"), prepared.ToPng());
        }
    }

    [Fact]
    public void CategoryCaptionsShareExplicitYMappingsFormattersDefaultsAndTimeAcrossAxesMarksAndLegends() {
        var chart = Horizontal().WithPointLegend().WithXLabels("Wrong", "Wrong", "Wrong", "Wrong", "Wrong", "Wrong", "Wrong", "Wrong");
        chart.Options.XAxis.WithLabelFormatter(value => "$" + value.ToString("0", CultureInfo.InvariantCulture));
        var calls = new Dictionary<double, int>();
        chart.Options.YAxis.WithLabelFormatter(value => {
            calls[value] = calls.TryGetValue(value, out var count) ? count + 1 : 1;
            return "Stage " + value.ToString("0", CultureInfo.InvariantCulture);
        });
        chart.Options.YAxis.Labels.Add(new ChartAxisLabel(2, "Costs"));
        AssertCaptions(chart, new[] { "Stage 4", "Stage 1", "Stage 3", "Costs", "Stage 5", "Stage 6", "Stage 7", "Stage 8" });
        Assert.Equal(7, calls.Count); Assert.All(calls.Values, count => Assert.Equal(1, count));
        chart.Options.YAxis.WithLabelFormatter(null); chart.Options.YAxis.Labels.Clear(); chart.Options.XAxis.Labels.Clear();
        chart.Options.Labels.Subtotal = "Stage sum"; chart.Options.Labels.Total = "Balance";
        AssertCaptions(chart, new[] { "4", "Stage sum", "3", "Stage sum", "Stage sum", "6", "Balance", "8" });

        var date = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        var time = Chart.Create().WithPointLegend().ConfigureWaterfall(options => options.Orientation = ChartOrientation.Horizontal)
            .AddWaterfall("Dates", new[] { ChartWaterfallItem.Delta(date, 20), ChartWaterfallItem.Total(date.AddDays(1)) });
        time.Options.YAxis.WithTimeScale(TimeZoneInfo.Utc);
        AssertCaptions(time, new[] { ChartTimeScale.Format(time.Options.YAxis, date.ToOADate()), "Total" });
        var fallback = Chart.Create().WithPointLegend().WithXLabels("Income", "Balance")
            .ConfigureWaterfall(options => options.Orientation = ChartOrientation.Horizontal)
            .AddWaterfall("Movement", new[] { ChartWaterfallItem.Delta(1, 20), ChartWaterfallItem.Total(2) });
        AssertCaptions(fallback, new[] { "Income", "Balance" });
    }

    [Theory]
    [InlineData(120d, false)]
    [InlineData(-120d, true)]
    public void HorizontalOpeningDefaultsShareLocalizedCaptionsAndRetainRawSourceAndContributorFacts(double opening, bool reverse) {
        var chart = Opening(opening).WithDataLabels().WithBarStyle(ChartBarStyle.SegmentedCapsule);
        chart.ConfigureLabels(labels => { labels.OpeningBalance = "Initial balance"; labels.Subtotal = "Stage sum"; labels.Total = "Balance"; });
        chart.Options.XAxis.WithBounds(-160, 160).WithReversal(reverse);
        chart.Options.YAxis.WithReversal();
        var expected = new[] { "Initial balance", "2", "Stage sum", "4", "Stage sum", "Balance" };
        var prepared = AssertCaptions(chart, expected);
        Assert.Equal(expected, prepared.Scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == "legend-label")
            .Select(text => string.Concat(text.Text.Lines.Select(line => line.Text))));
        var marks = Marks(prepared.Scene);
        var first = marks[0].Metadata;
        Assert.Equal(3, chart.Series[0].SourcePointCount);
        Assert.Equal("opening-balance", first["data-cfx-waterfall-kind"]);
        Assert.Equal("horizontal", first["data-cfx-orientation"]);
        Assert.Equal("1", first["data-cfx-x"]); Assert.Equal("0", first["data-cfx-source-point"]);
        Assert.Equal("0", first["data-cfx-start"]);
        Assert.Equal(opening.ToString(CultureInfo.InvariantCulture), first["data-cfx-value"]);
        Assert.Equal(first["data-cfx-value"], first["data-cfx-end"]);
        Assert.False(first.ContainsKey("data-cfx-delta")); Assert.False(first.ContainsKey("data-cfx-derived"));
        Assert.False(first.ContainsKey("data-cfx-derived-identity")); Assert.False(first.ContainsKey("data-cfx-source-points"));
        Assert.False(ChartWaterfallSteps.Create(chart.Series[0])[0].IsCheckpoint);
        Assert.Equal("1", marks[1].Metadata["data-cfx-source-point"]);
        Assert.Equal("2", marks[3].Metadata["data-cfx-source-point"]);
        Assert.Equal("1", marks[2].Metadata["data-cfx-source-points"]);
        Assert.Equal("2", marks[4].Metadata["data-cfx-source-points"]);
        Assert.Equal("0,1,2", marks[5].Metadata["data-cfx-source-points"]);
        Assert.Equal((opening > 0 ? -30 : 30).ToString(CultureInfo.InvariantCulture), marks[2].Metadata["data-cfx-value"]);
        Assert.Equal((opening > 0 ? -40 : 40).ToString(CultureInfo.InvariantCulture), marks[5].Metadata["data-cfx-value"]);
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture);
            var name = "horizontal-waterfall-opening-source-" + (opening > 0 ? "positive" : "negative");
            File.WriteAllText(Path.Combine(capture, name + ".native.svg"), prepared.ToSvg());
            File.WriteAllBytes(Path.Combine(capture, name + ".native.png"), prepared.ToPng());
        }
    }

    [Fact]
    public void HorizontalOpeningCategoryLabelsPrecedeCallbacksAndPreparationFormatsEachUnmappedCategoryOnce() {
        var chart = Opening(120).WithXLabels("Previous balance");
        chart.Options.YAxis.Labels.Add(new ChartAxisLabel(1, "Carried balance"));
        var calls = new Dictionary<double, int>();
        chart.Options.YAxis.WithLabelFormatter(value => {
            calls[value] = calls.TryGetValue(value, out var count) ? count + 1 : 1;
            return "Stage " + value.ToString("0", CultureInfo.InvariantCulture);
        });
        _ = VisualExportRequest.ForChart(chart);
        Assert.Empty(calls);
        AssertCaptions(chart, new[] { "Carried balance", "Stage 2", "Stage 3", "Stage 4", "Stage 5", "Stage 6" });
        Assert.Equal(5, calls.Count); Assert.False(calls.ContainsKey(1)); Assert.All(calls.Values, count => Assert.Equal(1, count));
        calls.Clear(); chart.Options.YAxis.Labels.Clear();
        AssertCaptions(chart, new[] { "Previous balance", "Stage 2", "Stage 3", "Stage 4", "Stage 5", "Stage 6" });
        Assert.Equal(5, calls.Count); Assert.False(calls.ContainsKey(1)); Assert.All(calls.Values, count => Assert.Equal(1, count));
        calls.Clear(); chart.Options.XAxis.Labels.Clear();
        var prepared = AssertCaptions(chart, new[] { "Stage 1", "Stage 2", "Stage 3", "Stage 4", "Stage 5", "Stage 6" });
        Assert.Equal(6, calls.Count); Assert.All(calls.Values, count => Assert.Equal(1, count));
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        chart.Options.YAxis.WithLabelFormatter(_ => throw new InvalidOperationException("Changed category callback"));
        chart.ConfigureLabels(labels => labels.OpeningBalance = "Changed"); chart.ClearSeries();
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        Assert.All(calls.Values, count => Assert.Equal(1, count));
    }

    [Fact]
    public void LegendVisibilityCountsAuthoredItemsWithoutFormattingCategoriesOrCountingLegacyTotals() {
        var cases = new[] {
            (Chart.Create().AddWaterfall("Only total", new[] { ChartWaterfallItem.Total(1) }), 1, 0, 0),
            (Chart.Create().AddWaterfall("Checkpoints", new[] { ChartWaterfallItem.Subtotal(1), ChartWaterfallItem.Total(2) }), 2, 0, 2),
            (Chart.Create().AddWaterfall("Typed", new[] { ChartWaterfallItem.Delta(1, 20), ChartWaterfallItem.Total(2) }), 2, 1, 2),
            (Chart.Create().AddWaterfall("Legacy one", new[] { new ChartPoint(1, 20) }), 2, 1, 0),
            (Chart.Create().AddWaterfall("Legacy two", new[] { new ChartPoint(1, 20), new ChartPoint(2, -5) }), 3, 2, 2),
            (Chart.Create().AddHorizontalBar("Bars", new[] { new ChartPoint(1, 20), new ChartPoint(2, 15) }), 2, 2, 2)
        };
        foreach (var (chart, markCount, sourceCount, legendCount) in cases) {
            chart.WithPointLegend().ConfigureWaterfall(options => options.Orientation = ChartOrientation.Horizontal);
            var calls = new Dictionary<double, int>();
            chart.Options.YAxis.WithLabelFormatter(value => {
                calls[value] = calls.TryGetValue(value, out var count) ? count + 1 : 1;
                return "Category " + value.ToString("0", CultureInfo.InvariantCulture);
            });
            var request = VisualExportRequest.ForChart(chart);
            Assert.Empty(calls);
            Assert.Equal(legendCount > 0, request.Context.Frame.ShowLegend);
            var prepared = chart.Prepare(request.Context);
            Assert.Equal(sourceCount, chart.Series[0].SourcePointCount);
            Assert.Equal(markCount, Marks(prepared.Scene).Length);
            Assert.Equal(legendCount, prepared.Scene.Nodes.OfType<VisualSceneGroup>().Count(group => group.Role == "legend-entry"));
            Assert.Equal(markCount, calls.Count);
            Assert.All(calls.Values, count => Assert.Equal(1, count));
        }
    }

    [Theory]
    [InlineData(ChartScaleKind.SymmetricLogarithmic)]
    [InlineData(ChartScaleKind.Logarithmic)]
    public void HorizontalValueTransformsAndLegacyTotalRetainTheirBaselineContracts(ChartScaleKind scale) {
        var chart = Chart.Create().WithAxes(false).WithLegend(false).WithBarStyle(ChartBarStyle.Flat)
            .ConfigureWaterfall(options => options.Orientation = ChartOrientation.Horizontal)
            .AddWaterfall("Movement", new[] { new ChartPoint(1, 10), new ChartPoint(2, 90), new ChartPoint(3, -80) });
        chart.Options.XAxis.WithScale(scale);
        var range = ChartRange.FromChart(chart);
        if (scale == ChartScaleKind.Logarithmic) { Assert.Equal(1, range.MinX); Assert.Equal(100, range.MaxX); }
        else { chart.Options.XAxis.WithBounds(-100, 100); range = ChartRange.FromChart(chart); }
        var scene = Scene(chart); var groups = Marks(scene);
        var total = Assert.Single(groups, group => group.Role == "waterfall-total");
        Assert.Equal("series-0-total", total.Id);
        Assert.Equal("0,1,2", total.Metadata["data-cfx-source-points"]);
        Assert.False(total.Metadata.ContainsKey("data-cfx-source-point"));
        Assert.Equal("20", total.Metadata["data-cfx-value"]);
        var first = scene.Regions.Single(region => region.Id == "series-0-point-0").Bounds;
        if (scale == ChartScaleKind.Logarithmic) {
            Assert.Equal(0, first.Left); Assert.Equal(400, first.Width, 8);
        } else {
            Assert.Equal(400, first.Left, 8);
            Assert.Equal(Math.Log10(11) / Math.Log10(101) * 400, first.Width, 8);
        }
        chart.Series[0].Points[0] = new ChartPoint(1, -10);
        if (scale == ChartScaleKind.Logarithmic) Assert.Throws<InvalidOperationException>(() => chart.Prepare(new VisualRenderContext()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReversedCapsAndAutomaticLabelsFollowMappedEndsAndContainedInkUsesActualPaint(bool reverse) {
        var chart = Chart.Create().WithSize(800, 400).WithHeader(false).WithAxes(false).WithLegend(false).WithGrid(false)
            .WithDataLabels().WithBarStyle(ChartBarStyle.SegmentedCapsule)
            .ConfigureWaterfall(options => options.Orientation = ChartOrientation.Horizontal)
            .AddWaterfall("Movement", new[] { ChartWaterfallItem.Delta(1, 40), ChartWaterfallItem.Delta(2, -20), ChartWaterfallItem.Total(3) }, ChartColor.FromHex("#172554"));
        chart.Options.XAxis.WithBounds(-100, 100).WithReversal(reverse);
        var scene = Scene(chart);
        var marks = scene.Regions.Where(region => region.Role is "point" or "waterfall-total").ToArray();
        var caps = scene.Nodes.OfType<VisualSceneLine>().Where(line => line.Role == "waterfall-bar-cap").ToArray();
        var labels = scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == "data-label").ToArray();
        Assert.Equal(3, caps.Length); Assert.Equal(3, labels.Length);
        for (var index = 0; index < marks.Length; index++) {
            var right = (index != 1) != reverse; var bounds = marks[index].Bounds;
            Assert.Equal(caps[index].Start.X, caps[index].End.X);
            Assert.True(right ? caps[index].Start.X > bounds.Left + bounds.Width / 2 : caps[index].Start.X < bounds.Left + bounds.Width / 2);
            var text = labels[index]; var caption = new ChartRect(text.X, text.Baseline - text.Text.Ascent, text.Text.Metrics.Width, text.Text.Metrics.Height);
            Assert.True(right ? caption.Left > bounds.Right : caption.Right < bounds.Left);
        }
        chart.WithDataLabelPlacement(ChartDataLabelPlacement.Inside);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var fill = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().First(node => node.Role == "waterfall-bar").Fill!.Value;
        var context = VisualExportRequest.ForChart(chart).Context;
        var backdrop = ChartStateMark.Backdrop(chart.Options, context.Theme.Resolve(context.ThemeMode), context.Frame);
        var expected = ChartColorMath.AccessibleTextOnBackground(ChartColorMath.Blend(backdrop, ChartColor.FromRgb(fill.R, fill.G, fill.B), fill.A / 255d));
        var insideLabels = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == "data-label").ToArray();
        Assert.Equal(3, insideLabels.Length);
        Assert.All(insideLabels, text => Assert.Equal(expected, text.Color));
        Assert.True(prepared.ToPng().Length > 64);
    }

    [Fact]
    public void OrientationMutationRoutesSecondaryWaterfallToThePhysicalValueAxis() {
        var chart = Sample(); chart.Series[0].UseSecondaryValueAxis();
        Assert.NotEmpty(chart.Prepare(new VisualRenderContext()).Regions);
        chart.Options.Waterfall.Orientation = ChartOrientation.Horizontal;
        var horizontal = chart.Prepare(new VisualRenderContext());
        Assert.NotNull(horizontal.SecondaryXProjection); Assert.Null(horizontal.SecondaryYProjection);
        Assert.NotEmpty(horizontal.ToPng());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CollapsedNonzeroCapsulesKeepTheirMappedDirectionWithoutInventingCheckpointValues(bool reverse) {
        var chart = Chart.Create().WithAxes(false).WithLegend(false).WithBarStyle(ChartBarStyle.SegmentedCapsule)
            .ConfigureWaterfall(options => options.Orientation = ChartOrientation.Horizontal)
            .AddWaterfall("Movement", new[] { ChartWaterfallItem.Delta(1, 1e20), ChartWaterfallItem.Delta(2, -1),
                ChartWaterfallItem.Subtotal(3), ChartWaterfallItem.Subtotal(4) });
        chart.Options.XAxis.WithBounds(0, 1e21).WithReversal(reverse);
        var scene = Scene(chart);
        var collapsed = scene.Regions.Single(region => region.Id == "series-0-point-1").Bounds;
        Assert.Equal(1, collapsed.Width);
        Assert.Equal(reverse ? 720 : 79, collapsed.Left, 8);
        var groups = Marks(scene);
        Assert.Equal("-1", groups[1].Metadata["data-cfx-delta"]);
        Assert.Equal(groups[1].Metadata["data-cfx-start"], groups[1].Metadata["data-cfx-end"]);
        Assert.Equal("0", groups[3].Metadata["data-cfx-value"]);
        Assert.Equal(0, scene.Regions.Single(region => region.Id == "series-0-point-3").Bounds.Width);
        var cap = scene.Nodes.OfType<VisualSceneLine>().Where(line => line.Role == "waterfall-bar-cap").ElementAt(1);
        Assert.Equal(reverse ? collapsed.Right - .5 : collapsed.Left + .5, cap.Start.X, 8);
        Assert.Equal(3, scene.Nodes.Count(node => node.Role == "waterfall-bar-cap"));
    }

    private static PreparedVisual AssertCaptions(Chart chart, string[] expected) {
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var groups = Marks(prepared.Scene);
        Assert.Equal(expected, groups.Select(group => group.Metadata["data-cfx-category-label"]));
        var categoryRegions = prepared.Regions.Where(region => region.Role == "axis-y-label").ToArray();
        foreach (var caption in expected) Assert.Contains(categoryRegions, region => region.Label!.StartsWith(caption + " (", StringComparison.Ordinal));
        var legends = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "legend-entry" && group.Metadata.ContainsKey("data-cfx-waterfall-kind")).ToArray();
        Assert.Equal(expected, legends.Select(group => group.Metadata["data-cfx-category-label"]));
        return prepared;
    }

    private static VisualSceneGroup[] Marks(VisualScene scene) => scene.Nodes.OfType<VisualSceneGroup>().Where(group =>
        group.Role is "point" or "waterfall-subtotal" or "waterfall-total").ToArray();

    private static Chart Horizontal() => Sample().ConfigureWaterfall(options => options.Orientation = ChartOrientation.Horizontal);

    private static Chart Opening(double value) => Chart.Create().WithSize(900, 500).WithPointLegend()
        .ConfigureWaterfall(options => options.Orientation = ChartOrientation.Horizontal).AddWaterfall("Movement", new[] {
            ChartWaterfallItem.OpeningBalance(1, value), ChartWaterfallItem.Delta(2, value > 0 ? -30 : 30), ChartWaterfallItem.Subtotal(3),
            ChartWaterfallItem.Delta(4, value > 0 ? -130 : 130), ChartWaterfallItem.Subtotal(5), ChartWaterfallItem.Total(6)
        });

    private static Chart Sample() => Chart.Create().WithSize(900, 500).AddWaterfall("Movement", new[] {
        ChartWaterfallItem.Delta(4, 100), ChartWaterfallItem.Subtotal(1), ChartWaterfallItem.Delta(3, -140),
        ChartWaterfallItem.Subtotal(2), ChartWaterfallItem.Subtotal(5), ChartWaterfallItem.Delta(6, 25),
        ChartWaterfallItem.Total(7), ChartWaterfallItem.Delta(8, 10)
    });

    private static VisualScene Scene(Chart chart) {
        var context = new VisualRenderContext();
        var builder = new VisualSceneBuilder(new VisualSize(800, 400), context.Font);
        VisualCartesianCompiler.Build(chart, context, builder, new ChartRect(0, 0, 800, 400));
        return builder.Build();
    }
}
