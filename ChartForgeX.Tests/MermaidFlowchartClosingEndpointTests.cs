using System.Diagnostics;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidFlowchartClosingEndpointTests {
    [Theory]
    [InlineData("--", "o--o", TopologyMarkerKind.Circle)]
    [InlineData("==", "x==x", TopologyMarkerKind.Cross)]
    [InlineData("-.", "o-.-o", TopologyMarkerKind.Circle)]
    [InlineData("--", "<-->", TopologyMarkerKind.Arrow)]
    [InlineData("==", "<==>", TopologyMarkerKind.Arrow)]
    [InlineData("-.", "<.->", TopologyMarkerKind.Arrow)]
    public void ClosingLinkTokensRetainBothEndpointsAndCaption(string prefix, string suffix, TopologyMarkerKind marker) {
        var parsed = new MermaidParser().ParseFlowchart("flowchart LR\nA " + prefix + " label " + suffix + " B; B --> C");
        Assert.Empty(parsed.Diagnostics);
        var edge = parsed.Document!.Edges[0];
        Assert.Equal(("A", "B", "label"), (edge.SourceId, edge.TargetId, edge.Label));
        var native = parsed.Document.ToTopologyChart().Edges[0];
        Assert.Equal(marker, native.SourceMarker);
        Assert.Equal(marker, native.TargetMarker);
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
        foreach (var count in sizes) {
            var text = "flowchart LR\nA " + (fragment == "." ? "-. a" : "") + string.Concat(Enumerable.Repeat(fragment, count)) + "\nZ[End]";
            var timer = Stopwatch.StartNew();
            var result = new MermaidParser().ParseFlowchart(text);
            timer.Stop();
            Assert.Contains(result.Document!.Nodes, node => node.Id == "Z" && node.Text == "End");
            Console.WriteLine("Incomplete " + fragment + " " + count + ": " + timer.Elapsed.TotalMilliseconds + "ms");
        }
    }
}
