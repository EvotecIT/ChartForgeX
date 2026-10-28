using System.Text;
using ChartForgeX.Raster;
using ChartForgeX.SvgRaster;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Public import workflows protect the cumulative declared compositor-array ceiling.</summary>
public sealed class SvgRasterGaussianCompositorBudgetTests {
    [Theory]
    [InlineData("root", 160000L)]
    [InlineData("ancestors", 190000L)]
    [InlineData("viewports", 180000L)]
    [InlineData("embedded", 310000L)]
    [InlineData("references", 300000L)]
    public void OrdinarySurroundingSurfacesCannotEscapeTheExplicitCumulativeBudget(string scenario, long limit) {
        var source = Source(scenario); var accepted = Import(source);
        Assert.Empty(accepted.Diagnostics); Assert.Equal(64, accepted.Image.Width); Assert.Equal(64, accepted.Image.Height);
        Assert.True(accepted.Image.Pixels[(16 * 64 + 16) * 4 + 3] > 0);
        Capture("gaussian-budget-" + scenario + "-generous", source, accepted);
        var error = Assert.Throws<NotSupportedException>(() => Import(source, limit));
        Assert.Contains("SVG filter safety limit:", error.Message);
    }

    [Theory]
    [InlineData("root", 173136L)]
    [InlineData("ancestors", 205904L)]
    [InlineData("viewports", 189520L)]
    [InlineData("embedded", 320592L)]
    [InlineData("references", 395424L)]
    public void AConcreteWorkflowAtItsDeclaredArrayLimitRetainsEveryPixelAndOneByteLessRejects(string scenario, long exactBytes) {
        var source = Source(scenario); var generous = Import(source); var exact = Import(source, exactBytes);
        Assert.Empty(generous.Diagnostics); Assert.Empty(exact.Diagnostics);
        Assert.Equal(generous.Image.Pixels, exact.Image.Pixels);
        Assert.Equal(64 * 64 * 4, exact.Image.Pixels.Length);
        Assert.True(exact.Image.Pixels[(16 * 64 + 16) * 4 + 3] > 0);
        if (scenario == "root") Assert.Equal(14, exact.Image.Pixels[(16 * 64 + 16) * 4 + 3]);
        if (scenario == "viewports") Assert.Equal(28, exact.Image.Pixels[(16 * 64 + 16) * 4 + 3]);
        Capture("gaussian-budget-" + scenario + "-exact", source, exact);
        Assert.Contains("SVG filter safety limit:", Assert.Throws<NotSupportedException>(() => Import(source, exactBytes - 1)).Message);
    }

    [Theory]
    [InlineData("root")] [InlineData("ancestors")] [InlineData("viewports")] [InlineData("embedded")] [InlineData("references")]
    public void NullOptionsAndUnfilteredCompositorsKeepTheirPixelsAndStrictLossContract(string scenario) {
        var source = Source(scenario); var ordinary = SvgRasterizer.Rasterize(source);
        var explicitNull = SvgRasterizer.Rasterize(source, filters: null);
        Assert.Equal(ordinary.Image.Pixels, explicitNull.Image.Pixels);
        Assert.Contains(ordinary.Diagnostics, warning => warning.Code == "SFR002");
        Assert.Equal(ordinary.Diagnostics.Select(item => (item.Code, item.ElementId)), explicitNull.Diagnostics.Select(item => (item.Code, item.ElementId)));
        Assert.Contains("SFR002", Assert.Throws<NotSupportedException>(() => SvgRasterizer.Rasterize(source, strict: true, filters: null)).Message);
        var unfiltered = Source(scenario, false);
        var plain = SvgRasterizer.Rasterize(unfiltered, strict: true);
        var counted = Import(unfiltered);
        Assert.Empty(plain.Diagnostics); Assert.Empty(counted.Diagnostics); Assert.Equal(plain.Image.Pixels, counted.Image.Pixels);
    }

    private static SvgRasterizationResult Import(string source, long maximumBytes = 256L * 1024 * 1024) =>
        SvgRasterizer.Rasterize(source, strict: true, filters: new SvgRasterFilterOptions(maximumLiveBytes: maximumBytes));
    private static string Source(string scenario, bool filtered = true) {
        if (scenario == "root") return filtered ? ReviewedRoot : ReviewedRoot.Replace(" filter=\"url(#blur)\"", "");
        var impulse = "<rect x='16' y='16' width='1' height='1' fill='red'" + (filtered ? " filter='url(#blur)'" : "") + "/>";
        if (scenario == "ancestors") return Svg(Filter + OpacityAncestors(impulse, 5));
        if (scenario == "viewports") {
            for (var index = 0; index < 3; index++) impulse = "<svg width='64' height='64' viewBox='0 0 64 64'>" + impulse + "</svg>";
            return Svg(Filter + impulse);
        }
        if (scenario == "embedded") {
            var inner = Svg(Filter + OpacityAncestors(impulse, 7));
            var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(inner));
            return Svg("<image width='64' height='64' opacity='.8' href='data:image/svg+xml;base64," + payload + "'/>");
        }
        if (scenario == "references") return Svg(Filter + "<defs><g id='payload'>" + OpacityAncestors(impulse, 5)
            + "</g></defs><use href='#payload'/><use href='#payload'/>");
        throw new ArgumentOutOfRangeException(nameof(scenario));
    }
    private static string OpacityAncestors(string source, int count) {
        for (var index = 0; index < count; index++) source = "<g opacity='.9'>" + source + "</g>";
        return source;
    }
    private static string Svg(string body) => "<svg xmlns='http://www.w3.org/2000/svg' width='64' height='64' viewBox='0 0 64 64'>" + body + "</svg>";
    private const string Filter = "<defs><filter id='blur' filterUnits='userSpaceOnUse' primitiveUnits='userSpaceOnUse' x='8' y='8' width='16' height='16' color-interpolation-filters='sRGB'>"
        + "<feGaussianBlur in='SourceGraphic' stdDeviation='1' edgeMode='none'/></filter></defs>";
    private const string ReviewedRoot = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"64\" height=\"64\" viewBox=\"0 0 64 64\" opacity=\".5\" clip-path=\"url(#rootclip)\" mask=\"url(#rootmask)\">\n  <defs>\n    <filter id=\"blur\" filterUnits=\"userSpaceOnUse\" primitiveUnits=\"userSpaceOnUse\" x=\"8\" y=\"8\" width=\"16\" height=\"16\" color-interpolation-filters=\"sRGB\"><feGaussianBlur in=\"SourceGraphic\" stdDeviation=\"1\" edgeMode=\"none\"/></filter>\n    <clipPath id=\"rootclip\" clipPathUnits=\"userSpaceOnUse\"><rect width=\"64\" height=\"64\"/></clipPath>\n    <mask id=\"rootmask\" maskUnits=\"userSpaceOnUse\" maskContentUnits=\"userSpaceOnUse\" x=\"0\" y=\"0\" width=\"64\" height=\"64\" mask-type=\"alpha\"><rect width=\"64\" height=\"64\" fill=\"white\"/></mask>\n  </defs>\n  <rect x=\"16\" y=\"16\" width=\"1\" height=\"1\" fill=\"red\" filter=\"url(#blur)\"/>\n</svg>\n";
    private static void Capture(string name, string source, SvgRasterizationResult result) {
        if (Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, name + ".source.svg"), source);
        File.WriteAllBytes(Path.Combine(output, name + ".native.png"), PngWriter.WriteRgba(result.Image.Width, result.Image.Height, result.Image.Pixels));
    }
}
