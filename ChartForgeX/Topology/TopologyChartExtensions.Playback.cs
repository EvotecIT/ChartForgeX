using System;
using System.Globalization;
using System.Linq;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Topology;

public static partial class TopologyChartExtensions {
    /// <summary>Metadata key that links a sequence message to the scenario step it was projected from.</summary>
    public const string ScenarioStepMetadataKey = "cfx.scenario.step";

    /// <summary>
    /// Resolves a scenario against the topology, so each step knows its source, target, edge, label, and payload.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="scenarioId">The scenario id.</param>
    /// <returns>The resolved scenario.</returns>
    /// <exception cref="ArgumentException">The scenario does not exist or references missing nodes or edges.</exception>
    public static TopologyResolvedScenario ResolveScenario(this TopologyChart chart, string scenarioId) => TopologyScenarioResolver.Resolve(chart, scenarioId);

    /// <summary>
    /// Prepares the topology with one scenario step highlighted and the rest of the diagram dimmed.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="scenarioId">The scenario id.</param>
    /// <param name="stepIndex">The zero-based step index.</param>
    /// <param name="options">Optional render options. The caller's instance is not modified.</param>
    /// <returns>A prepared topology for SVG, PNG, or terminal output of that step.</returns>
    public static PreparedTopology PrepareScenarioStep(this TopologyChart chart, string scenarioId, int stepIndex, TopologyRenderOptions? options = null) {
        var resolved = TopologyScenarioResolver.Resolve(chart, scenarioId);
        if (stepIndex < 0 || stepIndex >= resolved.Steps.Count) {
            throw new ArgumentOutOfRangeException(nameof(stepIndex), stepIndex, "Scenario '" + resolved.Id + "' has " + resolved.Steps.Count.ToString(CultureInfo.InvariantCulture) + " steps.");
        }

        var step = resolved.Steps[stepIndex];
        var stepOptions = (options ?? chart.DefaultRenderOptions ?? new TopologyRenderOptions()).Clone();
        stepOptions.ActiveScenarioId = null;
        stepOptions.HighlightNodeIds.Clear();
        stepOptions.HighlightNodeIds.AddRange(step.NodeIds);
        stepOptions.HighlightEdgeIds.Clear();
        stepOptions.HighlightEdgeIds.AddRange(step.EdgeIds);
        stepOptions.HighlightConnectedEdges = false;
        // Lay out with the source chart, then repaint only the step's edge, so every step keeps the same geometry.
        var color = step.IsFailure ? FailureColor : string.IsNullOrWhiteSpace(resolved.Scenario.Color) ? ActiveStepColor : resolved.Scenario.Color!.Trim();
        return chart.Prepare(stepOptions).Restyle(frame => {
            foreach (var edge in frame.Edges.Where(candidate => step.EdgeIds.Contains(candidate.Id))) {
                edge.Emphasis = TopologyEdgeEmphasis.Strong;
                edge.LineStyle = TopologyEdgeLineStyle.Solid;
                edge.DashPattern.Clear();
                edge.Color = color;
                edge.Direction = step.TravelsBackward ? Primitives.VisualLinkDirection.Backward : Primitives.VisualLinkDirection.Forward;
            }
        }, stepOptions);
    }

    private const string ActiveStepColor = "#2563EB";
    private const string FailureColor = "#DC2626";

    /// <summary>
    /// Projects a scenario into the matching sequence diagram. Participants are the nodes in the order the scenario
    /// reaches them, edge steps become messages, and node steps become notes.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="scenarioId">The scenario id.</param>
    /// <returns>A sequence artifact whose messages carry <see cref="ScenarioStepMetadataKey"/>.</returns>
    public static SequenceArtifact ToSequenceArtifact(this TopologyChart chart, string scenarioId) {
        var resolved = TopologyScenarioResolver.Resolve(chart, scenarioId);
        var baseId = string.IsNullOrWhiteSpace(chart.Id) ? "topology" : chart.Id!;
        var sequence = SequenceArtifact.Create(baseId + "-" + resolved.Id + "-sequence").WithTitle(resolved.Label);
        if (!string.IsNullOrWhiteSpace(resolved.Scenario.Description)) sequence.WithSubtitle(resolved.Scenario.Description!);
        sequence.Theme = chart.Theme;
        sequence.ThemeMode = chart.ThemeMode;
        foreach (var nodeId in resolved.ParticipantNodeIds) {
            var node = chart.Nodes.First(candidate => string.Equals(candidate.Id, nodeId, StringComparison.Ordinal));
            sequence.AddParticipant(node.Id, string.IsNullOrWhiteSpace(node.Label) ? node.Id : node.Label, ParticipantKind(node.Kind));
        }

        foreach (var step in resolved.Steps) {
            var stepText = step.Index.ToString(CultureInfo.InvariantCulture);
            if (step.TargetNodeId == null) {
                sequence.AddNote(SequenceArtifactNotePlacement.Over, new[] { step.SourceNodeId }, step.Label);
                continue;
            }

            var (lineStyle, kind, text) = step.Interaction switch {
                TopologyScenarioInteraction.Response => (SequenceArtifactMessageLineStyle.Dashed, SequenceArtifactMessageKind.Return, step.Label),
                TopologyScenarioInteraction.Message => (SequenceArtifactMessageLineStyle.Solid, SequenceArtifactMessageKind.Async, step.Label),
                TopologyScenarioInteraction.Failure => (SequenceArtifactMessageLineStyle.Solid, SequenceArtifactMessageKind.Call, "✕ " + step.Label),
                _ => (SequenceArtifactMessageLineStyle.Solid, SequenceArtifactMessageKind.Call, step.Label)
            };
            sequence.AddMessage(step.SourceNodeId, step.TargetNodeId, text, lineStyle, kind);
            sequence.Messages[sequence.Messages.Count - 1].Metadata[ScenarioStepMetadataKey] = stepText;
        }

        return sequence;
    }

    private static SequenceArtifactParticipantKind ParticipantKind(TopologyNodeKind kind) => kind switch {
        TopologyNodeKind.Person => SequenceArtifactParticipantKind.Actor,
        TopologyNodeKind.Team => SequenceArtifactParticipantKind.Actor,
        TopologyNodeKind.Database => SequenceArtifactParticipantKind.Database,
        TopologyNodeKind.Storage => SequenceArtifactParticipantKind.Collections,
        TopologyNodeKind.Queue => SequenceArtifactParticipantKind.Queue,
        TopologyNodeKind.Gateway => SequenceArtifactParticipantKind.Boundary,
        _ => SequenceArtifactParticipantKind.Participant
    };
}
