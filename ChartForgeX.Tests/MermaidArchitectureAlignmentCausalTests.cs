using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidArchitectureAlignmentCausalTests {
    [Theory]
    [InlineData("row", true)] [InlineData("column", false)]
    public void AuthoredAlignmentIsAcceptedAndItsActualBodyCentersAndOrderReachTheNativeScene(string kind, bool beforeDeclarations) {
        var declarations = "service third[Third]\nservice first[First]\nservice second[Second]\n";
        var alignment = "align " + kind + " first second third\n";
        var source = "architecture-beta\n" + (beforeDeclarations ? alignment + declarations : declarations + alignment);
        var parsed = new MermaidParser().ParseArchitecture(source); var document = parsed.Document; VisualArtifactInterchangeEnvelope? saved = null;
        if (document != null && !parsed.HasErrors) {
            var artifact = document.ToVisualArtifact(); saved = artifact.ToInterchangeEnvelope(); Capture(artifact, "architecture-alignment-causal-" + kind);
        }
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, "architecture-alignment-causal-" + kind + ".parse.json"), JsonSerializer.Serialize(new { source,
                diagnostics = parsed.Diagnostics.Select(diagnostic => new { diagnostic.Code, diagnostic.Message, diagnostic.Span.Line }) }));
        }
        Assert.Empty(parsed.Diagnostics); Assert.NotNull(document); Assert.NotNull(saved); Assert.Equal(source, document.SourceText);
        var nodes = new[] { "first", "second", "third" }.Select(id => saved.Nodes.Single(node => node.Id == id)).ToArray();
        for (var index = 1; index < nodes.Length; index++) {
            if (kind == "row") {
                Assert.Equal(nodes[0].Y!.Value + nodes[0].Height!.Value / 2, nodes[index].Y!.Value + nodes[index].Height!.Value / 2, 6);
                Assert.True(nodes[index - 1].X!.Value + nodes[index - 1].Width!.Value < nodes[index].X!.Value);
            } else {
                Assert.Equal(nodes[0].X!.Value + nodes[0].Width!.Value / 2, nodes[index].X!.Value + nodes[index].Width!.Value / 2, 6);
                Assert.True(nodes[index - 1].Y!.Value + nodes[index - 1].Height!.Value < nodes[index].Y!.Value);
            }
        }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void OrdinaryUnalignedArchitectureRetainsItsExistingSvgPngAndPortableBytes(bool grouped) {
        var source = "architecture-beta\n" + (grouped ? "group scope[Scope]\nservice a[Accept] in scope\nservice b[Build] in scope\n" : "service a[Accept]\nservice b[Build]\n") + "a:R -- L:b\n";
        var result = MermaidRenderer.Render(source); Assert.Empty(result.Diagnostics); var artifact = result.Artifact!;
        Capture(artifact, "architecture-unaligned-default-" + (grouped ? "grouped" : "ungrouped"));
        Assert.DoesNotContain("\"alignments\"", artifact.ToInterchangeJson(), StringComparison.Ordinal);
        Assert.Equal(2, artifact.ToInterchangeEnvelope().Nodes.Count); Assert.Equal(grouped ? 1 : 0, artifact.ToInterchangeEnvelope().Groups.Count);
    }

    private static void Capture(VisualArtifact artifact, string name) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output); var path = Path.Combine(output, name); File.WriteAllText(path + ".svg", artifact.ToSvg());
        File.WriteAllBytes(path + ".png", artifact.ToPng()); File.WriteAllText(path + ".json", artifact.ToInterchangeJson());
    }
}
