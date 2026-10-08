using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Authored CSS variables retain a safe SVG expression and an identical concrete native fallback.</summary>
public sealed class SvgPaintCssVariableTests {
    [Theory]
    [InlineData("var(--directory-edge)", 40, 100, 180, 128)]
    [InlineData("var(--directory-edge, #12345680)", 18, 52, 86, 128)]
    [InlineData("var(--directory-edge, rgba(18, 52, 86, 0.5))", 18, 52, 86, 128)]
    public void AuthoredVariablesRetainStandaloneFallbackAndNativeRgba(string css, byte red, byte green, byte blue, byte alpha) {
        var fallback = new ChartColor(40, 100, 180, 128);
        Assert.True(SvgPaint.TryCssVariable(css, fallback, out var resolved, out var paint));
        Assert.Equal(new ChartColor(red, green, blue, alpha), resolved);
        var builder = new VisualSceneBuilder(new VisualSize(20, 20), FontSpec.FromFamily("Missing paint test font"));
        builder.Rect(new ChartRect(2, 2, 16, 16), resolved, paint: new VisualScenePaintBinding(fill: paint));
        var scene = builder.Build();
        var svg = XDocument.Parse(VisualSceneSvgRenderer.Render(scene));
        var rect = svg.Descendants().Single(element => element.Name.LocalName == "rect");
        Assert.Equal("var(--directory-edge, " + resolved.ToCss() + ")", rect.Attribute("fill")!.Value);
        var image = VisualSceneRasterRenderer.Render(scene, supersampling: 1);
        Assert.Equal(new byte[] { red, green, blue, alpha }, image.Pixels.Skip((10 * image.Width + 10) * 4).Take(4));
        Assert.Equal(SvgPaint.ResolveWithPattern(paint.Value!, null), SvgPaint.Resolve(paint.Value!, null));
    }

    [Fact]
    public void OpacityAndReconstitutedTokensPreserveAuthoredVariableWithoutHostThemeMapping() {
        var source = new ChartColor(40, 100, 180, 128);
        Assert.True(SvgPaint.TryCssVariable("var(--directory-edge)", source, out _, out var paint));
        var result = ChartColorMath.WithOpacity(source, .5);
        var opacity = SvgPaint.FromToken(paint.WithOpacity(result, .5).Value!);
        Assert.True(opacity.HasCssVariable);
        var expected = "color-mix(in srgb, var(--directory-edge, " + source.ToCss() + ") 50%, transparent)";
        Assert.Equal(expected, SvgPaint.Resolve(opacity.Value!, null));
        Assert.Equal(expected, SvgPaint.ResolveWithPattern(opacity.Value!, null));
        var builder = new VisualSceneBuilder(new VisualSize(20, 20), FontSpec.FromFamily("Missing paint test font"));
        builder.Rect(new ChartRect(2, 2, 16, 16), result, paint: new VisualScenePaintBinding(fill: opacity));
        var scene = builder.Build();
        Assert.Contains(expected, VisualSceneSvgRenderer.Render(scene));
        var image = VisualSceneRasterRenderer.Render(scene, supersampling: 1);
        Assert.Equal(new byte[] { source.R, source.G, source.B, result.A }, image.Pixels.Skip((10 * image.Width + 10) * 4).Take(4));
    }

    [Theory]
    [InlineData("var(--edge, url(https://example.com/a))")]
    [InlineData("var(--edge, var(--other))")]
    [InlineData("var(--edge);stroke:red")]
    [InlineData("var(--edge\" onclick=\"alert(1))")]
    [InlineData("var(--edge, red);fill:blue")]
    [InlineData("var(--edge,)")]
    [InlineData("var(edge)")]
    public void UnsupportedExpressionsCannotEnterTypedPaint(string expression) {
        Assert.False(SvgPaint.TryCssVariable(expression, ChartColor.Black, out _, out _));
    }

    [Fact]
    public void PlainVariableBindingUsesTheConcreteMarkFallbackAndExpressionBudgetsAreBounded() {
        var color = new ChartColor(40, 100, 180);
        var builder = new VisualSceneBuilder(new VisualSize(20, 20), FontSpec.FromFamily("Missing paint test font"));
        builder.Rect(new ChartRect(2, 2, 16, 16), color,
            paint: new VisualScenePaintBinding(fill: SvgPaint.Plain("var(--directory-edge)")));
        Assert.Contains("var(--directory-edge, " + color.ToCss() + ")", VisualSceneSvgRenderer.Render(builder.Build()));
        Assert.False(SvgPaint.TryCssVariable("var(--" + new string('x', 129) + ")", color, out _, out _));
        Assert.False(SvgPaint.TryCssVariable(new string(' ', 513), color, out _, out _));
        // Generic opacity cannot turn an arbitrary unvalidated CSS string into an executable paint expression.
        var unsafeOpacity = SvgPaint.Plain("var(--edge);stroke:red").WithOpacity(color, .5);
        Assert.Equal(color.ToCss(), SvgPaint.Resolve(unsafeOpacity.Value!, new SvgColorVariables()));
    }
}
