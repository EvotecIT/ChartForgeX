using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class BoxPlotLayoutTests {
    [Fact]
    public void OrientationAndLocalizedFiveNumberReadoutsAreCapturedWithPreparedGeometry() {
        var chart = Fixture();
        Assert.Equal(ChartOrientation.Vertical, chart.Options.BoxPlot.Orientation);
        Assert.Same(chart, chart.ConfigureBoxPlot(options => options.Orientation = ChartOrientation.Horizontal));
        Assert.Throws<ArgumentNullException>(() => chart.ConfigureBoxPlot(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.Options.BoxPlot.Orientation = (ChartOrientation)99);
        chart.ConfigureLabels(labels => { labels.Category = "Kategoria"; labels.Minimum = "Minimum lokalne";
            labels.Q1 = "Dolny kwartyl"; labels.Median = "Mediana"; labels.Q3 = "Górny kwartyl"; labels.Maximum = "Maksimum"; });
        var copied = chart.Options.Labels.Capture();
        var prepared = chart.Prepare(new VisualRenderContext()); var svg = prepared.ToSvg(); var png = prepared.ToPng();
        chart.Options.BoxPlot.Orientation = ChartOrientation.Vertical;
        chart.ConfigureLabels(labels => { labels.Category = "Changed"; labels.Minimum = "Changed"; labels.Q1 = "Changed";
            labels.Median = "Changed"; labels.Q3 = "Changed"; labels.Maximum = "Changed"; });
        chart.ClearSeries();
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        var expected = new[] { "Kategoria", "Minimum lokalne", "Dolny kwartyl", "Mediana", "Górny kwartyl", "Maksimum" };
        Assert.Equal(expected, new[] { copied.Category, copied.Minimum, copied.Q1, copied.Median, copied.Q3, copied.Maximum });
        Assert.All(prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "point"), group =>
            Assert.Equal(expected, new[] { "category", "minimum", "q1", "median", "q3", "maximum" }.Select(key => group.Metadata["data-cfx-label-" + key])));
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    public void GroupedSummariesUsePhysicalAxesAndStableSlotsWhileRetainingAllFiveFacts(bool horizontal, bool reverseValue, bool reverseCategory) {
        var chart = Fixture(); chart.Series[1].WithStrokeWidth(8);
        chart.ConfigureBoxPlot(options => options.Orientation = horizontal ? ChartOrientation.Horizontal : ChartOrientation.Vertical);
        (horizontal ? chart.Options.XAxis : chart.Options.YAxis).WithBounds(-20, 20).WithReversal(reverseValue);
        (horizontal ? chart.Options.YAxis : chart.Options.XAxis).WithBounds(0, 4).WithReversal(reverseCategory);
        chart.WithStackedBars(); // A scalar bar mode does not stack summary statistics.
        var scene = Scene(chart);
        var bodies = scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "boxplot-body").ToArray();
        var medians = scene.Nodes.OfType<VisualSceneLine>().Where(node => node.Role == "boxplot-median").ToArray();
        var groups = scene.Nodes.OfType<VisualSceneGroup>().Where(node => node.Role == "point").ToArray();
        var categories = new[] { 3d, 1, 2, 3, 1 };
        var values = new[] { new[] { -8d, -4, 0, 4, 12 }, new[] { -12d, -8, -2, 6, 10 }, new[] { -3d, 1, 4, 9, 14 },
            new[] { -5d, 2, 5, 10, 16 }, new[] { -10d, -6, 2, 8, 12 } };
        double Category(double value) => horizontal ? (reverseCategory ? 4 - value : value) * 100 : (reverseCategory ? 4 - value : value) * 200;
        double Value(double value) => horizontal ? (reverseValue ? 20 - value : value + 20) * 20 : (reverseValue ? value + 20 : 20 - value) * 10;
        double Center(ChartRect rect) => horizontal ? rect.Top + rect.Height / 2 : rect.Left + rect.Width / 2;
        Assert.Equal(5, bodies.Length);
        for (var item = 0; item < bodies.Length; item++) {
            Assert.Equal(Math.Min(Value(values[item][1]), Value(values[item][3])), horizontal ? bodies[item].Bounds.Left : bodies[item].Bounds.Top, 8);
            Assert.Equal(Math.Abs(Value(values[item][3]) - Value(values[item][1])), horizontal ? bodies[item].Bounds.Width : bodies[item].Bounds.Height, 8);
            Assert.Equal(Value(values[item][2]), horizontal ? medians[item].Start.X : medians[item].Start.Y, 8);
            Assert.Equal(horizontal ? medians[item].Start.X : medians[item].Start.Y, horizontal ? medians[item].End.X : medians[item].End.Y, 8);
            Assert.Equal("1", groups[item].Metadata["data-cfx-source-count"]);
            Assert.Equal((item < 3 ? item : item - 3).ToString(CultureInfo.InvariantCulture), groups[item].Metadata["data-cfx-source-point"]);
            Assert.Equal(categories[item].ToString(CultureInfo.InvariantCulture), groups[item].Metadata["data-cfx-x"]);
            var keys = new[] { "minimum", "q1", "median", "q3", "maximum" };
            for (var member = 0; member < 5; member++) {
                Assert.Equal(values[item][member].ToString(CultureInfo.InvariantCulture), groups[item].Metadata["data-cfx-" + keys[member]]);
            }
            Assert.False(groups[item].Metadata.ContainsKey("data-cfx-base"));
        }
        Assert.Equal(Category(3), (Center(bodies[0].Bounds) + Center(bodies[3].Bounds)) / 2, 8);
        Assert.Equal(Category(1), (Center(bodies[1].Bounds) + Center(bodies[4].Bounds)) / 2, 8);
        Assert.Equal(Center(bodies[0].Bounds) - Category(3), Center(bodies[2].Bounds) - Category(2), 8);
        double End(VisualSceneLine line) => (horizontal ? Math.Max(line.Start.Y, line.End.Y) : Math.Max(line.Start.X, line.End.X)) + line.StrokeWidth / 2;
        double Start(VisualSceneLine line) => (horizontal ? Math.Min(line.Start.Y, line.End.Y) : Math.Min(line.Start.X, line.End.X)) - line.StrokeWidth / 2;
        Assert.True(End(medians[0]) < Start(medians[3])); Assert.True(End(medians[1]) < Start(medians[4]));
        var caps = scene.Nodes.OfType<VisualSceneLine>().Where(line => line.Role == "boxplot-cap").ToArray();
        Assert.Equal(10, caps.Length);
        for (var item = 0; item < bodies.Length; item++) {
            Assert.Equal(Value(values[item][0]), horizontal ? caps[item * 2].Start.X : caps[item * 2].Start.Y, 8);
            Assert.Equal(Value(values[item][4]), horizontal ? caps[item * 2 + 1].Start.X : caps[item * 2 + 1].Start.Y, 8);
        }
        Assert.Throws<InvalidOperationException>(() => chart.Series[0].WithStackGroup("summaries"));
        Assert.Throws<InvalidOperationException>(() => chart.Series[0].WithNormalization());
    }

    [Fact]
    public void RawQuartilesInterpolateAtNMinusOneAndRetainDerivedSampleIdentity() {
        var samples = new[] { 30d, 0, 20, 10 };
        var chart = Chart.Create().WithAxes(false).WithLegend(false).AddBoxPlot("Raw", 1, samples);
        var summary = chart.Series[0].BoxPlots[0];
        Assert.Equal(new[] { 0d, 7.5, 15, 22.5, 30 }, new[] { summary.Minimum, summary.Q1, summary.Median, summary.Q3, summary.Maximum });
        var prepared = chart.Prepare(new VisualRenderContext()); var svg = prepared.ToSvg();
        var group = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), node => node.Role == "point");
        Assert.Equal("five-number-summary", group.Metadata["data-cfx-derived"]);
        Assert.Equal("4", group.Metadata["data-cfx-source-count"]);
        Assert.False(group.Metadata.ContainsKey("data-cfx-source-point"));
        samples[0] = 999;
        Assert.Equal(new[] { 30d, 0, 20, 10 }, chart.Series[0].BoxPlotSourceSamples);
        chart.Series[0].UpdateBoxPlot(0, new ChartBoxPlot(1, 0, 7.5, 16, 22.5, 30));
        Assert.Empty(chart.Series[0].BoxPlotSourceSamples); Assert.Equal(svg, prepared.ToSvg());
        var singleton = Chart.Create().AddBoxPlot("One", 1, new[] { 4d });
        var one = Assert.Single(singleton.Series[0].BoxPlots);
        Assert.All(new[] { one.Minimum, one.Q1, one.Median, one.Q3, one.Maximum }, value => Assert.Equal(4, value));
        singleton.ConfigureBoxPlot(options => options.Orientation = ChartOrientation.Horizontal);
        Assert.NotEmpty(singleton.Prepare(new VisualRenderContext()).Regions);
    }

    [Fact]
    public void HorizontalDomainsUseAllStatisticsAndLogValuesWithoutAnInventedBaseline() {
        var chart = Chart.Create().ConfigureBoxPlot(options => options.Orientation = ChartOrientation.Horizontal)
            .AddBoxPlot("Summary", new[] { new ChartBoxPlot(2, 10, 20, 30, 50, 100), new ChartBoxPlot(5, 20, 25, 30, 35, 40) });
        var range = ChartRange.FromChart(chart);
        Assert.Equal(10, range.MinX); Assert.Equal(100, range.MaxX); Assert.Equal(.5, range.MinY); Assert.Equal(6.5, range.MaxY);
        chart.Options.XAxis.WithScale(ChartScaleKind.Logarithmic);
        Assert.NotEmpty(chart.Prepare(new VisualRenderContext()).Regions);
        chart.Series[0].UpdateBoxPlot(0, new ChartBoxPlot(2, 0, 20, 30, 50, 100));
        Assert.Throws<InvalidOperationException>(() => chart.Prepare(new VisualRenderContext()));
    }

    [Fact]
    public void VerticalAndHorizontalSecondaryProjectionsKeepMixedOrientationConstraints() {
        var chart = Chart.Create().WithAxes(false).WithLegend(false)
            .AddBoxPlot("Primary", new[] { new ChartBoxPlot(1, 1, 2, 5, 8, 9) })
            .AddBoxPlot("Secondary", new[] { new ChartBoxPlot(1, 100, 200, 300, 400, 500) });
        chart.Series[1].UseSecondaryValueAxis();
        chart.Options.YAxis.WithBounds(0, 10); chart.Options.SecondaryYAxis.WithBounds(100, 500);
        var bodies = Scene(chart).Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "boxplot-body").ToArray();
        Assert.Equal(80, bodies[0].Bounds.Top, 8); Assert.Equal(240, bodies[0].Bounds.Height, 8);
        Assert.Equal(100, bodies[1].Bounds.Top, 8); Assert.Equal(200, bodies[1].Bounds.Height, 8);
        Assert.True(bodies[0].Bounds.Right < bodies[1].Bounds.Left);
        chart.ConfigureBoxPlot(options => options.Orientation = ChartOrientation.Horizontal);
        var horizontal = chart.Prepare(new VisualRenderContext());
        Assert.NotNull(horizontal.SecondaryXProjection); Assert.NotEmpty(horizontal.ToPng());
        chart.Series[1].ValueAxis = ChartAxisSide.Primary; chart.AddRangeBar("Interval", new[] { new ChartInterval(1, 2, 8) });
        Assert.Contains("horizontal orientation", Assert.Throws<NotSupportedException>(() => chart.Prepare(new VisualRenderContext())).Message);
        chart.ConfigureBoxPlot(options => options.Orientation = ChartOrientation.Vertical);
        chart.AddLine("Target", new[] { new ChartPoint(1, 5), new ChartPoint(2, 6) });
        Assert.NotEmpty(chart.Prepare(new VisualRenderContext()).Regions);
    }

    [Fact]
    public void HorizontalCategoryCaptionsAgreeAcrossAxisPointLegendAndSummaryReadoutWithoutRepeatedFormatting() {
        var chart = Chart.Create().WithPointLegend().WithXLabels("Fallback", "Other")
            .ConfigureBoxPlot(options => options.Orientation = ChartOrientation.Horizontal)
            .AddBoxPlot("Summary", new[] { new ChartBoxPlot(1, 1, 2, 3, 4, 5), new ChartBoxPlot(2, 2, 3, 4, 5, 6) });
        chart.Options.YAxis.Labels.Add(new ChartAxisLabel(1, "First")); var calls = 0;
        chart.Options.YAxis.WithLabelFormatter(value => { calls++; return "Category " + value; });
        chart.Series[0].WithPointLabel(1, "Custom median").WithPointColor(1, "#123456").WithPointFillPattern(1, ChartFillPattern.DiagonalForward);
        var request = VisualExportRequest.ForChart(chart); Assert.Equal(0, calls);
        var prepared = chart.Prepare(request.Context); Assert.Equal(1, calls);
        var points = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(node => node.Role == "point").ToArray();
        Assert.Equal(new[] { "First", "Category 2" }, points.Select(node => node.Metadata["data-cfx-category-label"]));
        Assert.Equal("Custom median", points[1].Metadata["data-cfx-label"]);
        Assert.Equal(new[] { "First", "Category 2" }, prepared.Scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == "legend-label")
            .Select(text => string.Concat(text.Text.Lines.Select(line => line.Text))));
        Assert.Contains(prepared.Regions, region => region.Role == "axis-y-label" && region.Label!.StartsWith("Category 2", StringComparison.Ordinal));
        Assert.Contains(prepared.Scene.Nodes, node => node.Role == "boxplot-pattern");
        chart.Options.YAxis.Labels.Clear(); chart.Options.YAxis.WithLabelFormatter(null);
        Assert.Contains("data-cfx-category-label=\"Fallback\"", chart.Prepare(new VisualRenderContext()).ToSvg());
    }

    [Theory]
    [InlineData("horizontal", VisualThemeMode.Light)]
    [InlineData("compact-horizontal", VisualThemeMode.Dark)]
    public void HorizontalMedianLabelsHaveClearLanesBesideGroupedQuartileBodies(string variant, VisualThemeMode mode) {
        var chart = V2GalleryModels.Create(ChartSeriesKind.BoxPlot, variant, mode);
        chart.WithSize(variant.StartsWith("compact", StringComparison.Ordinal) ? 360 : 800, 440)
            .WithTitle("Delivery time by service").WithSubtitle("Expected and observed five-number summaries")
            .WithFontFamily("Carlito").WithPngFont(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf"));
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var labels = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "data-label").ToArray();
        // Wide layouts keep every median caption; the 360px compact layout may move one that has no clear lane to the tooltip.
        Assert.InRange(labels.Length, variant.StartsWith("compact", StringComparison.Ordinal) ? 7 : 8, 8);
        Assert.Subset(new HashSet<string>(new[] { "15", "22", "25", "28", "30", "32", "35", "40" }), new HashSet<string>(labels.Select(node => string.Concat(node.Text.Lines.Select(line => line.Text)))));
    }

    private static Chart Fixture() => Chart.Create().WithAxes(false).WithLegend(false)
        .AddBoxPlot("Expected", new[] { new ChartBoxPlot(3, -8, -4, 0, 4, 12), new ChartBoxPlot(1, -12, -8, -2, 6, 10), new ChartBoxPlot(2, -3, 1, 4, 9, 14) })
        .AddBoxPlot("Observed", new[] { new ChartBoxPlot(3, -5, 2, 5, 10, 16), new ChartBoxPlot(1, -10, -6, 2, 8, 12) });

    private static VisualScene Scene(Chart chart) {
        var context = new VisualRenderContext(); var builder = new VisualSceneBuilder(new VisualSize(800, 400), context.Font);
        VisualCartesianCompiler.Build(chart, context, builder, new ChartRect(0, 0, 800, 400)); return builder.Build();
    }
}
