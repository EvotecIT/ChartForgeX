using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Outside mark captions and physical axis captions retain separate measured lanes.</summary>
public sealed class CartesianLabelLaneTests {
    [Theory]
    [InlineData(false, false, 1)]
    [InlineData(false, true, 1)]
    [InlineData(false, false, -1)]
    [InlineData(false, true, -1)]
    [InlineData(true, false, 1)]
    [InlineData(true, true, 1)]
    [InlineData(true, false, -1)]
    [InlineData(true, true, -1)]
    public void VisibleAxesStayOutsideSignedAndReversedStackTotalLanes(bool horizontal, bool reversed, int sign) {
        var chart = Frame().WithStackedBars().WithStackTotals().WithDataLabels(false);
        var first = Enumerable.Range(1, 4).Select(x => new ChartPoint(x, 10 * sign));
        var second = Enumerable.Range(1, 4).Select(x => new ChartPoint(x, 20 * sign));
        if (horizontal) chart.AddHorizontalBar("Completed", first).AddHorizontalBar("Remaining", second);
        else chart.AddBar("Completed", first).AddBar("Remaining", second);
        (horizontal ? chart.Options.XAxis : chart.Options.YAxis)
            .WithBounds(sign > 0 ? 0 : -30, sign > 0 ? 30 : 0).WithReversal(reversed);
        var prepared = Prepare(chart);
        AssertLanes(prepared, "stack-total-label", 4);
        var expected = (30 * sign).ToString(CultureInfo.InvariantCulture);
        Assert.All(Totals(prepared), total => Assert.Equal(expected, Text(total)));
        var facts = prepared.Regions.Where(region => region.Role == "stack-total").ToArray();
        Assert.Equal(4, facts.Length);
        foreach (var fact in facts) {
            var source = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.Id == fact.Id);
            Assert.Equal(expected, source.Metadata["data-cfx-source-total"]);
            Assert.Equal(expected, source.Metadata["data-cfx-y"]);
        }
        Assert.Equal(8, chart.Series.Sum(series => series.SourcePointCount));
    }

    [Theory]
    [InlineData(360)]
    [InlineData(640)]
    public void PublishedNormalizedGroupedRecipeKeepsAllTotalsAndCategoryNames(int width) {
        var chart = BarRecipe.Create("compact-options").WithSize(width, 360)
            .WithTitle("Completed orders").WithSubtitle("Normalized grouped stacks")
            .WithPngFont(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf"));
        var prepared = Prepare(chart);
        AssertLanes(prepared, "stack-total-label", 8);
        Assert.All(Totals(prepared), total => Assert.Equal("100", Text(total)));
        Assert.Equal(new[] { "North", "South", "East", "West" },
            prepared.Scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == "axis-x-label").Select(Text));
        var sourceTotals = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "stack-total").ToArray();
        Assert.Equal(8, sourceTotals.Length);
        Assert.All(sourceTotals, group => Assert.Equal("100", group.Metadata["data-cfx-y"]));
        Assert.Equal(new[] { "first-half", "second-half" }, sourceTotals.Select(group => group.Metadata["data-cfx-stack-group"]).Distinct().OrderBy(value => value));
        Assert.Equal(16, chart.Series.Sum(series => series.SourcePointCount));
        Assert.Equal(8, XDocument.Parse(prepared.ToSvg()).Descendants().Count(node => (string?)node.Attribute("data-cfx-role") == "stack-total-label"));
    }

    [Fact]
    public void GroupedCategoriesSecondaryAxisAndAnnotationsKeepIndependentCaptionLanes() {
        var chart = Frame().WithSize(640, 380).WithStackedBars().WithStackTotals().WithDataLabels(false)
            .WithSecondaryYAxis("Secondary")
            .WithXLabels(Enumerable.Range(1, 4).Select(x => new ChartAxisLabel(x, "Region " + x, x <= 2 ? "First half" : "Second half")));
        for (var index = 0; index < 4; index++) {
            var series = ChartSeries.CreateBar(new ChartSeriesId("measure-" + index), "Measure " + index,
                Enumerable.Range(1, 4).Select(x => new ChartPoint(x, index % 2 == 0 ? 10 : 20)));
            if (index >= 2) series.UseSecondaryValueAxis();
            chart.AddSeries(series);
        }
        chart.Options.YAxis.WithBounds(0, 30);
        chart.Options.SecondaryYAxis.WithBounds(0, 30).WithReversal();
        chart.AddHorizontalLine(15, "Capacity").AddHorizontalBand(5, 8, "Review window");
        var prepared = Prepare(chart);
        AssertLanes(prepared, "stack-total-label", 8);
        Assert.Equal(2, prepared.Scene.Nodes.OfType<VisualSceneText>().Count(text => text.Role == "axis-x-category-group-label"));
        Assert.Equal(2, prepared.Regions.Count(region => region.Role == "axis-x-category-group"));
        Assert.Contains(prepared.Scene.Nodes.OfType<VisualSceneText>(), text => Text(text) == "Capacity");
        Assert.Contains(prepared.Scene.Nodes.OfType<VisualSceneText>(), text => Text(text) == "Review window");
        Assert.Equal(4, chart.Series.Count);
    }

    [Theory]
    [InlineData(false, -1)]
    [InlineData(true, 1)]
    public void OrdinaryLowerBarCaptionsDoNotUseTheCategoryCaptionStrip(bool reversed, int sign) {
        var chart = Frame().WithDataLabels().WithDataLabelPlacement(ChartDataLabelPlacement.Below)
            .AddBar("Completed", Enumerable.Range(1, 4).Select(x => new ChartPoint(x, 30 * sign)));
        chart.Options.YAxis.WithBounds(sign > 0 ? 0 : -30, sign > 0 ? 30 : 0).WithReversal(reversed);
        var prepared = Prepare(chart);
        AssertLanes(prepared, "data-label", 4);
        var plot = Assert.Single(prepared.Regions, region => region.Role == "cartesian-plot").Bounds;
        Assert.All(prepared.Scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == "data-label"), text => {
            Assert.Equal((30 * sign).ToString(CultureInfo.InvariantCulture), Text(text));
            Assert.True(Bounds(text).Top >= plot.Bottom + 2, "An explicit lower caption must remain outside its bar.");
        });
    }

    private static Chart Frame() => Chart.Create().WithSize(360, 360).WithLegend(false).WithHeader(false)
        .WithXAxis("Region").WithYAxis("Orders").WithXLabels("North", "South", "East", "West")
        .WithPngFont(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf"));

    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    private static VisualSceneText[] Totals(PreparedVisual prepared) => prepared.Scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == "stack-total-label").ToArray();
    private static string Text(VisualSceneText text) => string.Join("\n", text.Text.Lines.Select(line => line.Text));

    private static void AssertLanes(PreparedVisual prepared, string role, int count) {
        var labels = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == role).ToArray();
        Assert.Equal(count, labels.Length);
        var axes = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role?.StartsWith("axis-", StringComparison.Ordinal) == true).ToArray();
        Assert.NotEmpty(axes);
        foreach (var label in labels) {
            var bounds = Bounds(label);
            Assert.True(LabelPlacementService.Contains(new ChartRect(0, 0, prepared.Size.Width, prepared.Size.Height), bounds));
            foreach (var axis in axes) Assert.False(Overlaps(bounds, Bounds(axis)), Text(label) + " overlaps " + Text(axis));
            foreach (var other in labels.Where(other => !ReferenceEquals(other, label))) Assert.False(Overlaps(bounds, Bounds(other)));
        }
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "cartesian.data-label-overflow");
    }

    private static ChartRect Bounds(VisualSceneText text) {
        var left = text.Text.Lines.Min(text.LineLeft);
        var right = text.Text.Lines.Max(line => text.LineLeft(line) + line.Width);
        return new ChartRect(left, text.Baseline - text.Text.Ascent, right - left, text.Text.Metrics.Height);
    }

    private static bool Overlaps(ChartRect first, ChartRect second) => first.Left < second.Right - .001 && first.Right > second.Left + .001
        && first.Top < second.Bottom - .001 && first.Bottom > second.Top + .001;
}
