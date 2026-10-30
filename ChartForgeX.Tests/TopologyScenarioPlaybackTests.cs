using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects the request-flow playback model shared by topology frames, sequence diagrams, HTML and terminals.</summary>
public sealed class TopologyScenarioPlaybackTests {
    internal static TopologyChart ReplicationChart() => TopologyChart.Create()
        .WithId("replication")
        .WithTitle("Replicate a change")
        .WithLayout(TopologyLayoutMode.Layered, TopologyLayoutDirection.LeftToRight)
        .AddGroup("hq", "HQ site", 0, 0, 0, 0)
        .AddNode("admin", "Admin", 0, 0, TopologyNodeKind.Person)
        .AddNode("dc01", "DC01", 0, 0, TopologyNodeKind.Server, TopologyHealthStatus.Healthy, "hq", "PDC emulator")
        .AddNode("dc02", "DC02", 0, 0, TopologyNodeKind.Database, TopologyHealthStatus.Warning, "hq")
        .AddEdge("ldap", "admin", "dc01", "LDAP", direction: VisualLinkDirection.Forward)
        .AddEdge("rpc", "dc01", "dc02", "RPC", direction: VisualLinkDirection.Forward)
        .AddScenario("replicate", "Replicate a change", scenario => scenario
            .WithDescription("A title change reaches DC02")
            .AddMessageStep("ldap", TopologyScenarioInteraction.Request, "Modify user", configure: step => step.WithPayload("{\n  \"title\": \"Site manager\"\n}", TopologyScenarioPayloadFormat.Json))
            .AddNodeStep("dc01", "Commit locally")
            .AddMessageStep("rpc", TopologyScenarioInteraction.Message, "Change notification")
            .AddMessageStep("rpc", TopologyScenarioInteraction.Request, "GetNCChanges", reverse: true)
            .AddMessageStep("rpc", TopologyScenarioInteraction.Response, "1 object")
            .AddMessageStep("ldap", TopologyScenarioInteraction.Failure, "Timeout"));

    [Fact]
    public void ResolveScenario_EdgeAndNodeSteps_ResolveTravelDirectionLabelsAndPayloads() {
        var resolved = ReplicationChart().ResolveScenario("replicate");

        Assert.Equal(6, resolved.Steps.Count);
        Assert.Equal(new[] { "admin", "dc01", "dc02" }, resolved.ParticipantNodeIds);
        var first = resolved.Steps[0];
        Assert.Equal(("admin", "dc01", "ldap"), (first.SourceNodeId, first.TargetNodeId, first.EdgeId));
        Assert.Equal("Admin → DC01: Modify user", first.Summary);
        Assert.Equal(TopologyScenarioPayloadFormat.Json, first.PayloadFormat);
        Assert.Contains("Site manager", first.Payload);
        Assert.Null(resolved.Steps[1].TargetNodeId);
        Assert.Equal(new[] { "dc01" }, resolved.Steps[1].NodeIds);
        var reverse = resolved.Steps[3];
        Assert.Equal(("dc02", "dc01"), (reverse.SourceNodeId, reverse.TargetNodeId));
        Assert.True(resolved.Steps[5].IsFailure);
        Assert.Throws<ArgumentException>(() => ReplicationChart().ResolveScenario("missing"));
    }

    [Fact]
    public void ResolveScenario_PlainRouteBetweenTargetAndSourceNodeSteps_TravelsBackwardLikeMotion() {
        var chart = ReplicationChart().AddScenario("legacy", "Legacy route", scenario => scenario
            .AddNodeStep("dc02").AddEdgeStep("rpc").AddNodeStep("dc01"));
        var step = chart.ResolveScenario("legacy").Steps[1];

        Assert.True(step.TravelsBackward);
        Assert.Equal(("dc02", "dc01"), (step.SourceNodeId, step.TargetNodeId));
        Assert.False(chart.ResolveScenario("replicate").Steps[2].TravelsBackward);
    }

    [Fact]
    public void StaticSvg_ScenarioMetadata_LeavesPayloadsOutOfPublishedDiagrams() {
        var svg = ReplicationChart().ToSvg();

        Assert.Contains("data-cfx-scenarios=", svg, StringComparison.Ordinal);
        Assert.DoesNotContain("Site manager", svg, StringComparison.Ordinal);
    }

