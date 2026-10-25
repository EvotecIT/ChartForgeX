using System.Threading;
using System.Xml.Linq;
using ChartForgeX.Composition;
using ChartForgeX.Raster;
using ChartForgeX.SvgRaster;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Consequential contracts for the explicitly opted-in finite Gaussian subset; declarations are not qualification results.</summary>
public sealed class SvgRasterGaussianFilterTests {
    [Fact]
    public void NullOptionsRetainOrdinaryPixelsAndStrictUnsupportedFilterDiagnostics() {
        var svg = Svg(Filter() + Impulse());
        var ordinary = SvgRasterizer.Rasterize(svg);
        Assert.Equal(ordinary.Image.Pixels, SvgRasterizer.Rasterize(svg, filters: null).Image.Pixels);
        var warning = Assert.Single(ordinary.Diagnostics); Assert.Equal("SFR002", warning.Code); Assert.Equal("source", warning.ElementId);
        Assert.Equal(new byte[] { 255, 0, 0, 255 }, Pixel(ordinary.Image, 16, 16));
        Assert.Equal(new byte[4], Pixel(ordinary.Image, 15, 16));
        Assert.Contains("SFR002", Assert.Throws<NotSupportedException>(() => SvgRasterizer.Rasterize(svg, strict: true)).Message);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void OnePixelSourceHasExactSharedKernelSupportWithoutImplicitBackdrop(bool group) {
        var source = group ? "<g id='source' filter='url(#blur)'><rect x='16' y='16' width='1' height='1' fill='#ff0000'/></g>" : Impulse();
        var svg = Svg(Filter() + source);
        var result = Import(svg); Assert.Empty(result.Diagnostics);
        Capture(group ? "gaussian-group-impulse" : "gaussian-direct-impulse", svg, result);
        for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
            Assert.Equal(x is >= 15 and <= 17 && y is >= 15 and <= 17 ? new byte[] { 255, 0, 0, 28 } : new byte[4], Pixel(result.Image, x, y));
        Assert.Equal(32, result.Image.Width); Assert.Equal(32, result.Image.Height);
    }

    [Fact]
    public void WholeSourceGraphicCombinesMarksAndTextBeforeOneSharedKernelOperation() {
        var body = "<rect x='8' y='10' width='9' height='8' fill='#2166ac'/><text x='17' y='19' font-size='7' fill='#b2182b'>FX</text>";
        var unfiltered = SvgRasterizer.ToImage(Svg("<g>" + body + "</g>"));
        var actual = Import(Svg(Filter("2") + "<g filter='url(#blur)'>" + body + "</g>"));
        Assert.Empty(actual.Diagnostics);
        Capture("gaussian-whole-source-actual", Svg(Filter("2") + "<g filter='url(#blur)'>" + body + "</g>"), actual);
        CaptureImage("gaussian-whole-source-expected", Svg("<g>" + body + "</g>"), BlurViewport(unfiltered, 512, 512));
        Assert.Equal(BlurViewport(unfiltered, 512, 512).Pixels, actual.Image.Pixels);
        Assert.NotEqual(unfiltered.Pixels, actual.Image.Pixels);
    }

    [Fact]
    public void FilterThenOwnClipThenOwnMaskThenOwnOpacityUsesTheExistingCompositor() {
        var definitions = Filter() + "<defs><clipPath id='clip' clipPathUnits='userSpaceOnUse'><rect x='16' y='0' width='16' height='32'/></clipPath>"
            + "<mask id='mask' maskUnits='userSpaceOnUse' maskContentUnits='userSpaceOnUse' x='0' y='0' width='32' height='32' mask-type='alpha'>"
            + "<rect x='0' y='16' width='32' height='16' fill='white'/></mask></defs>";
        var source = Impulse("clip-path='url(#clip)' mask='url(#mask)' opacity='.5'");
        var result = Import(Svg(definitions + source)); Assert.Empty(result.Diagnostics);
        for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
            Assert.Equal(x is >= 16 and <= 17 && y is >= 16 and <= 17 ? new byte[] { 255, 0, 0, 14 } : new byte[4], Pixel(result.Image, x, y));
    }

    [Fact]
    public void SourceOutsideRootViewportStillContributesItsSupportedBlurIntoTheViewport() {
        var result = Import(Svg(Filter("1", "-4", "-4", "40", "40")
            + "<rect filter='url(#blur)' x='-1' y='16' width='1' height='1' fill='red'/>"));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(new byte[] { 255, 0, 0, 28 }, Pixel(result.Image, 0, 16));
        Assert.Equal(new byte[] { 255, 0, 0, 28 }, Pixel(result.Image, 0, 15));
        Assert.Equal(new byte[4], Pixel(result.Image, 1, 16));
    }

    [Theory]
    [InlineData("0")] [InlineData("0.001")]
    public void RoundedZeroKeepsExactSourcePixelsWithoutEffectConversion(string sigma) {
        const string body = "<rect x='8' y='10' width='9' height='8' fill='#257bdb' fill-opacity='.678'/>";
        var expected = SvgRasterizer.ToImage(Svg(body));
        var actual = Import(Svg(Filter(sigma) + "<g filter='url(#blur)'>" + body + "</g>"));
        Assert.Empty(actual.Diagnostics); Assert.Equal(expected.Pixels, actual.Image.Pixels);
    }

    [Fact]
    public void AnisotropicPositiveScaleAndTranslationConvertSigmaExactlyOnce() {
        const string body = "<g transform='translate(4 4) scale(2 3)'><rect x='4' y='3' width='1' height='1' fill='red'/></g>";
        var expected = BlurViewport(SvgRasterizer.ToImage(Svg(body)), 512, 768);
        var filtered = body.Replace("<g transform=", "<g filter='url(#blur)' transform=");
        var actual = Import(Svg(Filter("1", "-2", "-1", "16", "9") + filtered));
        Capture("gaussian-anisotropic-actual", Svg(Filter("1", "-2", "-1", "16", "9") + filtered), actual);
        CaptureImage("gaussian-anisotropic-expected", Svg(body), expected);
        Assert.Empty(actual.Diagnostics); Assert.Equal(expected.Pixels, actual.Image.Pixels);
    }

    [Fact]
    public void PrimitiveComputedColorSpaceUsesCssCascadeVariablesAndRealDefinitionAncestry() {
        var valid = Svg("<style>defs .srgb{color-interpolation-filters:var(--space)}</style><defs style='--space:sRGB'>"
            + "<filter id='blur' filterUnits='userSpaceOnUse' primitiveUnits='userSpaceOnUse' color-interpolation-filters='linearRGB' x='0' y='0' width='32' height='32'>"
            + "<feGaussianBlur class='srgb' in='SourceGraphic' stdDeviation='1' edgeMode='none'/></filter></defs>" + Impulse());
        Assert.Empty(Import(valid).Diagnostics);
        var rejected = valid.Replace("color-interpolation-filters:var(--space)", "color-interpolation-filters:linearRGB!important");
        Assert.Contains("SFR002", Assert.Throws<NotSupportedException>(() => Import(rejected)).Message);
    }

    [Theory]
    [InlineData("filterUnits='objectBoundingBox'", "filterUnits='userSpaceOnUse'", true)]
    [InlineData("primitiveUnits='objectBoundingBox'", "primitiveUnits='userSpaceOnUse'", true)]
    [InlineData("color-interpolation-filters='linearRGB'", "color-interpolation-filters='sRGB'", true)]
    [InlineData("", "color-interpolation-filters='sRGB'", true)]
    [InlineData("stdDeviation='129'", "stdDeviation='1'", true)]
    [InlineData("stdDeviation='-1'", "stdDeviation='1'", true)]
    [InlineData("stdDeviation='NaN'", "stdDeviation='1'", true)]
    [InlineData("stdDeviation='1%'", "stdDeviation='1'", true)]
    [InlineData("in='SourceAlpha'", "in='SourceGraphic'", true)]
    [InlineData("edgeMode='duplicate'", "edgeMode='none'", true)]
    [InlineData("x='0.5'", "x='0'", true)]
    [InlineData("width='100%'", "width='32'", true)]
    [InlineData("result='named' in='SourceGraphic'", "in='SourceGraphic'", true)]
    [InlineData("x='0' in='SourceGraphic'", "in='SourceGraphic'", true)]
    [InlineData("feOffset", "feGaussianBlur", true)]
    [InlineData("filter='blur(1px)'", "filter='url(#blur)'", false)]
    [InlineData("filter='url(#blur) url(#blur)'", "filter='url(#blur)'", false)]
    [InlineData("transform='rotate(15)' filter='url(#blur)'", "filter='url(#blur)'", false)]
    [InlineData("transform='scale(-1 1)' filter='url(#blur)'", "filter='url(#blur)'", false)]
    public void UnsupportedPrimitiveColorSpaceUnitsAndTransformsRemainStrictlyRejected(string replacement, string original, bool definition) {
        var filter = definition ? Filter().Replace(original, replacement) : Filter();
        var source = definition ? Impulse() : Impulse().Replace(original, replacement);
        var svg = Svg(filter + source);
        var loose = SvgRasterizer.Rasterize(svg, filters: new SvgRasterFilterOptions());
        Assert.Contains(loose.Diagnostics, warning => warning.Code == "SFR002");
        Assert.Contains("SFR002", Assert.Throws<NotSupportedException>(() => Import(svg)).Message);
    }

    [Theory]
    [InlineData("<g filter='url(#blur)'><rect transform='translate(0 0)' x='16' y='16' width='1' height='1'/></g>")]
    [InlineData("<g filter='url(#blur)'><rect filter='url(#blur)' x='16' y='16' width='1' height='1'/></g>")]
    [InlineData("<g filter='url(#blur)'><svg width='32' height='32'><rect x='16' y='16' width='1' height='1'/></svg></g>")]
    [InlineData("<rect filter='url(#blur)' x='16' y='16' width='1' height='1' fill='url(#paint)'/>")]
    public void UnsupportedNestedSourceGraphsRetainLocatedLossInsteadOfClaimingFilteredOutput(string body) {
        var svg = Svg(Filter() + body);
        Assert.Contains(SvgRasterizer.Rasterize(svg, filters: new SvgRasterFilterOptions()).Diagnostics, item => item.Code == "SFR002");
        Assert.Throws<NotSupportedException>(() => Import(svg));
    }

    [Fact]
    public void SourcePaintCrossingTheExplicitRegionStaysDiagnosedUntilEdgeSemanticsQualify() {
        var svg = Svg(Filter("1", "8", "8", "16", "16") + "<rect id='edge' filter='url(#blur)' x='7' y='12' width='4' height='4' fill='red'/>");
        var result = SvgRasterizer.Rasterize(svg, filters: new SvgRasterFilterOptions());
        var warning = Assert.Single(result.Diagnostics); Assert.Equal("SFR002", warning.Code); Assert.Equal("edge", warning.ElementId);
        Assert.Contains("outside the explicit filter region", warning.Message);
        Assert.Throws<NotSupportedException>(() => Import(svg));
    }

    [Fact]
    public void UseInstancesShareTheDocumentApplicationLimitInsteadOfResettingPerDefinition() {
        var svg = Svg(Filter() + "<defs>" + Impulse().Replace("id='source'", "id='reused'") + "</defs><use href='#reused'/><use href='#reused'/>");
        Assert.Empty(SvgRasterizer.Rasterize(svg, strict: true, filters: new SvgRasterFilterOptions(maximumApplications: 2)).Diagnostics);
        var failure = Assert.Throws<NotSupportedException>(() => SvgRasterizer.Rasterize(svg, strict: true, filters: new SvgRasterFilterOptions(maximumApplications: 1)));
        Assert.Contains("instance count", failure.Message);
    }

    [Fact]
    public void EmbeddedSvgDocumentsShareTheSameFilterBudgetAndRetainUnsupportedLoss() {
        var nested = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(Svg(Filter() + Impulse())));
        var image = "<image width='32' height='32' href='data:image/svg+xml;base64," + nested + "'/>";
        var source = Svg(image + image);
        Assert.Empty(SvgRasterizer.Rasterize(source, strict: true, filters: new SvgRasterFilterOptions(maximumApplications: 2)).Diagnostics);
        Assert.Contains("instance count", Assert.Throws<NotSupportedException>(() => SvgRasterizer.Rasterize(source, strict: true,
            filters: new SvgRasterFilterOptions(maximumApplications: 1))).Message);
        Assert.Contains("SFR002", Assert.Throws<NotSupportedException>(() => SvgRasterizer.Rasterize(source, strict: true)).Message);
    }

