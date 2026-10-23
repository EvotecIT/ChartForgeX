using System.Text.Json;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidAgentflowMetadataSequenceTests {
    private const string Source = """
        agentflow-beta
        a@{
          steps:
            - role: reader
              policy:
                attempts: 2
                flags:
                  - safe
                  - deterministic
              instruction: |
                %%{init: {'theme':'dark'}}%%
                accTitle: Literal instruction
                } remains literal
              constructor:
                ignored: true
            - role: writer
              enabled: true
            - {role: observer, tags: [stable, inert]}
            - https://example.invalid/a:b
            - "role: literal"
        }
        b
        """;

    [Fact]
    public void InlineMappingItemsRetainContinuationKeysNestedValuesAndArraySiblingsAsTypedInertData() {
        var parsed = new MermaidParser().Parse(Source); Assert.Empty(parsed.Diagnostics);
        var document = Assert.IsType<MermaidAgentflowDocument>(parsed.Document);
        Assert.Equal(Source, document.SourceText);
        Assert.Null(document.Theme); Assert.Null(document.Accessibility.Name);
        var steps = document.Nodes.Single(item => item.Id == "a").Metadata["steps"];
        Assert.Equal(MermaidMetadataKind.Array, steps.Kind); Assert.Equal(5, steps.Items.Count);
        var reader = steps.Items[0];
        Assert.Equal(MermaidMetadataKind.Object, reader.Kind);
        Assert.Equal("reader", reader.Properties["role"].Text);
        Assert.Equal(2d, reader.Properties["policy"].Properties["attempts"].Number);
        Assert.Equal(new[] { "safe", "deterministic" }, reader.Properties["policy"].Properties["flags"].Items.Select(item => item.Text));
        Assert.Contains("%%{init:", reader.Properties["instruction"].Text);
        Assert.Contains("} remains literal", reader.Properties["instruction"].Text);
        Assert.False(reader.Properties.ContainsKey("constructor"));
        Assert.Equal("writer", steps.Items[1].Properties["role"].Text);
        Assert.True(steps.Items[1].Properties["enabled"].Boolean);
        Assert.Equal("observer", steps.Items[2].Properties["role"].Text);
        Assert.Equal(new[] { "stable", "inert" }, steps.Items[2].Properties["tags"].Items.Select(item => item.Text));
        Assert.Equal("https://example.invalid/a:b", steps.Items[3].Text);
        Assert.Equal("role: literal", steps.Items[4].Text);
        Assert.Empty(document.Nodes.Single(item => item.Id == "b").Metadata);
        Assert.Equal((4, 7), (reader.Properties["role"].Span.Line, reader.Properties["role"].Span.Column));
        var artifact = MermaidRenderer.Render(Source).Artifact!;
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson());
        using var retained = JsonDocument.Parse(envelope.Nodes.Single(item => item.Id == "a").Extensions["mermaid.metadata.steps"]);
        Assert.Equal(JsonValueKind.Object, retained.RootElement[0].ValueKind);
        Assert.Equal("reader", retained.RootElement[0].GetProperty("role").GetString());
    }

    [Fact]
    public void FirstMappingKeyCanOwnNestedDataBeforeItsFollowingKeysAndNextArrayItem() {
        const string source = "agentflow-beta\na@{\n  steps:\n    - policy:\n        attempts: 2\n        tags: [read, safe]\n      role: reader\n    - role: writer\n}";
        var parsed = new MermaidParser().Parse(source); Assert.Empty(parsed.Diagnostics);
        var items = Assert.Single(Assert.IsType<MermaidAgentflowDocument>(parsed.Document).Nodes).Metadata["steps"].Items;
        Assert.Equal(2, items.Count);
        Assert.Equal("reader", items[0].Properties["role"].Text);
        Assert.Equal(2d, items[0].Properties["policy"].Properties["attempts"].Number);
        Assert.Equal("writer", items[1].Properties["role"].Text);
    }

    [Theory]
    [InlineData("} remains literal")]
    [InlineData("] remains literal")]
    public void BlockLiteralDelimitersCannotReclassifyFollowingFlowValues(string instruction) {
        var source = "agentflow-beta\na@{\n  instruction: |\n    " + instruction
            + "\n  steps:\n    - {role: reader, tags: [safe, inert]}\n    - role: writer\n}";
        var result = MermaidRenderer.Render(source); Assert.Empty(result.Diagnostics);
        var node = Assert.Single(Assert.IsType<MermaidAgentflowDocument>(result.Document).Nodes);
        Assert.Equal(instruction + "\n", node.Metadata["instruction"].Text);
        var items = node.Metadata["steps"].Items;
        Assert.Equal(2, items.Count);
        Assert.Equal("reader", items[0].Properties["role"].Text);
        Assert.Equal(new[] { "safe", "inert" }, items[0].Properties["tags"].Items.Select(item => item.Text));
        Assert.Equal("writer", items[1].Properties["role"].Text);
        var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.False(fenced.HasErrors);
        Assert.Equal(result.Artifact!.ToSvg(), Assert.Single(fenced.Artifacts).ToSvg());
    }

    [Fact]
    public void UnwrappedFlowMappingsRetainNestedPhysicalLineContinuations() {
        const string source = "agentflow-beta\na@{ steps: [\n  {role: reader, tags: [safe, inert]},\n  {role: writer}\n], enabled: true }";
        var parsed = new MermaidParser().Parse(source); Assert.Empty(parsed.Diagnostics);
        var metadata = Assert.Single(Assert.IsType<MermaidAgentflowDocument>(parsed.Document).Nodes).Metadata;
        Assert.True(metadata["enabled"].Boolean);
        var items = metadata["steps"].Items;
        Assert.Equal(new[] { "reader", "writer" }, items.Select(item => item.Properties["role"].Text));
        Assert.Equal(new[] { "safe", "inert" }, items[0].Properties["tags"].Items.Select(item => item.Text));
    }

    [Theory]
    [InlineData("    - role: reader\n      role: writer")]
    [InlineData("    - budget: .inf")]
    public void DuplicateMappingKeysAndNonFiniteItemValuesCannotProduceArtifacts(string entries) {
        var source = "agentflow-beta\na@{\n  steps:\n" + entries + "\n}";
        var direct = MermaidRenderer.Render(source); Assert.True(direct.HasErrors); Assert.Null(direct.Artifact);
        Assert.Contains(direct.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidStatement && item.Span.Line == 2);
        var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.True(fenced.HasErrors); Assert.Empty(fenced.Artifacts);
    }

    [Fact]
    public void TypedMappingItemsRemainInsideCapturedNestingAndElementBudgets() {
        foreach (var limits in new[] {
            new MermaidResourceLimits { MaximumNestingDepth = 3 },
            new MermaidResourceLimits { MaximumElements = 5 }
        }) {
            var result = new MermaidParser().Parse(Source, new MermaidParseOptions { ResourceLimits = limits });
            Assert.True(result.HasErrors);
            Assert.Contains(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.ResourceLimitExceeded);
        }
    }
}
