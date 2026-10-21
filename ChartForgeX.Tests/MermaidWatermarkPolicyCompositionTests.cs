using ChartForgeX.Mermaid;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Stories;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidWatermarkPolicyCompositionTests {
    [Theory]
    [InlineData("flowchart LR\nA[Receive]-->B[Deliver]")]
    [InlineData("sequenceDiagram\nA->>B: Deliver")]
    [InlineData("packet-beta\n0-7: \"Header\"")]
    public void NativeMermaidWatermarksKeepMeasuredPixelsPoliciesAndSemanticEnvelopes(string source) {
        var original = MermaidRenderer.Render(source).Artifact!;
        var options = new VisualArtifactRenderOptions { Topology = new TopologyRenderOptions { PngOutputScale = 2 } };
        var originalJson = original.ToInterchangeJson();
        var expectedSize = original.MeasureRasterSize(options);
        var marked = original.ToWatermarkedArtifact(options, VisualWatermark.FromText("REVIEW"));
        Assert.Same(original.Model, marked.Model);
        Assert.Same(original.ExportPolicy, marked.ExportPolicy);
        Assert.Null(original.RenderSource);
        var measured = marked.MeasureRasterSize();
        Assert.Equal(expectedSize, measured);
        var png = RasterImageDecoder.Decode(marked.ToPng());
        Assert.Equal((int)measured.Width, png.Width);
        Assert.Equal((int)measured.Height, png.Height);
        Assert.Contains("REVIEW", marked.ToSvg());
        var expectedEnvelope = VisualArtifactInterchangeEnvelope.FromJson(originalJson);
        expectedEnvelope.Extensions["presentation.watermarks"] = "1";
        Assert.Equal(expectedEnvelope.ToJson(), marked.ToInterchangeJson());
        Assert.Equal(originalJson, original.ToInterchangeJson());
        options.Topology.PngOutputScale = 1;
        Assert.Equal(expectedSize, marked.Clone().MeasureRasterSize());
        var nested = marked.ToWatermarkedArtifact(VisualWatermark.FromText("COPY")).Clone();
        Assert.Equal(expectedSize, nested.MeasureRasterSize());
        Assert.Equal("2", nested.Metadata["presentation.watermarks"]);
        Assert.Contains("REVIEW", nested.ToSvg());
        Assert.Contains("COPY", nested.ToSvg());
        var nestedPng = RasterImageDecoder.Decode(nested.ToPng());
        Assert.Equal(png.Width, nestedPng.Width);
        Assert.Equal(png.Height, nestedPng.Height);
    }

    [Theory]
    [InlineData("flowchart LR\nA-->B")]
    [InlineData("sequenceDiagram\nA->>B: Deliver")]
    [InlineData("packet-beta\n0-7: \"Header\"")]
    public void DecorationsCannotBypassCapturedPixelOrEncodedOutputLimits(string source) {
        var pixelBound = MermaidRenderer.Render(source, new MermaidRenderOptions { ParseOptions = new MermaidParseOptions {
            ResourceLimits = new MermaidResourceLimits { MaximumRasterPixels = 1 }
        } }).Artifact!;
        var marked = pixelBound.ToWatermarkedArtifact(VisualWatermark.FromText("REVIEW")).Clone();
        var pixelFailure = Assert.Throws<MermaidResourceLimitException>(() => marked.ToPng());
        Assert.Equal(nameof(MermaidResourceLimits.MaximumRasterPixels), pixelFailure.LimitName);
        var outputBound = MermaidRenderer.Render(source, new MermaidRenderOptions { ParseOptions = new MermaidParseOptions {
            ResourceLimits = new MermaidResourceLimits { MaximumOutputBytes = 32 }
        } }).Artifact!.ToWatermarkedArtifact(VisualWatermark.FromText("REVIEW")).ToWatermarkedArtifact(VisualWatermark.FromText("COPY"));
        Assert.Equal(nameof(MermaidResourceLimits.MaximumOutputBytes), Assert.Throws<MermaidResourceLimitException>(() => outputBound.ToSvg()).LimitName);
        Assert.Equal(nameof(MermaidResourceLimits.MaximumOutputBytes), Assert.Throws<MermaidResourceLimitException>(() => outputBound.ToPng()).LimitName);
    }

    [Fact]
    public void PixelRejectionDoesNotRenderAMeasuredStaticProducerOrItsWatermarks() {
        var producer = new MeasuredSource();
        var artifact = VisualArtifact.Create("producer", VisualArtifactKind.VisualBlock, producer);
        artifact.ExportPolicy = PixelPolicy(1);
        var marked = artifact.ToWatermarkedArtifact(VisualWatermark.FromText("REVIEW"));
        Assert.Equal(nameof(MermaidResourceLimits.MaximumRasterPixels), Assert.Throws<MermaidResourceLimitException>(() => marked.ToPng()).LimitName);
        Assert.Equal(0, producer.RasterCalls);
        Assert.Equal(0, producer.SvgCalls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnmeasuredProducersRemainExplicitlyUnsupportedBeforeRasterAllocation(bool renderSource) {
        var producer = new UnmeasuredSource();
        var artifact = VisualArtifact.Create("producer", VisualArtifactKind.VisualBlock, producer);
        artifact.ExportPolicy = PixelPolicy(100000);
        if (renderSource) artifact.RenderSource = producer;
        var marked = artifact.ToWatermarkedArtifact(VisualWatermark.FromText("REVIEW"));
        Assert.Throws<InvalidOperationException>(() => marked.ToPng());
        Assert.Equal(0, producer.RasterCalls);
        Assert.Equal(0, producer.SvgCalls);
    }

    [Fact]
    public void CapturedStorySourcesProvideExactDimensionsThroughNestedWatermarks() {
        var story = VisualStory.Create("Completed story").WithSize(480, 320);
        story.Scene("result", "Ready", 1).Panel("result", new VisualStoryTextSurface("Delivered"));
        story.Outcome("done", "Delivered", "result");
        var original = story.ToVisualArtifact();
        original.ExportPolicy = PixelPolicy(1000000);
        var expected = original.MeasureRasterSize();
        var source = Assert.IsAssignableFrom<IMeasuredStaticVisualSource>(original.RenderSource);
        Assert.Equal(expected.Width, source.MeasureRasterSize().Width);
        var marked = original.ToWatermarkedArtifact(VisualWatermark.FromText("REVIEW"))
            .ToWatermarkedArtifact(VisualWatermark.FromText("COPY"));
        Assert.Equal(expected, marked.MeasureRasterSize());
        var pixels = RasterImageDecoder.Decode(marked.ToPng());
        Assert.Equal((int)expected.Width, pixels.Width);
        Assert.Equal((int)expected.Height, pixels.Height);
        Assert.Contains("Delivered", marked.ToSvg());
    }

    private static IVisualArtifactExportPolicy PixelPolicy(long pixels) => MermaidRenderer.Render("flowchart LR\nA-->B", new MermaidRenderOptions {
        ParseOptions = new MermaidParseOptions { ResourceLimits = new MermaidResourceLimits { MaximumRasterPixels = pixels } }
    }).Artifact!.ExportPolicy!;

    private class UnmeasuredSource : IStaticVisualSource {
        internal int RasterCalls, SvgCalls;
        public string RenderSvg(string idScope) { SvgCalls++; return "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"80\" height=\"40\"></svg>"; }
        public RgbaImage RenderRgba() { RasterCalls++; return new RgbaImage(80, 40, new byte[80 * 40 * 4]); }
    }
    private sealed class MeasuredSource : UnmeasuredSource, IMeasuredStaticVisualSource {
        public VisualSize MeasureRasterSize() => new(80, 40);
    }
}
