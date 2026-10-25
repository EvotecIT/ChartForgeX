using System.Text;
using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NativeSvgRasterBudgetTests {
    [Theory]
    [InlineData(1)]
    [InlineData(16)]
    [InlineData(64)]
    public void BoundedImagePngKeepsOrdinaryBytesAndTheExactCompressedBoundary(int size) {
        var image = Noise(size, size); var ordinary = PngWriter.WriteRgba(image);
        var bounded = PngWriter.WriteRgbaBounded(image, ordinary.Length, Budget);
        var rejected = Observe("png-boundary-" + size, () => PngWriter.WriteRgbaBounded(image, ordinary.Length - 1, Budget));
        Write("png-boundary-" + size + ".png", bounded);
        Assert.Equal(ordinary, bounded); Assert.Equal(image.Pixels, RasterImageDecoder.Decode(bounded).Pixels);
        var error = Assert.IsType<VisualResourceLimitException>(rejected.Error);
        Assert.Equal("test-png-bytes", error.Limit); Assert.True(error.Observed > ordinary.Length - 1);
        Exception Budget(long observed) => new VisualResourceLimitException("test-png-bytes", ordinary.Length - 1, observed);
    }

    [Fact]
    public void JpegInSvgIsBoundedByItsActualPngExpansionAndARejectedAttemptDoesNotPoisonTheResource() {
        var jpeg = RasterImageEncoder.Encode(Noise(128, 128), RasterImageFormat.Jpeg);
        var decoded = RasterImageDecoder.Decode(jpeg); var resource = new VisualSceneImageData(decoded);
        var sourceUriCharacters = "data:image/jpeg;base64,".Length + (jpeg.Length + 2) / 3 * 4;
        var bounded = Observe("jpeg-png-expansion.bound", () => resource.BoundedDataUri(sourceUriCharacters,
            observed => new VisualResourceLimitException("MaximumOutputBytes", sourceUriCharacters, observed)));
        var ordinary = Observe("jpeg-png-expansion.uri", () => resource.DataUri);
        Write("jpeg-png-expansion.input.jpg", jpeg);
        Assert.IsType<VisualResourceLimitException>(bounded.Error); Assert.Null(ordinary.Error);
        Assert.True(ordinary.Value!.Length > sourceUriCharacters);
        var png = Convert.FromBase64String(ordinary.Value.Substring(ordinary.Value.IndexOf(',') + 1));
        Assert.Equal(decoded.Pixels, RasterImageDecoder.Decode(png).Pixels);
        Assert.Equal(ordinary.Value, resource.BoundedDataUri(ordinary.Value.Length, observed => new InvalidOperationException(observed.ToString())));
    }

    [Fact]
    public void ActualSourceSvgAndHtmlRejectRepeatedUrisWhileTheSamePngRequestRemainsAvailable() {
        var chart = Chart(TopologyIconArtwork.Portrait(Noise(64, 64)), 8);
        var source = "flowchart LR\n" + string.Join("\n", Enumerable.Range(0, 8).Select(index =>
            (index == 0 ? "image" : "image_" + index) + "@{img: 'key'}"));
        VisualArtifact SourceArtifact(MermaidFlowchartDocument model) {
            var value = model.ToVisualArtifact(new MermaidFlowchartRenderOptions { Width = 320, Height = 240, FitContent = false, Title = "" });
            value.Model = chart; value.NaturalSize = new VisualArtifactSize(320, 240); value.PreserveNaturalSize = true;
            return value;
        }
        var control = SourceArtifact(Assert.IsType<MermaidFlowchartDocument>(new MermaidParser().Parse(source).Document));
        var nativePng = control.ToPng(); var nativeSvg = control.ToSvg();
        var controlImage = RasterImageDecoder.Decode(nativePng);
        Assert.Equal(320, controlImage.Width); Assert.Equal(240, controlImage.Height);
        Write("source-output-budget.unbounded-control.png", nativePng);
        Write("source-output-budget.unbounded-control.svg", nativeSvg);
        var budget = (nativePng.Length + Encoding.UTF8.GetByteCount(nativeSvg)) / 2;
        var parsed = new MermaidParser().Parse(source, new MermaidParseOptions {
            ResourceLimits = new MermaidResourceLimits { MaximumOutputBytes = budget }
        });
        var document = parsed.Document as MermaidFlowchartDocument ?? throw new InvalidOperationException("The source control must parse.");
        Assert.DoesNotContain(parsed.Diagnostics, diagnostic => diagnostic.Severity == MermaidDiagnosticSeverity.Error);
        Assert.Equal(8, document.Nodes.Count);
        // The ordinary public host model replacement keeps this real source export policy; no new source binding API is needed here.
        var artifact = SourceArtifact(document);
        var svg = Observe("source-output-budget.svg", () => artifact.ToSvg());
        var html = Observe("source-output-budget.html", () => artifact.ToHtmlPage());
        var png = Observe("source-output-budget.png", () => artifact.ToPng());
        Write("source-output-budget.input.mmd", source);
        Write("source-output-budget.input.json", JsonSerializer.Serialize(new { budget, pngBytes = nativePng.Length, svgBytes = Encoding.UTF8.GetByteCount(nativeSvg) }));
        Assert.True(nativePng.Length < budget); Assert.True(Encoding.UTF8.GetByteCount(nativeSvg) > budget);
        var svgError = Assert.IsType<MermaidResourceLimitException>(svg.Error);
        var htmlError = Assert.IsType<MermaidResourceLimitException>(html.Error);
        Assert.Equal(nameof(MermaidResourceLimits.MaximumOutputBytes), svgError.LimitName);
        var nativeError = Assert.IsType<VisualResourceLimitException>(svgError.InnerException);
        var sourceLines = source.Split('\n');
        var expectedLine = Array.FindIndex(sourceLines, line => line.StartsWith(nativeError.ItemId + "@{", StringComparison.Ordinal));
        Assert.True(expectedLine > 0); Assert.Equal(expectedLine + 1, svgError.Span.Line);
        Assert.Equal(sourceLines[expectedLine].IndexOf("'key'", StringComparison.Ordinal) + 1, svgError.Span.Column);
        Assert.Equal(svgError.Span, htmlError.Span); Assert.Null(png.Error); Assert.Equal(nativePng, png.Value);
    }

    [Fact]
    public void NativeResourceFailureTranslatesThroughTheActualSourceRasterExportPolicy() {
        const string source = "flowchart LR\nimage@{img: 'key'}";
        var document = (MermaidFlowchartDocument)new MermaidParser().Parse(source).Document!;
        var artifact = document.ToVisualArtifact(new MermaidFlowchartRenderOptions { Width = 320, Height = 240, FitContent = false, Title = "" });
        var header = Encoding.ASCII.GetBytes("P6\n6000 6000\n255\n");
        artifact.Model = Chart(TopologyIconArtwork.Image("data:image/x-portable-pixmap;base64," + Convert.ToBase64String(header)), 1);
        artifact.NaturalSize = new VisualArtifactSize(320, 240); artifact.PreserveNaturalSize = true;
        var failure = Observe("source-raster-resource-limit.png", () => artifact.ToPng());
        var error = Assert.IsType<MermaidResourceLimitException>(failure.Error);
        Assert.Equal("NativeRasterResourceBytes", error.LimitName); Assert.Equal(2, error.Span.Line);
        Assert.Equal(source.Split('\n')[1].IndexOf("'key'", StringComparison.Ordinal) + 1, error.Span.Column);
    }

    [Fact]
    public void IdentityCountsRealRgbaVisitsButAnExplicitPrefixDoesNotChargeHypotheticalHashWork() {
        var resource = new VisualSceneImageData(new RgbaImage(1000, 1000, new byte[4 * 1000 * 1000]));
        var commands = new List<VisualSceneNode>();
        for (var index = 0; index < 251; index++) {
            commands.Add(new VisualSceneGroup("image-owner", "image-" + index, null, new Dictionary<string, string> { ["data-node-id"] = "image-" + index }));
            commands.Add(new VisualSceneImage(resource, new ChartRect(1, 1, 1, 1), "image", null)); commands.Add(new VisualSceneEndGroup());
        }
        var scene = new VisualScene(new VisualSize(8, 8), commands, Array.Empty<VisualDiagnostic>(), Array.Empty<VisualSemanticRegion>());
        var identity = Observe("identity-real-pixel-work", () => VisualSceneSvgRenderer.Identity(scene, null, null, null, false));
        var explicitPrefix = Observe("identity-explicit-prefix.svg", () => VisualSceneSvgRenderer.Render(scene, idPrefix: "host"));
        var error = Assert.IsType<VisualResourceLimitException>(identity.Error);
        Assert.Equal("SvgImageIdentityWorkSamples", error.Limit); Assert.Equal(RasterEffectPreflight.MaximumWorkSamples, error.Maximum);
        Assert.Equal(1_004_000_000, error.Observed); Assert.Equal("image-250", error.ItemId);
        Assert.Null(explicitPrefix.Error); Assert.Contains("<image", explicitPrefix.Value!);
    }

    [Fact]
    public void EmbeddedVectorResourcesAreAdmittedBeforeTheirBase64RepresentationAndSkipNativeFallbackUris() {
        var vector = "<svg xmlns='http://www.w3.org/2000/svg'><!--" + new string('x', 10000) + "--></svg>";
        var resource = VisualSceneImageResource.EmbeddedSvg(vector, new ChartRect(0, 0, 2, 2), null, 1, "vector", null);
        var fallback = new VisualSceneImage(new VisualSceneImageData(Noise(16, 16)), new ChartRect(0, 0, 2, 2), "fallback", null);
        var scene = new VisualScene(new VisualSize(4, 4), new VisualSceneNode[] {
            new VisualSceneGroup(null, null, null, null, imageResource: resource), fallback, new VisualSceneEndGroup()
        }, Array.Empty<VisualDiagnostic>(), Array.Empty<VisualSemanticRegion>());
        var rejected = Observe("embedded-svg-resource-bound", () => VisualSceneSvgRenderer.Render(scene, maximumCharacters: 1024));
        var ordinary = Observe("embedded-svg-resource-control.svg", () => VisualSceneSvgRenderer.Render(scene, idPrefix: "host"));
        Assert.IsType<VisualResourceLimitException>(rejected.Error); Assert.Null(ordinary.Error);
        Assert.Contains("data:image/svg+xml;base64,", ordinary.Value!); Assert.DoesNotContain("data:image/png;base64,", ordinary.Value!);
    }

    private static TopologyChart Chart(TopologyIconArtwork image, int count) {
        var chart = TopologyChart.Create().WithTitle("").WithViewport(320, 240, 12).WithLegend(null); chart.Id = "source-budget"; chart.LayoutMode = TopologyLayoutMode.Manual;
        for (var index = 0; index < count; index++) chart.Nodes.Add(new TopologyNode { Id = index == 0 ? "image" : "image_" + index, Label = "Image", X = 72, Y = 48, Width = 64, Height = 64,
            DisplayMode = TopologyNodeDisplayMode.Artwork, Artwork = image, ShowLabel = false, ShowStatusBadge = false, PreserveDisplayModeSize = true });
        return chart;
    }
    private static RgbaImage Noise(int width, int height) {
        var pixels = new byte[width * height * 4]; uint value = 0x67b8a3d5;
        for (var index = 0; index < pixels.Length; index++) { value ^= value << 13; value ^= value >> 17; value ^= value << 5; pixels[index] = (byte)value; }
        return new RgbaImage(width, height, pixels);
    }
    private static (T? Value, Exception? Error) Observe<T>(string name, Func<T> action) {
        try { var value = action(); if (value is string text) Write(name, text); else if (value is byte[] bytes) Write(name, bytes); return (value, null); }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or NotSupportedException or OverflowException) {
            Write(name + ".error.json", JsonSerializer.Serialize(new { type = exception.GetType().FullName, exception.Message })); return (default, exception);
        }
    }
    private static void Write(string name, string text) { var output = Output(); if (output != null) File.WriteAllText(Path.Combine(output, name), text); }
    private static void Write(string name, byte[] bytes) { var output = Output(); if (output != null) File.WriteAllBytes(Path.Combine(output, name), bytes); }
    private static string? Output() { var output = Environment.GetEnvironmentVariable("CFX_RASTER_RESOURCE_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(output)) return null; Directory.CreateDirectory(output); return output; }
}
