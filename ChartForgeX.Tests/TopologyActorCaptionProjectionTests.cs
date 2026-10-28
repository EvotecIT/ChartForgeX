using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyActorCaptionProjectionTests {
    [Fact]
    public void FittedExternalActorCaptionKeepsItsOriginalFittingDomainAcrossRecaptureAndPositionEdits() {
        var parsed = MermaidRenderer.Render(UseCaseJsonTableFixture.Read(), MermaidUseCaseJsonTableTests.Options(1100, false));
        Assert.Empty(parsed.Diagnostics);
        var chart = Assert.IsType<TopologyChart>(parsed.Artifact!.Model);
        var before = chart.Prepare(); Capture("direct", before);
        var restored = VisualArtifactInterchangeEnvelope.FromJson(before.ToInterchangeEnvelope().ToJson()).ToTopologyChart();
        var after = restored.Prepare(); Capture("restored", after);
        SameCaption(before, after); Assert.Equal(before.ToPng(), after.ToPng());
        var repeated = VisualArtifactInterchangeEnvelope.FromJson(after.ToInterchangeEnvelope().ToJson()).ToTopologyChart().Prepare();
        Capture("recaptured", repeated); SameCaption(after, repeated); Assert.Equal(after.ToPng(), repeated.ToPng());
        var old = Label(after); var actor = Assert.Single(restored.Nodes, node => node.Id == "User"); actor.X += 12;
        var translated = restored.Prepare(); Capture("translated", translated); var moved = Label(translated);
        Assert.Equal(old.X + 12, moved.X, 10); Assert.Equal(old.Baseline, moved.Baseline, 10); Assert.Equal(old.Text.Size, moved.Text.Size);
        Assert.Equal("Release coordinator", actor.Label);
    }
    private static VisualSceneText Label(PreparedTopology value) => Assert.Single(value.Visual.Scene.Nodes.OfType<VisualSceneText>(),
        mark => mark.Role == "topology-node-label" && mark.Id == "User-label-0");
    private static void SameCaption(PreparedTopology expected, PreparedTopology actual) {
        var before = Label(expected); var after = Label(actual);
        Assert.Equal(before.X, after.X, 10); Assert.Equal(before.Baseline, after.Baseline, 10);
        Assert.Equal(before.Text.Size, after.Text.Size); Assert.Equal(before.Text.Ascent, after.Text.Ascent);
        Assert.Equal(before.Text.Lines.Select(line => line.Text), after.Text.Lines.Select(line => line.Text));
    }
    private static string Caption(PreparedTopology value) {
        var mark = Label(value);
        return JsonSerializer.Serialize(new { mark.X, mark.Baseline, mark.Text.Size, mark.Text.Ascent,
            Lines = mark.Text.Lines.Select(line => new { line.Text, line.Width }).ToArray() });
    }
    internal static void CaptureCaption(string path, PreparedTopology value) => File.WriteAllText(path, JsonSerializer.Serialize(
        value.Visual.Scene.Nodes.OfType<VisualSceneText>().Where(mark => mark.Role == "topology-node-label").Select(mark => new {
            mark.Id, mark.X, mark.Baseline, mark.Text.Size, mark.Text.Ascent,
            Lines = mark.Text.Lines.Select(line => new { line.Text, line.Width }).ToArray()
        }).ToArray(), new JsonSerializerOptions { WriteIndented = true }));
    private static void Capture(string name, PreparedTopology value) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output); var path = Path.Combine(output, "actor-caption192-" + name);
        File.WriteAllText(path + ".svg", value.ToSvg()); File.WriteAllBytes(path + ".png", value.ToPng());
        File.WriteAllText(path + ".caption-facts.json", Caption(value));
        File.WriteAllText(path + ".json", value.ToInterchangeEnvelope().ToJson());
    }
}
