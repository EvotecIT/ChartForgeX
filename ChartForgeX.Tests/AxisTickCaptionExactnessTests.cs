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

    [Theory]
    [InlineData(1000000000001d, "1000000000001")]
    [InlineData(2000000000000001d, "2000000000000001")]
    [InlineData(9000000000000001d, "9000000000000001")]
    public void AxisCaption_SingleLargeNonRoundTick_ReadsBackAsItself(double tick, string expected) {
        var ticks = new[] { tick };
        var format = ChartAxisValueFormatter.Create(new ChartForgeX.Core.ChartAxis(), ticks);

        Assert.Equal(expected, format(ticks[0]));
    }

    public static IEnumerable<object[]> NearRoundIntegers() {
        for (var power = 3; power <= 15; power++)
            foreach (var lead in new[] { 1d, 2d, 9d }) {
                var value = lead * Math.Pow(10, power) + 1;
                if (value <= 9007199254740992d) yield return new object[] { value };
            }
        yield return new object[] { 9007199254740991d };
    }

    [Theory]
    [MemberData(nameof(NearRoundIntegers))]
    public void AxisCaption_SingleTickJustAboveARoundValue_ReadsBackExactly(double tick) {
        var caption = ChartAxisValueFormatter.Create(new ChartForgeX.Core.ChartAxis(), new[] { tick })(tick);
        var unit = caption.EndsWith("k", StringComparison.Ordinal) ? 1e3m : caption.EndsWith("M", StringComparison.Ordinal) ? 1e6m : caption.EndsWith("B", StringComparison.Ordinal) ? 1e9m : 1m;
        var number = decimal.Parse(unit == 1m ? caption : caption.Substring(0, caption.Length - 1), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);

        Assert.Equal(decimal.Parse(tick.ToString("R", System.Globalization.CultureInfo.InvariantCulture), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture), number * unit);
    }
}