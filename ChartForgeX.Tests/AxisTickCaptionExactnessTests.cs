using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>A default value axis never labels a gridline with a number it doesn't sit on, at the precision its step needs.</summary>
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

    [Fact]
    public void FormatCompactScaleValues_FineStepNearACompactUnit_ShowsEveryTickAsItself() {
        Assert.Equal(new[] { "12250", "12255", "12260" },
            ChartNumericFormatter.FormatCompactScaleValues(new[] { 12250d, 12255, 12260 }));
        Assert.Equal(new[] { "1000", "1000.5", "1001" },
            ChartNumericFormatter.FormatCompactScaleValues(new[] { 1000d, 1000.5, 1001 }));
    }

    [Fact]
    public void FormatCompactScaleValues_LogarithmicTicks_RoundWithinTheirStep() {
        Assert.Equal(new[] { "10", "31.62", "100", "316.23", "1k" },
            ChartNumericFormatter.FormatCompactScaleValues(new[] { 10d, Math.Pow(10, 1.5), 100, Math.Pow(10, 2.5), 1000 }));
    }

    [Fact]
    public void AxisCaption_SingleLargeNonRoundTick_ReadsBackAsItself() {
        var ticks = new[] { 1000000000001d };
        var format = ChartAxisValueFormatter.Create(new ChartForgeX.Core.ChartAxis(), ticks);

        Assert.Equal("1000000000001", format(ticks[0]));
    }
}