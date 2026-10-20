using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Automatic numeric captions cannot become plausible-looking fragments when a chart is narrow.</summary>
public sealed class CartesianNumericCaptionIntegrityTests {
    [Theory]
    [InlineData(ChartSeriesKind.Line)]
    [InlineData(ChartSeriesKind.Bar)]
    [InlineData(ChartSeriesKind.RangeBar)]
    [InlineData(ChartSeriesKind.DifferenceArea)]
    public void NonfittingGeneratedNumbersAreOmittedWholeAndRemainAvailableAsExactSourceFacts(ChartSeriesKind kind) {
        const string number = "123456789012345678901234567890.123456789";
        var chart = Chart.Create().WithSize(230, 150).WithHeader(false).WithLegend(false).WithAxes(false).WithGrid(false).WithDataLabels()
            .WithValueFormat(ChartValueFormat.Custom(_ => number));
        if (kind == ChartSeriesKind.RangeBar) chart.AddRangeBar("Range", new[] { new ChartInterval(1, 1, 2), new ChartInterval(2, 2, 3) });
        else if (kind == ChartSeriesKind.DifferenceArea) chart.AddDifferenceArea("Compared", new[] { new ChartDifferencePoint(1, 1, 2), new ChartDifferencePoint(2, 3, 2) });
        else chart.AddSeries(new ChartSeries(new ChartSeriesId("facts"), "Observed", kind, new[] { new ChartPoint(1, 1), new ChartPoint(2, 2) }));
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.DoesNotContain(prepared.Scene.Nodes, node => node.Role == "data-label");
        Assert.Contains(prepared.Diagnostics, diagnostic => diagnostic.Code == "cartesian.data-label-overflow");
        var points = XDocument.Parse(prepared.ToSvg()).Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "point").ToArray();
        Assert.Equal(2, points.Length);
        Assert.All(points, point => Assert.Contains(number, (string)point.Attribute("data-cfx-label")!, StringComparison.Ordinal));
        Assert.NotEmpty(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NonfittingStackTotalsKeepTheCompleteFactWithoutPaintingNumericFragments(bool horizontal) {
        const string number = "123456789012345678901234567890.123456789";
        var chart = Chart.Create().WithSize(230, 150).WithHeader(false).WithLegend(false).WithAxes(false).WithGrid(false)
            .WithStackedBars().WithStackTotals().WithDataLabels(false).WithValueFormat(ChartValueFormat.Custom(_ => number));
        var first = new[] { new ChartPoint(1, 1) }; var second = new[] { new ChartPoint(1, 2) };
        if (horizontal) chart.AddHorizontalBar("First", first).AddHorizontalBar("Second", second);
        else chart.AddBar("First", first).AddBar("Second", second);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.DoesNotContain(prepared.Scene.Nodes, node => node.Role == "stack-total-label");
        Assert.Contains(prepared.Diagnostics, diagnostic => diagnostic.Code == "cartesian.data-label-overflow");
        var total = Assert.Single(XDocument.Parse(prepared.ToSvg()).Descendants(), element => (string?)element.Attribute("data-cfx-role") == "stack-total");
        Assert.Equal(number, (string?)total.Attribute("data-cfx-label"));
        Assert.Equal("3", (string?)total.Attribute("data-cfx-source-total"));
        Assert.Equal(2, prepared.Regions.Count(region => region.Role == "point"));
        Assert.NotEmpty(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
    }
}
