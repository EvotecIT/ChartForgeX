using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NativeRasterResourceCaptureTests {
    [Theory]
    [InlineData("xMidYMid meet")]
    [InlineData("xMaxYMid slice")]
    [InlineData("none")]
    public void RepeatedExactRasterUrisReuseCapturedPixelsAndKeepCanonicalCrop(string aspect) {
        var pixels = new byte[] { 220,30,40,255, 30,40,220,128, 0,0,0,0, 30,180,40,255 };
        var caller = new RgbaImage(2, 2, pixels);
        var artwork = TopologyIconArtwork.Portrait(caller).WithPreserveAspectRatio(aspect);
        var chart = Chart(artwork, artwork);
        var prepared = chart.Prepare(Options());
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        Capture("resource-crop-" + aspect.Replace(' ', '-'), svg, png);
        Array.Clear(pixels, 0, pixels.Length);
        var images = prepared.Visual.Scene.Nodes.OfType<VisualSceneImage>().ToArray();
        Assert.Equal(2, images.Length); Assert.Same(images[0].Image.Pixels, images[1].Image.Pixels);
        Assert.Equal(16, VisualSceneRasterRenderer.BorrowedNativeImageBytes(prepared.Visual.Scene));
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        var expected = new VisualSceneBuilder(prepared.Visual.Size, new FontSpec());
        var capturedNodes = prepared.ToInterchangeEnvelope().Nodes;
        for (var index = 0; index < images.Length; index++) {
            var facts = capturedNodes[index];
            // Geometry comparisons below use the actual node viewport, including the incumbent clip owner.
            var bounds = new ChartRect(facts.X!.Value, facts.Y!.Value, facts.Width!.Value, facts.Height!.Value);
            expected.Image(new RgbaImage(2, 2, new byte[] { 220,30,40,255, 30,40,220,128, 0,0,0,0, 30,180,40,255 }), bounds, preserveAspectRatio: aspect);
        }
        var oldPlacementScene = expected.Build();
        var imageOnly = new VisualScene(prepared.Visual.Size,
            prepared.Visual.Scene.Nodes.Where(node => node is VisualSceneImage or VisualSceneGroup or VisualSceneEndGroup).ToArray(),
            Array.Empty<VisualDiagnostic>(), Array.Empty<VisualSemanticRegion>());
        Assert.Equal(VisualSceneRasterRenderer.Render(oldPlacementScene, supersampling: 1).Pixels,
            VisualSceneRasterRenderer.Render(imageOnly, supersampling: 1).Pixels);
        Assert.All(XDocument.Parse(svg).Descendants().Where(item => item.Name.LocalName == "image"), element =>
            Assert.Equal("data:image/png;base64," + Convert.ToBase64String(PngWriter.WriteRgba(images[0].Image)), (string?)element.Attribute("href")));
    }

    [Fact]
    public void DistinctPixelsStayIndependentWhileExactRepeatedUrisShareOneImmutableResource() {
        var red = TopologyIconArtwork.Portrait(new RgbaImage(1, 1, new byte[] { 220,30,40,255 }));
        var blue = TopologyIconArtwork.Portrait(new RgbaImage(1, 1, new byte[] { 30,40,220,255 }));
        var prepared = Chart(red, blue, red).Prepare(Options());
        Capture("distinct-and-repeated-resources", prepared.ToSvg(), prepared.ToPng());
        var images = prepared.Visual.Scene.Nodes.OfType<VisualSceneImage>().ToArray();
        Assert.Equal(3, images.Length); Assert.Same(images[0].Image.Pixels, images[2].Image.Pixels);
        Assert.NotSame(images[0].Image.Pixels, images[1].Image.Pixels);
        Assert.Equal(8, VisualSceneRasterRenderer.BorrowedNativeImageBytes(prepared.Visual.Scene));
        Assert.Equal(new byte[] { 220,30,40,255 }, images[0].Image.Pixels);
        Assert.Equal(new byte[] { 30,40,220,255 }, images[1].Image.Pixels);
    }

    [Fact]
    public void MalformedExactUrisAreCachedAndDistinctDeclaredPixelsRemainConservativelyBounded() {
        var first = Header(3000, 3000, "one");
        var ordinary = Chart(first, first, first, first).Prepare(Options());
        Capture("repeated-malformed-resource-control", ordinary.ToSvg(), ordinary.ToPng());
        var distinct = Chart(Header(3000, 3000, "one"), Header(3000, 3000, "two"), Header(3000, 3000, "three"), Header(3000, 3000, "four"));
        var failure = ObserveFailure("distinct-malformed-resource-limit", () => distinct.Prepare(Options()));
        Assert.Contains(ordinary.Visual.Diagnostics, item => item.Code == "topology.artwork-fallback");
        Assert.Empty(ordinary.Visual.Scene.Nodes.OfType<VisualSceneImage>());
        var error = Assert.IsType<VisualResourceLimitException>(failure);
        Assert.Equal("NativeRasterResourceBytes", error.Limit); Assert.Equal("image-3", error.ItemId);
        Assert.Equal(RasterEffectPreflight.MaximumLiveBytes, error.Maximum); Assert.True(error.Observed > error.Maximum);
    }

    [Fact]
    public void ExhaustedDeclaredPixelsStillDistinguishMalformedNoDimensionsFromARealNewRaster() {
        var full = Header(4096, 8192, "full-declared-allowance");
        var noDimensions = TopologyIconArtwork.Image("data:image/png;base64,AA==");
        var malformed = Chart(full, noDimensions).Prepare(Options());
        Capture("full-declared-malformed-control", malformed.ToSvg(), malformed.ToPng());
        var additional = ObserveFailure("full-declared-new-pixel-limit", () => Chart(full, Header(1, 1, "new-pixel")).Prepare(Options()));
        Assert.Empty(malformed.Visual.Scene.Nodes.OfType<VisualSceneImage>());
        Assert.Contains(malformed.Visual.Diagnostics, item => item.Code == "topology.artwork-fallback");
        var error = Assert.IsType<VisualResourceLimitException>(additional);
        Assert.Equal("image-1", error.ItemId); Assert.Equal(RasterEffectPreflight.MaximumLiveBytes + 8, error.Observed);
    }

    [Theory]
    [MemberData(nameof(RasterDecodeLimitTests.TwoPixelInputs), MemberType = typeof(RasterDecodeLimitTests))]
    public void EveryExistingReaderReportsThePixelLimitAtItsExistingPreallocationCheckpoint(byte[] encoded) {
        var limits = new RasterDecodeOptions { MaximumPixels = 1 };
        var observation = new RasterDecodeObservation();
        var decoded = RasterImageDecoder.TryDecodeObserved(encoded, limits, observation, out _);
        Write("reader-limit-" + RasterImageDecoder.MimeTypeFor(encoded).Replace('/', '-') + ".json",
            JsonSerializer.Serialize(new { decoded, observation.DeclaredPixels, observation.ObservedPixels, observation.PixelLimitExceeded }));
        Assert.False(decoded); Assert.True(observation.PixelLimitExceeded); Assert.Equal(2, observation.ObservedPixels); Assert.Equal(0, observation.DeclaredPixels);
        Assert.False(RasterImageDecoder.TryDecode(encoded, limits, out _));
        Assert.Equal(64 * 1024 * 1024, new RasterDecodeOptions().MaximumPixels);
    }

    [Fact]
    public void AnOversizedDeclaredRasterIsANativeLimitRatherThanASuccessfulGlyphFallback() {
        var failure = ObserveFailure("single-declared-resource-limit", () => Chart(Header(6000, 6000, "oversize")).Prepare(Options()));
        var error = Assert.IsType<VisualResourceLimitException>(failure);
        Assert.Equal("NativeRasterResourceBytes", error.Limit); Assert.Equal("image-0", error.ItemId);
        Assert.True(error.Observed > error.Maximum);
    }

    private static TopologyIconArtwork Header(int width, int height, string comment) => TopologyIconArtwork.Image(
        "data:image/x-portable-pixmap;base64," + Convert.ToBase64String(Encoding.ASCII.GetBytes("P6\n# " + comment + "\n" + width + " " + height + "\n255\n")));
    private static TopologyChart Chart(params TopologyIconArtwork[] images) {
        var chart = TopologyChart.Create().WithTitle("").WithViewport(400, 180, 8).WithLegend(null); chart.LayoutMode = TopologyLayoutMode.Manual;
        for (var index = 0; index < images.Length; index++) chart.Nodes.Add(new TopologyNode { Id = "image-" + index, Label = "Image", X = 16 + index * 80, Y = 30,
            Width = 60, Height = 40, DisplayMode = TopologyNodeDisplayMode.Artwork, Artwork = images[index], ShowLabel = false, ShowStatusBadge = false, PreserveDisplayModeSize = true });
        return chart;
    }
    private static TopologyRenderOptions Options() => new() { FitContentToViewport = false, IncludeLegend = false, IncludeTitle = false };
    private static Exception? ObserveFailure(string name, Action action) {
        try { action(); return null; } catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or NotSupportedException or OverflowException) {
            Write(name + ".error.json", JsonSerializer.Serialize(new { type = exception.GetType().FullName, exception.Message })); return exception;
        }
    }
    private static void Capture(string name, string svg, byte[] png) { Write(name + ".svg", svg); var output = Output(); if (output != null) File.WriteAllBytes(Path.Combine(output, name + ".png"), png); }
    private static void Write(string name, string text) { var output = Output(); if (output != null) File.WriteAllText(Path.Combine(output, name), text); }
    private static string? Output() { var output = Environment.GetEnvironmentVariable("CFX_RASTER_RESOURCE_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(output)) return null; Directory.CreateDirectory(output); return output; }
}
