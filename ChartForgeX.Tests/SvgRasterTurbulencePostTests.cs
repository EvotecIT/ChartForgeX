using System;
using System.Linq;
using ChartForgeX.Effects;
using ChartForgeX.Rendering;
using ChartForgeX.SvgRaster;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>POST-API finite source membership; no browser-conformance or selected algorithm claim.</summary>
public sealed class SvgRasterTurbulencePostTests {
    [Theory]
    [InlineData("sRGB", false, VisualFilterColorSpace.Srgb)]
    [InlineData("linearRGB", false, VisualFilterColorSpace.LinearRgb)]
    [InlineData("", false, VisualFilterColorSpace.LinearRgb)]
    [InlineData("sRGB", true, VisualFilterColorSpace.Srgb)]
    public void DefinitionAncestryAndDefaultFilterSpaceFeedTheSameNativeSourceIndependentGenerator(
        string colorSpace, bool inherited, VisualFilterColorSpace expected) {
        var source = Source(colorSpace, inherited);
        var rendered = SvgRasterizer.Rasterize(source, strict: true, filters: new SvgRasterFilterOptions());
        var generated = new VisualTurbulence(.071, .113, 2, 13, VisualTurbulenceType.FractalNoise, colorSpace: expected)
            .Generate(new VisualSize(32, 24));
        NativeEffectBeforeCapture.Write("turbulence-source-space-" + colorSpace + "-" + inherited, source, rendered.Image.ToPng(), rendered.Image);
        Assert.Empty(rendered.Diagnostics);
        Assert.Equal(generated.Pixels, rendered.Image.Pixels);
        // SourceGraphic color/alpha are irrelevant: this primitive generates its own RGBA field.
        var transparent = SvgRasterizer.Rasterize(source.Replace("fill='#fff'", "fill='none'"), strict: true, filters: new SvgRasterFilterOptions());
        Assert.Empty(transparent.Diagnostics); Assert.Equal(rendered.Image.Pixels, transparent.Image.Pixels);
    }

    [Fact]
    public void TranslatedSubregionKeepsSourceCoordinatesAndOnlyItsActualOutputExtent() {
        var source = Source("sRGB", false, translated: true);
        var rendered = SvgRasterizer.Rasterize(source, strict: true, filters: new SvgRasterFilterOptions());
        var generated = new VisualTurbulence(.071, .113, 2, 13, VisualTurbulenceType.FractalNoise).Generate(new VisualSize(8, 6));
        NativeEffectBeforeCapture.Write("turbulence-source-translated-region", source, rendered.Image.ToPng(), rendered.Image);
        Assert.Empty(rendered.Diagnostics);
        for (var y = 0; y < 24; y++) for (var x = 0; x < 32; x++) {
            var pixel = rendered.Image.Pixels.Skip((y * 32 + x) * 4).Take(4).ToArray();
            Assert.Equal(x >= 6 && x < 14 && y >= 4 && y < 10
                ? generated.Pixels.Skip(((y - 4) * 8 + x - 6) * 4).Take(4).ToArray() : new byte[4], pixel);
        }
    }

    [Fact]
    public void UnsupportedOutputUnitsRemainLocatedDiagnosticsRatherThanGuessedNoise() {
        var source = Source("sRGB", false).Replace("filterUnits='userSpaceOnUse'", "filterUnits='objectBoundingBox'");
        var rendered = SvgRasterizer.Rasterize(source, filters: new SvgRasterFilterOptions());
        NativeEffectBeforeCapture.Write("turbulence-source-rejection-units", source, rendered.Image.ToPng(), rendered.Image);
        NativeEffectBeforeCapture.Json("turbulence-source-rejection-units-diagnostics", rendered.Diagnostics);
        Assert.Contains(rendered.Diagnostics, item => item.Code == "SFR002");
        Assert.Throws<NotSupportedException>(() => SvgRasterizer.Rasterize(source, strict: true, filters: new SvgRasterFilterOptions()));
    }

    private static string Source(string space, bool inherited, bool translated = false) {
        var color = space.Length == 0 ? "" : " color-interpolation-filters='" + space + "'";
        var body = "<filter id='noise' x='0' y='0' width='" + (translated ? 8 : 32) + "' height='" + (translated ? 6 : 24) +
            "' filterUnits='userSpaceOnUse' primitiveUnits='userSpaceOnUse'" + (inherited ? "" : color) +
            "><feTurbulence type='fractalNoise' baseFrequency='.071 .113' numOctaves='2' seed='13'/></filter>";
        return "<svg xmlns='http://www.w3.org/2000/svg' width='32' height='24'><defs" + (inherited ? color : "") + ">" +
            body + "</defs>" + (translated ? "<g transform='translate(6 4)'>" : "") +
            "<rect width='8' height='6' fill='#fff' filter='url(#noise)'/>" + (translated ? "</g>" : "") + "</svg>";
    }
}
