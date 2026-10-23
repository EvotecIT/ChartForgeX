using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidZenUmlTests {
    // Original fixtures based on documented syntax, not copied upstream parser code:
    // https://github.com/mermaid-js/zenuml-core/blob/main/docs/DSL_SYNTAX.md
    [Fact]
    public void OccurrencesRetainCallerReturnCreationAndBranchSemanticsInTheNativeModel() {
        var parsed = new MermaidParser().Parse(Fixture("zenuml-native-occurrences"));
        Assert.Empty(parsed.Diagnostics);
        var document = Assert.IsType<MermaidZenUmlDocument>(parsed.Document);
        Assert.False(document.IsDiagnosticOnly);
        Assert.Equal("Checkout occurrences", document.Title);
        Assert.Equal("User", document.StarterId);
        Assert.Equal(new[] { "User", "Service", "Store", "Bus", "Receipt" }, document.Participants.Select(item => item.Id));
        Assert.Equal(new[] { "User", "Service" }, Assert.Single(document.ParticipantGroups).ParticipantIds);
        var checkout = document.Statements[0];
        Assert.Equal(("User", "Service", "checkout(order)"), (checkout.CallerId, checkout.TargetId, checkout.Text));
        Assert.Equal("Accept the request", Assert.Single(checkout.Comments).Text);
        var creation = checkout.Children[0].Children[0];
        Assert.Equal(MermaidZenUmlStatementKind.Create, creation.Kind);
        Assert.Equal(("Service", "Receipt", "String receipt"), (creation.CallerId, creation.TargetId, creation.Assignment));
        Assert.Equal(("Receipt", "Store"), (creation.Children[0].CallerId, creation.Children[0].TargetId));
        Assert.Equal(("Receipt", "Service", "saved"), (creation.Children[1].CallerId, creation.Children[1].TargetId, creation.Children[1].Text));
        var sequence = document.ToSequenceArtifact();
        Assert.Equal(14, sequence.Messages.Count);
        Assert.Equal(6, sequence.Blocks.Count);
        Assert.Single(sequence.Blocks, item => item.Kind == SequenceArtifactBlockKind.Try);
        Assert.DoesNotContain(sequence.Blocks, item => item.Kind == SequenceArtifactBlockKind.Break);
        Assert.Equal(new[] { "Primary", "else", "Primary", "catch", "finally" }, sequence.Branches.Select(item => item.Kind));
        Assert.Equal(1, sequence.Participants.Single(item => item.Id == "Receipt").CreatedAtMessageIndex);
        Assert.True(sequence.Messages[1].ActivatesTarget);
        Assert.DoesNotContain(sequence.Activations, item => item.ParticipantId == "Receipt" && item.Active && item.StepIndex == 1);
        Assert.Equal(("Receipt", "Service", SequenceArtifactMessageKind.Return),
            (sequence.Messages[3].SourceId, sequence.Messages[3].TargetId, sequence.Messages[3].Kind));
        Assert.Equal(("Service", "User", SequenceArtifactMessageKind.Return),
            (sequence.Messages[13].SourceId, sequence.Messages[13].TargetId, sequence.Messages[13].Kind));
        Assert.Equal(2, sequence.Messages.Count(item => item.Kind == SequenceArtifactMessageKind.Async));
        Assert.Equal((0, 1), (sequence.ParticipantGroups[0].FirstParticipantIndex, sequence.ParticipantGroups[0].LastParticipantIndex));
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(document.ToVisualArtifact().ToInterchangeJson());
        Assert.Equal(14, envelope.Edges.Count);
        Assert.Equal(1, envelope.Nodes.Single(item => item.Label == "Receipt").Sequence!.CreatedAtMessageIndex);
        Assert.Equal(SequenceArtifactMessageKind.Return, envelope.Edges[3].Sequence!.Kind);
        Assert.Equal("Create", envelope.Edges[1].Extensions["mermaid.zenuml.kind"]);
        Assert.Single(envelope.Annotations, item => item.Sequence?.BlockKind == SequenceArtifactBlockKind.Try);
        Assert.Equal(3, envelope.Annotations.Count(item => item.Sequence?.ParentBlockKind == SequenceArtifactBlockKind.Try));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ActualDirectAndFencedNativeSvgPngKeepTheSameOccurrenceFactsAndPaint(bool dark) {
        var source = (dark ? "%%{init: {'theme':'dark'}}%%\n" : string.Empty) + Fixture("zenuml-native-occurrences");
        var direct = MermaidRenderer.Render(source); Assert.Empty(direct.Diagnostics);
        var artifact = direct.Artifact!; var svg = artifact.ToSvg(); var png = artifact.ToPng();
        var xml = XDocument.Parse(svg);
        Assert.Equal(14, xml.Descendants().Count(item => (string?)item.Attribute("data-cfx-role") == "sequence-message"));
        Assert.Contains(xml.Descendants(), item => item.Name.LocalName == "text" && item.Value.Contains("checkout(order)", StringComparison.Ordinal));
        Assert.DoesNotContain(xml.Descendants(), item => item.Name.LocalName is "script" or "foreignObject" or "a");
        var sequence = Assert.IsType<SequenceArtifact>(artifact.Model);
        Assert.Equal(string.Empty, Assert.Single(sequence.Blocks, item => item.Kind == SequenceArtifactBlockKind.Try).Text);
        var facts = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson());
        var tryFact = Assert.Single(facts.Annotations, item => item.Sequence?.BlockKind == SequenceArtifactBlockKind.Try);
        var tryGroup = Assert.Single(xml.Descendants(), item => (string?)item.Attribute("data-cfx-role") == "sequence-block"
            && (string?)item.Attribute("data-cfx-source-id") == tryFact.Id);
        Assert.Equal(string.Empty, (string?)tryGroup.Attribute("data-label"));
        Assert.Contains(tryGroup.Descendants(), item => item.Name.LocalName == "text" && item.Value.Trim() == "try");
        var branchGroups = xml.Descendants().Where(item => (string?)item.Attribute("data-cfx-role") == "sequence-branch").ToArray();
        Assert.Contains(branchGroups, group => (string?)group.Attribute("data-label") == "StorageError error"
            && group.Descendants().Any(item => item.Name.LocalName == "text" && item.Value.Contains("catch", StringComparison.Ordinal)
                && item.Value.Contains("StorageError error", StringComparison.Ordinal)));
        Assert.Contains(branchGroups, group => (string?)group.Attribute("data-label") == string.Empty
            && group.Descendants().Any(item => item.Name.LocalName == "text" && item.Value.Trim() == "finally"));
        Assert.DoesNotContain(sequence.Blocks, item => item.Kind == SequenceArtifactBlockKind.Break);
        Assert.True(png.Length > 64);
        var fence = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.Empty(fence.Diagnostics);
        var fenced = Assert.Single(fence.Artifacts);
        Assert.Equal(svg, fenced.ToSvg()); Assert.Equal(png, fenced.ToPng());
        Capture("zenuml-occurrences" + (dark ? "-dark" : "-light"), svg, png);
    }

    [Fact]
    public void RootLocalCallsAndNestedLocalCallsUseTheirActualContextWithoutInventingMethodParticipants() {
        var result = MermaidRenderer.Render(Fixture("zenuml-native-local-context")); Assert.Empty(result.Diagnostics);
        var document = Assert.IsType<MermaidZenUmlDocument>(result.Document);
        Assert.Equal("A", Assert.Single(document.Participants).Id);
        var rootLocal = Assert.Single(document.Statements[0].Children);
        Assert.Null(rootLocal.CallerId); Assert.Null(rootLocal.TargetId);
        var sequence = Assert.IsType<SequenceArtifact>(result.Artifact!.Model);
        var starter = sequence.Participants.Single(item => item.Metadata.ContainsKey("mermaid.zenuml.external"));
        Assert.Equal(6, sequence.Messages.Count);
        Assert.Equal((starter.Id, starter.Id), (sequence.Messages[0].SourceId, sequence.Messages[0].TargetId));
        Assert.Equal("true", sequence.Messages[0].Metadata["mermaid.zenuml.external-context"]);
        Assert.Equal((starter.Id, starter.Id, SequenceArtifactMessageKind.Return),
            (sequence.Messages[2].SourceId, sequence.Messages[2].TargetId, sequence.Messages[2].Kind));
        Assert.Equal(("A", "A"), (sequence.Messages[4].SourceId, sequence.Messages[4].TargetId));
        Assert.NotEmpty(result.Artifact.ToPng());
    }

    [Fact]
    public void ExplicitAndImplicitStarterIdentitiesCannotCollideAndAssignmentsRetainTheirReturnMeaning() {
        const string source = "zenuml\nparticipant zenuml-starter\nvalue = A.compute(input=2)\nA -> zenuml-starter: sent";
        var result = MermaidRenderer.Render(source); Assert.Empty(result.Diagnostics);
        var sequence = Assert.IsType<SequenceArtifact>(result.Artifact!.Model);
        var external = sequence.Participants.Single(item => item.Metadata.ContainsKey("mermaid.zenuml.external"));
        Assert.NotEqual("zenuml-starter", external.Id);
        Assert.Equal(3, sequence.Participants.Count);
        Assert.Equal("value = compute(input=2)", sequence.Messages[0].Text);
        Assert.Equal(("A", external.Id, "value", SequenceArtifactMessageKind.Return),
            (sequence.Messages[1].SourceId, sequence.Messages[1].TargetId, sequence.Messages[1].Text, sequence.Messages[1].Kind));
        Assert.Equal(("A", "zenuml-starter"), (sequence.Messages[2].SourceId, sequence.Messages[2].TargetId));
        Assert.NotEmpty(result.Artifact.ToPng());
    }

    [Theory]
    [InlineData("return orphan", 2)]
    [InlineData("else { A.call() }", 2)]
    [InlineData("catch (Error error) { A.call() }", 2)]
    [InlineData("A.call() {", 2)]
    [InlineData("}", 2)]
    [InlineData("A.call() B.second()", 2)]
    [InlineData("A.call()\nnew A()", 3)]
    [InlineData("new A()\nnew A()", 3)]
    [InlineData("A.call()\ntitle Too late", 3)]
    [InlineData("if (ready) { A.call() } else { B.call() } else { C.call() }", 2)]
    public void InvalidScopesAndLifetimesAreLocatedBeforeEitherHostCanCreateAnArtifact(string body, int line) {
        var source = "zenuml\n" + body;
        var parsed = new MermaidParser().Parse(source); Assert.True(parsed.HasErrors);
        Assert.Contains(parsed.Diagnostics, item => item.Span.Line == line && item.Code == MermaidDiagnosticCodes.InvalidStatement);
        Assert.Null(MermaidRenderer.Render(source).Artifact);
        var fence = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.True(fence.HasErrors); Assert.Empty(fence.Artifacts);
    }

    [Fact]
    public void UnsupportedAuthoredPresentationAndReferenceDividersRemainVisibleDiagnostics() {
        const string source = "zenuml\nparticipant <<Service>> [rocket] API 160 as \"API service\" #336699\nA.call()\nref(Login)\n== Next phase ==";
        var parsed = new MermaidParser().Parse(source); Assert.False(parsed.HasErrors);
        Assert.Equal(6, parsed.Diagnostics.Count);
        Assert.All(parsed.Diagnostics, item => Assert.Equal(MermaidDiagnosticCodes.UnsupportedStatement, item.Code));
        var document = Assert.IsType<MermaidZenUmlDocument>(parsed.Document);
        var api = document.Participants[0];
        Assert.Equal(("API", "API service", "160", "#336699"), (api.Id, api.Alias, api.Attributes["width"], api.Attributes["color"]));
        var sequence = document.ToSequenceArtifact();
        Assert.Contains(sequence.Notes, item => item.Text == "ref: Login");
        Assert.Contains(sequence.Notes, item => item.Text == "Next phase");
        var strict = MermaidRenderer.Render(source, new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject });
        Assert.True(strict.HasErrors); Assert.Null(strict.Artifact);
    }

    [Fact]
    public void AsyncPayloadAndArgumentsRemainInertTextRatherThanStructureOrExecutableMetadata() {
        const string source = "zenuml\n用户 -> 系统: {\"payload\": \"don't execute()\"}\n系统.validate(url=\"https://example.invalid/a\", predicate=call(2))\n系统 --> 用户: done";
        var result = MermaidRenderer.Render(source); Assert.Empty(result.Diagnostics);
        var sequence = Assert.IsType<SequenceArtifact>(result.Artifact!.Model);
        Assert.Equal(new[] { "用户", "系统" }, sequence.Participants.Select(item => item.Id));
        Assert.Equal("{\"payload\": \"don't execute()\"}", sequence.Messages[0].Text);
        Assert.Equal("validate(url=\"https://example.invalid/a\", predicate=call(2))", sequence.Messages[1].Text);
        Assert.Equal(SequenceArtifactMessageKind.Return, sequence.Messages[2].Kind);
        Assert.All(sequence.Participants, item => Assert.Null(item.Href));
        var svg = XDocument.Parse(result.Artifact.ToSvg());
        Assert.DoesNotContain(svg.Descendants(), item => item.Name.LocalName is "a" or "script");
    }

    [Fact]
    public void CapturedDepthExpansionOutputAndPixelPoliciesApplyToTheNewFrontend() {
        var deep = new MermaidParser().Parse("zenuml\nA.one() { B.two() { C.three() } }", new MermaidParseOptions {
            ResourceLimits = new MermaidResourceLimits { MaximumNestingDepth = 1 }
        });
        Assert.Contains(deep.Diagnostics, item => item.Code == MermaidDiagnosticCodes.ResourceLimitExceeded);
        var expanded = MermaidRenderer.Render("zenuml\nresult = A.compute()", new MermaidRenderOptions {
            ParseOptions = new MermaidParseOptions { ResourceLimits = new MermaidResourceLimits { MaximumEdges = 1 } }
        });
        Assert.True(expanded.HasErrors); Assert.Null(expanded.Artifact);
        Assert.Contains(expanded.Diagnostics, item => item.Code == MermaidDiagnosticCodes.ResourceLimitExceeded);
        var output = MermaidRenderer.Render("zenuml\nA.work()", new MermaidRenderOptions {
            ParseOptions = new MermaidParseOptions { ResourceLimits = new MermaidResourceLimits { MaximumOutputBytes = 32 } }
        }).Artifact!;
        Assert.Equal(nameof(MermaidResourceLimits.MaximumOutputBytes), Assert.Throws<MermaidResourceLimitException>(() => output.ToSvg()).LimitName);
        var pixels = MermaidRenderer.Render("zenuml\nA.work()", new MermaidRenderOptions {
            ParseOptions = new MermaidParseOptions { ResourceLimits = new MermaidResourceLimits { MaximumRasterPixels = 1 } }
        }).Artifact!;
        Assert.Equal(nameof(MermaidResourceLimits.MaximumRasterPixels), Assert.Throws<MermaidResourceLimitException>(() => pixels.ToPng()).LimitName);
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        Assert.Throws<OperationCanceledException>(() => new MermaidParser().Parse("zenuml\nA.work()", null, cancel.Token));
    }

    [Fact]
    public void HostAuthoredCyclesAreRejectedBeforeNativeTraversal() {
        var document = new MermaidZenUmlDocument(); var span = new MermaidSourceSpan(1, 1, 1);
        document.Participants.Add(new MermaidZenUmlParticipant("A", span));
        var call = new MermaidZenUmlStatement(MermaidZenUmlStatementKind.Call, "work()", span) { CallerId = "A", TargetId = "A" };
        call.Children.Add(call); document.Statements.Add(call);
        Assert.ThrowsAny<ArgumentException>(() => document.ToSequenceArtifact());
    }

    private static string Fixture(string name) => File.ReadAllText(Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "external-plugins", "zenuml", name + ".mmd"));
    private static void Capture(string name, string svg, byte[] png) {
        var root = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(root)) return;
        Directory.CreateDirectory(root); File.WriteAllText(Path.Combine(root, name + ".native.svg"), svg); File.WriteAllBytes(Path.Combine(root, name + ".native.png"), png);
    }
}
