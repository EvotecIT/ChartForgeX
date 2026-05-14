namespace ChartForgeX.Topology;

internal static class TopologyScenarioCloner {
    public static TopologyScenario Clone(TopologyScenario scenario) {
        var copy = new TopologyScenario {
            Id = scenario.Id,
            Label = scenario.Label,
            Description = scenario.Description,
            Color = scenario.Color
        };
        foreach (var item in scenario.Metadata) copy.Metadata[item.Key] = item.Value;
        foreach (var step in scenario.Steps) {
            var stepCopy = new TopologyScenarioStep { Id = step.Id, Kind = step.Kind, Label = step.Label, Description = step.Description };
            foreach (var item in step.Metadata) stepCopy.Metadata[item.Key] = item.Value;
            copy.Steps.Add(stepCopy);
        }

        return copy;
    }
}
