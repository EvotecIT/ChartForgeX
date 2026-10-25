using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidNestedSwimlaneCausalTests {
    [Theory]
    [InlineData("LR")] [InlineData("RL")] [InlineData("TB")] [InlineData("BT")]
    public void NestedResponsibilityGroupsActuallyContainTheirAuthoredChildLanes(string direction) {
        var source = "swimlane-beta " + direction + "\nsubgraph Product[Product]\nP[Plan]\nsubgraph API[API]\nA[Accept] --> B[Build]\nend\nsubgraph Ops[Ops]\nC[Check] --> D[Deliver]\nend\nend\nsubgraph Partner[Partner]\nE[Observe]\nend\nP --> A\nB --> C\nD --> E";
        var rendered = MermaidRenderer.Render(source); Assert.Empty(rendered.Diagnostics);
        var artifact = rendered.Artifact!; var saved = artifact.ToInterchangeEnvelope();
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); var path = Path.Combine(output, "swimlane-nested-causal-" + direction);
            File.WriteAllText(path + ".svg", artifact.ToSvg()); File.WriteAllBytes(path + ".native.png", artifact.ToPng());
            File.WriteAllText(path + ".native.json", saved.ToJson());
        }
        var parent = saved.Groups.Single(group => group.Id == "Product");
        foreach (var id in new[] { "API", "Ops" }) {
            var child = saved.Groups.Single(group => group.Id == id); Assert.Equal("Product", child.Topology!.ParentGroupId);
            Assert.True(parent.X!.Value <= child.X!.Value && parent.Y!.Value <= child.Y!.Value
                && parent.X!.Value + parent.Width!.Value >= child.X!.Value + child.Width!.Value
                && parent.Y!.Value + parent.Height!.Value >= child.Y!.Value + child.Height!.Value,
                "The actual child responsibility lane must be enclosed by its parent shell.");
        }
    }
}
