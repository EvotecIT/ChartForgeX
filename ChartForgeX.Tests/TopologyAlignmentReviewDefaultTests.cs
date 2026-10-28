using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Old public paths for before/after byte capture while the shared direction owner is extracted.</summary>
public sealed class TopologyAlignmentReviewDefaultTests {
    [Theory]
    [InlineData(TopologyLayoutDirection.TopToBottom, TopologyLayoutDirection.RightToLeft, TopologyAlignmentKind.Column)]
    [InlineData(TopologyLayoutDirection.RightToLeft, TopologyLayoutDirection.BottomToTop, TopologyAlignmentKind.Row)]
    public void OrdinaryUnalignedInheritedCompartmentsKeepTheSameAuthoredFactsAndPaint(TopologyLayoutDirection global, TopologyLayoutDirection local, TopologyAlignmentKind kind) {
        var chart = TopologyAlignmentReviewCausalTests.CompartmentChart(global, local, kind); chart.Alignments.Clear();
        var artifact = chart.ToVisualArtifact(); TopologyAlignmentReviewCausalTests.Capture(artifact, "alignment-review-unaligned-default-" + global);
        var saved = artifact.ToInterchangeEnvelope(); Assert.Empty(saved.Topology!.Alignments);
        var first = saved.Groups.Single(group => group.Id == "first"); var second = saved.Groups.Single(group => group.Id == "second");
        if (kind == TopologyAlignmentKind.Column) { Assert.Equal(first.X, second.X); Assert.Equal(first.Width, second.Width); Assert.True(first.Y!.Value + first.Height!.Value < second.Y!.Value); }
        else { Assert.Equal(first.Y, second.Y); Assert.Equal(first.Height, second.Height); Assert.True(first.X!.Value + first.Width!.Value < second.X!.Value); }
        Assert.Equal(global, chart.LayoutDirection); Assert.Equal(local, chart.Groups.Single(group => group.Id == "outer").LayoutDirection);
        Assert.Null(chart.Groups.Single(group => group.Id == "inner").LayoutDirection);
    }
}
