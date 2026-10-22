using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class RangeBarLayoutTests {
    [Fact]
    public void OrientationDefaultsToVerticalAndPreparedOutputRetainsItsGeometryAndLabels() {
        var chart = Fixture();
        Assert.Equal(ChartOrientation.Vertical, chart.Options.RangeBar.Orientation);
        Assert.Same(chart, chart.ConfigureRangeBar(options => options.Orientation = ChartOrientation.Horizontal));
        Assert.Throws<ArgumentNullException>(() => chart.ConfigureRangeBar(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.Options.RangeBar.Orientation = (ChartOrientation)99);
        chart.ConfigureLabels(labels => { labels.Category = "Kategoria"; labels.Start = "Początek"; labels.End = "Koniec"; });
        var prepared = chart.Prepare(new VisualRenderContext());
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        chart.Options.RangeBar.Orientation = ChartOrientation.Vertical;
        chart.ConfigureLabels(labels => { labels.Category = "Changed category"; labels.Start = "Changed start"; labels.End = "Changed end"; });
        chart.ClearSeries();
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        Assert.All(prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "point"), group => {
            Assert.Equal("Kategoria", group.Metadata["data-cfx-label-category"]);
            Assert.Equal("Początek", group.Metadata["data-cfx-label-start"]);
            Assert.Equal("Koniec", group.Metadata["data-cfx-label-end"]);
        });
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    public void GroupedIntervalsUsePhysicalAxesAndStableSlotsWithoutChangingSourcePairs(bool horizontal, bool reverseValue, bool reverseCategory) {
        var chart = Fixture();
        chart.ConfigureRangeBar(options => options.Orientation = horizontal ? ChartOrientation.Horizontal : ChartOrientation.Vertical);
        var valueAxis = horizontal ? chart.Options.XAxis : chart.Options.YAxis;
        var categoryAxis = horizontal ? chart.Options.YAxis : chart.Options.XAxis;
        valueAxis.WithBounds(-20, 20).WithReversal(reverseValue);
        categoryAxis.WithBounds(0, 4).WithReversal(reverseCategory);
        chart.WithStackedBars(); // Scalar bar mode must never stack interval endpoints.
        var scene = Scene(chart);
        var marks = scene.Nodes.OfType<VisualSceneRectangle>().Where(mark => mark.Role == "range-bar").ToArray();
        var groups = scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "point").ToArray();
        Assert.Equal(5, marks.Length);
        var coordinates = new[] { 3d, 1, 2, 3, 1 };
        var starts = new[] { 8d, -12, -3, 4, 10 };
        var ends = new[] { -5d, 6, 9, 12, -8 };
        double Category(double value) => horizontal ? (reverseCategory ? value : 4 - value) * 100 : (reverseCategory ? 4 - value : value) * 200;
        double Value(double value) => horizontal ? (reverseValue ? 20 - value : value + 20) * 20 : (reverseValue ? value + 20 : 20 - value) * 10;
        double Center(ChartRect bounds) => horizontal ? bounds.Top + bounds.Height / 2 : bounds.Left + bounds.Width / 2;
        for (var item = 0; item < marks.Length; item++) {
            var bounds = marks[item].Bounds;
            Assert.Equal(Math.Min(Value(starts[item]), Value(ends[item])), horizontal ? bounds.Left : bounds.Top, 8);
            Assert.Equal(Math.Abs(Value(starts[item]) - Value(ends[item])), horizontal ? bounds.Width : bounds.Height, 8);
            Assert.Equal(starts[item].ToString(CultureInfo.InvariantCulture), groups[item].Metadata["data-cfx-start"]);
            Assert.Equal(ends[item].ToString(CultureInfo.InvariantCulture), groups[item].Metadata["data-cfx-end"]);
            Assert.Equal(coordinates[item].ToString(CultureInfo.InvariantCulture), groups[item].Metadata["data-cfx-x"]);
            Assert.Equal("1", groups[item].Metadata["data-cfx-source-count"]);
            Assert.Equal((item < 3 ? item : item - 3).ToString(CultureInfo.InvariantCulture), groups[item].Metadata["data-cfx-source-point"]);
            Assert.False(groups[item].Metadata.ContainsKey("data-cfx-base"));
        }
        Assert.Equal(Category(3), (Center(marks[0].Bounds) + Center(marks[3].Bounds)) / 2, 8);
        Assert.Equal(Category(1), (Center(marks[1].Bounds) + Center(marks[4].Bounds)) / 2, 8);
        Assert.Equal(Center(marks[0].Bounds) - Category(3), Center(marks[2].Bounds) - Category(2), 8);
        Assert.True(Center(marks[0].Bounds) < Center(marks[3].Bounds));
        var caps = scene.Nodes.OfType<VisualSceneLine>().Where(line => line.Role == "range-bar-cap").ToArray();
        double CrossStart(VisualSceneLine line) => (horizontal ? Math.Min(line.Start.Y, line.End.Y) : Math.Min(line.Start.X, line.End.X)) - line.StrokeWidth / 2;
        double CrossEnd(VisualSceneLine line) => (horizontal ? Math.Max(line.Start.Y, line.End.Y) : Math.Max(line.Start.X, line.End.X)) + line.StrokeWidth / 2;
        Assert.True(CrossEnd(caps[0]) < CrossStart(caps[6]));
        Assert.True(CrossEnd(caps[2]) < CrossStart(caps[8]));
        Assert.All(caps, cap => Assert.Equal(horizontal ? cap.Start.X : cap.Start.Y, horizontal ? cap.End.X : cap.End.Y, 8));
    }

    [Fact]
    public void HorizontalDomainsIncludeBothEndpointsAndRespectLogarithmicValuesWithoutAnInventedBaseline() {
        var chart = Chart.Create().ConfigureRangeBar(options => options.Orientation = ChartOrientation.Horizontal)
            .AddRangeBar("Range", new[] { new ChartInterval(2, 100, 10), new ChartInterval(5, 20, 40) });
        var range = ChartRange.FromChart(chart);
        Assert.Equal(10, range.MinX); Assert.Equal(100, range.MaxX);
        Assert.Equal(.5, range.MinY); Assert.Equal(6.5, range.MaxY);
        chart.Options.XAxis.WithScale(ChartScaleKind.Logarithmic);
        range = ChartRange.FromChart(chart);
        Assert.Equal(10, range.MinX); Assert.Equal(100, range.MaxX);
        Assert.NotEmpty(chart.Prepare(new VisualRenderContext()).Regions);
        chart.Series[0].UpdateInterval(0, new ChartInterval(2, 0, chart.Series[0].Intervals[0].End));
        Assert.Throws<InvalidOperationException>(() => chart.Prepare(new VisualRenderContext()));
    }

    [Fact]
    public void HorizontalRangeBarsSupportSecondaryValueAndScalarMixtures() {
        var chart = Fixture(); chart.Series[1].ValueAxis = ChartAxisSide.Secondary;
        Assert.NotEmpty(chart.Prepare(new VisualRenderContext()).Regions);
        chart.ConfigureRangeBar(options => options.Orientation = ChartOrientation.Horizontal);
        var secondary = chart.Prepare(new VisualRenderContext());
        Assert.NotNull(secondary.SecondaryXProjection); Assert.NotEmpty(secondary.ToPng());
        chart.Series[1].ValueAxis = ChartAxisSide.Primary;
        chart.AddHorizontalBar("Scalar", new[] { new ChartPoint(1, 3) });
        Assert.Contains(chart.Prepare(new VisualRenderContext()).Scene.Nodes.OfType<VisualSceneRectangle>(), node => node.Role == "horizontal-bar");
        chart.RemoveSeries(chart.Series[2]);
        chart.ConfigureRangeBar(options => options.Orientation = ChartOrientation.Vertical);
        chart.AddLine("Target", new[] { new ChartPoint(1, 5), new ChartPoint(3, 6) });
        Assert.NotEmpty(chart.Prepare(new VisualRenderContext()).Regions);
    }

    [Fact]
    public void VerticalGroupedIntervalsKeepIndependentPrimaryAndSecondaryValueProjection() {
        var chart = Chart.Create().WithAxes(false).WithLegend(false).WithBarStyle(ChartBarStyle.Flat)
            .AddRangeBar("Primary", new[] { new ChartInterval(1, 2, 8) })
            .AddRangeBar("Secondary", new[] { new ChartInterval(1, 300, 150) });
        chart.Series[1].UseSecondaryValueAxis();
        chart.Options.YAxis.WithBounds(0, 10); chart.Options.SecondaryYAxis.WithBounds(100, 500);
        var bodies = Scene(chart).Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "range-bar").ToArray();
        Assert.Equal(80, bodies[0].Bounds.Top, 8); Assert.Equal(240, bodies[0].Bounds.Height, 8);
        Assert.Equal(200, bodies[1].Bounds.Top, 8); Assert.Equal(150, bodies[1].Bounds.Height, 8);
        Assert.True(bodies[0].Bounds.Right < bodies[1].Bounds.Left);
    }

    [Fact]
    public void HorizontalCategoryCaptionsAndLocalizedReadoutNamesAgreeAcrossAxesMarksAndPointLegends() {
        var chart = Chart.Create().WithPointLegend().WithXLabels("Fallback", "Other")
            .ConfigureRangeBar(options => options.Orientation = ChartOrientation.Horizontal)
            .AddRangeBar("Range", new[] { new ChartInterval(1, 2, 8), new ChartInterval(2, 10, 4) });
        chart.Options.YAxis.Labels.Add(new ChartAxisLabel(1, "First"));
        var calls = 0;
        chart.Options.YAxis.WithLabelFormatter(value => { calls++; return "Category " + value; });
        chart.ConfigureLabels(labels => { labels.Category = "Kategoria"; labels.Start = "Początek"; labels.End = "Koniec"; });
        var request = VisualExportRequest.ForChart(chart);
        Assert.Equal(0, calls);
        var prepared = chart.Prepare(request.Context);
        Assert.Equal(1, calls);
        var points = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "point").ToArray();
        Assert.Equal(new[] { "First", "Category 2" }, points.Select(group => group.Metadata["data-cfx-category-label"]));
        Assert.All(points, group => {
            Assert.Equal("Kategoria", group.Metadata["data-cfx-label-category"]);
            Assert.Equal("Początek", group.Metadata["data-cfx-label-start"]);
            Assert.Equal("Koniec", group.Metadata["data-cfx-label-end"]);
        });
        Assert.Equal(new[] { "First", "Category 2" }, prepared.Scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == "legend-label")
            .Select(text => string.Concat(text.Text.Lines.Select(line => line.Text))));
        Assert.Contains(prepared.Regions, region => region.Role == "axis-y-label" && region.Label!.StartsWith("Category 2", StringComparison.Ordinal));
        chart.Options.YAxis.Labels.Clear(); chart.Options.YAxis.WithLabelFormatter(null);
        var fallback = chart.Prepare(new VisualRenderContext()).ToSvg();
        Assert.Contains("data-cfx-category-label=\"Fallback\"", fallback);
    }

    private static Chart Fixture() => Chart.Create().WithAxes(false).WithLegend(false).WithBarStyle(ChartBarStyle.Flat)
        .AddRangeBar("Expected", new[] { new ChartInterval(3, 8, -5), new ChartInterval(1, -12, 6), new ChartInterval(2, -3, 9) })
        .AddRangeBar("Observed", new[] { new ChartInterval(3, 4, 12), new ChartInterval(1, 10, -8) });

    private static VisualScene Scene(Chart chart) {
        var context = new VisualRenderContext();
        var builder = new VisualSceneBuilder(new VisualSize(800, 400), context.Font);
        VisualCartesianCompiler.Build(chart, context, builder, new ChartRect(0, 0, 800, 400));
        return builder.Build();
    }
}
