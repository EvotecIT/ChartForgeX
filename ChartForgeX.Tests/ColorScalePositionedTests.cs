using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Absolute anchors preserve numeric meaning independently of observed domains and bounded key sampling.</summary>
public sealed class ColorScalePositionedTests {
    private static readonly ChartColor Red = ChartColor.FromHex("#FF0000");
    private static readonly ChartColor Green = ChartColor.FromHex("#00FF00");
    private static readonly ChartColor Blue = ChartColor.FromHex("#0000FF");

    [Fact]
    public void AbsoluteAnchorsInterpolateOnlyBetweenAdjacentValuesAndClampToTheirDomain() {
        var scale = ChartColorScale.Sequential(new[] {
            new ChartColorStop(-20, ChartColor.Black), new ChartColorStop(0, Red),
            new ChartColorStop(5, Green), new ChartColorStop(40, ChartColor.White)
        });
        Assert.Equal(ChartColorScaleMode.Sequential, scale.Mode);
        Assert.Equal(-20, scale.MinimumValue); Assert.Equal(40, scale.MaximumValue);
        Assert.Equal("#800000", scale.ColorFor(-10).ToHex());
        Assert.Equal("#808000", scale.ColorFor(2.5).ToHex());
        Assert.Equal("#80FF80", scale.ColorFor(22.5).ToHex());
        Assert.Equal(ChartColor.Black, scale.ColorFor(-100)); Assert.Equal(ChartColor.White, scale.ColorFor(100));
        foreach (var stop in scale.Stops) Assert.Equal(stop.Color, scale.ColorFor(stop.Value, 1000, 2000));
        Assert.Equal(scale.Stops.Select(stop => stop.Color), scale.Colors);
    }

    [Fact]
    public void DivergingAnchorsKeepAnExactPivotWithoutRedistributingEitherArm() {
        var scale = Diverging();
        Assert.Equal(ChartColorScaleMode.Diverging, scale.Mode);
        Assert.Equal(0, scale.MidpointValue); Assert.Equal(ChartColor.White, scale.MidpointColor);
        Assert.Equal(Blue, scale.ColorFor(-3)); Assert.Equal(Green, scale.ColorFor(5));
        Assert.Equal("#8080FF", scale.ColorFor(-1.5).ToHex());
        Assert.Equal("#80FF80", scale.ColorFor(2.5).ToHex());
        Assert.Equal(0, scale.EffectiveMidpoint(100, 200));
    }

    [Fact]
    public void FactoriesDetachAuthoredCollectionsAndExposeReadOnlyAnchors() {
        var first = new ChartColorStop(-20, Blue); var last = new ChartColorStop(40, Red);
        var source = new List<ChartColorStop> { first, last };
        var scale = ChartColorScale.Sequential(source);
        source[0] = new ChartColorStop(30, Green); source.Clear();
        Assert.Equal(Blue, scale.ColorFor(-20)); Assert.Equal(Red, scale.ColorFor(40));
        Assert.Equal(new[] { -20d, 40 }, scale.Stops.Select(stop => stop.Value));
        Assert.Throws<NotSupportedException>(() => ((IList<ChartColorStop>)scale.Stops)[0] = last);
        var low = new[] { first }; var high = new[] { last };
        var diverging = ChartColorScale.Diverging(low, new ChartColorStop(0, ChartColor.White), high);
        low[0] = new ChartColorStop(-2, Green); high[0] = new ChartColorStop(3, Green);
        Assert.Equal(new[] { -20d, 0, 40 }, diverging.Stops.Select(stop => stop.Value));
        Assert.Equal(Blue, diverging.ColorFor(-20)); Assert.Equal(Red, diverging.ColorFor(40));
    }

