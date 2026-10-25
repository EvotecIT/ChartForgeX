using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed partial class MermaidSequenceLifecycleTests {
    // Uses official participant box and create/destroy message notation:
    // https://mermaid.js.org/syntax/sequenceDiagram.html#actor-creation-and-destruction-v10-3-0
    internal const string Source = "sequenceDiagram\n" +
        "title Delivery lifecycle\n" +
        "box transparent Client\nactor U as User\nend\n" +
        "box rgba(210,230,255,0.4) Services\nparticipant API as Service\nparticipant DB as Store\nend\n" +
        "U->>API: Submit request\nAPI->>DB: Read policy\n" +
        "create participant W as Worker\nAPI->>W: Start work\n" +
        "W-->>API: Result\ndestroy W\nAPI-xW: Stop worker\nAPI-->>U: Delivered";

    [Fact]
    public void SourceSpansLifetimesAndBoxesReachTheNativeModelAndTypedInterchange() {
        var parsed = new MermaidParser().ParseSequence(Source);
        Assert.Empty(parsed.Diagnostics);
        var document = parsed.Document!;
        Assert.Equal("Delivery lifecycle", document.Title);
        Assert.Equal(new[] { (true, "W", 2, 12), (false, "W", 4, 15) },
            document.Lifetimes.Select(item => (item.Creates, item.ParticipantId, item.MessageIndex, item.Span.Line)));
        Assert.Equal(new[] { "U" }, document.ParticipantBoxes[0].ParticipantIds);
        Assert.Equal(new[] { "API", "DB" }, document.ParticipantBoxes[1].ParticipantIds);
        Assert.Equal((6, 9), (document.ParticipantBoxes[1].Span.Line, document.ParticipantBoxes[1].EndSpan!.Value.Line));
        var sequence = document.ToSequenceArtifact();
        var worker = sequence.Participants.Single(participant => participant.Id == "W");
        Assert.Equal((2, 4), (worker.CreatedAtMessageIndex, worker.DestroyedAtMessageIndex));
        Assert.Equal((1, 2), (sequence.ParticipantGroups[1].FirstParticipantIndex, sequence.ParticipantGroups[1].LastParticipantIndex));
        Assert.Equal(ChartColor.FromRgba(210, 230, 255, 102), sequence.ParticipantGroups[1].Fill);
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(document.ToVisualArtifact().ToInterchangeJson());
        Assert.Equal(2, envelope.Groups.Count);
        Assert.All(envelope.Groups, group => Assert.Equal(VisualArtifactInterchangeGroupRole.SequenceParticipantGroup, group.Role));
        Assert.Equal(envelope.Nodes.Single(node => node.Label == "Service").GroupId, envelope.Nodes.Single(node => node.Label == "Store").GroupId);
        var copiedWorker = envelope.Nodes.Single(node => node.Label == "Worker");
        Assert.Equal((2, 4), (copiedWorker.Sequence!.CreatedAtMessageIndex, copiedWorker.Sequence.DestroyedAtMessageIndex));
        var visual = SequencePreparedCompiler.PrepareDefault(sequence);
        var prepared = visual.ToArtifact().ToInterchangeEnvelope();
        var workerGeometry = prepared.Nodes.Single(node => node.Label == "Worker");
        Assert.True(workerGeometry.Y > prepared.Nodes.Single(node => node.Label == "Service").Y);
        Assert.True(workerGeometry.Y + workerGeometry.Height <= prepared.Edges[2].ResolvedRoute[0].Y);
        var workerLine = visual.Scene.Nodes.OfType<VisualSceneLine>().Where(line => line.Role == "sequence-lifeline").ElementAt(3);
        Assert.Equal(workerGeometry.Y + workerGeometry.Height, workerLine.Start.Y);
        var workerFooter=visual.Regions.Single(region=>region.Role=="sequence-participant-footer"&&region.Id=="W");
        Assert.Equal(prepared.Edges[4].ResolvedRoute.Last().Y,workerFooter.Bounds.Top+workerFooter.Bounds.Height/2);
        Assert.Equal(workerFooter.Bounds.Top, workerLine.End.Y);
        var svg = XDocument.Parse(document.ToSvg());
        Assert.Equal(2, svg.Descendants().Count(element => (string?)element.Attribute("data-cfx-role") == "sequence-participant-group"));
        Assert.DoesNotContain(svg.Descendants(), element => (string?)element.Attribute("data-cfx-role") == "sequence-destruction");
        Assert.True(document.ToPng().Length > 64);
        var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + Source + "\n```");
        Assert.Empty(fenced.Diagnostics);
        var fencedSequence = Assert.IsType<SequenceArtifact>(Assert.Single(fenced.Artifacts).Model);
        Assert.Equal(sequence.ParticipantGroups.Count, fencedSequence.ParticipantGroups.Count);
        Assert.Equal(4, fencedSequence.Participants.Single(participant => participant.Id == "W").DestroyedAtMessageIndex);
    }

    [Theory]
    [InlineData("create participant B\nB->>A: invalid", 2)]
    [InlineData("create participant B", 2)]
    [InlineData("destroy B\nA->>C: unrelated", 2)]
    [InlineData("destroy B", 2)]
    [InlineData("A->>B: already used\ncreate participant B\nA->>B: create", 2)]
    [InlineData("A->>B: first\ndestroy B\nB-->>A: final\nA->>B: too late", 5)]
    [InlineData("create participant B\nA->>B: first\ncreate participant B\nA->>B: again", 4)]
    [InlineData("A->>B: first\ndestroy B\nB-->>A: final\ndestroy B\nA->>B: again", 5)]
    [InlineData("box Client\nparticipant A", 2)]
    [InlineData("box Client\nend\nA->>B: call", 2)]
    [InlineData("box Client\nparticipant A\nA->>B: call\nend", 4)]
    public void InvalidLifetimesAndParticipantBoxesAreLocatedBeforeDirectOrFenceArtifacts(string body, int line) {
        var source = "sequenceDiagram\n" + body;
        var parsed = new MermaidParser().ParseSequence(source);
        Assert.True(parsed.HasErrors);
        Assert.Contains(parsed.Diagnostics, diagnostic => diagnostic.Span.Line == line && diagnostic.Code == MermaidDiagnosticCodes.InvalidStatement);
        Assert.Null(MermaidRenderer.Render(source).Artifact);
        var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.True(fenced.HasErrors);
        Assert.Empty(fenced.Artifacts);
    }

    [Fact]
    public void FractionalAutonumberAndExplicitTitleHaveTheSameNativeResultInBothPalettes() {
        const string source = "sequenceDiagram\ntitle: Repeated request\nautonumber 1.5 0.25\nA->>B: One\nB-->>A: Two";
        var document = new MermaidParser().ParseSequence(source).Document!;
        foreach (var mode in new[] { VisualThemeMode.Light, VisualThemeMode.Dark }) {
            var native = document.ToSequenceArtifact();
            native.ThemeMode = mode;
            var svg = native.ToSvg();
            Assert.Contains("Repeated request", svg);
            var prepared = SequencePreparedCompiler.PrepareDefault(native); var facts = prepared.ToArtifact().ToInterchangeEnvelope();
            Assert.Equal("1.5", prepared.Regions.Single(region => region.Role == "sequence-number" && region.Id == facts.Edges[0].Id).Label);
            Assert.Equal("1.75", prepared.Regions.Single(region => region.Role == "sequence-number" && region.Id == facts.Edges[1].Id).Label);
            Assert.Contains("One", svg); Assert.Contains("Two", svg);
            Assert.True(native.ToPng().Length > 64);
        }
        Assert.Equal("Caller heading", document.ToSequenceArtifact(new MermaidSequenceRenderOptions { Title = "Caller heading" }).Title);
    }

    [Fact]
    public void NativeParticipantRangesAndLifetimeIndicesRejectInvalidPublicModels() {
        var sequence = SequenceArtifact.Create("native").AddParticipant("A").AddParticipant("B").AddMessage("A", "B", "Create");
        sequence.Participants[1].CreatedAtMessageIndex = 1;
        Assert.Throws<InvalidOperationException>(() => sequence.ToSvg());
        sequence.Participants[1].CreatedAtMessageIndex = 0;
        sequence.AddParticipantGroup("invalid", "Invalid range", 1, 2);
        Assert.Throws<InvalidOperationException>(() => sequence.ToSvg());
    }

    [Theory]
    [InlineData("A->>B: Stop")]
    [InlineData("B-->>A: Final response")]
    public void DestructionEndsEitherMessageEndpointAndClosesItsActivation(string finalMessage) {
        var parsed = new MermaidParser().ParseSequence("sequenceDiagram\ncreate actor B\nA->>+B: Create\ndestroy B\n" + finalMessage);
        Assert.Empty(parsed.Diagnostics);
        var sequence = parsed.Document!.ToSequenceArtifact();
        var visual = SequencePreparedCompiler.PrepareDefault(sequence);
        var lifeline = visual.Scene.Nodes.OfType<VisualSceneLine>().Where(line => line.Role == "sequence-lifeline").ElementAt(0);
        var b = sequence.Participants[0];
        Assert.Equal("B", b.Id);
        Assert.Equal((0, 1), (b.CreatedAtMessageIndex, b.DestroyedAtMessageIndex));
        var final = visual.ToArtifact().ToInterchangeEnvelope().Edges.Last().ResolvedRoute;
        var footer=visual.Regions.Single(region=>region.Role=="sequence-participant-footer"&&region.Id=="B");
        Assert.Equal(finalMessage.StartsWith("B", StringComparison.Ordinal) ? final.First().Y : final.Last().Y, footer.Bounds.Top+footer.Bounds.Height/2);
        Assert.Equal(footer.Bounds.Top, lifeline.End.Y);
        Assert.True(visual.ToPng().Length > 64);
    }

    [Fact]
    public void NativeNotesAndActivationsRespectTheCreatedParticipantBoundary() {
        var sequence = SequenceArtifact.Create("native").AddParticipant("A").AddParticipant("B").AddMessage("A", "B", "Create");
        sequence.Participants[1].CreatedAtMessageIndex = 0;
        sequence.AddNote(SequenceArtifactNotePlacement.Over, new[] { "B" }, "Before creation");
        sequence.Notes[0].StepIndex = 0;
        Assert.Throws<InvalidOperationException>(() => sequence.ToSvg());
        sequence.Notes[0].StepIndex = 1;
        sequence.AddActivation("B", true, 0);
        Assert.Throws<InvalidOperationException>(() => sequence.ToSvg());
        sequence.Activations[0].StepIndex = 1;
        Assert.True(sequence.ToPng().Length > 64);
    }
}