    [Fact]
    public void InteractiveHtml_NonAsciiChartId_RendersSequenceFigures() {
        var chart = ReplicationChart().WithId("zamówienia-łódź");
        var html = chart.ToInteractiveHtmlPage(new TopologyRenderOptions().WithHtmlScenarioPanel().WithHtmlScenarioSequence());

        Assert.Contains("data-cfx-scenario-sequence=\"replicate\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public void ToText_ActiveStep_MarksTheStepAndShowsOnlyItsPayload() {
        var text = ReplicationChart().ResolveScenario("replicate").ToText(0, TopologyScenarioTextPayloads.Active);

        Assert.StartsWith("Replicate a change (6 steps)\nA title change reaches DC02\n", text, StringComparison.Ordinal);
        Assert.Contains("▶ 1. Admin → DC01  [request]  Modify user", text, StringComparison.Ordinal);
        Assert.Contains("\"title\": \"Site manager\"", text, StringComparison.Ordinal);
        Assert.Contains("  4. DC02 → DC01  [request]  GetNCChanges", text, StringComparison.Ordinal);
        var ascii = ReplicationChart().ResolveScenario("replicate").ToText(3, TopologyScenarioTextPayloads.None, TopologyCellGlyphSet.Ascii);
        Assert.Contains("> 4. DC02 -> DC01", ascii, StringComparison.Ordinal);
        Assert.DoesNotContain("Site manager", ascii, StringComparison.Ordinal);
    }

    [Fact]
    public void Validation_NodeStepWithInteraction_ReportsScenarioStepInteraction() {
        var chart = ReplicationChart();
        chart.Scenarios[0].Steps[1].Interaction = TopologyScenarioInteraction.Request;

        var exception = Assert.Throws<TopologyValidationException>(() => chart.ToSvg());
        Assert.Contains(exception.Result.Errors, error => error.Code == "scenario-step-interaction");
    }

    [Fact]
    public void Payload_LongerThanLimit_IsRejected() {
        var step = new TopologyScenarioStep { Id = "x", Kind = TopologyScenarioStepKind.Edge };
        Assert.Throws<ArgumentOutOfRangeException>(() => step.Payload = new string('x', 65_537));
    }

    [Fact]
    public void ToSequenceArtifact_SameScenario_ProjectsParticipantsMessagesNotesAndStepIndices() {
        var sequence = ReplicationChart().ToSequenceArtifact("replicate");

        Assert.Equal(new[] { "admin", "dc01", "dc02" }, sequence.Participants.Select(participant => participant.Id));
        Assert.All(sequence.Participants, participant => Assert.NotNull(participant.Card));
        Assert.Equal(TopologyNodeKind.Person, sequence.Participants[0].Card!.IconKind);
        Assert.Equal("PDC emulator", sequence.Participants[1].Card!.Subtitle);
        Assert.Equal(TopologyNodeKind.Database, sequence.Participants[2].Card!.IconKind);
        Assert.Single(sequence.ParticipantGroups);
        Assert.Equal((1, 2), (sequence.ParticipantGroups[0].FirstParticipantIndex, sequence.ParticipantGroups[0].LastParticipantIndex));
        Assert.NotNull(sequence.Paint.MessageLabelFill);
        Assert.NotNull(sequence.Messages[4].Color);
        Assert.Null(sequence.Messages[0].Color);
        Assert.Equal(5, sequence.Messages.Count);
        Assert.Single(sequence.Notes);
        Assert.Equal(SequenceArtifactMessageKind.Async, sequence.Messages[1].Kind);
        Assert.Equal(("dc02", "dc01"), (sequence.Messages[2].SourceId, sequence.Messages[2].TargetId));
        Assert.Equal(SequenceArtifactMessageKind.Return, sequence.Messages[3].Kind);
        Assert.Equal(SequenceArtifactMessageLineStyle.Dashed, sequence.Messages[3].LineStyle);
        Assert.StartsWith("✕ ", sequence.Messages[4].Text, StringComparison.Ordinal);
        Assert.Equal("3", sequence.Messages[2].Metadata[TopologyChartExtensions.ScenarioStepMetadataKey]);
        var svg = sequence.ToSvg();
        Assert.Contains("data-cfx-scenario-step=\"3\"", svg, StringComparison.Ordinal);
        Assert.Contains("sequence-participant-icon", svg, StringComparison.Ordinal);
        Assert.Contains("sequence-message-label-pill", svg, StringComparison.Ordinal);
    }

    [Fact]
    public void PrepareScenarioStep_EveryStep_KeepsTheLayoutAndAccentsOnlyTheActiveEdge() {
        var chart = ReplicationChart();
        var baseline = chart.Prepare().Analyze().Nodes.ToDictionary(node => node.Id, node => node.Bounds);
        for (var index = 0; index < chart.Scenarios[0].Steps.Count; index++) {
            var frame = chart.PrepareScenarioStep("replicate", index).Analyze();
            foreach (var node in frame.Nodes) Assert.Equal(baseline[node.Id], node.Bounds);
        }

        var reverse = chart.PrepareScenarioStep("replicate", 3).ToSvg();
        Assert.Contains("#2563EB", reverse, StringComparison.OrdinalIgnoreCase);
        var failure = chart.PrepareScenarioStep("replicate", 5).ToSvg();
        Assert.Contains("#DC2626", failure, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(VisualLinkDirection.Forward, chart.Edges.Single(edge => edge.Id == "rpc").Direction);
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.PrepareScenarioStep("replicate", 6));
    }

    [Fact]
    public void Interchange_RoundTrip_KeepsInteractionDirectionAndPayload() {
        var envelope = VisualArtifactInterchangeJson.Deserialize(VisualArtifactInterchangeJson.Serialize(ReplicationChart().Prepare().ToInterchangeEnvelope()));

        var steps = envelope.Scenarios.Single().Steps;
        Assert.Equal(TopologyScenarioInteraction.Request, steps[0].Interaction);
        Assert.Equal(TopologyScenarioPayloadFormat.Json, steps[0].PayloadFormat);
        Assert.Contains("Site manager", steps[0].Payload);
        Assert.True(steps[3].Reverse);
        var restored = envelope.ToTopologyChart().Scenarios.Single().Steps;
        Assert.True(restored[3].Reverse);
        Assert.Equal(TopologyScenarioInteraction.Response, restored[4].Interaction);
    }

    [Fact]
    public void InteractiveHtml_ScenarioSequence_RendersPayloadPanelSequenceFiguresAndStepData() {
        var options = new TopologyRenderOptions { ActiveScenarioId = "replicate" }.WithHtmlScenarioPanel().WithHtmlScenarioSequence();
        var html = ReplicationChart().ToInteractiveHtmlPage(options);

        Assert.Contains("data-cfx-scenario-payload=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("data-cfx-scenario-payload-body=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("data-cfx-scenario-sequence=\"replicate\"", html, StringComparison.Ordinal);
        Assert.Contains("data-cfx-scenario-step=\"4\"", html, StringComparison.Ordinal);
        Assert.Contains("&quot;interaction&quot;:&quot;response&quot;", html, StringComparison.Ordinal);
        Assert.Contains("&quot;source&quot;:&quot;DC02&quot;", html, StringComparison.Ordinal);
        Assert.Contains("&quot;payloadFormat&quot;:&quot;json&quot;", html, StringComparison.Ordinal);
        Assert.Contains("renderScenarioPayload", html, StringComparison.Ordinal);
        Assert.Contains("syncScenarioSequence", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-cfx-scenario-sequence=", ReplicationChart().ToInteractiveHtmlPage(), StringComparison.Ordinal);
    }

    [Fact]
    public void InteractiveHtml_ScenarioSequence_KeepsTheTopologyAndSequenceTypeScale() {
        var chart = ReplicationChart();
        var options = new TopologyRenderOptions { ActiveScenarioId = "replicate" }.WithHtmlScenarioPanel().WithHtmlScenarioSequence();
        var html = chart.ToInteractiveHtmlPage(options);
        var topologyWidth = chart.Prepare().Width;
        var sequenceWidth = VisualArtifacts.SequencePreparedCompiler.PrepareDefault(chart.ToSequenceArtifact("replicate")).Size.Width;
        double Minimum(string name) => double.Parse(System.Text.RegularExpressions.Regex.Match(html, "--" + name + ":([0-9.]+)px").Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);

        // The topology and its sequence shrink at most an eighth to fit the column; past that they scroll in their frames.
        Assert.Contains("data-cfx-text-size=\"kept\"", html, StringComparison.Ordinal);
        Assert.Equal(topologyWidth * 0.875, Minimum("cfx-topology-min-width"), 1);
        Assert.Equal(sequenceWidth * 0.875, Minimum("cfx-sequence-min-width"), 1);
        Assert.Contains("max-width:" + Math.Max(topologyWidth, sequenceWidth).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + "px", html, StringComparison.Ordinal);
        Assert.Contains("[data-cfx-text-size='kept'] .cfx-topology-viewport{overflow-x:auto", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-cfx-text-size=\"", chart.ToInteractiveHtmlPage(new TopologyRenderOptions().WithHtmlScenarioPanel()), StringComparison.Ordinal);
    }
    [Fact]
    public void ScenarioStory_OneScenePerStep_RendersEveryChapter() {
        var story = ReplicationChart().ToScenarioStory("replicate").Prepare();

        Assert.Equal(6, story.Chapters.Count);
        Assert.StartsWith("1. Admin → DC01", ReplicationChart().ToScenarioStory("replicate").Scenes[0].Title, StringComparison.Ordinal);
        var png = story.ToPng(story.Chapters[2].Start + TimeSpan.FromMilliseconds(500));
        Assert.True(png.Length > 1000);
        Assert.Contains("Change notification", story.ToTranscript(), StringComparison.Ordinal);
    }
}
