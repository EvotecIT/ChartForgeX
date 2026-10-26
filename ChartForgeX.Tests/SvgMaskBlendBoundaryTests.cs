using ChartForgeX.SvgRaster;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class SvgMaskBlendBoundaryTests {
    [Theory]
    [InlineData("root")] [InlineData("element")] [InlineData("text")]
    public void DefinitionAncestryControlsLuminanceForEveryActualMaskApplication(string owner) {
        const string definitions = "<defs color-interpolation='linearRGB'><mask id='m' maskUnits='userSpaceOnUse' maskContentUnits='userSpaceOnUse' x='0' y='0' width='48' height='48'><rect width='48' height='48' fill='#808080'/></mask></defs>";
        var mark = owner == "text" ? "<text x='3' y='34' font-size='32' fill='white' mask='url(#m)'>M</text>"
            : "<rect x='8' y='8' width='32' height='32' fill='white'" + (owner == "element" ? " mask='url(#m)'" : "") + "/>";
        var svg = "<svg xmlns='http://www.w3.org/2000/svg' width='48' height='48'" + (owner == "root" ? " mask='url(#m)'" : "") + ">" + definitions + mark + "</svg>";
        var actual = SvgRasterizer.Rasterize(svg, strict: true);
        MaskBlendBeforeFixture.Capture("svg-mask-owner-" + owner, svg, actual);
        var maximum = Enumerable.Range(0, actual.Image.Pixels.Length / 4).Max(at => actual.Image.Pixels[at * 4 + 3]);
        Assert.Equal(55, maximum); Assert.Empty(actual.Diagnostics);
    }
    [Fact]
    public void CssVariablesInitialResetAndIndependentFilterPropertyResolveAtTheDefinition() {
        var svg = "<svg xmlns='http://www.w3.org/2000/svg' width='32' height='32'><style>defs{color-interpolation:linearRGB}mask{--mask-space:sRGB;color-interpolation:var(--mask-space)}</style>"
            + "<defs><mask id='m' maskUnits='userSpaceOnUse' maskContentUnits='userSpaceOnUse' x='0' y='0' width='32' height='32' color-interpolation-filters='linearRGB'>"
            + "<rect width='32' height='32' fill='#808080'/></mask></defs><rect width='32' height='32' fill='white' mask='url(#m)'/></svg>";
        var css = SvgRasterizer.Rasterize(svg, strict: true);
        var resetSource = svg.Replace("color-interpolation:var(--mask-space)", "color-interpolation:initial", StringComparison.Ordinal);
        var reset = SvgRasterizer.Rasterize(resetSource, strict: true);
        MaskBlendBeforeFixture.Capture("svg-mask-css-space", svg, css); MaskBlendBeforeFixture.Capture("svg-mask-initial-space", resetSource, reset);
        Assert.Equal(128, MaskBlendFixture.Pixel(css.Image, 16, 16)[3]); Assert.Equal(css.Image.Pixels, reset.Image.Pixels);
    }
    [Fact]
    public void ManyAttributesUseTheSameDefinitionColorInterpolationAsTheSmallPresentationReader() {
        var source = MaskBlendBeforeFixture.Mask("#808080", "luminance", "1", "linearRGB");
        var attributes = string.Join(" ", Enumerable.Range(0, 36).Select(index => "data-probe-" + index + "='present'"));
        var larger = source.Replace("<mask id=", "<mask " + attributes + " id=", StringComparison.Ordinal);
        var normal = SvgRasterizer.Rasterize(source, strict: true); var many = SvgRasterizer.Rasterize(larger, strict: true);
        MaskBlendBeforeFixture.Capture("svg-mask-many-attributes", larger, many);
        Assert.Equal(55, MaskBlendFixture.Pixel(many.Image, 16, 16)[3]); Assert.Equal(normal.Image.Pixels, many.Image.Pixels);
    }

    [Theory]
    [InlineData("color-interpolation-filters='sRGB'", "plus-lighter")]
    public void UnqualifiedBlendOperatorsStillDiagnoseAndReject(string space, string mode) {
        var svg = NativeMaskExample.FilterSource("normal").Replace("mode='normal'", "mode='" + mode + "'", StringComparison.Ordinal).Replace("color-interpolation-filters='sRGB'", space, StringComparison.Ordinal);
        var diagnostic = SvgRasterizer.Rasterize(svg, filters: new SvgRasterFilterOptions());
        MaskBlendBeforeFixture.Capture("svg-blend-reject-" + mode + (space.Contains("linearRGB", StringComparison.Ordinal) ? "-linear" : "-sRGB"), svg, diagnostic);
        Assert.Contains(diagnostic.Diagnostics, item => item.Code == "SFR002");
        Assert.Throws<NotSupportedException>(() => SvgRasterizer.Rasterize(svg, strict: true, filters: new SvgRasterFilterOptions()));
    }
}
