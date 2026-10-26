using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidCartesianSequenceFontBudgetTests {
    [Trait("Category", "Browser")]
    [Theory]
    [InlineData("quadrant", 18)]
    [InlineData("journey", 18)]
    [InlineData("quadrant", 96)]
    [InlineData("journey", 96)]
    [InlineData("quadrant", 256)]
    [InlineData("journey", 256)]
    public async Task AuthoredCartesianPointAndTaskCaptionsFitBeforeTheyAreShortened(string kind, int size) {
        var body = kind == "quadrant" ? "quadrantChart\nx-axis Low --> High\ny-axis Low --> High\nAlpha: [0.25, 0.25]\nBeta: [0.75, 0.75]"
            : "journey\nsection Work\nAlpha: 5: Sam\nBeta: 3: Sam";
        var result = MermaidRenderer.Render(Source(size, body));
        Assert.Empty(result.Diagnostics);
        var artifact = result.Artifact!; var svg = artifact.ToSvg(); var png = artifact.ToPng();
        await Capture(svg, png, kind + "-" + size, 960, 720);
        if (kind == "journey") {
            var journey = Assert.IsType<JourneyDiagramBlock>(artifact.Model);
            Assert.Equal(size, journey.Options.Theme.Typography.DataLabelSize);
            var xml = XDocument.Parse(svg);
            var captions = xml.Descendants().Where(item => (string?)item.Attribute("data-cfx-role") == "journey-task-label").ToArray();
            Assert.Equal(new[] { "Alpha", "Beta" }, captions.Select(item => item.Descendants().Single(child => child.Name.LocalName == "text").Value));
            Assert.Equal(2, xml.Descendants().Count(item => (string?)item.Attribute("data-cfx-role") == "journey-actor-membership"));
            Assert.Single(xml.Descendants(), item => (string?)item.Attribute("data-cfx-role") == "journey-actor-label" && item.Value.Contains("Sam", StringComparison.Ordinal));
            foreach (var caption in captions) foreach (var text in caption.Descendants().Where(item => item.Name.LocalName == "text"))
                Assert.InRange(double.Parse(text.Attribute("font-size")!.Value, CultureInfo.InvariantCulture), .001, size);
            return;
        }
        var labels = XDocument.Parse(svg).Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "data-label")
            .SelectMany(element => element.DescendantsAndSelf().Where(child => child.Name.LocalName == "text")).Select(element => element.Value).ToArray();
        Assert.Contains(kind == "quadrant" ? "Alpha" : "Alpha [Sam]", labels);
        Assert.Contains(kind == "quadrant" ? "Beta" : "Beta [Sam]", labels);
        Assert.Equal(size, Assert.IsType<Chart>(artifact.Model).Options.Theme.Typography.DataLabelSize);
        var chart = Assert.IsType<Chart>(artifact.Model);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var captionBounds = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "data-label")
            .Select(node => new ChartRect(node.X, node.Baseline - node.Text.Ascent, node.Text.Metrics.Width, node.Text.Metrics.Height)).ToArray();
        Assert.Equal(2, captionBounds.Length);
        Assert.False(captionBounds[0].Left < captionBounds[1].Right && captionBounds[0].Right > captionBounds[1].Left
            && captionBounds[0].Top < captionBounds[1].Bottom && captionBounds[0].Bottom > captionBounds[1].Top,
            "Complete oversized captions must fit around the earlier caption rather than overlap or drop the later one.");
        ScatterBubbleColorRenderingTests.Capture(prepared, kind + "-complete-caption-pair-" + size);
    }

    [Trait("Category", "Browser")]
    [Theory]
    [InlineData(18, 128)]
    [InlineData(256, 128)]
    [InlineData(18, 160)]
    [InlineData(256, 160)]
    public async Task DenseAcceptedSequenceTypographyPreservesVectorGeometryAndDefaultExportResourceBounds(int size, int count) {
        var participants = string.Join("\n", Enumerable.Range(0, count).Select(index => "participant P" + index + " as W"));
        var result = MermaidRenderer.Render(Source(size, "sequenceDiagram\n" + participants + "\nP0->>P" + (count - 1) + ": Save"));
        Assert.Empty(result.Diagnostics); var artifact = result.Artifact!;
        var sequence = Assert.IsType<SequenceArtifact>(artifact.Model);
        // Use the exact natural preparation owner used by artifact exports. Public Prepare(context)
        // intentionally uses a fixed viewport; rebuilding that frame here would test different geometry.
        var prepared = SequencePreparedCompiler.PrepareDefault(sequence);
        var measured = artifact.MeasureRasterSize();
        var svg = prepared.ToSvg(new VisualSvgOptions(effectProfile: VisualSvgEffectProfile.NativeFilters));
        await Capture(svg, null, "dense-sequence-" + size + "-" + count, 960, 720);
        Assert.Equal(size, sequence.Theme.Typography.DataLabelSize);
        Assert.Equal(Math.Ceiling(prepared.Size.Width), measured.Width);
        Assert.Equal(Math.Ceiling(prepared.Size.Height), measured.Height);
        var vector = XDocument.Parse(svg);
        var shown = vector.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "sequence-participant").ToArray();
        Assert.Equal(count, shown.Length);
        Assert.All(shown, participant => {
            Assert.Equal("W", (string?)participant.Attribute("data-label"));
            Assert.Contains(participant.Descendants(), text => text.Name.LocalName == "text" && text.Value == "W");
        });
        var limits = new MermaidResourceLimits();
        Assert.True(System.Text.Encoding.UTF8.GetByteCount(svg) <= limits.MaximumOutputBytes);
        var pixels = checked((long)measured.Width * (long)measured.Height);
        var observations = new List<object>();
        void Record(string format, string status, Exception? failure = null) {
            observations.Add(new { format, status, exceptionType = failure?.GetType().FullName,
                parameter = (failure as ArgumentOutOfRangeException)?.ParamName, message = failure?.Message,
                sourceLimit = (failure as MermaidResourceLimitException)?.LimitName,
                maximum = (failure as MermaidResourceLimitException)?.Limit, actual = (failure as MermaidResourceLimitException)?.Actual,
                sourceLine = (failure as MermaidResourceLimitException)?.Span.Line,
                sourceColumn = (failure as MermaidResourceLimitException)?.Span.Column,
                sourceLength = (failure as MermaidResourceLimitException)?.Span.Length });
            var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
            if (string.IsNullOrWhiteSpace(capture)) return;
            File.WriteAllText(Path.Combine(capture, "dense-sequence-" + size + "-" + count + "-resource-bounds.json"),
                JsonSerializer.Serialize(new { size, count, width = measured.Width, height = measured.Height, pixels,
                    defaultMaximumRasterPixels = limits.MaximumRasterPixels,
                    compositionMaximumBytes = ChartForgeX.Raster.RasterEffectPreflight.MaximumLiveBytes,
                    compositionMaximumWorkSamples = ChartForgeX.Raster.RasterEffectPreflight.MaximumWorkSamples,
                    nativeVectorSvgCaptured = true, nativePngWritten = false, observations }, new JsonSerializerOptions { WriteIndented = true }));
        }
        // The default profile bakes represented shadows. These retained large-font cases exceed
        // its composition preflight; explicit vector effects preserve the same complete scene.
        if (size == 256) {
            var failure = CompositionPreflight(() => artifact.ToSvg());
            Record("default-svg", "composition-preflight-rejected", failure);
        } else {
            var baked = XDocument.Parse(artifact.ToSvg());
            Assert.Equal((string?)vector.Root!.Attribute("width"), (string?)baked.Root!.Attribute("width"));
            Assert.Equal((string?)vector.Root.Attribute("height"), (string?)baked.Root.Attribute("height"));
            Assert.Equal(count, baked.Descendants().Count(element => (string?)element.Attribute("data-cfx-role") == "sequence-participant"));
            Record("default-svg", "admitted");
        }
        // Pixel policy uses the native measured destination, never rounded SVG attributes.
        // A destination inside that limit can still exceed the independent effect-work/buffer cap.
        if (pixels > limits.MaximumRasterPixels) {
            var failure = Assert.Throws<MermaidResourceLimitException>(() => artifact.ToPng());
            Assert.Equal(nameof(MermaidResourceLimits.MaximumRasterPixels), failure.LimitName);
            Assert.Equal(limits.MaximumRasterPixels, failure.Limit); Assert.Equal(pixels, failure.Actual);
            Assert.Equal(result.Document!.HeaderSpan, failure.Span);
            Record("default-png", "located-pixel-limit-rejected", failure);
        } else {
            var failure = CompositionPreflight(() => artifact.ToPng());
            Record("default-png", "composition-preflight-rejected", failure);
        }
    }

    private static ArgumentOutOfRangeException CompositionPreflight(Action export) {
        var failure = Assert.Throws<ArgumentOutOfRangeException>(export);
        Assert.Equal("additionalPixelBytes", failure.ParamName);
        Assert.StartsWith("Composition exceeds its raster preflight before execution.", failure.Message, StringComparison.Ordinal);
        return failure;
    }

    private static string Source(int size, string body) => "%%{init: {'fontFamily':'Arial','themeVariables':{'fontSize':" + size.ToString(CultureInfo.InvariantCulture) + "}}}%%\n" + body;

    private static async Task Capture(string svg, byte[]? png, string name, int width, int height) {
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture); File.WriteAllText(Path.Combine(capture, name + ".svg"), svg);
            var nativePng = Path.Combine(capture, name + "-native.png");
            if (png != null) File.WriteAllBytes(nativePng, png);
            else Assert.False(File.Exists(nativePng), "A rejected native raster must not be represented by an empty or stale PNG.");
        }
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'><div style='overflow:auto;max-width:100vw'>" + svg + "</div></body></html>", width, height);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        if (!string.IsNullOrWhiteSpace(capture)) await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(capture, name + "-browser.png"), FullPage = true });
    }
}
