using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>A default value axis never labels a gridline with a number it doesn't sit on.</summary>
public sealed class AxisTickCaptionExactnessTests {
    [Fact]
    public void FormatCompactScaleValues_QuarterThousandTicks_UseFullNumbersInsteadOfRoundedCompactOnes() {
        Assert.Equal(new[] { "0", "250", "500", "750", "1,000", "1,250" },
            ChartNumericFormatter.FormatCompactScaleValues(new[] { 0d, 250, 500, 750, 1000, 1250 }));
    }

    [Fact]
    public void FormatCompactScaleValues_ExactCompactTicks_StayCompact() {
        Assert.Equal(new[] { "0", "2.5k", "5k", "7.5k", "10k" },
            ChartNumericFormatter.FormatCompactScaleValues(new[] { 0d, 2500, 5000, 7500, 10000 }));
    }

    [Fact]
    public void FormatCompactScaleValues_LargeTicksThatWouldRound_KeepTheirUnitWithTheDecimalsTheyNeed() {
        Assert.Equal(new[] { "0", "12.25k", "24.5k", "36.75k" },
            ChartNumericFormatter.FormatCompactScaleValues(new[] { 0d, 12250, 24500, 36750 }));
    }

    [Theory]
    [InlineData(12255d, "12.255k")]
    [InlineData(1250.5d, "1,250.5")]
    [InlineData(3456789d, "3.456789M")]
    public void FormatCompactScaleValues_TickThatNeedsMorePrecision_ReadsBackAsItself(double tick, string expected) {
        Assert.Equal(expected, ChartNumericFormatter.FormatCompactScaleValues(new[] { 0d, tick })[1]);
    }
}