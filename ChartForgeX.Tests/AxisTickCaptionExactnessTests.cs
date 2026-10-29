using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>A default value axis never labels a gridline with a number it doesn't sit on.</summary>
public sealed class AxisTickCaptionExactnessTests {
    [Fact]
    public void FormatCompactScaleValues_QuarterThousandTicks_UsePlainNumbersInsteadOfRoundedCompactOnes() {
        Assert.Equal(new[] { "0", "250", "500", "750", "1000", "1250" },
            ChartNumericFormatter.FormatCompactScaleValues(new[] { 0d, 250, 500, 750, 1000, 1250 }));
    }

    [Fact]
    public void FormatCompactScaleValues_ExactCompactTicks_StayCompact() {
        Assert.Equal(new[] { "0", "2.5k", "5k", "7.5k", "10k" },
            ChartNumericFormatter.FormatCompactScaleValues(new[] { 0d, 2500, 5000, 7500, 10000 }));
    }

    [Theory]
    [InlineData(12255d, "12255")]
    [InlineData(1250.5d, "1250.5")]
    [InlineData(1000.0000001d, "1000.0000001")]
    [InlineData(1000000000.9d, "1000000000.9")]
    [InlineData(1000000000999d, "1000000000999")]
    public void FormatCompactScaleValues_TickThatCompactNotationWouldRound_ReadsBackAsItself(double tick, string expected) {
        Assert.Equal(expected, ChartNumericFormatter.FormatCompactScaleValues(new[] { 0d, tick })[1]);
    }
}
