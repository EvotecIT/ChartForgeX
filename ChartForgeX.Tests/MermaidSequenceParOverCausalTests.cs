using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidSequenceParOverCausalTests {
    [Fact]
    public void AuthoredParOverAndBranchKeepsItsTerminalNoteInsideTheFrameBeforeTheNextMessage() {
        const string source = "sequenceDiagram\npar_over First\nA->>B: Request\nand Notes only\nNote over A,B: No reply\nend\nB-->>A: After";
        var rendered = MermaidRenderer.Render(source);
        Assert.Empty(rendered.Diagnostics);
        var sequence = Assert.IsType<SequenceArtifact>(rendered.Artifact!.Model);
        var prepared = SequencePreparedCompiler.PrepareDefault(sequence);
        var frame = Assert.Single(prepared.Regions, region => region.Role == "sequence-block");
        var note = Assert.Single(prepared.Regions, region => region.Role == "sequence-note");
        var branch = Assert.Single(prepared.Regions, region => region.Role == "sequence-branch" && region.Label == "Notes only");
        var after = Assert.Single(prepared.Regions, region => region.Role == "sequence-message" && region.Label == "After");
        Assert.InRange(note.Bounds.Bottom, frame.Bounds.Top, frame.Bounds.Bottom);
        Assert.InRange(branch.Bounds.Bottom, frame.Bounds.Top, frame.Bounds.Bottom);
        Assert.True(frame.Bounds.Bottom <= after.Bounds.Top);
    }
}
