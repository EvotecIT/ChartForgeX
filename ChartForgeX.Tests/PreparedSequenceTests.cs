using System.Xml.Linq;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects the native sequence family and its portable prepared geometry.</summary>
public sealed class PreparedSequenceTests {
    private static VisualRenderContext Context(double width = 960, double height = 900, VisualThemeMode mode = VisualThemeMode.Light) =>
        new(new VisualLayoutOptions(new VisualSize(width, height)), themeMode: mode, frame: new VisualFrame(showLegend: false));

    [Theory]
    [InlineData(SequenceArtifactParticipantKind.Participant)]
    [InlineData(SequenceArtifactParticipantKind.Actor)]
    [InlineData(SequenceArtifactParticipantKind.Boundary)]
    [InlineData(SequenceArtifactParticipantKind.Control)]
    [InlineData(SequenceArtifactParticipantKind.Entity)]
    [InlineData(SequenceArtifactParticipantKind.Database)]
    [InlineData(SequenceArtifactParticipantKind.Collections)]
    [InlineData(SequenceArtifactParticipantKind.Queue)]
    public void ParticipantNotationRetainsIdentityAndPreparedBounds(SequenceArtifactParticipantKind kind) {
        var model = SequenceArtifact.Create("symbols").AddParticipant("source", "A measured\nparticipant", kind);
        var prepared = model.Prepare(Context(480, 320));
        var envelope = prepared.ToArtifact("symbols", VisualArtifactKind.Sequence).ToInterchangeEnvelope();
        var node = Assert.Single(envelope.Nodes);
        var region = Assert.Single(prepared.Regions, r => r.Role == "sequence-participant");
        Assert.Equal("source", node.Id);
        Assert.Equal(kind, node.Sequence!.Kind);
        Assert.Equal(region.Bounds.X, node.X); Assert.Equal(region.Bounds.Y, node.Y);
        Assert.Equal(region.Bounds.Width, node.Width); Assert.Equal(region.Bounds.Height, node.Height);
        var xml = XDocument.Parse(prepared.ToSvg());
        Assert.Contains(xml.Descendants(), e => (string?)e.Attribute("data-cfx-role") == (kind == SequenceArtifactParticipantKind.Participant ? "sequence-participant-box" : "sequence-notation"));
        Assert.Equal(480, prepared.ToRgba(new VisualRenderOptions(supersampling: 1)).Width);
    }

    [Theory]
    [InlineData(SequenceArtifactMessageKind.Call, "sequence-call-arrow")]
    [InlineData(SequenceArtifactMessageKind.Return, "sequence-return-arrow")]
    [InlineData(SequenceArtifactMessageKind.Async, "sequence-async-arrow")]
    [InlineData(SequenceArtifactMessageKind.Event, "sequence-event-arrow")]
    public void SelfAndOrdinaryMessagesRetainFullNativeRoutes(SequenceArtifactMessageKind kind, string arrowRole) {
        var model = SequenceArtifact.Create("arrows").AddParticipant("a").AddParticipant("b")
            .AddMessage("a", "b", "outbound", kind: kind)
            .AddMessage("b", "b", "self message", SequenceArtifactMessageLineStyle.Dashed, kind);
        var prepared = model.Prepare(Context());
        var envelope = prepared.ToArtifact("arrows", VisualArtifactKind.Sequence).ToInterchangeEnvelope();
        Assert.Equal(2, envelope.Edges[0].ResolvedRoute.Count);
        var loop = envelope.Edges[1].ResolvedRoute;
        Assert.Equal(4, loop.Count);
        Assert.Equal(loop[0].X, loop[3].X); Assert.True(loop[3].Y > loop[0].Y);
        Assert.True(loop[1].X > loop[0].X); Assert.Equal(loop[1].X, loop[2].X);
        var xml = XDocument.Parse(prepared.ToSvg());
        Assert.Contains(xml.Descendants(), e => (string?)e.Attribute("data-cfx-role") == arrowRole);
        Assert.Contains(xml.Descendants(), e => (string?)e.Attribute("data-cfx-role") == "sequence-message-line" && e.Attribute("stroke-dasharray") != null);
        Assert.Equal(kind, envelope.Edges[1].Sequence!.Kind);
        Assert.All(envelope.Edges, edge => {
            Assert.True(edge.ResolvedLabelBounds.HasValue);
            var bounds = edge.ResolvedLabelBounds.Value;
            Assert.True(bounds.Width > 0); Assert.True(bounds.Height > 0);
            Assert.True(bounds.Bottom < edge.ResolvedRoute[0].Y);
        });
        var labels = xml.Descendants().Where(e => (string?)e.Attribute("data-cfx-role") == "sequence-message")
            .Select(e => e.Descendants().Single(t => t.Name.LocalName == "text")).ToArray();
        for (int i = 0; i < labels.Length; i++)
            Assert.Equal(envelope.Edges[i].ResolvedLabelBounds!.Value.X,
                double.Parse((string)labels[i].Attribute("x")!, System.Globalization.CultureInfo.InvariantCulture), 2);
    }

