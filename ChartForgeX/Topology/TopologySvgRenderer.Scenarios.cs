using System;
using System.Linq;
using ChartForgeX.Svg;

namespace ChartForgeX.Topology;

public sealed partial class TopologySvgRenderer {
    private static void AddScenarioDataAttributes(SvgElement element, TopologyChart chart, TopologyScenarioStepKind kind, string elementId) {
        if (chart.Scenarios.Count == 0) return;
        var memberships = chart.Scenarios
            .Select(scenario => new {
                Scenario = scenario,
                StepIndices = ScenarioStepIndices(chart, scenario, kind, elementId)
            })
            .Where(item => item.StepIndices.Length > 0)
            .ToArray();
        if (memberships.Length == 0) return;
        var scenarioIds = memberships.Select(item => item.Scenario.Id).ToArray();
        var stepIndexTokens = memberships.Select(item => item.Scenario.Id + ":" + string.Join(",", item.StepIndices)).ToArray();
        element
            .Attribute("data-scenario-ids", string.Join(" ", scenarioIds))
            .Attribute("data-scenario-count", memberships.Length)
            .Attribute("data-scenario-step-count", memberships.Sum(item => item.StepIndices.Length))
            .Attribute("data-scenario-step-indices", string.Join(" ", stepIndexTokens));
    }

    private static int[] ScenarioStepIndices(TopologyChart chart, TopologyScenario scenario, TopologyScenarioStepKind kind, string elementId) {
        return scenario.Steps
            .Select((step, index) => new { Step = step, Index = index })
            .Where(item => ScenarioStepMatches(chart, item.Step, kind, elementId))
            .Select(item => item.Index)
            .ToArray();
    }

    private static bool ScenarioStepMatches(TopologyChart chart, TopologyScenarioStep step, TopologyScenarioStepKind kind, string elementId) {
        if (step.Kind == kind && string.Equals(step.Id, elementId, StringComparison.Ordinal)) return true;
        if (kind != TopologyScenarioStepKind.Node || step.Kind != TopologyScenarioStepKind.Edge) return false;
        return chart.Edges.Any(candidate =>
            string.Equals(candidate.Id, step.Id, StringComparison.Ordinal) &&
            (string.Equals(candidate.SourceNodeId, elementId, StringComparison.Ordinal) || string.Equals(candidate.TargetNodeId, elementId, StringComparison.Ordinal)));
    }
}
