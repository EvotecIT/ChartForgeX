using System.Text.Json;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed partial class MermaidSequenceParOverTests {
    internal static string Fixture() => File.ReadAllText(Path.Combine(TestRepository.Root, "ChartForgeX.Tests", "Fixtures", "Mermaid", "sequence-par-over.mmd"));
    internal static string Source(bool dark) => (dark ? "%%{init: {'theme':'dark'}}%%\n" : "") + Fixture();

    [Fact]
    public void AuthoredParOverRetainsItsDistinctTypedAndPortableKindThroughExistingFragmentOwners() {
        var source = Fixture();
        using var expected = JsonDocument.Parse(File.ReadAllText(Path.Combine(TestRepository.Root, "ChartForgeX.Tests", "Fixtures", "Mermaid", "sequence-par-over.expected.json")));
        var parsed = new MermaidParser().ParseSequence(source);
        Assert.Empty(parsed.Diagnostics);
        var document = parsed.Document!;
        var authored = Assert.Single(document.Blocks, block => block.Kind == MermaidSequenceBlockKind.ParOver);
        Assert.Equal(expected.RootElement.GetProperty("sourceParOverLine").GetInt32(), authored.Span.Line);
        Assert.Equal("First task", authored.Text); Assert.Equal(0, authored.Depth);
        Assert.Equal(source, document.SourceText);
        var sequence = document.ToSequenceArtifact();
        Assert.Equal(expected.RootElement.GetProperty("postorderBlockKinds").EnumerateArray().Select(value => value.GetString()), sequence.Blocks.Select(block => block.Kind.ToString()));
        Assert.Equal(expected.RootElement.GetProperty("messages").EnumerateArray().Select(value => value.GetString()), sequence.Messages.Select(message => message.Text));
        Assert.Equal(expected.RootElement.GetProperty("autonumberValues").EnumerateArray().Select(value => value.GetString()), sequence.Messages.Select(message => message.Metadata["mermaid.autonumber.value"]));
        var branches = sequence.Branches.Where(branch => branch.ParentKind == SequenceArtifactBlockKind.ParOver).ToArray();
        Assert.Equal(expected.RootElement.GetProperty("outerBranchLabels").EnumerateArray().Select(value => value.GetString()), branches.Select(branch => branch.Text));
        Assert.Equal(new[] { "Primary", "And", "And" }, branches.Select(branch => branch.Kind));
        Assert.True(branches.Last().IsEmpty); Assert.Null(sequence.Blocks.Last().BackgroundColor);
        var prepared = SequencePreparedCompiler.PrepareDefault(sequence);
        var envelope = prepared.ToArtifact().ToInterchangeEnvelope();
        var outer = Assert.Single(envelope.Annotations, annotation => annotation.Sequence?.BlockKind == SequenceArtifactBlockKind.ParOver);
        Assert.Equal("First task", outer.Text); Assert.Equal(0, outer.Sequence!.Depth);
        Assert.Equal(3, envelope.Annotations.Count(annotation => annotation.Sequence?.ParentBlockKind == SequenceArtifactBlockKind.ParOver));
        var roundtrip = VisualArtifactInterchangeEnvelope.FromJson(envelope.ToJson());
        Assert.Equal(envelope.ToJson(), roundtrip.ToJson());
        var before = prepared.ToSvg(); var snapshotJson = prepared.ToArtifact().ToInterchangeJson();
        sequence.Blocks.Last().Kind = SequenceArtifactBlockKind.Par;
        sequence.Branches.Last().ParentKind = SequenceArtifactBlockKind.Par;
        Assert.Equal(before, prepared.ToSvg()); Assert.Equal(snapshotJson, prepared.ToArtifact().ToInterchangeJson());
        Assert.Contains(prepared.Scene.Nodes.OfType<VisualSceneText>(), text => text.Text.Lines.Any(line => line.Text == "par  First task"));
        Assert.DoesNotContain(prepared.Scene.Nodes.OfType<VisualSceneText>(), text => text.Text.Lines.Any(line => line.Text.Contains("parover", StringComparison.OrdinalIgnoreCase)));
        var intervals = prepared.Regions.Where(region => region.Role == "sequence-activation" && region.Label?.StartsWith("activate ", StringComparison.Ordinal) == true).ToArray();
        var endings = prepared.Regions.Where(region => region.Role == "sequence-activation" && region.Label?.StartsWith("deactivate ", StringComparison.Ordinal) == true).ToArray();
        Assert.Equal(expected.RootElement.GetProperty("activationBars").GetInt32(), intervals.Length);
        Assert.Equal(sequence.Messages.Count(message => message.ActivatesTarget), intervals.Length);
        Assert.Equal(sequence.Messages.Count(message => message.Deactivates), endings.Length);
        Assert.All(intervals, interval => Assert.True(interval.Bounds.Height > 4));
        Assert.All(endings, ending => Assert.Equal(4, ending.Bounds.Height));
        Assert.Equal(intervals.Length + endings.Length, prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Count(rect => rect.Role == "sequence-activation-bar"));
        Assert.True(prepared.ToPng().Length > 64);
    }

    [Fact]
    public void NestedFramesBranchesAndTerminalNoteStayInsideTheParOverFrameBeforeTheFollowingMessage() {
        var rendered = MermaidRenderer.Render(Fixture()); Assert.Empty(rendered.Diagnostics);
        var prepared = SequencePreparedCompiler.PrepareDefault(Assert.IsType<SequenceArtifact>(rendered.Artifact!.Model));
        var frame = Assert.Single(prepared.Regions, region => region.Role == "sequence-block" && region.Label == "First task");
        var terminal = Assert.Single(prepared.Regions, region => region.Role == "sequence-note" && region.Label == "Terminal task note");
        var emptyBranch = Assert.Single(prepared.Regions, region => region.Role == "sequence-branch" && region.Label == "Notes only");
        var second = Assert.Single(prepared.Regions, region => region.Role == "sequence-branch" && region.Label == "Second task");
        var critical = Assert.Single(prepared.Regions, region => region.Role == "sequence-block" && region.Label == "Guard");
        var nested = Assert.Single(prepared.Regions, region => region.Role == "sequence-block" && region.Label == "Nested fanout");
        var after = Assert.Single(prepared.Regions, region => region.Role == "sequence-message" && region.Label == "After fragment");
        foreach (var item in new[] { terminal, emptyBranch, critical, nested }) {
            Assert.InRange(item.Bounds.Top, frame.Bounds.Top, frame.Bounds.Bottom);
            Assert.InRange(item.Bounds.Bottom, frame.Bounds.Top, frame.Bounds.Bottom);
        }
        Assert.True(second.Bounds.Top < critical.Bounds.Top);
        Assert.True(frame.Bounds.Bottom <= after.Bounds.Top);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SourceAndDirectNativeParOverShareTheExistingParallelGeometryAndNullDefaults(bool dark) {
        var source = Source(dark); var ordinary = source.Replace("par_over ", "par ", StringComparison.Ordinal);
        var direct = MermaidRenderer.Render(source); var control = MermaidRenderer.Render(ordinary);
        Assert.Empty(direct.Diagnostics); Assert.Empty(control.Diagnostics);
        Assert.Equal(control.Artifact!.ToSvg(), direct.Artifact!.ToSvg());
        Assert.Equal(control.Artifact!.ToPng(), direct.Artifact!.ToPng());
        var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.Empty(fenced.Diagnostics); Assert.Equal(direct.Artifact!.ToSvg(), Assert.Single(fenced.Artifacts).ToSvg());
        var native = Native(SequenceArtifactBlockKind.ParOver); var nativeControl = Native(SequenceArtifactBlockKind.Par);
        native.ThemeMode = nativeControl.ThemeMode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light;
        Assert.Null(native.Blocks.Single().BackgroundColor);
        Assert.Equal(nativeControl.ToSvg(), native.ToSvg());
        Assert.Equal(SequenceArtifactBlockKind.ParOver, native.ToVisualArtifact().ToInterchangeEnvelope().Annotations.Single(annotation => annotation.Role == VisualArtifactInterchangeAnnotationRole.SequenceBlock).Sequence!.BlockKind);
    }

    [Theory]
    [InlineData("par_over Work\nelse Wrong\nend", 3)]
    [InlineData("par_over Work\noption Wrong\nend", 3)]
    [InlineData("par_over Outer\nloop Inner\nand Wrong\nend\nend", 4)]
    [InlineData("par_over Unclosed\nA->>B: Message", 2)]
    public void ParOverStillRequiresBalancedBlocksAndImmediateMatchingBranchOwners(string body, int line) {
        var source = "sequenceDiagram\n" + body;
        var rendered = MermaidRenderer.Render(source);
        Assert.True(rendered.HasErrors); Assert.Null(rendered.Artifact);
        Assert.Contains(rendered.Diagnostics, diagnostic => diagnostic.Severity == MermaidDiagnosticSeverity.Error && diagnostic.Span.Line == line);
        var fence = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.True(fence.HasErrors); Assert.Empty(fence.Artifacts);
    }

    private static SequenceArtifact Native(SequenceArtifactBlockKind kind) => SequenceArtifact.Create("native-par")
        .AddParticipant("A").AddParticipant("B").AddMessage("A", "B", "Request").AddMessage("B", "A", "Reply")
        .AddBlock(kind, "First", 0, 1).AddBranch(kind, "Primary", "First", 0, 0).AddBranch(kind, "And", "Second", 1, 1);
}
