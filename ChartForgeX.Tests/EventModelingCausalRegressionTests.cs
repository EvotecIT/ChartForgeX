using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class EventModelingCausalRegressionTests {
    private const string OpaqueSource = "eventmodeling\naccTitle: Outer timeline\ntf 01 ui CartUI [[Payload]]\ndata Payload {\n  accTitle: Inner data field\n  quantity: 2\n}";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DataBlockWordsStayDataInsteadOfReplacingAccessibilityOrDisappearing(bool body) {
        var parsed = new MermaidParser().ParseEventModeling(OpaqueSource); Assert.False(parsed.HasErrors);
        if (body) Assert.Contains("accTitle: Inner data field", Assert.Single(parsed.Document!.DataBlocks).Content);
        else Assert.Equal("Outer timeline", parsed.Document!.Accessibility.Name);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ResetIsAvailableToTheNextFrameAndSameLaneFramesDoNotInventInferredLinks(bool sameLane) {
        var source = sameLane ? "eventmodeling\ntf 01 ui CartUI\ntf 02 ui CartUIAgain\ntf 03 cmd SaveCart"
            : "eventmodeling\ntf 01 evt FirstEvent\nrf 02 evt ExternalEvent\ntf 03 pcr Handler";
        var parsed = new MermaidParser().ParseEventModeling(source); Assert.False(parsed.HasErrors);
        if (sameLane) Assert.DoesNotContain(parsed.Document!.Relations, relation => relation.SourceNumber == "01" && relation.TargetNumber == "02");
        else Assert.Contains(parsed.Document!.Relations, relation => relation.SourceNumber == "02" && relation.TargetNumber == "03" && relation.IsInferred);
    }

    [Fact]
    public void RepeatedSourceTokensAreRelationsAndFrameNumbersRemainIdentitiesRatherThanSortingKeys() {
        const string source = "eventmodeling\nrf 02 evt CartCreated\nrf 03 evt ItemAdded\ntf 01 rmo CartView ->> 02 ->> 03";
        var parsed = new MermaidParser().ParseEventModeling(source); Assert.False(parsed.HasErrors);
        Assert.Equal(new[] { "02", "03", "01" }, parsed.Document!.Frames.Select(frame => frame.Number));
        Assert.Equal(new[] { ("02", "01"), ("03", "01") }, parsed.Document.Relations.Select(relation => (relation.SourceNumber, relation.TargetNumber)));
        Assert.All(parsed.Document.Relations, relation => Assert.False(relation.IsInferred));
    }

    [Fact]
    public void ReturningToAnEarlierLaneStillAdvancesTheTimelineInSourceOrder() {
        const string source = "eventmodeling\ntf 30 ui CartUI\ntf 10 cmd SaveCart\ntf 20 evt CartSaved\ntf 05 ui CartUIAgain";
        var parsed = new MermaidParser().ParseEventModeling(source); Assert.False(parsed.HasErrors);
        var prepared = parsed.Document!.ToTopologyChart(new MermaidTopologyRenderOptions { Width = 1000, Height = 700 }).Prepare().ToInterchangeEnvelope();
        var x = new[] { "30", "10", "20", "05" }.Select(id => prepared.Nodes.Single(node => node.Id == id).X!.Value).ToArray();
        Assert.True(x.Zip(x.Skip(1), (left, right) => left < right).All(value => value), "Every later source frame must advance the native timeline, including a return to an existing swimlane.");
    }
}