    [Fact]
    public void ImmutableCopiesMoveOnlyRequestedAnchorsAndRetainPaintAndLabels() {
        var original = Diverging().WithLabels(" Cold ", " Pivot ", " Hot ").WithNoDataColor(Green);
        var updated = original.WithValueRange(-40, 80).WithMidpoint(1);
        Assert.Equal(new[] { -20d, -3, 0, 5, 40 }, original.Stops.Select(stop => stop.Value));
        Assert.Equal(new[] { -40d, -3, 1, 5, 80 }, updated.Stops.Select(stop => stop.Value));
        Assert.Equal(original.Colors, updated.Colors);
        Assert.Equal(Green, updated.NoDataColor); Assert.Equal("Cold", updated.LowLabel);
        Assert.Equal("Pivot", updated.MidpointLabel); Assert.Equal("Hot", updated.HighLabel);
        Assert.Equal(-40, updated.MinimumValue); Assert.Equal(80, updated.MaximumValue); Assert.Equal(1, updated.MidpointValue);
        Assert.Equal(Blue, updated.ColorFor(-3)); Assert.Equal(ChartColor.White, updated.ColorFor(1));
        var presentationCopy = updated.WithNoDataColor(Blue).WithLabels(null, "Zero", "High");
        Assert.Equal(updated.Stops.Select(stop => stop.Value), presentationCopy.Stops.Select(stop => stop.Value));
        Assert.Equal(updated.ColorFor(2), presentationCopy.ColorFor(2));
    }

