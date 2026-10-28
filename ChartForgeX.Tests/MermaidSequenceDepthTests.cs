using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.SvgRaster;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed partial class MermaidSequenceDepthTests {
    internal static string Fixture(string name) => File.ReadAllText(Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures", name + ".mmd"));

    [Theory]
    [InlineData("-|\\", true, true, false)]
    [InlineData("-|/", true, false, false)]
    [InlineData("-\\\\", false, true, false)]
    [InlineData("-//", false, false, false)]
    [InlineData("/|-", true, true, true)]
    [InlineData("\\|-", true, false, true)]
    [InlineData("//-", false, true, true)]
    [InlineData("\\\\-", false, false, true)]
    [InlineData("--|\\", true, true, false)]
    [InlineData("--|/", true, false, false)]
    [InlineData("--\\\\", false, true, false)]
    [InlineData("--//", false, false, false)]
    [InlineData("/|--", true, true, true)]
    [InlineData("\\|--", true, false, true)]
    [InlineData("//--", false, true, true)]
    [InlineData("\\\\--", false, false, true)]
    public void HalfArrowTerminalsKeepTheirSideEndpointLineStyleAndSourceIdentity(string notation, bool solid, bool top, bool reverse) {
        var parsed = new MermaidParser().ParseSequence("sequenceDiagram\nparticipant A\nparticipant B\nA" + notation + "B: Signal");
        Assert.Empty(parsed.Diagnostics);
        var message = Assert.Single(parsed.Document!.Messages);
        Assert.Equal(("A", "B", notation, "Signal"), (message.SourceId, message.TargetId, message.Operator, message.Text));
        var sequence = parsed.Document!.ToSequenceArtifact();
        var prepared = SequencePreparedCompiler.PrepareDefault(sequence);
        var edge = Assert.Single(prepared.ToArtifact().ToInterchangeEnvelope().Edges);
        Assert.Equal(notation, edge.Extensions["mermaid.operator"]);
        Assert.Equal(SequenceArtifactMessageKind.Call, edge.Sequence!.Kind);
        Assert.Equal(notation.Contains("--", StringComparison.Ordinal) ? SequenceArtifactMessageLineStyle.Dashed : SequenceArtifactMessageLineStyle.Solid, edge.Sequence.LineStyle);
        var endpoint = reverse ? edge.ResolvedRoute.First() : edge.ResolvedRoute.Last();
        double terminalX, terminalY, wingY;
        if (solid) {
            var terminal = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "sequence-half-call-arrow");
            terminalX = terminal.Commands[0].X; terminalY = terminal.Commands[0].Y; wingY = terminal.Commands[1].Y;
            Assert.True(terminal.Close);
        } else {
            var terminal = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneLine>(), node => node.Role == "sequence-half-async-arrow");
            terminalX = terminal.End.X; terminalY = terminal.End.Y; wingY = terminal.Start.Y;
        }
        Assert.Equal(endpoint.X, terminalX); Assert.Equal(endpoint.Y, terminalY);
        Assert.True(top ? wingY < endpoint.Y : wingY > endpoint.Y);
    }

    [Theory]
    [InlineData("<<->>", SequenceArtifactMessageLineStyle.Solid)]
    [InlineData("<<-->>", SequenceArtifactMessageLineStyle.Dashed)]
    public void BidirectionalVariantsRetainBothTerminalsAndTheirDeclaredLineStyle(string notation, SequenceArtifactMessageLineStyle style) {
        var rendered = MermaidRenderer.Render("sequenceDiagram\nA" + notation + "B: Bidirectional");
        Assert.Empty(rendered.Diagnostics);
        var sequence = Assert.IsType<SequenceArtifact>(rendered.Artifact!.Model);
        Assert.Equal(style, Assert.Single(sequence.Messages).LineStyle);
        var svg = XDocument.Parse(rendered.Artifact.ToSvg());
        Assert.Equal(2, svg.Descendants().Count(element => (string?)element.Attribute("data-cfx-role") == "sequence-call-arrow"));
        var line = svg.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "sequence-message-line");
        Assert.Equal(style == SequenceArtifactMessageLineStyle.Dashed, line.Attribute("stroke-dasharray") != null);
    }

    [Fact]
    public void SharedColorOwnerMapsHslHueUnitsAlphaAndExistingColorForms() {
        foreach (var value in new[] { "red", "#ff0000", "rgb(255,0,0)", "hsl(0,100%,50%)", "hsl(360deg 100% 50%)", "hsl(400grad 100% 50%)", "hsl(1turn 100% 50%)" }) {
            Assert.True(SvgRasterColor.TryParse(value, out var color), value);
            Assert.Equal(ChartColor.FromRgb(255, 0, 0), color);
        }
        Assert.True(SvgRasterColor.TryParse("hsla(-240deg,100%,50%,25%)", out var alpha));
        Assert.Equal(ChartColor.FromRgba(0, 255, 0, 64), alpha);
        foreach (var value in new[] { "hsl(NaN 10% 20%)", "hsl(Infinity 10% 20%)", "hsl(120 1 50%)", "hsla(0 100% 50% NaN)", "hsl(0 100%)" })
            Assert.False(SvgRasterColor.TryParse(value, out _), value);
    }

    [Fact]
    public void NestedSourcePaintCaptionsNotesActivationsAndNumbersReachDirectAndFencedNativeArtifacts() {
        var source = Fixture("sequence-native-depth");
        var direct = MermaidRenderer.Render(source);
        Assert.Empty(direct.Diagnostics);
        var document = Assert.IsType<MermaidSequenceDocument>(direct.Document);
        var sequence = Assert.IsType<SequenceArtifact>(direct.Artifact!.Model);
        Assert.Equal("Native<br/>API", document.Participants[0].Alias);
        Assert.Equal("Native\nAPI", sequence.Participants[0].Label);
        Assert.Equal("Native<br/>API", sequence.Participants[0].Metadata["mermaid.label"]);
        Assert.Equal("Submit<br/>request", document.Messages[0].Text);
        Assert.Equal("Submit\nrequest", sequence.Messages[0].Text);
        Assert.Equal("Submit<br/>request", sequence.Messages[0].Metadata["mermaid.text"]);
        Assert.Equal(ChartColor.FromRgb(222, 235, 247), sequence.ParticipantGroups[0].Fill);
        Assert.Equal(3, sequence.Notes.Count);
        Assert.Contains(sequence.Branches, branch => branch.Kind == "Option" && branch.IsEmpty);
        var prepared = SequencePreparedCompiler.PrepareDefault(sequence);
        Assert.Equal(2, prepared.Regions.Count(region => region.Role == "sequence-activation" && region.Bounds.Height > 4));
        var critical = Assert.Single(prepared.Regions, region => region.Role == "sequence-block" && region.Label == "Read policy");
        var cache = Assert.Single(prepared.Regions, region => region.Role == "sequence-note" && region.Label == "Read\ncached policy");
        var receipt = Assert.Single(prepared.Regions, region => region.Role == "sequence-branch" && region.Label == "Receipt path");
        Assert.InRange(cache.Bounds.Bottom, critical.Bounds.Top, critical.Bounds.Bottom);
        Assert.True(critical.Bounds.Bottom <= receipt.Bounds.Top);

        var labels = prepared.Scene.Nodes.OfType<VisualSceneText>().SelectMany(node => node.Text.Lines).Select(line => line.Text).ToArray();
        Assert.Contains(labels, label => label.Contains("Submit", StringComparison.Ordinal));
        Assert.Contains(labels, label => label.Contains("Complete", StringComparison.Ordinal));
        var numberFacts = prepared.ToArtifact().ToInterchangeEnvelope();
        Assert.Equal("1", prepared.Regions.Single(region => region.Role == "sequence-number" && region.Id == numberFacts.Edges[0].Id).Label);
        Assert.Equal("4", prepared.Regions.Single(region => region.Role == "sequence-number" && region.Id == numberFacts.Edges[3].Id).Label);
        var xml = XDocument.Parse(prepared.ToSvg());
        var participant = xml.Descendants().First(node => (string?)node.Attribute("data-cfx-role") == "sequence-participant-box");
        Assert.Equal("#E3EFFF", (string?)participant.Attribute("fill"));
        Assert.Contains(xml.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "sequence-message-line" && (string?)node.Attribute("stroke") == "#276749");
        var json = prepared.ToArtifact().ToInterchangeJson();
        Assert.Equal("#E3EFFF", VisualArtifactInterchangeEnvelope.FromJson(json).Extensions["chartforgex.sequence.paint.participantFill"]);
        Assert.True(direct.Artifact.ToPng().Length > 64);
        var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.Empty(fenced.Diagnostics);
        Assert.Equal(direct.Artifact.ToSvg(), Assert.Single(fenced.Artifacts).ToSvg());
        Assert.True(fenced.Artifacts[0].ToPng().Length > 64);
    }

    [Fact]
    public void ExplicitRolePaintsRemainAuthoredWhileNullRolesUseTheHostAndPreparedSnapshotsDetach() {
        var sequence = SequenceArtifact.Create("paint").AddParticipant("A").AddParticipant("B").AddMessage("A", "B", "Message");
        sequence.Paint.ParticipantFill = ChartColor.FromRgb(255, 0, 0);
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(720, 400)), VisualTheme.Graphite(), VisualThemeMode.Dark);
        var prepared = sequence.Prepare(context);
        var artifact = prepared.ToArtifact();
        var svg = prepared.ToSvg(); var json = artifact.ToInterchangeJson();
        var nativeBox = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().First(node => node.Role == "sequence-participant-box");
        Assert.Equal(ChartColor.FromRgb(255, 0, 0), nativeBox.Fill);
        Assert.Equal(context.Theme.Resolve(context.ThemeMode).Border, nativeBox.Stroke);
        sequence.Paint.ParticipantFill = ChartColor.FromRgb(0, 0, 255);
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(json, artifact.ToInterchangeJson());
        Assert.Equal("#FF0000", artifact.ToInterchangeEnvelope().Extensions["chartforgex.sequence.paint.participantFill"]);
    }

    [Fact]
    public void SafeCaptionEscapesDoNotSplitCompactStatementsOrExecuteHtml() {
        const string source = "sequenceDiagram\nparticipant A as First<br/>actor\nparticipant B\nA->>B: One#59;two; Note over A,B: #60;b#62;literal#60;/b#62;";
        var rendered = MermaidRenderer.Render(source);
        Assert.Empty(rendered.Diagnostics);
        var sequence = Assert.IsType<SequenceArtifact>(rendered.Artifact!.Model);
        Assert.Equal("One;two", Assert.Single(sequence.Messages).Text);
        Assert.Equal("<b>literal</b>", Assert.Single(sequence.Notes).Text);
        Assert.Contains("&lt;b&gt;literal&lt;/b&gt;", rendered.Artifact.ToSvg());
        Assert.DoesNotContain("<foreignObject", rendered.Artifact.ToSvg(), StringComparison.OrdinalIgnoreCase);
        var literal = new MermaidParser().ParseSequence("sequenceDiagram\nA->>B: <b>literal</b>");
        Assert.Contains(literal.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.UnsupportedStatement);
    }

    [Theory]
    [InlineData("{'themeVariables':{'actorBkg':'hsl(nope,20%,30%)'}}")]
    [InlineData("{'sequence':{'showSequenceNumbers':'true'}}")]
    public void InvalidSequencePresentationIsLocatedBeforeDirectAndFenceArtifacts(string configuration) {
        var source = "%%{init: " + configuration + "}%%\nsequenceDiagram\nA->>B: Signal";
        var direct = MermaidRenderer.Render(source);
        Assert.True(direct.HasErrors); Assert.Null(direct.Artifact);
        Assert.Contains(direct.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.InvalidConfiguration && diagnostic.Span.Line == 1);
        var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.True(fenced.HasErrors); Assert.Empty(fenced.Artifacts);
    }

    [Theory]
    [InlineData("false", false)]
    [InlineData("true", true)]
    public void SourceNumberingOptionUsesExistingDecimalOwnerAndExplicitAutonumberWins(string enabled, bool numbered) {
        var prefix = "%%{init: {'sequence':{'showSequenceNumbers':" + enabled + "}}}%%\nsequenceDiagram\n";
        var implicitSource = MermaidRenderer.Render(prefix + "A->>B: Signal");
        Assert.Empty(implicitSource.Diagnostics);
        var implicitModel = Assert.IsType<SequenceArtifact>(implicitSource.Artifact!.Model);
        var implicitPrepared = SequencePreparedCompiler.PrepareDefault(implicitModel);
        Assert.Equal(numbered, implicitPrepared.Regions.Any(region => region.Role == "sequence-number" && region.Label == "1"));
        Assert.Contains("Signal", implicitPrepared.ToSvg());
        var explicitSource = MermaidRenderer.Render(prefix + "autonumber 1.5 0.25\nA->>B: One\nB-->>A: Two");
        Assert.Empty(explicitSource.Diagnostics);
        var explicitPrepared = SequencePreparedCompiler.PrepareDefault(Assert.IsType<SequenceArtifact>(explicitSource.Artifact!.Model));
        var explicitFacts = explicitPrepared.ToArtifact().ToInterchangeEnvelope();
        Assert.Equal("1.75", explicitPrepared.Regions.Single(region => region.Role == "sequence-number" && region.Id == explicitFacts.Edges[1].Id).Label);
        Assert.Contains("Two", explicitPrepared.ToSvg());
    }
}
