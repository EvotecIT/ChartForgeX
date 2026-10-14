using System;
using System.Linq;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class VisualArtifactPaletteInterchangeTests {
    [Theory]
    [InlineData(0, 1)]
    [InlineData(int.MaxValue, 7)]
    public void NativePaletteSlotsRoundTripWithoutReplacingExplicitColors(int nodeIndex, int edgeIndex) {
        var chart = Diagram();
        chart.Nodes[0].PaletteIndex = nodeIndex;
        chart.Nodes[0].Color = "#123456";
        chart.Edges[0].PaletteIndex = edgeIndex;
        chart.Edges[0].Color = "#654321";

        var envelope = chart.ToVisualArtifact().ToInterchangeEnvelope();
        Assert.Equal(nodeIndex, envelope.Nodes.Single(node => node.Id == "source").Topology!.PaletteIndex);
        Assert.Equal(edgeIndex, Assert.Single(envelope.Edges).Topology!.PaletteIndex);
        string json = envelope.ToJson();
        var restored = VisualArtifactInterchangeEnvelope.FromUtf8Json(envelope.ToUtf8Json());
        var source = restored.Nodes.Single(node => node.Id == "source");
        var edge = Assert.Single(restored.Edges);
        Assert.Equal(nodeIndex, source.Topology!.PaletteIndex);
        Assert.Equal(edgeIndex, edge.Topology!.PaletteIndex);
        Assert.Equal("#123456", source.Color);
        Assert.Equal("#654321", edge.Color);
        Assert.Null(restored.Nodes.Single(node => node.Id == "target").Topology!.PaletteIndex);
        Assert.Equal(json, restored.ToJson());
    }

    [Fact]
    public void PreparedSemanticPaletteSlotsStayDetachedFromSourceAndReturnedEnvelopes() {
        var chart = Diagram();
        chart.Nodes[0].PaletteIndex = 2;
        chart.Edges[0].PaletteIndex = 3;
        var prepared = chart.Prepare();
        chart.Nodes[0].PaletteIndex = 5;
        chart.Edges[0].PaletteIndex = null;

        var first = prepared.ToInterchangeEnvelope();
        Assert.Equal(2, first.Nodes.Single(node => node.Id == "source").Topology!.PaletteIndex);
        Assert.Equal(3, Assert.Single(first.Edges).Topology!.PaletteIndex);
        first.Nodes.Single(node => node.Id == "source").Topology!.PaletteIndex = 6;
        first.Edges[0].Topology!.PaletteIndex = null;
        var second = prepared.ToInterchangeEnvelope();
        Assert.Equal(2, second.Nodes.Single(node => node.Id == "source").Topology!.PaletteIndex);
        Assert.Equal(3, Assert.Single(second.Edges).Topology!.PaletteIndex);
    }

    [Fact]
    public void MissingAndNullPaletteSlotsRetainOptionalInterchangeSemantics() {
        var envelope = Diagram().ToVisualArtifact().ToInterchangeEnvelope();
        string omitted = envelope.ToJson();
        Assert.DoesNotContain("\"paletteIndex\"", omitted);
        var absent = VisualArtifactInterchangeEnvelope.FromJson(omitted);
        Assert.All(absent.Nodes, node => Assert.Null(node.Topology!.PaletteIndex));
        Assert.All(absent.Edges, edge => Assert.Null(edge.Topology!.PaletteIndex));

        envelope.Nodes[0].Topology!.PaletteIndex = 0;
        envelope.Edges[0].Topology!.PaletteIndex = 0;
        string explicitNull = envelope.ToJson().Replace("\"paletteIndex\":0", "\"paletteIndex\":null", StringComparison.Ordinal);
        Assert.Contains("\"paletteIndex\":null", explicitNull);
        var restored = VisualArtifactInterchangeEnvelope.FromJson(explicitNull);
        Assert.All(restored.Nodes, node => Assert.Null(node.Topology!.PaletteIndex));
        Assert.All(restored.Edges, edge => Assert.Null(edge.Topology!.PaletteIndex));
        Assert.Equal(omitted, restored.ToJson());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TypedInterchangeRejectsNegativeNodeAndEdgePaletteSlots(bool edge) {
        var envelope = Diagram().ToVisualArtifact().ToInterchangeEnvelope();
        if (edge) envelope.Edges[0].Topology!.PaletteIndex = -1;
        else envelope.Nodes[0].Topology!.PaletteIndex = -1;
        Assert.Throws<ArgumentOutOfRangeException>(() => envelope.Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => envelope.ToJson());
    }

    [Theory]
    [InlineData(false, "-1")]
    [InlineData(true, "-1")]
    [InlineData(false, "1.5")]
    [InlineData(true, "1.5")]
    [InlineData(false, "2147483648")]
    [InlineData(true, "2147483648")]
    public void JsonRejectsNegativeFractionalAndOutOfRangePaletteSlots(bool edge, string value) {
        var envelope = Diagram().ToVisualArtifact().ToInterchangeEnvelope();
        if (edge) envelope.Edges[0].Topology!.PaletteIndex = 0;
        else envelope.Nodes[0].Topology!.PaletteIndex = 0;
        string json = envelope.ToJson().Replace("\"paletteIndex\":0", "\"paletteIndex\":" + value, StringComparison.Ordinal);
        Assert.Contains("\"paletteIndex\":" + value, json);
        Assert.ThrowsAny<ArgumentException>(() => VisualArtifactInterchangeEnvelope.FromJson(json));
    }

    private static TopologyChart Diagram() => TopologyChart.Create().WithViewport(600, 320).WithLegend(null)
        .AddNode("source", "Source", 70, 100)
        .AddNode("target", "Target", 340, 100)
        .AddEdge("connection", "source", "target");
}
