using System;
using System.IO;
using System.Linq;
using System.Threading;
using ChartForgeX.Raster;
using ChartForgeX.SvgRaster;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class SvgFiniteCompositeBoundaryTests {
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void WholeSourceIsFilteredBeforeItsOwnClipMaskAndOpacity(bool group) {
        var definitions = "<clipPath id='clip' clipPathUnits='userSpaceOnUse'><rect x='16' y='0' width='16' height='32'/></clipPath>"
            + "<mask id='mask' maskUnits='userSpaceOnUse' maskContentUnits='userSpaceOnUse' x='0' y='0' width='32' height='32' mask-type='alpha'>"
            + "<rect width='32' height='32' fill='white' fill-opacity='0.5019607843137255'/></mask>";
        const string own = "filter='url(#composite)' clip-path='url(#clip)' mask='url(#mask)' opacity='0.5'";
        var mark = "<rect x='8' y='8' width='16' height='16' fill='red' fill-opacity='0.5019607843137255' " + (group ? "" : own) + "/>";
        var body = group ? "<g " + own + ">" + mark + "</g>" : mark;
        var source = Svg(body, definitions);
        var result = SvgRasterizer.Rasterize(source, strict: true, filters: new SvgRasterFilterOptions());
        Capture("finite-composite-order-" + group, source, result);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(new byte[] { 255, 0, 0, 16 }, CompositeOperatorFixture.Pixel(result.Image, 17, 16));
        Assert.Equal(new byte[4], CompositeOperatorFixture.Pixel(result.Image, 15, 16));
        Assert.Equal(new byte[4], CompositeOperatorFixture.Pixel(result.Image, 2, 2));
    }

    [Fact]
    public void NullOptionsRetainTheExistingSourcePixelsAndStrictUnsupportedDiagnostic() {
        var source = Svg();
        var ordinary = SvgRasterizer.Rasterize(source);
        var explicitNull = SvgRasterizer.Rasterize(source, filters: null);
        Capture("finite-composite-null-control", source, ordinary);
        Assert.Equal(ordinary.Image.Pixels, explicitNull.Image.Pixels);
        var warning = Assert.Single(ordinary.Diagnostics);
        Assert.Equal("SFR002", warning.Code);
        Assert.Equal("source", warning.ElementId);
        Assert.Contains("SFR002", Assert.Throws<NotSupportedException>(() => SvgRasterizer.Rasterize(source, strict: true)).Message);
    }

    [Theory]
    [InlineData("operator='arithmetic' k1='1'")]
    [InlineData("operator='invalid'")]
    [InlineData("operator='in' k4='0.5'")]
    public void UnsupportedOperatorsAndArithmeticRemainLocatedStrictDiagnostics(string attributes) {
        var source = Svg().Replace("operator='in'", attributes, StringComparison.Ordinal);
        var result = SvgRasterizer.Rasterize(source, filters: new SvgRasterFilterOptions());
        Capture("finite-composite-reject-" + attributes.Split('=')[0], source, result);
        Assert.Contains(result.Diagnostics, item => item.Code == "SFR002" && item.ElementId == "source");
        Assert.Throws<NotSupportedException>(() => SvgRasterizer.Rasterize(source, strict: true, filters: new SvgRasterFilterOptions()));
    }

    [Fact]
    public void FloodAndColorSpaceResolveThroughActualDefinitionAncestry() {
        var explicitSource = Svg();
        var inherited = explicitSource.Replace("<defs>", "<style>.flood-paint{flood-color:#0000ff;flood-opacity:0.5019607843137255}</style><defs>", StringComparison.Ordinal)
            .Replace("<filter id='composite'", "<g style='color-interpolation-filters:sRGB'><filter id='composite'", StringComparison.Ordinal)
            .Replace(" color-interpolation-filters='sRGB'", "", StringComparison.Ordinal)
            .Replace(" flood-color='#0000ff' flood-opacity='0.5019607843137255'", " class='flood-paint'", StringComparison.Ordinal)
            .Replace("</filter>", "</filter></g>", StringComparison.Ordinal);
        var expected = SvgRasterizer.Rasterize(explicitSource, strict: true, filters: new SvgRasterFilterOptions());
        var result = SvgRasterizer.Rasterize(inherited, strict: true, filters: new SvgRasterFilterOptions());
        Capture("finite-composite-inherited-style", inherited, result);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(expected.Image.Pixels, result.Image.Pixels);
        var unsupported = inherited.Replace("color-interpolation-filters:sRGB", "color-interpolation-filters:linearRGB", StringComparison.Ordinal);
        Assert.Contains(SvgRasterizer.Rasterize(unsupported, filters: new SvgRasterFilterOptions()).Diagnostics, item => item.Code == "SFR002");
    }

    [Fact]
    public void ReusedInstancesShareApplicationBudgetsAndCancellationWithoutOrdinaryFallback() {
        var single = Svg();
        var result = SvgRasterizer.Rasterize(single, strict: true, filters: new SvgRasterFilterOptions());
        Capture("finite-composite-budget-control", single, result);
        var repeated = single.Replace("</svg>", "<use href='#source' x='0' y='0'/></svg>", StringComparison.Ordinal);
        Assert.Throws<NotSupportedException>(() => SvgRasterizer.Rasterize(repeated, filters: new SvgRasterFilterOptions(maximumApplications: 1)));
        Assert.Throws<NotSupportedException>(() => SvgRasterizer.Rasterize(single, filters: new SvgRasterFilterOptions(maximumLiveBytes: 1)));
        Assert.Throws<OperationCanceledException>(() => SvgRasterizer.Rasterize(single,
            filters: new SvgRasterFilterOptions(cancellationToken: new CancellationToken(true))));
        Assert.Empty(result.Diagnostics);
    }

    internal static string Svg(string? body = null, string extraDefinitions = "") =>
        "<svg xmlns='http://www.w3.org/2000/svg' width='32' height='32'><defs>" + extraDefinitions
        + "<filter id='composite' filterUnits='userSpaceOnUse' primitiveUnits='userSpaceOnUse' x='0' y='0' width='32' height='32' color-interpolation-filters='sRGB'>"
        + "<feFlood flood-color='#0000ff' flood-opacity='0.5019607843137255' result='backdrop'/>"
        + "<feComposite in='SourceGraphic' in2='backdrop' operator='in'/></filter></defs>"
        + (body ?? "<rect id='source' x='8' y='8' width='16' height='16' fill='red' fill-opacity='0.5019607843137255' filter='url(#composite)'/>") + "</svg>";

    private static void Capture(string name, string source, SvgRasterizationResult result) {
        var output = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, name + ".svg"), source);
        File.WriteAllBytes(Path.Combine(output, name + ".native.png"), PngWriter.WriteRgba(result.Image));
        File.WriteAllText(Path.Combine(output, name + ".diagnostics.txt"), string.Join("\n", result.Diagnostics.Select(item => item.Code + ":" + item.ElementId)));
    }
}
