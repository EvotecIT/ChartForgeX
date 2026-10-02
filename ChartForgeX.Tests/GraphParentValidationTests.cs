using ChartForgeX.Interactivity;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class GraphParentValidationTests {
    [Fact]
    public void DeepParentFirstHierarchyValidatesWithoutRecursionAndStillRejectsCycles() {
        var scene = GraphScene.Create("hierarchy", "Hierarchy");
        for (var index = 0; index < 10_000; index++) scene.AddNode("n" + index, "Node " + index, node => node.ParentId = index == 0 ? null : "n" + (index - 1));
        scene.Validate();
        scene.Nodes[0].ParentId = "n9999";
        Assert.Contains("parent cycle", Assert.Throws<InvalidOperationException>(scene.Validate).Message);
    }
}