    [Fact]
    public void TightenedWorkAndBufferLimitsFailHardBeforeAnUnfilteredImageCanBeReturned() {
        var svg = Svg(Filter() + Impulse());
        Assert.Contains("safety limit", Assert.Throws<NotSupportedException>(() => SvgRasterizer.Rasterize(svg,
            filters: new SvgRasterFilterOptions(maximumLiveBytes: 1))).Message);
        Assert.Contains("safety limit", Assert.Throws<NotSupportedException>(() => SvgRasterizer.Rasterize(svg,
            filters: new SvgRasterFilterOptions(maximumWorkSamples: 1))).Message);
        Assert.Throws<ArgumentOutOfRangeException>(() => new SvgRasterFilterOptions(maximumLiveBytes: 256L * 1024 * 1024 + 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SvgRasterFilterOptions(maximumApplications: 17));
    }

    [Fact]
    public void CancellationUsesTheExistingKernelAndDoesNotPublishOrMutateBorrowedPixels() {
        var svg = Svg(Filter() + Impulse());
        Assert.Throws<OperationCanceledException>(() => SvgRasterizer.Rasterize(svg,
            filters: new SvgRasterFilterOptions(cancellationToken: new CancellationToken(true))));
        using var cancellation = new CancellationTokenSource();
        var context = new SvgRasterFilterContext(new SvgRasterFilterOptions(cancellationToken: cancellation.Token));
        var source = new RgbaImage(1, 1, new byte[] { 255, 0, 0, 255 }); var before = source.Pixels.ToArray();
        context.ReserveCanvas(1, 1);
        var plan = RasterEffectPlan.Blur(new RasterEffectRegion(0, 0, 1, 1), 256, 256);
        var permit = context.Reserve(plan, 1, 1, 1, 1); cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => permit.Execute(source)); Assert.Equal(before, source.Pixels);
    }

