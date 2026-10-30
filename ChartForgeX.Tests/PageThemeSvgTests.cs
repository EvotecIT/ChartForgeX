using System;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class PageThemeSvgTests {
    [Fact]
    public void SeriesAndPointColorsKeepDistinctVariablesAheadOfExportMaps() {
        var fallback = ChartColor.FromHex("#2468AC");
        var chart = Chart.Create().WithSize(360, 240)
            .AddBar("Status", new[] { new ChartPoint(1, 2), new ChartPoint(2, 3) }, fallback.WithCssVariable("--accent"));
        chart.Series[0].WithPointColor(1, fallback.WithCssVariable("--critical"));
        var svg = XDocument.Parse(chart.Prepare(new VisualRenderContext()).ToSvg(new VisualSvgOptions(
            colorVariables: new SvgColorVariables().Add("--mapped", fallback, SvgColorRole.Series))));
        var paints = svg.Descendants().Attributes().Where(attribute => attribute.Name.LocalName is "fill" or "stroke" or "stop-color")
            .Select(attribute => attribute.Value).ToArray();
        Assert.Contains(paints, value => value.Contains("var(--accent, #2468AC)", StringComparison.Ordinal));
        Assert.Contains(paints, value => value.Contains("var(--critical, #2468AC)", StringComparison.Ordinal));
        Assert.DoesNotContain(paints, value => value.Contains("var(--mapped,", StringComparison.Ordinal));
    }

    [Fact]
    public void EqualFallbackVariablesHaveDistinctDeterministicSvgNamespaces() {
        var fallback = ChartColor.FromHex("#2468AC");
        var first = Solid(fallback.WithCssVariable("--accent"));
        var second = Solid(fallback.WithCssVariable("--critical"));
        var svg = first.ToSvg();
        Assert.Equal(svg, first.ToSvg());
        Assert.NotEqual(Mark(XDocument.Parse(svg), "sample").Attribute("id")!.Value,
            Mark(XDocument.Parse(second.ToSvg()), "sample").Attribute("id")!.Value);
    }

    [Fact]
    public void OpacityChangesRetainTheVariableAndRasterUsesTheFallbackChannels() {
        var color = ChartColor.FromHex("#2468AC").WithCssVariable("--accent");
        var builder = Builder();
        builder.Rect(new ChartRect(0, 0, 20, 20), color.WithAlpha(128), role: "alpha");
        builder.Rect(new ChartRect(20, 0, 20, 20), color.WithOpacity(.25), role: "opacity");
        builder.Rect(new ChartRect(40, 0, 20, 20), ChartColorMath.WithOpacity(color.WithAlpha(128), .5), role: "multiplied");
        var prepared = new PreparedVisual(builder.Build());
        var svg = XDocument.Parse(prepared.ToSvg());
        Assert.Equal("color-mix(in srgb, var(--accent, #2468AC) 50.19608%, transparent)", (string?)Mark(svg, "alpha").Attribute("fill"));
        Assert.Equal("color-mix(in srgb, var(--accent, #2468AC) 25.09804%, transparent)", (string?)Mark(svg, "opacity").Attribute("fill"));
        Assert.Equal("color-mix(in srgb, var(--accent, #2468AC) 25.09804%, transparent)", (string?)Mark(svg, "multiplied").Attribute("fill"));

        var literal = Builder();
        literal.Rect(new ChartRect(0, 0, 20, 20), ChartColor.FromRgba(36, 104, 172, 128));
        literal.Rect(new ChartRect(20, 0, 20, 20), ChartColor.FromRgba(36, 104, 172, 64));
        literal.Rect(new ChartRect(40, 0, 20, 20), ChartColor.FromRgba(36, 104, 172, 64));
        var options = new VisualRenderOptions(supersampling: 1);
        var pixels = prepared.ToRgba(options);
        Assert.Equal(new PreparedVisual(literal.Build()).ToRgba(options).Pixels, pixels.Pixels);
        var offset = (10 * pixels.Width + 10) * 4;
        Assert.Equal(new byte[] { 36, 104, 172, 128 }, pixels.Pixels.Skip(offset).Take(4).ToArray());
        Assert.Equal(0x2468AC80u, color.WithAlpha(128).ToRgba());
    }

    [Fact]
    public void GradientTintRetainsTheDirectVariableWithoutAHostColorMap() {
        var color = ChartColor.FromHex("#2468AC").WithCssVariable("--accent");
        var tint = ChartColorMath.Blend(ChartColor.White, color, .5);
        var builder = Builder();
        builder.RectGradient(new ChartRect(0, 0, 60, 20), new ChartPoint(0, 0), new ChartPoint(60, 0), new[] {
            new VisualGradientStop(0, tint, SvgPaint.Mix(tint, ChartColor.White, null, color, SvgColorRole.Series, .5)),
            new VisualGradientStop(1, color, SvgPaint.Of(color, SvgColorRole.Series))
        });
        var stops = XDocument.Parse(new PreparedVisual(builder.Build()).ToSvg()).Descendants()
            .Where(element => element.Name.LocalName == "stop").ToArray();
        Assert.Equal("color-mix(in srgb, #FFFFFF 50%, var(--accent, #2468AC))", (string?)stops[0].Attribute("stop-color"));
        Assert.Equal("var(--accent, #2468AC)", (string?)stops[1].Attribute("stop-color"));
    }

    [Fact]
    public void InheritedFontsLeaveTextFamilyAndSizeUnsetAndKeepPreparedPositions() {
        var prepared = Caption();
        var original = prepared.ToSvg();
        Assert.Equal(original, prepared.ToSvg(new VisualSvgOptions()));
        var fixedText = XDocument.Parse(original).Descendants().Single(element => element.Name.LocalName == "text");
        Assert.Equal("sans-serif", (string?)fixedText.Attribute("font-family"));
        Assert.Equal("12", (string?)fixedText.Attribute("font-size"));

        var inherited = XDocument.Parse(prepared.ToSvg(new VisualSvgOptions().WithInheritedFonts()
            .WithPatternTiles().WithTextPresentation(VisualSvgTextPresentation.Browser)));
        var text = inherited.Descendants().Single(element => element.Name.LocalName == "text");
        Assert.Null(text.Attribute("font-family")); Assert.Null(text.Attribute("font-size"));
        Assert.Equal("inherit", (string?)text.Parent!.Attribute("font-family"));
        Assert.Equal("inherit", (string?)text.Parent!.Attribute("font-size"));
        Assert.Equal((string?)fixedText.Attribute("x"), (string?)text.Attribute("x"));
        Assert.Equal((string?)fixedText.Attribute("y"), (string?)text.Attribute("y"));
        Assert.Equal("Caption", text.Value);
        Assert.NotEqual((string?)fixedText.Parent!.Attribute("id"), (string?)text.Parent!.Attribute("id"));
        Assert.Equal(original, prepared.ToSvg());
    }

    [Theory]
    [InlineData("--body-font", "--chart-size", "var(--body-font, inherit)", "var(--chart-size, inherit)")]
    [InlineData("--body-font", null, "var(--body-font, inherit)", "inherit")]
    [InlineData(null, "--Chart_Size-2", "inherit", "var(--Chart_Size-2, inherit)")]
    public void FontVariablesAreWrittenVerbatimWithInheritanceFallback(string? family, string? size, string expectedFamily, string expectedSize) {
        var options = new VisualSvgOptions().WithPatternTiles().WithFontVariables(family, size).WithPatternTiles(false);
        var document = XDocument.Parse(Caption().ToSvg(options));
        var text = document.Descendants().Single(element => element.Name.LocalName == "text");
        Assert.Null(text.Attribute("font-family")); Assert.Null(text.Attribute("font-size"));
        Assert.Equal(expectedFamily, (string?)text.Parent!.Attribute("font-family"));
        Assert.Equal(expectedSize, (string?)text.Parent!.Attribute("font-size"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("--")]
    [InlineData("accent")]
    [InlineData("--accent\n")]
    [InlineData("--accent);fill:red")]
    [InlineData("--x\" onclick=\"alert(1)")]
    [InlineData("--with space")]
    public void PageThemeRejectsUnsafeCustomPropertyNames(string token) {
        Assert.Throws<ArgumentException>(() => ChartColor.Black.WithCssVariable(token));
        Assert.Throws<ArgumentException>(() => new VisualSvgOptions().WithFontVariables(familyVariable: token));
        Assert.Throws<ArgumentException>(() => new VisualSvgOptions().WithFontVariables(sizeVariable: token));
    }

    private static VisualSceneBuilder Builder() => new(new VisualSize(80, 40), new FontSpec { Family = "sans-serif" });
    private static PreparedVisual Solid(ChartColor color) {
        var builder = Builder(); builder.Rect(new ChartRect(0, 0, 20, 20), color, role: "sample", id: "sample");
        return new PreparedVisual(builder.Build());
    }
    private static PreparedVisual Caption() {
        var builder = Builder(); builder.Text("Caption", 4, 20, 12, ChartColor.Black, role: "caption", id: "caption");
        return new PreparedVisual(builder.Build());
    }
    private static XElement Mark(XDocument document, string role) => document.Descendants()
        .Single(element => (string?)element.Attribute("data-cfx-role") == role);
}
