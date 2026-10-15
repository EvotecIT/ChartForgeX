using ChartForgeX.Core;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NiceNumericLayoutTests {
    [Theory]
    [InlineData(0, 7, 5)]
    [InlineData(-3.1, 17.3, 6)]
    [InlineData(0, 7e180, 5)]
    public void AutomaticTicksCoverDataWithAnEvenRoundStep(double minimum, double maximum, int count) {
        var ticks = ChartTicks.Generate(minimum, maximum, count);
        Assert.True(ticks[0] <= minimum && ticks[^1] >= maximum);
        var step = ticks[1] - ticks[0];
        for (var i = 2; i < ticks.Count; i++) Assert.InRange((ticks[i] - ticks[i - 1]) / step, 0.999999, 1.000001);
        if (maximum == 7) Assert.Equal(new[] { 0d, 2d, 4d, 6d, 8d }, ticks);
    }

    [Fact]
    public void InteriorTicksDoNotAppendUnalignedEndpoints() {
        var ticks = ChartTicks.GenerateInside(1.7, 14, 6);
        Assert.All(ticks, tick => Assert.InRange(tick, 1.7, 14));
        for (var i = 2; i < ticks.Count; i++) Assert.Equal(ticks[1] - ticks[0], ticks[i] - ticks[i - 1], 9);
    }

    [Theory]
    [InlineData(.2, .8)]
    [InlineData(-.8, -.2)]
    [InlineData(-.2, .4)]
    public void ArithmeticRoundoffPreservesAlignedDecimalTicks(double minimum, double maximum) {
        foreach (var inside in new[] { false, true }) {
            var ticks = inside ? ChartTicks.GenerateInside(minimum, maximum, 4) : ChartTicks.Generate(minimum, maximum, 4);
            Assert.Equal(4, ticks.Count);
            for (var index = 0; index < ticks.Count; index++) Assert.Equal(minimum + .2 * index, ticks[index], 14);
            if (inside) Assert.All(ticks, tick => Assert.InRange(tick, minimum, maximum));
            else Assert.True(ticks[0] <= minimum && ticks[^1] >= maximum);
        }
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 2.5)]
    [InlineData(2.5, 5)]
    [InlineData(5, 10)]
    public void GenuinelyLargerIntervalsStillRoundUp(double boundary, double expectedStep) {
        var requestedStep = boundary + 5e-13;
        foreach (var inside in new[] { false, true }) {
            var ticks = inside ? ChartTicks.GenerateInside(0, requestedStep * 3, 4) : ChartTicks.Generate(0, requestedStep * 3, 4);
            Assert.InRange(ticks.Count, 2, 4);
            Assert.Equal(expectedStep, ticks[1] - ticks[0]);
            for (var index = 1; index < ticks.Count; index++) Assert.True(ticks[index] - ticks[index - 1] >= requestedStep);
        }
    }

    [Fact]
    public void OutwardAlignmentAdvancesTheStepToRespectTheTickBudget() {
        var ticks = ChartTicks.Generate(.1, .7, 4);
        Assert.Equal(new[] { 0d, .25, .5, .75 }, ticks);
    }

    [Theory]
    [InlineData(1e180, 9.999999999999999e181)]
    [InlineData(2e180, 1.9999999999999998e182)]
    [InlineData(-9.999999999999999e181, -1e180)]
    public void DenormalizedInteriorTicksRetainStrictAuthoredBounds(double minimum, double maximum) {
        var ticks = ChartTicks.GenerateInside(minimum, maximum, 100);
        Assert.InRange(ticks.Count, 2, 100);
        Assert.Equal(minimum, ticks[0]);
        Assert.All(ticks, tick => Assert.InRange(tick, minimum, maximum));
        for (var index = 1; index < ticks.Count; index++) Assert.True(ticks[index] > ticks[index - 1]);
        var outside = ChartTicks.Generate(minimum, maximum, 100);
        Assert.InRange(outside.Count, 2, 100);
        Assert.True(outside[0] <= minimum && outside[^1] >= maximum);
    }

    [Fact]
    public void DecimalCountBinsRetainTheirAlignedDataEnvelope() {
        var bins = ChartHistogramBinLayout.FromCount(.2, .8, 3);
        Assert.Equal(3, bins.Count);
        Assert.Equal(.2, bins.Minimum);
        Assert.Equal(.8, bins.Maximum);
        for (var index = 0; index < bins.Count; index++) Assert.Equal(.2, bins.GetWidth(index), 14);
    }

    [Theory]
    [InlineData(28, 98, 3)]
    [InlineData(-14, 79, 4)]
    [InlineData(-2, 0, 1)]
    public void CountBinsUseAlignedEqualRoundWidths(double minimum, double maximum, int count) {
        var bins = ChartHistogramBinLayout.FromCount(minimum, maximum, count);
        Assert.True(bins.Minimum <= minimum && bins.Maximum >= maximum);
        Assert.Equal(Math.Round(bins.Minimum / bins.GetWidth(0)), bins.Minimum / bins.GetWidth(0), 9);
        for (var i = 0; i < bins.Count; i++) Assert.Equal(bins.GetWidth(0), bins.GetWidth(i), 9);
        if (minimum == 28) Assert.Equal(new[] { 25d, 50d, 75d, 100d }, Enumerable.Range(0, bins.Count).Select(bins.GetLowerBound).Append(bins.Maximum));
    }

    [Fact]
    public void WidthBinsAlignAndExactModeRetainsTheRemainder() {
        var bins = ChartHistogramBinLayout.FromWidth(2, 10, 3);
        Assert.Equal(0, bins.Minimum); Assert.Equal(12, bins.Maximum); Assert.Equal(3, bins.GetWidth(0));
        var exact = ChartHistogramBinLayout.FromWidth(2, 10, 3, roundBounds: false);
        Assert.Equal(2, exact.Minimum); Assert.Equal(10, exact.Maximum);
        Assert.Equal(2, exact.GetUpperBound(2) - exact.GetLowerBound(2));
        var chart = Chart.Create().AddHistogram("rounded", new[] { 28d, 52d, 98d }, 3);
        Assert.Equal(new[] { (25d, 50d), (50d, 75d), (75d, 100d) }, chart.Series[0].HistogramBins.Select(bin => (bin.LowerBound, bin.UpperBound)));
        Assert.Equal(3, chart.Series[0].Points.Sum(point => point.Y));
    }
}
