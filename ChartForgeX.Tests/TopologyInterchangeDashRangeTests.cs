using System;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyInterchangeDashRangeTests {
    [Theory]
    [InlineData(0D)]
    [InlineData(-1D)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void NonPositiveOrNonFiniteEdgeDashLengthsKeepTheRangeExceptionContract(double invalid) {
        var chart = TopologyChart.Create();
        chart.Nodes.Add(new TopologyNode { Id = "source", Label = "Source" });
        chart.Nodes.Add(new TopologyNode { Id = "target", Label = "Target" });
        chart.Edges.Add(new TopologyEdge { Id = "edge", SourceNodeId = "source", TargetNodeId = "target" });
        var envelope = chart.ToVisualArtifact().ToInterchangeEnvelope();
        envelope.Edges[0].Topology!.DashPattern.AddRange(new[] { 4D, invalid });
        Assert.Throws<ArgumentOutOfRangeException>(() => envelope.ToJson());
    }
}
