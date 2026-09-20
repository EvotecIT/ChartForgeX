using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Svg;
using ChartForgeX.Raster;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class LegendDensityTests {
    [Theory]
    [InlineData("line", ChartLegendPosition.Bottom)]
    [InlineData("line", ChartLegendPosition.Top)]
    [InlineData("line", ChartLegendPosition.Right)]
    [InlineData("pie", ChartLegendPosition.Bottom)]
    [InlineData("pie", ChartLegendPosition.Left)]
    [InlineData("radial", ChartLegendPosition.Bottom)]
    [InlineData("radial", ChartLegendPosition.Right)]
    public void DenseLegendsDiscloseOverflowAndRetainAllData(string kind, ChartLegendPosition position) {
        var chart = Chart.Create().WithSize(900, 560).WithTitle("Dense legend").WithLegendPosition(position).WithLegendBudget(maximumRows: 3);
        var points = Enumerable.Range(0, 100).Select(i => new ChartPoint(i, i + 1)).ToArray();
        if (kind == "pie") chart.AddPie("Values", points);
        else if (kind == "radial") chart.AddRadialBar("Values", points);
        else foreach (var point in points) chart.AddLine("Service " + point.X, new[] { new ChartPoint(0, point.Y), new ChartPoint(1, point.Y + 1) });
        var svg = XDocument.Parse(new SvgChartRenderer().Render(chart));
        var summary = Assert.Single(svg.Descendants(), e => (string?)e.Attribute("data-cfx-role") == "legend-overflow");
        var omitted = (int)summary.Attribute("data-cfx-omitted")!;
        var role = kind == "pie" ? "slice-legend-swatch" : kind == "radial" ? "radial-bar-legend-marker" : "legend-item";
        var visible = svg.Descendants().Count(e => (string?)e.Attribute("data-cfx-role") == role);
        Assert.Equal(100, visible + omitted);
        Assert.True(visible > 0 && omitted > 0);
        Assert.Contains(omitted.ToString(), summary.Value);
        Assert.InRange((double)summary.Attribute("y")!, 1, 559);
        Assert.Equal(kind == "line" ? 100 : 1, chart.Series.Count);
        Assert.Equal(kind == "line" ? 200 : 100, chart.Series.Sum(series => series.Points.Count));
        Assert.NotEmpty(new PngChartRenderer().Render(chart));
    }

    [Theory]
    [InlineData("line", 560, 1.0)]
    [InlineData("pie", 560, 1.0)]
    [InlineData("radial", 560, 1.0)]
    [InlineData("line", 240, 0.35)]
    [InlineData("pie", 240, 0.35)]
    [InlineData("radial", 240, 0.35)]
    public void SideLegendBudgetUsesDrawableHeightAndKeepsOverflowVisible(string kind, int height, double maximumHeightFraction) {
        var chart = Chart.Create()
            .WithSize(900, height)
            .WithTitle("Bounded side legend")
            .WithLegendPosition(ChartLegendPosition.Right)
            .WithLegendBudget(maximumHeightFraction);
        var points = Enumerable.Range(0, 100).Select(i => new ChartPoint(i, i + 1)).ToArray();
        if (kind == "pie") chart.AddPie("Values", points);
        else if (kind == "radial") chart.AddRadialBar("Values", points);
        else foreach (var point in points) chart.AddLine("Service " + point.X, new[] { new ChartPoint(0, point.Y), new ChartPoint(1, point.Y + 1) });

        var svg = XDocument.Parse(new SvgChartRenderer().Render(chart));
        var summary = Assert.Single(svg.Descendants(), e => (string?)e.Attribute("data-cfx-role") == "legend-overflow");
        var omitted = (int)summary.Attribute("data-cfx-omitted")!;
        var role = kind == "pie" ? "slice-legend-swatch" : kind == "radial" ? "radial-bar-legend-marker" : "legend-item";
        var visible = svg.Descendants().Count(e => (string?)e.Attribute("data-cfx-role") == role);
        Assert.Equal(100, visible + omitted);
        Assert.True(omitted > 0);
        Assert.InRange((double)summary.Attribute("y")!, 1, height - 1);
        Assert.NotEmpty(new PngChartRenderer().Render(chart));
    }

    [Theory]
    [InlineData(ChartLegendPosition.TopLeft, 40, 180)]
    [InlineData(ChartLegendPosition.Top, 300, 600)]
    [InlineData(ChartLegendPosition.TopRight, 680, 860)]
    public void HorizontalOverflowSummaryHonorsLegendAlignment(ChartLegendPosition position, double minimumX, double maximumX) {
        var chart = Chart.Create().WithSize(900, 560).WithLegendPosition(position).WithLegendBudget(maximumRows: 2);
        for (var index = 0; index < 40; index++) {
            chart.AddLine("Service " + index, new[] { new ChartPoint(0, index), new ChartPoint(1, index + 1) });
        }

        var svg = XDocument.Parse(new SvgChartRenderer().Render(chart));
        var summary = Assert.Single(svg.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "legend-overflow");

        Assert.InRange((double)summary.Attribute("x")!, minimumX, maximumX);
        Assert.NotEmpty(new PngChartRenderer().Render(chart));
    }

    [Fact]
    public void OmittedWideSideLabelDoesNotShrinkThePlot() {
        static Chart Create(string finalLabel) {
            var chart = Chart.Create().WithSize(700, 400).WithLegendPosition(ChartLegendPosition.Right).WithLegendBudget(maximumRows: 3);
            for (var index = 0; index < 8; index++) {
                chart.AddLine(index == 7 ? finalLabel : "Service " + index, new[] { new ChartPoint(0, index), new ChartPoint(1, index + 1) });
            }
            return chart;
        }
        static double PlotWidth(Chart chart) {
            var svg = XDocument.Parse(new SvgChartRenderer().Render(chart));
            var clip = svg.Descendants().Single(element => element.Name.LocalName == "clipPath" && ((string?)element.Attribute("id"))?.EndsWith("-plotClip", StringComparison.Ordinal) == true);
            return (double)clip.Elements().Single(element => element.Name.LocalName == "rect").Attribute("width")!;
        }

        double ordinary = PlotWidth(Create("Ordinary omitted label"));
        double extreme = PlotWidth(Create(new string('W', 400)));

        Assert.Equal(ordinary, extreme, 6);
        Assert.NotEmpty(new PngChartRenderer().Render(Create(new string('W', 400))));
    }

    [Fact]
    public void SmallLegendsStayCompleteAndInvalidBudgetIsAtomic() {
        var chart = Chart.Create().WithSize(900, 560).AddLine("Value", new[] { new ChartPoint(0, 1), new ChartPoint(1, 2) });
        Assert.DoesNotContain("legend-overflow", new SvgChartRenderer().Render(chart));
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.WithLegendBudget(0.5, 0));
        Assert.Equal(0.35, chart.Options.LegendMaximumHeightFraction);
        Assert.Null(chart.Options.LegendMaximumRows);
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.WithLegendBudget(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.WithLegendBudget(1.1));
    }
}
