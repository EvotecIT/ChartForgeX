using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyInterchangeRestorationRejectionTests {
    [Fact]
    public void InvalidReferenceAndUnsafeArtworkAreRejectedBeforeAnEditableGraphEscapes() {
        var envelope = Native(); var serialized = envelope.ToJson(); var valid = envelope.ToTopologyChart(); var before = valid.ToSvg();
        envelope.Edges[0].SourceId = "missing";
        Assert.ThrowsAny<ArgumentException>(() => envelope.ToTopologyChart()); Assert.Equal(before, valid.ToSvg());
        envelope = VisualArtifactInterchangeEnvelope.FromJson(serialized);
        envelope.Nodes[0].Topology!.Artwork!.SvgBody = "<script>alert('inert')</script>";
        Assert.ThrowsAny<ArgumentException>(() => envelope.ToTopologyChart()); Assert.Equal(before, valid.ToSvg());
        Assert.Equal("source", valid.Edges[0].SourceNodeId);
    }

    [Fact]
    public void EnvelopeFieldsWithoutFaithfulNativeOwnersAreRejectedWithoutEditingTheirSource() {
        var envelope = Native(); envelope.Nodes[0].X = null;
        Assert.Throws<NotSupportedException>(() => envelope.ToTopologyChart()); Assert.Null(envelope.Nodes[0].X);
        envelope = Native(); envelope.Annotations[0].Extensions["unmapped"] = "must not disappear";
        Assert.Throws<NotSupportedException>(() => envelope.ToTopologyChart()); Assert.Equal("must not disappear", envelope.Annotations[0].Extensions["unmapped"]);
        envelope = Native(); envelope.Nodes[0].Details[0].Status = "ExternalStatus";
        Assert.Throws<NotSupportedException>(() => envelope.ToTopologyChart());
        envelope = Native(); envelope.Presentation!.Theme!.Accent = "var(--host-accent, #123456)";
        Assert.Throws<NotSupportedException>(() => envelope.ToTopologyChart());
        envelope = Native(); envelope.Edges[0].Topology!.MinimumRankSpan = 0;
        Assert.Throws<NotSupportedException>(() => envelope.ToTopologyChart());
        envelope = Native(); envelope.Edges[0].Kind = "ExternalEdgeKind";
        Assert.Throws<NotSupportedException>(() => envelope.ToTopologyChart());
    }

    [Fact]
    public void SemanticEdgeOrderingIsRestoredAndAmbiguousOrdersAreRejected() {
        var envelope = Native(); var edge = envelope.Edges[0];
        var second = new VisualArtifactInterchangeEdge { Id = "second", Role = edge.Role, Kind = edge.Kind,
            SourceId = edge.SourceId, TargetId = edge.TargetId, Order = 4,
            Topology = new VisualArtifactInterchangeTopologyEdge { MinimumRankSpan = 1 } };
        envelope.Edges.Insert(0, second);
        Assert.Equal(new[] { edge.Id, "second" }, envelope.ToTopologyChart().Edges.Select(item => item.Id));
        second.Order = edge.Order;
        Assert.Throws<NotSupportedException>(() => envelope.ToTopologyChart());
    }

    [Fact]
    public void GeographicBoundsAndRawCoordinatesRemainEditableWhileProjectionTokensMustBeNative() {
        var envelope = Native();
        envelope.Presentation!.MapViewport = new VisualArtifactInterchangeMapViewport { Name = "Local window", Projection = "Equirectangular",
            MinimumLongitude = 10, MaximumLongitude = 30, MinimumLatitude = 45, MaximumLatitude = 60 };
        envelope.Nodes[0].Topology!.Longitude = 20; envelope.Nodes[0].Topology!.Latitude = 52;
        var chart = envelope.ToTopologyChart(); Assert.Equal("Local window", chart.MapViewport.Name);
        Assert.Equal(10, chart.MapViewport.MinimumLongitude); Assert.Equal(20, chart.Nodes[0].Longitude); Assert.Equal(52, chart.Nodes[0].Latitude);
        envelope.Presentation.MapViewport.Projection = "UnknownProjection";
        Assert.Throws<NotSupportedException>(() => envelope.ToTopologyChart());
    }

    private static VisualArtifactInterchangeEnvelope Native() => TopologyInterchangeRestorationTests.Source(false, 1000).ToVisualArtifact().ToInterchangeEnvelope();
}
