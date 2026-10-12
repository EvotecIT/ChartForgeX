using ChartForgeX.Core;
using ChartForgeX.Primitives;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Independently authored endpoints share one finite, ordered continuous mapping.</summary>
public sealed class ColorScalePartialDomainTests {
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void OneFixedEndpointInfersTheOtherAndClampsValues(bool fixMinimum) {
        var automatic = ChartColorScale.Sequential(ChartColor.Black, ChartColor.White);
        Func<double?, double?, ChartColorScale> setRange = automatic.WithValueRange;
        var scale = fixMinimum ? setRange(0, null) : setRange(null, 40);
        Assert.Equal(fixMinimum ? 0 : 10, scale.EffectiveMinimum(10));
        Assert.Equal(fixMinimum ? 30 : 40, scale.EffectiveMaximum(30));
        Assert.Equal(fixMinimum ? 15 : 25, scale.EffectiveMidpoint(10, 30));
        Assert.Equal(ChartColor.Black, scale.ColorFor(-100, 10, 30));
        Assert.Equal("#808080", scale.ColorFor(fixMinimum ? 15 : 25, 10, 30).ToHex());
        Assert.Equal(ChartColor.White, scale.ColorFor(100, 10, 30));
        Assert.Throws<InvalidOperationException>(() => scale.ColorFor(20));
        Assert.Null(automatic.MinimumValue); Assert.Null(automatic.MaximumValue);
    }

    [Theory]
    [InlineData(true, -20, -10)]
    [InlineData(true, -10, 0)]
    [InlineData(false, 10, 20)]
    [InlineData(false, 0, 10)]
    public void InferredEndpointCannotCrossItsFixedCounterpart(bool fixMinimum, double sourceMinimum, double sourceMaximum) {
        var scale = ChartColorScale.Sequential(ChartColor.Black, ChartColor.White)
            .WithValueRange(fixMinimum ? 0 : null, fixMinimum ? null : 0);
        Assert.Equal(0, scale.EffectiveMinimum(sourceMinimum));
        Assert.Equal(0, scale.EffectiveMaximum(sourceMaximum));
        Assert.Equal(0, scale.EffectiveMidpoint(sourceMinimum, sourceMaximum));
        Assert.Equal(fixMinimum ? ChartColor.Black : ChartColor.White, scale.ColorFor(0, sourceMinimum, sourceMaximum));
        Assert.Equal(fixMinimum ? ChartColor.Black : ChartColor.White, scale.ColorFor(fixMinimum ? sourceMinimum : sourceMaximum, sourceMinimum, sourceMaximum));
        Assert.Equal(ChartColor.Black, scale.ColorFor(-1, sourceMinimum, sourceMaximum));
        Assert.Equal(ChartColor.White, scale.ColorFor(1, sourceMinimum, sourceMaximum));
    }

    [Fact]
    public void ImmutableCopiesAndResetRetainMidpointColorsLabelsAndMissingPaint() {
        var middle = ChartColor.FromHex("#CC6600");
        var scale = ChartColorScale.Diverging(new[] { ChartColor.Black }, middle, new[] { ChartColor.White }, 5)
            .WithValueRange(-10, null).WithLabels("Low", "Target", "High").WithNoDataColor(ChartColor.FromHex("#778899"));
        var changed = scale.WithMidpoint(7).WithNoDataColor(ChartColor.White).WithLabels("Lower", "Pivot", "Upper");
        Assert.Equal(-10, changed.MinimumValue); Assert.Null(changed.MaximumValue);
        var reset = scale.WithValueRange(null, null);
        Assert.Null(reset.MinimumValue); Assert.Null(reset.MaximumValue);
        Assert.Equal(scale.Colors, reset.Colors); Assert.Equal(5, reset.MidpointValue);
        Assert.Equal(scale.NoDataColor, reset.NoDataColor);
        Assert.Equal("Low", reset.LowLabel); Assert.Equal("Target", reset.MidpointLabel); Assert.Equal("High", reset.HighLabel);
        Assert.Equal(middle, reset.ColorFor(5, 0, 10));
        Assert.Equal(ChartColor.Black, reset.ColorFor(5, 5, 5));
        Assert.Equal(ChartColor.Black, reset.ColorFor(100, 5, 5));
        Assert.Equal(-10, scale.MinimumValue); Assert.Null(scale.MaximumValue);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PartialFiniteExtremeDomainKeepsItsExactMidpointAndEndpointPaint(bool fixMinimum) {
        var scale = ChartColorScale.Sequential(ChartColor.Black, ChartColor.White)
            .WithValueRange(fixMinimum ? -double.MaxValue : null, fixMinimum ? null : double.MaxValue);
        Assert.Equal(-double.MaxValue, scale.EffectiveMinimum(-double.MaxValue));
        Assert.Equal(double.MaxValue, scale.EffectiveMaximum(double.MaxValue));
        Assert.Equal(0, scale.EffectiveMidpoint(-double.MaxValue, double.MaxValue));
        Assert.Equal("#808080", scale.ColorFor(0, -double.MaxValue, double.MaxValue).ToHex());
        Assert.Equal(ChartColor.Black, scale.ColorFor(-double.MaxValue, -double.MaxValue, double.MaxValue));
        Assert.Equal(ChartColor.White, scale.ColorFor(double.MaxValue, -double.MaxValue, double.MaxValue));
        var collapsed = scale.WithValueRange(fixMinimum ? double.MaxValue : null, fixMinimum ? null : -double.MaxValue);
        Assert.Equal(fixMinimum ? ChartColor.Black : ChartColor.White, collapsed.ColorFor(0, -double.MaxValue, double.MaxValue));
    }

    [Fact]
    public void PartialBoundsKeepFiniteValidationAndPositionedAndDiscreteBoundaries() {
        var scale = ChartColorScale.Sequential(ChartColor.Black, ChartColor.White);
        Assert.Throws<ArgumentOutOfRangeException>(() => scale.WithValueRange(double.NaN, null));
        Assert.Throws<ArgumentOutOfRangeException>(() => scale.WithValueRange(null, double.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => scale.WithValueRange(10, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => scale.WithValueRange(10, -10));
        Assert.Throws<ArgumentOutOfRangeException>(() => scale.WithValueRange(0, null).ColorFor(1, 10, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => scale.WithValueRange(null, 10).ColorFor(1, double.NegativeInfinity, 0));
        var positioned = ChartColorScale.Sequential(new[] {
            new ChartColorStop(0, ChartColor.Black), new ChartColorStop(10, ChartColor.FromHex("#888888")), new ChartColorStop(20, ChartColor.White)
        });
        Assert.Throws<ArgumentException>(() => positioned.WithValueRange(null, 30));
        Assert.Throws<ArgumentException>(() => positioned.WithValueRange(-10, null));
        Assert.Throws<ArgumentException>(() => positioned.WithValueRange(null, null));
        Assert.Equal(new[] { -10d, 10, 30 }, positioned.WithValueRange(-10, 30).Stops.Select(stop => stop.Value));
        var discrete = ChartColorScale.Discrete(new[] { new ChartColorBand(null, ChartColor.Black) });
        Assert.Throws<InvalidOperationException>(() => discrete.WithValueRange(null, 30));
        Assert.Throws<InvalidOperationException>(() => discrete.WithValueRange(null, null));
    }
}
