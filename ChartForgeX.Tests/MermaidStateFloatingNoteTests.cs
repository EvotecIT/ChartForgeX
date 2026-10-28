using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidStateFloatingNoteTests {
    [Fact]
    public void PreviouslyRetainedFixtureNowHasOneVisibleNamedAnnotationAndNoInventedState() {
        var path = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures", "state-floating-note");
        var source = File.ReadAllText(path + ".mmd"); var rendered = MermaidRenderer.Render(source);
        Assert.Empty(rendered.Diagnostics);
        Assert.NotNull(MermaidRenderer.Render(source, new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject }).Artifact);
        using var expected = JsonDocument.Parse(File.ReadAllText(path + ".expected.json"));
        var document = Assert.IsType<MermaidStateDocument>(rendered.Document);
        Assert.Empty(document.Statements); Assert.Empty(document.RawStatements); Assert.Equal(source, document.SourceText);
        var note = Assert.Single(document.Notes); var expectedNote = Assert.Single(expected.RootElement.GetProperty("nativeNotes").EnumerateArray());
        Assert.Equal(expectedNote.GetProperty("id").GetString(), note.Id);
        Assert.Equal(expectedNote.GetProperty("text").GetString(), note.Text); Assert.Null(note.NodeId);
        Assert.Equal((4, 1, "note \"Remember: A --> Phantom\" as N".Length), (note.Span.Line, note.Span.Column, note.Span.Length));
        var chart = Assert.IsType<TopologyChart>(rendered.Artifact!.Model);
        Assert.Equal(new[] { "A", "B", "C", "direction" }, chart.Nodes.Select(node => node.Id));
        Assert.Equal(("N", (string?)null, TopologyAnnotationSide.Right), (Assert.Single(chart.Annotations).Id, chart.Annotations[0].TargetId, chart.Annotations[0].Side));
        Assert.DoesNotContain(chart.Nodes, node => node.Id is "N" or "Phantom"); Assert.Equal(2, chart.Edges.Count);
        var xml = XDocument.Parse(rendered.Artifact.ToSvg());
        Assert.Single(xml.Descendants(), item => Role(item) == "topology-annotation");
        Assert.DoesNotContain(xml.Descendants(), item => Role(item) == "topology-annotation-leader");
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(rendered.Artifact.ToInterchangeJson());
        Assert.Equal("N", Assert.Single(envelope.Annotations).Id); Assert.Empty(envelope.Annotations[0].TargetIds); Assert.Equal(note.Text, envelope.Annotations[0].Text);
        var fence = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.Empty(fence.Diagnostics); var fenced = Assert.Single(fence.Artifacts);
        Assert.Equal(rendered.Artifact.ToSvg(), fenced.ToSvg()); Assert.Equal(rendered.Artifact.ToPng(), fenced.ToPng());
    }

    [Fact]
    public void QuotedBodiesKeepSyntaxWordsEscapesAndHtmlLiteralAtTheirOriginalSpan() {
        const string statement = "note \"Remember: A --> Phantom\\naccTitle: literal\\n<svg onload='ignored'>\\nA \\\"quote\\\" and \\\\path\" as N";
        const string source = "stateDiagram-v2\nA --> B\n  " + statement + " %% outside comment";
        var rendered = MermaidRenderer.Render(source); Assert.Empty(rendered.Diagnostics);
        var document = Assert.IsType<MermaidStateDocument>(rendered.Document); var note = Assert.Single(document.Notes);
        Assert.Equal("Remember: A --> Phantom\naccTitle: literal\n<svg onload='ignored'>\nA \"quote\" and \\path", note.Text);
        Assert.Equal((3, 3, statement.Length), (note.Span.Line, note.Span.Column, note.Span.Length));
        Assert.Equal(statement, source.Split('\n')[2].Substring(note.Span.Column - 1, note.Span.Length));
        Assert.Equal(new[] { "A", "B" }, document.States.Select(state => state.Id)); Assert.Single(document.Transitions); Assert.Null(document.Accessibility.Name);
        var artifact = Assert.IsType<VisualArtifact>(rendered.Artifact);
        var xml = XDocument.Parse(artifact.ToSvg());
        Assert.DoesNotContain(xml.Root!.Descendants(), element => element.Name.LocalName is "svg" or "script" or "image" or "foreignObject");
        Assert.DoesNotContain(xml.Descendants().Attributes(), attribute => attribute.Name.LocalName.StartsWith("on", StringComparison.OrdinalIgnoreCase));
        Assert.NotEmpty(artifact.ToPng());
    }

    [Fact]
    public void FourArgumentNotesKeepTheirAutomaticIdentityAndTheNewApiPreservesAuthoredIdentity() {
        var span = new MermaidSourceSpan(2, 3, 12);
        var legacy = new MermaidDiagramNote("Attached", "A", true, span); Assert.Null(legacy.Id);
        var document = new MermaidClassDocument { Kind = MermaidDiagramKind.Class, Header = "classDiagram" }; document.Classes.Add(new MermaidClassNode("A", span)); document.Notes.Add(legacy);
        var attached = Assert.Single(document.ToTopologyChart().Annotations);
        Assert.Equal(("mermaid-note-0", "A", TopologyAnnotationSide.Left), (attached.Id, attached.TargetId, attached.Side));
        var state = new MermaidStateDocument { Kind = MermaidDiagramKind.State, Header = "stateDiagram-v2" }; state.Notes.Add(new MermaidDiagramNote("Independent", null, false, span, "Authored"));
        var artifact = state.ToVisualArtifact(); var chart = Assert.IsType<TopologyChart>(artifact.Model);
        Assert.Empty(chart.Nodes); Assert.Empty(chart.Edges); Assert.Empty(chart.Groups);
        Assert.Equal("Authored", Assert.Single(chart.Annotations).Id); Assert.Null(chart.Annotations[0].TargetId);
        Assert.NotEmpty(artifact.ToSvg()); Assert.NotEmpty(artifact.ToPng());
        Assert.Equal("Authored", Assert.Single(VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson()).Annotations).Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\n")]
    public void AuthoredApiIdentityCannotBeBlank(string id) =>
        Assert.Throws<ArgumentException>(() => new MermaidDiagramNote("Text", null, false, new MermaidSourceSpan(1, 1, 1), id));

    [Fact]
    public void AuthoredIdsAreReservedBeforeGeneratedNotesRegionsAndEdgesWithoutChangingGraphFacts() {
        const string source = "stateDiagram-v2\nstate Active {\nA --> B\n--\nC --> D\n}\nnote right of A : Attached\nnote \"reserved note\" as mermaid-note-0\nnote \"reserved region\" as mermaid-region-Active-0\nnote \"reserved edge\" as mermaid-edge-0";
        var rendered = MermaidRenderer.Render(source); Assert.Empty(rendered.Diagnostics);
        var document = Assert.IsType<MermaidStateDocument>(rendered.Document); var chart = Assert.IsType<TopologyChart>(rendered.Artifact!.Model);
        Assert.Equal(new[] { "A", "B", "C", "D" }, chart.Nodes.Select(node => node.Id)); Assert.Equal(2, chart.Edges.Count);
        Assert.Equal(2, chart.Groups.Count(group => group.Presentation == TopologyGroupPresentation.Compartment));
        var named = document.Notes.Where(note => note.Id != null).Select(note => note.Id!).ToArray();
        Assert.Equal(named, chart.Annotations.Where(note => note.TargetId == null).Select(note => note.Id));
        Assert.All(named, id => { Assert.DoesNotContain(chart.Groups, group => group.Id == id); Assert.DoesNotContain(chart.Edges, edge => edge.Id == id); });
        var ids = chart.Nodes.Select(node => node.Id).Concat(chart.Groups.Select(group => group.Id)).Concat(chart.Edges.Select(edge => edge.Id)).Concat(chart.Annotations.Select(note => note.Id)).ToArray();
        Assert.Equal(ids.Length, ids.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(document.Transitions.Select(edge => (edge.SourceId, edge.TargetId)), chart.Edges.Select(edge => (edge.SourceNodeId, edge.TargetNodeId)));
        var facts = VisualArtifactInterchangeEnvelope.FromJson(rendered.Artifact.ToInterchangeJson());
        Assert.Equal(chart.Annotations.Select(note => note.Id), facts.Annotations.Select(note => note.Id));
        Assert.All(facts.Groups.Where(group => group.Topology!.Presentation == TopologyGroupPresentation.Compartment), group => Assert.Equal("Active", group.Topology!.ParentGroupId));
    }

    [Theory]
    [InlineData("note \"unclosed", 3)]
    [InlineData("note \"text\"", 3)]
    [InlineData("note \"text\" as", 3)]
    [InlineData("note \"text\" as N extra", 3)]
    [InlineData("note \"text\" as \"N\"", 3)]
    [InlineData("note \"text\" as N:::styled", 3)]
    [InlineData("note \"first\" as N\nnote \"second\" as N", 4)]
    [InlineData("note \"collision\" as A", 3)]
    [InlineData("note \"collision\" as Active\nstate Active {\nC\n}", 3)]
    [InlineData("note \"not a transition target\" as N\nN --> A", 3)]
    public void MalformedOrCollidingNotesFailWithLocatedErrorsAndNoPartialArtifact(string statement, int line) {
        var source = "stateDiagram-v2\nA --> B\n  " + statement;
        var rendered = MermaidRenderer.Render(source); Assert.True(rendered.HasErrors); Assert.Null(rendered.Artifact);
        Assert.Contains(rendered.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidStatement && item.Span.Line == line && item.Severity == MermaidDiagnosticSeverity.Error);
        var fence = new MermaidVisualMarkupParser().Parse("Intro\n\n```mermaid\n" + source + "\n```");
        Assert.Empty(fence.Artifacts); Assert.True(fence.HasErrors); Assert.Contains(fence.Diagnostics, item => item.Line == line + 3);
    }

    [Fact]
    public void NotesParticipateInCapturedModelAndDeferredExportLimitsAndCancellation() {
        const string source = "stateDiagram-v2\nnote \"One\" as N\nnote \"Two\" as M";
        var limits = new MermaidResourceLimits { MaximumElements = 1 };
        var options = new MermaidRenderOptions { ParseOptions = new MermaidParseOptions { ResourceLimits = limits } };
        var rejected = MermaidRenderer.Render(source, options); Assert.Null(rejected.Artifact);
        Assert.Contains(rejected.Diagnostics, item => item.Code == MermaidDiagnosticCodes.ResourceLimitExceeded && item.Span.Line == 3);
        var fence = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```"); Assert.Empty(fence.Artifacts); Assert.True(fence.HasErrors);
        limits.MaximumElements = 2; var accepted = MermaidRenderer.Render(source, options); Assert.Empty(accepted.Diagnostics);
        Assert.Empty(Assert.IsType<MermaidStateDocument>(accepted.Document).States); Assert.Equal(2, Assert.IsType<TopologyChart>(accepted.Artifact!.Model).Annotations.Count);
        limits.MaximumOutputBytes = 32; limits.MaximumRasterPixels = 1;
        var bounded = MermaidRenderer.Render(source, options); Assert.Empty(bounded.Diagnostics);
        limits.MaximumOutputBytes = 1_000_000; limits.MaximumRasterPixels = 1_000_000;
        Assert.Equal(nameof(limits.MaximumOutputBytes), Assert.Throws<MermaidResourceLimitException>(() => bounded.Artifact!.ToSvg()).LimitName);
        Assert.Equal(nameof(limits.MaximumRasterPixels), Assert.Throws<MermaidResourceLimitException>(() => bounded.Artifact!.ToPng()).LimitName);
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => new MermaidParser().Parse(source, null, cancellation.Token));
    }

    [Fact]
    public void SourceAndNativeEditsRetainNoteIdentityAndDetachedPortableSnapshots() {
        const string source = "stateDiagram-v2\nA --> B\nnote \"Original\" as N";
        var rendered = MermaidRenderer.Render(source); var artifact = rendered.Artifact!;
        var saved = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson()); var json = saved.ToJson(); var svg = artifact.ToSvg(); var png = artifact.ToPng();
        var document = Assert.IsType<MermaidStateDocument>(rendered.Document); var original = Assert.Single(document.Notes);
        document.Notes[0] = new MermaidDiagramNote("Edited caption", null, false, original.Span, original.Id);
        var edited = document.ToVisualArtifact(); Assert.NotEqual(svg, edited.ToSvg()); Assert.False(png.SequenceEqual(edited.ToPng()));
        Assert.Equal(svg, artifact.ToSvg()); Assert.Equal(png, artifact.ToPng());
        var editedFacts = VisualArtifactInterchangeEnvelope.FromJson(edited.ToInterchangeJson());
        Assert.Equal(("N", "Edited caption"), (Assert.Single(editedFacts.Annotations).Id, editedFacts.Annotations[0].Text));
        Assert.Equal(source, document.SourceText); Assert.Equal(original.Span, document.Notes[0].Span); Assert.Equal(json, saved.ToJson());
        var chart = Assert.IsType<TopologyChart>(edited.Model); chart.Annotations[0] = new TopologyAnnotation("N", "Native edit");
        Assert.Equal("Native edit", Assert.Single(VisualArtifactInterchangeEnvelope.FromJson(edited.ToInterchangeJson()).Annotations).Text);
        Assert.Equal("Original", Assert.Single(saved.Annotations).Text);
    }

    [Fact]
    public void UnsupportedSyntaxAndNoteStyleTargetsRemainLocatedWithoutManufacturingStatesOrCss() {
        const string source = "stateDiagram-v2\nA\nnote \"Literal\" as N\nclassDef painted fill:#ff0000\nclass N painted\nnote over A : Unsupported";
        var rendered = MermaidRenderer.Render(source); Assert.False(rendered.HasErrors); Assert.Equal(2, rendered.Diagnostics.Count);
        Assert.All(rendered.Diagnostics, item => Assert.Equal(MermaidDiagnosticCodes.UnsupportedStatement, item.Code));
        var chart = Assert.IsType<TopologyChart>(rendered.Artifact!.Model); Assert.Equal("A", Assert.Single(chart.Nodes).Id); Assert.Equal("N", Assert.Single(chart.Annotations).Id);
        var surface = Assert.Single(XDocument.Parse(rendered.Artifact.ToSvg()).Descendants(), element => Role(element) == "topology-annotation-surface");
        Assert.NotEqual("#ff0000", (string?)surface.Attribute("fill"));
        Assert.Null(MermaidRenderer.Render(source, new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject }).Artifact);
    }

    internal static string Source => File.ReadAllText(Path.Combine(TestRepository.Root, "ChartForgeX.Examples", "Assets", "state-floating-notes.mmd"));
    internal static MermaidRenderOptions Options(bool dark, int width) => new() {
        Presentation = new MermaidPresentationOptions { FontFamily = BrowserProofFonts.Family, FontSize = 12, ThemeMode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light, TopologySizing = MermaidTopologySizing.Natural },
        State = new MermaidTopologyRenderOptions { Width = width, Height = 680 }
    };
    internal static string? Role(XElement element) => (string?)element.Attribute("data-cfx-role");
}
