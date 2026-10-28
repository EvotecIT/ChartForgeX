using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects source-ordered fragment geometry through the existing public sequence conversion.</summary>
public sealed class MermaidSequenceFragmentCausalTests {
    [Fact]
    public void OuterParallelBranchCaptionPrecedesTheNestedCriticalFrameAtTheirSharedMessageBoundary() {
        const string source = "sequenceDiagram\nparticipant A\nparticipant B\n" +
            "par First path\nA->>B: Request\nand Second path\n" +
            "critical Guard work\nB->>A: Guarded\noption Recover\nA->>B: Recovery\nend\nend";
        var parsed = new MermaidParser().ParseSequence(source);
        Assert.Empty(parsed.Diagnostics);
        var prepared = SequencePreparedCompiler.PrepareDefault(parsed.Document!.ToSequenceArtifact());
        var branch = Assert.Single(prepared.Regions, region => region.Role == "sequence-branch" && region.Label == "Second path");
        var critical = Assert.Single(prepared.Regions, region => region.Role == "sequence-block" && region.Label == "Guard work");
        Assert.True(branch.Bounds.Top < critical.Bounds.Top,
            "An outer parallel branch caption must be laid out before the nested critical frame that begins in that branch.");
    }

    [Fact]
    public void TerminalEmptyBranchAndItsNoteRemainInsideTheOwningParallelFrame() {
        const string source = "sequenceDiagram\nparticipant A\nparticipant B\n" +
            "par Primary\nA->>B: Send\nand Optional response\nNote over A,B: Nothing to send\nend\nB-->>A: After fragment";
        var parsed = new MermaidParser().ParseSequence(source);
        Assert.Empty(parsed.Diagnostics);
        var prepared = SequencePreparedCompiler.PrepareDefault(parsed.Document!.ToSequenceArtifact());
        var block = Assert.Single(prepared.Regions, region => region.Role == "sequence-block");
        var branch = Assert.Single(prepared.Regions, region => region.Role == "sequence-branch" && region.Label == "Optional response");
        var note = Assert.Single(prepared.Regions, region => region.Role == "sequence-note");
        var after = Assert.Single(prepared.Regions, region => region.Role == "sequence-message" && region.Label == "After fragment");
        Assert.InRange(branch.Bounds.Bottom, block.Bounds.Top, block.Bounds.Bottom);
        Assert.InRange(note.Bounds.Bottom, block.Bounds.Top, block.Bounds.Bottom);
        Assert.True(block.Bounds.Bottom <= after.Bounds.Top,
            "The owning frame must close after its empty branch and note, before the next message.");
    }
}
