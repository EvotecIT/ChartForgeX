using ChartForgeX.Markup;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Quoted public relationship IDs must retain token boundaries in every ordering intake.</summary>
public sealed class ChordQuotedIdentityCausalTests {
    [Theory]
    [InlineData("chordNodeOrder", "\"node a\" b", "command")]
    [InlineData("chordOutgoingFlowOrder", "\"flow one\"", "command")]
    [InlineData("chordIncomingFlowOrder", "\"flow one\"", "command")]
    [InlineData("chordNodeOrder", "\"node a\" b", "table")]
    [InlineData("chordOutgoingFlowOrder", "\"flow one\"", "table")]
    [InlineData("chordIncomingFlowOrder", "\"flow one\"", "table")]
    [InlineData("chordNodeOrder", "\"node a\" b", "section")]
    [InlineData("chordOutgoingFlowOrder", "\"flow one\"", "section")]
    [InlineData("chordIncomingFlowOrder", "\"flow one\"", "section")]
    public void QuotedIdentityOrderOptionsPreserveTheExistingNodeAndFlowIds(string key, string ids, string form) {
        const string source = "type chord\nnode \"node a\" A\nnode b B\nflow \"flow one\" \"node a\" b 3\n";
        var baseline = new MarkupChartParser().Parse(source);
        Assert.Empty(baseline.Diagnostics);
        var option = form == "table" ? "options:\n| key | value |\n| --- | --- |\n| " + key + " | " + ids + " |\nend"
            : form == "section" ? "options:\n" + key + " " + ids + "\nend" : "option " + key + " " + ids;
        var parsed = new MarkupChartParser().Parse(source + option);
        Assert.Empty(parsed.Diagnostics);
        var series = Assert.Single(parsed.Document!.Chart.Series);
        Assert.Equal(new[] { "node a", "b" }, series.Nodes.Select(node => node.Id));
        Assert.Equal("flow one", Assert.Single(series.FlowLinks).Id);
        var order = parsed.Document.Chart.Options.Chord.Ordering!;
        var actual = key == "chordNodeOrder" ? order.NodeIds
            : key == "chordOutgoingFlowOrder" ? order.OutgoingFlowIds : order.IncomingFlowIds;
        Assert.Equal(key == "chordNodeOrder" ? new[] { "node a", "b" } : new[] { "flow one" }, actual);
        // Explicit order metadata changes SVG identity; unchanged source-order paint must remain exact.
        Assert.Equal(baseline.Document!.Chart.ToPng(), parsed.Document.Chart.ToPng());
    }
}