    [Fact]
    public void ComputedPixelsCanUseTheExistingSelfContainedSnapshotWithoutInventingLiveSvgFilters() {
        var image = Import(Svg(Filter() + Impulse())).Image; var before = image.Pixels.ToArray();
        var canvas = VisualCanvas.Create(32, 32).WithPngOutputScale(1).WithBackdrop(VisualCanvasBackdropStyle.Transparent).AddImage(image, 0, 0, 32, 32);
        var svg = canvas.ToSvg(); var png = canvas.ToPng(); var document = XDocument.Parse(svg);
        // VisualCanvas retains its ordinary unused decoration definitions; no filter is applied to this snapshot.
        Assert.DoesNotContain(document.Descendants(), element => element.Name.LocalName == "script" || element.Attribute("filter") != null);
        var href = Assert.Single(document.Descendants(), element => element.Name.LocalName == "image").Attribute("href")!.Value;
        Assert.StartsWith("data:image/png;base64,", href);
        Assert.Equal(before, RasterImageDecoder.Decode(Convert.FromBase64String(href.Substring(href.IndexOf(',') + 1))).Pixels);
        Assert.Empty(SvgRasterizer.Rasterize(svg, strict: true).Diagnostics);
        Assert.Equal(32, RasterImageDecoder.Decode(png).Width);
        Array.Clear(image.Pixels, 0, image.Pixels.Length); Assert.Equal(svg, canvas.ToSvg()); Assert.Equal(png, canvas.ToPng());
    }

