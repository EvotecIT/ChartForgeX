using System.Diagnostics;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidFlowchartClosingEndpointTests {
    [Trait("Category", "Browser")]
    [Theory]
    [InlineData("--", "o--o", TopologyMarkerKind.Circle)]
    [InlineData("==", "x==x", TopologyMarkerKind.Cross)]
    [InlineData("-.", "o-.-o", TopologyMarkerKind.Circle)]
    [InlineData("--", "<-->", TopologyMarkerKind.Arrow)]
    [InlineData("==", "<==>", TopologyMarkerKind.Arrow)]
    [InlineData("-.", "<.->", TopologyMarkerKind.Arrow)]
    public async Task ClosingLinkTokensRetainBothEndpointsAndCaption(string prefix, string suffix, TopologyMarkerKind marker) {
        var source = "flowchart LR\nA " + prefix + " label " + suffix + " B; B --> C";
        var parsed = new MermaidParser().ParseFlowchart(source);
        Assert.Empty(parsed.Diagnostics);
        var edge = parsed.Document!.Edges[0];
        Assert.Equal(("A", "B", "label"), (edge.SourceId, edge.TargetId, edge.Label));
        var native = parsed.Document.ToTopologyChart().Edges[0];
        Assert.Equal(marker, native.SourceMarker);
        Assert.Equal(marker, native.TargetMarker);
        var artifact = MermaidRenderer.Render(source).Artifact!;
        var svg = artifact.ToSvg(); var png = artifact.ToPng();
        Assert.NotEmpty(png);
        var folder = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        var name = marker + "-" + (prefix == "==" ? "thick" : prefix == "-." ? "dotted" : "normal");
        if (!string.IsNullOrWhiteSpace(folder)) {
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, name + ".svg"), svg);
            File.WriteAllBytes(Path.Combine(folder, name + "-native.png"), png);
        }
        if (InteractiveChartBrowser.Enabled) {
            await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body>" + svg + "</body></html>", 1000, 720);
            InteractiveChartBrowser.AssertNoConsoleErrors(session);
            if (!string.IsNullOrWhiteSpace(folder))
                await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(folder, name + "-browser.png"), FullPage = true });
        }
    }

    [Theory]
    [InlineData("--a")]
    [InlineData("==a")]
    [InlineData("-.a")]
    [InlineData(".")]
    public void LongIncompleteCaptionInputRetainsItsFollowingStatement(string fragment) {
        // A real untrusted-input path: incomplete captions must not repeatedly scan
        // every remaining suffix. Timings are retained for diagnosis, not CI thresholds.
        var sizes = new[] { 6000, 12000, 50000 };
        var options = MermaidInnerBoundaryTestPolicy.ParseOptions();
        // The malformed text can still form repeated edge tokens before its incomplete
        // suffix is diagnosed. This fixture targets scan recovery beyond the outer caps.
        options.ResourceLimits.MaximumEdges = 60_000;
        options.ResourceLimits.MaximumElements = 60_000;
        foreach (var count in sizes) {
            var text = "flowchart LR\nA " + (fragment == "." ? "-. a" : "") + string.Concat(Enumerable.Repeat(fragment, count)) + "\nZ[End]";
            var timer = Stopwatch.StartNew();
            var result = new MermaidParser().Parse(text, options);
            timer.Stop();
            Assert.Contains(Assert.IsType<MermaidFlowchartDocument>(result.Document).Nodes, node => node.Id == "Z" && node.Text == "End");
            Console.WriteLine("Incomplete " + fragment + " " + count + ": " + timer.Elapsed.TotalMilliseconds + "ms");
        }
    }
}
