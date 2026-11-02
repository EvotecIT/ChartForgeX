using ChartForgeX.Markup;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MarkupFlowIdentityTests {
    [Theory]
    [InlineData("", "", "flow-1", "flow-2")]
    [InlineData(" {#flow}", "", "flow", "flow")]
    [InlineData("", "id flow\n", "flow", "flow")]
    public void MultipleFlowFences_NumberOnlyOmittedIds(string attributes, string command, string firstId, string secondId) {
        var fence = "```chartforgex flow v1" + attributes + "\n" + command + "start intake \"Intake\"\n```\n";
        var parser = new VisualMarkupParser();
        var result = parser.Parse(fence + fence);

        Assert.False(result.HasErrors);
        Assert.Equal(new[] { firstId, secondId }, result.Artifacts.Select(artifact => artifact.Id));
        Assert.All(result.Artifacts, artifact => Assert.Equal(artifact.Id, Assert.IsType<FlowArtifact>(artifact.Model).Id));
        Assert.Equal(new[] { firstId, secondId }, parser.ParseBlocks(VisualMarkupScanner.Scan(fence + fence).Blocks).Artifacts.Select(artifact => artifact.Id));
    }
}