    [Fact]
    public void TwoAnchorRangeCanMoveCompletelyBeyondThePreviousDomain() {
        var original = ChartColorScale.Sequential(new[] { new ChartColorStop(0, ChartColor.Black), new ChartColorStop(1, ChartColor.White) });
        var moved = original.WithValueRange(10, 20);
        Assert.Equal(new[] { 10d, 20 }, moved.Stops.Select(stop => stop.Value));
        Assert.Equal("#808080", moved.ColorFor(15).ToHex()); Assert.Equal(1, original.MaximumValue);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.PositiveInfinity)]
    public void StopValuesAndMovedAnchorsMustBeFinite(double invalid) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartColorStop(invalid, Red));
        var scale = Diverging();
        Assert.Throws<ArgumentOutOfRangeException>(() => scale.WithValueRange(invalid, 80));
        Assert.Throws<ArgumentOutOfRangeException>(() => scale.WithValueRange(-40, invalid));
        Assert.Throws<ArgumentOutOfRangeException>(() => scale.WithMidpoint(invalid));
    }

    [Fact]
    public void AuthoredStopsMustBePresentStrictlyOrderedAndSurroundThePivot() {
        var zero = new ChartColorStop(0, Red); var one = new ChartColorStop(1, Green);
        Assert.Throws<ArgumentNullException>(() => ChartColorScale.Sequential((IEnumerable<ChartColorStop>)null!));
        foreach (var invalid in new[] { Array.Empty<ChartColorStop>(), new[] { zero }, new[] { zero, zero }, new[] { one, zero }, new[] { zero, null! } })
            Assert.Throws<ArgumentException>(() => ChartColorScale.Sequential(invalid));
        Assert.Throws<ArgumentNullException>(() => ChartColorScale.Diverging(new[] { zero }, null!, new[] { one }));
        Assert.Throws<ArgumentNullException>(() => ChartColorScale.Diverging(null!, zero, new[] { one }));
        Assert.Throws<ArgumentNullException>(() => ChartColorScale.Diverging(new[] { zero }, one, null!));
        Assert.Throws<ArgumentException>(() => ChartColorScale.Diverging(Array.Empty<ChartColorStop>(), zero, new[] { one }));
        Assert.Throws<ArgumentException>(() => ChartColorScale.Diverging(new[] { zero }, one, Array.Empty<ChartColorStop>()));
        Assert.Throws<ArgumentException>(() => ChartColorScale.Diverging(new[] { one }, zero, new[] { one }));
        Assert.Throws<ArgumentException>(() => ChartColorScale.Diverging(new[] { zero }, one, new[] { one }));
    }

    [Fact]
    public void RangeAndPivotChangesCannotCrossOrCollapseTheirNeighboringAnchors() {
        var scale = Diverging();
        Assert.Throws<ArgumentOutOfRangeException>(() => scale.WithValueRange(-3, 80));
        Assert.Throws<ArgumentOutOfRangeException>(() => scale.WithValueRange(-40, 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => scale.WithValueRange(40, -20));
        Assert.Throws<ArgumentOutOfRangeException>(() => scale.WithMidpoint(-3));
        Assert.Throws<ArgumentOutOfRangeException>(() => scale.WithMidpoint(5));
        Assert.Throws<ArgumentOutOfRangeException>(() => scale.WithMidpoint(-100));
        Assert.Equal(ChartColor.White, scale.ColorFor(0));
        Assert.Throws<InvalidOperationException>(() => ChartColorScale.Sequential(scale.Stops).WithMidpoint(0));
    }

    [Theory]
    [InlineData(-double.MaxValue, 0, double.MaxValue)]
    [InlineData(0, 1e-7, 2e-7)]
    public void ExtremeFiniteIntervalsKeepExactAndIntermediateColors(double minimum, double midpoint, double maximum) {
        var scale = ChartColorScale.Sequential(new[] {
            new ChartColorStop(minimum, ChartColor.Black), new ChartColorStop(midpoint, Red), new ChartColorStop(maximum, ChartColor.White)
        });
        Assert.Equal(Red, scale.ColorFor(midpoint));
        Assert.Equal("#800000", scale.ColorFor(ChartMath.InterpolateRange(minimum, midpoint, .5)).ToHex());
        Assert.Equal("#FF8080", scale.ColorFor(ChartMath.InterpolateRange(midpoint, maximum, .5)).ToHex());
        Assert.All(ChartColorScaleLegend.Steps(scale, 0, 0), value => Assert.True(double.IsFinite(value)));
        Assert.Throws<ArgumentOutOfRangeException>(() => scale.ColorFor(0, double.NaN, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => scale.ColorFor(0, 2, 1));
    }

    [Fact]
    public void KeySamplesUseEqualNumericIntervalsAndAFractionalPivotRatherThanStopOrdinals() {
        var scale = Diverging();
        Assert.Equal(new[] { -20d, -5, 10, 25, 40 }, ChartColorScaleLegend.Steps(scale, 0, 0));
        Assert.Equal(4d / 3, ChartColorScaleLegend.MidpointStep(scale, 0, 0, 5), 12);
        Assert.Equal(new[] { -20d, -3, 0, 5, 40 }, Enumerable.Range(0, scale.Stops.Count).Select(index => scale.StopValue(index, 0, 0)));
        var sequential = ChartColorScale.Sequential(scale.Stops.Take(4));
        Assert.Equal(1.5, ChartColorScaleLegend.MidpointStep(sequential, 0, 0, 4));
    }

    [Fact]
    public void BoundedKeySamplingRetainsEveryAuthoredStopEvenWhenOnlyElevenSamplesFit() {
        var stops = Enumerable.Range(0, 16).Select(index => new ChartColorStop(index * index, index % 2 == 0 ? Blue : Red)).ToArray();
        var scale = ChartColorScale.Sequential(stops);
        var samples = ChartColorScaleLegend.Steps(scale, 0, 0);
        Assert.Equal(11, samples.Length); Assert.Equal(0, samples[0]); Assert.Equal(225, samples[10]); Assert.Equal(22.5, samples[1]);
        Assert.Equal(stops.Select(stop => stop.Value), scale.Stops.Select(stop => stop.Value));
        foreach (var stop in stops) Assert.Equal(stop.Color, scale.ColorFor(stop.Value));
    }

    private static ChartColorScale Diverging() => ChartColorScale.Diverging(
        new[] { new ChartColorStop(-20, ChartColor.Black), new ChartColorStop(-3, Blue) },
        new ChartColorStop(0, ChartColor.White),
        new[] { new ChartColorStop(5, Green), new ChartColorStop(40, Red) });
}
