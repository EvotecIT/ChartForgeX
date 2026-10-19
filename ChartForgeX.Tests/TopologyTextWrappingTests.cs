using ChartForgeX.Topology;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyTextWrappingTests {
    [Fact]
    public void WrappingKeepsACompleteFittingWordAndOnlyTheFinalRowReservesAnEllipsis() {
        const string caption = "Review access and approve";
        const double width = 80;
        var options = new TopologyRenderOptions { WrapNodeLabels = true };
        var diagnostics = new List<string>();
        TextMetrics Measure(string text, double size, int weight) => new(text.Length * 6, 12, 12);

        var wrapped = TopologyTextLayout.FitLines(caption, width, 12, 600, 2, false, options, Measure,
            (code, _) => diagnostics.Add(code), out var overflow);
        Assert.Equal(new[] { "Review access", "and approve" }, wrapped);
        Assert.Equal(caption, string.Join(" ", wrapped));
        Assert.False(overflow); Assert.Empty(diagnostics);
        Assert.All(wrapped, line => Assert.True(Measure(line, 12, 600).Width <= width));

        var finalRow = TopologyTextLayout.FitLines(caption, width, 12, 600, 1, false, options, Measure,
            (code, _) => diagnostics.Add(code), out overflow);
        Assert.Equal("Review acces…", Assert.Single(finalRow));
        Assert.True(overflow); Assert.Contains("topology.label-truncated", diagnostics);
        Assert.True(Measure(finalRow[0], 12, 600).Width <= width);
    }
}
