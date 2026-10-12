using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class StyledGuideOwnerClosureTests {
    [Theory]
    [InlineData(1)]
    [InlineData(1024)]
    public void QuarterPixelDashesRenderAtTheMaximumAcceptedChartWidth(double strokeWidth) {
        var chart = Chart.Create().WithSize(1000000, 16).WithHeader(false).WithLegend(false)
            .AddLine("Values", new[] { new ChartPoint(0, 1), new ChartPoint(1, 2) }).AddHorizontalLine(1.5, "");
        chart.Options.ShowAxes = false;
        chart.Options.ShowGrid = false;
        chart.Annotations[0].StrokeStyle = new ChartStrokeStyle(width: strokeWidth, dash: new[] { .25, .25 });
        Assert.Contains("stroke-dasharray=\"0.25 0.25\"", chart.ToSvg());
        Assert.True(chart.ToPng().Length > 64);
    }

    [Fact]
    public void QuarterPixelDashesRenderOnAnAcceptedWideCanvas() {
        var chart = Chart.Create().WithSize(100000, 100).WithHeader(false).WithLegend(false)
            .AddLine("Values", new[] { new ChartPoint(0, 1), new ChartPoint(1, 2) }).AddHorizontalLine(1.5, "");
        chart.Annotations[0].StrokeStyle = new ChartStrokeStyle(width: 1, dash: new[] { .25, .25 });
        Assert.Contains("stroke-dasharray=\"0.25 0.25\"", chart.ToSvg());
        Assert.True(chart.ToPng().Length > 64);
    }

    [Fact]
    public void GuideThatBecomesOversizedAfterTotalGuttersKeepsTheUnexpandedProjection() {
        var chart = Chart.Create().WithSize(640, 360).WithHeader(false).WithLegend(false)
            .WithStackedBars().WithStackTotals().WithValueFormatter(value => "A complete stack total " + value)
            .AddHorizontalBar("First", new[] { new ChartPoint(1, 80), new ChartPoint(2, 60) })
            .AddHorizontalBar("Second", new[] { new ChartPoint(1, 20), new ChartPoint(2, 40) }).AddVerticalLine(0, "");
        chart.Annotations[0].StrokeStyle = new ChartStrokeStyle(width: 0);
        var context = VisualExportRequest.ForChart(chart).Context;
        var control = chart.Prepare(context);
        var axis = Assert.Single(control.Scene.Nodes.OfType<VisualSceneLine>(), node => node.Role == "axis-x");
        var guideWidth = axis.End.X - axis.Start.X + 20;
        chart.Annotations[0].StrokeStyle = new ChartStrokeStyle(width: guideWidth);
        var actual = chart.Prepare(context);
        var finalAxis = Assert.Single(actual.Scene.Nodes.OfType<VisualSceneLine>(), node => node.Role == "axis-x");
        Assert.True(guideWidth > finalAxis.End.X - finalAxis.Start.X, "The guide must be oversized in the final plot.");
        Assert.Equal(control.Regions.Where(region => region.Role == "point").Select(region => region.Bounds),
            actual.Regions.Where(region => region.Role == "point").Select(region => region.Bounds));
    }

    [Fact]
    public void GuideSkippedAfterVerticalTotalGuttersKeepsTheDataRangeTickSpacing() {
        var chart = Chart.Create().WithSize(640, 360).WithHeader(false).WithLegend(false)
            .WithStackedBars().WithStackTotals()
            .AddBar("First", new[] { new ChartPoint(1, 80), new ChartPoint(2, 60) })
            .AddBar("Second", new[] { new ChartPoint(1, 20), new ChartPoint(2, 40) }).AddHorizontalLine(0, "");
        chart.Options.YAxis.Labels.Add(new ChartAxisLabel(0, "Baseline"));
        chart.Annotations[0].StrokeStyle = new ChartStrokeStyle(width: 0);
        var context = VisualExportRequest.ForChart(chart).Context;
        var control = chart.Prepare(context);
        var axis = Assert.Single(control.Scene.Nodes.OfType<VisualSceneLine>(), node => node.Role == "axis-y");
        var guideWidth = axis.End.Y - axis.Start.Y + 2;
        chart.Annotations[0].StrokeStyle = new ChartStrokeStyle(width: guideWidth);
        var actual = chart.Prepare(context);
        var finalAxis = Assert.Single(actual.Scene.Nodes.OfType<VisualSceneLine>(), node => node.Role == "axis-y");
        Assert.True(guideWidth > finalAxis.End.Y - finalAxis.Start.Y, "The guide must be oversized in the final plot.");
        Assert.Equal(control.Regions.Where(region => region.Role == "axis-y-label").Select(region => region.Label),
            actual.Regions.Where(region => region.Role == "axis-y-label").Select(region => region.Label));
        Assert.Equal(control.Scene.Nodes.OfType<VisualSceneLine>().Where(node => node.Role == "grid-y").Select(node => node.Start.Y),
            actual.Scene.Nodes.OfType<VisualSceneLine>().Where(node => node.Role == "grid-y").Select(node => node.Start.Y));
    }
}
