using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class EmptyHorizontalBaselineCompanionTests {
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void EmptyBaselineCompanionDoesNotLowerALogarithmicLineDomain(int physicalAxis) {
        var chart = Chart.Create().AddLine("Readings", new[] { new ChartPoint(1, 20), new ChartPoint(2, 40) });
        if (physicalAxis == 0) {
            chart.Options.LineOrientation = ChartOrientation.Horizontal;
            chart.Options.XAxis.Scale = ChartScaleKind.Logarithmic;
        } else if (physicalAxis == 1) chart.Options.YAxis.Scale = ChartScaleKind.Logarithmic;
        else {
            chart.Series[0].UseSecondaryYAxis();
            chart.Options.SecondaryYAxis.Scale = ChartScaleKind.Logarithmic;
        }
        ChartRange Domain() {
            var primary = ChartRange.FromChart(chart);
            return physicalAxis == 2 ? ChartRange.FromSecondaryYAxis(chart, primary) : primary;
        }
        var expected = Domain();
        if (physicalAxis == 0) chart.AddHorizontalBar("Empty", Array.Empty<ChartPoint>());
        else {
            chart.AddBar("Empty", Array.Empty<ChartPoint>());
            if (physicalAxis == 2) chart.Series[1].UseSecondaryYAxis();
        }
        var actual = Domain();
        Assert.Equal(expected.MinX, actual.MinX); Assert.Equal(expected.MaxX, actual.MaxX);
        Assert.Equal(expected.MinY, actual.MinY); Assert.Equal(expected.MaxY, actual.MaxY);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void EmptyBaselineCompanionDoesNotChangePopulatedHorizontalLineDomain(bool waterfall, bool negative) {
        var sign = negative ? -1 : 1;
        var chart = Chart.Create().AddLine("Readings", new[] { new ChartPoint(1, sign * 20), new ChartPoint(2, sign * 40) });
        chart.Options.LineOrientation = ChartOrientation.Horizontal;
        var expected = ChartRange.FromChart(chart);
        if (waterfall) chart.AddWaterfall("Empty", Array.Empty<ChartPoint>()).ConfigureWaterfall(options => options.Orientation = ChartOrientation.Horizontal);
        else chart.AddHorizontalBar("Empty", Array.Empty<ChartPoint>());
        var actual = ChartRange.FromChart(chart);
        Assert.Equal(expected.MinX, actual.MinX); Assert.Equal(expected.MaxX, actual.MaxX);
        Assert.Equal(expected.MinY, actual.MinY); Assert.Equal(expected.MaxY, actual.MaxY);
    }
}
