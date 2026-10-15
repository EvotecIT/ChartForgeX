using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;
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
        var result = MermaidRenderer.Render(Source(size, body)); Assert.Empty(result.Diagnostics);
        var artifact = result.Artifact!; var svg = artifact.ToSvg(); var png = artifact.ToPng();
        await Capture(svg, png, kind + "-" + size, 960, 720);
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
    public async Task DenseAcceptedSequenceTypographyPreservesDefaultSvgAndPngExport(int size, int count) {
        var participants = string.Join("\n", Enumerable.Range(0, count).Select(index => "participant P" + index + " as W"));
        var result = MermaidRenderer.Render(Source(size, "sequenceDiagram\n" + participants + "\nP0->>P" + (count - 1) + ": Save"));
        Assert.Empty(result.Diagnostics); var artifact = result.Artifact!;
        var svg = artifact.ToSvg();
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) { Directory.CreateDirectory(capture); File.WriteAllText(Path.Combine(capture, "dense-sequence-" + size + "-" + count + ".svg"), svg); }
        var png = artifact.ToPng(); Assert.NotEmpty(png);
        Assert.Equal(size, Assert.IsType<SequenceArtifact>(artifact.Model).Theme.Typography.DataLabelSize);
        Assert.Equal(count, XDocument.Parse(svg).Descendants().Count(element => (string?)element.Attribute("data-cfx-role") == "sequence-participant"));
        await Capture(svg, png, "dense-sequence-" + size + "-" + count, 960, 720);
    }

    private static string Source(int size, string body) => "%%{init: {'fontFamily':'Arial','themeVariables':{'fontSize':" + size.ToString(CultureInfo.InvariantCulture) + "}}}%%\n" + body;

    private static async Task Capture(string svg, byte[] png, string name, int width, int height) {
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture); File.WriteAllText(Path.Combine(capture, name + ".svg"), svg); File.WriteAllBytes(Path.Combine(capture, name + "-native.png"), png);
        }
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'><div style='overflow:auto;max-width:100vw'>" + svg + "</div></body></html>", width, height);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        if (!string.IsNullOrWhiteSpace(capture)) await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(capture, name + "-browser.png"), FullPage = true });
    }
}
