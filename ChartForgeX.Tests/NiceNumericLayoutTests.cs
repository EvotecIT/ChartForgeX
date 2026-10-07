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
    [InlineData(28, 98, 3)]
    [InlineData(-14, 79, 4)]
    [InlineData(-1, 1, 1)]
    public void CountBinsUseAlignedEqualRoundWidths(double minimum, double maximum, int count) {
        var bins = ChartHistogramBinLayout.FromCount(minimum, maximum, count);
        Assert.True(bins.Minimum <= minimum && bins.Maximum >= maximum);
        Assert.Equal(Math.Round(bins.Minimum / bins.Width), bins.Minimum / bins.Width, 9);
        for (var i = 0; i < bins.Count; i++) Assert.Equal(bins.Width, bins.GetUpperBound(i) - bins.GetLowerBound(i), 9);
        if (minimum == 28) Assert.Equal(new[] { 25d, 50d, 75d, 100d }, Enumerable.Range(0, bins.Count).Select(bins.GetLowerBound).Append(bins.Maximum));
    }

    [Fact]
    public void WidthBinsAlignAndExactModeRetainsTheRemainder() {
        var bins = ChartHistogramBinLayout.FromWidth(2, 10, 3);
        Assert.Equal(0, bins.Minimum); Assert.Equal(12, bins.Maximum); Assert.Equal(3, bins.Width);
        var exact = ChartHistogramBinLayout.FromWidth(2, 10, 3, roundBounds: false);
        Assert.Equal(2, exact.Minimum); Assert.Equal(10, exact.Maximum);
        Assert.Equal(2, exact.GetUpperBound(2) - exact.GetLowerBound(2));
        var chart = Chart.Create().AddHistogram("rounded", new[] { 28d, 52d, 98d }, 3);
        Assert.Equal(new[] { "25-50", "50-75", "75-100" }, chart.Options.XAxisLabels.Select(label => label.Text));
        Assert.Equal(3, chart.Series[0].Points.Sum(point => point.Y));
    }
}
