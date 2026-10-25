using ChartForgeX.Mermaid;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class MermaidC4ExplicitTypographyWeightTests {
    [Fact]
    public void MaintainedSourceWeightsReachNativePaintWhileUnspecifiedHeadingKeepsItsDefault() {
        var chart = MermaidC4ConfigurationDepthTests.Parse(MermaidC4ConfigurationDepthTests.Fixture()).ToTopologyChart();
        Assert.Equal("500", chart.Nodes.Single(node => node.Id == "operator").TextStyle!.FontWeight);
        Assert.Equal("400", chart.Nodes.Single(node => node.Id == "service").TextStyle!.FontWeight);
        Assert.Null(chart.Nodes.Single(node => node.Id == "partner").TextStyle!.FontWeight);
        var prepared = chart.Prepare();
        var labels = prepared.Visual.Scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == "topology-node-label").ToArray();
        foreach (var (id, weight) in new[] { ("operator", 500), ("service", 400), ("partner", 600) }) {
            var rows = labels.Where(text => text.Id == id + "-label").ToArray();
            Assert.NotEmpty(rows); Assert.All(rows, row => Assert.Equal(weight, row.Text.Style.Font.Weight));
        }
        Assert.NotEmpty(prepared.ToPng());
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } directory) return;
        Directory.CreateDirectory(directory); var stem = Path.Combine(directory, "c4-review-explicit-element-weights");
        File.WriteAllText(stem + ".svg", prepared.ToSvg()); File.WriteAllBytes(stem + ".native.png", prepared.ToPng());
        File.WriteAllText(stem + ".json", prepared.ToInterchangeEnvelope().ToJson());
    }
}
