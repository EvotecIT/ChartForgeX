using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class CartesianVisibleDomainTests {
    [Fact]
    public void FinancialHighLowAndCrossingXUncertaintyContributeTheirWholeValueIntervals() {
        var financial = Chart.Create().WithLegend(false).AddCandlestick("Price", new[] {
            new ChartCandlestick(0, 900, 1000, 800, 950), new ChartCandlestick(5, 25, 40, 20, 30), new ChartCandlestick(10, 900, 1000, 800, 950)
        });
        var crossing = Chart.Create().WithLegend(false).AddErrorBar("Uncertainty", new[] {
            new ChartErrorBar(0,30,new ChartErrorBounds(-10,10),new ChartErrorBounds(20,40)),
            new ChartErrorBar(100,900,yBounds:new ChartErrorBounds(800,1000))
        });
        foreach (var (chart, window) in new[] { (financial,new ChartAxisWindow(4.5,5.5)), (crossing,new ChartAxisWindow(4,6)) }) {
            if (chart.Series[0].Kind == ChartSeriesKind.ErrorBar) chart.Series[0].Markers.Enabled = false;
            var count = chart.Series[0].SourcePointCount;
            var prepared = Prepare(chart,window);
            // Whole intervals contribute, and the automatic domain then ends on clean ticks (design rule 1).
            Assert.InRange(prepared.YProjection!.Minimum, 10, 20); Assert.InRange(prepared.YProjection.Maximum, 40, 50);
            Assert.Equal(count, chart.Series[0].SourcePointCount);
            Assert.Equal(count, XDocument.Parse(prepared.ToSvg()).Descendants().Count(node => (string?)node.Attribute("data-cfx-role") == "point"));
            Assert.NotEmpty(prepared.ToPng());
        }
    }

    [Theory]
    [InlineData(ChartSeriesKind.RangeBand)]
    [InlineData(ChartSeriesKind.RangeArea)]
    [InlineData(ChartSeriesKind.DifferenceArea)]
    public void PairedCrossingsFitBothBoundariesEvenWhenNoAuthoredEndpointIsInside(ChartSeriesKind kind) {
        var chart = Chart.Create().WithLegend(false);
        if (kind == ChartSeriesKind.DifferenceArea) chart.AddDifferenceArea("Pair", new[] { new ChartDifferencePoint(0,30,10), new ChartDifferencePoint(10,130,110) });
        else if (kind == ChartSeriesKind.RangeArea) chart.AddRangeArea("Pair", new[] { new ChartRangeBand(0,10,30), new ChartRangeBand(10,110,130) });
        else chart.AddRangeBand("Pair", new[] { new ChartRangeBand(0,10,30), new ChartRangeBand(10,110,130) });
        var prepared = Prepare(chart,new ChartAxisWindow(4,6));
        Assert.InRange(prepared.YProjection!.Minimum,40,50); Assert.InRange(prepared.YProjection.Maximum,90,100);
        var facts = XDocument.Parse(prepared.ToSvg()).Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "point").ToArray();
        Assert.Equal(2,facts.Length); Assert.All(facts,node=>Assert.Equal("false",(string?)node.Attribute("data-cfx-visible")));
        Assert.Contains(prepared.Scene.Nodes.OfType<VisualScenePath>(),node=>node.Commands.Count>3);
        Assert.NotEmpty(prepared.ToPng());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FullDataStacksRemainAuthoritativeWhenOnlyOneCategoryIsVisible(bool horizontal) {
        var first = new[] { new ChartPoint(1,10),new ChartPoint(2,500) };
        var second = new[] { new ChartPoint(1,90),new ChartPoint(2,500) };
        var chart = Chart.Create().WithLegend(false).WithStackedBars();
        if (horizontal) chart.AddHorizontalBar("First",first).AddHorizontalBar("Second",second);
        else chart.AddBar("First",first).AddBar("Second",second);
        var request = horizontal ? new ChartCartesianViewport(y:new ChartAxisWindow(.9,1.1),valueDomain:ChartValueDomain.VisibleCategories)
            : new ChartCartesianViewport(x:new ChartAxisWindow(.9,1.1),valueDomain:ChartValueDomain.VisibleCategories);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context,request);
        var value = horizontal ? prepared.XProjection! : prepared.YProjection!;
        Assert.Equal(0,value.Minimum); Assert.InRange(value.Maximum,100,120);
        var facts = XDocument.Parse(prepared.ToSvg()).Descendants().Where(node=>(string?)node.Attribute("data-cfx-role")=="point").ToArray();
        Assert.Contains(facts,node=>(string?)node.Attribute("data-cfx-stack-end")=="100"&&(string?)node.Attribute("data-cfx-y")=="90");
        Assert.Equal(4,facts.Length); Assert.Equal(500,chart.Series[1].Points[1].Y);
        Assert.NotEmpty(prepared.ToPng());
    }

    [Fact]
    public void SmoothAndStepCrossingsFollowTheirPaintedInterpolationInsteadOfEndpointOnlySelection() {
        var smooth = Chart.Create().WithLegend(false).AddLine("Smooth",new[] {
            new ChartPoint(0,0),new ChartPoint(3,100),new ChartPoint(7,-100),new ChartPoint(10,0)
        });
        smooth.Series[0].WithSmooth();
        var prepared = Prepare(smooth,new ChartAxisWindow(4,6));
        Assert.InRange(prepared.YProjection!.Minimum,-95,-55); Assert.InRange(prepared.YProjection.Maximum,55,95);
        var step = Chart.Create().WithLegend(false).AddStepLine("Held",new[] { new ChartPoint(0,10),new ChartPoint(10,1000) });
        var end = Prepare(step,new ChartAxisWindow(2,4));
        Assert.InRange(end.YProjection!.Minimum,9,10); Assert.InRange(end.YProjection.Maximum,11,12);
        step.Series[0].WithInterpolation(ChartInterpolation.Step,ChartStepPosition.Start);
        var start = Prepare(step,new ChartAxisWindow(2,4));
        Assert.InRange(start.YProjection!.Minimum,999,1000); Assert.InRange(start.YProjection.Maximum,1000,1002);
    }

    [Fact]
    public void VisibleNegativeLogarithmicDomainRetainsItsSignBaseAndAuthoredBound() {
        var chart = Chart.Create().WithLegend(false).AddLine("Negative",new[] {
            new ChartPoint(0,-1024),new ChartPoint(5,-8),new ChartPoint(10,-2)
        });
        chart.ConfigureYAxis(axis=>axis.WithLogarithmicScale(2,ChartLogarithmicDomain.Negative));
        var prepared = Prepare(chart,new ChartAxisWindow(4,6));
        var y = prepared.YProjection!;
        Assert.True(y.Minimum < -8 && y.Minimum > -1024); Assert.True(y.Maximum > -8 && y.Maximum < 0);
        Assert.Equal(2,y.LogarithmBase); Assert.Equal(ChartLogarithmicDomain.Negative,y.LogarithmicDomain);
        Assert.Equal(-8,y.Unproject(y.Project(-8)),9);
        chart.ConfigureYAxis(axis=>axis.Minimum=-1024);
        var bounded = Prepare(chart,new ChartAxisWindow(4,6));
        Assert.Equal(-1024,bounded.YProjection!.Minimum); Assert.NotEmpty(bounded.ToPng());
    }

    private static PreparedVisual Prepare(Chart chart,ChartAxisWindow window) => chart.Prepare(VisualExportRequest.ForChart(chart).Context,
        new ChartCartesianViewport(x:window,valueDomain:ChartValueDomain.VisibleCategories));
}
