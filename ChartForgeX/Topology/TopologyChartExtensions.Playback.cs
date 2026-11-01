using System.Collections.Generic;
using System;
using System.Globalization;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
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
    /// Projects a scenario into the matching sequence diagram, drawn in the topology's visual language: participants
    /// are node cards (icon, subtitle, status), groups become participant bands, messages use the topology's neutral
    /// edge color with label pills, replies are dashed, and failed calls take the critical status color.
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
        sequence.Padding = chart.Viewport.Padding;
        var paint = new TopologyPaint(chart.Theme, chart.ThemeMode);
        var neutral = SequenceColor(paint.Unknown, paint.Border);
        var card = SequenceColor(paint.Card, paint.Surface);
        var border = SequenceColor(paint.Border, paint.Unknown);
        sequence.Paint.MessageLine = neutral;
        sequence.Paint.MessageText = SequenceColor(paint.Foreground, paint.Foreground);
        sequence.Paint.MessageLabelFill = card;
        sequence.Paint.MessageLabelBorder = border;
        sequence.Paint.NoteFill = card;
        sequence.Paint.NoteBorder = border;
        sequence.Paint.NoteText = SequenceColor(paint.Foreground, paint.Foreground);

        var nodes = chart.Nodes.GroupBy(node => node.Id, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var order = GroupedParticipants(resolved.ParticipantNodeIds, nodes);
        foreach (var nodeId in order) {
            var node = nodes[nodeId];
            var label = string.IsNullOrWhiteSpace(node.Label) ? node.Id : node.Label;
            var status = StatusColor(paint, node.Status);
            sequence.AddParticipant(node.Id, label, SequenceArtifactParticipantKind.Participant);
            var participant = sequence.Participants[sequence.Participants.Count - 1];
            participant.Card = new SequenceParticipantCard {
                Subtitle = node.Subtitle,
                IconKind = node.Kind,
                IconShape = node.IconShape ?? TopologyIconShape.Auto,
                IconColor = status ?? SequenceColor(paint.MutedForeground, paint.Unknown),
                StatusColor = status ?? neutral
            };
            participant.Fill = card;
            participant.Border = status ?? border;
            participant.MinimumWidth = Math.Max(150, Math.Max(label.Length * 7.6, (node.Subtitle ?? string.Empty).Length * 6.4) + 70);
        }

        AddGroupBands(chart, sequence, order, nodes, paint);
        foreach (var step in resolved.Steps) {
            var stepText = step.Index.ToString(CultureInfo.InvariantCulture);
            if (step.TargetNodeId == null) {
                sequence.AddNote(SequenceArtifactNotePlacement.Over, new[] { step.SourceNodeId }, step.Label);
                continue;
            }

            var (lineStyle, kind) = step.Interaction switch {
                TopologyScenarioInteraction.Response => (SequenceArtifactMessageLineStyle.Dashed, SequenceArtifactMessageKind.Return),
                TopologyScenarioInteraction.Message => (SequenceArtifactMessageLineStyle.Solid, SequenceArtifactMessageKind.Async),
                _ => (SequenceArtifactMessageLineStyle.Solid, SequenceArtifactMessageKind.Call)
            };
            sequence.AddMessage(step.SourceNodeId, step.TargetNodeId, step.IsFailure ? "✕ " + step.Label : step.Label, lineStyle, kind);
            var message = sequence.Messages[sequence.Messages.Count - 1];
            message.Metadata[ScenarioStepMetadataKey] = stepText;
            if (step.IsFailure) message.Color = SequenceColor(paint.Critical, paint.Unknown);
        }

        return sequence;
    }

    /// <summary>Keeps the members of each group next to each other, so groups can show as participant bands.</summary>
    private static List<string> GroupedParticipants(IReadOnlyList<string> appearance, Dictionary<string, TopologyNode> nodes) {
        var order = new List<string>();
        foreach (var id in appearance) {
            if (order.Contains(id)) continue;
            var group = nodes[id].GroupId;
            if (string.IsNullOrWhiteSpace(group)) { order.Add(id); continue; }
            order.AddRange(appearance.Where(member => !order.Contains(member) && string.Equals(nodes[member].GroupId, group, StringComparison.Ordinal)));
        }

        return order;
    }

    private static void AddGroupBands(TopologyChart chart, SequenceArtifact sequence, List<string> order, Dictionary<string, TopologyNode> nodes, TopologyPaint paint) {
        for (var index = 0; index < order.Count;) {
            var groupId = nodes[order[index]].GroupId;
            var last = index;
            while (last + 1 < order.Count && groupId != null && string.Equals(nodes[order[last + 1]].GroupId, groupId, StringComparison.Ordinal)) last++;
            var group = groupId == null ? null : chart.Groups.FirstOrDefault(candidate => string.Equals(candidate.Id, groupId, StringComparison.Ordinal));
            if (group != null) {
                var tint = ChartColor.TryParse(group.Color, out var color) ? color : SequenceColor(paint.Border, paint.Unknown);
                sequence.AddParticipantGroup(group.Id, group.Label, index, last, ChartColorMath.WithOpacity(tint, group.DashPattern.Count > 0 ? .08 : .14));
            }

            index = last + 1;
        }
    }

    private static ChartColor? StatusColor(TopologyPaint paint, TopologyHealthStatus status) => status switch {
        TopologyHealthStatus.Healthy => SequenceColor(paint.Healthy, paint.Healthy),
        TopologyHealthStatus.Warning => SequenceColor(paint.Warning, paint.Warning),
        TopologyHealthStatus.Critical => SequenceColor(paint.Critical, paint.Critical),
        TopologyHealthStatus.Disabled => SequenceColor(paint.Disabled, paint.Disabled),
        _ => null
    };

    private static ChartColor SequenceColor(string value, string fallback) =>
        ChartColor.TryParse(value, out var color) ? color : ChartColor.TryParse(fallback, out var second) ? second : ChartColor.FromHex("#64748B");
}