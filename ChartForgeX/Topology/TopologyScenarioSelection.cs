using System;
using System.Collections.Generic;
using System.Linq;

namespace ChartForgeX.Topology;

internal sealed class TopologyScenarioSelection {
    private TopologyScenarioSelection(string id) {
        Id = id;
    }

    public string Id { get; }

    public HashSet<string> GroupIds { get; } = new(StringComparer.Ordinal);

    public HashSet<string> NodeIds { get; } = new(StringComparer.Ordinal);

    public HashSet<string> EdgeIds { get; } = new(StringComparer.Ordinal);

    public static TopologyScenarioSelection? From(TopologyChart chart, string? scenarioId) {
        var scenario = ResolveScenario(chart, scenarioId);
        if (scenario == null) return null;
        var selection = new TopologyScenarioSelection(scenario.Id);
        var edgesById = chart.Edges
            .Where(edge => !string.IsNullOrWhiteSpace(edge.Id))
            .GroupBy(edge => edge.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        foreach (var step in scenario.Steps) {
            if (step.Kind == TopologyScenarioStepKind.Node) {
                selection.NodeIds.Add(step.Id);
                continue;
            }

            if (!edgesById.TryGetValue(step.Id, out var edges)) continue;
            foreach (var edge in edges) {
                selection.EdgeIds.Add(edge.Id);
                selection.NodeIds.Add(edge.SourceNodeId);
                selection.NodeIds.Add(edge.TargetNodeId);
            }
        }

        var groupsByNode = chart.Nodes
            .Where(node => !string.IsNullOrWhiteSpace(node.GroupId))
            .GroupBy(node => node.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().GroupId!, StringComparer.Ordinal);
        foreach (var nodeId in selection.NodeIds) {
            if (groupsByNode.TryGetValue(nodeId, out var groupId)) selection.GroupIds.Add(groupId);
        }

        return selection;
    }

    public static string? ResolveActiveScenarioId(TopologyChart chart, string? scenarioId) =>
        ResolveScenario(chart, scenarioId)?.Id;

    private static TopologyScenario? ResolveScenario(TopologyChart chart, string? scenarioId) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        if (chart.Scenarios.Count == 0 || string.IsNullOrWhiteSpace(scenarioId)) return null;
        var requested = scenarioId!.Trim();
        foreach (var scenario in chart.Scenarios) {
            if (string.Equals(scenario.Id, requested, StringComparison.Ordinal)) return scenario;
        }

        return null;
    }
}
