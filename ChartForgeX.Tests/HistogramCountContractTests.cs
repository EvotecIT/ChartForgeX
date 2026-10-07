using ChartForgeX.Core;
using ChartForgeX.Rendering;
using System.Xml.Linq;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class HistogramCountContractTests {
    [Theory]
    [InlineData(-1, 1)]
    [InlineData(-0.1, 0.2)]
    [InlineData(-1, 0)]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    [InlineData(-2, -1)]
    public void OneRequestedBinCoversBothEndpointsWithoutAddingAZeroBoundary(double minimum, double maximum) {
        var layout = ChartHistogramBinLayout.FromCount(minimum, maximum, 1);
        Assert.Equal(1, layout.Count);
        Assert.True(layout.Minimum <= minimum);
        Assert.True(layout.Maximum >= maximum);
        Assert.Equal(layout.Minimum, layout.GetLowerBound(0));
        Assert.Equal(layout.Maximum, layout.GetUpperBound(0));
        Assert.Equal(layout.Maximum - layout.Minimum, layout.Width);

        var chart = Chart.Create().AddHistogram("Samples", new[] { minimum, 0.5 * minimum + 0.5 * maximum, maximum }, layout);
        Assert.Equal(3, Assert.Single(chart.Series[0].Points).Y);
        var prepared = chart.Prepare(new VisualRenderContext());
        var svg = XDocument.Parse(prepared.ToSvg());
        Assert.Single(svg.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "bar");
        Assert.True(prepared.ToPng().Length > 64);
    }

    [Fact]
    public void RoundedFactoriesKeepDecimalAndAdjacentRepresentableEndpointsInTheirCoverage() {
        var aboveHalf = BitConverter.Int64BitsToDouble(BitConverter.DoubleToInt64Bits(0.5) + 1);
        foreach (var bounds in new[] { (0.3, 0.5), (-0.5, -0.3), (0d, aboveHalf) }) {
            foreach (var layout in new[] {
                ChartHistogramBinLayout.FromCount(bounds.Item1, bounds.Item2, 2),
                ChartHistogramBinLayout.FromWidth(bounds.Item1, bounds.Item2, 0.1)
            }) {
                Assert.True(layout.Minimum <= bounds.Item1);
                Assert.True(layout.Maximum >= bounds.Item2);
                var values = new[] { bounds.Item1, 0.5 * bounds.Item1 + 0.5 * bounds.Item2, bounds.Item2 };
                var chart = Chart.Create().AddHistogram("Samples", values, layout);
                Assert.Equal(values.Length, chart.Series[0].Points.Sum(point => point.Y));
                Assert.Equal(2, ChartHistogramBinLayout.FromCount(bounds.Item1, bounds.Item2, 2).Count);
            }
        }
    }

    [Fact]
    public void SingleCrossZeroBinPreservesExactBoundsAtTinyAndLargeFiniteMagnitudes() {
        foreach (var bounds in new[] { (-double.Epsilon, double.Epsilon), (-1e308, 1e307) }) {
            var rounded = ChartHistogramBinLayout.FromCount(bounds.Item1, bounds.Item2, 1);
            var exact = ChartHistogramBinLayout.FromCount(bounds.Item1, bounds.Item2, 1, roundBounds: false);
            Assert.Equal(1, rounded.Count);
            Assert.Equal(exact.Minimum, rounded.Minimum);
            Assert.Equal(exact.Maximum, rounded.Maximum);
            Assert.Equal(exact.Width, rounded.Width);
        }
        var multiple = ChartHistogramBinLayout.FromCount(-1, 1, 2);
        Assert.Equal(2, multiple.Count);
        Assert.Equal(0, multiple.GetUpperBound(0));
    }
}
