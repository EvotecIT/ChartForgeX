using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Topology;

/// <summary>
/// A scenario whose steps are resolved against the topology, so hosts can show who sends what to whom.
/// </summary>
/// <remarks>
/// The same resolved steps drive topology playback, the matching sequence diagram, per-step renders and terminal views.
/// </remarks>
public sealed class TopologyResolvedScenario {
    internal TopologyResolvedScenario(TopologyScenario scenario, IReadOnlyList<TopologyResolvedScenarioStep> steps) {
        Scenario = scenario;
        Steps = steps;
    }

    /// <summary>Gets the source scenario.</summary>
    public TopologyScenario Scenario { get; }

    /// <summary>Gets the scenario id.</summary>
    public string Id => Scenario.Id;

    /// <summary>Gets the scenario label.</summary>
    public string Label => Scenario.Label;

    /// <summary>Gets the ordered resolved steps.</summary>
    public IReadOnlyList<TopologyResolvedScenarioStep> Steps { get; }

    /// <summary>
    /// Writes the steps as plain text, one numbered line per step, with payloads indented below.
    /// </summary>
    /// <param name="activeStepIndex">The zero-based step to mark, or <see langword="null"/>.</param>
    /// <param name="payloads">Which payloads to include: all, only the active step's, or none.</param>
    /// <param name="glyphs">Whether arrows and markers use Unicode or plain ASCII.</param>
    /// <returns>Text with <c>\n</c> line breaks.</returns>
    public string ToText(int? activeStepIndex = null, TopologyScenarioTextPayloads payloads = TopologyScenarioTextPayloads.All, TopologyCellGlyphSet glyphs = TopologyCellGlyphSet.Unicode) {
        var ascii = glyphs == TopologyCellGlyphSet.Ascii;
        var lines = new List<string> { Label + " (" + Steps.Count + (Steps.Count == 1 ? " step)" : " steps)") };
        if (!string.IsNullOrWhiteSpace(Scenario.Description)) lines.Add(Scenario.Description!.Trim());
        var number = Steps.Count.ToString(System.Globalization.CultureInfo.InvariantCulture).Length;
        foreach (var step in Steps) {
            var active = activeStepIndex == step.Index;
            var kind = step.Interaction == TopologyScenarioInteraction.None ? (step.TargetNodeId == null ? "visit" : "route") : step.Interaction.ToString().ToLowerInvariant();
            var route = step.TargetNodeId == null ? step.SourceLabel : step.SourceLabel + (ascii ? " -> " : " → ") + step.TargetLabel;
            lines.Add((active ? (ascii ? "> " : "▶ ") : "  ") + (step.Index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture).PadLeft(number) + ". " + route + "  [" + kind + "]  " + step.Label);
            var show = payloads == TopologyScenarioTextPayloads.All || payloads == TopologyScenarioTextPayloads.Active && active;
            if (!show || string.IsNullOrEmpty(step.Payload)) continue;
            foreach (var line in step.Payload!.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n')) lines.Add(new string(' ', number + 6) + line.TrimEnd());
        }

        return string.Join("\n", lines);
    }

    /// <summary>Gets the node ids in the order the scenario first reaches them.</summary>
    public IReadOnlyList<string> ParticipantNodeIds {
        get {
            var ids = new List<string>();
            foreach (var step in Steps) {
                if (!ids.Contains(step.SourceNodeId)) ids.Add(step.SourceNodeId);
                if (step.TargetNodeId != null && !ids.Contains(step.TargetNodeId)) ids.Add(step.TargetNodeId);
            }

            return ids;
        }
    }
}

/// <summary>
/// One scenario step resolved to the nodes and edge it touches.
/// </summary>
public sealed class TopologyResolvedScenarioStep {
    internal TopologyResolvedScenarioStep(int index, TopologyScenarioStep step, TopologyNode source, TopologyNode? target, TopologyEdge? edge) {
        Index = index;
        Step = step;
        SourceNodeId = source.Id;
        SourceLabel = DisplayLabel(source);
        TargetNodeId = target?.Id;
        TargetLabel = target == null ? null : DisplayLabel(target);
        EdgeId = edge?.Id;
        Label = FirstText(step.Label, edge?.Label, target == null ? SourceLabel : null) ?? step.Id;
        NodeIds = target == null || string.Equals(target.Id, source.Id, StringComparison.Ordinal) ? new[] { source.Id } : new[] { source.Id, target.Id };
        EdgeIds = edge == null ? Array.Empty<string>() : new[] { edge.Id };
        TravelsBackward = edge != null && !string.Equals(edge.SourceNodeId, source.Id, StringComparison.Ordinal);
    }

    /// <summary>Gets the zero-based step index.</summary>
    public int Index { get; }

    /// <summary>Gets the source step.</summary>
    public TopologyScenarioStep Step { get; }

    /// <summary>Gets whether the step references a node or an edge.</summary>
    public TopologyScenarioStepKind Kind => Step.Kind;

