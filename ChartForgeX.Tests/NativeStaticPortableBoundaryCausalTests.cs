using System.Text;
using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Incumbent public API controls for static wrappers whose captured semantics exceed the portable envelope.</summary>
public sealed class NativeStaticPortableBoundaryCausalTests {
    [Fact]
    public void RepeatedOrdinaryPortraitsKeepNativePngWrappingIndependentOfPortableJson() {
        var image = Noise(100, 100);
        var artwork = TopologyIconArtwork.Portrait(image);
        var chart = Native(artwork, 180);
        Write("repeated-artwork.input.png", PngWriter.WriteRgba(image));
        Write("repeated-artwork.input.json", JsonSerializer.Serialize(new { nodes = chart.Nodes.Count, image.Width, image.Height, uriCharacters = artwork.ImageHref!.Length }));
        var prepared = chart.Prepare(Options());
        var svg = Observe("repeated-artwork.prepared.svg", () => prepared.ToSvg());
        var png = Observe("repeated-artwork.prepared.png", () => prepared.ToPng());
        var wrappedTopology = Observe("repeated-artwork.topology-wrapper.png", () => chart.ToVisualArtifact().ToPng());
        var wrappedPrepared = Observe("repeated-artwork.prepared-wrapper.png", () => prepared.Visual.ToArtifact().ToPng());
        var portable = Observe("repeated-artwork.portable.json", () => prepared.ToInterchangeEnvelope().ToJson());
        // Static outputs and both wrapper failures are captured before an assertion can stop the causal row.
        Assert.InRange(artwork.ImageHref!.Length, 50000, 65536);
        Assert.Null(svg.Error); Assert.Null(png.Error);
        Assert.Null(wrappedTopology.Error); Assert.Null(wrappedPrepared.Error);
        Assert.Equal(png.Value, wrappedTopology.Value); Assert.Equal(png.Value, wrappedPrepared.Value);
        Assert.IsType<InvalidOperationException>(portable.Error);
    }

    [Fact]
    public void AdmittedSourceMetadataKeepsNativePngWrappingIndependentOfPortableJson() {
        var opaque = new string('x', 60000);
        var source = "flowchart LR\n" + string.Join("\n", Enumerable.Range(0, 150).Select(index => "n" + index + "@{opaque: '" + opaque + "'}"));
        Write("source-large-semantics.input.mmd", source);
        var limits = new MermaidResourceLimits { MaximumSourceCharacters = source.Length + 1, MaximumLineCharacters = 65536 };
        var parsed = new MermaidParser().Parse(source, new MermaidParseOptions { ResourceLimits = limits });
        Write("source-large-semantics.parse.json", JsonSerializer.Serialize(parsed.Diagnostics.Select(item => new { item.Code, item.Message, item.Span.Line, item.Span.Column })));
        var options = new MermaidFlowchartRenderOptions { FitContent = false, Width = 320, Height = 240, Padding = 12, Title = "" };
        var document = parsed.Document as MermaidFlowchartDocument;
        var topologyPng = Observe("source-large-semantics.topology.png", () => RequireDocument(document).ToTopologyChart(options).ToPng());
        var wrapperPng = Observe("source-large-semantics.wrapper.png", () => RequireDocument(document).ToVisualArtifact(options).ToPng());
        var preparedWrapper = Observe("source-large-semantics.prepared-wrapper.png", () => RequireDocument(document).ToTopologyChart(options).Prepare(Options()).Visual.ToArtifact().ToPng());
        var portable = Observe("source-large-semantics.portable.json", () => RequireDocument(document).ToTopologyChart(options).Prepare(Options()).ToInterchangeEnvelope().ToJson());
        Assert.False(parsed.HasErrors, string.Join("; ", parsed.Diagnostics.Select(item => item.Message)));
        Assert.NotNull(document); Assert.Null(topologyPng.Error); Assert.Null(wrapperPng.Error); Assert.Null(preparedWrapper.Error);
        Assert.NotEmpty(topologyPng.Value!); Assert.NotEmpty(wrapperPng.Value!); Assert.NotEmpty(preparedWrapper.Value!);
        Assert.IsType<InvalidOperationException>(portable.Error);
    }

