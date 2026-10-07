using System;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects source role and exact RGBA fallback through derived opacity expressions.</summary>
public sealed class SvgPaintOpacityTests {
    [Fact]
    public void OpacityRetainsOriginalAlphaBeforeApplyingItsMultiplier() {
        var source = ChartColor.FromHex("#2864B4").WithAlpha(128);
        var actual = ChartColorMath.WithOpacity(source, .5);
        var paint = SvgPaint.Of(source, SvgColorRole.Status).WithOpacity(actual, .5);
        var variables = new SvgColorVariables().Add("--source", source, SvgColorRole.Status);
        Assert.Equal("color-mix(in srgb, var(--source, #2864B480) 50%, transparent)", SvgPaint.Resolve(paint.Value!, variables));
        Assert.Equal(actual.ToCss(), SvgPaint.Resolve(paint.Value!, null));
        // A variable less opaque than the source cannot represent that source. Do not map its attenuated result by RGB.
        var attenuatedOnly = new SvgColorVariables().Add("--attenuated", actual, SvgColorRole.Status);
        Assert.Equal(actual.ToCss(), SvgPaint.Resolve(paint.Value!, attenuatedOnly));
        var opaque = new SvgColorVariables().Add("--opaque", ChartColor.FromHex("#2864B4"), SvgColorRole.Status);
        Assert.Equal("color-mix(in srgb, color-mix(in srgb, var(--opaque, #2864B4) 50.2%, transparent) 50%, transparent)", SvgPaint.Resolve(paint.Value!, opaque));
    }

    [Fact]
    public void NestedMixedOpacityScannerMatchesReferenceAndProtectsLiteralFallbacks() {
        var source = ChartColor.FromHex("#2864B4");
        var mix = ChartColorMath.Blend(ChartColor.White, source, .8);
        var actual = ChartColorMath.WithOpacity(mix, .25);
        var expression = SvgPaint.Mix(mix, ChartColor.White, null, source, SvgColorRole.Series, .8)
            .WithOpacity(ChartColorMath.WithOpacity(mix, .5), .5).WithOpacity(actual, .5);
        var variables = new SvgColorVariables().Add("--series", source, SvgColorRole.Series).Add("--derived", actual);
        var literal = SvgPaint.Literal(source).WithOpacity(ChartColorMath.WithOpacity(source, .3), .3);
        var text = "<rect fill=\"" + expression.Value + "\"/>" + expression.Value + literal.Value
            + "﷐O2864B440=:0.5﷑" + "﷐O2864B440not_base64:0.5﷑";
        foreach (var current in new[] { null, variables }) foreach (var keep in new[] { false, true })
            Assert.Equal(SvgPaint.ResolveWithPattern(text, current, keep), SvgPaint.Resolve(text, current, keep));
        Assert.Equal(actual.ToCss(), SvgPaint.Resolve(expression.Value!, null));
        Assert.Contains("var(--series,", SvgPaint.Resolve(expression.Value!, variables));
        Assert.DoesNotContain("var(", SvgPaint.Resolve(literal.Value!, variables));
        Assert.DoesNotContain("var(--derived,", SvgPaint.Resolve(expression.Value!, variables));
    }
}
