using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Checks that one continuous-fill SVG can obtain readable ink from either host theme.</summary>
public sealed class SvgDerivedInkTests {
    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void DivergingCellLabelsContrastWithActualFillsInEitherHostTheme(VisualThemeMode sourceMode) {
        var theme = VisualTheme.Graphite();
        Chart ChartFor(VisualThemeMode mode) => Chart.Create().WithSize(960, 240).WithTheme(theme, mode).WithDataLabels()
            .WithHeatmapColorScale(theme.Resolve(mode).DivergingRamp!.ToColorScale(0))
            .AddHeatmapRow("Change", new double?[] { -3, -2, -1, 0, 1, 2, 3 });
        XElement[] Cells(string svg) => XDocument.Parse(svg).Descendants()
            .Where(element => (string?)element.Attribute("data-cfx-role") == "heatmap-cell").ToArray();
        var bound = ChartFor(sourceMode).WithSvgColorVariables(SvgColorVariables.SharedRoles(theme, sourceMode)).ToSvg();
        var boundCells = Cells(bound);
        Assert.Equal(7, boundCells.Length);
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
            var nativeCells = Cells(ChartFor(mode).ToSvg());
            var variables = SvgColorVariables.SharedRoles(theme, mode).GetVariablesForSvg(bound).ToDictionary(variable => variable.Name);
            Assert.Equal(7, nativeCells.Length);
            for (var index = 0; index < nativeCells.Length; index++) {
                var fill = ChartColor.Parse(nativeCells[index].Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "heatmap-cell-shape").Attribute("fill")!.Value);
                var nativeInk = ChartColor.Parse(nativeCells[index].Descendants().Single(element => element.Name.LocalName == "text").Attribute("fill")!.Value);
                var paint = boundCells[index].Descendants().Single(element => element.Name.LocalName == "text").Attribute("fill")!.Value;
                Assert.StartsWith("var(--cfx-derived-ink-", paint, StringComparison.Ordinal);
                var reboundInk = variables[paint.Substring(4, paint.IndexOf(',') - 4)].Color;
                Assert.Equal(nativeInk, reboundInk);
                Assert.True(ChartColorMath.ContrastRatio(fill, nativeInk) >= 4.5, mode + " cell " + (index - 3));
                if (mode == VisualThemeMode.Dark && index == 2) Assert.Equal(ChartColor.White, nativeInk);
            }
        }
    }

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