    [Fact]
    public void RootSymbolAndBoundingBoxMaskScopesRemainStrictlyUnsupported() {
        var body = "<rect x='16' y='16' width='1' height='1' fill='red'/>";
        var root = Svg(Filter() + body).Replace("viewBox='0 0 32 32'", "viewBox='0 0 32 32' filter='url(#blur)'");
        var symbol = Svg(Filter() + "<defs><symbol id='icon' viewBox='0 0 32 32' filter='url(#blur)'>" + body + "</symbol></defs><use href='#icon' width='32' height='32'/>");
        var masked = Svg(Filter() + "<defs><mask id='mask' maskUnits='objectBoundingBox'><rect width='32' height='32' fill='white'/></mask></defs>" + Impulse("mask='url(#mask)'"));
        foreach (var source in new[] { root, symbol, masked })
            Assert.Contains("SFR002", Assert.Throws<NotSupportedException>(() => Import(source)).Message);
    }

    private static void Capture(string stem, string source, SvgRasterizationResult result) => CaptureImage(stem, source, result.Image);

    private static void CaptureImage(string stem, string source, RgbaImage image) {
        var directory = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, stem + ".source.svg"), source);
        File.WriteAllBytes(Path.Combine(directory, stem + ".native.png"), PngWriter.WriteRgba(image.Width, image.Height, image.Pixels));
        File.WriteAllBytes(Path.Combine(directory, stem + ".rgba"), image.Pixels);
    }

    private static SvgRasterizationResult Import(string svg) => SvgRasterizer.Rasterize(svg, strict: true, filters: new SvgRasterFilterOptions());
    private static string Svg(string body) => "<svg xmlns='http://www.w3.org/2000/svg' width='32' height='32' viewBox='0 0 32 32'>" + body + "</svg>";
    private static string Filter(string sigma = "1", string x = "0", string y = "0", string width = "32", string height = "32") =>
        "<defs><filter id='blur' filterUnits='userSpaceOnUse' primitiveUnits='userSpaceOnUse' color-interpolation-filters='sRGB' x='" + x + "' y='" + y + "' width='" + width + "' height='" + height
        + "'><feGaussianBlur in='SourceGraphic' stdDeviation='" + sigma + "' edgeMode='none'/></filter></defs>";
    private static string Impulse(string extra = "") => "<rect id='source' filter='url(#blur)' x='16' y='16' width='1' height='1' fill='#ff0000' " + extra + "/>";
    private static byte[] Pixel(RgbaImage image, int x, int y) => image.Pixels.Skip((y * image.Width + x) * 4).Take(4).ToArray();
    private static RgbaImage BlurViewport(RgbaImage source, int xQ8, int yQ8) {
        var plan = RasterEffectPlan.Blur(new RasterEffectRegion(0, 0, source.Width, source.Height), xQ8, yQ8);
        var effect = RasterEffects.Execute(RasterEffectPreflight.Prepare(new[] { plan }, new[] { source }), 0, false, CancellationToken.None);
        var result = new byte[source.Pixels.Length];
        for (int y = 0; y < source.Height; y++) Buffer.BlockCopy(effect.Image.Pixels,
            ((y - effect.Region.Y) * effect.Image.Width - effect.Region.X) * 4, result, y * source.Width * 4, source.Width * 4);
        // Import finishes with the incumbent source-over compositor, including its existing byte quantization.
        var composed = new RgbaCanvas(source.Width, source.Height, 1);
        composed.DrawImage(0, 0, source.Width, source.Height, result);
        return new RgbaImage(source.Width, source.Height, composed.Pixels);
    }
}