    [Fact]
    public void OrdinaryNativeSourceAndPreparedWrappersKeepCapturedPixelsAndJson() {
        var image = Noise(4, 4); var chart = Native(TopologyIconArtwork.Portrait(image), 1);
        var prepared = chart.Prepare(Options());
        var native = Observe("ordinary-static-portable.native.png", () => prepared.ToPng());
        var wrapper = Observe("ordinary-static-portable.wrapper.png", () => chart.ToVisualArtifact().ToPng());
        var preparedWrapper = Observe("ordinary-static-portable.prepared-wrapper.png", () => prepared.Visual.ToArtifact().ToPng());
        var portable = Observe("ordinary-static-portable.json", () => prepared.Visual.ToArtifact().ToInterchangeJson());
        const string source = "flowchart LR\na[Plan] --> b[Deliver]";
        Write("ordinary-static-portable.input.mmd", source);
        var result = MermaidRenderer.Render(source);
        Write("ordinary-static-portable.diagnostics.json", JsonSerializer.Serialize(result.Diagnostics.Select(item => new { item.Code, item.Message })));
        var sourcePng = Observe("ordinary-static-portable.source.png", () => RequireArtifact(result).ToPng());
        var sourceJson = Observe("ordinary-static-portable.source.json", () => RequireArtifact(result).ToInterchangeJson());
        Assert.Null(native.Error); Assert.Null(wrapper.Error); Assert.Null(preparedWrapper.Error); Assert.Null(portable.Error);
        Assert.Equal(native.Value, wrapper.Value); Assert.Equal(native.Value, preparedWrapper.Value);
        Assert.Empty(result.Diagnostics); Assert.Null(sourcePng.Error); Assert.Null(sourceJson.Error);
        Assert.Single(VisualArtifactInterchangeEnvelope.FromJson(portable.Value!).Nodes);
    }

    [Fact]
    public void ExplicitSuppliedSemanticsStillHonorPortableLimitsWithoutMutatingNativeOutput() {
        var prepared = Native(TopologyIconArtwork.Portrait(Noise(4, 4)), 1).Prepare(Options());
        var pixels = prepared.ToPng();
        var supplied = prepared.ToInterchangeEnvelope();
        var opaque = new string('x', 60000);
        for (var index = 0; index < 200; index++) supplied.Extensions.Add("opaque-" + index, opaque);
        var rejected = Observe("explicit-oversized-semantics", () =>
            prepared.Visual.ToArtifact(supplied.Id, supplied.Kind, supplied));
        var ordinary = prepared.Visual.ToArtifact();
        Write("explicit-oversized-semantics.native.png", ordinary.ToPng());
        Assert.IsType<InvalidOperationException>(rejected.Error);
        Assert.Equal(pixels, ordinary.ToPng());
        Assert.Equal(pixels, prepared.ToPng());
        var restored = VisualArtifactInterchangeEnvelope.FromJson(ordinary.ToInterchangeJson());
        Assert.Single(restored.Nodes);
        Assert.DoesNotContain(restored.Extensions.Keys, key => key.StartsWith("opaque-", StringComparison.Ordinal));
    }

    private static TopologyChart Native(TopologyIconArtwork artwork, int count) {
        var chart = TopologyChart.Create().WithTitle("").WithViewport(320, 240, 12).WithLegend(null);
        chart.LayoutMode = TopologyLayoutMode.Manual;
        for (var index = 0; index < count; index++) chart.Nodes.Add(new TopologyNode {
            Id = "image-" + index, Label = "Image", X = 72, Y = 48, Width = 100, Height = 100,
            DisplayMode = TopologyNodeDisplayMode.Artwork, Artwork = artwork, ShowLabel = false, ShowStatusBadge = false, PreserveDisplayModeSize = true
        });
        return chart;
    }
    private static TopologyRenderOptions Options() => new() { FitContentToViewport = false, IncludeLegend = false, IncludeTitle = false };
    private static MermaidFlowchartDocument RequireDocument(MermaidFlowchartDocument? document) => document ?? throw new InvalidOperationException("The source document was not admitted.");
    private static VisualArtifact RequireArtifact(MermaidRenderResult result) => result.Artifact ?? throw new InvalidOperationException("The source artifact was not admitted.");
    private static RgbaImage Noise(int width, int height) {
        var pixels = new byte[width * height * 4]; uint value = 0x67b8a3d5;
        for (var index = 0; index < pixels.Length; index++) { value ^= value << 13; value ^= value >> 17; value ^= value << 5; pixels[index] = (byte)value; }
        return new RgbaImage(width, height, pixels);
    }
    private static (T? Value, Exception? Error) Observe<T>(string name, Func<T> observe) {
        try { var value = observe(); if (value is string text) Write(name, text); else if (value is byte[] bytes) Write(name, bytes); return (value, null); }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or NotSupportedException or OverflowException) {
            Write(name + ".error.json", JsonSerializer.Serialize(new { type = exception.GetType().FullName, exception.Message })); return (default, exception);
        }
    }
    private static void Write(string name, string text) { var output = Output(); if (output != null) File.WriteAllText(Path.Combine(output, name), text); }
    private static void Write(string name, byte[] bytes) { var output = Output(); if (output != null) File.WriteAllBytes(Path.Combine(output, name), bytes); }
    private static string? Output() {
        var output = Environment.GetEnvironmentVariable("CFX_STATIC_PORTABLE_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(output)) return null; Directory.CreateDirectory(output); return output;
    }
}
