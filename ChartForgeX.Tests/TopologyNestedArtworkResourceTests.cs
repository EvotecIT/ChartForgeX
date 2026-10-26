using System.Text;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.SvgRaster;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyNestedArtworkResourceTests {
    [Theory] [InlineData(false)] [InlineData(true)]
    public void ExactRepeatedSvgCapturesShareImmutablePixelsWhileSamplingAspectAndViewBoxRemainInputs(bool dataSvg) {
        const string body = "<rect width='16' height='32' fill='#DC1E28'/><rect x='16' width='16' height='32' fill='#1E28DC'/>";
        TopologyIconArtwork Artwork(string aspect = "xMidYMid meet", string viewBox = "0 0 32 32") =>
            (dataSvg ? TopologyIconArtwork.Image(Svg(body, viewBox)) : TopologyIconArtwork.InlineSvg(body, viewBox)).WithPreserveAspectRatio(aspect);
        var chart = Chart(Artwork(), Artwork(), Artwork("none"), Artwork(viewBox: "0 0 64 32"), Artwork());
        chart.Nodes[4].Width = 48;
        var prepared = chart.Prepare(Options());
        var images = prepared.Visual.Scene.Nodes.OfType<VisualSceneImage>().ToArray();
        Assert.Equal(5, images.Length); Assert.Same(images[0].Image.Pixels, images[1].Image.Pixels);
        Assert.NotSame(images[0].Image.Pixels, images[2].Image.Pixels);
        Assert.NotSame(images[0].Image.Pixels, images[3].Image.Pixels);
        Assert.NotSame(images[0].Image.Pixels, images[4].Image.Pixels);
        Assert.NotEqual(images[0].Image.Width, images[4].Image.Width);
        var png = prepared.ToPng(); var svg = prepared.ToSvg();
        chart.Nodes[0].Artwork!.SvgBody = "<rect width='32' height='32' fill='black'/>";
        chart.Nodes[0].Artwork!.ImageHref = "data:image/png;base64,AA==";
        Assert.Equal(png, prepared.ToPng()); Assert.Equal(svg, prepared.ToSvg());
    }

    [Theory] [InlineData(false)] [InlineData(true)]
    public void TwoDifferentNestedResourcesWithEqualDimensionsChargeSeparatelyButOneExactHrefIsReused(bool dataSvg) {
        var same = Header(3000, 3000, "one");
        var repeated = Chart(Artwork(Images(same, same, same, same), dataSvg)).Prepare(Options());
        Assert.Single(repeated.Visual.Scene.Nodes.OfType<VisualSceneImage>());
        var distinct = Chart(Artwork(Images(same, Header(3000, 3000, "two"), Header(3000, 3000, "three"), Header(3000, 3000, "four")), dataSvg));
        var failure = Assert.Throws<VisualResourceLimitException>(() => distinct.Prepare(Options()));
        Assert.Equal("NativeRasterResourceBytes", failure.Limit); Assert.Equal("image-0", failure.ItemId);
        Assert.Equal(RasterEffectPreflight.MaximumLiveBytes, failure.Maximum); Assert.True(failure.Observed > failure.Maximum);
    }

    [Theory]
    [MemberData(nameof(RasterDecodeLimitTests.TwoPixelInputs), MemberType = typeof(RasterDecodeLimitTests))]
    public void AllSixReadersRetainTheirExistingEarlyAdmissionUnderTheSharedCompilationDebit(byte[] encoded) {
        var mime = RasterImageDecoder.MimeTypeFor(encoded);
        var chart = Chart(TopologyIconArtwork.Image(Header(1, 33554431, "leave-eight-bytes")),
            TopologyIconArtwork.Image("data:" + mime + ";base64," + Convert.ToBase64String(encoded)));
        var failure = Assert.Throws<VisualResourceLimitException>(() => chart.Prepare(Options()));
        Assert.Equal("NativeRasterResourceBytes", failure.Limit); Assert.Equal("image-1", failure.ItemId);
        Assert.Equal(RasterEffectPreflight.MaximumLiveBytes + 8, failure.Observed);
    }

    [Theory]
    [InlineData("<g opacity='.5'><rect width='32' height='32' fill='#24A060'/></g>")]
    [InlineData("<defs><mask id='m'><rect width='32' height='32' fill='white'/></mask></defs><rect width='32' height='32' fill='#24A060' mask='url(#m)'/>")]
    [InlineData("<text x='2' y='20' font-size='16' fill='#24A060' stroke='black' stroke-width='2'>A</text>")]
    public void ExistingOpacityMaskAndTextIntermediatesJoinTheSamePriorDeclaredDebit(string body) {
        var chart = Chart(TopologyIconArtwork.Image(Header(4096, 8191, "leave-32768-bytes")), TopologyIconArtwork.InlineSvg(body, "0 0 32 32"));
        var failure = Assert.Throws<VisualResourceLimitException>(() => chart.Prepare(Options()));
        Assert.Equal("NativeRasterResourceBytes", failure.Limit); Assert.Equal("image-1", failure.ItemId);
        Assert.True(failure.Observed > failure.Maximum);
    }

    [Fact]
    public void ARepeatedRasterStillPaysItsRealNestedImageBoxAndOrdinarySvgCopy() {
        var tiny = "data:image/x-portable-pixmap;base64," + Convert.ToBase64String(Encoding.ASCII.GetBytes("P6\n1 1\n255\nRGB"));
        var chart = Chart(TopologyIconArtwork.Image(Header(4096, 8191, "leave-32768-bytes")),
            TopologyIconArtwork.InlineSvg(Images(tiny), "0 0 32 32"));
        var failure = Assert.Throws<VisualResourceLimitException>(() => chart.Prepare(Options()));
        Assert.Equal("NativeRasterResourceBytes", failure.Limit); Assert.Equal("image-1", failure.ItemId);
    }

    [Fact]
    public void CacheLookupNeverBypassesTheActualImageDepthGuardOrReusesAShallowerResult() {
        var leaf = Svg("<rect width='32' height='32' fill='#DC1E28'/>");
        string Wrap(string child) => Svg("<rect width='32' height='32' fill='#24A060'/><image width='32' height='32' href='" + child + "'/>");
        var admitted = Wrap(Wrap(Wrap(leaf))); // Leaf is encountered at depth three.
        var rejected = Wrap(admitted); // Same leaf at depth four must retain the green fallback.
        var prepared = Chart(TopologyIconArtwork.Image(leaf), TopologyIconArtwork.Image(admitted), TopologyIconArtwork.Image(rejected)).Prepare(Options());
        var images = prepared.Visual.Scene.Nodes.OfType<VisualSceneImage>().ToArray();
        Assert.Equal(3, images.Length);
        Assert.Equal(new byte[] { 220, 30, 40, 255 }, Pixel(images[0].Image));
        Assert.Equal(new byte[] { 220, 30, 40, 255 }, Pixel(images[1].Image));
        Assert.Equal(new byte[] { 36, 160, 96, 255 }, Pixel(images[2].Image));
    }

    [Fact]
    public void ScopeAttachmentKeepsDefaultFilterOptInMalformedFallbackAndExternalImagePolicy() {
        const string body = "<defs><filter id='f'><feGaussianBlur stdDeviation='2'/></filter></defs>"
            + "<rect x='8' y='8' width='16' height='16' fill='#24A060' filter='url(#f)'/>"
            + "<image width='32' height='32' href='data:image/png;base64,AA=='/>"
            + "<image width='32' height='32' href='https://example.invalid/never-fetched'/>";
        var prepared = Chart(TopologyIconArtwork.Image(Svg(body))).Prepare(Options());
        var image = Assert.Single(prepared.Visual.Scene.Nodes.OfType<VisualSceneImage>()).Image;
        var standalone = SvgRasterizer.ToImage(Document(body), image.Width, image.Height);
        Assert.Equal(standalone.Pixels, image.Pixels);
    }

    [Fact]
    public void PortableNativeArtworkUsesTheSameResourceOwnerAfterPublicJsonRestoration() {
        var ordinary = Chart(TopologyIconArtwork.InlineSvg("<rect width='32' height='32' fill='#24A060'/>", "0 0 32 32"));
        var envelope = ordinary.ToVisualArtifact().ToInterchangeEnvelope();
        envelope.Nodes[0].Topology!.Artwork!.SvgBody = Images(Header(6000, 6000, "nested-portable-limit"));
        var restored = VisualArtifactInterchangeEnvelope.FromJson(envelope.ToJson()).ToTopologyChart();
        var failure = Assert.Throws<VisualResourceLimitException>(() => restored.Prepare(Options()));
        Assert.Equal("NativeRasterResourceBytes", failure.Limit); Assert.Equal("image-0", failure.ItemId);
    }

    private static byte[] Pixel(RgbaImage image) => image.Pixels.Skip(((image.Height / 2) * image.Width + image.Width / 2) * 4).Take(4).ToArray();
    private static TopologyIconArtwork Artwork(string body, bool dataSvg) => dataSvg ? TopologyIconArtwork.Image(Svg(body)) : TopologyIconArtwork.InlineSvg(body, "0 0 32 32");
    private static string Images(params string[] hrefs) => "<rect width='32' height='32' fill='#24A060'/>" +
        string.Join("", hrefs.Select(href => "<image width='32' height='32' href='" + href + "'/>"));
    private static string Header(int width, int height, string comment) => "data:image/x-portable-pixmap;base64," +
        Convert.ToBase64String(Encoding.ASCII.GetBytes("P6\n# " + comment + "\n" + width + " " + height + "\n255\n"));
    private static string Document(string body, string viewBox = "0 0 32 32") => "<svg xmlns='http://www.w3.org/2000/svg' viewBox='" + viewBox + "'>" + body + "</svg>";
    private static string Svg(string body, string viewBox = "0 0 32 32") => "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(Document(body, viewBox)));
    private static TopologyChart Chart(params TopologyIconArtwork[] artwork) {
        var chart = TopologyChart.Create().WithTitle("").WithViewport(400, 180, 8).WithLegend(null); chart.LayoutMode = TopologyLayoutMode.Manual;
        for (var index = 0; index < artwork.Length; index++) chart.Nodes.Add(new TopologyNode {
            Id = "image-" + index, Label = "Image", X = 16 + index * 60, Y = 30, Width = 32, Height = 32,
            DisplayMode = TopologyNodeDisplayMode.Artwork, Artwork = artwork[index], ShowLabel = false, ShowStatusBadge = false, PreserveDisplayModeSize = true
        });
        return chart;
    }
    private static TopologyRenderOptions Options() => new() { FitContentToViewport = false, IncludeLegend = false, IncludeTitle = false };
}