    /// <summary>Gets what happens on the edge.</summary>
    public TopologyScenarioInteraction Interaction => Step.Interaction;

    /// <summary>Gets the node the step starts at, or the visited node for node steps.</summary>
    public string SourceNodeId { get; }

    /// <summary>Gets the display label of the source node.</summary>
    public string SourceLabel { get; }

    /// <summary>Gets the node the step travels to, or <see langword="null"/> for node steps.</summary>
    public string? TargetNodeId { get; }

    /// <summary>Gets the display label of the target node.</summary>
    public string? TargetLabel { get; }

    /// <summary>Gets the edge the step travels over, or <see langword="null"/> for node steps.</summary>
    public string? EdgeId { get; }

    /// <summary>Gets the step label, falling back to the edge label or the visited node label.</summary>
    public string Label { get; }

    /// <summary>Gets whether the step travels its edge from the edge target to the edge source.</summary>
    public bool TravelsBackward { get; }

    /// <summary>Gets the optional payload.</summary>
    public string? Payload => Step.Payload;

    /// <summary>Gets the payload format.</summary>
    public TopologyScenarioPayloadFormat PayloadFormat => Step.PayloadFormat;

    /// <summary>Gets whether the step is a failed call.</summary>
    public bool IsFailure => Step.Interaction == TopologyScenarioInteraction.Failure;

    /// <summary>Gets the node ids to highlight while the step is active.</summary>
    public IReadOnlyList<string> NodeIds { get; }

    /// <summary>Gets the edge ids to highlight while the step is active.</summary>
    public IReadOnlyList<string> EdgeIds { get; }

    /// <summary>Gets a one-line summary such as <c>Admin → DC01: Modify user</c>.</summary>
    public string Summary => TargetLabel == null ? SourceLabel + ": " + Label : SourceLabel + " → " + TargetLabel + ": " + Label;

    private static string DisplayLabel(TopologyNode node) => string.IsNullOrWhiteSpace(node.Label) ? node.Id : node.Label;

    private static string? FirstText(params string?[] values) => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();
}

internal static class TopologyScenarioResolver {
    public static TopologyResolvedScenario Resolve(TopologyChart chart, string scenarioId) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        if (string.IsNullOrWhiteSpace(scenarioId)) throw new ArgumentException("A scenario id is required.", nameof(scenarioId));
        var scenario = chart.Scenarios.FirstOrDefault(candidate => string.Equals(candidate.Id, scenarioId.Trim(), StringComparison.Ordinal))
            ?? throw new ArgumentException("Topology scenario '" + scenarioId + "' does not exist.", nameof(scenarioId));
        return Resolve(chart, scenario, out var error) ?? throw new ArgumentException(error, nameof(scenarioId));
    }

    public static TopologyResolvedScenario? TryResolve(TopologyChart chart, TopologyScenario scenario) => Resolve(chart, scenario, out _);

    private static TopologyResolvedScenario? Resolve(TopologyChart chart, TopologyScenario scenario, out string error) {
        var nodes = new Dictionary<string, TopologyNode>(StringComparer.Ordinal);
        foreach (var node in chart.Nodes) {
            if (!string.IsNullOrWhiteSpace(node.Id) && !nodes.ContainsKey(node.Id)) nodes[node.Id] = node;
        }

        var edges = new Dictionary<string, TopologyEdge>(StringComparer.Ordinal);
        foreach (var edge in chart.Edges) {
            if (!string.IsNullOrWhiteSpace(edge.Id) && !edges.ContainsKey(edge.Id)) edges[edge.Id] = edge;
        }

        var steps = new List<TopologyResolvedScenarioStep>(scenario.Steps.Count);
        for (var index = 0; index < scenario.Steps.Count; index++) {
            var step = scenario.Steps[index];
            if (step.Kind == TopologyScenarioStepKind.Node) {
                if (!nodes.TryGetValue(step.Id, out var visited)) {
                    error = "Scenario '" + scenario.Id + "' step " + (index + 1) + " references missing node '" + step.Id + "'.";
                    return null;
                }

                steps.Add(new TopologyResolvedScenarioStep(index, step, visited, null, null));
                continue;
            }

            if (!edges.TryGetValue(step.Id, out var edge) || !nodes.TryGetValue(edge.SourceNodeId, out var from) || !nodes.TryGetValue(edge.TargetNodeId, out var to)) {
                error = "Scenario '" + scenario.Id + "' step " + (index + 1) + " references missing edge '" + step.Id + "' or one of its nodes.";
                return null;
            }

            steps.Add(TopologyScenarioTraversal.Reversed(scenario, index, edge)
                ? new TopologyResolvedScenarioStep(index, step, to, from, edge)
                : new TopologyResolvedScenarioStep(index, step, from, to, edge));
        }

        error = string.Empty;
        return new TopologyResolvedScenario(scenario, steps);
    }
}
