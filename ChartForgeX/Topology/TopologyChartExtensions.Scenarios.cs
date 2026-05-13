using System;

namespace ChartForgeX.Topology;

public static partial class TopologyChartExtensions {
    /// <summary>
    /// Adds an interactive scenario that references existing topology nodes and edges.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="id">The scenario id.</param>
    /// <param name="label">The scenario label.</param>
    /// <param name="configure">Optional scenario configuration.</param>
    /// <returns>The current topology chart.</returns>
    public static TopologyChart AddScenario(this TopologyChart chart, string id, string label, Action<TopologyScenario>? configure = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        var scenario = new TopologyScenario {
            Id = RequiredText(id, nameof(id), "Topology scenario ids"),
            Label = RequiredText(label, nameof(label), "Topology scenario labels")
        };
        configure?.Invoke(scenario);
        chart.Scenarios.Add(scenario);
        return chart;
    }

    /// <summary>
    /// Sets an optional scenario accent color.
    /// </summary>
    /// <param name="scenario">The topology scenario.</param>
    /// <param name="color">The scenario color.</param>
    /// <returns>The current scenario.</returns>
    public static TopologyScenario WithColor(this TopologyScenario scenario, string? color) {
        if (scenario == null) throw new ArgumentNullException(nameof(scenario));
        scenario.Color = color;
        return scenario;
    }

    /// <summary>
    /// Sets an optional scenario description.
    /// </summary>
    /// <param name="scenario">The topology scenario.</param>
    /// <param name="description">The scenario description.</param>
    /// <returns>The current scenario.</returns>
    public static TopologyScenario WithDescription(this TopologyScenario scenario, string? description) {
        if (scenario == null) throw new ArgumentNullException(nameof(scenario));
        scenario.Description = description;
        return scenario;
    }

    /// <summary>
    /// Adds host-readable metadata to a topology scenario.
    /// </summary>
    /// <param name="scenario">The topology scenario.</param>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <returns>The current scenario.</returns>
    public static TopologyScenario WithMetadata(this TopologyScenario scenario, string key, string? value) {
        if (scenario == null) throw new ArgumentNullException(nameof(scenario));
        scenario.Metadata[RequiredText(key, nameof(key), "Topology scenario metadata keys")] = value ?? string.Empty;
        return scenario;
    }

    /// <summary>
    /// Adds host-readable metadata to a topology scenario step.
    /// </summary>
    /// <param name="step">The topology scenario step.</param>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <returns>The current scenario step.</returns>
    public static TopologyScenarioStep WithMetadata(this TopologyScenarioStep step, string key, string? value) {
        if (step == null) throw new ArgumentNullException(nameof(step));
        step.Metadata[RequiredText(key, nameof(key), "Topology scenario step metadata keys")] = value ?? string.Empty;
        return step;
    }

    /// <summary>
    /// Adds a scenario hop that references an existing topology node.
    /// </summary>
    /// <param name="scenario">The topology scenario.</param>
    /// <param name="nodeId">The node id.</param>
    /// <param name="label">The optional hop label.</param>
    /// <param name="description">The optional hop description.</param>
    /// <param name="configure">Optional step configuration.</param>
    /// <returns>The current scenario.</returns>
    public static TopologyScenario AddNodeStep(this TopologyScenario scenario, string nodeId, string? label = null, string? description = null, Action<TopologyScenarioStep>? configure = null) {
        return AddStep(scenario, TopologyScenarioStepKind.Node, nodeId, label, description, configure);
    }

    /// <summary>
    /// Adds a scenario segment that references an existing topology edge.
    /// </summary>
    /// <param name="scenario">The topology scenario.</param>
    /// <param name="edgeId">The edge id.</param>
    /// <param name="label">The optional segment label.</param>
    /// <param name="description">The optional segment description.</param>
    /// <param name="configure">Optional step configuration.</param>
    /// <returns>The current scenario.</returns>
    public static TopologyScenario AddEdgeStep(this TopologyScenario scenario, string edgeId, string? label = null, string? description = null, Action<TopologyScenarioStep>? configure = null) {
        return AddStep(scenario, TopologyScenarioStepKind.Edge, edgeId, label, description, configure);
    }

    private static TopologyScenario AddStep(TopologyScenario scenario, TopologyScenarioStepKind kind, string id, string? label, string? description, Action<TopologyScenarioStep>? configure) {
        if (scenario == null) throw new ArgumentNullException(nameof(scenario));
        ValidateEnum(typeof(TopologyScenarioStepKind), kind, nameof(kind), "Topology scenario step kinds");
        var step = new TopologyScenarioStep {
            Id = RequiredText(id, nameof(id), "Topology scenario step ids"),
            Kind = kind,
            Label = label,
            Description = description
        };
        configure?.Invoke(step);
        scenario.Steps.Add(step);
        return scenario;
    }
}
