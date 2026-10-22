using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ChartAxisProjectionConfinementTests {
    [Theory]
    [InlineData(false, 3, 7, 3, 7)]
    [InlineData(true, 3, 7, 3, 7)]
    [InlineData(false, -2, 2, 0, 4)]
    [InlineData(true, -2, 2, 0, 4)]
    [InlineData(false, 8, 12, 6, 10)]
    [InlineData(true, 8, 12, 6, 10)]
    [InlineData(false, -100, -96, 0, 4)]
    [InlineData(true, 96, 100, 6, 10)]
    public void GestureWindowsKeepTheirSpanWhenPannedAcrossEitherFullDomainEdge(bool reversed,
        double first, double second, double expectedMinimum, double expectedMaximum) {
        var projection = Projection(reversed);
        // Drag direction and X-axis reversal must both yield the same increasing native window.
        foreach (var pair in new[] { (first, second), (second, first) }) {
            var window = Assert.IsType<ChartAxisWindow>(projection.ConstrainViewport(projection.Project(pair.Item1), projection.Project(pair.Item2)));
            Assert.Equal(expectedMinimum, window.Minimum, 10);
            Assert.Equal(expectedMaximum, window.Maximum, 10);
            Assert.Equal(Math.Abs(second - first), window.Maximum - window.Minimum, 10);
        }
    }

    [Theory]
    [InlineData(false, 0, 10)]
    [InlineData(true, 0, 10)]
    [InlineData(false, -10, 10)]
    [InlineData(true, -10, 10)]
    [InlineData(false, 100, 110)]
    [InlineData(true, 100, 110)]
    public void AFullOrLargerRequestedSpanResetsToOrdinaryPreparation(bool reversed, double first, double second) {
        var projection = Projection(reversed);
        Assert.Null(projection.ConstrainViewport(projection.Project(first), projection.Project(second)));
    }

    [Fact]
    public void EmptyAndNonfiniteGesturesFailWithoutChangingTheCapturedProjection() {
        var projection = Projection(false);
        Assert.Throws<ArgumentOutOfRangeException>(() => projection.ConstrainViewport(100, 100));
        Assert.Throws<ArgumentOutOfRangeException>(() => projection.ConstrainViewport(double.NaN, 100));
        Assert.Throws<ArgumentOutOfRangeException>(() => projection.ConstrainViewport(100, double.PositiveInfinity));
        Assert.Equal(2, projection.Minimum); Assert.Equal(6, projection.Maximum);
        Assert.Equal(0, projection.FullMinimum); Assert.Equal(10, projection.FullMaximum);
    }

    [Fact]
    public void FiniteExtremeDomainsDoNotOverflowSpanComparisonOrEdgeShifting() {
        var projection = new ChartAxisProjection(-double.MaxValue, double.MaxValue, -double.MaxValue, double.MaxValue,
            new ChartRect(0, 0, 100, 100), new ChartAxis { Reversed = false }, ChartAxisDimension.X);
        Assert.Null(projection.ConstrainViewport(0, 100));
        var window = Assert.IsType<ChartAxisWindow>(projection.ConstrainViewport(5, 75));
        Assert.InRange(window.Minimum, -double.MaxValue, 0);
        Assert.InRange(window.Maximum, 0, double.MaxValue);
        // The requested span overflows a direct subtraction, while each requested endpoint stays finite.
        var shifted = new ChartAxisProjection(-1.7e308, 1.7e308, -1e308, 1e308,
            new ChartRect(0, 0, 100, 100), new ChartAxis { Reversed = true }, ChartAxisDimension.X);
        var confined = Assert.IsType<ChartAxisWindow>(shifted.ConstrainViewport(-39, 85));
        Assert.Equal(1.7e308, confined.Maximum);
        Assert.True(double.IsFinite(confined.Minimum));
        Assert.InRange(confined.Minimum, -double.MaxValue, 0);
    }

    private static ChartAxisProjection Projection(bool reversed) => new(0, 10, 2, 6, new ChartRect(50, 40, 400, 200), new ChartAxis { Reversed = reversed }, ChartAxisDimension.X);
}
