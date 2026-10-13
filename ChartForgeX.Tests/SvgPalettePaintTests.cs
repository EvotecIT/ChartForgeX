using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class SvgPalettePaintTests {
    [Fact]
    public void IndexedPalettePaintRetainsSlotsThroughCloningAndSparseHostMappings() {
        var color = ChartColor.FromRgba(96, 112, 128, 128);
        var tokens = new VisualDesignTokens { Palette = new[] { color, color } };
        var variables = tokens.ToSvgColorVariables(path => "--host-" + path.Replace('.', '-')).Clone();
        foreach (var index in new[] { 0, 1 }) {
            var paint = SvgPaint.Palette(color, index).WithOpacity(ChartColorMath.WithOpacity(color, .14), .14);
            var mapped = SvgPaint.Resolve(paint.Value!, variables);
            Assert.Contains("var(--host-series-" + (index + 1) + ",", mapped);
            Assert.Equal(SvgPaint.ResolveWithPattern(paint.Value!, variables), mapped);
            Assert.Equal(ChartColorMath.WithOpacity(color, .14).ToCss(), SvgPaint.Resolve(paint.Value!, null));
        }
        var sparse = tokens.ToSvgColorVariables(path => path == "series.1" ? null : "--host-" + path.Replace('.', '-'));
        Assert.Equal(color.ToCss(), SvgPaint.Resolve(SvgPaint.Palette(color, 0).Value!, sparse));
        Assert.Contains("var(--host-series-2,", SvgPaint.Resolve(SvgPaint.Palette(color, 1).Value!, sparse));
        var custom = new SvgColorVariables().Add("--manual", color, SvgColorRole.Series);
        Assert.Equal("var(--manual, #60708080)", SvgPaint.Resolve(SvgPaint.Palette(color, 1).Value!, custom));
    }

    [Fact]
    public void IndexedPaletteTintRetainsOpaqueOperandsAndExactStaticFallback() {
        var color = ChartColor.FromRgba(96, 112, 128, 128);
        var tokens = new VisualDesignTokens { Palette = new[] { color, color } };
        var opaque = ChartColor.FromRgb(color.R, color.G, color.B);
        var tint = new ChartColorBlend(ChartColor.White, SvgPaint.Opaque(ChartColor.White, SvgColorRole.Surface),
            opaque, SvgPaint.Palette(opaque, 1, opaque: true), .12);
        var ink = ChartColorBlend.Contrast(tint);
        var variables = tokens.ToSvgColorVariables();
        Assert.Contains("rgb(from var(--cfx-series-2, #60708080) r g b / 1)", SvgPaint.Resolve(tint.Paint.Value!, variables));
        Assert.Equal(tint.Color.ToCss(), SvgPaint.Resolve(tint.Paint.Value!, null));
        Assert.Equal(ink.Color.ToCss(), SvgPaint.Resolve(ink.Paint.Value!, null));
        foreach (var paint in new[] { tint.Paint, ink.Paint }) foreach (var keep in new[] { false, true })
            Assert.Equal(SvgPaint.ResolveWithPattern(paint.Value!, variables, keep), SvgPaint.Resolve(paint.Value!, variables, keep));
    }
}
