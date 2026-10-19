using ChartForgeX.Core;
using ChartForgeX.Primitives;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Authored key captions retain absolute meaning through immutable scale configuration.</summary>
public sealed class ColorScaleCaptionModelTests {
    [Fact]
    public void CaptionCatalogCopiesSortsAndClearsWithoutChangingMapping() {
        var original = ChartColorScale.Sequential(ChartColor.Black, ChartColor.White).WithValueRange(-20, 40);
        var captions = new List<ChartColorScaleCaption> { new(10, " Warm threshold "), new(-3, "Cold threshold") };
        var scale = original.WithCaptions(captions);
        captions.Clear();
        Assert.Equal(new[] { -3d, 10 }, scale.Captions.Select(caption => caption.Value));
        Assert.Equal(new[] { "Cold threshold", "Warm threshold" }, scale.Captions.Select(caption => caption.Text));
        Assert.Throws<NotSupportedException>(() => ((IList<ChartColorScaleCaption>)scale.Captions).Clear());
        Assert.Empty(original.Captions);
        foreach (var value in new[] { -20d, -3, 0, 10, 40 }) Assert.Equal(original.ColorFor(value), scale.ColorFor(value));
        Assert.Empty(scale.WithCaptions(Array.Empty<ChartColorScaleCaption>()).Captions);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EveryScaleCopyRetainsAbsoluteCaptionPositions(bool positioned) {
        var scale = positioned
            ? ChartColorScale.Diverging(new[] { new ChartColorStop(-20, ChartColor.Black) },
                new ChartColorStop(0, ChartColor.White), new[] { new ChartColorStop(40, ChartColor.Black) })
            : ChartColorScale.Diverging(ChartColor.Black, ChartColor.White, ChartColor.Black).WithValueRange(-20, 40);
        scale = scale.WithCaptions(new[] { new ChartColorScaleCaption(-3, "Lower"), new ChartColorScaleCaption(10, "Upper") });
        foreach (var copy in new[] { scale.WithValueRange(-40, 80), scale.WithMidpoint(1),
            scale.WithNoDataColor(ChartColor.Black), scale.WithLabels("Low", "Middle", "High") }) {
            Assert.Equal(scale.Captions, copy.Captions);
            Assert.Equal(new[] { -3d, 10 }, copy.Captions.Select(caption => caption.Value));
        }
        var outside = scale.WithValueRange(-2, 2);
        Assert.Equal(scale.Captions, outside.Captions);
        Assert.Equal(-2, outside.MinimumValue);
    }

    [Fact]
    public void InvalidCatalogCannotReplaceAnExistingScale() {
        var scale = ChartColorScale.Sequential(ChartColor.Black, ChartColor.White)
            .WithCaptions(new[] { new ChartColorScaleCaption(0, "Kept") });
        Assert.Throws<ArgumentNullException>(() => scale.WithCaptions(null!));
        Assert.Throws<ArgumentException>(() => scale.WithCaptions(new ChartColorScaleCaption[] { null! }));
        Assert.Throws<ArgumentException>(() => scale.WithCaptions(new[] { new ChartColorScaleCaption(0, "First"), new ChartColorScaleCaption(-0d, "Second") }));
        Assert.Throws<InvalidOperationException>(() => scale.WithCaptions(Throwing()));
        Assert.Equal("Kept", Assert.Single(scale.Captions).Text);
        var discrete = ChartColorScale.Discrete(new[] { new ChartColorBand(null, ChartColor.Black, "All") });
        Assert.Throws<InvalidOperationException>(() => discrete.WithCaptions(Array.Empty<ChartColorScaleCaption>()));

        static IEnumerable<ChartColorScaleCaption> Throwing() {
            yield return new ChartColorScaleCaption(1, "Not committed");
            throw new InvalidOperationException("Input enumeration failed.");
        }
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.PositiveInfinity)]
    public void CaptionPositionsMustBeFinite(double invalid) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartColorScaleCaption(invalid, "Caption"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t ")]
    public void CaptionsMustHaveText(string? invalid) =>
        Assert.ThrowsAny<ArgumentException>(() => new ChartColorScaleCaption(1, invalid!));
}
