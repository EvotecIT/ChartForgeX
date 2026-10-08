using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Checks that one continuous-fill SVG can obtain readable ink from either host theme.</summary>
public sealed class SvgDerivedInkTests {
    [Theory]
    [InlineData(.14)]
    [InlineData(.53)]
    [InlineData(.91)]
    public void OtherThemeDerivesOnlyUsedInkFromTheOriginalSvg(double amount) {
        var light = new SvgColorVariables().Add("--surface", ChartColor.White, SvgColorRole.Surface)
            .Add("--magnitude", ChartColor.FromHex("#104281"), SvgColorRole.Ramp);
        var dark = new SvgColorVariables().Add("--surface", ChartColor.FromHex("#16181C"), SvgColorRole.Surface)
            .Add("--magnitude", ChartColor.FromHex("#86B6EF"), SvgColorRole.Ramp);
        string Svg(SvgColorVariables variables) {
            var from = variables.Variables.Single(variable => variable.Name == "--surface").Color;
            var to = variables.Variables.Single(variable => variable.Name == "--magnitude").Color;
            var fill = new ChartColorBlend(from, SvgColorRole.Surface, to, SvgColorRole.Ramp, amount);
            return "<svg><text fill=\"" + SvgPaint.Resolve(ChartColorBlend.Contrast(fill).Paint.Value!, variables) + "\">42</text></svg>";
        }
        var lightSvg = Svg(light); var darkSvg = Svg(dark);
        var lightInk = light.GetVariablesForSvg(lightSvg).Single(variable => variable.Name.StartsWith("--cfx-derived-ink-", StringComparison.Ordinal));
        var reboundInk = dark.GetVariablesForSvg(lightSvg).Single(variable => variable.Name.StartsWith("--cfx-derived-ink-", StringComparison.Ordinal));
        var independentlyRenderedInk = dark.GetVariablesForSvg(darkSvg).Single(variable => variable.Name.StartsWith("--cfx-derived-ink-", StringComparison.Ordinal));
        Assert.Equal(lightInk.Name, reboundInk.Name);
        Assert.Equal(independentlyRenderedInk.Name, reboundInk.Name);
        Assert.Equal(independentlyRenderedInk.Color, reboundInk.Color);
        var darkFill = ChartColorMath.Blend(dark.Variables[0].Color, dark.Variables[1].Color, amount);
        Assert.True(ChartColorMath.ContrastRatio(darkFill, reboundInk.Color) >= 4.5);
        Assert.Equal(3, dark.GetVariablesForSvg(lightSvg + lightSvg).Count);
        Assert.Equal(2, dark.Variables.Count);
        Assert.Equal(2, dark.GetVariablesForSvg("<svg/>").Count);
        Assert.Equal(SvgPaint.ResolveWithPattern(ChartColorBlend.Contrast(new ChartColorBlend(ChartColor.White,
            SvgColorRole.Surface, ChartColor.FromHex("#104281"), SvgColorRole.Ramp, amount)).Paint.Value!, light),
            XDocument.Parse(lightSvg).Descendants("text").Single().Attribute("fill")!.Value);
    }

    [Fact]
    public void LiteralOperandAndResolvedFallbackAreRetained() {
        var source = ChartColor.FromHex("#104281");
        var fill = new ChartColorBlend(ChartColor.White, null, source, SvgColorRole.Series, .38);
        var paint = ChartColorBlend.Contrast(fill).Paint.Value!;
        Assert.Equal(ChartColorMath.AccessibleTextOnBackground(fill.Color).ToCss(), SvgPaint.Resolve(paint, null));
        var variables = new SvgColorVariables().Add("--magnitude", source, SvgColorRole.Series);
        var svg = "<svg><text fill=\"" + SvgPaint.Resolve(paint, variables) + "\"/></svg>";
        var ink = variables.GetVariablesForSvg(svg).Last();
        Assert.Equal(ChartColorMath.AccessibleTextOnBackground(fill.Color), ink.Color);
        var other = new SvgColorVariables().Add("--magnitude", ChartColor.Black, SvgColorRole.Series);
        Assert.Equal(ChartColorMath.AccessibleTextOnBackground(ChartColorMath.Blend(ChartColor.White, ChartColor.Black, .38)), other.GetVariablesForSvg(svg).Last().Color);
        Assert.Single(other.GetVariablesForSvg("var(--cfx-derived-ink-v1-invalid, #000000)"));
    }
}
