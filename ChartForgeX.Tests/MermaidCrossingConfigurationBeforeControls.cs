using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class MermaidCrossingConfigurationBeforeControls {
    [Theory]
    [InlineData("json-null", "null", false)] [InlineData("json-array", "[]", false)] [InlineData("json-object", "{}", false)]
    [InlineData("json-nested-array", "[1]", false)] [InlineData("json-nested-object", "{\"x\":1}", false)]
    [InlineData("json-quoted-null", "\"null\"", false)] [InlineData("json-boolean", "true", false)]
    [InlineData("yaml-null", "null", true)] [InlineData("yaml-tilde", "~", true)]
    [InlineData("yaml-array", "[]", true)] [InlineData("yaml-object", "{}", true)]
    [InlineData("yaml-block-array", "\n      - item", true)] [InlineData("yaml-block-object", "\n      x: 1", true)]
    public void RetainCurrentPublicScalarProjectionAndLocationsBeforeNodeKindRefinement(string name, string value, bool yaml) {
        var source = Source(value, yaml); var parsed = new MermaidParser().ParseFlowchart(source);
        Capture(name, source, parsed); Assert.NotNull(parsed.Document); Assert.Equal(source, parsed.Document.SourceText);
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void UnrelatedSourceConsumersKeepTheirCurrentOutputAndDiagnosticControls(bool sequence) {
        var source = sequence ? "%%{init: {\"theme\":\"dark\",\"sequence\":{\"mirrorActors\":false},\"other\":[]}}%%\nsequenceDiagram\n A->>B: Review\n"
            : "%%{init: {\"flowchart\":{\"nodeSpacing\":25},\"other\":{}}}%%\nflowchart LR\n A[Accept] --> B[Build]\n";
        var rendered = MermaidRenderer.Render(source);
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); var path = Path.Combine(output, "crossing-parser-other-" + sequence);
            File.WriteAllText(path + ".source.mmd", source); File.WriteAllText(path + ".diagnostics.json", JsonSerializer.Serialize(rendered.Diagnostics));
            if (rendered.Artifact is { } capturedArtifact) { File.WriteAllText(path + ".svg", capturedArtifact.ToSvg()); File.WriteAllBytes(path + ".native.png", capturedArtifact.ToPng()); File.WriteAllText(path + ".portable.json", capturedArtifact.ToInterchangeJson()); }
        }
        Assert.NotNull(rendered.Artifact);
        var again = MermaidRenderer.Render(source);
        var artifact = rendered.Artifact ?? throw new InvalidOperationException("The first source render did not produce an artifact.");
        var repeatedArtifact = again.Artifact ?? throw new InvalidOperationException("The repeated source render did not produce an artifact.");
        Assert.Equal(artifact.ToSvg(), repeatedArtifact.ToSvg());
        Assert.Equal(artifact.ToPng(), repeatedArtifact.ToPng());
        Assert.Equal(JsonSerializer.Serialize(rendered.Diagnostics), JsonSerializer.Serialize(again.Diagnostics));
    }
    internal static string Source(string value, bool yaml) => yaml
        ? "---\nconfig:\n  swimlane:\n    lineHops: " + value + "\n---\n" + CrossingPaintControlFixture.Swimlane()
        : CrossingPaintControlFixture.Swimlane(value);
    internal static void Capture(string name, string source, MermaidParseResult<MermaidFlowchartDocument> parsed) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output); var path = Path.Combine(output, "crossing-parser-kind-" + name);
        File.WriteAllText(path + ".source.mmd", source); File.WriteAllText(path + ".diagnostics.json", JsonSerializer.Serialize(parsed.Diagnostics));
        File.WriteAllText(path + ".settings.json", JsonSerializer.Serialize(parsed.Document?.Configuration.Settings));
    }
}
