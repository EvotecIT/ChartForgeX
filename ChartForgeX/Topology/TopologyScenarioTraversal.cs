namespace ChartForgeX.Topology;

/// <summary>Decides which way a scenario edge step travels, shared by resolution, sequences and motion.</summary>
internal static class TopologyScenarioTraversal {
    /// <summary>
    /// Returns whether the step travels its edge from target to source. Request-flow steps (with an interaction) and
    /// explicit <see cref="TopologyScenarioStep.Reverse"/> decide alone; plain route steps follow adjacent node steps
    /// when every available neighbour agrees on target-to-source travel.
    /// </summary>
    public static bool Reversed(TopologyScenario scenario, int index, TopologyEdge edge) {
        var step = scenario.Steps[index];
        if (step.Reverse || step.Interaction != TopologyScenarioInteraction.None) return step.Reverse;
        var previous = index > 0 && scenario.Steps[index - 1].Kind == TopologyScenarioStepKind.Node ? scenario.Steps[index - 1].Id : null;
        var following = index + 1 < scenario.Steps.Count && scenario.Steps[index + 1].Kind == TopologyScenarioStepKind.Node ? scenario.Steps[index + 1].Id : null;
        return ByNeighbours(edge, previous, following);
    }

    /// <summary>Uses adjacent node steps only when all available hints agree on target-to-source traversal.</summary>
    public static bool ByNeighbours(TopologyEdge edge, string? previousNode, string? followingNode) =>
        !string.Equals(edge.SourceNodeId, edge.TargetNodeId, System.StringComparison.Ordinal)
        && (previousNode != null || followingNode != null)
        && (previousNode == null || string.Equals(previousNode, edge.TargetNodeId, System.StringComparison.Ordinal))
        && (followingNode == null || string.Equals(followingNode, edge.SourceNodeId, System.StringComparison.Ordinal));
}