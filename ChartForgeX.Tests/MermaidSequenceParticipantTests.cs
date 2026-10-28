using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed partial class MermaidSequenceParticipantTests {
    internal static string Fixture() => File.ReadAllText(Path.Combine(TestRepository.Root, "ChartForgeX.Tests", "Fixtures", "Mermaid", "sequence-participant-contracts.mmd"));
    internal static string Source(bool dark) => (dark ? "%%{init: {'theme':'dark'}}%%\n" : "") + Fixture();

    [Fact]
    public void CompleteParticipantFixturePreservesRawDeclarationsAndReachesNativeGlyphLinksAndPortableFacts() {
        var source = Fixture(); var parsed = new MermaidParser().ParseSequence(source);
        Assert.False(parsed.HasErrors); Assert.Empty(parsed.Diagnostics);
        var document = parsed.Document!;
        using var expected = JsonDocument.Parse(File.ReadAllText(Path.Combine(TestRepository.Root, "ChartForgeX.Tests", "Fixtures", "Mermaid", "sequence-participant-contracts.expected.json")));
        Assert.Equal(source, document.SourceText);
        Assert.Equal(expected.RootElement.GetProperty("kinds").EnumerateArray().Select(item => item.GetString()), document.Participants.Select(participant => participant.Kind.ToString()));
        Assert.Equal("@{type: database, alias: Storage}", document.Participants.Single(participant => participant.Id == "D").Configuration);
        Assert.Equal(new[] { 14, 15 }, document.PropertyStatements.Select(statement => statement.Span.Line));
        Assert.Equal("{\"owner\":\"Operations\"}", document.PropertyStatements.Last().RawJson);
        Assert.Equal(new[] { 11, 12, 13 }, document.Links.Select(link => link.Span.Line));
        var sequence = document.ToSequenceArtifact();
        Assert.Equal(expected.RootElement.GetProperty("nativeLabels").EnumerateArray().Select(item => item.GetString()), sequence.Participants.Select(participant => participant.Label));
        var storage = sequence.Participants.Single(participant => participant.Id == "D");
        var queue = sequence.Participants.Single(participant => participant.Id == "Q");
        Assert.Equal("https://example.test/status", storage.Href); Assert.Null(queue.Href);
        Assert.Equal(new[]{"Reference","Status"},storage.NavigationLinks.Select(item=>item.Label));Assert.Equal(2,queue.NavigationLinks.Count);
        using var entries = JsonDocument.Parse(storage.Metadata["mermaid.menu.entries"]);
        Assert.Equal(new[] { "Reference", "Status" }, entries.RootElement.EnumerateArray().Select(entry => entry.GetProperty("label").GetString()));
        using var declarations = JsonDocument.Parse(storage.Metadata["mermaid.properties.statements"]);
        Assert.Equal(new[] { 14, 15 }, declarations.RootElement.EnumerateArray().Select(entry => entry.GetProperty("line").GetInt32()));
        using var values = JsonDocument.Parse(storage.Metadata["mermaid.properties.values"]);
        Assert.Equal("Operations", values.RootElement.EnumerateArray().Single(entry => entry.GetProperty("name").GetString() == "owner").GetProperty("value").GetString());
        Assert.Equal(JsonValueKind.Null, values.RootElement.EnumerateArray().Single(entry => entry.GetProperty("name").GetString() == "note").GetProperty("value").ValueKind);
        var prepared = SequencePreparedCompiler.PrepareDefault(sequence); var artifact = prepared.ToArtifact();
        var svg = prepared.ToSvg(); var xml = XDocument.Parse(svg);
        var headers=xml.Descendants().Where(element=>(string?)element.Attribute("data-cfx-role")=="sequence-participant").ToArray();
        Assert.Equal(7, headers.SelectMany(element=>element.Descendants()).Count(element => (string?)element.Attribute("data-cfx-role") == "sequence-notation"));
        Assert.Equal(sequence.Participants.Count,xml.Descendants().Count(element=>(string?)element.Attribute("data-cfx-role")=="sequence-participant-footer"));
        Assert.Contains(xml.Descendants(), element => (string?)element.Attribute("data-kind") == "Database");
        Assert.Single(xml.Descendants(), element => element.Name.LocalName == "a" && element.Attributes().Any(attribute => attribute.Name.LocalName == "href" && attribute.Value == storage.Href));
        Assert.DoesNotContain(xml.Descendants(), element => element.Name.LocalName is "script" or "foreignObject");
        Assert.Null(sequence.Participants.Single(participant=>participant.Kind==SequenceArtifactParticipantKind.Actor).SurfaceShadow);
        Assert.All(sequence.Participants.Where(participant=>participant.Kind!=SequenceArtifactParticipantKind.Actor),participant=>Assert.NotNull(participant.SurfaceShadow));
        Assert.Equal((sequence.Participants.Count-1)*2,xml.Descendants().Count(element=>element.Name.LocalName=="image"));
        var json = artifact.ToInterchangeJson(); var portable = VisualArtifactInterchangeEnvelope.FromJson(json);
        Assert.Equal(SequenceArtifactParticipantKind.Database, portable.Nodes.Single(node => node.Id == "D").Sequence!.Kind);
        Assert.Equal(storage.Metadata["mermaid.properties.statements"], portable.Nodes.Single(node => node.Id == "D").Extensions["mermaid.properties.statements"]);
        storage.Href = "/changed"; storage.Metadata["mermaid.menu.entries"] = "[]";
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(json, artifact.ToInterchangeJson()); Assert.True(prepared.ToPng().Length > 64);
        var direct = MermaidRenderer.Render(source); var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.False(direct.HasErrors); Assert.False(fenced.HasErrors);
        Assert.Equal(direct.Artifact!.ToSvg(), Assert.Single(fenced.Artifacts).ToSvg()); Assert.True(fenced.Artifacts[0].ToPng().Length > 64);
    }

    [Theory]
    [InlineData("participant", SequenceArtifactParticipantKind.Participant)]
    [InlineData("actor", SequenceArtifactParticipantKind.Actor)]
    [InlineData("boundary", SequenceArtifactParticipantKind.Boundary)]
    [InlineData("control", SequenceArtifactParticipantKind.Control)]
    [InlineData("entity", SequenceArtifactParticipantKind.Entity)]
    [InlineData("database", SequenceArtifactParticipantKind.Database)]
    [InlineData("collections", SequenceArtifactParticipantKind.Collections)]
    [InlineData("queue", SequenceArtifactParticipantKind.Queue)]
    public void OfficialAttachedAndSpacedMetadataUseExistingNativeKindsAndExternalAliasPrecedence(string type, SequenceArtifactParticipantKind kind) {
        var source = "sequenceDiagram\nparticipant A@{type: " + type + ", alias: Inline} as External\nparticipant B\nA->>B: Signal";
        var rendered = MermaidRenderer.Render(source); Assert.Empty(rendered.Diagnostics);
        var sequence = Assert.IsType<SequenceArtifact>(rendered.Artifact!.Model);
        Assert.Equal(kind, sequence.Participants[0].Kind); Assert.Equal("External", sequence.Participants[0].Label);
        var spaced = MermaidRenderer.Render(source.Replace("A@{", "A @{", StringComparison.Ordinal));
        Assert.Empty(spaced.Diagnostics); Assert.Equal(rendered.Artifact!.ToSvg(), spaced.Artifact!.ToSvg());
        Assert.Equal(kind, rendered.Artifact!.ToInterchangeEnvelope().Nodes[0].Sequence!.Kind);
        Assert.True(rendered.Artifact!.ToPng().Length > 64);
    }

    [Theory]
    [InlineData("links A: {\"Only\":\"/one\"}", "/one")]
    [InlineData("link A: First @ /first\nlinks A: {\"Only\":\"/one\"}", "/one")]
    [InlineData("link A: First @ /first\nlinks A: {\"One\":\"/one\",\"Two\":\"/two\"}", null)]
    [InlineData("links A: {\"One\":\"/one\",\"Two\":\"/two\"}\nlink A: Final @ /final", "/final")]
    public void AuthoredMenuDirectiveOrderOwnsOneSafeStaticHrefWithoutChoosingFromMultipleEntries(string declarations, string? expectedHref) {
        var rendered = MermaidRenderer.Render("sequenceDiagram\nparticipant A\nparticipant B\n" + declarations + "\nA->>B: Signal");
        Assert.False(rendered.HasErrors);
        var participant = Assert.IsType<SequenceArtifact>(rendered.Artifact!.Model).Participants[0];
        Assert.Equal(expectedHref, participant.Href);
        using var entries = JsonDocument.Parse(participant.Metadata["mermaid.menu.entries"]);
        Assert.True(entries.RootElement.GetArrayLength() > 0);
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(rendered.Artifact!.ToInterchangeJson());
        Assert.Equal(expectedHref, envelope.Nodes[0].Href);
    }

    [Theory]
    [InlineData("properties A: {\"class\":\"custom\"}")]
    [InlineData("properties A: {\"icon\":\"https://example.test/icon.svg\"}")]
    [InlineData("properties A: {\"structured\":{\"literal\":true}}")]
    [InlineData("details A: external-dom-id")]
    [InlineData("links A: {\"NotText\":true}")]
    [InlineData("links A: {\"Unsafe\":\"javascript:alert(1)\"}")]
    public void UnappliedPropertiesDetailsAndUnsafeNavigationStayDiagnosedAndInert(string declaration) {
        var source = "sequenceDiagram\nparticipant A\nparticipant B\n" + declaration + "\nA->>B: Signal";
        var rendered = MermaidRenderer.Render(source); Assert.False(rendered.HasErrors);
        Assert.Contains(rendered.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.UnsupportedStatement && diagnostic.Span.Line == 4);
        var xml = XDocument.Parse(rendered.Artifact!.ToSvg());
        Assert.DoesNotContain(xml.Descendants(), element => element.Name.LocalName is "script" or "foreignObject" or "a");
        var ordinary=MermaidRenderer.Render("sequenceDiagram\nparticipant A\nparticipant B\nA->>B: Signal");
        var ordinaryArtifact = Assert.IsType<VisualArtifact>(ordinary.Artifact);
        var renderedArtifact = Assert.IsType<VisualArtifact>(rendered.Artifact);
        Assert.Equal(ordinaryArtifact.ToSvg(),renderedArtifact.ToSvg());
        Assert.Equal(ordinaryArtifact.ToPng(),renderedArtifact.ToPng());
        Assert.DoesNotContain(xml.Descendants().Attributes(), attribute => attribute.Name.LocalName.StartsWith("on", StringComparison.OrdinalIgnoreCase));
        Assert.Null(Assert.IsType<SequenceArtifact>(rendered.Artifact!.Model).Participants[0].Href);
        var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```"); Assert.False(fenced.HasErrors);
        Assert.Equal(rendered.Artifact!.ToSvg(), Assert.Single(fenced.Artifacts).ToSvg()); Assert.True(rendered.Artifact!.ToPng().Length > 64);
    }

    [Theory]
    [InlineData("participant A@{type: database, type: actor}")]
    [InlineData("participant A@{alias: 3}")]
    [InlineData("create participant")]
    [InlineData("participant A@{type: database} trailing")]
    [InlineData("links A: {\"One\":\"/one\",\"One\":\"/two\"}")]
    [InlineData("properties A: []")]
    public void MalformedParticipantDataIsLocatedBeforeDirectAndFencedArtifacts(string declaration) {
        var source = "sequenceDiagram\n" + declaration + "\nA->>B: Signal";
        var rendered = MermaidRenderer.Render(source); Assert.True(rendered.HasErrors); Assert.Null(rendered.Artifact);
        Assert.Contains(rendered.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.InvalidStatement && diagnostic.Span.Line == 2);
        var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```"); Assert.True(fenced.HasErrors); Assert.Empty(fenced.Artifacts);
    }

    [Fact]
    public void OfficialMetadataInCreationDeclarationsKeepsTheExistingParticipantLifetimeIdentity() {
        const string source = "sequenceDiagram\nparticipant A\ncreate participant D@{type: database, alias: Storage}\nA->>D: Create\nD-->>A: Ready\ndestroy D\nA->>D: Close";
        var rendered = MermaidRenderer.Render(source); Assert.Empty(rendered.Diagnostics);
        var sequence = Assert.IsType<SequenceArtifact>(rendered.Artifact!.Model);
        var participant = sequence.Participants.Single(item => item.Id == "D");
        Assert.Equal(SequenceArtifactParticipantKind.Database, participant.Kind); Assert.Equal("Storage", participant.Label);
        Assert.Equal(0, participant.CreatedAtMessageIndex); Assert.Equal(2, participant.DestroyedAtMessageIndex);
        var node = rendered.Artifact!.ToInterchangeEnvelope().Nodes.Single(item => item.Id == "D");
        Assert.Equal(0, node.Sequence!.CreatedAtMessageIndex); Assert.Equal(2, node.Sequence!.DestroyedAtMessageIndex);
        Assert.True(rendered.Artifact!.ToPng().Length > 64);
    }

    [Fact]
    public void ExplicitDefaultKindAndLegacyBraceConfigurationKeepTheExistingDefaultScene() {
        const string ordinary = "sequenceDiagram\nparticipant A as Native\nparticipant B\nA->>B: Signal";
        var baseline = MermaidRenderer.Render(ordinary);
        foreach (var declaration in new[] { "participant A@{type: participant, alias: Native}", "participant A {\"type\":\"participant\",\"alias\":\"Native\"}" }) {
            var result = MermaidRenderer.Render(ordinary.Replace("participant A as Native", declaration, StringComparison.Ordinal));
            Assert.Empty(result.Diagnostics); Assert.Equal(baseline.Artifact!.ToSvg(), result.Artifact!.ToSvg());
        }
    }

    [Fact]
    public void AggregatePortableMenuFactsUseTheExistingLocatedInterchangeTextBound() {
        var source = "sequenceDiagram\nparticipant A\nparticipant B\n" +
            string.Join("\n", Enumerable.Range(0, 100).Select(index => "link A: " + new string('L', 1000) + index + " @ /guide")) + "\nA->>B: Signal";
        var parsed = new MermaidParser().ParseSequence(source);
        Assert.True(parsed.HasErrors); Assert.Null(parsed.Document);
        Assert.Contains(parsed.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.ResourceLimitExceeded && diagnostic.Span.Line == 103);
        var direct = MermaidRenderer.Render(source); Assert.True(direct.HasErrors); Assert.Null(direct.Artifact);
    }

    [Theory]
    [InlineData(65469, false)]
    [InlineData(65470, true)]
    public void PortablePropertyCompositionAcceptsItsExactTextBoundAndRejectsTheNextCharacterAtSource(int contentLength, bool exceeds) {
        var source = "sequenceDiagram\nparticipant A\nparticipant B\nproperties A: {\"owner\":\"" + new string('x', contentLength) + "\"}\nA->>B: Signal";
        var options = new MermaidRenderOptions { ParseOptions = new MermaidParseOptions { ResourceLimits = new MermaidResourceLimits { MaximumLineCharacters = 70000 } } };
        var rendered = MermaidRenderer.Render(source, options);
        Assert.Equal(exceeds, rendered.HasErrors);
        if (exceeds) {
            Assert.Null(rendered.Artifact);
            Assert.Contains(rendered.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.ResourceLimitExceeded && diagnostic.Span.Line == 4);
        } else {
            Assert.Empty(rendered.Diagnostics);
            var participant = Assert.IsType<SequenceArtifact>(rendered.Artifact!.Model).Participants[0];
            Assert.Equal(65536, participant.Metadata["mermaid.properties.statements"].Length);
            var envelope = VisualArtifactInterchangeEnvelope.FromJson(rendered.Artifact!.ToInterchangeJson());
            Assert.Equal(65536, envelope.Nodes[0].Extensions["mermaid.properties.statements"].Length);
        }
    }

    [Fact]
    public void CapturedSemanticBudgetReservesMenuAndPropertyExpansionBeforeDirectNativeMaterialization() {
        const string source = "sequenceDiagram\nparticipant A@{type: database, alias: Storage}\nparticipant B\nlinks A: {\"Guide\":\"/guide\"}\nproperties A: {\"owner\":\"Platform\"}\nA->>B: Read";
        var limits = new MermaidResourceLimits { MaximumElements = 5 };
        var parsed = new MermaidParser().Parse(source, new MermaidParseOptions { ResourceLimits = limits });
        Assert.False(parsed.HasErrors); limits.MaximumElements = 5000;
        var document = Assert.IsType<MermaidSequenceDocument>(parsed.Document);
        var failed = MermaidRenderer.RenderParsed(parsed); Assert.True(failed.HasErrors); Assert.Null(failed.Artifact);
        Assert.Contains(failed.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.ResourceLimitExceeded && diagnostic.Span.Line == 4);
        Assert.Throws<MermaidResourceLimitException>(() => document.ToSequenceArtifact());
        // Four retained source records + one expanded menu entry + one typed navigation
        // record + one expanded property value require seven slots. Six stops at the property.
        var propertyBound = MermaidRenderer.Render(source,new MermaidRenderOptions { ParseOptions = new MermaidParseOptions { ResourceLimits = new MermaidResourceLimits { MaximumElements = 6 } } });
        Assert.True(propertyBound.HasErrors);Assert.Null(propertyBound.Artifact);
        Assert.Contains(propertyBound.Diagnostics,diagnostic=>diagnostic.Code==MermaidDiagnosticCodes.ResourceLimitExceeded&&diagnostic.Span.Line==5);
        var exact = MermaidRenderer.Render(source, new MermaidRenderOptions { ParseOptions = new MermaidParseOptions { ResourceLimits = new MermaidResourceLimits { MaximumElements = 7 } } });
        Assert.False(exact.HasErrors); Assert.NotNull(exact.Artifact);
    }

    [Fact]
    public void EscapedQuotesAndSemicolonsRemainInsideTheAuthoredAliasAndCompactDeclaration() {
        const string source = """
            sequenceDiagram
            participant A@{"type":"database","alias":"A\";B"}; participant B; A->>B: Signal
            """;
        var rendered = MermaidRenderer.Render(source); Assert.Empty(rendered.Diagnostics);
        var sequence = Assert.IsType<SequenceArtifact>(rendered.Artifact!.Model);
        Assert.Equal("A\";B", sequence.Participants[0].Label); Assert.Equal(2, sequence.Participants.Count); Assert.Single(sequence.Messages);
        Assert.Equal(SequenceArtifactParticipantKind.Database, sequence.Participants[0].Kind);
    }
}