    [Theory]
    [InlineData(VisualThemeMode.Light)]
    [InlineData(VisualThemeMode.Dark)]
    public void NestedFragmentsNotesAndActivationsHaveDisjointMeasuredRows(VisualThemeMode mode) {
        var model = SequenceArtifact.Create("fragments").WithTitle("Shared frame")
            .AddParticipant("client", "Client", SequenceArtifactParticipantKind.Actor)
            .AddParticipant("server", "Server", SequenceArtifactParticipantKind.Control)
            .AddMessage("client", "server", "Request a long description that must wrap within the lane")
            .AddMessage("server", "server", "Internal work")
            .AddMessage("server", "client", "Reply", SequenceArtifactMessageLineStyle.Dashed, SequenceArtifactMessageKind.Return)
            .AddBlock(SequenceArtifactBlockKind.Alt, "Outcome", 0, 2)
            .AddBlock(SequenceArtifactBlockKind.Loop, "Retry", 1, 1, depth: 1)
            .AddBranch(SequenceArtifactBlockKind.Alt, "Primary", "Success", 0, 1)
            .AddBranch(SequenceArtifactBlockKind.Alt, "Else", "Failure", 2, 2)
            .AddActivation("server", true, 0).AddActivation("server", true, 1)
            .AddActivation("server", false, 2).AddActivation("server", false, 3)
            .AddNote(SequenceArtifactNotePlacement.LeftOf, new[] { "client" }, "Before\nrequest")
            .AddNote(SequenceArtifactNotePlacement.Over, new[] { "client", "server" }, "After processing");
        model.Notes[0].StepIndex = 0; model.Notes[1].StepIndex = 2;
        var prepared = model.Prepare(Context(960, 1100, mode));
        var messages = prepared.Regions.Where(r => r.Role == "sequence-message").ToArray();
        Assert.True(messages[0].Bounds.Bottom < messages[1].Bounds.Top);
        Assert.True(messages[1].Bounds.Bottom < messages[2].Bounds.Top);
        Assert.Equal(2, prepared.Regions.Count(r => r.Role == "sequence-note"));
        Assert.Equal(2, prepared.Regions.Count(r => r.Role == "sequence-block"));
        Assert.Equal(2, prepared.Regions.Count(r => r.Role == "sequence-branch"));
        var bars = prepared.Regions.Where(r => r.Role == "sequence-activation" && r.Bounds.Height > 4).OrderBy(r => r.Bounds.Y).ToArray();
        Assert.Equal(2, bars.Length); Assert.True(bars[1].Bounds.X > bars[0].Bounds.X);
        var envelope = prepared.ToArtifact("fragments", VisualArtifactKind.Sequence).ToInterchangeEnvelope();
        Assert.All(envelope.Annotations, a => Assert.True(a.Extensions.ContainsKey("chartforgex.bounds.x")));
        Assert.Contains(envelope.Annotations, a => a.Sequence?.Depth == 1 && a.Sequence.BlockKind == SequenceArtifactBlockKind.Loop);
        Assert.NotEmpty(prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
    }

    [Fact]
    public void PreparedSceneAndSemanticSnapshotDetachFromEverySequenceCollection() {
        var model = SequenceArtifact.Create("snapshot").AddParticipant("a", "Original participant").AddParticipant("b")
            .AddMessage("a", "b", "Original message").AddMessage("b", "a", "Return")
            .AddActivation("b", true, 0).AddActivation("b", false, 1)
            .AddNote(SequenceArtifactNotePlacement.RightOf, new[] { "b" }, "Original note")
            .AddBlock(SequenceArtifactBlockKind.Opt, "Original block", 0, 1)
            .AddBranch(SequenceArtifactBlockKind.Opt, "Primary", "Original branch", 0, 1);
        model.Notes[0].StepIndex = 0;
        model.Participants[0].Href = "https://example.com/details?a=1&b=2";
        model.Metadata["mermaid.autonumber"] = "true"; model.Metadata["mermaid.autonumber.start"] = "10"; model.Metadata["mermaid.autonumber.increment"] = "5";
        var prepared = model.Prepare(Context());
        var artifact = prepared.ToArtifact("snapshot", VisualArtifactKind.Sequence);
        string svg = prepared.ToSvg(), json = artifact.ToInterchangeJson(); byte[] png = prepared.ToPng(new VisualRenderOptions(supersampling: 1));
        model.Participants[0].Label = "Changed"; model.Participants[0].Href = "https://example.com/changed";
        model.Messages[0].Text = "Changed"; model.Messages[0].Metadata["new"] = "changed";
        model.Notes[0].Text = "Changed"; model.Notes[0].ParticipantIds.Clear(); model.Activations[0].Active = false;
        model.Blocks[0].Text = "Changed"; model.Branches[0].Text = "Changed"; model.Metadata.Clear();
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(json, artifact.ToInterchangeJson());
        Assert.Equal(png, prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        var xml = XDocument.Parse(svg);
        Assert.Contains(xml.Descendants(), e => e.Name.LocalName == "a" && (string?)e.Attribute("href") == "https://example.com/details?a=1&b=2");
        Assert.Contains("10. Original message", string.Concat(xml.Descendants().Where(e => e.Name.LocalName == "text").Select(e => e.Value)));
        Assert.Contains("15. Return", string.Concat(xml.Descendants().Where(e => e.Name.LocalName == "text").Select(e => e.Value)));
        var firstRead = artifact.ToInterchangeEnvelope(); firstRead.Edges[0].ResolvedRoute[0].X = -1;
        Assert.True(artifact.ToInterchangeEnvelope().Edges[0].ResolvedRoute[0].X >= 0);
    }

    [Fact]
    public void InvalidReferencesActivationBalanceAndInsufficientViewportFailExplicitly() {
        var unmatched = SequenceArtifact.Create("invalid").AddParticipant("a").AddActivation("a", false, 0);
        Assert.Throws<InvalidOperationException>(() => unmatched.Prepare(Context()));
        var invalid = SequenceArtifact.Create("invalid").AddParticipant("a").AddParticipant("b").AddMessage("a", "b", "Message");
        invalid.Messages[0].TargetId = "missing";
        Assert.Throws<InvalidOperationException>(() => invalid.Prepare(Context()));
        var crowded = SequenceArtifact.Create("crowded");
        for (int i = 0; i < 8; i++) crowded.AddParticipant("p" + i);
        Assert.Throws<NotSupportedException>(() => crowded.Prepare(Context(320, 240)));
    }

    [Theory]
    [InlineData("->", "sequence-async-arrow", 1)]
    [InlineData("->>", "sequence-call-arrow", 1)]
    [InlineData("--x", "sequence-event-cross", 2)]
    [InlineData("<<->>", "sequence-call-arrow", 2)]
    public void RetainedAdapterOperatorsControlNotationWithoutChangingDomainKind(string notation, string role, int count) {
        var model = SequenceArtifact.Create("operators").AddParticipant("a").AddParticipant("b").AddMessage("a", "b", "Signal");
        model.Messages[0].Metadata["mermaid.operator"] = notation;
        var prepared = model.Prepare(Context());
        var xml = XDocument.Parse(prepared.ToSvg());
        Assert.Equal(count, xml.Descendants().Count(e => (string?)e.Attribute("data-cfx-role") == role));
        var message = Assert.Single(prepared.ToArtifact("operators", VisualArtifactKind.Sequence).ToInterchangeEnvelope().Edges);
        Assert.Equal(SequenceArtifactMessageKind.Call, message.Sequence!.Kind);
        Assert.Equal(notation, message.Extensions["mermaid.operator"]);
    }

    [Fact]
    public void PublicExportsUseNativeContentSizingWhileSourceEnvelopeRetainsAuthoredBounds() {
        var model = SequenceArtifact.Create("public-native").WithSize(500, 240)
            .AddParticipant("a", "Caller", SequenceArtifactParticipantKind.Actor).AddParticipant("b", "Worker");
        for (int i = 0; i < 8; i++) model.AddMessage("a", "b", "Request " + i);
        model.AddActivation("b", true, 0).AddActivation("b", false, 7)
            .AddBlock(SequenceArtifactBlockKind.Alt, "Result", 0, 7)
            .AddBranch(SequenceArtifactBlockKind.Alt, "Else", "Retry", 4, 7);
        var source = model.ToVisualArtifact();
        Assert.Same(model, source.Model);
        Assert.Equal(500, source.NaturalSize!.Value.Width); Assert.Equal(240, source.NaturalSize.Value.Height);
        Assert.Equal(240, source.ToInterchangeEnvelope().Height);
        var xml = XDocument.Parse(model.ToSvg());
        double width = double.Parse((string)xml.Root!.Attribute("width")!, System.Globalization.CultureInfo.InvariantCulture);
        double height = double.Parse((string)xml.Root.Attribute("height")!, System.Globalization.CultureInfo.InvariantCulture);
        Assert.True(width >= 500); Assert.True(height > 240);
        Assert.Contains(xml.Descendants(), e => (string?)e.Attribute("data-cfx-role") == "sequence-activation-bar");
        Assert.Contains(xml.Descendants(), e => (string?)e.Attribute("data-cfx-role") == "sequence-branch");
        Assert.Equal(model.ToSvg(), source.ToSvg());
        var pixels = ChartForgeX.Raster.RasterImageDecoder.Decode(model.ToPng());
        Assert.Equal((int)Math.Ceiling(width), pixels.Width); Assert.Equal((int)Math.Ceiling(height), pixels.Height);
        Assert.Throws<NotSupportedException>(() => model.Prepare(Context(500, 240)));
    }

    [Fact]
    public void NativePlanUsesTheEstablishedNonnegativeOrderedSequenceSpans() {
        var model = SequenceArtifact.Create("normalized").AddParticipant("a").AddParticipant("b").AddMessage("a", "b", "Call")
            .AddNote(SequenceArtifactNotePlacement.Over, new[] { "a" }, "Note")
            .AddBlock(SequenceArtifactBlockKind.Opt, "Span", -2, -5);
        model.Notes[0].StepIndex = -3;
        var prepared = model.Prepare(Context());
        var envelope = prepared.ToArtifact("normalized", VisualArtifactKind.Sequence).ToInterchangeEnvelope();
        Assert.All(envelope.Annotations, annotation => Assert.Equal(0, annotation.StartIndex));
        Assert.Equal(0, envelope.Annotations.Single(a => a.Role == VisualArtifactInterchangeAnnotationRole.SequenceBlock).EndIndex);
        Assert.Equal("960", envelope.Extensions["chartforgex.source.width"]);
        Assert.Equal("560", envelope.Extensions["chartforgex.source.height"]);
        Assert.Contains(prepared.Regions, r => r.Role == "sequence-note");
    }

    [Fact]
    public void EmptyFragmentsEndBeforeTheNextMessageAndPlainConnectionsHaveNoArrowhead() {
        var model = SequenceArtifact.Create("empty").AddParticipant("a").AddParticipant("b").AddMessage("a", "b", "After empty fragment")
            .AddBlock(SequenceArtifactBlockKind.Opt, "No work", 0, 0, isEmpty: true);
        model.Messages[0].Metadata["mermaid.operator"] = "--";
        var prepared = model.Prepare(Context());
        var block = Assert.Single(prepared.Regions, r => r.Role == "sequence-block");
        var message = Assert.Single(prepared.Regions, r => r.Role == "sequence-message");
        Assert.True(block.Bounds.Bottom <= message.Bounds.Top);
        var xml = XDocument.Parse(prepared.ToSvg());
        Assert.DoesNotContain(xml.Descendants(), e => ((string?)e.Attribute("data-cfx-role"))?.EndsWith("-arrow", StringComparison.Ordinal) == true);
        Assert.Contains(xml.Descendants(), e => (string?)e.Attribute("data-cfx-role") == "sequence-message-line");
    }
}
